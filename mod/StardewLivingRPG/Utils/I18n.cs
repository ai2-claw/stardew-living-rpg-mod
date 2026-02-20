using System;
using System.Reflection;
using StardewModdingAPI;

namespace StardewLivingRPG.Utils;

public static class I18n
{
    private static ITranslationHelper? _translations;

    public static void Initialize(ITranslationHelper translations)
    {
        _translations = translations;
    }

    public static string Get(string key, string fallback)
    {
        return Get(key, fallback, tokens: null);
    }

    public static string Get(string key, string fallback, object? tokens)
    {
        if (_translations is null)
            return fallback;

        var text = tokens is null
            ? _translations.Get(key).ToString()
            : _translations.Get(key, tokens).ToString();

        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        if (text.Equals($"{{{{{key}}}}}", StringComparison.Ordinal))
            return fallback;

        return text;
    }

    public static string GetCurrentLocaleCode()
    {
        if (_translations is null)
            return "en";

        try
        {
            var localeProp = _translations.GetType().GetProperty("Locale", BindingFlags.Public | BindingFlags.Instance);
            var locale = localeProp?.GetValue(_translations)?.ToString();
            if (string.IsNullOrWhiteSpace(locale))
                return "en";

            return locale.Trim();
        }
        catch
        {
            return "en";
        }
    }

    public static string BuildPromptLanguageInstruction()
    {
        var language = ResolveLanguageNameFromLocale(GetCurrentLocaleCode());

        return $"LANGUAGE_RULE: Write player-facing text in {language}. Keep command names, JSON keys, and schema field names in English.";
    }

    public static string ResolveLanguageNameFromLocale(string? localeCode)
    {
        var normalized = NormalizeLocale(string.IsNullOrWhiteSpace(localeCode) ? GetCurrentLocaleCode() : localeCode!);
        var language = ResolveLanguageName(normalized);

        if (normalized == "en" || normalized.StartsWith("en-", StringComparison.Ordinal))
            language = "English";

        return language;
    }

    private static string NormalizeLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            return "en";

        return locale.Trim().Replace('_', '-').ToLowerInvariant();
    }

    private static string ResolveLanguageName(string normalizedLocale)
    {
        if (normalizedLocale.StartsWith("es", StringComparison.Ordinal))
            return "Spanish";
        if (normalizedLocale.StartsWith("fr", StringComparison.Ordinal))
            return "French";
        if (normalizedLocale.StartsWith("de", StringComparison.Ordinal))
            return "German";
        if (normalizedLocale.StartsWith("it", StringComparison.Ordinal))
            return "Italian";
        if (normalizedLocale.StartsWith("pt", StringComparison.Ordinal))
            return "Portuguese";
        if (normalizedLocale.StartsWith("ru", StringComparison.Ordinal))
            return "Russian";
        if (normalizedLocale.StartsWith("ja", StringComparison.Ordinal))
            return "Japanese";
        if (normalizedLocale.StartsWith("ko", StringComparison.Ordinal))
            return "Korean";
        if (normalizedLocale.StartsWith("zh", StringComparison.Ordinal))
            return "Chinese";

        return $"the active locale language ({normalizedLocale})";
    }
}
