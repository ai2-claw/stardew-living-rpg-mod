using System.Reflection;
using System.Text.Json;
using StardewModdingAPI;
using StardewValley;
using StardewLivingRPG.CustomNpcFramework.Models;
using StardewLivingRPG.CustomNpcFramework.Utilities;

namespace StardewLivingRPG.CustomNpcFramework.Services;

internal sealed class PortraitEmotionProfileService
{
    private const string BuiltInProfilesPath = "assets/portrait-emotion-profiles.json";
    private const string ContentPackProfilesPath = "content/portrait-profiles.json";
    private const string AssetsProfilesPath = "assets/portrait-profiles.json";
    private static readonly BindingFlags NpcFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly FieldInfo? NpcLastAppearanceIdField = typeof(NPC).GetField("LastAppearanceId", NpcFieldFlags);
    private static readonly FieldInfo? NpcLastLocationNameForAppearanceField = typeof(NPC).GetField("LastLocationNameForAppearance", NpcFieldFlags);

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly string _hostModUniqueId;
    private readonly Dictionary<string, RuntimePortraitProfile> _profilesByToken = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aliasToToken = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ValidationIssue> _validationIssues = new();

    public PortraitEmotionProfileService(IModHelper helper, IMonitor monitor, string hostModUniqueId)
    {
        _helper = helper;
        _monitor = monitor;
        _hostModUniqueId = hostModUniqueId ?? string.Empty;
    }

    public int LoadedContentPackCount { get; private set; }
    public int LoadedExternalProfileFileCount { get; private set; }
    public int ProfileCount => _profilesByToken.Count;
    public IReadOnlyList<ValidationIssue> ValidationIssues => _validationIssues;

    public void Reload(bool strictMode)
    {
        _profilesByToken.Clear();
        _aliasToToken.Clear();
        _validationIssues.Clear();
        LoadedContentPackCount = 0;
        LoadedExternalProfileFileCount = 0;

        var builtInProfiles = _helper.Data.ReadJsonFile<PortraitEmotionProfilesFile>(BuiltInProfilesPath);
        if (builtInProfiles is null)
        {
            _validationIssues.Add(Warning(
                "_builtin",
                string.Empty,
                BuiltInProfilesPath,
                "W_PORTRAIT_BASE_MISSING",
                $"Built-in portrait profile file '{BuiltInProfilesPath}' was not found."));
        }
        else
        {
            MergeProfiles(
                builtInProfiles,
                sourceId: "_builtin",
                sourcePath: BuiltInProfilesPath,
                strictMode: strictMode,
                replaceExisting: true);
        }

        var contentPacks = _helper.ContentPacks
            .GetOwned()
            .OrderBy(p => p.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var pack in contentPacks)
        {
            var profiles = pack.ReadJsonFile<PortraitEmotionProfilesFile>(ContentPackProfilesPath);
            if (profiles is null || profiles.Npcs.Count == 0)
                continue;

            LoadedContentPackCount++;
            var packId = pack.Manifest.UniqueID ?? string.Empty;
            MergeProfiles(
                profiles,
                sourceId: packId,
                sourcePath: ContentPackProfilesPath,
                strictMode: strictMode,
                replaceExisting: true);
        }

        LoadExternalModProfiles(strictMode);

        _monitor.Log(
            $"Portrait profile framework loaded contentPacks={LoadedContentPackCount}, externalProfileFiles={LoadedExternalProfileFileCount}, npcs={ProfileCount}, issues={_validationIssues.Count}.",
            LogLevel.Info);
    }

