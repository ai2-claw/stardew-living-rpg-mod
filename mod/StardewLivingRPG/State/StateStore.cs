using StardewModdingAPI;

namespace StardewLivingRPG.State;

public static class StateStore
{
    private const string DataKey = "mx146323.StardewLivingRPG.SaveState";
    private const string LegacyDataKey = "mx146323.StardewLivingRPG/SaveState";
    private const string CurrentStateVersion = "0.5.0";
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
        if (state.Social.PairEmotions is null)
        {
            state.Social.PairEmotions = new Dictionary<string, NpcPairEmotionEntry>(StringComparer.OrdinalIgnoreCase);
            changed = true;
        }
        foreach (var key in state.Social.PairEmotions.Keys.ToArray())
        {
            var entry = state.Social.PairEmotions[key];
            if (entry is null)
            {
                state.Social.PairEmotions.Remove(key);
                changed = true;
                continue;
            }
            if (entry.EmotionAxes is null)
            {
                entry.EmotionAxes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                changed = true;
            }
            if (entry.ActiveFlags is null)
            {
                entry.ActiveFlags = new List<string>();
                changed = true;
            }
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
        foreach (var issue in state.Newspaper.Issues)
        {
            if (issue.Sections is null)
            {
                issue.Sections = new List<string>();
                changed = true;
            }
            if (issue.PredictiveHints is null)
            {
                issue.PredictiveHints = new List<string>();
                changed = true;
            }
            if (issue.MarketSections is null)
            {
                issue.MarketSections = new List<NewspaperMarketLine>();
                changed = true;
            }
            if (issue.MarketHintFallbacks is null)
            {
                issue.MarketHintFallbacks = new List<NewspaperMarketLine>();
                changed = true;
            }
            if (issue.Articles is null)
            {
                issue.Articles = new List<NewspaperArticle>();
                changed = true;
            }
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
        if (state.Telemetry.Daily.VisitDenialByReason is null)
        {
            state.Telemetry.Daily.VisitDenialByReason = new Dictionary<string, int>();
            changed = true;
        }
        if (state.Telemetry.Daily.PairEmotionUpdatesByAxis is null)
        {
            state.Telemetry.Daily.PairEmotionUpdatesByAxis = new Dictionary<string, int>();
            changed = true;
        }
        if (state.Telemetry.Daily.AutonomyRejectByReason is null)
        {
            state.Telemetry.Daily.AutonomyRejectByReason = new Dictionary<string, int>();
            changed = true;
        }

        if (state.NpcMemory is null)
        {
            state.NpcMemory = new NpcMemoryState();
            changed = true;
        }
        var normalizedNpcProfiles = NormalizeNpcMemoryProfiles(state.NpcMemory.Profiles, out var npcProfilesChanged, out var npcProfileMergeCount);
        if (npcProfilesChanged)
        {
            state.NpcMemory.Profiles = normalizedNpcProfiles;
            changed = true;
        }
        if (npcProfileMergeCount > 0)
            monitor.Log($"Merged {npcProfileMergeCount} case-variant NPC memory profile entries during save load normalization.", LogLevel.Info);
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
            if (profile.ImportantMemories is null)
            {
                profile.ImportantMemories = new List<ImportantMemoryEntry>();
                changed = true;
            }
            if (profile.TopicCounters is null)
            {
                profile.TopicCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                changed = true;
            }

            foreach (var memory in profile.ImportantMemories)
            {
                if (memory is null)
                    continue;
                if (memory.Keywords is null)
                {
                    memory.Keywords = Array.Empty<string>();
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(memory.Visibility))
                {
                    memory.Visibility = "npc_only";
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(memory.Status))
                {
                    memory.Status = "active";
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(memory.SourceRefKind))
                {
                    memory.SourceRefKind = "chat_rule";
                    changed = true;
                }
            }
        }

        if (state.TranscriptArchive is null)
        {
            state.TranscriptArchive = new TranscriptArchiveState();
            changed = true;
        }
        var normalizedTranscriptArchives = NormalizeTranscriptArchives(state.TranscriptArchive.Archives, out var transcriptArchivesChanged, out var transcriptArchiveMergeCount);
        if (transcriptArchivesChanged)
        {
            state.TranscriptArchive.Archives = normalizedTranscriptArchives;
            changed = true;
        }
        if (transcriptArchiveMergeCount > 0)
            monitor.Log($"Merged {transcriptArchiveMergeCount} case-variant transcript archive entries during save load normalization.", LogLevel.Info);
        foreach (var archive in state.TranscriptArchive.Archives.Values)
        {
            if (archive is null)
                continue;
            if (archive.RawExchanges is null)
            {
                archive.RawExchanges = new List<TranscriptExchange>();
                changed = true;
            }
            if (archive.Chunks is null)
            {
                archive.Chunks = new List<TranscriptChunkHeader>();
                changed = true;
            }
            if (archive.PendingExchanges is null)
            {
                archive.PendingExchanges = new List<PendingTranscriptExchange>();
                changed = true;
            }

            foreach (var exchange in archive.RawExchanges)
            {
                if (exchange is null)
                    continue;
                if (exchange.Keywords is null)
                {
                    exchange.Keywords = Array.Empty<string>();
                    changed = true;
                }
                if (exchange.LinkedImportantMemoryIds is null)
                {
                    exchange.LinkedImportantMemoryIds = new List<string>();
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(exchange.Visibility))
                {
                    exchange.Visibility = "npc_only";
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(exchange.CompletionState))
                {
                    exchange.CompletionState = "complete";
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(exchange.SourceRefKind))
                {
                    exchange.SourceRefKind = "chat";
                    changed = true;
                }
            }

            foreach (var chunk in archive.Chunks)
            {
                if (chunk is null)
                    continue;
                if (chunk.TopKeywords is null)
                {
                    chunk.TopKeywords = Array.Empty<string>();
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(chunk.CompressionCodec))
                {
                    chunk.CompressionCodec = "gzip";
                    changed = true;
                }
            }

            foreach (var pending in archive.PendingExchanges)
            {
                if (pending is null)
                    continue;
                if (string.IsNullOrWhiteSpace(pending.Visibility))
                {
                    pending.Visibility = "npc_only";
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(pending.SourceRefKind))
                {
                    pending.SourceRefKind = "chat";
                    changed = true;
                }
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
        var normalizedTownKnowledge = NormalizeTownKnowledgeByNpc(state.TownMemory.KnowledgeByNpc, out var townKnowledgeChanged, out var townKnowledgeMergeCount);
        if (townKnowledgeChanged)
        {
            state.TownMemory.KnowledgeByNpc = normalizedTownKnowledge;
            changed = true;
        }
        if (townKnowledgeMergeCount > 0)
            monitor.Log($"Merged {townKnowledgeMergeCount} case-variant town memory knowledge entries during save load normalization.", LogLevel.Info);
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

        if (state.MiniGames is null)
        {
            state.MiniGames = new MiniGameState();
            changed = true;
        }
        if (state.MiniGames.TownSquareMagician is null)
        {
            state.MiniGames.TownSquareMagician = new TownSquareMagicianState();
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(state.MiniGames.TownSquareMagician.LastOutcome))
        {
            state.MiniGames.TownSquareMagician.LastOutcome = "fresh";
            changed = true;
        }
        if (state.MiniGames.TownSquareMagician.AttemptsUsed < 0)
        {
            state.MiniGames.TownSquareMagician.AttemptsUsed = 0;
            changed = true;
        }
        if (state.MiniGames.TownSquareMagician.HintsUsed < 0)
        {
            state.MiniGames.TownSquareMagician.HintsUsed = 0;
            changed = true;
        }
        if (state.MiniGames.TownSquareMagician.LifetimeLosses < 0)
        {
            state.MiniGames.TownSquareMagician.LifetimeLosses = 0;
            changed = true;
        }
        if (state.MiniGames.TownSquareMagician.LifetimeHintsUsed < 0)
        {
            state.MiniGames.TownSquareMagician.LifetimeHintsUsed = 0;
            changed = true;
        }
        if (state.MiniGames.TownSquareMagician.LifetimeBonusRoundsPlayed < 0)
        {
            state.MiniGames.TownSquareMagician.LifetimeBonusRoundsPlayed = 0;
            changed = true;
        }
        if (state.MiniGames.TownSquareMagician.RareDryRewardDays < 0)
        {
            state.MiniGames.TownSquareMagician.RareDryRewardDays = 0;
            changed = true;
        }
        if (state.MiniGames.TownSquareMagician.GrandDryRewardDays < 0)
        {
            state.MiniGames.TownSquareMagician.GrandDryRewardDays = 0;
            changed = true;
        }
        if (state.MiniGames.TownSquareMagician.ConsecutiveWins < 0)
        {
            state.MiniGames.TownSquareMagician.ConsecutiveWins = 0;
            changed = true;
        }
        if (state.MiniGames.TownSquareMagician.ConsecutiveLosses < 0)
        {
            state.MiniGames.TownSquareMagician.ConsecutiveLosses = 0;
            changed = true;
        }
        if (state.MiniGames.TownSquareMagician.ArcProgressPoints < 0)
        {
            state.MiniGames.TownSquareMagician.ArcProgressPoints = 0;
            changed = true;
        }
        if (state.MiniGames.TownSquareMagician.SessionsStartedToday < 0)
        {
            state.MiniGames.TownSquareMagician.SessionsStartedToday = 0;
            changed = true;
        }
        if (state.MiniGames.TownSquareMagician.PlayedRoundIdsToday is null)
        {
            state.MiniGames.TownSquareMagician.PlayedRoundIdsToday = new List<string>();
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(state.MiniGames.TownSquareMagician.ArcStageId))
        {
            state.MiniGames.TownSquareMagician.ArcStageId = "street_smoke";
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(state.MiniGames.TownSquareMagician.LastPlayStyleTag))
        {
            state.MiniGames.TownSquareMagician.LastPlayStyleTag = "steady";
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

    private static Dictionary<string, NpcMemoryProfile> NormalizeNpcMemoryProfiles(
        Dictionary<string, NpcMemoryProfile>? source,
        out bool changed,
        out int mergedCaseVariantEntries)
    {
        changed = source is null || !ReferenceEquals(source.Comparer, StringComparer.OrdinalIgnoreCase);
        mergedCaseVariantEntries = 0;

        if (source is null)
            return new Dictionary<string, NpcMemoryProfile>(StringComparer.OrdinalIgnoreCase);

        var normalized = new Dictionary<string, NpcMemoryProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, rawProfile) in source)
        {
            if (string.IsNullOrWhiteSpace(key) || rawProfile is null)
            {
                changed = true;
                continue;
            }

            var profile = NormalizeNpcMemoryProfile(rawProfile, out var profileChanged);
            changed |= profileChanged;

            if (normalized.TryGetValue(key, out var existing))
            {
                MergeNpcMemoryProfile(existing, profile);
                mergedCaseVariantEntries++;
                changed = true;
                continue;
            }

            normalized[key] = profile;
        }

        return changed ? normalized : source;
    }

    private static NpcMemoryProfile NormalizeNpcMemoryProfile(NpcMemoryProfile profile, out bool changed)
    {
        changed = false;
        profile.Facts ??= new List<NpcMemoryFact>();
        profile.RecentTurns ??= new List<NpcMemoryTurn>();
        profile.ImportantMemories ??= new List<ImportantMemoryEntry>();

        if (profile.TopicCounters is null)
        {
            profile.TopicCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            changed = true;
        }
        else
        {
            var normalizedTopicCounters = NormalizeSummedIntDictionary(profile.TopicCounters, out var topicCounterChanged);
            if (topicCounterChanged)
            {
                profile.TopicCounters = normalizedTopicCounters;
                changed = true;
            }
        }

        return profile;
    }

    private static void MergeNpcMemoryProfile(NpcMemoryProfile target, NpcMemoryProfile incoming)
    {
        target.Facts = MergeFacts(target.Facts, incoming.Facts);
        target.ImportantMemories = MergeImportantMemories(target.ImportantMemories, incoming.ImportantMemories);
        target.RecentTurns = MergeRecentTurns(target.RecentTurns, incoming.RecentTurns);
        target.TopicCounters = MergeSummedIntDictionaries(target.TopicCounters, incoming.TopicCounters);
        target.LastUpdatedDay = Math.Max(target.LastUpdatedDay, incoming.LastUpdatedDay);
    }

    private static List<NpcMemoryFact> MergeFacts(IEnumerable<NpcMemoryFact>? existing, IEnumerable<NpcMemoryFact>? incoming)
    {
        var merged = new List<NpcMemoryFact>();
        var byKey = new Dictionary<string, NpcMemoryFact>(StringComparer.OrdinalIgnoreCase);

        void AddFact(NpcMemoryFact? fact)
        {
            if (fact is null || string.IsNullOrWhiteSpace(fact.Text))
                return;

            var key = BuildFactMergeKey(fact);
            if (byKey.TryGetValue(key, out var current))
            {
                current.FactId = PreferNonEmpty(current.FactId, fact.FactId);
                current.Category = PreferNonEmpty(current.Category, fact.Category, "event");
                current.Text = PreferNonEmpty(current.Text, fact.Text);
                current.Day = MergeEarliestPositiveDay(current.Day, fact.Day);
                current.Weight = Math.Max(current.Weight, fact.Weight);
                current.LastReferencedDay = Math.Max(current.LastReferencedDay, fact.LastReferencedDay);
                return;
            }

            byKey[key] = fact;
            merged.Add(fact);
        }

        foreach (var fact in existing ?? Enumerable.Empty<NpcMemoryFact>())
            AddFact(fact);
        foreach (var fact in incoming ?? Enumerable.Empty<NpcMemoryFact>())
            AddFact(fact);

        return merged;
    }

    private static List<ImportantMemoryEntry> MergeImportantMemories(IEnumerable<ImportantMemoryEntry>? existing, IEnumerable<ImportantMemoryEntry>? incoming)
    {
        var merged = new List<ImportantMemoryEntry>();
        var byKey = new Dictionary<string, ImportantMemoryEntry>(StringComparer.OrdinalIgnoreCase);

        void AddMemory(ImportantMemoryEntry? memory)
        {
            if (memory is null || string.IsNullOrWhiteSpace(memory.Summary))
                return;

            memory.Keywords ??= Array.Empty<string>();
            var key = BuildImportantMemoryMergeKey(memory);
            if (byKey.TryGetValue(key, out var current))
            {
                current.MemoryId = PreferNonEmpty(current.MemoryId, memory.MemoryId);
                current.Category = PreferNonEmpty(current.Category, memory.Category, "event");
                current.Summary = PreferNonEmpty(current.Summary, memory.Summary);
                current.Importance = Math.Max(current.Importance, memory.Importance);
                current.Visibility = PreferNonEmpty(current.Visibility, memory.Visibility, "npc_only");
                current.Status = PreferNonEmpty(current.Status, memory.Status, "active");
                current.SourceRefKind = PreferNonEmpty(current.SourceRefKind, memory.SourceRefKind, "chat_rule");
                current.SourceRefId = PreferNonEmpty(current.SourceRefId, memory.SourceRefId);
                current.SourceExchangeId = PreferNonEmpty(current.SourceExchangeId, memory.SourceExchangeId);
                current.EvidenceSnippet = PreferNonEmpty(current.EvidenceSnippet, memory.EvidenceSnippet);
                current.CreatedDay = MergeEarliestPositiveDay(current.CreatedDay, memory.CreatedDay);
                current.LastUpdatedDay = Math.Max(current.LastUpdatedDay, memory.LastUpdatedDay);
                current.LastReferencedDay = Math.Max(current.LastReferencedDay, memory.LastReferencedDay);
                current.Keywords = current.Keywords
                    .Concat(memory.Keywords)
                    .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(18)
                    .ToArray();
                return;
            }

            byKey[key] = memory;
            merged.Add(memory);
        }

        foreach (var memory in existing ?? Enumerable.Empty<ImportantMemoryEntry>())
            AddMemory(memory);
        foreach (var memory in incoming ?? Enumerable.Empty<ImportantMemoryEntry>())
            AddMemory(memory);

        return merged;
    }

    private static List<NpcMemoryTurn> MergeRecentTurns(IEnumerable<NpcMemoryTurn>? existing, IEnumerable<NpcMemoryTurn>? incoming)
    {
        var merged = new List<NpcMemoryTurn>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddTurn(NpcMemoryTurn? turn)
        {
            if (turn is null)
                return;

            turn.Tags ??= Array.Empty<string>();
            var key = $"{turn.Day}|{turn.PlayerText?.Trim()}|{turn.NpcText?.Trim()}";
            if (!seen.Add(key))
                return;

            merged.Add(turn);
        }

        foreach (var turn in existing ?? Enumerable.Empty<NpcMemoryTurn>())
            AddTurn(turn);
        foreach (var turn in incoming ?? Enumerable.Empty<NpcMemoryTurn>())
            AddTurn(turn);

        return merged
            .OrderBy(turn => turn.Day)
            .ToList();
    }

    private static string BuildFactMergeKey(NpcMemoryFact fact)
    {
        if (!string.IsNullOrWhiteSpace(fact.FactId))
            return $"factid:{fact.FactId.Trim()}";

        return $"text:{fact.Text.Trim()}";
    }

    private static string BuildImportantMemoryMergeKey(ImportantMemoryEntry memory)
    {
        if (!string.IsNullOrWhiteSpace(memory.MemoryId))
            return $"memory:{memory.MemoryId.Trim()}";
        if (!string.IsNullOrWhiteSpace(memory.SourceRefId))
            return $"source:{memory.SourceRefKind.Trim()}|{memory.SourceRefId.Trim()}";

        return $"summary:{memory.Category.Trim()}|{memory.Summary.Trim()}";
    }

    private static Dictionary<string, NpcTranscriptArchive> NormalizeTranscriptArchives(
        Dictionary<string, NpcTranscriptArchive>? source,
        out bool changed,
        out int mergedCaseVariantEntries)
    {
        changed = source is null || !ReferenceEquals(source.Comparer, StringComparer.OrdinalIgnoreCase);
        mergedCaseVariantEntries = 0;

        if (source is null)
            return new Dictionary<string, NpcTranscriptArchive>(StringComparer.OrdinalIgnoreCase);

        var normalized = new Dictionary<string, NpcTranscriptArchive>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, rawArchive) in source)
        {
            if (string.IsNullOrWhiteSpace(key) || rawArchive is null)
            {
                changed = true;
                continue;
            }

            var archive = NormalizeTranscriptArchive(rawArchive, out var archiveChanged);
            changed |= archiveChanged;

            if (normalized.TryGetValue(key, out var existing))
            {
                MergeTranscriptArchive(existing, archive);
                mergedCaseVariantEntries++;
                changed = true;
                continue;
            }

            normalized[key] = archive;
        }

        return changed ? normalized : source;
    }

