using System.Collections;
using StardewLivingRPG.CustomNpcFramework.Models;
using StardewLivingRPG.CustomNpcFramework.Utilities;

namespace StardewLivingRPG.CustomNpcFramework.Services;

internal sealed class NpcRegistry
{
    private readonly Dictionary<string, FrameworkNpcRecord> _npcsByToken = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aliasToNpcToken = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _locationLoreByToken = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, FrameworkNpcRecord> NpcsByToken => _npcsByToken;
    public IReadOnlyDictionary<string, string> LocationLoreByToken => _locationLoreByToken;

    public IReadOnlyList<ValidationIssue> BuildFromPacks(IReadOnlyList<LoadedNpcPack> packs)
    {
        _npcsByToken.Clear();
        _aliasToNpcToken.Clear();
        _locationLoreByToken.Clear();

        var issues = new List<ValidationIssue>();

        foreach (var pack in packs)
        {
            foreach (var (token, npc) in pack.NpcsByToken)
            {
                if (_npcsByToken.ContainsKey(token))
                {
                    issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Error,
                        Code = "E_DUPLICATE_NPC_TOKEN",
                        Message = $"NPC token '{token}' is duplicated across packs. Keep unique NPC ids.",
                        PackId = pack.PackId,
                        NpcId = npc.NpcId,
                        SourcePath = "content/npcs.json"
                    });
                    continue;
                }

                _npcsByToken[token] = npc;

                RegisterAlias(token, token);
                RegisterAlias(npc.DisplayName, token);
                RegisterAlias(npc.NpcId, token);
                foreach (var alias in npc.Aliases)
                    RegisterAlias(alias, token);
            }