    private void LoadExternalModProfiles(bool strictMode)
    {
        var helperDirectory = _helper.DirectoryPath;
        if (string.IsNullOrWhiteSpace(helperDirectory))
            return;

        var modsRoot = TryGetParentDirectory(helperDirectory);
        if (string.IsNullOrWhiteSpace(modsRoot) || !Directory.Exists(modsRoot))
            return;

        var hostDirectory = NormalizePath(helperDirectory);
        foreach (var modDirectory in Directory.GetDirectories(modsRoot))
        {
            var normalizedModDirectory = NormalizePath(modDirectory);
            if (string.Equals(normalizedModDirectory, hostDirectory, StringComparison.OrdinalIgnoreCase))
                continue;

            var contentPackForUniqueId = TryReadManifestContentPackForUniqueId(modDirectory);
            if (!string.IsNullOrWhiteSpace(contentPackForUniqueId)
                && !string.IsNullOrWhiteSpace(_hostModUniqueId)
                && string.Equals(contentPackForUniqueId, _hostModUniqueId, StringComparison.OrdinalIgnoreCase))
            {
                // Owned content packs are already loaded through Helper.ContentPacks.GetOwned().
                continue;
            }

            TryLoadExternalProfileFile(modDirectory, ContentPackProfilesPath, strictMode);
            TryLoadExternalProfileFile(modDirectory, AssetsProfilesPath, strictMode);
        }
    }

    private void TryLoadExternalProfileFile(string modDirectory, string relativePath, bool strictMode)
    {
        var fullPath = Path.Combine(modDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            return;

        PortraitEmotionProfilesFile? profiles;
        try
        {
            profiles = JsonSerializer.Deserialize<PortraitEmotionProfilesFile>(File.ReadAllText(fullPath));
        }
        catch (Exception ex)
        {
            var sourceId = TryReadManifestUniqueId(modDirectory) ?? new DirectoryInfo(modDirectory).Name;
            _validationIssues.Add(Warning(
                sourceId,
                string.Empty,
                relativePath,
                "W_PORTRAIT_PROFILE_PARSE_FAILED",
                $"Failed to read '{relativePath}': {ex.Message}"));
            return;
        }

        if (profiles is null || profiles.Npcs.Count == 0)
            return;

        var packId = TryReadManifestUniqueId(modDirectory) ?? new DirectoryInfo(modDirectory).Name;
        MergeProfiles(
            profiles,
            sourceId: packId,
            sourcePath: relativePath,
            strictMode: strictMode,
            replaceExisting: true);
        LoadedExternalProfileFileCount++;
    }

    private static string? TryReadManifestUniqueId(string modDirectory)
    {
        try
        {
            var manifestPath = Path.Combine(modDirectory, "manifest.json");
            if (!File.Exists(manifestPath))
                return null;

            using var json = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (json.RootElement.TryGetProperty("UniqueID", out var uniqueIdElement)
                && uniqueIdElement.ValueKind == JsonValueKind.String)
            {
                var uniqueId = uniqueIdElement.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(uniqueId))
                    return uniqueId;
            }
        }
        catch
        {
            // Ignore malformed external manifests.
        }

        return null;
    }

    private static string? TryReadManifestContentPackForUniqueId(string modDirectory)
    {
        try
        {
            var manifestPath = Path.Combine(modDirectory, "manifest.json");
            if (!File.Exists(manifestPath))
                return null;

            using var json = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (!json.RootElement.TryGetProperty("ContentPackFor", out var contentPackForElement)
                || contentPackForElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (contentPackForElement.TryGetProperty("UniqueID", out var uniqueIdElement)
                && uniqueIdElement.ValueKind == JsonValueKind.String)
            {
                var uniqueId = uniqueIdElement.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(uniqueId))
                    return uniqueId;
            }
        }
        catch
        {
            // Ignore malformed external manifests.
        }

        return null;
    }

