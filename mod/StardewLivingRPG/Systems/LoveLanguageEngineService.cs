using StardewLivingRPG.CustomNpcFramework.Models;
using StardewLivingRPG.State;
using StardewModdingAPI;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class LoveLanguageEngineService
{
    private const int MaxRecentSignals = 40;
    private const int MicroDateSignalWindowDays = 14;
    private const int MinMicroDatePositiveSignals = 2;
    private const int MinMicroDateTrust = 8;
    private const int MinMicroDateSafety = 8;
    private readonly Func<string, LoveLanguageNpcConfig?> _configResolver;
    private readonly IMonitor? _monitor;
    private readonly int _maxFriendshipPointsPerChat;
    private readonly int _friendshipDailyCap;

    public LoveLanguageEngineService(
        Func<string, LoveLanguageNpcConfig?> configResolver,
        IMonitor? monitor = null,
        int maxFriendshipPointsPerChat = 20,
        int friendshipDailyCap = 40)
    {
        _configResolver = configResolver ?? throw new ArgumentNullException(nameof(configResolver));
        _monitor = monitor;
        _maxFriendshipPointsPerChat = Math.Max(1, maxFriendshipPointsPerChat);
        _friendshipDailyCap = Math.Max(_maxFriendshipPointsPerChat, friendshipDailyCap);
    }

    public LoveLanguageNpcConfig? TryGetConfig(string? npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return null;

        var config = _configResolver(npcName.Trim());
        if (config is null)
            return null;

        if (!string.Equals(config.Mechanic, "LoveLanguageEngine", StringComparison.OrdinalIgnoreCase))
            return null;

        return config;
    }

    public bool TryBuildPromptBlock(SaveState state, string? npcName, int day, out string block)
    {
        block = string.Empty;
        if (state is null || string.IsNullOrWhiteSpace(npcName))
            return false;

        var config = TryGetConfig(npcName);
        if (config is null)
            return false;

        var token = NormalizeToken(npcName);
        var profile = GetOrCreateProfile(state, token, config.ProfileAxes, day);
        var profileAxes = NormalizeDistinct(config.ProfileAxes);
        var axesText = profileAxes.Count == 0
            ? "none"
            : string.Join(", ", profileAxes.Select(axis => $"{axis}:{GetAxisValue(profile, axis)}"));

        var nextBeats = NormalizeDistinct(config.LLMOutputContract.NextBeatAllowed);
        var requiredFields = NormalizeDistinct(config.LLMOutputContract.RequiredFields);
        var objectiveTypes = NormalizeDistinct(config.MicroDateWhitelist.ObjectiveTypes);
        var rewardBundles = NormalizeDistinct(config.MicroDateWhitelist.RewardBundles);

        var microDateText = "none";
        if (state.Romance.ActiveMicroDates.TryGetValue(token, out var microDate)
            && microDate is not null
            && string.Equals(microDate.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            microDateText = $"active(type={TrimForPrompt(microDate.ObjectiveType, 24)}, payload={TrimForPrompt(microDate.ObjectivePayload, 90)}, reward={TrimForPrompt(microDate.RewardBundle, 40)}, expiresDay={microDate.ExpiresDay})";
        }

        block = string.Join(" ",
            $"ROMANCE_PROFILE[{token}]: axes=[{axesText}] trust={profile.Trust} safety={profile.Safety} nextBeat={profile.NextBeat}.",
            $"ROMANCE_WHITELIST[{token}]: objectiveTypes=[{string.Join(", ", objectiveTypes)}] rewardBundles=[{string.Join(", ", rewardBundles)}] nextBeatAllowed=[{string.Join(", ", nextBeats)}] requiredFields=[{string.Join(", ", requiredFields)}].",
            $"ROMANCE_MICRO_DATE[{token}]: {microDateText}.",
            $"ROMANCE_GATE[{token}]: propose_micro_date requires trust>={MinMicroDateTrust}, safety>={MinMicroDateSafety}, positiveSignals(last{MicroDateSignalWindowDays}d)>={MinMicroDatePositiveSignals}, nextBeat!=conflict, and no active micro-date.",
            "ROMANCE_RULES: If ROMANCE_GATE is unmet, do not promise date plans, meet times, or meet locations. Decline or defer naturally in-character.",
            "ROMANCE_RULES: Emit at most one romance command in a reply. Use romance commands only when context is genuinely relational and avoid forced progression.",
            "ROMANCE_RULES: signal_deltas values and trust/safety deltas must stay within -5..+5. Emit no romance command when confidence < 0.55.");

        if (block.Length > 1200)
            block = block[..1200];

        return true;
    }

    public LoveLanguageApplyResult ApplyProfileUpdate(SaveState state, string npcName, string intentId, RomanceProfileUpdateCommand command)
    {
        if (state is null)
            return LoveLanguageApplyResult.Rejected("state missing", "E_ROMANCE_STATE_MISSING");

        var config = TryGetConfig(npcName);
        if (config is null)
            return LoveLanguageApplyResult.Rejected($"romance config missing for '{npcName}'", "E_ROMANCE_CONFIG_MISSING");

        if (command.Confidence < 0f || command.Confidence > 1f)
            return LoveLanguageApplyResult.Rejected("romance confidence out of range (0..1)", "E_ROMANCE_CONFIDENCE_RANGE");

        var allowedAxes = new HashSet<string>(NormalizeDistinct(config.ProfileAxes), StringComparer.OrdinalIgnoreCase);
        if (allowedAxes.Count == 0)
            return LoveLanguageApplyResult.Rejected("romance profile axes are missing", "E_ROMANCE_AXES_EMPTY");

        var token = NormalizeToken(npcName);
        var profile = GetOrCreateProfile(state, token, config.ProfileAxes, state.Calendar.Day);

        var appliedAxisDeltas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in command.SignalDeltas)
        {
            var axis = NormalizeToken(kv.Key);
            if (string.IsNullOrWhiteSpace(axis))
                continue;
            if (!allowedAxes.Contains(axis))
                return LoveLanguageApplyResult.Rejected($"unknown romance axis '{axis}'", "E_ROMANCE_AXIS_INVALID");

            var delta = Math.Clamp(kv.Value, -5, 5);
            if (delta == 0)
                continue;

            profile.Axes.TryGetValue(axis, out var current);
            profile.Axes[axis] = Math.Clamp(current + delta, 0, 100);
            appliedAxisDeltas[axis] = delta;
            profile.RecentSignals.Add(new RomanceSignalEntry
            {
                Day = state.Calendar.Day,
                Axis = axis,
                Delta = delta,
                Confidence = Math.Clamp(command.Confidence, 0f, 1f),
                Evidence = TrimForPrompt(command.Evidence, 160)
            });
        }

        var allowedBeats = NormalizeDistinct(config.LLMOutputContract.NextBeatAllowed);
        var normalizedBeat = NormalizeToken(command.NextBeat);
        if (!string.IsNullOrWhiteSpace(normalizedBeat))
        {
            if (allowedBeats.Count > 0 && !allowedBeats.Contains(normalizedBeat, StringComparer.OrdinalIgnoreCase))
                return LoveLanguageApplyResult.Rejected($"next_beat '{normalizedBeat}' is not allowed", "E_ROMANCE_NEXT_BEAT_INVALID");

            profile.NextBeat = normalizedBeat;
        }

        profile.Trust = Math.Clamp(profile.Trust + Math.Clamp(command.TrustDelta, -5, 5), -100, 100);
        profile.Safety = Math.Clamp(profile.Safety + Math.Clamp(command.SafetyDelta, -5, 5), -100, 100);
        profile.LastUpdatedDay = state.Calendar.Day;

        if (profile.RecentSignals.Count > MaxRecentSignals)
            profile.RecentSignals = profile.RecentSignals.TakeLast(MaxRecentSignals).ToList();

        var friendshipPointsApplied = ApplyVanillaFriendshipSync(state, token, intentId, command, appliedAxisDeltas);
        state.Facts.Facts[$"romance:profile:{state.Calendar.Day}:{token}:{intentId}"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "romance_engine"
        };

        return LoveLanguageApplyResult.Success(
            outcomeId: $"romance_profile:{token}",
            friendshipPointsApplied: friendshipPointsApplied,
            appliedAxisDeltas: appliedAxisDeltas);
    }

    public LoveLanguageApplyResult ApplyMicroDateProposal(SaveState state, string npcName, string intentId, MicroDateProposalCommand command)
    {
        if (state is null)
            return LoveLanguageApplyResult.Rejected("state missing", "E_ROMANCE_STATE_MISSING");

        var config = TryGetConfig(npcName);
        if (config is null)
            return LoveLanguageApplyResult.Rejected($"romance config missing for '{npcName}'", "E_ROMANCE_CONFIG_MISSING");

        var eligibility = EvaluateMicroDateEligibility(state, npcName);
        if (!eligibility.Eligible)
            return LoveLanguageApplyResult.Rejected(eligibility.Reason, eligibility.ReasonCode);

        var objectiveType = NormalizeToken(command.ObjectiveType);
        var rewardBundle = (command.RewardBundle ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(objectiveType))
            return LoveLanguageApplyResult.Rejected("objective_type missing", "E_ROMANCE_MICRO_DATE_OBJECTIVE_INVALID");
        if (string.IsNullOrWhiteSpace(rewardBundle))
            return LoveLanguageApplyResult.Rejected("reward_bundle missing", "E_ROMANCE_MICRO_DATE_REWARD_INVALID");

        var allowedTypes = new HashSet<string>(NormalizeDistinct(config.MicroDateWhitelist.ObjectiveTypes), StringComparer.OrdinalIgnoreCase);
        if (allowedTypes.Count > 0 && !allowedTypes.Contains(objectiveType))
            return LoveLanguageApplyResult.Rejected($"objective_type '{objectiveType}' not in whitelist", "E_ROMANCE_MICRO_DATE_OBJECTIVE_INVALID");

        var allowedRewards = new HashSet<string>(NormalizeDistinct(config.MicroDateWhitelist.RewardBundles), StringComparer.OrdinalIgnoreCase);
        if (allowedRewards.Count > 0 && !allowedRewards.Contains(rewardBundle))
            return LoveLanguageApplyResult.Rejected($"reward_bundle '{rewardBundle}' not in whitelist", "E_ROMANCE_MICRO_DATE_REWARD_INVALID");

        var payload = (command.ObjectivePayload ?? string.Empty).Trim();
        if (payload.Length < 4)
            return LoveLanguageApplyResult.Rejected("objective_payload is too short", "E_ROMANCE_MICRO_DATE_PAYLOAD_INVALID");

        var expiryDays = Math.Clamp(command.ExpiryDays, 1, 3);
        var token = NormalizeToken(npcName);
        state.Romance.ActiveMicroDates[token] = new MicroDateState
        {
            ObjectiveType = objectiveType,
            ObjectivePayload = TrimForPrompt(payload, 160),
            RewardBundle = rewardBundle,
            IssuedDay = state.Calendar.Day,
            ExpiresDay = state.Calendar.Day + expiryDays,
            Status = "active"
        };

        state.Facts.Facts[$"romance:micro_date:{state.Calendar.Day}:{token}:{intentId}"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "romance_engine"
        };

        return LoveLanguageApplyResult.Success($"micro_date:{token}:{state.Calendar.Day}", 0, new Dictionary<string, int>());
    }

    public MicroDateEligibilityResult EvaluateMicroDateEligibility(SaveState state, string npcName)
    {
        if (state is null)
            return MicroDateEligibilityResult.Denied("E_ROMANCE_STATE_MISSING", "state missing");

        var config = TryGetConfig(npcName);
        if (config is null)
            return MicroDateEligibilityResult.Denied("E_ROMANCE_CONFIG_MISSING", $"romance config missing for '{npcName}'");

        var token = NormalizeToken(npcName);
        var profile = GetOrCreateProfile(state, token, config.ProfileAxes, state.Calendar.Day);
        var recentWindowStart = Math.Max(1, state.Calendar.Day - MicroDateSignalWindowDays + 1);
        var positiveSignals = profile.RecentSignals.Count(signal =>
            signal is not null
            && signal.Delta > 0
            && signal.Day >= recentWindowStart);
        var hasActiveMicroDate = state.Romance.ActiveMicroDates.TryGetValue(token, out var activeMicroDate)
            && activeMicroDate is not null
            && string.Equals(activeMicroDate.Status, "active", StringComparison.OrdinalIgnoreCase)
            && activeMicroDate.ExpiresDay >= state.Calendar.Day;
        var nextBeat = NormalizeToken(profile.NextBeat);

        if (hasActiveMicroDate)
        {
            return MicroDateEligibilityResult.Denied(
                "E_ROMANCE_MICRO_DATE_ALREADY_ACTIVE",
                "an active micro-date already exists",
                trust: profile.Trust,
                safety: profile.Safety,
                positiveSignals: positiveSignals,
                nextBeat: nextBeat,
                hasActiveMicroDate: true);
        }

        if (nextBeat.Equals("conflict", StringComparison.OrdinalIgnoreCase))
        {
            return MicroDateEligibilityResult.Denied(
                "E_ROMANCE_MICRO_DATE_BEAT_CONFLICT",
                "micro-date blocked while relationship beat is conflict",
                trust: profile.Trust,
                safety: profile.Safety,
                positiveSignals: positiveSignals,
                nextBeat: nextBeat,
                hasActiveMicroDate: false);
        }

        if (profile.Trust < MinMicroDateTrust
            || profile.Safety < MinMicroDateSafety
            || positiveSignals < MinMicroDatePositiveSignals)
        {
            var reason =
                $"micro-date gate unmet: trust={profile.Trust}/{MinMicroDateTrust}, safety={profile.Safety}/{MinMicroDateSafety}, positiveSignals={positiveSignals}/{MinMicroDatePositiveSignals} (last {MicroDateSignalWindowDays} days)";
            return MicroDateEligibilityResult.Denied(
                "E_ROMANCE_MICRO_DATE_RELATIONSHIP_LOW",
                reason,
                trust: profile.Trust,
                safety: profile.Safety,
                positiveSignals: positiveSignals,
                nextBeat: nextBeat,
                hasActiveMicroDate: false);
        }

        return MicroDateEligibilityResult.Allowed(
            trust: profile.Trust,
            safety: profile.Safety,
            positiveSignals: positiveSignals,
            nextBeat: nextBeat,
            hasActiveMicroDate: false);
    }

    public int ExpireMicroDates(SaveState state, int currentDay)
    {
        if (state?.Romance?.ActiveMicroDates is null || state.Romance.ActiveMicroDates.Count == 0)
            return 0;

        var expired = new List<string>();
        foreach (var (npcToken, microDate) in state.Romance.ActiveMicroDates)
        {
            if (microDate is null)
                continue;
            if (!string.Equals(microDate.Status, "active", StringComparison.OrdinalIgnoreCase))
                continue;
            if (microDate.ExpiresDay >= currentDay)
                continue;

            expired.Add(npcToken);
        }

        foreach (var npcToken in expired)
        {
            state.Romance.ActiveMicroDates.Remove(npcToken);
            state.Facts.Facts[$"romance:micro_date_expired:{currentDay}:{npcToken}"] = new FactValue
            {
                Value = true,
                SetDay = currentDay,
                Source = "romance_engine"
            };
        }

        return expired.Count;
    }

    private int ApplyVanillaFriendshipSync(
        SaveState state,
        string npcToken,
        string intentId,
        RomanceProfileUpdateCommand command,
        IReadOnlyDictionary<string, int> appliedAxisDeltas)
    {
        if (Game1.player?.friendshipData is null)
            return 0;

        if (!TryResolveFriendshipKey(npcToken, out var friendshipKey))
            return 0;

        var signalSum = appliedAxisDeltas.Values.Sum();
        var trustWeight = Math.Clamp(command.TrustDelta, -5, 5) * 2;
        var rawPoints = (int)Math.Round((signalSum + trustWeight) * 1.5, MidpointRounding.AwayFromZero);
        var boundedPerChat = Math.Clamp(rawPoints, -_maxFriendshipPointsPerChat, _maxFriendshipPointsPerChat);
        if (boundedPerChat == 0)
            return 0;

        var day = state.Calendar.Day;
        var dailyNet = GetDailyRomanceFriendshipNet(state, day, npcToken);
        var maxUp = _friendshipDailyCap - dailyNet;
        var maxDown = -_friendshipDailyCap - dailyNet;
        var boundedDaily = Math.Clamp(boundedPerChat, maxDown, maxUp);
        if (boundedDaily == 0)
            return 0;

        var npc = Game1.getCharacterFromName(friendshipKey);
        if (npc is null)
            return 0;

        try
        {
            Game1.player.changeFriendship(boundedDaily, npc);
        }
        catch (Exception ex)
        {
            _monitor?.Log($"LoveLanguageEngine friendship sync skipped for '{friendshipKey}': {ex.Message}", LogLevel.Trace);
            return 0;
        }

        state.Facts.Facts[$"romance:vanilla_sync:day:{day}:{npcToken}:pts:{boundedDaily}:{intentId}"] = new FactValue
        {
            Value = true,
            SetDay = day,
            Source = "romance_engine"
        };

        return boundedDaily;
    }

    private static int GetDailyRomanceFriendshipNet(SaveState state, int day, string npcToken)
    {
        var prefix = $"romance:vanilla_sync:day:{day}:{npcToken}:pts:";
        var total = 0;

        foreach (var key in state.Facts.Facts.Keys)
        {
            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var rest = key[prefix.Length..];
            var separator = rest.IndexOf(':');
            var token = separator >= 0 ? rest[..separator] : rest;
            if (int.TryParse(token, out var points))
                total += points;
        }

        return total;
    }

    private static bool TryResolveFriendshipKey(string npcName, out string friendshipKey)
    {
        friendshipKey = string.Empty;
        if (Game1.player?.friendshipData is null || string.IsNullOrWhiteSpace(npcName))
            return false;

        if (Game1.player.friendshipData.ContainsKey(npcName))
        {
            friendshipKey = npcName;
            return true;
        }

        foreach (var key in Game1.player.friendshipData.Keys)
        {
            if (!key.Equals(npcName, StringComparison.OrdinalIgnoreCase))
                continue;

            friendshipKey = key;
            return true;
        }

        return false;
    }

    private static LoveLanguageProfile GetOrCreateProfile(SaveState state, string npcToken, IReadOnlyList<string> configuredAxes, int currentDay)
    {
        if (!state.Romance.Profiles.TryGetValue(npcToken, out var profile) || profile is null)
        {
            profile = new LoveLanguageProfile();
            state.Romance.Profiles[npcToken] = profile;
        }

        var axes = NormalizeDistinct(configuredAxes);
        foreach (var axis in axes)
        {
            if (!profile.Axes.ContainsKey(axis))
                profile.Axes[axis] = 50;
        }

        if (string.IsNullOrWhiteSpace(profile.NextBeat))
            profile.NextBeat = "warmth";
        if (profile.LastUpdatedDay <= 0)
            profile.LastUpdatedDay = Math.Max(1, currentDay);

        return profile;
    }

    private static int GetAxisValue(LoveLanguageProfile profile, string axis)
    {
        if (profile.Axes.TryGetValue(axis, out var value))
            return value;

        return 50;
    }

    private static List<string> NormalizeDistinct(IEnumerable<string>? values)
    {
        var result = new List<string>();
        if (values is null)
            return result;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            var normalized = NormalizeToken(value);
            if (string.IsNullOrWhiteSpace(normalized))
                continue;
            if (seen.Add(normalized))
                result.Add(normalized);
        }

        return result;
    }

    private static string NormalizeToken(string? value)
    {
        var normalized = (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal);

        while (normalized.Contains("__", StringComparison.Ordinal))
            normalized = normalized.Replace("__", "_", StringComparison.Ordinal);

        return normalized.Trim('_');
    }

    private static string TrimForPrompt(string? value, int maxLength)
    {
        var text = (value ?? string.Empty).Trim();
        if (text.Length <= maxLength)
            return text;

        return text[..Math.Max(1, maxLength - 3)] + "...";
    }
}

