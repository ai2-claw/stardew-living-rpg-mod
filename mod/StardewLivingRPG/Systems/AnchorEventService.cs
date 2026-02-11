using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class AnchorEventService
{
    private const string TownHallFactKey = "anchor:town_hall_crisis:seen";
    private const string TownHallTriggeredDayKey = "anchor:town_hall_crisis:triggered_day";
    private const string TownHallCooldownKey = "anchor:town_hall_crisis:cooldown_until_day";
    private const string TownHallStatusTriggered = "anchor:town_hall_crisis:status:triggered";
    private const string TownHallStatusResolved = "anchor:town_hall_crisis:status:resolved";

    public bool TryTriggerEmergencyTownHall(SaveState state, out string note)
    {
        note = string.Empty;

        if (state.Calendar.Day < 7)
            return false;

        if (state.Social.TownSentiment.Economy > -30)
            return false;

        if (state.Facts.Facts.ContainsKey(TownHallFactKey))
            return false;

        if (TryReadIntFact(state, TownHallCooldownKey, out var cooldownUntilDay) && state.Calendar.Day <= cooldownUntilDay)
            return false;

        state.Facts.Facts[TownHallFactKey] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "anchor_event"
        };

        WriteIntFact(state, TownHallTriggeredDayKey, state.Calendar.Day);
        WriteIntFact(state, TownHallCooldownKey, state.Calendar.Day + 7);
        state.Facts.Facts[TownHallStatusTriggered] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "anchor_event"
        };

        state.Telemetry.Daily.AnchorEventsTriggered += 1;

        var questId = $"quest_anchor_stabilizer_{state.Calendar.Day}";
        if (!state.Quests.Available.Any(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase))
            && !state.Quests.Active.Any(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase))
            && !state.Quests.Completed.Any(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase)))
        {
            state.Quests.Available.Add(new QuestEntry
            {
                QuestId = questId,
                TemplateId = "deliver_item",
                Status = "available",
                Source = "anchor_event",
                Issuer = "lewis",
                ExpiresDay = state.Calendar.Day + 4,
                Summary = "Emergency Town Hall: Deliver mixed supplies to stabilize the local market.",
                TargetItem = "wheat",
                TargetCount = 10,
                RewardGold = 800
            });
        }

        // Visible next-day market impact marker.
        state.Economy.MarketEvents.Add(new MarketEventEntry
        {
            Id = $"anchor_evt_{state.Calendar.Day}",
            Type = "anchor_recovery_push",
            Crop = "wheat",
            DeltaPct = 0.08f,
            StartDay = state.Calendar.Day + 1,
            EndDay = state.Calendar.Day + 2
        });

        note = "Town Hall Emergency: Mayor Lewis convened a market crisis meeting after sustained instability. Recovery measures begin tomorrow.";
        return true;
    }

    public void TryResolveEmergencyTownHall(SaveState state)
    {
        if (!state.Facts.Facts.ContainsKey(TownHallFactKey))
            return;

        if (state.Facts.Facts.ContainsKey(TownHallStatusResolved))
            return;

        var resolved = state.Quests.Completed.Any(q => q.Source == "anchor_event")
            || state.Social.TownSentiment.Economy >= -10;

        if (!resolved)
            return;

        state.Facts.Facts[TownHallStatusResolved] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "anchor_event"
        };
    }

    private static bool TryReadIntFact(SaveState state, string key, out int value)
    {
        value = 0;
        if (!state.Facts.Facts.TryGetValue(key, out var fact))
            return false;

        if (!int.TryParse(fact.Source ?? string.Empty, out value))
            return false;

        return true;
    }

    private static void WriteIntFact(SaveState state, string key, int value)
    {
        state.Facts.Facts[key] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = value.ToString()
        };
    }
}