    private static string NormalizePath(string? path)
    {
        var normalized = path ?? string.Empty;
        try
        {
            normalized = Path.GetFullPath(normalized);
        }
        catch
        {
            // Keep original if path normalization fails.
        }

        return normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string? TryGetParentDirectory(string path)
    {
        try
        {
            return Directory.GetParent(path)?.FullName;
        }
        catch
        {
            return null;
        }
    }

    public bool TryResolveFrameIndex(
        NPC? npc,
        string? npcNameOrToken,
        string emotionKey,
        out int frameIndex,
        out string resolutionSource)
    {
        frameIndex = -1;
        resolutionSource = "none";

        if (!TryResolveProfile(npc, npcNameOrToken, out var profile))
            return false;

        var normalizedEmotion = NormalizeEmotionKey(emotionKey);
        var context = BuildResolutionContext(npc, npcNameOrToken);

        var variant = ResolveBestVariant(profile, context);
        if (variant is not null)
        {
            if (TryResolveEmotionFrame(variant.Frames, normalizedEmotion, out frameIndex))
            {
                resolutionSource = $"variant:{variant.Id}";
                return true;
            }

            if (TryResolveEmotionFrame(profile.DefaultFrames, normalizedEmotion, out var baseIndex))
            {
                var offsetIndex = baseIndex + variant.FrameOffset;
                if (offsetIndex >= 0)
                {
                    frameIndex = offsetIndex;
                    resolutionSource = $"variant_offset:{variant.Id}";
                    return true;
                }
            }
        }

        if (TryResolveEmotionFrame(profile.DefaultFrames, normalizedEmotion, out frameIndex))
        {
            resolutionSource = $"default:{profile.Token}";
            return true;
        }

        return false;
    }

    public string BuildProfileDebugDump(string? npcNameOrToken)
    {
        if (string.IsNullOrWhiteSpace(npcNameOrToken))
            return "PortraitProfile: name/token is required.";

        if (!TryResolveProfile(null, npcNameOrToken, out var profile))
            return $"PortraitProfile: no profile found for '{npcNameOrToken}'.";

        var variantIds = profile.Variants.Count == 0
            ? "none"
            : string.Join(", ", profile.Variants.Select(v => $"{v.Id}(priority={v.Priority},offset={v.FrameOffset})"));
        return $"PortraitProfile[{profile.Token}] source={profile.SourceId} aliases={string.Join(", ", profile.Aliases)} variants={variantIds}.";
    }

    private void MergeProfiles(
        PortraitEmotionProfilesFile file,
        string sourceId,
        string sourcePath,
        bool strictMode,
        bool replaceExisting)
    {
        foreach (var (rawToken, entry) in file.Npcs)
        {
            var token = TextTokenUtility.NormalizeToken(rawToken);
            if (string.IsNullOrWhiteSpace(token))
            {
                _validationIssues.Add(Error(
                    sourceId,
                    rawToken,
                    sourcePath,
                    "E_PORTRAIT_NPC_TOKEN_INVALID",
                    "Portrait profile NPC token is missing or invalid."));
                continue;
            }

            if (entry is null)
            {
                _validationIssues.Add(Error(
                    sourceId,
                    token,
                    sourcePath,
                    "E_PORTRAIT_NPC_ENTRY_NULL",
                    "Portrait profile entry is null."));
                continue;
            }

            var defaultFrames = NormalizeFrameMap(
                entry.DefaultFrames ?? new PortraitEmotionFrameMap(),
                sourceId,
                token,
                sourcePath,
                strictMode,
                scope: "default");
            if (strictMode && defaultFrames.Neutral is null)
            {
                _validationIssues.Add(Error(
                    sourceId,
                    token,
                    sourcePath,
                    "E_PORTRAIT_NEUTRAL_REQUIRED",
                    "Strict mode requires a neutral frame in DefaultFrames."));
                continue;
            }

            var variants = NormalizeVariants(
                entry.Variants,
                sourceId,
                token,
                sourcePath,
                strictMode);

            if (replaceExisting && _profilesByToken.ContainsKey(token))
            {
                _validationIssues.Add(Warning(
                    sourceId,
                    token,
                    sourcePath,
                    "W_PORTRAIT_PROFILE_OVERRIDE",
                    $"Portrait profile token '{token}' overrides a previously loaded profile."));
            }

            var aliases = entry.Aliases
                .Append(token)
                .Select(TextTokenUtility.NormalizeToken)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var runtime = new RuntimePortraitProfile(
                token,
                sourceId,
                sourcePath,
                aliases,
                defaultFrames,
                variants);
            _profilesByToken[token] = runtime;

            foreach (var alias in aliases)
            {
                if (_aliasToToken.TryGetValue(alias, out var previousToken)
                    && !string.Equals(previousToken, token, StringComparison.OrdinalIgnoreCase))
                {
                    _validationIssues.Add(Warning(
                        sourceId,
                        token,
                        sourcePath,
                        "W_PORTRAIT_ALIAS_OVERRIDE",
                        $"Alias '{alias}' was remapped from '{previousToken}' to '{token}'."));
                }
                _aliasToToken[alias] = token;
            }
        }
    }

    private List<RuntimePortraitVariant> NormalizeVariants(
        IReadOnlyList<PortraitVariantProfileEntry>? rawVariants,
        string sourceId,
        string token,
        string sourcePath,
        bool strictMode)
    {
        var variants = new List<RuntimePortraitVariant>();
        if (rawVariants is null || rawVariants.Count == 0)
            return variants;

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < rawVariants.Count; i++)
        {
            var raw = rawVariants[i] ?? new PortraitVariantProfileEntry();
            var variantId = string.IsNullOrWhiteSpace(raw.Id)
                ? $"variant_{i + 1}"
                : TextTokenUtility.NormalizeToken(raw.Id);
            if (!seenIds.Add(variantId))
            {
                _validationIssues.Add(Error(
                    sourceId,
                    token,
                    sourcePath,
                    "E_PORTRAIT_VARIANT_DUPLICATE_ID",
                    $"Duplicate variant id '{raw.Id}' in portrait profile '{token}'."));
                continue;
            }

            var frames = NormalizeFrameMap(
                raw.Frames ?? new PortraitEmotionFrameMap(),
                sourceId,
                token,
                sourcePath,
                strictMode,
                scope: $"variant:{variantId}");
            var appearanceContains = NormalizeStringList(raw.AppearanceIdContains, preserveText: true);
            var seasons = NormalizeStringList(raw.Seasons, preserveText: false);
            var locations = NormalizeStringList(raw.Locations, preserveText: false);
            var npcNames = NormalizeStringList(raw.NpcNames, preserveText: false);

            var hasSelector = appearanceContains.Count > 0
                              || seasons.Count > 0
                              || locations.Count > 0
                              || npcNames.Count > 0;
            if (!hasSelector)
            {
                _validationIssues.Add(Warning(
                    sourceId,
                    token,
                    sourcePath,
                    "W_PORTRAIT_VARIANT_BROAD",
                    $"Variant '{variantId}' has no selector constraints and will match all contexts."));
            }

            variants.Add(new RuntimePortraitVariant(
                variantId,
                raw.Priority,
                raw.FrameOffset,
                appearanceContains,
                seasons,
                locations,
                npcNames,
                frames));
        }

        return variants;
    }

