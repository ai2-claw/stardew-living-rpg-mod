using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class NpcMemoryService
{
    private const int MaxFacts = 200;
    private const int MaxTurns = 40;

    public void WriteFact(SaveState state, string npcName, string category, string text, int day, int weight = 2)
    {
        if (string.IsNullOrWhiteSpace(npcName) || string.IsNullOrWhiteSpace(text))
            return;

        var profile = GetProfile(state, npcName);
        var canonical = text.Trim();
        if (profile.Facts.Any(f => string.Equals(f.Text, canonical, StringComparison.OrdinalIgnoreCase)))
            return;

        profile.Facts.Add(new NpcMemoryFact
        {
            FactId = $"{npcName}:{category}:{day}:{Math.Abs(canonical.GetHashCode()) % 100000}",
            Category = string.IsNullOrWhiteSpace(category) ? "event" : category,
            Text = canonical,
            Day = day,
            Weight = Math.Clamp(weight, 1, 5),
            LastReferencedDay = day
        });

        profile.LastUpdatedDay = day;
        Prune(profile);
    }

    public void WriteTurn(SaveState state, string npcName, string playerText, string npcText, int day)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return;

        var profile = GetProfile(state, npcName);
        profile.RecentTurns.Add(new NpcMemoryTurn
        {
            Day = day,
            PlayerText = playerText ?? string.Empty,
            NpcText = npcText ?? string.Empty,
            Tags = ExtractTags(playerText).ToArray()
        });

        foreach (var tag in ExtractTags(playerText))
        {
            profile.TopicCounters.TryGetValue(tag, out var c);
            profile.TopicCounters[tag] = c + 1;
        }

        profile.LastUpdatedDay = day;
        Prune(profile);
    }

    public string BuildMemoryBlock(SaveState state, string npcName, string playerText, int day, int topK = 4, int charCap = 700)
    {
        var profile = GetProfile(state, npcName);
        var tags = ExtractTags(playerText).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var scoredFacts = profile.Facts
            .Select(f => new { f, score = ScoreFact(f, tags, day) })
            .OrderByDescending(x => x.score)
            .Take(topK)
            .Select(x => $"Fact: {x.f.Text}")
            .ToList();

        var scoredTurns = profile.RecentTurns
            .Select(t => new { t, score = ScoreTurn(t, tags, day) })
            .OrderByDescending(x => x.score)
            .Take(2)
            .Select(x => string.IsNullOrWhiteSpace(x.t.NpcText)
                ? $"Recent: Player said '{TrimForPrompt(x.t.PlayerText, 70)}'."
                : $"Recent: Player '{TrimForPrompt(x.t.PlayerText, 50)}' / NPC '{TrimForPrompt(x.t.NpcText, 50)}'.")
            .ToList();

        var parts = scoredFacts.Concat(scoredTurns).ToList();
        if (parts.Count == 0)
            return string.Empty;

        var block = $"NPC_MEMORY[{npcName}]: " + string.Join(" ", parts);
        return block.Length > charCap ? block[..charCap] : block;
    }

    public string DumpNpcMemory(SaveState state, string npcName)
    {
        var p = GetProfile(state, npcName);
        var facts = string.Join(" | ", p.Facts.Take(6).Select(f => f.Text));
        return $"NPC memory {npcName}: facts={p.Facts.Count}, turns={p.RecentTurns.Count}, sample=[{facts}]";
    }

    private static int ScoreFact(NpcMemoryFact f, HashSet<string> tags, int day)
    {
        var score = f.Weight * 5;
        var age = Math.Max(0, day - f.Day);
        score += Math.Max(0, 20 - age);

        foreach (var t in tags)
        {
            if (f.Text.Contains(t, StringComparison.OrdinalIgnoreCase))
                score += 8;
        }

        return score;
    }

    private static int ScoreTurn(NpcMemoryTurn t, HashSet<string> tags, int day)
    {
        var age = Math.Max(0, day - t.Day);
        var score = Math.Max(0, 15 - age);
        foreach (var tag in tags)
        {
            if (t.PlayerText.Contains(tag, StringComparison.OrdinalIgnoreCase) || t.NpcText.Contains(tag, StringComparison.OrdinalIgnoreCase))
                score += 6;
        }

        return score;
    }

    private static string TrimForPrompt(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        var t = text.Trim();
        return t.Length > max ? t[..max] + "…" : t;
    }

    private static IEnumerable<string> ExtractTags(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var tokens = text
            .ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '!', '?', ':', ';', '\'', '"', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 4)
            .Distinct();

        foreach (var t in tokens)
            yield return t;
    }

    private static NpcMemoryProfile GetProfile(SaveState state, string npcName)
    {
        if (!state.NpcMemory.Profiles.TryGetValue(npcName, out var profile))
        {
            profile = new NpcMemoryProfile();
            state.NpcMemory.Profiles[npcName] = profile;
        }

        return profile;
    }

    private static void Prune(NpcMemoryProfile profile)
    {
        if (profile.Facts.Count > MaxFacts)
        {
            profile.Facts = profile.Facts
                .OrderByDescending(f => f.Weight)
                .ThenByDescending(f => f.Day)
                .Take(MaxFacts)
                .ToList();
        }

        if (profile.RecentTurns.Count > MaxTurns)
            profile.RecentTurns = profile.RecentTurns.TakeLast(MaxTurns).ToList();
    }
}