public sealed class RomanceProfileUpdateCommand
{
    public IReadOnlyDictionary<string, int> SignalDeltas { get; init; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public int TrustDelta { get; init; }
    public int SafetyDelta { get; init; }
    public string NextBeat { get; init; } = "warmth";
    public float Confidence { get; init; } = 1f;
    public string Evidence { get; init; } = string.Empty;
}

public sealed class MicroDateProposalCommand
{
    public string ObjectiveType { get; init; } = string.Empty;
    public string ObjectivePayload { get; init; } = string.Empty;
    public string RewardBundle { get; init; } = string.Empty;
    public int ExpiryDays { get; init; } = 2;
}

public sealed class MicroDateEligibilityResult
{
    public bool Eligible { get; init; }
    public string ReasonCode { get; init; } = "OK";
    public string Reason { get; init; } = string.Empty;
    public int Trust { get; init; }
    public int Safety { get; init; }
    public int PositiveSignals { get; init; }
    public string NextBeat { get; init; } = string.Empty;
    public bool HasActiveMicroDate { get; init; }

    public static MicroDateEligibilityResult Allowed(
        int trust,
        int safety,
        int positiveSignals,
        string nextBeat,
        bool hasActiveMicroDate)
        => new()
        {
            Eligible = true,
            ReasonCode = "OK",
            Reason = string.Empty,
            Trust = trust,
            Safety = safety,
            PositiveSignals = positiveSignals,
            NextBeat = nextBeat,
            HasActiveMicroDate = hasActiveMicroDate
        };

