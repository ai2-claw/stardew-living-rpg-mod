using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewLivingRPG.State;
using StardewLivingRPG.Systems;
using StardewValley;

namespace StardewLivingRPG.Utils;

public static class QuestTextHelper
{
    private static readonly string[] SupportedQuestLocales =
    {
        "es", "pt-br", "ja", "ko", "de", "ru", "fr", "zh-cn", "it", "tr"
    };

    private static readonly Regex NonAlphaNumericPattern = new("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex CamelCaseSplitPattern = new("(?<!^)([A-Z])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex TemplateTokenPattern = new(@"\{\{[^}]+\}\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex AsciiWordPattern = new(@"\b[A-Za-z]{3,}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> EnglishLeakSensitiveLocales = new(StringComparer.OrdinalIgnoreCase)
    {
        "ja", "ko", "ru", "zh-cn"
    };

    private static readonly QuestTextLocaleOverlay EnglishOverlay = new()
    {
        TitleVariants = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["gather_crop"] = new() { "Gather {{target}}", "Harvest Help: {{target}}", "Field Run: {{target}}" },
            ["deliver_item"] = new() { "Supply Drop: {{target}}", "Market Delivery: {{target}}", "Town Delivery: {{target}}" },
            ["mine_resource"] = new() { "Mine Run: {{target}}", "Prospector's Call: {{target}}", "Ore Request: {{target}}" },
            ["social_visit"] = new() { "Friendly Visit: {{target}}", "Check-In with {{target}}", "Neighborly Errand: {{target}}" },
            ["default"] = new() { "Town Request: {{target}}", "Community Task: {{target}}", "Mayor's Request: {{target}}" }
        },
        SummaryTemplates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["gather_crop"] = "{{issuer}} says the town could use {{count}} {{target}}. Bringing them by would keep local shelves steady.",
            ["deliver_item"] = "{{issuer}} says the town could use {{count}} {{target}}. Bringing them by would keep local trade moving.",
            ["mine_resource"] = "{{issuer}} needs {{count}} {{target}} from the mines to keep rough work supplied.",
            ["social_visit"] = "{{issuer}} asked someone to check in with {{target}} and make sure they're doing all right.",
            ["default"] = "{{issuer}} posted a town request for {{count}} {{target}}."
        },
        Messages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["accepted"] = "Accepted request: {{title}}",
            ["active_not_found"] = "Active quest not found: {{questId}}",
            ["visit_first"] = "Visit {{target}} first, then complete this request.",
            ["not_ready"] = "Request not ready yet: {{title}}.",
            ["need_items"] = "Need {{need}} {{item}}, but only have {{have}}.",
            ["completed"] = "Completed request: {{title}} (+{{reward}}g{{consumed}})",
            ["completed_consumed_part"] = ", consumed {{count}} {{item}}",
            ["progress_ready"] = "Ready to complete: {{title}}",
            ["progress_items"] = "Not ready yet: {{have}}/{{need}} {{item}} delivered.",
            ["progress_visit"] = "Not ready yet: visit {{target}} first.",
            ["progress_items_bar"] = "Progress: {{have}}/{{need}} {{item}}",
            ["progress_visit_bar"] = "Check-in: {{have}}/{{need}} {{target}}"
        }
    };

    private static readonly Dictionary<string, string> NpcAliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["qi"] = "MisterQi",
        ["mr_qi"] = "MisterQi",
        ["wizard"] = "Wizard"
    };

    private static readonly object SyncRoot = new();
    private static IModHelper? _helper;
    private static IMonitor? _monitor;
    private static readonly Dictionary<string, QuestTextLocaleOverlay?> LocaleOverlayCache = new(StringComparer.OrdinalIgnoreCase);
    private static IReadOnlyDictionary<string, string>? _localizedObjectNames;
    private static IReadOnlyDictionary<string, string>? _localizedNpcNames;

    public static void Initialize(IModHelper helper, IMonitor monitor)
    {
        _helper = helper;
        _monitor = monitor;
        lock (SyncRoot)
        {
            LocaleOverlayCache.Clear();
            _localizedObjectNames = null;
            _localizedNpcNames = null;
        }

        foreach (var locale in SupportedQuestLocales)
            _ = GetLocaleOverlay(locale);
    }

    public static string BuildQuestTitle(QuestEntry q)
    {
        var overlay = ResolveActiveOverlay();
        var target = GetQuestTargetDisplayName(q);
        var templateId = NormalizeTemplateId(q.TemplateId);
        var variants = overlay.TitleVariants.TryGetValue(templateId, out var templateVariants) && templateVariants.Count > 0
            ? templateVariants
            : EnglishOverlay.TitleVariants["default"];
        var index = Math.Abs((q.QuestId ?? string.Empty).GetHashCode()) % variants.Count;
        return FormatTemplate(variants[index], new Dictionary<string, string>
        {
            ["target"] = target
        });
    }

    public static string BuildQuestSummary(QuestEntry q)
    {
        return BuildQuestSummary(q.Issuer, q.TemplateId, q.TargetItem, q.TargetCount);
    }

    public static string BuildQuestSummary(string issuer, string templateId, string target, int count)
    {
        var overlay = ResolveActiveOverlay();
        var summaryTemplate = overlay.SummaryTemplates.TryGetValue(NormalizeTemplateId(templateId), out var template)
            ? template
            : EnglishOverlay.SummaryTemplates["default"];
        return FormatTemplate(summaryTemplate, new Dictionary<string, string>
        {
            ["issuer"] = GetQuestIssuerDisplayName(issuer),
            ["target"] = GetQuestTargetDisplayName(target, string.Equals(templateId, "social_visit", StringComparison.OrdinalIgnoreCase)),
            ["count"] = Math.Max(1, count).ToString()
        });
    }

    public static string BuildAcceptedMessage(string title)
    {
        return BuildMessage("accepted", new Dictionary<string, string>
        {
            ["title"] = title
        });
    }

    public static string BuildQuestNotFoundMessage(string questId)
    {
        return BuildMessage("active_not_found", new Dictionary<string, string>
        {
            ["questId"] = questId
        });
    }

    public static string BuildVisitFirstMessage(string target)
    {
        return BuildMessage("visit_first", new Dictionary<string, string>
        {
            ["target"] = target
        });
    }

    public static string BuildNotReadyMessage(string title)
    {
        return BuildMessage("not_ready", new Dictionary<string, string>
        {
            ["title"] = title
        });
    }

    public static string BuildNeedItemsMessage(int need, string item, int have)
    {
        return BuildMessage("need_items", new Dictionary<string, string>
        {
            ["need"] = Math.Max(0, need).ToString(),
            ["item"] = item,
            ["have"] = Math.Max(0, have).ToString()
        });
    }

    public static string BuildCompletedMessage(string title, int rewardGold, int consumedCount, string item)
    {
        var consumed = consumedCount > 0
            ? BuildMessage("completed_consumed_part", new Dictionary<string, string>
            {
                ["count"] = consumedCount.ToString(),
                ["item"] = item
            })
            : string.Empty;
        return BuildMessage("completed", new Dictionary<string, string>
        {
            ["title"] = title,
            ["reward"] = Math.Max(0, rewardGold).ToString(),
            ["consumed"] = consumed
        });
    }

    public static string BuildProgressReadyMessage(string title)
    {
        return BuildMessage("progress_ready", new Dictionary<string, string>
        {
            ["title"] = title
        });
    }

    public static string BuildProgressItemsMessage(int have, int need, string item)
    {
        return BuildMessage("progress_items", new Dictionary<string, string>
        {
            ["have"] = Math.Max(0, have).ToString(),
            ["need"] = Math.Max(0, need).ToString(),
            ["item"] = item
        });
    }

    public static string BuildProgressVisitMessage(string target)
    {
        return BuildMessage("progress_visit", new Dictionary<string, string>
        {
            ["target"] = target
        });
    }

    public static string BuildProgressBarText(QuestEntry quest, QuestProgressResult progress)
    {
        var messageKey = string.Equals(quest.TemplateId, "social_visit", StringComparison.OrdinalIgnoreCase)
            ? "progress_visit_bar"
            : "progress_items_bar";
        return BuildMessage(messageKey, new Dictionary<string, string>
        {
            ["have"] = Math.Max(0, progress.HaveCount).ToString(),
            ["need"] = Math.Max(0, progress.NeedCount).ToString(),
            ["target"] = GetQuestTargetDisplayName(quest),
            ["item"] = GetQuestTargetDisplayName(quest)
        });
    }

    public static string GetQuestIssuerDisplayName(string? issuer)
    {
        return ResolveNpcDisplayName(issuer);
    }

    public static string GetQuestTargetDisplayName(QuestEntry quest)
    {
        return GetQuestTargetDisplayName(quest.TargetItem, string.Equals(quest.TemplateId, "social_visit", StringComparison.OrdinalIgnoreCase));
    }

    public static string GetQuestTargetDisplayName(string? target, bool npcTarget = false)
    {
        if (npcTarget)
            return ResolveNpcDisplayName(target);

        return ResolveObjectDisplayName(target);
    }

    public static string PrettyTarget(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Supplies";

        return PrettyName(value);
    }

    public static string PrettyName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Replace("_", " ").Trim();
        var words = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant());

        return string.Join(' ', words);
    }

    private static string BuildMessage(string key, IReadOnlyDictionary<string, string> tokens)
    {
        var overlay = ResolveActiveOverlay();
        var template = overlay.Messages.TryGetValue(key, out var localized)
            ? localized
            : EnglishOverlay.Messages[key];
        return FormatTemplate(template, tokens);
    }

    private static string ResolveObjectDisplayName(string? target)
    {
        var fallback = PrettyName(target);
        if (string.IsNullOrWhiteSpace(target))
            return fallback;

        var map = GetLocalizedObjectNames();
        var normalized = NormalizeLookupKey(target);
        return map.TryGetValue(normalized, out var localized) && !string.IsNullOrWhiteSpace(localized)
            ? localized
            : fallback;
    }

    private static string ResolveNpcDisplayName(string? npcName)
    {
        var fallback = PrettyName(npcName);
        if (string.IsNullOrWhiteSpace(npcName))
            return fallback;

        try
        {
            var liveNpc = Game1.getCharacterFromName(npcName.Trim(), mustBeVillager: false) as NPC;
            if (liveNpc is not null && !string.IsNullOrWhiteSpace(liveNpc.displayName))
                return liveNpc.displayName.Trim();
        }
        catch
        {
        }

        var map = GetLocalizedNpcNames();
        var normalized = NormalizeLookupKey(npcName);
        if (map.TryGetValue(normalized, out var localized) && !string.IsNullOrWhiteSpace(localized))
            return localized;

        if (NpcAliasMap.TryGetValue(normalized, out var alias))
        {
            var aliasKey = NormalizeLookupKey(alias);
            if (map.TryGetValue(aliasKey, out localized) && !string.IsNullOrWhiteSpace(localized))
                return localized;
        }

        return fallback;
    }

    private static IReadOnlyDictionary<string, string> GetLocalizedObjectNames()
    {
        lock (SyncRoot)
        {
            if (_localizedObjectNames is not null)
                return _localizedObjectNames;

            var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var objectData = TryGetGame1ObjectData();
            if (objectData is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Value is null)
                        continue;

                    var canonicalName = TryReadStringMember(entry.Value, "Name");
                    var displayName = TryReadStringMember(entry.Value, "DisplayName", "Name");
                    var normalized = NormalizeLookupKey(canonicalName);
                    if (string.IsNullOrWhiteSpace(normalized) || string.IsNullOrWhiteSpace(displayName))
                        continue;

                    if (!resolved.ContainsKey(normalized))
                        resolved[normalized] = displayName;
                }
            }

            _localizedObjectNames = resolved;
            return _localizedObjectNames;
        }
    }

    private static IReadOnlyDictionary<string, string> GetLocalizedNpcNames()
    {
        lock (SyncRoot)
        {
            if (_localizedNpcNames is not null)
                return _localizedNpcNames;

            var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var names = Game1.content.Load<Dictionary<string, string>>("Strings\\NPCNames");
                foreach (var (key, value) in names)
                {
                    var normalized = NormalizeLookupKey(key, splitCamelCase: true);
                    if (string.IsNullOrWhiteSpace(normalized) || string.IsNullOrWhiteSpace(value))
                        continue;

                    if (!resolved.ContainsKey(normalized))
                        resolved[normalized] = value.Trim();
                }
            }
            catch (Exception ex)
            {
                _monitor?.Log($"Failed to load localized NPC names for quests: {ex.Message}", LogLevel.Trace);
            }

            _localizedNpcNames = resolved;
            return _localizedNpcNames;
        }
    }

    private static object? TryGetGame1ObjectData()
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase;
        var prop = typeof(Game1).GetProperty("objectData", flags);
        if (prop is not null)
            return prop.GetValue(null);

        var field = typeof(Game1).GetField("objectData", flags);
        return field?.GetValue(null);
    }

    private static string? TryReadStringMember(object source, params string[] memberNames)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;
        foreach (var memberName in memberNames)
        {
            var prop = source.GetType().GetProperty(memberName, flags);
            if (prop is not null)
            {
                var text = Convert.ToString(prop.GetValue(source));
                if (!string.IsNullOrWhiteSpace(text))
                    return text.Trim();
            }

            var field = source.GetType().GetField(memberName, flags);
            if (field is not null)
            {
                var text = Convert.ToString(field.GetValue(source));
                if (!string.IsNullOrWhiteSpace(text))
                    return text.Trim();
            }
        }

        return null;
    }

    private static QuestTextLocaleOverlay ResolveActiveOverlay()
    {
        foreach (var candidate in BuildLocaleFallback(I18n.GetCurrentLocaleCode()))
        {
            var overlay = GetLocaleOverlay(candidate);
            if (overlay is not null)
                return overlay;
        }

        return EnglishOverlay;
    }

    private static QuestTextLocaleOverlay? GetLocaleOverlay(string locale)
    {
        var normalized = NormalizeLocale(locale);
        lock (SyncRoot)
        {
            if (LocaleOverlayCache.TryGetValue(normalized, out var cached))
                return cached;
        }

        QuestTextLocaleOverlay? overlay = null;
        if (_helper is not null)
        {
            try
            {
                overlay = _helper.Data.ReadJsonFile<QuestTextLocaleOverlay>($"assets/quest-text.{normalized}.json");
            }
            catch (Exception ex)
            {
                _monitor?.Log($"Failed to load quest locale overlay '{normalized}': {ex.Message}", LogLevel.Warn);
            }
        }

        if (overlay is not null)
            ValidateOverlay(normalized, overlay);

        lock (SyncRoot)
        {
            LocaleOverlayCache[normalized] = overlay;
        }

        return overlay;
    }

    private static void ValidateOverlay(string locale, QuestTextLocaleOverlay overlay)
    {
        foreach (var templateKey in EnglishOverlay.TitleVariants.Keys)
        {
            if (!overlay.TitleVariants.TryGetValue(templateKey, out var variants) || variants.Count == 0)
            {
                _monitor?.Log($"Quest locale overlay '{locale}' is missing title variants for '{templateKey}'.", LogLevel.Warn);
                continue;
            }

            foreach (var variant in variants)
            {
                WarnIfCorruptedText(locale, $"TitleVariants.{templateKey}", variant);
                WarnIfLikelyEnglish(locale, $"TitleVariants.{templateKey}", variant);
            }
        }

        foreach (var summaryKey in EnglishOverlay.SummaryTemplates.Keys)
        {
            if (!overlay.SummaryTemplates.ContainsKey(summaryKey))
            {
                _monitor?.Log($"Quest locale overlay '{locale}' is missing summary template for '{summaryKey}'.", LogLevel.Warn);
                continue;
            }

            WarnIfCorruptedText(locale, $"SummaryTemplates.{summaryKey}", overlay.SummaryTemplates[summaryKey]);
            WarnIfLikelyEnglish(locale, $"SummaryTemplates.{summaryKey}", overlay.SummaryTemplates[summaryKey]);
        }

        foreach (var messageKey in EnglishOverlay.Messages.Keys)
        {
            if (!overlay.Messages.ContainsKey(messageKey))
            {
                _monitor?.Log($"Quest locale overlay '{locale}' is missing message '{messageKey}'.", LogLevel.Warn);
                continue;
            }

            WarnIfCorruptedText(locale, $"Messages.{messageKey}", overlay.Messages[messageKey]);
            WarnIfLikelyEnglish(locale, $"Messages.{messageKey}", overlay.Messages[messageKey]);
        }
    }

    private static void WarnIfCorruptedText(string locale, string fieldName, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (text.Contains("??", StringComparison.Ordinal)
            || text.Contains("Ã", StringComparison.Ordinal)
            || text.Contains("Â", StringComparison.Ordinal)
            || ContainsBrokenQuestionMark(text))
        {
            _monitor?.Log($"Quest locale overlay '{locale}' field '{fieldName}' looks corrupted: '{text}'", LogLevel.Warn);
        }
    }

    private static bool ContainsBrokenQuestionMark(string text)
    {
        for (var i = 1; i < text.Length - 1; i++)
        {
            if (text[i] != '?')
                continue;

            if (char.IsLetter(text[i - 1]) && char.IsLetter(text[i + 1]))
                return true;
        }

        return false;
    }

    private static void WarnIfLikelyEnglish(string locale, string fieldName, string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || !EnglishLeakSensitiveLocales.Contains(locale))
            return;

        var sanitized = TemplateTokenPattern.Replace(text, " ").Replace("g", " ", StringComparison.Ordinal);
        if (!AsciiWordPattern.IsMatch(sanitized))
            return;

        _monitor?.Log($"Quest locale overlay '{locale}' field '{fieldName}' still looks English: '{text}'", LogLevel.Warn);
    }

    private static IEnumerable<string> BuildLocaleFallback(string? rawLocale)
    {
        var normalized = NormalizeLocale(rawLocale);
        var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return new[] { "en" };

        if (parts.Length == 1)
            return new[] { normalized, "en" };

        return new[] { normalized, parts[0], "en" };
    }

    private static string NormalizeTemplateId(string? templateId)
    {
        var normalized = (templateId ?? string.Empty).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "default" : normalized;
    }

    private static string NormalizeLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            return "en";

        return locale.Trim().Replace('_', '-').ToLowerInvariant();
    }

    private static string NormalizeLookupKey(string? raw, bool splitCamelCase = false)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var normalized = raw.Trim();
        if (splitCamelCase)
            normalized = CamelCaseSplitPattern.Replace(normalized, "_$1");

        normalized = normalized.Replace(' ', '_').Replace('-', '_').Replace("'", string.Empty, StringComparison.Ordinal);
        normalized = NonAlphaNumericPattern.Replace(normalized.ToLowerInvariant(), "_");
        return normalized.Trim('_');
    }

    private static string FormatTemplate(string template, IReadOnlyDictionary<string, string> tokens)
    {
        var formatted = template ?? string.Empty;
        foreach (var (key, value) in tokens)
            formatted = formatted.Replace($"{{{{{key}}}}}", value ?? string.Empty, StringComparison.Ordinal);

        return formatted;
    }
}

public sealed class QuestTextLocaleOverlay
{
    public Dictionary<string, List<string>> TitleVariants { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> SummaryTemplates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Messages { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