    private static NpcTranscriptArchive NormalizeTranscriptArchive(NpcTranscriptArchive archive, out bool changed)
    {
        changed = false;
        archive.RawExchanges ??= new List<TranscriptExchange>();
        archive.Chunks ??= new List<TranscriptChunkHeader>();
        archive.PendingExchanges ??= new List<PendingTranscriptExchange>();

        foreach (var exchange in archive.RawExchanges)
        {
            if (exchange is null)
                continue;

            if (exchange.Keywords is null)
            {
                exchange.Keywords = Array.Empty<string>();
                changed = true;
            }
            if (exchange.LinkedImportantMemoryIds is null)
            {
                exchange.LinkedImportantMemoryIds = new List<string>();
                changed = true;
            }
        }

        foreach (var chunk in archive.Chunks)
        {
            if (chunk is null)
                continue;

            if (chunk.TopKeywords is null)
            {
                chunk.TopKeywords = Array.Empty<string>();
                changed = true;
            }
        }

        return archive;
    }

    private static void MergeTranscriptArchive(NpcTranscriptArchive target, NpcTranscriptArchive incoming)
    {
        target.RawExchanges = MergeTranscriptExchanges(target.RawExchanges, incoming.RawExchanges);
        target.PendingExchanges = MergePendingTranscriptExchanges(target.PendingExchanges, incoming.PendingExchanges);
        target.Chunks = MergeTranscriptChunks(target.Chunks, incoming.Chunks);
        target.LastUpdatedDay = Math.Max(target.LastUpdatedDay, incoming.LastUpdatedDay);
    }

