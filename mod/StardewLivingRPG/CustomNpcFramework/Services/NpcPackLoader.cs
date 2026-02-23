using System.Collections;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewLivingRPG.CustomNpcFramework.Models;
using StardewLivingRPG.CustomNpcFramework.Utilities;

namespace StardewLivingRPG.CustomNpcFramework.Services;

internal sealed class NpcPackLoader
{
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
    private readonly string _frameworkVersion;
    private readonly CanonBaselineService _canonBaseline;
    private readonly Func<string> _localeResolver;
    private readonly bool _strictCanonValidation;

    public NpcPackLoader(
        IModHelper helper,
        IMonitor monitor,
        string frameworkVersion,
        CanonBaselineService canonBaseline,
        Func<string> localeResolver,
        bool strictCanonValidation)
    {
        _helper = helper;
        _monitor = monitor;
        _frameworkVersion = frameworkVersion;
        _canonBaseline = canonBaseline;
        _localeResolver = localeResolver;
        _strictCanonValidation = strictCanonValidation;
    }

    public IReadOnlyList<LoadedNpcPack> Load(out IReadOnlyList<ValidationIssue> issues)
    {
        var allIssues = new List<ValidationIssue>();
        var loadedPacks = new List<LoadedNpcPack>();
        var locale = _localeResolver();

        var contentPacks = _helper.ContentPacks
            .GetOwned()
            .OrderBy(p => p.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var pack in contentPacks)
        {
            var packIssues = new List<ValidationIssue>();
            var packId = pack.Manifest.UniqueID ?? string.Empty;

            if (!TryReadManifestExtraField(pack.Manifest, "TLVPackVersion", out var packFormatVersion))
            {
                packIssues.Add(Error(packId, string.Empty, "manifest.json", "E_PACK_VERSION_MISSING", "Missing required manifest field 'TLVPackVersion'."));
            }

            if (!TryReadManifestExtraField(pack.Manifest, "TLVFrameworkMinVersion", out var frameworkMinVersion))
            {
                packIssues.Add(Error(packId, string.Empty, "manifest.json", "E_FRAMEWORK_MIN_VERSION_MISSING", "Missing required manifest field 'TLVFrameworkMinVersion'."));
            }

            if (!string.IsNullOrWhiteSpace(frameworkMinVersion)
                && !VersionUtility.IsFrameworkVersionCompatible(_frameworkVersion, frameworkMinVersion))
            {
                packIssues.Add(Error(
                    packId,
                    string.Empty,
                    "manifest.json",
                    "E_FRAMEWORK_VERSION_UNSUPPORTED",
                    $"Pack requires framework version >= {frameworkMinVersion}, current is {_frameworkVersion}."));
            }

            var identities = pack.ReadJsonFile<NpcListFile>("content/npcs.json");
            if (identities is null || identities.Npcs.Count == 0)
            {
                packIssues.Add(Error(packId, string.Empty, "content/npcs.json", "E_NPCS_FILE_MISSING", "Missing or empty required file content/npcs.json."));
            }

            var loreBase = pack.ReadJsonFile<NpcLoreFile>("content/lore.json");
            if (loreBase is null || loreBase.Npcs.Count == 0)
            {
                packIssues.Add(Error(packId, string.Empty, "content/lore.json", "E_LORE_FILE_MISSING", "Missing or empty required file content/lore.json."));
            }

            var modules = pack.ReadJsonFile<NpcModulesFile>("content/modules.json") ?? new NpcModulesFile();
            var canonDelta = pack.ReadJsonFile<CanonDeltaFile>("content/canon-delta.json") ?? new CanonDeltaFile();
            var lore = MergeLocaleOverlay(pack, loreBase ?? new NpcLoreFile(), locale);

            if (packIssues.Any(i => i.Severity == ValidationSeverity.Error))
            {
                allIssues.AddRange(packIssues);
                continue;
            }

            var validPack = BuildValidatedPack(
                pack,
                packFormatVersion,
                frameworkMinVersion,
                identities!,
                lore,
                modules,
                canonDelta,
                packIssues);

            allIssues.AddRange(packIssues);
            if (validPack is not null)
                loadedPacks.Add(validPack);
        }

        issues = allIssues;
        _monitor.Log($"Discovered content packs: {contentPacks.Length}; loaded valid packs: {loadedPacks.Count}.", LogLevel.Info);
        return loadedPacks;
    }