    public static MicroDateEligibilityResult Denied(
        string reasonCode,
        string reason,
        int trust = 0,
        int safety = 0,
        int positiveSignals = 0,
        string nextBeat = "",
        bool hasActiveMicroDate = false)
        => new()
        {
            Eligible = false,
            ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? "E_ROMANCE_MICRO_DATE_GATE" : reasonCode,
            Reason = reason ?? string.Empty,
            Trust = trust,
            Safety = safety,
            PositiveSignals = positiveSignals,
            NextBeat = nextBeat ?? string.Empty,
            HasActiveMicroDate = hasActiveMicroDate
        };
}

public sealed class LoveLanguageApplyResult
{
    public bool Applied { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string ReasonCode { get; init; } = "OK";
    public string OutcomeId { get; init; } = string.Empty;
    public int FriendshipPointsApplied { get; init; }
    public IReadOnlyDictionary<string, int> AppliedAxisDeltas { get; init; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public static LoveLanguageApplyResult Rejected(string reason, string reasonCode)
        => new()
        {
            Applied = false,
            Reason = reason,
            ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? "E_ROMANCE_REJECTED" : reasonCode
        };

    public static LoveLanguageApplyResult Success(
        string outcomeId,
        int friendshipPointsApplied,
        IReadOnlyDictionary<string, int> appliedAxisDeltas)
        => new()
        {
            Applied = true,
            Reason = string.Empty,
            ReasonCode = "OK",
            OutcomeId = outcomeId,
            FriendshipPointsApplied = friendshipPointsApplied,
            AppliedAxisDeltas = appliedAxisDeltas
        };
}