    private static List<TranscriptExchange> MergeTranscriptExchanges(IEnumerable<TranscriptExchange>? existing, IEnumerable<TranscriptExchange>? incoming)
    {
        var merged = new List<TranscriptExchange>();
        var byKey = new Dictionary<string, TranscriptExchange>(StringComparer.OrdinalIgnoreCase);

        void AddExchange(TranscriptExchange? exchange)
        {
            if (exchange is null)
                return;

            exchange.Keywords ??= Array.Empty<string>();
            exchange.LinkedImportantMemoryIds ??= new List<string>();
            var key = BuildTranscriptExchangeMergeKey(exchange);
            if (byKey.TryGetValue(key, out var current))
            {
                current.RequestToken = PreferNonEmpty(current.RequestToken, exchange.RequestToken);
                current.NpcId = PreferNonEmpty(current.NpcId, exchange.NpcId);
                current.NpcDisplayName = PreferNonEmpty(current.NpcDisplayName, exchange.NpcDisplayName);
                current.Day = Math.Max(current.Day, exchange.Day);
                current.TimeOfDay = Math.Max(current.TimeOfDay, exchange.TimeOfDay);
                current.Season = PreferNonEmpty(current.Season, exchange.Season, "spring");
                current.Year = Math.Max(current.Year, exchange.Year);
                current.LocationName = PreferNonEmpty(current.LocationName, exchange.LocationName);
                current.ContextTag = PreferNonEmpty(current.ContextTag, exchange.ContextTag, "player_chat");
                current.PlayerText = PreferNonEmpty(current.PlayerText, exchange.PlayerText);
                current.NpcText = PreferNonEmpty(current.NpcText, exchange.NpcText);
                current.Keywords = current.Keywords
                    .Concat(exchange.Keywords)
                    .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(24)
                    .ToArray();
                current.Importance = Math.Max(current.Importance, exchange.Importance);
                current.Visibility = PreferNonEmpty(current.Visibility, exchange.Visibility, "npc_only");
                current.CompletionState = PreferNonEmpty(current.CompletionState, exchange.CompletionState, "complete");
                current.SourceRefKind = PreferNonEmpty(current.SourceRefKind, exchange.SourceRefKind, "chat");
                current.SourceRefId = PreferNonEmpty(current.SourceRefId, exchange.SourceRefId);
                current.LinkedImportantMemoryIds = current.LinkedImportantMemoryIds
                    .Concat(exchange.LinkedImportantMemoryIds)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return;
            }

            byKey[key] = exchange;
            merged.Add(exchange);
        }

        foreach (var exchange in existing ?? Enumerable.Empty<TranscriptExchange>())
            AddExchange(exchange);
        foreach (var exchange in incoming ?? Enumerable.Empty<TranscriptExchange>())
            AddExchange(exchange);

        return merged
            .OrderBy(exchange => exchange.Day)
            .ThenBy(exchange => exchange.TimeOfDay)
            .ToList();
    }