    private LoadedNpcPack? BuildValidatedPack(
        IContentPack pack,
        string packFormatVersion,
        string frameworkMinVersion,
        NpcListFile npcFile,
        NpcLoreFile loreFile,
        NpcModulesFile modulesFile,
        CanonDeltaFile deltaFile,
        List<ValidationIssue> issues)
    {
        var packId = pack.Manifest.UniqueID ?? string.Empty;
        var npcRecords = new Dictionary<string, FrameworkNpcRecord>(StringComparer.OrdinalIgnoreCase);
        var seenNpcTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var loreByToken = loreFile.Npcs
            .ToDictionary(
                kv => TextTokenUtility.NormalizeToken(kv.Key),
                kv => kv.Value,
                StringComparer.OrdinalIgnoreCase);
        var modulesByToken = modulesFile.Npcs
            .ToDictionary(
                kv => TextTokenUtility.NormalizeToken(kv.Key),
                kv => kv.Value ?? new NpcModuleConfig(),
                StringComparer.OrdinalIgnoreCase);

        var allowedLocationTokens = new HashSet<string>(_canonBaseline.CanonLocationTokens, StringComparer.OrdinalIgnoreCase);
        foreach (var item in deltaFile.AdditionalLocationTokens)
        {
            var token = TextTokenUtility.NormalizeToken(item);
            if (!string.IsNullOrWhiteSpace(token))
                allowedLocationTokens.Add(token);
        }

        var allowedTimelineAnchors = new HashSet<string>(_canonBaseline.TimelineAnchors, StringComparer.OrdinalIgnoreCase);
        foreach (var item in deltaFile.AdditionalTimelineAnchors)
        {
            var token = TextTokenUtility.NormalizeToken(item);
            if (!string.IsNullOrWhiteSpace(token))
                allowedTimelineAnchors.Add(token);
        }

        foreach (var npc in npcFile.Npcs)
        {
            var npcToken = TextTokenUtility.NormalizeToken(npc.Id);
            if (string.IsNullOrWhiteSpace(npcToken))
            {
                issues.Add(Error(packId, npc.Id, "content/npcs.json", "E_NPC_ID_INVALID", "Npc id is missing or invalid."));
                continue;
            }

            if (!seenNpcTokens.Add(npcToken))
            {
                issues.Add(Error(packId, npc.Id, "content/npcs.json", "E_NPC_ID_DUPLICATE", $"Duplicate npc id '{npc.Id}'."));
                continue;
            }

            if (_canonBaseline.CanonNpcTokens.Contains(npcToken) && _strictCanonValidation)
            {
                issues.Add(Error(
                    packId,
                    npc.Id,
                    "content/npcs.json",
                    "E_CANON_NPC_COLLISION",
                    $"Npc id '{npc.Id}' collides with TLV baseline canon npc token '{npcToken}'."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(npc.DisplayName))
            {
                issues.Add(Error(packId, npc.Id, "content/npcs.json", "E_NPC_DISPLAY_NAME_MISSING", "DisplayName is required."));
                continue;
            }

            var regionToken = TextTokenUtility.NormalizeToken(npc.HomeRegion);
            if (_strictCanonValidation && string.IsNullOrWhiteSpace(regionToken))
            {
                issues.Add(Error(packId, npc.Id, "content/npcs.json", "E_HOME_REGION_REQUIRED", "HomeRegion is required in strict canon mode."));
                continue;
            }

            if (_strictCanonValidation && !string.IsNullOrWhiteSpace(regionToken) && !allowedLocationTokens.Contains(regionToken))
            {
                issues.Add(Error(
                    packId,
                    npc.Id,
                    "content/npcs.json",
                    "E_HOME_REGION_UNKNOWN",
                    $"HomeRegion '{npc.HomeRegion}' is not in TLV canon baseline or this pack's canon-delta."));
                continue;
            }

            if (!loreByToken.TryGetValue(npcToken, out var lore))
            {
                issues.Add(Error(
                    packId,
                    npc.Id,
                    "content/lore.json",
                    "E_NPC_LORE_MISSING",
                    $"No lore entry was found for npc id '{npc.Id}'."));
                continue;
            }

            ValidateLoreEntry(packId, npc, lore, allowedLocationTokens, allowedTimelineAnchors, issues);
            var hasNpcErrors = issues.Any(i =>
                i.Severity == ValidationSeverity.Error
                && i.PackId.Equals(packId, StringComparison.OrdinalIgnoreCase)
                && i.NpcId.Equals(npc.Id, StringComparison.OrdinalIgnoreCase));
            if (hasNpcErrors)
                continue;

            if (!modulesByToken.TryGetValue(npcToken, out var moduleConfig))
                moduleConfig = new NpcModuleConfig();

            var aliases = npc.Aliases
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var tags = npc.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => TextTokenUtility.NormalizeToken(t))
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            npcRecords[npcToken] = new FrameworkNpcRecord
            {
                PackId = packId,
                PackName = pack.Manifest.Name ?? packId,
                NpcToken = npcToken,
                NpcId = npc.Id.Trim(),
                DisplayName = npc.DisplayName.Trim(),
                HomeRegionToken = regionToken,
                Aliases = aliases,
                Tags = tags,
                Lore = lore,
                Modules = moduleConfig
            };
        }

        var hasPackErrors = issues.Any(i => i.Severity == ValidationSeverity.Error && i.PackId.Equals(packId, StringComparison.OrdinalIgnoreCase));
        if (hasPackErrors)
            return null;

        var mergedLocations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rawToken, text) in loreFile.Locations)
        {
            var token = TextTokenUtility.NormalizeToken(rawToken);
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(text))
                continue;

