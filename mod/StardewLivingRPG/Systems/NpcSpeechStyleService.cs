using StardewLivingRPG.Config;

namespace StardewLivingRPG.Systems;

public sealed class NpcSpeechStyleService
{
    private readonly NpcVerbalProfile _defaultProfile;
    private readonly int _highCharismaThreshold;
    private readonly int _highSocialThreshold;
    private readonly Dictionary<string, NpcVerbalProfile> _profileByNpc;

    public NpcSpeechStyleService(NpcSpeechStyleConfig? config = null)
    {
        var resolved = config ?? NpcSpeechStyleConfig.CreateDefault();
        _defaultProfile = ParseProfile(resolved.DefaultProfile, NpcVerbalProfile.Traditionalist);
        _highCharismaThreshold = Math.Max(0, resolved.HighCharismaThreshold);
        _highSocialThreshold = Math.Max(0, resolved.HighSocialThreshold);
        _profileByNpc = new Dictionary<string, NpcVerbalProfile>(StringComparer.OrdinalIgnoreCase);

        if (resolved.NpcProfiles is null)
            return;

        foreach (var (npcName, profileName) in resolved.NpcProfiles)
        {
            if (string.IsNullOrWhiteSpace(npcName))
                continue;

            _profileByNpc[npcName] = ParseProfile(profileName, _defaultProfile);
        }
    }

    public string BuildPromptBlock(string? npcName, int heartLevel, bool isRaining, int charismaStat, int socialStat)
    {
        var profile = ResolveProfile(npcName);
        var warmthBand = heartLevel switch
        {
            <= 2 => "default",
            >= 6 => "softened",
            _ => "steady"
        };

        var dampnessModifier = isRaining && (profile is NpcVerbalProfile.Professional or NpcVerbalProfile.Traditionalist);
        var highReputation = charismaStat >= _highCharismaThreshold || socialStat >= _highSocialThreshold;
        var honorific = !highReputation
            ? "Farmer"
            : heartLevel >= 6
                ? "Neighbor"
                : heartLevel <= 2
                    ? "Stranger"
                    : "Farmer";

        var contractionRule = profile switch
        {
            NpcVerbalProfile.Intellectual => "Do not use contractions.",
            NpcVerbalProfile.Traditionalist => "Use contractions naturally.",
            _ => "Use natural contractions unless tone requires otherwise."
        };

        var stylePrimer = profile switch
        {
            NpcVerbalProfile.Professional => "Syntax: efficient and cost-aware; Vocabulary: inventory, turnaround, quality; Punctuation: use semicolons for compact lists.",
            NpcVerbalProfile.Traditionalist => "Syntax: slower communal pace; Vocabulary: folks, season, the old [thing]; Punctuation: occasional question tags like '...eh?' or '...right?'.",
            NpcVerbalProfile.Intellectual => "Syntax: precise and analytical; Vocabulary: hypothetical, profound, observation; Punctuation: disciplined comma usage.",
            NpcVerbalProfile.Enthusiast => "Syntax: energetic and fast; Vocabulary: did you see, incredible, look; Punctuation: enthusiastic exclamation marks.",
            NpcVerbalProfile.Recluse => "Syntax: clipped and guarded; Vocabulary: whatever, fine, busy; Punctuation: use ellipses for pauses and distance.",
            _ => "Syntax: grounded village speech."
        };

        var warmthModifier = warmthBand switch
        {
            "default" => "Keep the profile at full intensity for 0-2 hearts.",
            "softened" => "Soften rough edges for 6+ hearts while preserving identity cues.",
            _ => "Maintain balanced profile intensity for 3-5 hearts."
        };

        var rainModifier = dampnessModifier
            ? "RAIN_MODIFIER: Mention rain-linked practical discomforts (e.g., damp joints, leaks, muddy chores) in-character when relevant."
            : string.Empty;

        var reputationModifier = highReputation
            ? $"REPUTATION_MODIFIER: Address player with honorific '{honorific}' at least once when natural."
            : string.Empty;

        var relationshipModifier = heartLevel switch
        {
            <= 2 => "RELATIONSHIP_RULE: Keep tone reserved and polite. Do not use affectionate, flirty, or over-familiar language. Avoid pet names/endearments.",
            >= 6 => "RELATIONSHIP_RULE: Warmth is allowed, but stay natural and in-character for this NPC.",
            _ => "RELATIONSHIP_RULE: Keep tone neutral-friendly without intimate language."
        };

        return string.Join(" ",
            $"SPEECH_PROFILE: {profile}.",
            $"HEART_CONTEXT: {heartLevel} hearts ({warmthBand}).",
            $"RPG_STATS: Charisma={charismaStat}, Social={socialStat}.",
            $"STYLE_RULES: {stylePrimer}",
            $"WARMTH_RULE: {warmthModifier}",
            $"CONTRACTION_RULE: {contractionRule}",
            relationshipModifier,
            rainModifier,
            reputationModifier
        ).Trim();
    }

    public NpcVerbalProfile GetProfile(string? npcName)
    {
        return ResolveProfile(npcName);
    }

    private NpcVerbalProfile ResolveProfile(string? npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return _defaultProfile;

        return _profileByNpc.TryGetValue(npcName, out var profile)
            ? profile
            : _defaultProfile;
    }

    private static NpcVerbalProfile ParseProfile(string? value, NpcVerbalProfile fallback)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && Enum.TryParse<NpcVerbalProfile>(value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }
}