    private static List<PendingTranscriptExchange> MergePendingTranscriptExchanges(IEnumerable<PendingTranscriptExchange>? existing, IEnumerable<PendingTranscriptExchange>? incoming)
    {
        var merged = new List<PendingTranscriptExchange>();
        var byKey = new Dictionary<string, PendingTranscriptExchange>(StringComparer.OrdinalIgnoreCase);

        void AddPending(PendingTranscriptExchange? pending)
        {
            if (pending is null)
                return;

            var key = BuildPendingTranscriptExchangeMergeKey(pending);
            if (byKey.TryGetValue(key, out var current))
            {
                current.RequestToken = PreferNonEmpty(current.RequestToken, pending.RequestToken);
                current.NpcId = PreferNonEmpty(current.NpcId, pending.NpcId);
                current.NpcDisplayName = PreferNonEmpty(current.NpcDisplayName, pending.NpcDisplayName);
                current.Day = Math.Max(current.Day, pending.Day);
                current.TimeOfDay = Math.Max(current.TimeOfDay, pending.TimeOfDay);
                current.Season = PreferNonEmpty(current.Season, pending.Season, "spring");
                current.Year = Math.Max(current.Year, pending.Year);
                current.LocationName = PreferNonEmpty(current.LocationName, pending.LocationName);
                current.ContextTag = PreferNonEmpty(current.ContextTag, pending.ContextTag, "player_chat");
                current.PlayerText = PreferNonEmpty(current.PlayerText, pending.PlayerText);
                current.Visibility = PreferNonEmpty(current.Visibility, pending.Visibility, "npc_only");
                current.SourceRefKind = PreferNonEmpty(current.SourceRefKind, pending.SourceRefKind, "chat");
                current.SourceRefId = PreferNonEmpty(current.SourceRefId, pending.SourceRefId);
                return;
            }

            byKey[key] = pending;
            merged.Add(pending);
        }

        foreach (var pending in existing ?? Enumerable.Empty<PendingTranscriptExchange>())
            AddPending(pending);
        foreach (var pending in incoming ?? Enumerable.Empty<PendingTranscriptExchange>())
            AddPending(pending);

        return merged
            .OrderBy(pending => pending.Day)
            .ThenBy(pending => pending.TimeOfDay)
            .ToList();
    }

