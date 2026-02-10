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
