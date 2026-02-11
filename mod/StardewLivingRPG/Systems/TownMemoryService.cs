using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class TownMemoryService
{
    public void RecordEvent(SaveState state, string kind, string summary, string location, int day, int severity, string visibility, params string[] tags)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return;

        var dedupe = state.TownMemory.Events.FirstOrDefault(e =>
            string.Equals(e.Kind, kind, StringComparison.OrdinalIgnoreCase)
            && string.Equals(e.Location, location, StringComparison.OrdinalIgnoreCase)
            && Math.Abs(e.Day - day) <= 1
            && string.Equals(e.Summary, summary, StringComparison.OrdinalIgnoreCase));

        if (dedupe is not null)
            return;

        var ev = new TownMemoryEvent
        {
            EventId = $"{kind}:{day}:{Math.Abs((summary + location).GetHashCode()) % 100000}",
            Kind = kind,
            Summary = summary,
            Location = location,
            Day = day,
            Severity = Math.Clamp(severity, 1, 5),
            Visibility = string.IsNullOrWhiteSpace(visibility) ? "local" : visibility,
            Tags = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>(),
            MentionBudget = 4
        };

        state.TownMemory.Events.Add(ev);
        PropagateEvent(state, ev);

        if (state.TownMemory.Events.Count > 300)
            state.TownMemory.Events = state.TownMemory.Events.TakeLast(300).ToList();
    }

    public string BuildTownMemoryBlock(SaveState state, string npcName, string playerText, int day, int maxChars = 280)
    {
        if (!state.TownMemory.KnowledgeByNpc.TryGetValue(npcName, out var knowledge))
            return string.Empty;

        var tags = ExtractTags(playerText).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = new List<(TownMemoryEvent Ev, TownKnowledgeEntry K, int Score)>();
        foreach (var ev in state.TownMemory.Events)
        {
            if (!knowledge.ByEventId.TryGetValue(ev.EventId, out var k) || !k.Knows)
                continue;
            if (k.MentionCount >= ev.MentionBudget)
                continue;
            if (day - k.LastMentionDay < 2 && k.LastMentionDay > 0)
                continue;

            var score = ev.Severity * 6 + Math.Max(0, 10 - (day - ev.Day));
            foreach (var tag in tags)
            {
                if (ev.Summary.Contains(tag, StringComparison.OrdinalIgnoreCase) || ev.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                    score += 5;
            }

            candidates.Add((ev, k, score));
        }

        var top = candidates.OrderByDescending(c => c.Score).Take(2).ToList();
        if (top.Count == 0)
            return string.Empty;

        foreach (var t in top)
        {
            t.K.MentionCount += 1;
            t.K.LastMentionDay = day;
        }

        var body = string.Join(" ", top.Select(t => $"{t.Ev.Summary} (known to {npcName}; {t.K.Angle})."));
        var block = $"TOWN_MEMORY_FOR_{npcName.ToUpperInvariant()}: {body}";
        return block.Length > maxChars ? block[..maxChars] : block;
    }

    public string DumpNpcTownMemory(SaveState state, string npcName)
    {
        if (!state.TownMemory.KnowledgeByNpc.TryGetValue(npcName, out var k))
            return $"Town memory {npcName}: none";

        var known = k.ByEventId.Values.Count(v => v.Knows);
        return $"Town memory {npcName}: knownEvents={known}";
    }

    private static void PropagateEvent(SaveState state, TownMemoryEvent ev)
    {
        var defaultNpcList = new[] { "Lewis", "Robin", "Pierre", "Demetrius", "Linus", "Haley", "Alex", "Wizard" };

        foreach (var npc in defaultNpcList)
        {
            var knows = ev.Visibility.Equals("public", StringComparison.OrdinalIgnoreCase)
                || npc.Equals("Lewis", StringComparison.OrdinalIgnoreCase)
                || (ev.Tags.Contains("mines") && (npc.Equals("Robin", StringComparison.OrdinalIgnoreCase) || npc.Equals("Linus", StringComparison.OrdinalIgnoreCase)))
                || ev.Severity >= 4;

            if (!state.TownMemory.KnowledgeByNpc.TryGetValue(npc, out var perNpc))
            {
                perNpc = new NpcTownKnowledge();
                state.TownMemory.KnowledgeByNpc[npc] = perNpc;
            }

            perNpc.ByEventId[ev.EventId] = new TownKnowledgeEntry
            {
                Knows = knows,
                LearnedDay = ev.Day,
                Angle = npc.Equals("Robin", StringComparison.OrdinalIgnoreCase) ? "concerned" : npc.Equals("Lewis", StringComparison.OrdinalIgnoreCase) ? "official" : "town-talk"
            };
        }
    }

    private static IEnumerable<string> ExtractTags(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        foreach (var token in text.ToLowerInvariant()
                     .Split(new[] { ' ', ',', '.', '!', '?', ':', ';', '\'', '"', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                     .Where(t => t.Length >= 4)
                     .Distinct())
            yield return token;
    }
}
