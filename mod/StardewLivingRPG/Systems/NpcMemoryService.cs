using System.Text.RegularExpressions;
using StardewLivingRPG.State;
using StardewLivingRPG.Utils;

namespace StardewLivingRPG.Systems;

public sealed class NpcMemoryService
{
    private const int MaxFacts = 200;
    private const int MaxTurns = 40;
    private const int MaxImportantMemories = 64;

    private static readonly HashSet<string> ImportantCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "promise",
        "secret",
        "preference",
        "relationship"
    };

    private static readonly string[] SecretPhrases =
    {
        "secret",
        "between us",
        "don't tell",
        "dont tell",
        "keep this private",
        "keep it private",
        "confidential"
    };

    private static readonly string[] PromisePhrases =
    {
        "promise",
        "i will",
        "i'll",
        "ill",
        "count on me",
        "deal",
        "agreed",
        "swear",
        "i can do that"
    };

    private static readonly string[] PreferencePhrases =
    {
        "favorite",
        "favourite",
        "prefer",
        "like",
        "love",
        "hate",
        "dislike",
        "allergic"
    };

    public void WriteFact(
        SaveState state,
        string npcName,
        string category,
        string text,
        int day,
        int weight = 2,
        string visibility = "npc_only",
        string status = "active",
        string sourceRefKind = "chat_rule",
        string sourceRefId = "",
        string sourceExchangeId = "")
    {
        if (string.IsNullOrWhiteSpace(npcName) || string.IsNullOrWhiteSpace(text))
            return;

        var profile = GetProfile(state, npcName);
        var canonical = text.Trim();
        if (profile.Facts.Any(f => string.Equals(f.Text, canonical, StringComparison.OrdinalIgnoreCase)))
            return;

        var safeCategory = NormalizeCategory(category);
        profile.Facts.Add(new NpcMemoryFact
        {
            FactId = $"{npcName}:{safeCategory}:{day}:{Math.Abs(canonical.GetHashCode()) % 100000}",
            Category = safeCategory,
            Text = canonical,
            Day = day,
            Weight = Math.Clamp(weight, 1, 5),
            LastReferencedDay = day
        });

        if (ImportantCategories.Contains(safeCategory))
        {
            var summary = BuildImportantMemorySummary(npcName, safeCategory, canonical);
            UpsertImportantMemory(
                state,
                npcName,
                memoryId: string.IsNullOrWhiteSpace(sourceRefId)
                    ? $"{safeCategory}:{Math.Abs(summary.GetHashCode()) % 100000}"
                    : $"{safeCategory}:{sourceRefId}",
                category: safeCategory,
                summary: summary,
                importance: Math.Max(2, Math.Clamp(weight, 1, 5)),
                day: day,
                visibility: NormalizeVisibility(visibility, safeCategory),
                status: NormalizeStatus(status),
                sourceRefKind: string.IsNullOrWhiteSpace(sourceRefKind) ? "chat_rule" : sourceRefKind,
                sourceRefId: sourceRefId,
                sourceExchangeId: sourceExchangeId,
                evidenceSnippet: canonical,
                keywords: ExtractTags(canonical).ToArray());
        }

        profile.LastUpdatedDay = day;
        Prune(profile);
    }

    public void WriteTurn(SaveState state, string npcName, string playerText, string npcText, int day)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return;

        var safePlayerText = playerText?.Trim() ?? string.Empty;
        var safeNpcText = npcText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(safePlayerText) && string.IsNullOrWhiteSpace(safeNpcText))
            return;

        var profile = GetProfile(state, npcName);
        var playerTags = ExtractTags(safePlayerText).ToArray();
        NpcMemoryTurn? pairedTurn = null;

        if (!string.IsNullOrWhiteSpace(safeNpcText) && string.IsNullOrWhiteSpace(safePlayerText))
        {
            pairedTurn = profile.RecentTurns
                .LastOrDefault(turn => turn.Day == day
                    && string.IsNullOrWhiteSpace(turn.NpcText)
                    && !string.IsNullOrWhiteSpace(turn.PlayerText));
        }

        if (pairedTurn is not null)
        {
            pairedTurn.NpcText = safeNpcText;
            if (playerTags.Length > 0)
                pairedTurn.Tags = pairedTurn.Tags.Concat(playerTags).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
        else
        {
            profile.RecentTurns.Add(new NpcMemoryTurn
            {
                Day = day,
                PlayerText = safePlayerText,
                NpcText = safeNpcText,
                Tags = playerTags
            });
        }

        foreach (var tag in playerTags)
        {
            profile.TopicCounters.TryGetValue(tag, out var c);
            profile.TopicCounters[tag] = c + 1;
        }

        profile.LastUpdatedDay = day;
        Prune(profile);
    }

    public void PromoteImportantConversation(SaveState state, string npcName, TranscriptExchange exchange, int day)
    {
        if (exchange is null || string.IsNullOrWhiteSpace(npcName))
            return;

        var playerText = exchange.PlayerText ?? string.Empty;
        var npcText = exchange.NpcText ?? string.Empty;
        var combined = $"{playerText} {npcText}".Trim();
        if (string.IsNullOrWhiteSpace(combined))
            return;

        var category = DetectConversationCategory(combined);
        if (!ImportantCategories.Contains(category))
            return;

        var visibility = category.Equals("secret", StringComparison.OrdinalIgnoreCase) ? "private" : "npc_only";
        var status = category.Equals("promise", StringComparison.OrdinalIgnoreCase) ? "active" : "resolved";
        var summary = BuildConversationSummary(npcName, category, playerText, npcText);
        if (string.IsNullOrWhiteSpace(summary))
            return;

        UpsertImportantMemory(
            state,
            npcName,
            memoryId: $"{category}:{exchange.RequestToken}",
            category: category,
            summary: summary,
            importance: Math.Max(3, exchange.Importance),
            day: day,
            visibility: visibility,
            status: status,
            sourceRefKind: "chat",
            sourceRefId: exchange.RequestToken,
            sourceExchangeId: exchange.ExchangeId,
            evidenceSnippet: TrimForPrompt(combined, 120),
            keywords: ExtractTags(combined).ToArray());
    }

    public void SyncQuestLifecycleMemories(SaveState state)
    {
        foreach (var quest in state.Quests.Active.Where(q => !string.IsNullOrWhiteSpace(q.Issuer)))
            UpsertQuestPromise(state, quest, "active", state.Calendar.Day);

        foreach (var quest in state.Quests.Completed.Where(q => !string.IsNullOrWhiteSpace(q.Issuer)))
            UpsertQuestPromise(state, quest, "kept", state.Calendar.Day);

        foreach (var quest in state.Quests.Failed.Where(q => !string.IsNullOrWhiteSpace(q.Issuer)))
            UpsertQuestPromise(state, quest, "broken", state.Calendar.Day);
    }

    public string BuildImportantMemoryBlock(SaveState state, string npcName, string playerText, int day, int topK = 3, int charCap = 380)
    {
        var top = GetTopImportantMemories(state, npcName, playerText, day, topK);
        if (top.Count == 0)
            return string.Empty;

        foreach (var memory in top)
            memory.LastReferencedDay = day;

        var parts = top
            .Select(memory =>
            {
                var header = memory.Category.Equals("promise", StringComparison.OrdinalIgnoreCase)
                    ? $"Promise[{memory.Status}]"
                    : $"{UppercaseFirst(memory.Category)}[{memory.Status}]";
                return $"{header}: {memory.Summary}";
            })
            .ToList();

        return JoinWithinCap($"NPC_IMPORTANT_MEMORY[{npcName}]: ", parts, charCap);
    }

    public string BuildMemoryBlock(SaveState state, string npcName, string playerText, int day, int topK = 4, int charCap = 700)
    {
        var profile = GetProfile(state, npcName);
        var tags = ExtractTags(playerText).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var scoredTurns = profile.RecentTurns
            .Select((turn, index) => new { turn, index, score = ScoreTurn(turn, tags, day) })
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.turn.Day)
            .ThenByDescending(x => x.index)
            .Take(3)
            .Select(x => string.IsNullOrWhiteSpace(x.turn.NpcText)
                ? $"Recent: Player said '{TrimForPrompt(x.turn.PlayerText, 70)}'."
                : $"Recent: Player '{TrimForPrompt(x.turn.PlayerText, 50)}' / NPC '{TrimForPrompt(x.turn.NpcText, 50)}'.")
            .ToList();

        var scoredFacts = profile.Facts
            .Select(f => new { f, score = ScoreFact(f, tags, day) })
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.f.Day)
            .ThenByDescending(x => x.f.Weight)
            .Take(topK)
            .Select(x => $"Fact: {x.f.Text}")
            .ToList();

        var parts = scoredTurns.Concat(scoredFacts).ToList();
        if (parts.Count == 0)
            return string.Empty;

        return JoinWithinCap($"NPC_MEMORY[{npcName}]: ", parts, charCap);
    }

    public bool HasDurableHistory(SaveState state, string npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return false;

        if (!state.NpcMemory.Profiles.TryGetValue(npcName, out var profile) || profile is null)
            return false;

        return profile.ImportantMemories.Count > 0
            || profile.RecentTurns.Count > 0
            || profile.Facts.Count > 0;
    }

    public bool TryBuildGreetingCue(SaveState state, string npcName, int day, out string greeting)
    {
        greeting = string.Empty;
        var memory = GetTopImportantMemories(state, npcName, "remember", day, 1).FirstOrDefault();
        if (memory is null)
            return false;

        memory.LastReferencedDay = day;
        greeting = memory.Category switch
        {
            "promise" when memory.Status.Equals("active", StringComparison.OrdinalIgnoreCase) => "I haven't forgotten what we agreed to.",
            "promise" when memory.Status.Equals("broken", StringComparison.OrdinalIgnoreCase) => "What happened between us is still on my mind.",
            "secret" => "What you told me is still safe with me.",
            "preference" => "I still remember what matters to you.",
            "relationship" => "I've still been thinking about where things stand between us.",
            _ => EnsureSentenceTerminal(TrimForPrompt(memory.Summary, 90))
        };

        return !string.IsNullOrWhiteSpace(greeting);
    }

    public bool TryBuildGroundedReply(SaveState state, string npcName, int day, out string reply)
    {
        reply = string.Empty;
        var memory = GetTopImportantMemories(state, npcName, "remember last time", day, 1).FirstOrDefault();
        if (memory is null)
            return false;

        memory.LastReferencedDay = day;
        reply = memory.Category switch
        {
            "promise" when memory.Status.Equals("active", StringComparison.OrdinalIgnoreCase) => "I still remember what we agreed on.",
            "promise" when memory.Status.Equals("kept", StringComparison.OrdinalIgnoreCase) => "You kept your word, and I remember that.",
            "promise" when memory.Status.Equals("broken", StringComparison.OrdinalIgnoreCase) => "I haven't forgotten that promise fell through.",
            "secret" => "What you trusted me with is still between us.",
            _ => EnsureSentenceTerminal(TrimForPrompt(memory.Summary, 96))
        };

        return !string.IsNullOrWhiteSpace(reply);
    }

    public string DumpNpcMemory(SaveState state, string npcName)
    {
        var p = GetProfile(state, npcName);
        var facts = string.Join(" | ", p.Facts.Take(4).Select(f => f.Text));
        var important = string.Join(" | ", p.ImportantMemories.Take(3).Select(m => $"{m.Category}:{m.Status}:{m.Summary}"));
        return $"NPC memory {npcName}: important={p.ImportantMemories.Count}, facts={p.Facts.Count}, turns={p.RecentTurns.Count}, important_sample=[{important}], fact_sample=[{facts}]";
    }

    private void UpsertQuestPromise(SaveState state, QuestEntry quest, string status, int day)
    {
        if (string.IsNullOrWhiteSpace(quest.Issuer) || string.IsNullOrWhiteSpace(quest.QuestId))
            return;

        var summary = status switch
        {
            "kept" => $"Player kept the request '{QuestTextHelper.BuildQuestTitle(quest)}'.",
            "broken" => $"Player failed the request '{QuestTextHelper.BuildQuestTitle(quest)}'.",
            _ => $"Player agreed to help with '{QuestTextHelper.BuildQuestTitle(quest)}'."
        };

        UpsertImportantMemory(
            state,
            quest.Issuer,
            memoryId: $"quest:{quest.QuestId}",
            category: "promise",
            summary: summary,
            importance: 4,
            day: day,
            visibility: "npc_only",
            status: status,
            sourceRefKind: "quest",
            sourceRefId: quest.QuestId,
            sourceExchangeId: string.Empty,
            evidenceSnippet: quest.Summary,
            keywords: ExtractTags($"{quest.Summary} {quest.TargetItem} {quest.TemplateId}").ToArray());
    }

    public void UpsertImportantMemory(
        SaveState state,
        string npcName,
        string memoryId,
        string category,
        string summary,
        int importance,
        int day,
        string visibility,
        string status,
        string sourceRefKind,
        string sourceRefId,
        string sourceExchangeId,
        string evidenceSnippet,
        IEnumerable<string>? keywords = null)
    {
        if (string.IsNullOrWhiteSpace(npcName) || string.IsNullOrWhiteSpace(summary))
            return;

        var profile = GetProfile(state, npcName);
        var canonicalSummary = summary.Trim();
        var safeCategory = NormalizeCategory(category);
        var safeStatus = NormalizeStatus(status);
        var safeVisibility = NormalizeVisibility(visibility, safeCategory);
        var keywordArray = (keywords ?? ExtractTags(canonicalSummary))
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(18)
            .ToArray();

        var existing = profile.ImportantMemories.FirstOrDefault(memory =>
            (!string.IsNullOrWhiteSpace(memoryId) && memory.MemoryId.Equals(memoryId, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(sourceRefId)
                && memory.SourceRefKind.Equals(sourceRefKind, StringComparison.OrdinalIgnoreCase)
                && memory.SourceRefId.Equals(sourceRefId, StringComparison.OrdinalIgnoreCase))
            || (memory.Category.Equals(safeCategory, StringComparison.OrdinalIgnoreCase)
                && memory.Summary.Equals(canonicalSummary, StringComparison.OrdinalIgnoreCase)));

        if (existing is null)
        {
            existing = new ImportantMemoryEntry
            {
                MemoryId = string.IsNullOrWhiteSpace(memoryId)
                    ? $"{safeCategory}:{day}:{Math.Abs(canonicalSummary.GetHashCode()) % 100000}"
                    : memoryId,
                CreatedDay = day
            };
            profile.ImportantMemories.Add(existing);
        }

        existing.Category = safeCategory;
        existing.Summary = canonicalSummary;
        existing.Importance = Math.Clamp(Math.Max(existing.Importance, importance), 1, 5);
        existing.Visibility = safeVisibility;
        existing.Status = safeStatus;
        existing.SourceRefKind = string.IsNullOrWhiteSpace(sourceRefKind) ? "chat_rule" : sourceRefKind.Trim();
        existing.SourceRefId = sourceRefId?.Trim() ?? string.Empty;
        existing.SourceExchangeId = sourceExchangeId?.Trim() ?? string.Empty;
        existing.EvidenceSnippet = TrimForPrompt(evidenceSnippet, 120);
        existing.Keywords = keywordArray;
        existing.LastUpdatedDay = day;
        existing.LastReferencedDay = Math.Max(existing.LastReferencedDay, day);

        profile.LastUpdatedDay = day;
        Prune(profile);
    }

    private List<ImportantMemoryEntry> GetTopImportantMemories(SaveState state, string npcName, string playerText, int day, int topK)
    {
        var profile = GetProfile(state, npcName);
        var tags = ExtractTags(playerText).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var lowerPrompt = (playerText ?? string.Empty).Trim().ToLowerInvariant();

        return profile.ImportantMemories
            .Select(memory => new { memory, score = ScoreImportantMemory(memory, tags, lowerPrompt, day) })
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.memory.LastUpdatedDay)
            .ThenByDescending(x => x.memory.CreatedDay)
            .Take(topK)
            .Select(x => x.memory)
            .ToList();
    }

    private static int ScoreFact(NpcMemoryFact fact, HashSet<string> tags, int day)
    {
        var score = fact.Weight * 4;
        var age = Math.Max(0, day - fact.Day);
        score += Math.Max(0, 14 - age);

        foreach (var tag in tags)
        {
            if (fact.Text.Contains(tag, StringComparison.OrdinalIgnoreCase))
                score += 7;
        }

        return score;
    }

    private static int ScoreTurn(NpcMemoryTurn turn, HashSet<string> tags, int day)
    {
        var age = Math.Max(0, day - turn.Day);
        var score = Math.Max(0, 18 - age);
        if (!string.IsNullOrWhiteSpace(turn.PlayerText) && !string.IsNullOrWhiteSpace(turn.NpcText))
            score += 6;

        foreach (var tag in tags)
        {
            if (turn.PlayerText.Contains(tag, StringComparison.OrdinalIgnoreCase)
                || turn.NpcText.Contains(tag, StringComparison.OrdinalIgnoreCase))
            {
                score += 6;
            }
        }

        return score;
    }

    private static int ScoreImportantMemory(ImportantMemoryEntry memory, HashSet<string> tags, string promptLower, int day)
    {
        var age = Math.Max(0, day - Math.Max(memory.LastUpdatedDay, memory.CreatedDay));
        var score = memory.Importance * 10 + Math.Max(0, 28 - age);

        foreach (var tag in tags)
        {
            if (memory.Summary.Contains(tag, StringComparison.OrdinalIgnoreCase)
                || memory.Keywords.Any(keyword => keyword.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            {
                score += 9;
            }
        }

        if (promptLower.Contains("remember", StringComparison.Ordinal))
            score += 4;
        if (promptLower.Contains("promise", StringComparison.Ordinal) && memory.Category.Equals("promise", StringComparison.OrdinalIgnoreCase))
            score += 14;
        if ((promptLower.Contains("secret", StringComparison.Ordinal) || promptLower.Contains("between us", StringComparison.Ordinal))
            && memory.Category.Equals("secret", StringComparison.OrdinalIgnoreCase))
        {
            score += 16;
        }
        if ((promptLower.Contains("like", StringComparison.Ordinal) || promptLower.Contains("prefer", StringComparison.Ordinal))
            && memory.Category.Equals("preference", StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
        }
        if (memory.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            score += 4;
        if (memory.Visibility.Equals("private", StringComparison.OrdinalIgnoreCase))
            score += 2;

        return score;
    }

    private static string NormalizeCategory(string? category)
    {
        var value = (category ?? string.Empty).Trim().ToLowerInvariant();
        return value switch
        {
            "preference" or "pref" => "preference",
            "promise" => "promise",
            "secret" => "secret",
            "relationship" or "rel" => "relationship",
            _ => "event"
        };
    }

    private static string NormalizeVisibility(string? visibility, string category)
    {
        var value = (visibility ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(value))
            return category.Equals("secret", StringComparison.OrdinalIgnoreCase) ? "private" : "npc_only";

        return value switch
        {
            "private" => "private",
            "shareable" => "shareable",
            _ => "npc_only"
        };
    }

    private static string NormalizeStatus(string? status)
    {
        var value = (status ?? string.Empty).Trim().ToLowerInvariant();
        return value switch
        {
            "kept" => "kept",
            "broken" => "broken",
            "resolved" => "resolved",
            _ => "active"
        };
    }

    private static string DetectConversationCategory(string text)
    {
        if (ContainsAny(text, SecretPhrases))
            return "secret";
        if (ContainsAny(text, PromisePhrases))
            return "promise";
        if (ContainsAny(text, PreferencePhrases))
            return "preference";
        if (text.Contains("trust", StringComparison.OrdinalIgnoreCase)
            || text.Contains("upset", StringComparison.OrdinalIgnoreCase)
            || text.Contains("relationship", StringComparison.OrdinalIgnoreCase))
        {
            return "relationship";
        }

        return "event";
    }

    private static string BuildConversationSummary(string npcName, string category, string playerText, string npcText)
    {
        var primary = !string.IsNullOrWhiteSpace(playerText) ? playerText : npcText;
        if (string.IsNullOrWhiteSpace(primary))
            return string.Empty;

        var trimmed = TrimForPrompt(primary, 110);
        return category switch
        {
            "secret" => $"Shared a private confidence: {trimmed}",
            "promise" => $"A promise was made: {trimmed}",
            "preference" => $"Shared a personal preference: {trimmed}",
            "relationship" => $"A meaningful personal moment came up: {trimmed}",
            _ => trimmed
        };
    }

    private static string BuildImportantMemorySummary(string npcName, string category, string text)
    {
        return category switch
        {
            "secret" => $"Private confidence: {TrimForPrompt(text, 110)}",
            "promise" => $"Promise in conversation: {TrimForPrompt(text, 110)}",
            "preference" => $"Preference noted: {TrimForPrompt(text, 110)}",
            "relationship" => $"Relationship signal: {TrimForPrompt(text, 110)}",
            _ => TrimForPrompt(text, 110)
        };
    }

    private static string JoinWithinCap(string prefix, IEnumerable<string> parts, int charCap)
    {
        var safePrefix = prefix ?? string.Empty;
        var builder = new System.Text.StringBuilder(safePrefix);
        foreach (var part in parts.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            var separator = builder.Length == safePrefix.Length ? string.Empty : " ";
            if (builder.Length + separator.Length + part.Length > charCap)
                break;

            builder.Append(separator);
            builder.Append(part);
        }

        return builder.Length == safePrefix.Length ? string.Empty : builder.ToString();
    }

    private static string TrimForPrompt(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var t = Regex.Replace(text.Trim(), @"\s+", " ", RegexOptions.CultureInvariant);
        return t.Length > max ? t[..max] + "..." : t;
    }

    private static string EnsureSentenceTerminal(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.EndsWith(".", StringComparison.Ordinal)
            || text.EndsWith("!", StringComparison.Ordinal)
            || text.EndsWith("?", StringComparison.Ordinal)
            ? text
            : text + ".";
    }

    private static string UppercaseFirst(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    private static bool ContainsAny(string text, IEnumerable<string> phrases)
    {
        foreach (var phrase in phrases)
        {
            if (!string.IsNullOrWhiteSpace(phrase) && text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static IEnumerable<string> ExtractTags(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        foreach (Match match in Regex.Matches(text.ToLowerInvariant(), @"[\p{L}\p{N}][\p{L}\p{N}_'-]{2,}", RegexOptions.CultureInvariant))
        {
            var token = match.Value.Trim('\'', '"', '.', ',', '!', '?', ';', ':');
            if (token.Length >= 3)
                yield return token;
        }
    }

    private static NpcMemoryProfile GetProfile(SaveState state, string npcName)
    {
        if (!state.NpcMemory.Profiles.TryGetValue(npcName, out var profile))
        {
            var existingKey = state.NpcMemory.Profiles.Keys.FirstOrDefault(key =>
                string.Equals(key, npcName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(existingKey) && state.NpcMemory.Profiles.TryGetValue(existingKey, out profile))
                return profile;

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

        if (profile.ImportantMemories.Count > MaxImportantMemories)
        {
            profile.ImportantMemories = profile.ImportantMemories
                .OrderByDescending(memory => memory.Importance)
                .ThenByDescending(memory => memory.LastUpdatedDay)
                .ThenByDescending(memory => memory.LastReferencedDay)
                .Take(MaxImportantMemories)
                .ToList();
        }

        if (profile.RecentTurns.Count > MaxTurns)
            profile.RecentTurns = profile.RecentTurns.TakeLast(MaxTurns).ToList();
    }
}
