namespace StardewLivingRPG.Systems;

public sealed class NpcConversationService
{
    public const int MinTurnDepth = 2;
    public const int MaxTurnDepth = 4;
    public const int DefaultDailyLimit = 3;
    public const int DefaultPairCooldownDays = 2;

    public int ResolveTurnDepth(string? mode, int configuredDepth)
    {
        if (configuredDepth > 0)
            return Math.Clamp(configuredDepth, MinTurnDepth, MaxTurnDepth);

        var normalizedMode = (mode ?? string.Empty).Trim().ToLowerInvariant();
        return normalizedMode switch
        {
            "story_depth" => 3,
            "living_chaos" => 4,
            _ => 2
        };
    }

    public int ResolveDailyLimit(int configuredLimit)
    {
        return configuredLimit < 0
            ? DefaultDailyLimit
            : Math.Max(0, configuredLimit);
    }

    public int ResolvePairCooldownDays(int configuredCooldownDays)
    {
        return configuredCooldownDays < 0
            ? DefaultPairCooldownDays
            : Math.Max(0, configuredCooldownDays);
    }

    public string BuildPairKey(string npcIdA, string npcIdB)
    {
        var left = (npcIdA ?? string.Empty).Trim().ToLowerInvariant();
        var right = (npcIdB ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return string.Empty;

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{left}|{right}"
            : $"{right}|{left}";
    }

    public bool IsPairCoolingDown(
        IReadOnlyDictionary<string, int> lastConversationDayByPair,
        string pairKey,
        int currentDay,
        int cooldownDays)
    {
        if (cooldownDays <= 0 || string.IsNullOrWhiteSpace(pairKey))
            return false;

        if (!lastConversationDayByPair.TryGetValue(pairKey, out var lastDay))
            return false;

        return currentDay - lastDay < cooldownDays;
    }

    public static string NormalizeOverhearSnippet(string message, int maxChars = 110)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        var normalized = string.Join(" ",
            message
                .Trim()
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (normalized.Length <= maxChars)
            return normalized;

        return normalized[..Math.Max(1, maxChars - 3)] + "...";
    }
}