            foreach (var (location, loreText) in pack.LocationLoreByToken)
            {
                if (string.IsNullOrWhiteSpace(loreText))
                    continue;

                _locationLoreByToken[location] = loreText;
            }
        }

        foreach (var npc in _npcsByToken.Values)
        {
            foreach (var tie in npc.Lore.TiesToNpcs)
            {
                var tieToken = TextTokenUtility.NormalizeToken(tie);
                if (string.IsNullOrWhiteSpace(tieToken))
                    continue;
                if (!_npcsByToken.ContainsKey(tieToken))
                    continue;
            }
        }

        return issues;
    }

    public IReadOnlyList<string> GetAllNpcDisplayNames()
    {
        return _npcsByToken.Values
            .Select(v => v.DisplayName)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool TryGetNpcByName(string? rawName, out FrameworkNpcRecord npc)
    {
        npc = null!;
        if (string.IsNullOrWhiteSpace(rawName))
            return false;

        var token = TextTokenUtility.NormalizeToken(rawName);
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (_npcsByToken.TryGetValue(token, out var direct))
        {
            npc = direct;
            return true;
        }

        if (_aliasToNpcToken.TryGetValue(token, out var mapped) && _npcsByToken.TryGetValue(mapped, out var aliased))
        {
            npc = aliased;
            return true;
        }

        return false;
    }

    public bool TryResolveNpcTokenInText(string? text, out string npcToken)
    {
        npcToken = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (var candidate in _aliasToNpcToken.Keys.OrderByDescending(k => k.Length))
        {
            if (!TextTokenUtility.ContainsToken(text, candidate))
                continue;

            if (_aliasToNpcToken.TryGetValue(candidate, out var mapped) && _npcsByToken.ContainsKey(mapped))
            {
                npcToken = mapped;
                return true;
            }
        }

        return false;
    }

    public bool TryResolveNpcTokenFromTownEvent(object ev, out string npcToken)
    {
        npcToken = string.Empty;
        if (ev is null)
            return false;

        var tags = ReadStringEnumerableProperty(ev, "Tags");
        foreach (var tag in tags)
        {
            if (TryResolveNpcTokenInText(tag, out npcToken))
                return true;
        }

        var summary = ReadStringProperty(ev, "Summary");
        var location = ReadStringProperty(ev, "Location");
        var combined = $"{summary} {location}";
        if (TryResolveNpcTokenInText(combined, out npcToken))
            return true;

        return false;
    }

    public string BuildLorePromptBlock(string? npcName, string? locationName, string contextTag)
    {
        var parts = new List<string>();
        var hasNpcLore = TryGetNpcByName(npcName, out var npc);
        var locationLore = GetLocationLore(locationName);

        if (!hasNpcLore && string.IsNullOrWhiteSpace(locationLore))
            return string.Empty;

        parts.Add("CUSTOM_ROLEPLAY_RULE: Follow CUSTOM_NPC_LORE and CUSTOM_LOCATION_LORE exactly when provided.");
        parts.Add($"CUSTOM_CONTEXT: {TextTokenUtility.TrimForPrompt(contextTag, 32)}.");

        if (hasNpcLore)
        {
            var tiesToNpcs = npc.Lore.TiesToNpcs.Count == 0
                ? "none"
                : string.Join(", ", npc.Lore.TiesToNpcs.Take(5));
            var timeline = npc.Lore.TimelineAnchors.Count == 0
                ? "none"
                : string.Join(", ", npc.Lore.TimelineAnchors.Take(4));

            parts.Add(
                $"CUSTOM_NPC_LORE[{npc.DisplayName}]: role={TextTokenUtility.TrimForPrompt(npc.Lore.Role, 140)}; " +
                $"persona={TextTokenUtility.TrimForPrompt(npc.Lore.Persona, 140)}; " +
                $"speech={TextTokenUtility.TrimForPrompt(npc.Lore.Speech, 140)}; " +
                $"ties={TextTokenUtility.TrimForPrompt(npc.Lore.Ties, 160)}; " +
                $"boundaries={TextTokenUtility.TrimForPrompt(npc.Lore.Boundaries, 160)}; " +
                $"timeline={TextTokenUtility.TrimForPrompt(timeline, 90)}; ties_to_npcs={TextTokenUtility.TrimForPrompt(tiesToNpcs, 90)}.");
        }

        if (!string.IsNullOrWhiteSpace(locationLore))
            parts.Add($"CUSTOM_LOCATION_LORE: {TextTokenUtility.TrimForPrompt(locationLore, 220)}.");

        return string.Join(" ", parts);
    }

    public string BuildReferencedNpcLorePromptBlock(string? playerText, string? speakingNpcName = null, int maxMatches = 2)
    {
        if (string.IsNullOrWhiteSpace(playerText) || _npcsByToken.Count == 0)
            return string.Empty;

        var referenced = FindReferencedNpcRecords(playerText, speakingNpcName, maxMatches);
        if (referenced.Count == 0)
            return string.Empty;

        var parts = new List<string>
        {
            "CUSTOM_NPC_REFERENCE_RULE: If the player asks about a referenced custom NPC, use CUSTOM_NPC_REFERENCE_LORE and avoid inventing details."
        };

        foreach (var npc in referenced)
        {
            parts.Add(
                $"CUSTOM_NPC_REFERENCE_LORE[{npc.DisplayName}]: role={TextTokenUtility.TrimForPrompt(npc.Lore.Role, 120)}; " +
                $"persona={TextTokenUtility.TrimForPrompt(npc.Lore.Persona, 120)}; " +
                $"speech={TextTokenUtility.TrimForPrompt(npc.Lore.Speech, 110)}; " +
                $"ties={TextTokenUtility.TrimForPrompt(npc.Lore.Ties, 130)}; " +
                $"boundaries={TextTokenUtility.TrimForPrompt(npc.Lore.Boundaries, 130)}.");
        }

        return string.Join(" ", parts);
    }

    public string BuildLoreDebugDump(FrameworkNpcRecord npc)
    {
        var modules = $"quest={npc.Modules.EnableQuestProposals}, rumors={npc.Modules.EnableRumors}, articles={npc.Modules.EnableArticles}, events={npc.Modules.EnableTownEvents}";
        return $"NPC[{npc.DisplayName}] token={npc.NpcToken} pack={npc.PackId} home={npc.HomeRegionToken} modules={modules} role={npc.Lore.Role} persona={npc.Lore.Persona}";
    }

    private static string ReadStringProperty(object obj, string propertyName)
    {
        var prop = obj.GetType().GetProperty(propertyName);
        return prop?.GetValue(obj) as string ?? string.Empty;
    }

    private static IEnumerable<string> ReadStringEnumerableProperty(object obj, string propertyName)
    {
        var prop = obj.GetType().GetProperty(propertyName);
        var raw = prop?.GetValue(obj);
        if (raw is null)
            yield break;

        if (raw is IEnumerable<string> typed)
        {
            foreach (var item in typed)
            {
                if (!string.IsNullOrWhiteSpace(item))
                    yield return item;
            }
            yield break;
        }

        if (raw is not IEnumerable untyped)
            yield break;

        foreach (var item in untyped)
        {
            if (item is string str && !string.IsNullOrWhiteSpace(str))
                yield return str;
        }
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

    private void RegisterAlias(string rawAlias, string npcToken)
    {
        var alias = TextTokenUtility.NormalizeToken(rawAlias);
        if (string.IsNullOrWhiteSpace(alias))
            return;
        _aliasToNpcToken[alias] = npcToken;
    }

    private List<FrameworkNpcRecord> FindReferencedNpcRecords(string text, string? speakingNpcName, int maxMatches)
    {
        var found = new List<FrameworkNpcRecord>();
        var seenTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var speakingToken = TextTokenUtility.NormalizeToken(speakingNpcName);

        foreach (var alias in _aliasToNpcToken.Keys.OrderByDescending(k => k.Length))
        {
            if (!TextTokenUtility.ContainsToken(text, alias))
                continue;
            if (!_aliasToNpcToken.TryGetValue(alias, out var mappedToken))
                continue;
            if (string.IsNullOrWhiteSpace(mappedToken))
                continue;
            if (!seenTokens.Add(mappedToken))
                continue;
            if (!string.IsNullOrWhiteSpace(speakingToken) && speakingToken.Equals(mappedToken, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!_npcsByToken.TryGetValue(mappedToken, out var npc))
                continue;

            found.Add(npc);
            if (found.Count >= Math.Max(1, maxMatches))
                break;
        }

        return found;
    }
}