    private RuntimeFrameMap NormalizeFrameMap(
        PortraitEmotionFrameMap raw,
        string sourceId,
        string npcToken,
        string sourcePath,
        bool strictMode,
        string scope)
    {
        var normalized = new RuntimeFrameMap
        {
            Neutral = NormalizeFrameValue(raw.Neutral, "Neutral", sourceId, npcToken, sourcePath, scope),
            Happy = NormalizeFrameValue(raw.Happy, "Happy", sourceId, npcToken, sourcePath, scope),
            Content = NormalizeFrameValue(raw.Content, "Content", sourceId, npcToken, sourcePath, scope),
            Blush = NormalizeFrameValue(raw.Blush, "Blush", sourceId, npcToken, sourcePath, scope),
            Sad = NormalizeFrameValue(raw.Sad, "Sad", sourceId, npcToken, sourcePath, scope),
            Angry = NormalizeFrameValue(raw.Angry, "Angry", sourceId, npcToken, sourcePath, scope),
            Worried = NormalizeFrameValue(raw.Worried, "Worried", sourceId, npcToken, sourcePath, scope),
            Surprised = NormalizeFrameValue(raw.Surprised, "Surprised", sourceId, npcToken, sourcePath, scope)
        };

        if (strictMode
            && normalized.Neutral is null
            && normalized.Happy is null
            && normalized.Content is null
            && normalized.Blush is null
            && normalized.Sad is null
            && normalized.Angry is null
            && normalized.Worried is null
            && normalized.Surprised is null)
        {
            _validationIssues.Add(Error(
                sourceId,
                npcToken,
                sourcePath,
                "E_PORTRAIT_FRAMESET_EMPTY",
                $"Frame set '{scope}' for '{npcToken}' does not define any frame indices."));
        }

        return normalized;
    }

