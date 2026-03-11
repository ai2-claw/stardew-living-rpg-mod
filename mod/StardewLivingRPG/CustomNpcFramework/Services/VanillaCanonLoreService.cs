using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewLivingRPG.CustomNpcFramework.Models;
using StardewLivingRPG.CustomNpcFramework.Utilities;

namespace StardewLivingRPG.CustomNpcFramework.Services;

internal sealed class VanillaCanonLoreService
{
    private const string SourcePath = "assets/vanilla-canon-lore.json";
    private const string SourceId = "vanilla-canon";

    private static readonly Regex[] StyleWarningPatterns =
    {
        new(@"as an ai", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"language model", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"out of character", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"game mechanic", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"assistant", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)
    };

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly CanonBaselineService _canonBaseline;
    private readonly Func<string> _localeResolver;
    private readonly Dictionary<string, VanillaLoreRecord> _recordsByToken = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aliasToToken = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _locationLoreByToken = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ValidationIssue> _validationIssues = new();

    public IReadOnlyList<ValidationIssue> ValidationIssues => _validationIssues;
    public int RecordCount => _recordsByToken.Count;

    public VanillaCanonLoreService(
        IModHelper helper,
        IMonitor monitor,
        CanonBaselineService canonBaseline,
        Func<string> localeResolver)
    {
        _helper = helper;
        _monitor = monitor;
        _canonBaseline = canonBaseline;
        _localeResolver = localeResolver;
    }

    public void Reload(bool strictCanonValidation)
    {
        _recordsByToken.Clear();
        _aliasToToken.Clear();
        _locationLoreByToken.Clear();
        _validationIssues.Clear();

        var baseLore = _helper.Data.ReadJsonFile<NpcLoreFile>(SourcePath);
        if (baseLore is null)
        {
            _validationIssues.Add(Error(
                npcToken: string.Empty,
                code: "E_VANILLA_LORE_FILE_MISSING",
                message: $"Missing or unreadable {SourcePath}."));
            _monitor.Log($"Could not load {SourcePath}; vanilla canon prompt injection will be limited.", LogLevel.Warn);
            return;
        }

        var locale = ResolveLocale();
        var merged = MergeLocaleOverlay(baseLore, locale);
        BuildRecords(merged, strictCanonValidation);

        _monitor.Log(
            $"Vanilla canon lore loaded: npcs={_recordsByToken.Count}, locations={_locationLoreByToken.Count}, issues={_validationIssues.Count}.",
            LogLevel.Info);
    }

    public string BuildLorePromptBlock(string? npcName, string? locationName, string contextTag)
    {
        var hasNpc = TryResolveRecord(npcName, out var record);
        var locationLore = GetLocationLore(locationName);
        if (!hasNpc && string.IsNullOrWhiteSpace(locationLore))
            return string.Empty;

        var parts = new List<string>
        {
            "VANILLA_CANON_RULE: Follow VANILLA_NPC_LORE and VANILLA_LOCATION_LORE exactly when provided.",
            $"VANILLA_CANON_CONTEXT: {TextTokenUtility.TrimForPrompt(contextTag, 32)}."
        };

        if (hasNpc)
        {
            var timeline = record!.Lore.TimelineAnchors.Count == 0
                ? "none"
                : string.Join(", ", record.Lore.TimelineAnchors.Take(5));
            var tiesToNpcs = record.Lore.TiesToNpcs.Count == 0
                ? "none"
                : string.Join(", ", record.Lore.TiesToNpcs.Take(6));
            var knownLocations = record.Lore.KnownLocations.Count == 0
                ? "none"
                : string.Join(", ", record.Lore.KnownLocations.Take(6));
            var forbiddenClaims = record.Lore.ForbiddenClaims.Count == 0
                ? "none"
                : string.Join(" | ", record.Lore.ForbiddenClaims.Take(4));

            parts.Add(
                $"VANILLA_NPC_LORE[{record.DisplayName}]: role={TextTokenUtility.TrimForPrompt(record.Lore.Role, 140)}; " +
                $"persona={TextTokenUtility.TrimForPrompt(record.Lore.Persona, 140)}; " +
                $"speech={TextTokenUtility.TrimForPrompt(record.Lore.Speech, 140)}; " +
                $"ties={TextTokenUtility.TrimForPrompt(record.Lore.Ties, 180)}; " +
                $"boundaries={TextTokenUtility.TrimForPrompt(record.Lore.Boundaries, 180)}; " +
                $"known_locations={TextTokenUtility.TrimForPrompt(knownLocations, 120)}; " +
                $"timeline={TextTokenUtility.TrimForPrompt(timeline, 110)}; " +
                $"ties_to_npcs={TextTokenUtility.TrimForPrompt(tiesToNpcs, 120)}; " +
                $"forbidden_claims={TextTokenUtility.TrimForPrompt(forbiddenClaims, 200)}.");
        }

        if (!string.IsNullOrWhiteSpace(locationLore))
            parts.Add($"VANILLA_LOCATION_LORE: {TextTokenUtility.TrimForPrompt(locationLore, 220)}.");

        return string.Join(" ", parts);
    }

