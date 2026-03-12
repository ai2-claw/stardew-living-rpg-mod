using StardewModdingAPI;
using StardewLivingRPG.State;
using StardewLivingRPG.Utils;

namespace StardewLivingRPG.Systems;

public sealed class TownSquareMagicianFlavorService
{
    private const int MaxGeneratedAsideCharacters = TownSquareMagicianService.MaxBubbleTextLength;
    private const int MaxStandaloneBubbleCharacters = TownSquareMagicianService.MaxBubbleTextLength;
    private const int MaxGeneratedAsideWords = 8;
    private const int SessionCacheCapacity = 20;
    private static readonly TimeSpan RejectionCooldown = TimeSpan.FromSeconds(90);
    private static readonly string[] PoeticTokens =
    {
        "destiny", "fate", "echo", "echoes", "veil", "whisper", "whispers",
        "starlight", "moonlit", "prophecy", "omen", "omens", "cosmic", "ethereal"
    };
    private static readonly HashSet<string> OpeningStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "between", "for", "from", "guess", "hidden", "is", "it",
        "little", "look", "name", "number", "of", "one", "or", "pick", "prompt",
        "question", "riddle", "say", "the", "think", "this", "to", "today", "try", "word"
    };
    private static readonly Dictionary<string, string> NumberWordMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["zero"] = "0",
        ["one"] = "1",
        ["two"] = "2",
        ["three"] = "3",
        ["four"] = "4",
        ["five"] = "5",
        ["six"] = "6",
        ["seven"] = "7",
        ["eight"] = "8",
        ["nine"] = "9",
        ["ten"] = "10",
        ["eleven"] = "11",
        ["twelve"] = "12",
        ["thirteen"] = "13",
        ["fourteen"] = "14",
        ["fifteen"] = "15",
        ["sixteen"] = "16",
        ["seventeen"] = "17",
        ["eighteen"] = "18",
        ["nineteen"] = "19",
        ["twenty"] = "20"
    };

    private readonly IMonitor _monitor;
    private readonly object _gate = new();
    private readonly Dictionary<string, string> _dailyIntroCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _sessionCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly LinkedList<string> _sessionOrder = new();
    private readonly Dictionary<string, LinkedListNode<string>> _sessionOrderNodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _inflightKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _rejectionCooldownUntilUtc = new(StringComparer.OrdinalIgnoreCase);
    private int _activeDay = -1;

    public TownSquareMagicianFlavorService(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public void ResetForDay(int day)
    {
        day = Math.Max(1, day);
        lock (_gate)
        {
            if (_activeDay == day)
                return;

            _activeDay = day;
            _dailyIntroCache.Clear();
            _sessionCache.Clear();
            _sessionOrder.Clear();
            _sessionOrderNodes.Clear();
            _inflightKeys.Clear();
            _rejectionCooldownUntilUtc.Clear();
        }
    }

    public TownSquareMagicianFlavorRequest BuildOpeningRequest(SaveState state, TownSquareMagicianRoundView round)
    {
        var progress = state.MiniGames.TownSquareMagician;
        var isBonusRound = IsBonusRound(progress, round.RoundId);
        return BuildRequest(
            state,
            progress,
            intent: isBonusRound ? "practice_intro" : "daily_intro",
            coreText: round.Prompt,
            roundId: round.RoundId,
            roundMode: round.Mode,
            rewardClaimedToday: round.RewardClaimedToday,
            isBonusRound: isBonusRound,
            sessionEnded: false,
            solved: false);
    }

    public TownSquareMagicianFlavorRequest BuildFeedbackRequest(SaveState state, TownSquareMagicianGuessResult result, string submittedText)
    {
        var progress = state.MiniGames.TownSquareMagician;
        var currentRound = result.CurrentRound;
        var trimmedInput = (submittedText ?? string.Empty).Trim();
        var intent = ResolveFeedbackIntent(result, trimmedInput);
        var roundId = currentRound?.RoundId ?? progress.RoundId;
        var roundMode = currentRound?.Mode ?? progress.RoundMode;
        var rewardClaimedToday = currentRound?.RewardClaimedToday ?? progress.RewardClaimedToday;
        return BuildRequest(
            state,
            progress,
            intent,
            result.Feedback,
            roundId,
            roundMode,
            rewardClaimedToday,
            IsBonusRound(progress, roundId),
            result.SessionEnded,
            result.Solved);
    }

    public TownSquareMagicianFlavorRequest BuildIntentRequest(SaveState state, TownSquareMagicianRoundView round, string intent, string coreText)
    {
        var progress = state.MiniGames.TownSquareMagician;
        return BuildRequest(
            state,
            progress,
            intent,
            coreText,
            round.RoundId,
            round.Mode,
            round.RewardClaimedToday,
            IsBonusRound(progress, round.RoundId),
            sessionEnded: false,
            solved: false);
    }

    public string BuildFeedbackFlavorText(TownSquareMagicianFlavorRequest request)
    {
        if (!ShouldShowFeedbackFlavor(request))
            return string.Empty;

        if (TryGetCachedAside(request, out var cachedAside))
        {
            if (TryNormalizeStandaloneBubbleText(cachedAside, out var flavorText))
                return flavorText;

            Invalidate(request, "cached-feedback-flavor-overflow");
        }

        return string.Empty;
    }

    public string BuildOpeningIntroText(TownSquareMagicianFlavorRequest request)
    {
        var promptText = NormalizeCoreText(request.CoreText);
        if (string.IsNullOrWhiteSpace(promptText))
            return string.Empty;

        if (TryGetCachedAside(request, out var cachedAside))
        {
            if (TryNormalizeStandaloneBubbleText(cachedAside, out var introText)
                && !IsOpeningIntroRedundant(introText, promptText))
            {
                return introText;
            }

            Invalidate(request, "opening-intro-invalid");
        }

        var localAside = BuildLocalAside(request);
        if (!TryNormalizeStandaloneBubbleText(localAside, out var localIntro))
            return string.Empty;
        return IsOpeningIntroRedundant(localIntro, promptText) ? string.Empty : localIntro;
    }

    public bool TryReservePrefetch(TownSquareMagicianFlavorRequest request)
    {
        var key = BuildCacheKey(request);
        var now = DateTime.UtcNow;
        lock (_gate)
        {
            ClearExpiredCooldowns(now);
            if (HasCachedAsideInternal(request, key))
                return false;
            if (_inflightKeys.Contains(key))
                return false;
            if (_rejectionCooldownUntilUtc.TryGetValue(key, out var blockedUntil) && blockedUntil > now)
                return false;

            _inflightKeys.Add(key);
            return true;
        }
    }

    public void CompletePrefetch(TownSquareMagicianFlavorRequest request, string? rawAside)
    {
        var key = BuildCacheKey(request);
        var aside = ValidateGeneratedAside(rawAside);
        lock (_gate)
        {
            _inflightKeys.Remove(key);

            if (string.IsNullOrWhiteSpace(aside))
            {
                _rejectionCooldownUntilUtc[key] = DateTime.UtcNow.Add(RejectionCooldown);
                RemoveCachedAsideInternal(request, key);
                return;
            }

            if (!TryNormalizeStandaloneBubbleText(aside, out var normalizedAside))
            {
                _rejectionCooldownUntilUtc[key] = DateTime.UtcNow.Add(RejectionCooldown);
                RemoveCachedAsideInternal(request, key);
                return;
            }

            StoreCachedAsideInternal(request, key, normalizedAside);
            _rejectionCooldownUntilUtc.Remove(key);
        }
    }

    public void Invalidate(TownSquareMagicianFlavorRequest request, string reason)
    {
        var key = BuildCacheKey(request);
        lock (_gate)
        {
            RemoveCachedAsideInternal(request, key);
            _rejectionCooldownUntilUtc[key] = DateTime.UtcNow.Add(RejectionCooldown);
        }

        _monitor.Log($"Magician flavor cache invalidated ({reason}) for intent '{request.Intent}'.", LogLevel.Trace);
    }

    public void LogFlavorFallback(string reason, TownSquareMagicianFlavorRequest request)
    {
        _monitor.Log($"Magician flavor fallback ({reason}) for intent '{request.Intent}'.", LogLevel.Trace);
    }

    private TownSquareMagicianFlavorRequest BuildRequest(
        SaveState state,
        TownSquareMagicianState progress,
        string intent,
        string coreText,
        string roundId,
        string roundMode,
        bool rewardClaimedToday,
        bool isBonusRound,
        bool sessionEnded,
        bool solved)
    {
        return new TownSquareMagicianFlavorRequest
        {
            Day = Math.Max(1, state.Calendar.Day),
            Intent = intent,
            CoreText = NormalizeCoreText(coreText),
            RoundId = roundId ?? string.Empty,
            RoundMode = roundMode ?? string.Empty,
            RewardClaimedToday = rewardClaimedToday,
            IsBonusRound = isBonusRound,
            SessionEnded = sessionEnded,
            Solved = solved,
            ArcStageId = progress.ArcStageId,
            PlayStyleTag = progress.LastPlayStyleTag,
            TheatricalMode = ResolveTheatricalMode(progress, isBonusRound),
            AttemptsUsed = progress.AttemptsUsed,
            HintsUsed = progress.HintsUsed,
            ConsecutiveWins = progress.ConsecutiveWins,
            ConsecutiveLosses = progress.ConsecutiveLosses,
            LatestHeadline = GetLatestHeadline(state),
            RecentTownEventSummary = GetRecentTownEventSummary(state)
        };
    }

    private bool TryGetCachedAside(TownSquareMagicianFlavorRequest request, out string aside)
    {
        aside = string.Empty;
        var key = BuildCacheKey(request);
        lock (_gate)
        {
            if (IsDailyIntroRequest(request))
            {
                return _dailyIntroCache.TryGetValue(key, out aside!)
                    && !string.IsNullOrWhiteSpace(aside);
            }

            if (!_sessionCache.TryGetValue(key, out aside!))
                return false;

            TouchSessionKeyInternal(key);
            return !string.IsNullOrWhiteSpace(aside);
        }
    }

    private bool HasCachedAsideInternal(TownSquareMagicianFlavorRequest request, string key)
    {
        return IsDailyIntroRequest(request)
            ? _dailyIntroCache.ContainsKey(key)
            : _sessionCache.ContainsKey(key);
    }

    private void StoreCachedAsideInternal(TownSquareMagicianFlavorRequest request, string key, string aside)
    {
        if (IsDailyIntroRequest(request))
        {
            _dailyIntroCache[key] = aside;
            return;
        }

        _sessionCache[key] = aside;
        TouchSessionKeyInternal(key);
        while (_sessionCache.Count > SessionCacheCapacity && _sessionOrder.First is not null)
        {
            var oldestKey = _sessionOrder.First.Value;
            _sessionOrder.RemoveFirst();
            _sessionOrderNodes.Remove(oldestKey);
            _sessionCache.Remove(oldestKey);
        }
    }

    private void RemoveCachedAsideInternal(TownSquareMagicianFlavorRequest request, string key)
    {
        if (IsDailyIntroRequest(request))
        {
            _dailyIntroCache.Remove(key);
            return;
        }

        _sessionCache.Remove(key);
        if (_sessionOrderNodes.TryGetValue(key, out var node))
        {
            _sessionOrder.Remove(node);
            _sessionOrderNodes.Remove(key);
        }
    }

    private void TouchSessionKeyInternal(string key)
    {
        if (_sessionOrderNodes.TryGetValue(key, out var existingNode))
        {
            _sessionOrder.Remove(existingNode);
            _sessionOrder.AddLast(existingNode);
            return;
        }

        var node = _sessionOrder.AddLast(key);
        _sessionOrderNodes[key] = node;
    }

    private static void ClearExpiredCooldowns(Dictionary<string, DateTime> cooldowns, DateTime now)
    {
        var expiredKeys = cooldowns
            .Where(pair => pair.Value <= now)
            .Select(pair => pair.Key)
            .ToList();
        foreach (var key in expiredKeys)
            cooldowns.Remove(key);
    }

    private void ClearExpiredCooldowns(DateTime now)
    {
        ClearExpiredCooldowns(_rejectionCooldownUntilUtc, now);
    }

    private static string ResolveFeedbackIntent(TownSquareMagicianGuessResult result, string submittedText)
    {
        if (string.Equals(submittedText, "hint", StringComparison.OrdinalIgnoreCase))
            return "hint_dressing";
        if (result.Solved)
            return result.RewardGranted ? "victory_sting" : "practice_flavor";
        if (result.SessionEnded)
            return "consolation_sting";
        return "banter_layer";
    }

    private static bool IsBonusRound(TownSquareMagicianState progress, string? roundId)
    {
        return progress.RewardClaimedToday
            && !string.IsNullOrWhiteSpace(progress.FeaturedRoundId)
            && !string.IsNullOrWhiteSpace(roundId)
            && !string.Equals(progress.FeaturedRoundId, roundId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDailyIntroRequest(TownSquareMagicianFlavorRequest request)
    {
        return string.Equals(request.Intent, "daily_intro", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldShowFeedbackFlavor(TownSquareMagicianFlavorRequest request)
    {
        return request.Intent switch
        {
            "victory_sting" => true,
            "practice_flavor" => true,
            "consolation_sting" => true,
            _ => false
        };
    }

    private static string ResolveTheatricalMode(TownSquareMagicianState progress, bool isBonusRound)
    {
        if (isBonusRound)
            return "practice";
        if (progress.ConsecutiveWins >= 2)
            return "confident";
        if (progress.ConsecutiveLosses >= 2)
            return "needling";
        if (string.Equals(progress.LastPlayStyleTag, "careful", StringComparison.OrdinalIgnoreCase))
            return "measured";
        return "street";
    }

    private static string? GetLatestHeadline(SaveState state)
    {
        return state.Newspaper.Issues
            .OrderByDescending(issue => issue.Day)
            .Select(issue => (issue.Headline ?? string.Empty).Trim())
            .FirstOrDefault(headline => !string.IsNullOrWhiteSpace(headline));
    }

    private static string? GetRecentTownEventSummary(SaveState state)
    {
        var currentDay = Math.Max(1, state.Calendar.Day);
        return state.TownMemory.Events
            .Where(ev => ev is not null && currentDay - ev.Day <= 3)
            .OrderByDescending(ev => ev.Severity)
            .ThenByDescending(ev => ev.Day)
            .Select(ev => (ev.Summary ?? string.Empty).Trim())
            .FirstOrDefault(summary => !string.IsNullOrWhiteSpace(summary));
    }

    private static string NormalizeCoreText(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        var value = rawText
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
        while (value.Contains("  ", StringComparison.Ordinal))
            value = value.Replace("  ", " ", StringComparison.Ordinal);
        return value;
    }

    private static string? ValidateGeneratedAside(string? rawAside)
    {
        if (string.IsNullOrWhiteSpace(rawAside))
            return null;

        var value = rawAside
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim()
            .Trim('"')
            .Trim('\'')
            .Trim();
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (value.StartsWith("{", StringComparison.Ordinal) || value.StartsWith("[", StringComparison.Ordinal))
            return null;
        if (value.Contains("sender_message", StringComparison.OrdinalIgnoreCase))
            return null;
        if (value.StartsWith("<", StringComparison.Ordinal))
        {
            var closing = value.IndexOf('>');
            if (closing > 0 && closing + 1 < value.Length)
                value = value[(closing + 1)..].Trim();
        }

        if (value.StartsWith("aside:", StringComparison.OrdinalIgnoreCase))
            value = value["aside:".Length..].Trim();

        while (value.Contains("  ", StringComparison.Ordinal))
            value = value.Replace("  ", " ", StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxGeneratedAsideCharacters)
            return null;
        if (!value.Any(char.IsLetter))
            return null;
        if (value.Count(ch => ch is '.' or '!' or '?') > 1)
            return null;
        if (value.Contains(':', StringComparison.Ordinal) || value.Contains(';', StringComparison.Ordinal))
            return null;

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0 || words.Length > MaxGeneratedAsideWords)
            return null;

        if (words.Any(word => PoeticTokens.Contains(word.Trim('.', '!', '?', ',', '\'').ToLowerInvariant(), StringComparer.Ordinal)))
            return null;

        return value.TrimEnd('.', '!', '?').Trim();
    }

    private static bool TryNormalizeStandaloneBubbleText(string? text, out string normalizedText)
    {
        normalizedText = NormalizeCoreText(text);
        if (string.IsNullOrWhiteSpace(normalizedText))
            return false;
        if (normalizedText.Length > MaxStandaloneBubbleCharacters)
            return false;
        return true;
    }

    private static bool IsOpeningIntroRedundant(string? introText, string promptText)
    {
        var introTokens = TokenizeForOpeningComparison(introText);
        var promptTokens = TokenizeForOpeningComparison(promptText);
        if (introTokens.Count == 0 || promptTokens.Count == 0)
            return false;

        if (introTokens.SetEquals(promptTokens))
            return true;

        return introTokens.IsSubsetOf(promptTokens) || promptTokens.IsSubsetOf(introTokens);
    }

    private static HashSet<string> TokenizeForOpeningComparison(string? rawText)
    {
        var normalized = NormalizeCoreText(rawText).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var chars = normalized
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
            .ToArray();
        var compact = new string(chars);
        while (compact.Contains("  ", StringComparison.Ordinal))
            compact = compact.Replace("  ", " ", StringComparison.Ordinal);

        var tokens = compact
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => NumberWordMap.TryGetValue(token, out var mapped) ? mapped : token)
            .Where(token => !OpeningStopWords.Contains(token))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return tokens;
    }

    private static string BuildLocalAside(TownSquareMagicianFlavorRequest request)
    {
        return request.Intent switch
        {
            "daily_intro" => BuildOpeningAside(request),
            "practice_intro" => I18n.Get("magician.flavor.practice_intro", "No prize now. Just practice."),
            "hint_dressing" => BuildHintAside(request),
            "victory_sting" => BuildVictoryAside(request),
            "practice_flavor" => I18n.Get("magician.flavor.practice_win", "Still a clean guess."),
            "consolation_sting" => BuildLossAside(request),
            _ => BuildBanterAside(request)
        };
    }

    private static string BuildOpeningAside(TownSquareMagicianFlavorRequest request)
    {
        return request.ArcStageId switch
        {
            "town_mirror" => I18n.Get("magician.flavor.open.town_mirror", "You know my act by now."),
            "old_roads" => I18n.Get("magician.flavor.open.old_roads", "I have a fresh one today."),
            "house_of_cards" => I18n.Get("magician.flavor.open.house_of_cards", "Keep your eyes on me."),
            "keen_eye" => I18n.Get("magician.flavor.open.keen_eye", "You are getting sharper."),
            _ => I18n.Get("magician.flavor.open.street_smoke", "Try your luck.")
        };
    }

    private static string BuildHintAside(TownSquareMagicianFlavorRequest request)
    {
        if (string.Equals(request.PlayStyleTag, "careful", StringComparison.OrdinalIgnoreCase))
            return I18n.Get("magician.flavor.hint.careful", "Good. Take this one slow.");
        return I18n.Get("magician.flavor.hint.default", "Here is your hint.");
    }

    private static string BuildVictoryAside(TownSquareMagicianFlavorRequest request)
    {
        if (request.ConsecutiveWins >= 2)
            return I18n.Get("magician.flavor.win.streak", "You are on a run.");
        if (string.Equals(request.PlayStyleTag, "sharp", StringComparison.OrdinalIgnoreCase))
            return I18n.Get("magician.flavor.win.sharp", "Quick work.");
        return I18n.Get("magician.flavor.win.default", "That is it.");
    }

    private static string BuildLossAside(TownSquareMagicianFlavorRequest request)
    {
        if (request.ConsecutiveLosses >= 2)
            return I18n.Get("magician.flavor.loss.streak", "Not your day yet.");
        return I18n.Get("magician.flavor.loss.default", "Missed this one.");
    }

    private static string BuildBanterAside(TownSquareMagicianFlavorRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.LatestHeadline))
            return I18n.Get("magician.flavor.banter.headline", "Town is buzzing.");
        if (!string.IsNullOrWhiteSpace(request.RecentTownEventSummary))
            return I18n.Get("magician.flavor.banter.rumor", "Plenty of gossip around.");
        if (string.Equals(request.PlayStyleTag, "dogged", StringComparison.OrdinalIgnoreCase))
            return I18n.Get("magician.flavor.banter.dogged", "You keep coming at it.");
        return I18n.Get("magician.flavor.banter.default", "Give it another shot.");
    }

    private static string BuildCacheKey(TownSquareMagicianFlavorRequest request)
    {
        if (IsDailyIntroRequest(request))
            return $"intro:{request.Day}:{request.RoundId}";

        var rewardBucket = request.IsBonusRound || request.RewardClaimedToday ? "practice" : "reward";
        var styleBucket = NormalizeBucket(request.PlayStyleTag, "steady");
        var arcBucket = NormalizeBucket(request.ArcStageId, "street_smoke");
        return $"session:{request.RoundId}:{request.Intent}:{rewardBucket}:{styleBucket}:{arcBucket}";
    }

    private static string NormalizeBucket(string? value, string fallback)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return fallback;
        return normalized;
    }
}

public sealed class TownSquareMagicianFlavorRequest
{
    public int Day { get; set; }
    public string Intent { get; set; } = string.Empty;
    public string CoreText { get; set; } = string.Empty;
    public string RoundId { get; set; } = string.Empty;
    public string RoundMode { get; set; } = string.Empty;
    public bool RewardClaimedToday { get; set; }
    public bool IsBonusRound { get; set; }
    public bool SessionEnded { get; set; }
    public bool Solved { get; set; }
    public string ArcStageId { get; set; } = "street_smoke";
    public string PlayStyleTag { get; set; } = "steady";
    public string TheatricalMode { get; set; } = "street";
    public int AttemptsUsed { get; set; }
    public int HintsUsed { get; set; }
    public int ConsecutiveWins { get; set; }
    public int ConsecutiveLosses { get; set; }
    public string? LatestHeadline { get; set; }
    public string? RecentTownEventSummary { get; set; }
}