    private int? NormalizeFrameValue(
        int? value,
        string emotionName,
        string sourceId,
        string npcToken,
        string sourcePath,
        string scope)
    {
        if (value is null)
            return null;

        if (value.Value < 0)
        {
            _validationIssues.Add(Error(
                sourceId,
                npcToken,
                sourcePath,
                "E_PORTRAIT_FRAME_INVALID",
                $"Frame index for emotion '{emotionName}' in '{scope}' must be >= 0."));
            return null;
        }

        return value.Value;
    }

    private bool TryResolveProfile(NPC? npc, string? npcNameOrToken, out RuntimePortraitProfile profile)
    {
        profile = null!;

        var candidates = new List<string>();
        AddCandidate(candidates, npcNameOrToken);
        AddCandidate(candidates, npc?.Name);
        AddCandidate(candidates, npc?.displayName);

        foreach (var candidate in candidates)
        {
            if (_profilesByToken.TryGetValue(candidate, out var directProfile) && directProfile is not null)
            {
                profile = directProfile;
                return true;
            }
            if (_aliasToToken.TryGetValue(candidate, out var mappedToken)
                && !string.IsNullOrWhiteSpace(mappedToken)
                && _profilesByToken.TryGetValue(mappedToken, out var aliasedProfile)
                && aliasedProfile is not null)
            {
                profile = aliasedProfile;
                return true;
            }
        }

        return false;
    }

    private RuntimePortraitVariant? ResolveBestVariant(RuntimePortraitProfile profile, ResolutionContext context)
    {
        RuntimePortraitVariant? best = null;
        var bestScore = int.MinValue;
        var bestPriority = int.MinValue;
        var bestId = string.Empty;

        foreach (var variant in profile.Variants)
        {
            if (!TryScoreVariant(variant, context, out var score))
                continue;

            if (best is null
                || score > bestScore
                || (score == bestScore && variant.Priority > bestPriority)
                || (score == bestScore
                    && variant.Priority == bestPriority
                    && string.CompareOrdinal(variant.Id, bestId) < 0))
            {
                best = variant;
                bestScore = score;
                bestPriority = variant.Priority;
                bestId = variant.Id;
            }
        }

        return best;
    }