    private static List<TranscriptChunkHeader> MergeTranscriptChunks(IEnumerable<TranscriptChunkHeader>? existing, IEnumerable<TranscriptChunkHeader>? incoming)
    {
        var merged = new List<TranscriptChunkHeader>();
        var byKey = new Dictionary<string, TranscriptChunkHeader>(StringComparer.OrdinalIgnoreCase);

        void AddChunk(TranscriptChunkHeader? chunk)
        {
            if (chunk is null)
                return;

            chunk.TopKeywords ??= Array.Empty<string>();
            var key = string.IsNullOrWhiteSpace(chunk.ChunkId)
                ? $"summary:{chunk.DayRangeStart}|{chunk.DayRangeEnd}|{chunk.Summary?.Trim()}"
                : $"chunk:{chunk.ChunkId.Trim()}";
            if (byKey.TryGetValue(key, out var current))
            {
                current.NpcId = PreferNonEmpty(current.NpcId, chunk.NpcId);
                current.DayRangeStart = MergeEarliestPositiveDay(current.DayRangeStart, chunk.DayRangeStart);
                current.DayRangeEnd = Math.Max(current.DayRangeEnd, chunk.DayRangeEnd);
                current.ExchangeCount = Math.Max(current.ExchangeCount, chunk.ExchangeCount);
                current.Summary = PreferLongerNonEmpty(current.Summary, chunk.Summary);
                current.TopKeywords = current.TopKeywords
                    .Concat(chunk.TopKeywords)
                    .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(18)
                    .ToArray();
                current.ImportanceMax = Math.Max(current.ImportanceMax, chunk.ImportanceMax);
                current.CompressionCodec = PreferNonEmpty(current.CompressionCodec, chunk.CompressionCodec, "gzip");
                current.CompressedPayloadBase64 = PreferLongerNonEmpty(current.CompressedPayloadBase64, chunk.CompressedPayloadBase64);
                return;
            }

            byKey[key] = chunk;
            merged.Add(chunk);
        }

        foreach (var chunk in existing ?? Enumerable.Empty<TranscriptChunkHeader>())
            AddChunk(chunk);
        foreach (var chunk in incoming ?? Enumerable.Empty<TranscriptChunkHeader>())
            AddChunk(chunk);

        return merged
            .OrderBy(chunk => chunk.DayRangeStart)
            .ThenBy(chunk => chunk.DayRangeEnd)
            .ToList();
    }

