using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class AnchorEventService
{
    private const string TownHallFactKey = "anchor:town_hall_crisis:seen";

    public bool TryTriggerEmergencyTownHall(SaveState state, out string note)
    {
        note = string.Empty;

        if (state.Calendar.Day < 7)
            return false;

        if (state.Social.TownSentiment.Economy > -30)
            return false;

        if (state.Facts.Facts.ContainsKey(TownHallFactKey))
            return false;

        state.Facts.Facts[TownHallFactKey] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "anchor_event"
        };

        state.Telemetry.Daily.AnchorEventsTriggered += 1;

        // Spawn one stabilizer-style rumor quest as immediate gameplay follow-up.
        state.Quests.Available.Add(new QuestEntry
        {
            QuestId = $"quest_anchor_stabilizer_{state.Calendar.Day}",
            TemplateId = "community_drive",
            Status = "available",
            Source = "anchor_event",
            Issuer = "lewis",
            ExpiresDay = state.Calendar.Day + 4,
            Summary = "Emergency Town Hall: Deliver mixed supplies to stabilize the local market.",
            TargetItem = "mixed_bundle",
            TargetCount = 1,
            RewardGold = 800
        });

        note = "Town Hall Emergency: Mayor Lewis convened a market crisis meeting after sustained instability.";
        return true;
    }
}