    private static bool TryScoreVariant(RuntimePortraitVariant variant, ResolutionContext context, out int score)
    {
        score = 0;

        if (variant.AppearanceIdContains.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(context.AppearanceId))
                return false;

            var matched = variant.AppearanceIdContains.Any(fragment =>
                context.AppearanceId.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            if (!matched)
                return false;

            score += 8;
        }

        if (variant.Seasons.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(context.SeasonToken)
                || !variant.Seasons.Contains(context.SeasonToken, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            score += 2;
        }

        if (variant.Locations.Count > 0)
        {
            var matched = context.LocationTokens.Any(loc =>
                variant.Locations.Contains(loc, StringComparer.OrdinalIgnoreCase));
            if (!matched)
                return false;

            score += 4;
        }

        if (variant.NpcNames.Count > 0)
        {
            var matched = context.NpcNameTokens.Any(name =>
                variant.NpcNames.Contains(name, StringComparer.OrdinalIgnoreCase));
            if (!matched)
                return false;

            score += 1;
        }

        return true;
    }

    private static bool TryResolveEmotionFrame(RuntimeFrameMap frameMap, string emotionKey, out int frameIndex)
    {
        foreach (var candidate in GetEmotionFallbackKeys(emotionKey))
        {
            if (!TryGetEmotionFrameExact(frameMap, candidate, out frameIndex))
                continue;
            if (frameIndex < 0)
                continue;
            return true;
        }

        frameIndex = -1;
        return false;
    }

    private static bool TryGetEmotionFrameExact(RuntimeFrameMap map, string normalizedEmotion, out int frameIndex)
    {
        frameIndex = normalizedEmotion switch
        {
            "neutral" => map.Neutral ?? -1,
            "happy" => map.Happy ?? -1,
            "content" => map.Content ?? -1,
            "blush" => map.Blush ?? -1,
            "sad" => map.Sad ?? -1,
            "angry" => map.Angry ?? -1,
            "worried" => map.Worried ?? -1,
            "surprised" => map.Surprised ?? -1,
            _ => -1
        };
        return frameIndex >= 0;
    }

    private static IEnumerable<string> GetEmotionFallbackKeys(string normalizedEmotion)
    {
        yield return normalizedEmotion;

        if (normalizedEmotion.Equals("worried", StringComparison.Ordinal))
            yield return "sad";
        else if (normalizedEmotion.Equals("blush", StringComparison.Ordinal))
        {
            yield return "content";
            yield return "happy";
        }
        else if (normalizedEmotion.Equals("content", StringComparison.Ordinal))
            yield return "happy";

        if (!normalizedEmotion.Equals("neutral", StringComparison.Ordinal))
            yield return "neutral";
    }

    private static ResolutionContext BuildResolutionContext(NPC? npc, string? npcNameOrToken)
    {
        var npcNameTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddCandidate(npcNameTokens, npcNameOrToken);
        AddCandidate(npcNameTokens, npc?.Name);
        AddCandidate(npcNameTokens, npc?.displayName);

        var locationTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddCandidate(locationTokens, npc?.currentLocation?.Name);
        AddCandidate(locationTokens, Game1.currentLocation?.Name);
        AddCandidate(locationTokens, ReadNpcFieldAsString(npc, NpcLastLocationNameForAppearanceField));

        var appearance = ReadNpcFieldAsString(npc, NpcLastAppearanceIdField) ?? string.Empty;
        appearance = appearance.Trim().ToLowerInvariant();
        var season = TextTokenUtility.NormalizeToken(Game1.currentSeason);

        return new ResolutionContext(
            appearance,
            season,
            locationTokens,
            npcNameTokens);
    }

    private static string? ReadNpcFieldAsString(NPC? npc, FieldInfo? field)
    {
        if (npc is null || field is null)
            return null;

        try
        {
            return field.GetValue(npc)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static List<string> NormalizeStringList(IEnumerable<string>? rawValues, bool preserveText)
    {
        if (rawValues is null)
            return new List<string>();

        return rawValues
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => preserveText
                ? v.Trim().ToLowerInvariant()
                : TextTokenUtility.NormalizeToken(v))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddCandidate(ICollection<string> values, string? raw)
    {
        var normalized = TextTokenUtility.NormalizeToken(raw);
        if (!string.IsNullOrWhiteSpace(normalized))
            values.Add(normalized);
    }

    private static string NormalizeEmotionKey(string rawEmotion)
    {
        var key = TextTokenUtility.NormalizeToken(rawEmotion);
        return key switch
        {
            "calm" => "neutral",
            "glad" => "happy",
            "cheerful" => "happy",
            "smile" => "happy",
            "blush" => "blush",
            "blushes" => "blush",
            "blushing" => "blush",
            "bashful" => "blush",
            "flustered" => "blush",
            "shy" => "blush",
            "down" => "sad",
            "unhappy" => "sad",
            "mad" => "angry",
            "annoyed" => "angry",
            "frustrated" => "angry",
            "shock" => "surprised",
            "concerned" => "worried",
            "nervous" => "worried",
            "anxious" => "worried",
            "unsure" => "worried",
            "pensive" => "worried",
            "thoughtful" => "worried",
            "contemplative" => "worried",
            _ => key switch
            {
                "neutral" or "happy" or "content" or "blush" or "sad" or "angry" or "worried" or "surprised" => key,
                _ => "neutral"
            }
        };
    }

    private static ValidationIssue Error(string sourceId, string npcId, string sourcePath, string code, string message)
    {
        return new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Code = code,
            Message = message,
            PackId = sourceId,
            NpcId = npcId,
            SourcePath = sourcePath
        };
    }

    private static ValidationIssue Warning(string sourceId, string npcId, string sourcePath, string code, string message)
    {
        return new ValidationIssue
        {
            Severity = ValidationSeverity.Warning,
            Code = code,
            Message = message,
            PackId = sourceId,
            NpcId = npcId,
            SourcePath = sourcePath
        };
    }

    private sealed class RuntimePortraitProfile
    {
        public RuntimePortraitProfile(
            string token,
            string sourceId,
            string sourcePath,
            IReadOnlyList<string> aliases,
            RuntimeFrameMap defaultFrames,
            IReadOnlyList<RuntimePortraitVariant> variants)
        {
            Token = token;
            SourceId = sourceId;
            SourcePath = sourcePath;
            Aliases = aliases;
            DefaultFrames = defaultFrames;
            Variants = variants;
        }

        public string Token { get; }
        public string SourceId { get; }
        public string SourcePath { get; }
        public IReadOnlyList<string> Aliases { get; }
        public RuntimeFrameMap DefaultFrames { get; }
        public IReadOnlyList<RuntimePortraitVariant> Variants { get; }
    }

    private sealed class RuntimePortraitVariant
    {
        public RuntimePortraitVariant(
            string id,
            int priority,
            int frameOffset,
            IReadOnlyList<string> appearanceIdContains,
            IReadOnlyList<string> seasons,
            IReadOnlyList<string> locations,
            IReadOnlyList<string> npcNames,
            RuntimeFrameMap frames)
        {
            Id = id;
            Priority = priority;
            FrameOffset = frameOffset;
            AppearanceIdContains = appearanceIdContains;
            Seasons = seasons;
            Locations = locations;
            NpcNames = npcNames;
            Frames = frames;
        }

        public string Id { get; }
        public int Priority { get; }
        public int FrameOffset { get; }
        public IReadOnlyList<string> AppearanceIdContains { get; }
        public IReadOnlyList<string> Seasons { get; }
        public IReadOnlyList<string> Locations { get; }
        public IReadOnlyList<string> NpcNames { get; }
        public RuntimeFrameMap Frames { get; }
    }

    private sealed class RuntimeFrameMap
    {
        public int? Neutral { get; init; }
        public int? Happy { get; init; }
        public int? Content { get; init; }
        public int? Blush { get; init; }
        public int? Sad { get; init; }
        public int? Angry { get; init; }
        public int? Worried { get; init; }
        public int? Surprised { get; init; }
    }

    private readonly struct ResolutionContext
    {
        public ResolutionContext(
            string appearanceId,
            string seasonToken,
            HashSet<string> locationTokens,
            HashSet<string> npcNameTokens)
        {
            AppearanceId = appearanceId;
            SeasonToken = seasonToken;
            LocationTokens = locationTokens;
            NpcNameTokens = npcNameTokens;
        }

        public string AppearanceId { get; }
        public string SeasonToken { get; }
        public HashSet<string> LocationTokens { get; }
        public HashSet<string> NpcNameTokens { get; }
    }
}