    private static string BuildTranscriptExchangeMergeKey(TranscriptExchange exchange)
    {
        if (!string.IsNullOrWhiteSpace(exchange.ExchangeId))
            return $"exchange:{exchange.ExchangeId.Trim()}";
        if (!string.IsNullOrWhiteSpace(exchange.RequestToken))
            return $"request:{exchange.RequestToken.Trim()}|{exchange.Day}|{exchange.PlayerText?.Trim()}|{exchange.NpcText?.Trim()}";

        return $"text:{exchange.Day}|{exchange.PlayerText?.Trim()}|{exchange.NpcText?.Trim()}";
    }

    private static string BuildPendingTranscriptExchangeMergeKey(PendingTranscriptExchange exchange)
    {
        if (!string.IsNullOrWhiteSpace(exchange.ExchangeId))
            return $"pending:{exchange.ExchangeId.Trim()}";
        if (!string.IsNullOrWhiteSpace(exchange.RequestToken))
            return $"request:{exchange.RequestToken.Trim()}|{exchange.Day}|{exchange.PlayerText?.Trim()}";

        return $"pendingtext:{exchange.Day}|{exchange.PlayerText?.Trim()}";
    }

    private static Dictionary<string, NpcTownKnowledge> NormalizeTownKnowledgeByNpc(
        Dictionary<string, NpcTownKnowledge>? source,
        out bool changed,
        out int mergedCaseVariantEntries)
    {
        changed = source is null || !ReferenceEquals(source.Comparer, StringComparer.OrdinalIgnoreCase);
        mergedCaseVariantEntries = 0;

        if (source is null)
            return new Dictionary<string, NpcTownKnowledge>(StringComparer.OrdinalIgnoreCase);

        var normalized = new Dictionary<string, NpcTownKnowledge>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, rawKnowledge) in source)
        {
            if (string.IsNullOrWhiteSpace(key) || rawKnowledge is null)
            {
                changed = true;
                continue;
            }

            var knowledge = NormalizeNpcTownKnowledge(rawKnowledge, out var knowledgeChanged);
            changed |= knowledgeChanged;

            if (normalized.TryGetValue(key, out var existing))
            {
                MergeNpcTownKnowledge(existing, knowledge);
                mergedCaseVariantEntries++;
                changed = true;
                continue;
            }

            normalized[key] = knowledge;
        }

        return changed ? normalized : source;
    }

    private static NpcTownKnowledge NormalizeNpcTownKnowledge(NpcTownKnowledge knowledge, out bool changed)
    {
        changed = false;
        if (knowledge.ByEventId is null)
        {
            knowledge.ByEventId = new Dictionary<string, TownKnowledgeEntry>(StringComparer.OrdinalIgnoreCase);
            changed = true;
            return knowledge;
        }

        var normalizedByEventId = new Dictionary<string, TownKnowledgeEntry>(StringComparer.OrdinalIgnoreCase);
        var comparerChanged = !ReferenceEquals(knowledge.ByEventId.Comparer, StringComparer.OrdinalIgnoreCase);
        var mergedEntries = 0;
        foreach (var (eventId, entry) in knowledge.ByEventId)
        {
            if (string.IsNullOrWhiteSpace(eventId) || entry is null)
            {
                comparerChanged = true;
                continue;
            }

            if (normalizedByEventId.TryGetValue(eventId, out var existing))
            {
                MergeTownKnowledgeEntry(existing, entry);
                mergedEntries++;
                comparerChanged = true;
                continue;
            }

            normalizedByEventId[eventId] = entry;
        }

        if (comparerChanged || mergedEntries > 0)
        {
            knowledge.ByEventId = normalizedByEventId;
            changed = true;
        }

        return knowledge;
    }

    private static void MergeNpcTownKnowledge(NpcTownKnowledge target, NpcTownKnowledge incoming)
    {
        target.ByEventId ??= new Dictionary<string, TownKnowledgeEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var (eventId, entry) in incoming.ByEventId ?? new Dictionary<string, TownKnowledgeEntry>(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(eventId) || entry is null)
                continue;

            if (target.ByEventId.TryGetValue(eventId, out var existing))
            {
                MergeTownKnowledgeEntry(existing, entry);
                continue;
            }

            target.ByEventId[eventId] = entry;
        }
    }

    private static void MergeTownKnowledgeEntry(TownKnowledgeEntry target, TownKnowledgeEntry incoming)
    {
        target.Knows |= incoming.Knows;
        target.LearnedDay = MergeEarliestPositiveDay(target.LearnedDay, incoming.LearnedDay);
        target.MentionCount = Math.Max(target.MentionCount, incoming.MentionCount);
        target.LastMentionDay = Math.Max(target.LastMentionDay, incoming.LastMentionDay);
        target.Angle = PreferNonEmpty(target.Angle, incoming.Angle, "neutral");
        target.LearnedFromNpc = PreferNonEmpty(target.LearnedFromNpc, incoming.LearnedFromNpc);
    }

    private static Dictionary<string, int> NormalizeSummedIntDictionary(Dictionary<string, int> source, out bool changed)
    {
        changed = !ReferenceEquals(source.Comparer, StringComparer.OrdinalIgnoreCase);
        var normalized = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in source)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                changed = true;
                continue;
            }

            if (normalized.TryGetValue(key, out var existing))
            {
                normalized[key] = existing + value;
                changed = true;
                continue;
            }

            normalized[key] = value;
        }

        return changed ? normalized : source;
    }

    private static Dictionary<string, int> MergeSummedIntDictionaries(
        Dictionary<string, int>? existing,
        Dictionary<string, int>? incoming)
    {
        var merged = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in new[] { existing, incoming })
        {
            if (source is null)
                continue;

            foreach (var (key, value) in source)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                merged.TryGetValue(key, out var current);
                merged[key] = current + value;
            }
        }

        return merged;
    }

    private static int MergeEarliestPositiveDay(int existing, int incoming)
    {
        if (existing <= 0)
            return incoming;
        if (incoming <= 0)
            return existing;

        return Math.Min(existing, incoming);
    }

    private static string PreferNonEmpty(string? existing, string? incoming, string fallback = "")
    {
        if (!string.IsNullOrWhiteSpace(existing))
            return existing.Trim();
        if (!string.IsNullOrWhiteSpace(incoming))
            return incoming.Trim();

        return fallback;
    }

    private static string PreferLongerNonEmpty(string? existing, string? incoming)
    {
        var left = existing?.Trim() ?? string.Empty;
        var right = incoming?.Trim() ?? string.Empty;
        if (left.Length == 0)
            return right;
        if (right.Length == 0)
            return left;

        return right.Length > left.Length ? right : left;
    }
}



