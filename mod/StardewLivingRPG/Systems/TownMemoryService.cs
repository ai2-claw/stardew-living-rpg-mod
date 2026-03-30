using StardewLivingRPG.State;
using StardewLivingRPG.Utils;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class TownMemoryService
{
    private static readonly string[] DefaultTownNpcRoster =
    {
        "Lewis", "Pierre", "Robin",
        "Abigail", "Alex", "Caroline", "Clint", "Demetrius",
        "Dwarf", "Elliott", "Emily", "Evelyn", "George", "Gil", "Gunther",
        "Gus", "Haley", "Harvey", "Jas", "Jodi", "Kent",
        "Krobus", "Leah", "Leo", "Linus", "Marnie", "Marlon", "Maru", "Morris",
        "Pam", "Penny", "Qi", "Sam", "Sandy", "Sebastian", "Shane",
        "Vincent", "Willy", "Wizard"
    };

    public string RecordEvent(
        SaveState state,
        string kind,
        string summary,
        string location,
        int day,
        int severity,
        string visibility,
        string sourceNpc = "",
        params string[] tags)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return string.Empty;

        var normalizedKind = (kind ?? string.Empty).Trim();
        var normalizedSummary = summary.Trim();
        var normalizedLocation = string.IsNullOrWhiteSpace(location) ? "Town" : location.Trim();
        var normalizedTags = tags?
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
        var incomingSummaryTokens = BuildSemanticTokenSet(normalizedSummary);

        var dedupe = state.TownMemory.Events.FirstOrDefault(e =>
            IsSemanticallyDuplicateEvent(
                e,
                normalizedKind,
                normalizedSummary,
                normalizedLocation,
                day,
                normalizedTags,
                incomingSummaryTokens));

        if (dedupe is not null)
            return dedupe.EventId;

        var ev = new TownMemoryEvent
        {
            EventId = $"{normalizedKind}:{day}:{Math.Abs((normalizedSummary + normalizedLocation).GetHashCode()) % 100000}",
            Kind = normalizedKind,
            SourceNpc = sourceNpc?.Trim() ?? string.Empty,
            Summary = normalizedSummary,
            Location = normalizedLocation,
            Day = day,
            Severity = Math.Clamp(severity, 1, 5),
            Visibility = string.IsNullOrWhiteSpace(visibility) ? "local" : visibility,
            Tags = normalizedTags,
            MentionBudget = 4
        };

        state.TownMemory.Events.Add(ev);
        PropagateEvent(state, ev, sourceNpc ?? string.Empty);

        if (state.TownMemory.Events.Count > 300)
            state.TownMemory.Events = state.TownMemory.Events.TakeLast(300).ToList();

        return ev.EventId;
    }

    public string SpreadRumor(
        SaveState state,
        string sourceNpc,
        string topic,
        string targetGroup,
        float confidence,
        int day,
        string location,
        string originKind = "npc",
        string directRecipientNpc = "")
    {
        if (string.IsNullOrWhiteSpace(topic))
            return string.Empty;

        var normalizedTopic = topic.Trim();
        var normalizedLocation = string.IsNullOrWhiteSpace(location) ? "Town" : location.Trim();
        var normalizedGroup = string.IsNullOrWhiteSpace(targetGroup) ? "town" : targetGroup.Trim();
        var normalizedOriginKind = string.IsNullOrWhiteSpace(originKind) ? "npc" : originKind.Trim().ToLowerInvariant();
        var normalizedSourceNpc = NormalizeNpcNameForKnowledge(sourceNpc);
        var normalizedRecipientNpc = NormalizeNpcNameForKnowledge(directRecipientNpc);
        var rumorTags = new[] { "rumor", "gossip", normalizedGroup }
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var incomingSummaryTokens = BuildSemanticTokenSet(normalizedTopic);

        var dedupe = state.TownMemory.Events.FirstOrDefault(e =>
            e is not null
            && string.Equals(e.Kind, "rumor", StringComparison.OrdinalIgnoreCase)
            && IsSemanticallyDuplicateEvent(
                e,
                "rumor",
                normalizedTopic,
                normalizedLocation,
                day,
                rumorTags,
                incomingSummaryTokens));

        if (dedupe is not null)
        {
            SeedRumorKnowledge(state, dedupe, day, normalizedSourceNpc, normalizedRecipientNpc);
            return dedupe.EventId;
        }

        var ev = new TownMemoryEvent
        {
            EventId = $"rumor:{day}:{Math.Abs((normalizedTopic + normalizedGroup + normalizedLocation).GetHashCode()) % 100000}",
            Kind = "rumor",
            SourceNpc = normalizedSourceNpc,
            OriginKind = normalizedOriginKind,
            Summary = normalizedTopic,
            Location = normalizedLocation,
            Day = day,
            Severity = confidence >= 0.80f ? 3 : 2,
            Visibility = "local",
            TargetGroup = normalizedGroup,
            Confidence = Math.Clamp(confidence, 0f, 1f),
            Tags = rumorTags,
            MentionBudget = 5
        };

        state.TownMemory.Events.Add(ev);
        SeedRumorKnowledge(state, ev, day, normalizedSourceNpc, normalizedRecipientNpc);

        if (state.TownMemory.Events.Count > 300)
            state.TownMemory.Events = state.TownMemory.Events.TakeLast(300).ToList();

        return ev.EventId;
    }

    public int SeedImmediateKnowledge(
        SaveState state,
        string eventId,
        IEnumerable<string> npcNames,
        int learnedDay,
        string angle = "family-circle")
    {
        if (string.IsNullOrWhiteSpace(eventId) || npcNames is null)
            return 0;

        var count = 0;
        foreach (var rawNpc in npcNames)
        {
            var npc = NormalizeNpcNameForKnowledge(rawNpc);
            if (string.IsNullOrWhiteSpace(npc))
                continue;

            if (SetKnowledgeEntry(
                    state,
                    npc,
                    eventId,
                    Math.Max(1, learnedDay),
                    string.IsNullOrWhiteSpace(angle) ? "family-circle" : angle.Trim()))
            {
                count += 1;
            }
        }

        return count;
    }

    public int PropagateTaggedEventsByDay(
        SaveState state,
        string tag,
        int day,
        int maxNewNpcsPerEvent = 4)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return 0;

        var normalizedTag = tag.Trim().ToLowerInvariant();
        if (maxNewNpcsPerEvent <= 0)
            maxNewNpcsPerEvent = 1;

        var events = state.TownMemory.Events
            .Where(ev =>
                ev is not null
                && ev.Day < day
                && day - ev.Day <= 21
                && ev.Tags.Any(t => string.Equals(t, normalizedTag, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(ev => ev.Day)
            .ThenBy(ev => ev.EventId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (events.Count == 0)
            return 0;

        var totalPromoted = 0;
        foreach (var ev in events)
        {
            var promotedForEvent = 0;
            var targets = BuildPropagationTargetNpcList(state, DefaultTownNpcRoster, ev.SourceNpc)
                .OrderBy(npc => npc, StringComparer.OrdinalIgnoreCase);

            foreach (var npc in targets)
            {
                if (promotedForEvent >= maxNewNpcsPerEvent)
                    break;

                if (SetKnowledgeEntry(state, npc, ev.EventId, day, "town-talk", ev.SourceNpc))
                {
                    promotedForEvent += 1;
                    totalPromoted += 1;
                }
            }
        }

        return totalPromoted;
    }

    public int TryShareRumorBetweenNpcs(
        SaveState state,
        string speakerNpc,
        string listenerNpc,
        int day,
        int maxRumors = 1)
    {
        var normalizedSpeaker = NormalizeNpcNameForKnowledge(speakerNpc);
        var normalizedListener = NormalizeNpcNameForKnowledge(listenerNpc);
        if (string.IsNullOrWhiteSpace(normalizedSpeaker)
            || string.IsNullOrWhiteSpace(normalizedListener)
            || string.Equals(normalizedSpeaker, normalizedListener, StringComparison.OrdinalIgnoreCase)
            || !state.TownMemory.KnowledgeByNpc.TryGetValue(normalizedSpeaker, out var speakerKnowledge))
        {
            return 0;
        }

        state.TownMemory.KnowledgeByNpc.TryGetValue(normalizedListener, out var listenerKnowledge);
        var shareCap = Math.Max(1, maxRumors);
        var shared = 0;

        foreach (var ev in state.TownMemory.Events
                     .Where(EventIsRumor)
                     .OrderByDescending(ev => ScoreRumorForSharing(ev, speakerKnowledge, day)))
        {
            if (shared >= shareCap)
                break;
            if (!speakerKnowledge.ByEventId.TryGetValue(ev.EventId, out var speakerEntry) || !speakerEntry.Knows)
                continue;
            if (listenerKnowledge is not null
                && listenerKnowledge.ByEventId.TryGetValue(ev.EventId, out var listenerEntry)
                && listenerEntry.Knows)
            {
                continue;
            }

            if (SetKnowledgeEntry(state, normalizedListener, ev.EventId, day, "heard-directly", normalizedSpeaker))
                shared += 1;
        }

        return shared;
    }

    public string BuildTownMemoryBlock(SaveState state, string npcName, string playerText, int day, int maxChars = 280)
    {
        if (!state.TownMemory.KnowledgeByNpc.TryGetValue(npcName, out var knowledge))
            return string.Empty;

        var tags = ExtractTags(playerText).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var currentTimeOfDay = Game1.timeOfDay;

        var candidates = new List<(TownMemoryEvent Ev, TownKnowledgeEntry K, int Score)>();
        foreach (var ev in state.TownMemory.Events)
        {
            if (!knowledge.ByEventId.TryGetValue(ev.EventId, out var k) || !k.Knows)
                continue;
            if (k.MentionCount >= ev.MentionBudget)
                continue;
            if (day - k.LastMentionDay < 2 && k.LastMentionDay > 0)
                continue;
            if (ev.Day > day + 2)
                continue;

            var dayDistance = Math.Abs(day - ev.Day);
            var recencyScore = Math.Max(0, 10 - dayDistance);
            var score = ev.Severity * 6 + recencyScore;
            if (string.Equals(ev.Kind, "pass_out", StringComparison.OrdinalIgnoreCase))
                score += 8;
            if (ev.Tags.Any(tag => string.Equals(tag, "player", StringComparison.OrdinalIgnoreCase)))
                score += 4;
            if (TownEventTemporalHelper.IsUpcoming(ev, day, currentTimeOfDay))
                score -= 2;
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

        var body = string.Join(" ", top.Select(t =>
        {
            var temporalLabel = TownEventTemporalHelper.BuildTemporalLabel(t.Ev, day, currentTimeOfDay);
            return $"[{temporalLabel}] {t.Ev.Summary} (known to {npcName}; {t.K.Angle}).";
        }));
        var block = $"TOWN_MEMORY_FOR_{npcName.ToUpperInvariant()}: {body}";
        return block.Length > maxChars ? block[..maxChars] : block;
    }

    public string BuildRumorBlock(SaveState state, string npcName, string playerText, int day, int maxChars = 240)
    {
        if (!state.TownMemory.KnowledgeByNpc.TryGetValue(npcName, out var knowledge))
            return string.Empty;

        var rumorAsked = IsRumorPromptLikely(playerText);
        var tags = ExtractTags(playerText).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<(TownMemoryEvent Ev, TownKnowledgeEntry Knowledge, int Score)>();

        foreach (var ev in state.TownMemory.Events.Where(EventIsRumor))
        {
            if (!knowledge.ByEventId.TryGetValue(ev.EventId, out var entry) || !entry.Knows)
                continue;
            if (day - ev.Day > 14)
                continue;

            var score = (int)Math.Round(ev.Confidence * 10f) + (ev.Severity * 4) - Math.Max(0, day - ev.Day);
            if (rumorAsked)
                score += 8;
            foreach (var tag in tags)
            {
                if (ev.Summary.Contains(tag, StringComparison.OrdinalIgnoreCase)
                    || ev.Tags.Any(existingTag => string.Equals(existingTag, tag, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 4;
                }
            }

            candidates.Add((ev, entry, score));
        }

        var limit = rumorAsked ? 2 : 1;
        var top = candidates
            .OrderByDescending(candidate => candidate.Score)
            .Take(limit)
            .ToList();
        if (top.Count == 0)
            return string.Empty;

        var body = string.Join(" ", top.Select(candidate =>
        {
            var learnedFrom = !string.IsNullOrWhiteSpace(candidate.Knowledge.LearnedFromNpc)
                ? candidate.Knowledge.LearnedFromNpc
                : !string.IsNullOrWhiteSpace(candidate.Ev.SourceNpc)
                    ? candidate.Ev.SourceNpc
                    : "town talk";
            var confidenceLabel = candidate.Ev.Confidence switch
            {
                >= 0.85f => "strong",
                >= 0.60f => "plausible",
                _ => "uncertain"
            };
            return $"[{confidenceLabel}; from {learnedFrom}] {candidate.Ev.Summary}";
        }));

        var block = $"KNOWN_RUMORS[{npcName}]: {body}.";
        return block.Length > maxChars ? block[..maxChars] : block;
    }

    public bool TryMarkRumorToastShown(SaveState state, string eventId, int day)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            return false;

        var ev = state.TownMemory.Events.FirstOrDefault(existing =>
            existing is not null
            && string.Equals(existing.EventId, eventId, StringComparison.OrdinalIgnoreCase)
            && EventIsRumor(existing));
        if (ev is null)
            return false;
        if (!string.Equals(ev.OriginKind, "npc", StringComparison.OrdinalIgnoreCase))
            return false;

        if (ev.ToastShownDay == day)
            return false;

        ev.ToastShownDay = day;
        return true;
    }

    public string DumpNpcTownMemory(SaveState state, string npcName)
    {
        if (!state.TownMemory.KnowledgeByNpc.TryGetValue(npcName, out var k))
            return $"Town memory {npcName}: none";

        var known = k.ByEventId.Values.Count(v => v.Knows);
        return $"Town memory {npcName}: knownEvents={known}";
    }

    private static void PropagateEvent(SaveState state, TownMemoryEvent ev, string sourceNpc)
    {
        var allTargets = BuildPropagationTargetNpcList(state, DefaultTownNpcRoster, sourceNpc);
        var normalizedSource = NormalizeNpcNameForKnowledge(sourceNpc);

        foreach (var npc in allTargets)
        {
            var isSourceNpc = !string.IsNullOrWhiteSpace(normalizedSource)
                              && string.Equals(npc, normalizedSource, StringComparison.OrdinalIgnoreCase);
            var knows = ev.Visibility.Equals("public", StringComparison.OrdinalIgnoreCase)
                || isSourceNpc
                || npc.Equals("Lewis", StringComparison.OrdinalIgnoreCase)
                || (ev.Tags.Contains("mines") && (npc.Equals("Robin", StringComparison.OrdinalIgnoreCase) || npc.Equals("Linus", StringComparison.OrdinalIgnoreCase)))
                || EventMentionsNpc(ev, npc)
                || ev.Severity >= 4;

            SetKnowledgeEntry(
                state,
                npc,
                ev.EventId,
                ev.Day,
                npc.Equals("Robin", StringComparison.OrdinalIgnoreCase) ? "concerned" : npc.Equals("Lewis", StringComparison.OrdinalIgnoreCase) ? "official" : "town-talk",
                normalizedSource,
                knows);
        }
    }

    private void SeedRumorKnowledge(
        SaveState state,
        TownMemoryEvent ev,
        int learnedDay,
        string sourceNpc,
        string directRecipientNpc)
    {
        if (!string.IsNullOrWhiteSpace(sourceNpc))
            SetKnowledgeEntry(state, sourceNpc, ev.EventId, learnedDay, "source", sourceNpc);

        if (!string.IsNullOrWhiteSpace(directRecipientNpc))
            SetKnowledgeEntry(state, directRecipientNpc, ev.EventId, learnedDay, "heard-directly", sourceNpc);

        if (!string.Equals(ev.OriginKind, "npc", StringComparison.OrdinalIgnoreCase))
            return;

        var seeded = 0;
        foreach (var npc in BuildRumorSeedNpcList(state, ev, sourceNpc, directRecipientNpc))
        {
            if (seeded >= 3)
                break;
            if (SetKnowledgeEntry(state, npc, ev.EventId, learnedDay, "town-talk", sourceNpc))
                seeded += 1;
        }
    }

    private static IEnumerable<string> BuildRumorSeedNpcList(
        SaveState state,
        TownMemoryEvent ev,
        string sourceNpc,
        string directRecipientNpc)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<string>();

        void AddCandidate(string? rawNpc)
        {
            var normalized = NormalizeNpcNameForKnowledge(rawNpc);
            if (string.IsNullOrWhiteSpace(normalized))
                return;
            if (string.Equals(normalized, sourceNpc, StringComparison.OrdinalIgnoreCase))
                return;
            if (string.Equals(normalized, directRecipientNpc, StringComparison.OrdinalIgnoreCase))
                return;
            if (seen.Add(normalized))
                candidates.Add(normalized);
        }
        foreach (var anchor in GetRumorTargetGroupAnchors(ev.TargetGroup))
            AddCandidate(anchor);

        if (!string.IsNullOrWhiteSpace(ev.Location))
        {
            foreach (var location in Game1.locations)
            {
                if (!string.Equals(location?.Name, ev.Location, StringComparison.OrdinalIgnoreCase)
                    || location?.characters is null)
                {
                    continue;
                }
                foreach (var character in location.characters)
                    AddCandidate(character?.Name);
            }
        }

        foreach (var npc in BuildPropagationTargetNpcList(state, DefaultTownNpcRoster, sourceNpc))
        {
            if (EventMentionsNpc(ev, npc))
                AddCandidate(npc);
        }

        return candidates;
    }

    private static IEnumerable<string> GetRumorTargetGroupAnchors(string? targetGroup)
    {
        var group = (targetGroup ?? string.Empty).Trim().ToLowerInvariant();
        return group switch
        {
            "shopkeepers_guild" => new[] { "Pierre", "Caroline", "Gus", "Robin", "Clint", "Morris" },
            "farmers_circle" => new[] { "Pierre", "Robin", "Marnie", "Lewis", "Caroline", "Jodi" },
            "adventurers_club" => new[] { "Marlon", "Gil", "Clint", "Linus", "Sebastian" },
            "nature_keepers" => new[] { "Linus", "Leah", "Demetrius", "Caroline", "Wizard" },
            "household" => new[] { "Robin", "Demetrius", "Sebastian", "Maru", "Jodi", "Kent", "Sam", "Vincent" },
            _ => Array.Empty<string>()
        };
    }

    private static bool SetKnowledgeEntry(
        SaveState state,
        string npc,
        string eventId,
        int learnedDay,
        string angle,
        string learnedFromNpc = "",
        bool knows = true)
    {
        if (string.IsNullOrWhiteSpace(npc) || string.IsNullOrWhiteSpace(eventId))
            return false;

        if (!state.TownMemory.KnowledgeByNpc.TryGetValue(npc, out var perNpc))
        {
            var existingKey = state.TownMemory.KnowledgeByNpc.Keys.FirstOrDefault(key =>
                string.Equals(key, npc, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(existingKey) && state.TownMemory.KnowledgeByNpc.TryGetValue(existingKey, out perNpc))
                npc = existingKey;
        }

        if (perNpc is null)
        {
            perNpc = new NpcTownKnowledge();
            state.TownMemory.KnowledgeByNpc[npc] = perNpc;
        }

        if (perNpc.ByEventId.TryGetValue(eventId, out var existing)
            && existing.Knows == knows
            && existing.LearnedDay <= learnedDay
            && string.Equals(existing.Angle, angle, StringComparison.OrdinalIgnoreCase)
            && string.Equals(existing.LearnedFromNpc ?? string.Empty, learnedFromNpc ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        perNpc.ByEventId[eventId] = new TownKnowledgeEntry
        {
            Knows = knows,
            LearnedDay = Math.Max(1, learnedDay),
            MentionCount = existing?.MentionCount ?? 0,
            LastMentionDay = existing?.LastMentionDay ?? 0,
            Angle = string.IsNullOrWhiteSpace(angle) ? "town-talk" : angle.Trim(),
            LearnedFromNpc = learnedFromNpc?.Trim() ?? string.Empty
        };
        return true;
    }

    private static bool EventIsRumor(TownMemoryEvent? ev)
    {
        return ev is not null
            && string.Equals(ev.Kind, "rumor", StringComparison.OrdinalIgnoreCase);
    }

    private static int ScoreRumorForSharing(TownMemoryEvent ev, NpcTownKnowledge speakerKnowledge, int day)
    {
        speakerKnowledge.ByEventId.TryGetValue(ev.EventId, out var entry);
        var agePenalty = Math.Max(0, day - ev.Day);
        var mentionPenalty = entry?.MentionCount ?? 0;
        return (int)Math.Round(ev.Confidence * 12f) + (ev.Severity * 3) - agePenalty - mentionPenalty;
    }

    private static bool IsRumorPromptLikely(string? playerText)
    {
        if (string.IsNullOrWhiteSpace(playerText))
            return false;

        var text = playerText.Trim().ToLowerInvariant();
        return text.Contains("rumor", StringComparison.Ordinal)
            || text.Contains("rumour", StringComparison.Ordinal)
            || text.Contains("gossip", StringComparison.Ordinal)
            || text.Contains("heard", StringComparison.Ordinal)
            || text.Contains("whisper", StringComparison.Ordinal)
            || text.Contains("talking about", StringComparison.Ordinal);
    }

    private static bool EventMentionsNpc(TownMemoryEvent ev, string npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return false;

        return ev.Summary.Contains(npcName, StringComparison.OrdinalIgnoreCase)
               || ev.Location.Contains(npcName, StringComparison.OrdinalIgnoreCase)
               || ev.Tags.Any(tag => tag.Contains(npcName, StringComparison.OrdinalIgnoreCase)
                                     || npcName.Contains(tag, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> BuildPropagationTargetNpcList(SaveState state, IEnumerable<string> baseline, string? sourceNpcName)
    {
        var merged = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var npc in baseline)
            AddNpcName(npc);

        AddNpcName(sourceNpcName);

        foreach (var npc in state.TownMemory.KnowledgeByNpc.Keys)
            AddNpcName(npc);

        foreach (var npc in state.Social.NpcReputation.Keys)
            AddNpcName(npc);

        foreach (var issuer in state.Quests.Active.Select(q => q.Issuer))
            AddNpcName(issuer);
        foreach (var issuer in state.Quests.Available.Select(q => q.Issuer))
            AddNpcName(issuer);
        foreach (var issuer in state.Quests.Completed.Select(q => q.Issuer))
            AddNpcName(issuer);
        foreach (var issuer in state.Quests.Failed.Select(q => q.Issuer))
            AddNpcName(issuer);

        try
        {
            foreach (var location in Game1.locations)
            {
                if (location?.characters is null)
                    continue;
                foreach (var character in location.characters)
                {
                    AddNpcName(character?.Name);
                    AddNpcName(character?.displayName);
                }
            }
        }
        catch
        {
            // Ignore dynamic world read failures and keep baseline fallback.
        }

        return merged;

        void AddNpcName(string? raw)
        {
            var normalized = NormalizeNpcNameForKnowledge(raw);
            if (string.IsNullOrWhiteSpace(normalized))
                return;
            if (seen.Add(normalized))
                merged.Add(normalized);
        }
    }

    private static string NormalizeNpcNameForKnowledge(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var value = raw.Trim();
        if (value.Length == 0)
            return string.Empty;

        // Ignore opaque transport/session ids; we only keep friendly NPC names in town memory.
        if (value.Length > 32 && value.Contains('-', StringComparison.Ordinal))
            return string.Empty;

        return value;
    }

    private static bool IsSemanticallyDuplicateEvent(
        TownMemoryEvent existing,
        string kind,
        string summary,
        string location,
        int day,
        string[] incomingTags,
        HashSet<string> incomingSummaryTokens)
    {
        if (!string.Equals(existing.Kind, kind, StringComparison.OrdinalIgnoreCase))
            return false;
        if (Math.Abs(existing.Day - day) > 1)
            return false;

        if (string.Equals(existing.Summary, summary, StringComparison.OrdinalIgnoreCase)
            && string.Equals(existing.Location, location, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var existingLocationTokens = BuildSemanticTokenSet(existing.Location);
        var incomingLocationTokens = BuildSemanticTokenSet(location);
        var locationSimilarity = ComputeJaccardSimilarity(existingLocationTokens, incomingLocationTokens);
        if (locationSimilarity < 0.5d)
            return false;

        var existingSummaryTokens = BuildSemanticTokenSet(existing.Summary);
        var summarySimilarity = ComputeJaccardSimilarity(existingSummaryTokens, incomingSummaryTokens);
        if (summarySimilarity >= 0.65d)
            return true;

        var existingTagSet = new HashSet<string>(
            existing.Tags?.Select(t => (t ?? string.Empty).Trim().ToLowerInvariant()) ?? Enumerable.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
        var incomingTagSet = new HashSet<string>(incomingTags, StringComparer.OrdinalIgnoreCase);
        var tagSimilarity = ComputeJaccardSimilarity(existingTagSet, incomingTagSet);

        return summarySimilarity >= 0.45d && tagSimilarity >= 0.5d;
    }

    private static HashSet<string> BuildSemanticTokenSet(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return value
            .ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '!', '?', ':', ';', '\'', '"', '\n', '\r', '\t', '-', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static double ComputeJaccardSimilarity(HashSet<string> left, HashSet<string> right)
    {
        if (left.Count == 0 || right.Count == 0)
            return 0d;

        var intersectionCount = left.Count(token => right.Contains(token));
        var unionCount = left.Count + right.Count - intersectionCount;
        if (unionCount <= 0)
            return 0d;

        return intersectionCount / (double)unionCount;
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
