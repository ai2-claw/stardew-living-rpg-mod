using System.Text.RegularExpressions;

namespace StardewLivingRPG.CustomNpcFramework.Utilities;

internal static class TextTokenUtility
{
    public static string NormalizeToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var token = raw
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace("'", string.Empty, StringComparison.Ordinal);

        token = Regex.Replace(token, @"[^a-z0-9_]+", string.Empty, RegexOptions.CultureInvariant);
        if (token.EndsWith("ies", StringComparison.Ordinal) && token.Length > 3)
            token = token[..^3] + "y";
        else if (token.EndsWith("s", StringComparison.Ordinal) && token.Length > 3)
            token = token[..^1];
        return token;
    }

    public static bool ContainsToken(string text, string target)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = NormalizeToken(target).Replace("_", " ", StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        var pattern = normalized.EndsWith("y", StringComparison.Ordinal) && normalized.Length > 1
            ? $@"\b(?:{Regex.Escape(normalized)}|{Regex.Escape(normalized[..^1] + "ies")})\b"
            : $@"\b{Regex.Escape(normalized)}s?\b";
        return Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    public static string CompactWhitespace(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return Regex.Replace(text.Trim(), @"\s+", " ", RegexOptions.CultureInvariant);
    }

    public static string TrimForPrompt(string? value, int maxLength)
    {
        var compact = CompactWhitespace(value);
        if (compact.Length <= maxLength)
            return compact;

        return compact[..maxLength].TrimEnd();
    }
}