    public string BuildReferencedNpcLorePromptBlock(string? playerText, string? speakingNpcName = null, int maxMatches = 2)
    {
        if (string.IsNullOrWhiteSpace(playerText) || _recordsByToken.Count == 0)
            return string.Empty;

        var references = FindReferencedRecords(playerText, speakingNpcName, maxMatches);
        if (references.Count == 0)
            return string.Empty;

        var parts = new List<string>
        {
            "VANILLA_CANON_REFERENCE_RULE: For referenced vanilla NPCs, do not contradict explicit family/job/canon constraints. If uncertain, answer partially without contradiction.",
            "VANILLA_RELATIONSHIP_RULE: Do not infer romance from two vanilla NPCs simply being seen together; honor family and household ties first."
        };

        foreach (var record in references)
        {
            var forbiddenClaims = record.Lore.ForbiddenClaims.Count == 0
                ? "none"
                : string.Join(" | ", record.Lore.ForbiddenClaims.Take(3));
            parts.Add(
                $"VANILLA_NPC_REFERENCE_LORE[{record.DisplayName}]: role={TextTokenUtility.TrimForPrompt(record.Lore.Role, 120)}; " +
                $"persona={TextTokenUtility.TrimForPrompt(record.Lore.Persona, 120)}; " +
                $"ties={TextTokenUtility.TrimForPrompt(record.Lore.Ties, 140)}; " +
                $"boundaries={TextTokenUtility.TrimForPrompt(record.Lore.Boundaries, 140)}; " +
                $"forbidden_claims={TextTokenUtility.TrimForPrompt(forbiddenClaims, 180)}.");
        }

        return string.Join(" ", parts);
    }

    public string BuildLoreDebugDump(string npcNameOrToken)
    {
        if (!TryResolveRecord(npcNameOrToken, out var record) || record is null)
            return $"VanillaCanonLore: no lore entry found for '{npcNameOrToken}'.";

        return
            $"VanillaCanonLore[{record.DisplayName}] token={record.Token} " +
            $"timeline={string.Join(", ", record.Lore.TimelineAnchors)} " +
            $"locations={string.Join(", ", record.Lore.KnownLocations)} " +
            $"ties_to_npcs={string.Join(", ", record.Lore.TiesToNpcs)} " +
            $"forbidden_claims={string.Join(" | ", record.Lore.ForbiddenClaims)}";
    }

