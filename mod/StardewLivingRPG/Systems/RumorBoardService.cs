using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class RumorBoardService
{
    public void RefreshDailyRumors(SaveState state)
    {
        // Keep active quests untouched; rotate available list daily.
        state.Quests.Available.Clear();

        var crop = state.Economy.Crops
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .ThenBy(kv => kv.Value.SupplyPressureFactor)
            .Select(kv => kv.Key)
            .FirstOrDefault() ?? "parsnip";

        state.Quests.Available.Add(new QuestEntry
        {
            QuestId = $"quest_gather_{crop}_{state.Calendar.Day}",
            TemplateId = "gather_crop",
            Status = "available",
            Source = "rumor_mill",
            Issuer = "lewis",
            ExpiresDay = state.Calendar.Day + 3,
            Summary = $"Rumor Mill: Supply {crop} x20 to stabilize town demand.",
            TargetItem = crop,
            TargetCount = 20,
            RewardGold = 500
        });

        state.Quests.Available.Add(new QuestEntry
        {
            QuestId = $"quest_social_{state.Calendar.Day}",
            TemplateId = "social_visit",
            Status = "available",
            Source = "rumor_mill",
            Issuer = "haley",
            ExpiresDay = state.Calendar.Day + 2,
            Summary = "Rumor Mill: Check in with a town resident and bring a thoughtful gift.",
            TargetItem = "gift",
            TargetCount = 1,
            RewardGold = 250
        });
    }

    public void ExpireOverdueQuests(SaveState state)
    {
        var overdue = state.Quests.Active
            .Where(q => q.ExpiresDay > 0 && state.Calendar.Day > q.ExpiresDay)
            .ToList();

        foreach (var quest in overdue)
        {
            state.Quests.Active.Remove(quest);
            quest.Status = "failed";
            state.Quests.Failed.Add(quest);

            state.Facts.Facts[$"quest:{quest.QuestId}:failed"] = new FactValue
            {
                Value = true,
                SetDay = state.Calendar.Day,
                Source = "system"
            };

            // Small non-punitive impact for missed commitments.
            state.Social.TownSentiment.Community = Math.Clamp(state.Social.TownSentiment.Community - 1, -100, 100);
            if (!string.IsNullOrWhiteSpace(quest.Issuer))
            {
                state.Social.NpcReputation.TryGetValue(quest.Issuer, out var rep);
                state.Social.NpcReputation[quest.Issuer] = Math.Clamp(rep - 1, -100, 100);
            }

            state.Telemetry.Daily.WorldMutations += 1;
        }
    }

    public bool AcceptQuest(SaveState state, string questId)
    {
        var quest = state.Quests.Available.FirstOrDefault(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase));
        if (quest is null)
            return false;

        state.Quests.Available.Remove(quest);
        quest.Status = "active";
        state.Quests.Active.Add(quest);

        state.Facts.Facts[$"quest:{quest.QuestId}:accepted"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "system"
        };

        state.Telemetry.Daily.RumorBoardAccepts += 1;
        return true;
    }

    public bool CompleteQuest(SaveState state, string questId)
    {
        var quest = state.Quests.Active.FirstOrDefault(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase));
        if (quest is null)
            return false;

        state.Quests.Active.Remove(quest);
        quest.Status = "completed";
        state.Quests.Completed.Add(quest);

        state.Facts.Facts[$"quest:{quest.QuestId}:completed"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "system"
        };

        ApplyRewards(state, quest);
        state.Telemetry.Daily.RumorBoardCompletions += 1;
        return true;
    }

    public string? CreateQuestFromNpcProposal(
        SaveState state,
        string npcId,
        string templateId,
        string target,
        string urgency,
        string intentKey)
    {
        if (state.Facts.ProcessedIntents.ContainsKey(intentKey))
            return null;

        var safeTemplate = NormalizeTemplate(templateId);
        var safeTarget = string.IsNullOrWhiteSpace(target) ? "parsnip" : target.Trim().ToLowerInvariant();
        var safeUrgency = NormalizeUrgency(urgency);

        var (count, rewardGold, expiresDelta) = safeUrgency switch
        {
            "high" => (25, 700, 2),
            "medium" => (20, 500, 3),
            _ => (14, 350, 4)
        };

        var suffix = Math.Abs(intentKey.GetHashCode()) % 100000;
        var questId = $"quest_ai_{safeTemplate}_{safeTarget}_{state.Calendar.Day}_{suffix}";

        if (state.Quests.Available.Any(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase)) ||
            state.Quests.Active.Any(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase)) ||
            state.Quests.Completed.Any(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase)))
            return null;

        var quest = new QuestEntry
        {
            QuestId = questId,
            TemplateId = safeTemplate,
            Status = "available",
            Source = "npc_intent",
            Issuer = "lewis",
            ExpiresDay = state.Calendar.Day + expiresDelta,
            Summary = $"Mayor request ({safeUrgency}): {BuildSummary(safeTemplate, safeTarget, count)}",
            TargetItem = safeTarget,
            TargetCount = count,
            RewardGold = rewardGold
        };

        state.Quests.Available.Add(quest);
        state.Facts.ProcessedIntents[intentKey] = new ProcessedIntentValue
        {
            Day = state.Calendar.Day,
            NpcId = npcId,
            Command = "propose_quest",
            Status = "applied"
        };

        state.Facts.Facts[$"quest:{questId}:proposed"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "npc_command"
        };

        state.Telemetry.Daily.WorldMutations += 1;
        return questId;
    }

    private static string NormalizeTemplate(string templateId)
    {
        var t = (templateId ?? string.Empty).Trim().ToLowerInvariant();
        return t switch
        {
            "gather_crop" => "gather_crop",
            "deliver_item" => "deliver_item",
            "mine_resource" => "mine_resource",
            "social_visit" => "social_visit",
            _ => "gather_crop"
        };
    }

    private static string NormalizeUrgency(string urgency)
    {
        var u = (urgency ?? string.Empty).Trim().ToLowerInvariant();
        return u switch
        {
            "high" => "high",
            "medium" => "medium",
            _ => "low"
        };
    }

    private static string BuildSummary(string templateId, string target, int count)
    {
        return templateId switch
        {
            "deliver_item" => $"Deliver {target} x{count} to support current demand.",
            "mine_resource" => $"Gather {target} x{count} from the mines.",
            "social_visit" => $"Visit a resident and bring {target} x{count}.",
            _ => $"Supply {target} x{count} for the town market."
        };
    }

    private static void ApplyRewards(SaveState state, QuestEntry quest)
    {
        // Minimal reward model for now: gold -> positive economy sentiment proxy + issuer rep.
        // (Actual wallet integration comes later.)
        var sentimentBoost = Math.Clamp(quest.RewardGold / 250, 1, 5);
        state.Social.TownSentiment.Economy = Math.Clamp(state.Social.TownSentiment.Economy + sentimentBoost, -100, 100);

        if (!string.IsNullOrWhiteSpace(quest.Issuer))
        {
            state.Social.NpcReputation.TryGetValue(quest.Issuer, out var rep);
            state.Social.NpcReputation[quest.Issuer] = Math.Clamp(rep + 3, -100, 100);
        }

        state.Telemetry.Daily.WorldMutations += 1;
    }
}