            if (_strictCanonValidation && !allowedLocationTokens.Contains(token))
            {
                issues.Add(Error(
                    packId,
                    string.Empty,
                    "content/lore.json",
                    "E_LOCATION_LORE_UNKNOWN",
                    $"Location lore token '{rawToken}' is not in TLV baseline or pack canon-delta."));
                continue;
            }

            mergedLocations[token] = TextTokenUtility.CompactWhitespace(text);
        }

        if (issues.Any(i => i.Severity == ValidationSeverity.Error && i.PackId.Equals(packId, StringComparison.OrdinalIgnoreCase)))
            return null;

        return new LoadedNpcPack
        {
            PackId = packId,
            PackName = pack.Manifest.Name ?? packId,
            PackVersion = packFormatVersion,
            FrameworkMinVersion = frameworkMinVersion,
            NpcsByToken = npcRecords,
            LocationLoreByToken = mergedLocations
        };
    }

    private NpcLoreFile MergeLocaleOverlay(IContentPack pack, NpcLoreFile baseLore, string locale)
    {
        var merged = new NpcLoreFile();
        foreach (var (name, lore) in baseLore.Npcs)
            merged.Npcs[name] = lore;
        foreach (var (loc, text) in baseLore.Locations)
            merged.Locations[loc] = text;

        foreach (var candidate in BuildLocaleFallback(locale))
        {
            var path = $"i18n/lore.{candidate}.json";
            var overlay = pack.ReadJsonFile<NpcLoreFile>(path);
            if (overlay is null)
                continue;

            foreach (var (name, lore) in overlay.Npcs)
                merged.Npcs[name] = MergeLoreEntry(merged.Npcs.TryGetValue(name, out var current) ? current : new NpcLoreEntry(), lore);
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

    private void ValidateLoreEntry(
        string packId,
        NpcIdentityEntry npc,
        NpcLoreEntry lore,
        HashSet<string> allowedLocationTokens,
        HashSet<string> allowedTimelineAnchors,
        List<ValidationIssue> issues)
    {
        ValidateRequiredLoreText(packId, npc.Id, "Role", lore.Role, issues);
        ValidateRequiredLoreText(packId, npc.Id, "Persona", lore.Persona, issues);
        ValidateRequiredLoreText(packId, npc.Id, "Speech", lore.Speech, issues);
        ValidateRequiredLoreText(packId, npc.Id, "Ties", lore.Ties, issues);
        ValidateRequiredLoreText(packId, npc.Id, "Boundaries", lore.Boundaries, issues);

        ValidateStyleWarnings(packId, npc.Id, "Role", lore.Role, issues);
        ValidateStyleWarnings(packId, npc.Id, "Persona", lore.Persona, issues);
        ValidateStyleWarnings(packId, npc.Id, "Speech", lore.Speech, issues);
        ValidateStyleWarnings(packId, npc.Id, "Ties", lore.Ties, issues);
        ValidateStyleWarnings(packId, npc.Id, "Boundaries", lore.Boundaries, issues);

        if (_strictCanonValidation)
        {
            foreach (var location in lore.KnownLocations)
            {
                var token = TextTokenUtility.NormalizeToken(location);
                if (string.IsNullOrWhiteSpace(token))
                    continue;
                if (!allowedLocationTokens.Contains(token))
                {
                    issues.Add(Error(
                        packId,
                        npc.Id,
                        "content/lore.json",
                        "E_NPC_LOCATION_UNKNOWN",
                        $"KnownLocations entry '{location}' is not in TLV baseline or this pack's canon-delta."));
                }
            }

            foreach (var anchor in lore.TimelineAnchors)
            {
                var token = TextTokenUtility.NormalizeToken(anchor);
                if (string.IsNullOrWhiteSpace(token))
                    continue;
                if (!allowedTimelineAnchors.Contains(token))
                {
                    issues.Add(Error(
                        packId,
                        npc.Id,
                        "content/lore.json",
                        "E_TIMELINE_ANCHOR_UNKNOWN",
                        $"TimelineAnchors entry '{anchor}' is not in TLV baseline or this pack's canon-delta."));
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
                    issues.Add(Error(
                        packId,
                        npc.Id,
                        "content/lore.json",
                        "E_CANON_PATTERN_MATCH",
                        $"Lore text matched forbidden canon pattern '{pattern}'."));
                }
            }
        }
    }

    private static void ValidateRequiredLoreText(string packId, string npcId, string field, string value, List<ValidationIssue> issues)
    {
        var compact = TextTokenUtility.CompactWhitespace(value);
        if (compact.Length < 12)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = "E_LORE_FIELD_TOO_SHORT",
                Message = $"{field} must be at least 12 characters to support roleplay immersion.",
                PackId = packId,
                NpcId = npcId,
                SourcePath = "content/lore.json"
            });
        }
    }

    private static void ValidateStyleWarnings(string packId, string npcId, string field, string value, List<ValidationIssue> issues)
    {
        var compact = TextTokenUtility.CompactWhitespace(value);
        if (compact.Length < 32)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Code = "W_STYLE_DETAIL_THIN",
                Message = $"{field} is very short and may reduce immersion depth.",
                PackId = packId,
                NpcId = npcId,
                SourcePath = "content/lore.json"
            });
        }

        foreach (var pattern in StyleWarningPatterns)
        {
            if (!pattern.IsMatch(compact))
                continue;
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Code = "W_STYLE_META_LANGUAGE",
                Message = $"{field} contains meta language ('{pattern}'), which can break immersion.",
                PackId = packId,
                NpcId = npcId,
                SourcePath = "content/lore.json"
            });
        }
    }

    private static bool ContainsPattern(string text, string pattern)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(pattern))
            return false;
        return Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private static ValidationIssue Error(string packId, string npcId, string sourcePath, string code, string message)
    {
        return new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Code = code,
            Message = message,
            PackId = packId,
            NpcId = npcId,
            SourcePath = sourcePath
        };
    }

    private static bool TryReadManifestExtraField(object manifest, string key, out string value)
    {
        value = string.Empty;

        var prop = manifest.GetType().GetProperty("ExtraFields");
        var raw = prop?.GetValue(manifest);
        if (raw is null)
            return false;

        if (raw is IDictionary dict)
        {
            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Key is not string k || !k.Equals(key, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (entry.Value is null)
                    return false;
                value = entry.Value.ToString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(value);
            }

            return false;
        }

        if (raw is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is null)
                    continue;

                var itemType = item.GetType();
                var keyProp = itemType.GetProperty("Key");
                var valueProp = itemType.GetProperty("Value");
                if (keyProp is null || valueProp is null)
                    continue;

                var itemKey = keyProp.GetValue(item)?.ToString() ?? string.Empty;
                if (!itemKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    continue;

                value = valueProp.GetValue(item)?.ToString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        // Fallback for dictionary-like indexers.
        var containsKeyMethod = raw.GetType().GetMethod("ContainsKey", new[] { typeof(string) });
        var indexer = raw.GetType().GetProperty("Item", new[] { typeof(string) });
        if (containsKeyMethod is not null && indexer is not null)
        {
            var contains = containsKeyMethod.Invoke(raw, new object[] { key }) as bool?;
            if (contains == true)
            {
                value = indexer.GetValue(raw, new object[] { key })?.ToString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        return false;
    }

    private static bool? AsNullableBool(object? value)
    {
        if (value is bool b)
            return b;
        if (value is null)
            return null;
        if (bool.TryParse(value.ToString(), out var parsed))
            return parsed;
        return null;
    }
}