    private void BuildRecords(NpcLoreFile loreFile, bool strictCanonValidation)
    {
        var allowedLocations = _canonBaseline.CanonLocationTokens;
        var allowedTimelineAnchors = _canonBaseline.TimelineAnchors;

        foreach (var (rawName, rawLore) in loreFile.Npcs)
        {
            var token = TextTokenUtility.NormalizeToken(rawName);
            if (string.IsNullOrWhiteSpace(token))
            {
                _validationIssues.Add(Error(
                    npcToken: string.Empty,
                    code: "E_VANILLA_NPC_TOKEN_INVALID",
                    message: $"Lore key '{rawName}' is invalid."));
                continue;
            }

            if (_recordsByToken.ContainsKey(token))
            {
                _validationIssues.Add(Error(
                    npcToken: token,
                    code: "E_VANILLA_NPC_TOKEN_DUPLICATE",
                    message: $"Duplicate vanilla lore token '{token}'."));
                continue;
            }

            if (strictCanonValidation && !_canonBaseline.CanonNpcTokens.Contains(token))
            {
                _validationIssues.Add(Error(
                    npcToken: token,
                    code: "E_VANILLA_NPC_UNKNOWN",
                    message: $"Vanilla lore npc '{rawName}' is not listed in canon baseline."));
                continue;
            }

            var lore = NormalizeLoreEntry(rawLore ?? new NpcLoreEntry());
            ValidateLoreEntry(token, lore, allowedLocations, allowedTimelineAnchors, strictCanonValidation);

            var hasNpcErrors = _validationIssues.Any(issue =>
                issue.Severity == ValidationSeverity.Error
                && issue.NpcId.Equals(token, StringComparison.OrdinalIgnoreCase));
            if (hasNpcErrors)
                continue;

            var displayName = string.IsNullOrWhiteSpace(rawName) ? token : rawName.Trim();
            var record = new VanillaLoreRecord(token, displayName, lore);
            _recordsByToken[token] = record;
            RegisterAlias(token, token);
            RegisterAlias(displayName, token);
        }

        foreach (var (rawToken, loreText) in loreFile.Locations)
        {
            var token = TextTokenUtility.NormalizeToken(rawToken);
            var compact = TextTokenUtility.CompactWhitespace(loreText);
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(compact))
                continue;

            if (strictCanonValidation && !_canonBaseline.CanonLocationTokens.Contains(token))
            {
                _validationIssues.Add(Error(
                    npcToken: string.Empty,
                    code: "E_VANILLA_LOCATION_UNKNOWN",
                    message: $"Location lore token '{rawToken}' is not in canon baseline."));
                continue;
            }

            _locationLoreByToken[token] = compact;
        }
    }

    private static NpcLoreEntry NormalizeLoreEntry(NpcLoreEntry raw)
    {
        var normalized = new NpcLoreEntry
        {
            Role = TextTokenUtility.CompactWhitespace(raw.Role),
            Persona = TextTokenUtility.CompactWhitespace(raw.Persona),
            Speech = TextTokenUtility.CompactWhitespace(raw.Speech),
            Ties = TextTokenUtility.CompactWhitespace(raw.Ties),
            Boundaries = TextTokenUtility.CompactWhitespace(raw.Boundaries),
            TimelineAnchors = NormalizeList(raw.TimelineAnchors),
            KnownLocations = NormalizeList(raw.KnownLocations),
            TiesToNpcs = NormalizeList(raw.TiesToNpcs),
            ForbiddenClaims = NormalizeList(raw.ForbiddenClaims)
        };

        return normalized;
    }

    private void ValidateLoreEntry(
        string npcToken,
        NpcLoreEntry lore,
        HashSet<string> allowedLocations,
        HashSet<string> allowedTimelineAnchors,
        bool strictCanonValidation)
    {
        ValidateRequiredLoreText(npcToken, "Role", lore.Role);
        ValidateRequiredLoreText(npcToken, "Persona", lore.Persona);
        ValidateRequiredLoreText(npcToken, "Speech", lore.Speech);
        ValidateRequiredLoreText(npcToken, "Ties", lore.Ties);
        ValidateRequiredLoreText(npcToken, "Boundaries", lore.Boundaries);

        ValidateStyleWarnings(npcToken, "Role", lore.Role);
        ValidateStyleWarnings(npcToken, "Persona", lore.Persona);
        ValidateStyleWarnings(npcToken, "Speech", lore.Speech);
        ValidateStyleWarnings(npcToken, "Ties", lore.Ties);
        ValidateStyleWarnings(npcToken, "Boundaries", lore.Boundaries);

        if (!strictCanonValidation)
            return;

        foreach (var location in lore.KnownLocations)
        {
            var token = TextTokenUtility.NormalizeToken(location);
            if (string.IsNullOrWhiteSpace(token))
                continue;
            if (!allowedLocations.Contains(token))
            {
                _validationIssues.Add(Error(
                    npcToken,
                    code: "E_VANILLA_NPC_LOCATION_UNKNOWN",
                    message: $"KnownLocations entry '{location}' is not in canon baseline."));
            }
        }

        foreach (var anchor in lore.TimelineAnchors)
        {
            var token = TextTokenUtility.NormalizeToken(anchor);
            if (string.IsNullOrWhiteSpace(token))
                continue;
            if (!allowedTimelineAnchors.Contains(token))
            {
                _validationIssues.Add(Error(
                    npcToken,
                    code: "E_VANILLA_TIMELINE_ANCHOR_UNKNOWN",
                    message: $"TimelineAnchors entry '{anchor}' is not in canon baseline."));
            }
        }

        foreach (var pattern in _canonBaseline.ForbiddenClaimPatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;
            if (ContainsPattern(lore.Role, pattern)
                || ContainsPattern(lore.Persona, pattern)
                || ContainsPattern(lore.Speech, pattern)
                || ContainsPattern(lore.Ties, pattern)
                || ContainsPattern(lore.Boundaries, pattern))
            {
                _validationIssues.Add(Error(
                    npcToken,
                    code: "E_VANILLA_CANON_PATTERN_MATCH",
                    message: $"Lore text matched forbidden canon pattern '{pattern}'."));
            }
        }
    }

    private void ValidateRequiredLoreText(string npcToken, string field, string value)
    {
        if (value.Length >= 12)
            return;

        _validationIssues.Add(new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Code = "E_VANILLA_LORE_FIELD_TOO_SHORT",
            Message = $"{field} must be at least 12 characters for stable roleplay grounding.",
            PackId = SourceId,
            NpcId = npcToken,
            SourcePath = SourcePath
        });
    }

    private void ValidateStyleWarnings(string npcToken, string field, string value)
    {
        if (value.Length < 32)
        {
            _validationIssues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Code = "W_VANILLA_STYLE_DETAIL_THIN",
                Message = $"{field} is short and may reduce grounding quality.",
                PackId = SourceId,
                NpcId = npcToken,
                SourcePath = SourcePath
            });
        }

        foreach (var pattern in StyleWarningPatterns)
        {
            if (!pattern.IsMatch(value))
                continue;

            _validationIssues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Code = "W_VANILLA_STYLE_META_LANGUAGE",
                Message = $"{field} contains meta language ('{pattern}').",
                PackId = SourceId,
                NpcId = npcToken,
                SourcePath = SourcePath
            });
        }
    }

    private bool TryResolveRecord(string? rawName, out VanillaLoreRecord? record)
    {
        record = null;
        if (string.IsNullOrWhiteSpace(rawName))
            return false;

        var token = TextTokenUtility.NormalizeToken(rawName);
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (_recordsByToken.TryGetValue(token, out record))
            return true;

        if (_aliasToToken.TryGetValue(token, out var mappedToken)
            && _recordsByToken.TryGetValue(mappedToken, out record))
        {
            return true;
        }

        return false;
    }

    private string GetLocationLore(string? rawLocationName)
    {
        if (string.IsNullOrWhiteSpace(rawLocationName))
            return string.Empty;

        var token = TextTokenUtility.NormalizeToken(rawLocationName);
        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;

        if (_locationLoreByToken.TryGetValue(token, out var exact))
            return exact;

        foreach (var (knownToken, loreText) in _locationLoreByToken.OrderByDescending(kv => kv.Key.Length))
        {
            if (token.Contains(knownToken, StringComparison.OrdinalIgnoreCase)
                || knownToken.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return loreText;
            }
        }

        return string.Empty;
    }

    private List<VanillaLoreRecord> FindReferencedRecords(string text, string? speakingNpcName, int maxMatches)
    {
        var found = new List<VanillaLoreRecord>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var speakingToken = TextTokenUtility.NormalizeToken(speakingNpcName);

        foreach (var alias in _aliasToToken.Keys.OrderByDescending(k => k.Length))
        {
            if (!TextTokenUtility.ContainsToken(text, alias))
                continue;
            if (!_aliasToToken.TryGetValue(alias, out var mappedToken))
                continue;
            if (!seen.Add(mappedToken))
                continue;
            if (!string.IsNullOrWhiteSpace(speakingToken)
                && speakingToken.Equals(mappedToken, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (!_recordsByToken.TryGetValue(mappedToken, out var record))
                continue;

            found.Add(record);
            if (found.Count >= Math.Max(1, maxMatches))
                break;
        }

        return found;
    }

    private static List<string> NormalizeList(List<string>? raw)
    {
        if (raw is null || raw.Count == 0)
            return new List<string>();

        return raw
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => TextTokenUtility.CompactWhitespace(value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ContainsPattern(string text, string pattern)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(pattern))
            return false;

        return Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private void RegisterAlias(string rawAlias, string npcToken)
    {
        var alias = TextTokenUtility.NormalizeToken(rawAlias);
        if (string.IsNullOrWhiteSpace(alias))
            return;

        _aliasToToken[alias] = npcToken;
    }

    private NpcLoreFile MergeLocaleOverlay(NpcLoreFile baseLore, string locale)
    {
        var merged = new NpcLoreFile();
        foreach (var (name, lore) in baseLore.Npcs)
            merged.Npcs[name] = lore;
        foreach (var (location, text) in baseLore.Locations)
            merged.Locations[location] = text;

        foreach (var candidate in BuildLocaleFallback(locale))
        {
            var path = $"i18n/vanilla-canon-lore.{candidate}.json";
            var overlay = _helper.Data.ReadJsonFile<NpcLoreFile>(path);
            if (overlay is null)
                continue;

            foreach (var (name, lore) in overlay.Npcs)
            {
                merged.Npcs[name] = MergeLoreEntry(
                    merged.Npcs.TryGetValue(name, out var current) ? current : new NpcLoreEntry(),
                    lore);
            }

            foreach (var (location, text) in overlay.Locations)
                merged.Locations[location] = text;
        }

        return merged;
    }

    private static NpcLoreEntry MergeLoreEntry(NpcLoreEntry original, NpcLoreEntry overlay)
    {
        if (!string.IsNullOrWhiteSpace(overlay.Role))
            original.Role = overlay.Role.Trim();
        if (!string.IsNullOrWhiteSpace(overlay.Persona))
            original.Persona = overlay.Persona.Trim();
        if (!string.IsNullOrWhiteSpace(overlay.Speech))
            original.Speech = overlay.Speech.Trim();
        if (!string.IsNullOrWhiteSpace(overlay.Ties))
            original.Ties = overlay.Ties.Trim();
        if (!string.IsNullOrWhiteSpace(overlay.Boundaries))
            original.Boundaries = overlay.Boundaries.Trim();
        if (overlay.TimelineAnchors.Count > 0)
            original.TimelineAnchors = overlay.TimelineAnchors;
        if (overlay.KnownLocations.Count > 0)
            original.KnownLocations = overlay.KnownLocations;
        if (overlay.TiesToNpcs.Count > 0)
            original.TiesToNpcs = overlay.TiesToNpcs;
        if (overlay.ForbiddenClaims.Count > 0)
            original.ForbiddenClaims = overlay.ForbiddenClaims;

        return original;
    }

    private static IEnumerable<string> BuildLocaleFallback(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            yield break;

        var normalized = locale.Trim().Replace('_', '-').ToLowerInvariant();
        yield return normalized;

        var dash = normalized.IndexOf('-', StringComparison.Ordinal);
        if (dash > 0)
            yield return normalized[..dash];
    }

    private string ResolveLocale()
    {
        try
        {
            return _localeResolver();
        }
        catch
        {
            return "en";
        }
    }

    private static ValidationIssue Error(string npcToken, string code, string message)
    {
        return new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Code = code,
            Message = message,
            PackId = SourceId,
            NpcId = npcToken,
            SourcePath = SourcePath
        };
    }

    private sealed record VanillaLoreRecord(string Token, string DisplayName, NpcLoreEntry Lore);
}
