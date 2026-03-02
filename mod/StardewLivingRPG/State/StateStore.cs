using StardewModdingAPI;

namespace StardewLivingRPG.State;

public static class StateStore
{
    private const string DataKey = "mx146323.StardewLivingRPG.SaveState";
    private const string LegacyDataKey = "mx146323.StardewLivingRPG/SaveState";
    private const string CurrentStateVersion = "0.3.0";
    private static readonly HashSet<string> ValidSeasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "spring", "summer", "fall", "winter"
    };

    public static SaveState LoadOrCreate(IModHelper helper, IMonitor monitor)
    {
        try
        {
            var state = helper.Data.ReadSaveData<SaveState>(DataKey);
            if (state is not null)
                return NormalizeAndMigrate(state, monitor);

            // Legacy migration fallback (older invalid key format with slash).
            try
            {
                state = helper.Data.ReadSaveData<SaveState>(LegacyDataKey);
                if (state is not null)
                {
                    monitor.Log("Loaded save state from legacy key; writing future saves to the new valid key format.", LogLevel.Info);
                    return NormalizeAndMigrate(state, monitor);
                }
            }
            catch
            {
                // Ignore invalid legacy key format on newer SMAPI validation.
            }

            return NormalizeAndMigrate(SaveState.CreateDefault(), monitor);
        }
        catch (Exception ex)
        {
            monitor.Log($"Failed to load save state, using defaults: {ex.Message}", LogLevel.Warn);
            return NormalizeAndMigrate(SaveState.CreateDefault(), monitor);
        }
    }

    public static void Save(IModHelper helper, SaveState state, IMonitor monitor)
    {
        try
        {
            helper.Data.WriteSaveData(DataKey, state);
        }
        catch (Exception ex)
        {
            monitor.Log($"Failed to save state: {ex.Message}", LogLevel.Error);
        }
    }

    private static SaveState NormalizeAndMigrate(SaveState state, IMonitor monitor)
    {
        state ??= SaveState.CreateDefault();
        var changed = false;
        var priorVersion = string.IsNullOrWhiteSpace(state.Version) ? "0.0.0" : state.Version.Trim();

        if (state.Config is null)
        {
            state.Config = new SaveConfig();
            changed = true;
        }

        if (state.Calendar is null)
        {
            state.Calendar = new CalendarState();
            changed = true;
        }
        if (state.Calendar.Day < 1)
        {
            state.Calendar.Day = 1;
            changed = true;
        }
        if (state.Calendar.Year < 1)
        {
            state.Calendar.Year = 1;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(state.Calendar.Season) || !ValidSeasons.Contains(state.Calendar.Season))
        {
            state.Calendar.Season = "spring";
            changed = true;
        }

        if (state.Economy is null)
        {
            state.Economy = new EconomyState();
            changed = true;
        }
        if (state.Economy.Crops is null)
        {
            state.Economy.Crops = new Dictionary<string, CropEconomyEntry>();
            changed = true;
        }
        if (state.Economy.MarketEvents is null)
        {
            state.Economy.MarketEvents = new List<MarketEventEntry>();
            changed = true;
        }

        if (state.Social is null)
        {
            state.Social = new SocialState();
            changed = true;
        }
        if (state.Social.Interests is null)
        {
            state.Social.Interests = new Dictionary<string, InterestState>();
            changed = true;
        }
        if (state.Social.NpcReputation is null)
        {
            state.Social.NpcReputation = new Dictionary<string, int>();
            changed = true;
        }
        if (state.Social.NpcRelationships is null)
        {
            state.Social.NpcRelationships = new Dictionary<string, RelationshipState>();
            changed = true;
        }
        if (state.Social.TownSentiment is null)
        {
            state.Social.TownSentiment = new TownSentimentState();
            changed = true;
        }

        if (state.Quests is null)
        {
            state.Quests = new QuestState();
            changed = true;
        }
        if (state.Quests.Available is null)
        {
            state.Quests.Available = new List<QuestEntry>();
            changed = true;
        }
        if (state.Quests.Active is null)
        {
            state.Quests.Active = new List<QuestEntry>();
            changed = true;
        }
        if (state.Quests.Completed is null)
        {
            state.Quests.Completed = new List<QuestEntry>();
            changed = true;
        }
        if (state.Quests.Failed is null)
        {
            state.Quests.Failed = new List<QuestEntry>();
            changed = true;
        }

        if (state.Facts is null)
        {
            state.Facts = new FactTableState();
            changed = true;
        }
        if (state.Facts.Facts is null)
        {
            state.Facts.Facts = new Dictionary<string, FactValue>();
            changed = true;
        }
        if (state.Facts.ProcessedIntents is null)
        {
            state.Facts.ProcessedIntents = new Dictionary<string, ProcessedIntentValue>();
            changed = true;
        }

        if (state.Newspaper is null)
        {
            state.Newspaper = new NewspaperState();
            changed = true;
        }
        if (state.Newspaper.Issues is null)
        {
            state.Newspaper.Issues = new List<NewspaperIssue>();
            changed = true;
        }
        if (state.Newspaper.Articles is null)
        {
            state.Newspaper.Articles = new List<NewspaperArticle>();
            changed = true;
        }

        if (state.Telemetry is null)
        {
            state.Telemetry = new TelemetryState();
            changed = true;
        }
        if (state.Telemetry.Daily is null)
        {
            state.Telemetry.Daily = new DailyTelemetry();
            changed = true;
        }
        if (state.Telemetry.Daily.NpcCommandAppliedByType is null)
        {
            state.Telemetry.Daily.NpcCommandAppliedByType = new Dictionary<string, int>();
            changed = true;
        }
        if (state.Telemetry.Daily.NpcPolicyRejectByReason is null)
        {
            state.Telemetry.Daily.NpcPolicyRejectByReason = new Dictionary<string, int>();
            changed = true;
        }
        if (state.Telemetry.Daily.AmbientCommandAppliedByType is null)
        {
            state.Telemetry.Daily.AmbientCommandAppliedByType = new Dictionary<string, int>();
            changed = true;
        }
        if (state.Telemetry.Daily.AmbientCommandRejectedByType is null)
        {
            state.Telemetry.Daily.AmbientCommandRejectedByType = new Dictionary<string, int>();
            changed = true;
        }
        if (state.Telemetry.Daily.AmbientCommandDuplicateByType is null)
        {
            state.Telemetry.Daily.AmbientCommandDuplicateByType = new Dictionary<string, int>();
            changed = true;
        }
        if (state.Telemetry.Daily.RomanceAxisUpdatesByType is null)
        {
            state.Telemetry.Daily.RomanceAxisUpdatesByType = new Dictionary<string, int>();
            changed = true;
        }
        if (state.Telemetry.Daily.RomanceRejectByReason is null)
        {
            state.Telemetry.Daily.RomanceRejectByReason = new Dictionary<string, int>();
            changed = true;
        }

        if (state.NpcMemory is null)
        {
            state.NpcMemory = new NpcMemoryState();
            changed = true;
        }
        if (state.NpcMemory.Profiles is null)
        {
            state.NpcMemory.Profiles = new Dictionary<string, NpcMemoryProfile>(StringComparer.OrdinalIgnoreCase);
            changed = true;
        }
        foreach (var profile in state.NpcMemory.Profiles.Values)
        {
            if (profile is null)
                continue;
            if (profile.Facts is null)
            {
                profile.Facts = new List<NpcMemoryFact>();
                changed = true;
            }
            if (profile.RecentTurns is null)
            {
                profile.RecentTurns = new List<NpcMemoryTurn>();
                changed = true;
            }
            if (profile.TopicCounters is null)
            {
                profile.TopicCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                changed = true;
            }
        }

        if (state.TownMemory is null)
        {
            state.TownMemory = new TownMemoryState();
            changed = true;
        }
        if (state.TownMemory.Events is null)
        {
            state.TownMemory.Events = new List<TownMemoryEvent>();
            changed = true;
        }
        if (state.TownMemory.KnowledgeByNpc is null)
        {
            state.TownMemory.KnowledgeByNpc = new Dictionary<string, NpcTownKnowledge>(StringComparer.OrdinalIgnoreCase);
            changed = true;
        }
        foreach (var ev in state.TownMemory.Events)
        {
            if (ev is null)
                continue;
            if (ev.Tags is null)
            {
                ev.Tags = Array.Empty<string>();
                changed = true;
            }
        }
        foreach (var knowledge in state.TownMemory.KnowledgeByNpc.Values)
        {
            if (knowledge is null)
                continue;
            if (knowledge.ByEventId is null)
            {
                knowledge.ByEventId = new Dictionary<string, TownKnowledgeEntry>(StringComparer.OrdinalIgnoreCase);
                changed = true;
            }
        }

        if (state.Romance is null)
        {
            state.Romance = new RomanceState();
            changed = true;
        }
        if (state.Romance.Profiles is null)
        {
            state.Romance.Profiles = new Dictionary<string, LoveLanguageProfile>(StringComparer.OrdinalIgnoreCase);
            changed = true;
        }
        if (state.Romance.ActiveMicroDates is null)
        {
            state.Romance.ActiveMicroDates = new Dictionary<string, MicroDateState>(StringComparer.OrdinalIgnoreCase);
            changed = true;
        }
        foreach (var profile in state.Romance.Profiles.Values)
        {
            if (profile is null)
                continue;
            if (profile.Axes is null)
            {
                profile.Axes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                changed = true;
            }
            if (profile.RecentSignals is null)
            {
                profile.RecentSignals = new List<RomanceSignalEntry>();
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(profile.NextBeat))
            {
                profile.NextBeat = "warmth";
                changed = true;
            }
        }
        foreach (var key in state.Romance.ActiveMicroDates.Keys.ToArray())
        {
            var microDate = state.Romance.ActiveMicroDates[key];
            if (microDate is null)
            {
                state.Romance.ActiveMicroDates.Remove(key);
                changed = true;
                continue;
            }
            if (string.IsNullOrWhiteSpace(microDate.Status))
            {
                microDate.Status = "active";
                changed = true;
            }
            if (microDate.ExpiresDay < microDate.IssuedDay)
            {
                microDate.ExpiresDay = microDate.IssuedDay;
                changed = true;
            }
        }

        if (state.PlayerFamily is null)
        {
            state.PlayerFamily = new PlayerFamilyState();
            changed = true;
        }
        if (state.PlayerFamily.Children is null)
        {
            state.PlayerFamily.Children = new List<PlayerChildProfile>();
            changed = true;
        }
        for (var i = state.PlayerFamily.Children.Count - 1; i >= 0; i--)
        {
            var child = state.PlayerFamily.Children[i];
            if (child is null || string.IsNullOrWhiteSpace(child.Name))
            {
                state.PlayerFamily.Children.RemoveAt(i);
                changed = true;
                continue;
            }

            child.Name = child.Name.Trim();
            if (string.IsNullOrWhiteSpace(child.AgeStage))
            {
                child.AgeStage = "infant";
                changed = true;
            }
            else
            {
                var normalizedStage = child.AgeStage.Trim().ToLowerInvariant();
                if (normalizedStage is not ("infant" or "toddler" or "child"))
                {
                    child.AgeStage = "child";
                    changed = true;
                }
                else if (!string.Equals(child.AgeStage, normalizedStage, StringComparison.Ordinal))
                {
                    child.AgeStage = normalizedStage;
                    changed = true;
                }
            }

            if (child.FirstObservedDay <= 0)
            {
                child.FirstObservedDay = Math.Max(1, state.Calendar.Day);
                changed = true;
            }
        }

        var hasSpouse = !string.IsNullOrWhiteSpace(state.PlayerFamily.SpouseNpcId)
            || !string.IsNullOrWhiteSpace(state.PlayerFamily.SpouseName);
        if (state.PlayerFamily.IsMarried != hasSpouse)
        {
            state.PlayerFamily.IsMarried = hasSpouse;
            changed = true;
        }

        var hasChildren = state.PlayerFamily.Children.Count > 0;
        if (state.PlayerFamily.IsParent != hasChildren)
        {
            state.PlayerFamily.IsParent = hasChildren;
            changed = true;
        }

        if (state.PlayerFamily.FactVersion < 1)
        {
            state.PlayerFamily.FactVersion = 1;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(state.Config.Mode))
        {
            state.Config.Mode = "cozy_canon";
            changed = true;
        }

        if (IsOlderThan(state.Version, CurrentStateVersion))
        {
            state.Version = CurrentStateVersion;
            changed = true;
        }

        if (changed)
        {
            monitor.Log(
                $"State compatibility normalization applied (version {priorVersion} -> {state.Version}).",
                LogLevel.Info);
        }

        return state;
    }

    private static bool IsOlderThan(string? currentVersion, string targetVersion)
    {
        if (!Version.TryParse(currentVersion, out var current))
            return true;
        if (!Version.TryParse(targetVersion, out var target))
            return false;

        return current.CompareTo(target) < 0;
    }
}



