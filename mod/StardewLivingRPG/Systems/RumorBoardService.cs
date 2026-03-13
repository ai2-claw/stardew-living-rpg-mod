using StardewLivingRPG.State;
using StardewLivingRPG.Utils;
using StardewValley;
using System.Text.RegularExpressions;

namespace StardewLivingRPG.Systems;

public sealed class RumorBoardService
{
    private const int MaxAvailableBoardQuests = 4;
    private const int MaxActiveBoardQuests = 4;

    private static readonly HashSet<string> ValidResources = new(StringComparer.OrdinalIgnoreCase)
    {
        "stone", "copper_ore", "iron_ore", "gold_ore", "iridium_ore",
        "coal", "quartz", "refined_quartz",
        "earth_crystal", "frozen_tear", "fire_quartz",
        "amethyst", "topaz", "jade", "aquamarine", "ruby", "emerald", "diamond"
    };

    private static readonly HashSet<string> ValidNpcTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "lewis", "pierre", "robin",
        "abigail", "alex", "caroline", "clint", "demetrius",
        "dwarf", "elliott", "emily", "evelyn", "george", "gil", "gunther",
        "gus", "haley", "harvey", "jas", "jodi", "kent",
        "krobus", "leah", "leo", "linus", "marnie", "marlon", "maru", "morris",
        "pam", "penny", "qi", "sam", "sandy", "sebastian", "shane",
        "vincent", "willy", "wizard"
    };

    private static readonly HashSet<string> SupplementalSupplyItems = new(StringComparer.OrdinalIgnoreCase)
    {
        "wild_horseradish", "daffodil", "leek", "dandelion",
        "salmonberry", "blackberry", "spice_berry", "sweet_pea",
        "grape", "hazelnut", "common_mushroom", "chanterelle", "morel", "red_mushroom", "purple_mushroom",
        "winter_root", "crystal_fruit", "snow_yam", "crocus", "holly",
        "anchovy", "sardine", "herring", "tuna", "salmon", "sunfish", "catfish", "shad",
        "smallmouth_bass", "largemouth_bass", "carp", "bream", "pike", "red_mullet", "tilapia",
        "squid", "halibut", "walleye", "eel", "flounder", "chub", "sturgeon", "ghostfish",
        "wood", "hardwood", "fiber", "sap", "clay"
    };

    private static readonly Dictionary<string, string> SupplyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["parsnips"] = "parsnip",
        ["potatoes"] = "potato",
        ["cauliflowers"] = "cauliflower",
        ["blueberries"] = "blueberry",
        ["melons"] = "melon",
        ["pumpkins"] = "pumpkin",
        ["cranberries"] = "cranberry",
        ["corns"] = "corn",
        ["wheats"] = "wheat",
        ["tomatoes"] = "tomato",
        ["berries"] = "blackberry",
        ["spice_berries"] = "spice_berry",
        ["sweet_peas"] = "sweet_pea",
        ["salmonberries"] = "salmonberry",
        ["horseradish"] = "wild_horseradish",
        ["wild_horseradishes"] = "wild_horseradish",
        ["largemouth_basses"] = "largemouth_bass",
        ["smallmouth_basses"] = "smallmouth_bass",
        ["goat_milks"] = "goat_milk",
        ["large_milks"] = "large_milk",
        ["large_goat_milks"] = "large_goat_milk",
        ["large_eggs"] = "large_egg",
        ["large_brown_eggs"] = "large_brown_egg",
        ["woods"] = "wood"
    };

    private static readonly Dictionary<string, int> ResourceUnitValues = new(StringComparer.OrdinalIgnoreCase)
    {
        ["stone"] = 2,
        ["copper_ore"] = 75,
        ["iron_ore"] = 150,
        ["gold_ore"] = 250,
        ["iridium_ore"] = 100,
        ["coal"] = 150,
        ["quartz"] = 50,
        ["refined_quartz"] = 50,
        ["earth_crystal"] = 50,
        ["frozen_tear"] = 75,
        ["fire_quartz"] = 100,
        ["amethyst"] = 100,
        ["topaz"] = 80,
        ["jade"] = 200,
        ["aquamarine"] = 180,
        ["ruby"] = 250,
        ["emerald"] = 250,
        ["diamond"] = 750
    };

    private static readonly Dictionary<string, int> SupplyUnitValues = new(StringComparer.OrdinalIgnoreCase)
    {
        ["wild_horseradish"] = 50,
        ["daffodil"] = 30,
        ["leek"] = 60,
        ["dandelion"] = 40,
        ["salmonberry"] = 5,
        ["blackberry"] = 25,
        ["spice_berry"] = 80,
        ["sweet_pea"] = 50,
        ["grape"] = 80,
        ["hazelnut"] = 90,
        ["common_mushroom"] = 40,
        ["chanterelle"] = 160,
        ["morel"] = 150,
        ["red_mushroom"] = 75,
        ["purple_mushroom"] = 250,
        ["winter_root"] = 70,
        ["crystal_fruit"] = 150,
        ["snow_yam"] = 100,
        ["crocus"] = 60,
        ["holly"] = 80,
        ["anchovy"] = 30,
        ["sardine"] = 40,
        ["herring"] = 30,
        ["tuna"] = 100,
        ["salmon"] = 75,
        ["sunfish"] = 30,
        ["catfish"] = 200,
        ["shad"] = 60,
        ["smallmouth_bass"] = 50,
        ["largemouth_bass"] = 100,
        ["carp"] = 30,
        ["bream"] = 45,
        ["pike"] = 100,
        ["red_mullet"] = 75,
        ["tilapia"] = 75,
        ["squid"] = 80,
        ["halibut"] = 80,
        ["walleye"] = 105,
        ["eel"] = 85,
        ["flounder"] = 100,
        ["chub"] = 50,
        ["sturgeon"] = 200,
        ["ghostfish"] = 45,
        ["wood"] = 2,
        ["hardwood"] = 15,
        ["fiber"] = 1,
        ["sap"] = 2,
        ["clay"] = 20
    };

    public void RefreshDailyRumors(SaveState state)
    {
        // Keep active quests untouched; rotate available list daily.
        state.Quests.Available.Clear();

        var eventSupplyTarget = TrySelectEventDerivedSupplyTarget(state);
        var crop = SelectFreshSupplyTarget(
            state,
            preferredTarget: eventSupplyTarget,
            fallbackSeed: $"daily_crop_{state.Calendar.Day}");

        var eventVisitTarget = TrySelectEventDerivedVisitTarget(state);
        var visitTarget = SelectFreshVisitTarget(
            state,
            preferredTarget: eventVisitTarget,
            fallbackSeed: $"daily_social_{state.Calendar.Day}");
        state.Quests.Available.Add(new QuestEntry
        {
            QuestId = $"quest_gather_{crop}_{state.Calendar.Day}",
            TemplateId = "gather_crop",
            Status = "available",
            Source = "rumor_mill",
            Issuer = "lewis",
            ExpiresDay = state.Calendar.Day + 3,
            Summary = QuestTextHelper.BuildQuestSummary("lewis", "gather_crop", crop, 20),
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
            Summary = QuestTextHelper.BuildQuestSummary("haley", "social_visit", visitTarget, 1),
            TargetItem = visitTarget,
            TargetCount = 1,
            RewardGold = 250
        });

        TrimAvailableQuestsToBoardCapacity(state);
    }

    public void ExpireOverdueQuests(SaveState state)
    {
        var currentDay = ResolveEffectiveCurrentDay(state);
        if (currentDay > state.Calendar.Day)
            state.Calendar.Day = currentDay;

        var overdueAvailable = state.Quests.Available
            .Where(q => q.ExpiresDay > 0 && currentDay > q.ExpiresDay)
            .ToList();
        foreach (var quest in overdueAvailable)
            state.Quests.Available.Remove(quest);

        var overdue = state.Quests.Active
            .Where(q => q.ExpiresDay > 0 && currentDay > q.ExpiresDay)
            .ToList();

        foreach (var quest in overdue)
        {
            state.Quests.Active.Remove(quest);
            quest.Status = "failed";
            state.Quests.Failed.Add(quest);

            state.Facts.Facts[$"quest:{quest.QuestId}:failed"] = new FactValue
            {
                Value = true,
                SetDay = currentDay,
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

        TrimAvailableQuestsToBoardCapacity(state);
    }

    private static int ResolveEffectiveCurrentDay(SaveState state)
    {
        var stateDay = Math.Max(1, state.Calendar.Day);
        var worldDay = TryGetWorldAbsoluteDayFromGame();
        if (worldDay <= 0)
            return stateDay;

        return Math.Max(stateDay, worldDay);
    }

    private static int TryGetWorldAbsoluteDayFromGame()
    {
        var dayOfMonth = Game1.dayOfMonth;
        var year = Game1.year;
        var season = Game1.currentSeason;
        if (year <= 0 || dayOfMonth <= 0 || string.IsNullOrWhiteSpace(season))
            return 0;

        var seasonIndex = season.Trim().ToLowerInvariant() switch
        {
            "spring" => 0,
            "summer" => 1,
            "fall" => 2,
            "winter" => 3,
            _ => 0
        };

        var clampedDay = Math.Clamp(dayOfMonth, 1, 28);
        return ((Math.Max(1, year) - 1) * 112) + (seasonIndex * 28) + clampedDay;
    }

    public bool AcceptQuest(SaveState state, string questId)
    {
        ExpireOverdueQuests(state);
        if (state.Quests.Active.Count >= MaxActiveBoardQuests)
            return false;

        var quest = state.Quests.Available.FirstOrDefault(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase));
        if (quest is null)
            return false;

        state.Quests.Available.Remove(quest);
        quest.Status = "active";
        state.Quests.Active.Add(quest);
        state.Facts.Facts.Remove(BuildSocialVisitFactKey(quest.QuestId));

        state.Facts.Facts[$"quest:{quest.QuestId}:accepted"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "system"
        };

        state.Telemetry.Daily.RumorBoardAccepts += 1;
        return true;
    }

    public int RecordSocialVisitProgress(SaveState state, string npcName)
    {
        var normalizedNpc = NormalizeNpcTarget(npcName);
        if (string.IsNullOrWhiteSpace(normalizedNpc))
            return 0;

        var marked = 0;
        foreach (var quest in state.Quests.Active)
        {
            if (!string.Equals(quest.TemplateId, "social_visit", StringComparison.OrdinalIgnoreCase))
                continue;

            var target = NormalizeNpcTarget(quest.TargetItem);
            if (!string.Equals(target, normalizedNpc, StringComparison.OrdinalIgnoreCase))
                continue;

            var factKey = BuildSocialVisitFactKey(quest.QuestId);
            if (state.Facts.Facts.TryGetValue(factKey, out var existing) && existing.Value)
                continue;

            state.Facts.Facts[factKey] = new FactValue
            {
                Value = true,
                SetDay = state.Calendar.Day,
                Source = "system"
            };
            marked += 1;
        }

        return marked;
    }

    public bool CompleteQuest(SaveState state, string questId)
    {
        var quest = state.Quests.Active.FirstOrDefault(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase));
        if (quest is null)
            return false;

        CompleteQuestInternal(state, quest);
        return true;
    }

    public QuestCompletionResult CompleteQuestWithChecks(SaveState state, string questId, Farmer? player, bool consumeItems = true)
    {
        var progress = GetQuestProgress(state, questId, player);
        if (!progress.Exists)
            return new QuestCompletionResult { Success = false, Message = QuestTextHelper.BuildQuestNotFoundMessage(questId) };

        var quest = progress.Quest!;

        if (!progress.IsReadyToComplete)
        {
            if (string.Equals(quest.TemplateId, "social_visit", StringComparison.OrdinalIgnoreCase))
            {
                var visitTarget = QuestTextHelper.GetQuestTargetDisplayName(quest);
                return new QuestCompletionResult
                {
                    Success = false,
                    Message = QuestTextHelper.BuildVisitFirstMessage(visitTarget)
                };
            }

            return new QuestCompletionResult
            {
                Success = false,
                Message = QuestTextHelper.BuildNotReadyMessage(QuestTextHelper.BuildQuestTitle(quest))
            };
        }

        if (progress.RequiresItems && progress.HaveCount < progress.NeedCount)
        {
            return new QuestCompletionResult
            {
                Success = false,
                Message = QuestTextHelper.BuildNeedItemsMessage(
                    progress.NeedCount,
                    QuestTextHelper.GetQuestTargetDisplayName(quest),
                    progress.HaveCount)
            };
        }

        var consumed = 0;
        if (progress.RequiresItems && consumeItems)
        {
            consumed = progress.NeedCount;
            ConsumeMatchingItems(player, quest.TargetItem, progress.NeedCount);
        }

        CompleteQuestInternal(state, quest);

        var reward = Math.Max(0, quest.RewardGold);
        if (player is not null && reward > 0)
            player.Money += reward;

        var title = QuestTextHelper.BuildQuestTitle(quest);
        return new QuestCompletionResult
        {
            Success = true,
            Message = QuestTextHelper.BuildCompletedMessage(
                title,
                reward,
                progress.RequiresItems ? consumed : 0,
                QuestTextHelper.GetQuestTargetDisplayName(quest)),
            RewardGold = reward
        };
    }

    public QuestProgressResult GetQuestProgress(SaveState state, string questId, Farmer? player)
    {
        var quest = state.Quests.Active.FirstOrDefault(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase));
        if (quest is null)
            return QuestProgressResult.NotFound(questId);

        if (string.Equals(quest.TemplateId, "social_visit", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedTarget = NormalizeNpcTarget(quest.TargetItem);
            if (!ValidNpcTargets.Contains(normalizedTarget))
            {
                return new QuestProgressResult
                {
                    Exists = true,
                    QuestId = quest.QuestId,
                    Quest = quest,
                    RequiresItems = false,
                    NeedCount = 0,
                    HaveCount = 0,
                    IsReadyToComplete = true
                };
            }

            var visited = HasRecordedSocialVisit(state, quest);
            return new QuestProgressResult
            {
                Exists = true,
                QuestId = quest.QuestId,
                Quest = quest,
                RequiresItems = false,
                NeedCount = 1,
                HaveCount = visited ? 1 : 0,
                IsReadyToComplete = visited
            };
        }

        var requiresItems = RequiresItemDelivery(quest.TemplateId);
        var need = requiresItems ? Math.Max(1, quest.TargetCount) : 0;
        var have = requiresItems ? CountMatchingItems(player, quest.TargetItem) : 0;

        return new QuestProgressResult
        {
            Exists = true,
            QuestId = quest.QuestId,
            Quest = quest,
            RequiresItems = requiresItems,
            NeedCount = need,
            HaveCount = have,
            IsReadyToComplete = !requiresItems || have >= need
        };
    }

    public QuestProposalResult CreateQuestFromNpcProposal(
        SaveState state,
        string npcId,
        string templateId,
        string target,
        string urgency,
        string intentKey,
        int? requestedCount = null)
    {
        if (state.Facts.ProcessedIntents.ContainsKey(intentKey))
            return QuestProposalResult.Duplicate;

        var safeTemplate = NormalizeTemplate(templateId);
        var safeTarget = NormalizeTargetForTemplate(state, safeTemplate, target, intentKey);
        var safeUrgency = NormalizeUrgency(urgency);
        var explicitRequestedCount = requestedCount ?? TryExtractRequestedCount(target);

        var (count, minRewardGold, expiresDelta) = BoundsForTemplateAndUrgency(safeTemplate, safeUrgency);
        if (explicitRequestedCount.HasValue)
            count = ResolveRequestedCountForTemplate(safeTemplate, explicitRequestedCount.Value, count);
        var rewardGold = ComputeRewardGold(state, safeTemplate, safeTarget, safeUrgency, count, minRewardGold);

        var suffix = Math.Abs(intentKey.GetHashCode()) % 100000;
        var questId = $"quest_ai_{safeTemplate}_{safeTarget}_{state.Calendar.Day}_{suffix}";

        if (state.Quests.Available.Any(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase)) ||
            state.Quests.Active.Any(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase)) ||
            state.Quests.Completed.Any(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase)))
            return QuestProposalResult.Duplicate;

        var quest = new QuestEntry
        {
            QuestId = questId,
            TemplateId = safeTemplate,
            Status = "available",
            Source = "npc_intent",
            Issuer = npcId,
            ExpiresDay = state.Calendar.Day + expiresDelta,
            Summary = QuestTextHelper.BuildQuestSummary(npcId, safeTemplate, safeTarget, count),
            TargetItem = safeTarget,
            TargetCount = count,
            RewardGold = rewardGold
        };

        state.Quests.Available.Add(quest);
        TrimAvailableQuestsToBoardCapacity(state);
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

        return new QuestProposalResult
        {
            CreatedQuestId = questId,
            RequestedTemplate = templateId,
            RequestedTarget = target,
            RequestedUrgency = urgency,
            AppliedTemplate = safeTemplate,
            AppliedTarget = safeTarget,
            AppliedUrgency = safeUrgency,
            Count = count,
            RewardGold = rewardGold,
            ExpiresDelta = expiresDelta
        };
    }

    private static void TrimAvailableQuestsToBoardCapacity(SaveState state)
    {
        while (state.Quests.Available.Count > MaxAvailableBoardQuests)
            state.Quests.Available.RemoveAt(0);
    }

    private static void CompleteQuestInternal(SaveState state, QuestEntry quest)
    {
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
    }

    private static bool RequiresItemDelivery(string templateId)
    {
        return string.Equals(templateId, "gather_crop", StringComparison.OrdinalIgnoreCase)
            || string.Equals(templateId, "deliver_item", StringComparison.OrdinalIgnoreCase)
            || string.Equals(templateId, "mine_resource", StringComparison.OrdinalIgnoreCase);
    }

    private static int CountMatchingItems(Farmer? player, string target)
    {
        if (player?.Items is null)
            return 0;

        var total = 0;
        var normalizedTarget = NormalizeItemKey(target);
        var canonicalTarget = NormalizeSupplyCandidate(target);
        foreach (var item in player.Items)
        {
            if (item is null)
                continue;

            if (!ItemMatchesTarget(item, normalizedTarget, canonicalTarget))
                continue;

            total += Math.Max(1, item.Stack);
        }

        return total;
    }

    private static void ConsumeMatchingItems(Farmer? player, string target, int needed)
    {
        if (player?.Items is null || needed <= 0)
            return;

        var normalizedTarget = NormalizeItemKey(target);
        var canonicalTarget = NormalizeSupplyCandidate(target);

        for (var i = 0; i < player.Items.Count && needed > 0; i++)
        {
            var item = player.Items[i];
            if (item is null)
                continue;

            if (!ItemMatchesTarget(item, normalizedTarget, canonicalTarget))
                continue;

            var take = Math.Min(needed, Math.Max(1, item.Stack));
            item.Stack -= take;
            needed -= take;

            if (item.Stack <= 0)
                player.Items[i] = null;
        }
    }

    private static string NormalizeItemKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value
            .Trim()
            .ToLowerInvariant()
            .Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal);

        normalized = Regex.Replace(normalized, @"[^\p{L}\p{Nd}]+", "_", RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"_+", "_", RegexOptions.CultureInvariant);
        return normalized.Trim('_');
    }

    private static bool ItemMatchesTarget(Item item, string normalizedTarget, string canonicalTarget)
    {
        if (string.IsNullOrWhiteSpace(normalizedTarget))
            return false;

        var displayKey = NormalizeItemKey(item.DisplayName);
        if (string.Equals(displayKey, normalizedTarget, StringComparison.Ordinal))
            return true;

        var internalNameKey = NormalizeItemKey(item.Name);
        if (string.Equals(internalNameKey, normalizedTarget, StringComparison.Ordinal))
            return true;

        if (string.IsNullOrWhiteSpace(canonicalTarget))
            return false;

        var canonicalDisplay = NormalizeSupplyCandidate(item.DisplayName);
        if (string.Equals(canonicalDisplay, canonicalTarget, StringComparison.Ordinal))
            return true;

        var canonicalInternal = NormalizeSupplyCandidate(item.Name);
        return string.Equals(canonicalInternal, canonicalTarget, StringComparison.Ordinal);
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

    private static (int Count, int MinRewardGold, int ExpiresDelta) BoundsForTemplateAndUrgency(string templateId, string urgency)
    {
        return templateId switch
        {
            "social_visit" => urgency switch
            {
                "high" => (1, 400, 2),
                "medium" => (1, 300, 3),
                _ => (1, 220, 4)
            },
            "mine_resource" => urgency switch
            {
                "high" => (20, 800, 2),
                "medium" => (14, 600, 3),
                _ => (10, 450, 4)
            },
            "deliver_item" => urgency switch
            {
                "high" => (18, 650, 2),
                "medium" => (14, 500, 3),
                _ => (10, 360, 4)
            },
            _ => urgency switch // gather_crop
            {
                "high" => (25, 700, 2),
                "medium" => (20, 500, 3),
                _ => (14, 350, 4)
            }
        };
    }

    private static int ComputeRewardGold(SaveState state, string templateId, string target, string urgency, int count, int minRewardGold)
    {
        if (string.Equals(templateId, "social_visit", StringComparison.OrdinalIgnoreCase))
            return minRewardGold;

        var unitValue = ResolveTargetUnitValue(state, templateId, target);
        var marketValue = Math.Max(1, unitValue) * Math.Max(1, count);
        var marketFloor = RoundUpToStep(marketValue, 25);
        var multiplier = ResolveRewardMultiplier(templateId, urgency);
        var scaledReward = (int)MathF.Round(marketValue * multiplier);
        var roundedReward = RoundToNearest(scaledReward, 25);

        return Math.Clamp(Math.Max(minRewardGold, Math.Max(marketFloor, roundedReward)), minRewardGold, 12000);
    }

    private static float ResolveRewardMultiplier(string templateId, string urgency)
    {
        return templateId switch
        {
            "mine_resource" => urgency switch
            {
                "high" => 1.05f,
                "medium" => 0.90f,
                _ => 0.75f
            },
            "deliver_item" => urgency switch
            {
                "high" => 0.90f,
                "medium" => 0.75f,
                _ => 0.60f
            },
            _ => urgency switch // gather_crop
            {
                "high" => 0.85f,
                "medium" => 0.70f,
                _ => 0.55f
            }
        };
    }

    private static int ResolveTargetUnitValue(SaveState state, string templateId, string target)
    {
        if (string.Equals(templateId, "mine_resource", StringComparison.OrdinalIgnoreCase))
            return ResourceUnitValues.TryGetValue(target, out var unit) ? unit : 75;

        if (state.Economy.Crops.TryGetValue(target, out var crop))
        {
            if (crop.PriceToday > 0)
                return crop.PriceToday;
            if (crop.BasePrice > 0)
                return crop.BasePrice;
        }

        var catalog = VanillaCropCatalog.GetEntries();
        if (catalog.TryGetValue(target, out var catalogEntry) && catalogEntry.BasePrice > 0)
            return catalogEntry.BasePrice;

        if (SupplyUnitValues.TryGetValue(target, out var supplyUnitValue))
            return supplyUnitValue;

        return 80;
    }

    private static int RoundToNearest(int value, int step)
    {
        if (step <= 1)
            return Math.Max(0, value);

        return (int)MathF.Round(value / (float)step) * step;
    }

    private static int RoundUpToStep(int value, int step)
    {
        if (step <= 1)
            return Math.Max(0, value);

        var safeValue = Math.Max(0, value);
        return ((safeValue + step - 1) / step) * step;
    }

    private static string NormalizeTargetForTemplate(SaveState state, string templateId, string rawTarget, string? fallbackSeed = null)
    {
        var itemTarget = NormalizeSupplyCandidate(rawTarget);
        var npcTarget = NormalizeNpcTarget(rawTarget);

        return templateId switch
        {
            "gather_crop" => NormalizeSupplyTargetOrFallback(state, itemTarget, fallbackSeed),
            "deliver_item" => NormalizeSupplyTargetOrFallback(state, itemTarget, fallbackSeed),
            "mine_resource" => ValidResources.Contains(itemTarget) ? itemTarget : SelectFallbackMineResourceTarget(state, fallbackSeed),
            "social_visit" => ValidNpcTargets.Contains(npcTarget) ? npcTarget : SelectFallbackVisitTarget(state, fallbackSeed),
            _ => NormalizeSupplyTargetOrFallback(state, itemTarget, fallbackSeed)
        };
    }

    private static string NormalizeSupplyTargetOrFallback(SaveState state, string candidate, string? fallbackSeed = null)
    {
        var validSupplyItems = GetValidSupplyItems(state);
        if (validSupplyItems.Contains(candidate))
            return candidate;

        return SelectFallbackSupplyTarget(state, fallbackSeed);
    }

    private static string NormalizeSupplyCandidate(string? rawTarget)
    {
        var cleaned = StripRequestedCountTokens(rawTarget);
        var t = cleaned
            .Replace(" ", "_", StringComparison.Ordinal)
            .Trim('"', '\'', '.', ',', '!', '?', ';', ':');

        if (SupplyAliases.TryGetValue(t, out var alias))
            return alias;

        var catalog = VanillaCropCatalog.GetEntries();
        if (catalog.ContainsKey(t) || SupplementalSupplyItems.Contains(t) || ValidResources.Contains(t))
            return t;

        if (t.EndsWith("ies", StringComparison.Ordinal) && t.Length > 3)
        {
            var singularY = t[..^3] + "y";
            if (SupplyAliases.TryGetValue(singularY, out var singularAlias))
                return singularAlias;
            if (catalog.ContainsKey(singularY) || SupplementalSupplyItems.Contains(singularY) || ValidResources.Contains(singularY))
                return singularY;
            return singularY;
        }

        if (t.EndsWith("s", StringComparison.Ordinal) && t.Length > 1)
        {
            var singular = t[..^1];
            if (SupplyAliases.TryGetValue(singular, out var singularAlias))
                return singularAlias;
            if (catalog.ContainsKey(singular) || SupplementalSupplyItems.Contains(singular) || ValidResources.Contains(singular))
                return singular;
            return singular;
        }

        return t;
    }

    private static string SelectFreshSupplyTarget(SaveState state, string? preferredTarget, string? fallbackSeed = null)
    {
        var validSupplyItems = GetValidSupplyItems(state);
        var candidates = new List<string>();
        var preferred = NormalizeSupplyCandidate(preferredTarget);
        if (validSupplyItems.Contains(preferred))
            candidates.Add(preferred);

        candidates.AddRange(state.Economy.Crops
            .Where(kv => validSupplyItems.Contains(kv.Key))
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .ThenByDescending(kv => kv.Value.DemandFactor)
            .ThenByDescending(kv => kv.Value.SupplyPressureFactor)
            .Select(kv => NormalizeSupplyCandidate(kv.Key)));

        candidates.AddRange(GetSeasonalFallbackSupplyItems(state.Calendar.Season));

        var ordered = candidates
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Where(validSupplyItems.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var candidate in ordered)
        {
            if (!IsQuestTargetRecentlyUsed(state, "gather_crop", candidate))
                return candidate;
        }

        if (ordered.Count > 0)
        {
            var topCandidates = ordered.Take(Math.Min(6, ordered.Count)).ToList();
            var index = GetDiversifiedFallbackIndex(topCandidates.Count, fallbackSeed, state.Calendar.Day);
            return topCandidates[index];
        }

        return SelectFallbackSupplyTarget(state, fallbackSeed);
    }

    private static string SelectFreshVisitTarget(SaveState state, string? preferredTarget, string? fallbackSeed = null)
    {
        var normalizedPreferred = NormalizeNpcTarget(preferredTarget);
        var candidates = new List<string>();
        if (ValidNpcTargets.Contains(normalizedPreferred))
            candidates.Add(normalizedPreferred);

        candidates.AddRange(ValidNpcTargets.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        var ordered = candidates
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var candidate in ordered)
        {
            if (!IsQuestTargetRecentlyUsed(state, "social_visit", candidate))
                return candidate;
        }

        if (ordered.Count == 0)
            return "lewis";

        var index = GetDiversifiedFallbackIndex(ordered.Count, fallbackSeed, state.Calendar.Day);
        return ordered[index];
    }

    private static string TrySelectEventDerivedSupplyTarget(SaveState state)
    {
        var recentEvents = state.TownMemory.Events
            .Where(ev => ev.Day >= state.Calendar.Day - 1)
            .OrderByDescending(ev => ev.Severity)
            .ThenByDescending(ev => ev.Day)
            .ToList();
        if (recentEvents.Count == 0)
            return string.Empty;

        var validSupplyItems = GetValidSupplyItems(state);
        var summaryScanItems = validSupplyItems
            .OrderByDescending(item => item.Length)
            .ThenBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var ev in recentEvents)
        {
            foreach (var tag in ev.Tags ?? Array.Empty<string>())
            {
                var normalizedTag = NormalizeSupplyCandidate(tag);
                if (validSupplyItems.Contains(normalizedTag))
                    return normalizedTag;
            }

            foreach (var item in summaryScanItems)
            {
                if (ev.Summary.Contains(item.Replace("_", " ", StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase)
                    || ev.Summary.Contains(item, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
        }

        return string.Empty;
    }

    private static string TrySelectEventDerivedVisitTarget(SaveState state)
    {
        var recentEvents = state.TownMemory.Events
            .Where(ev => ev.Day >= state.Calendar.Day - 1)
            .OrderByDescending(ev => ev.Severity)
            .ThenByDescending(ev => ev.Day)
            .ToList();
        if (recentEvents.Count == 0)
            return string.Empty;

        foreach (var ev in recentEvents)
        {
            foreach (var tag in ev.Tags ?? Array.Empty<string>())
            {
                var normalizedTag = NormalizeNpcTarget(tag);
                if (ValidNpcTargets.Contains(normalizedTag))
                    return normalizedTag;
            }

            foreach (var npc in ValidNpcTargets)
            {
                var pretty = QuestTextHelper.PrettyName(npc);
                if (ev.Summary.Contains(pretty, StringComparison.OrdinalIgnoreCase))
                    return npc;
            }
        }

        return string.Empty;
    }

    private static bool IsQuestTargetRecentlyUsed(SaveState state, string templateId, string target)
    {
        if (string.IsNullOrWhiteSpace(templateId) || string.IsNullOrWhiteSpace(target))
            return false;

        var normalizedTemplate = templateId.Trim().ToLowerInvariant();
        var normalizedTarget = target.Trim().ToLowerInvariant();

        bool Match(QuestEntry quest) =>
            quest.TemplateId.Equals(normalizedTemplate, StringComparison.OrdinalIgnoreCase)
            && quest.TargetItem.Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase);

        return state.Quests.Available.Any(Match)
            || state.Quests.Active.Any(Match)
            || state.Quests.Completed.TakeLast(8).Any(Match)
            || state.Quests.Failed.TakeLast(8).Any(Match);
    }

    private static string SelectFallbackSupplyTarget(SaveState state, string? fallbackSeed = null)
    {
        var validSupplyItems = GetValidSupplyItems(state);
        var ordered = state.Economy.Crops
            .Where(kv => validSupplyItems.Contains(kv.Key))
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .ThenByDescending(kv => kv.Value.DemandFactor)
            .ThenByDescending(kv => kv.Value.SupplyPressureFactor)
            .Select(kv => NormalizeSupplyCandidate(kv.Key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        ordered.AddRange(GetSeasonalFallbackSupplyItems(state.Calendar.Season)
            .Where(validSupplyItems.Contains));

        if (ordered.Count > 0)
        {
            var topCandidates = ordered.Take(Math.Min(6, ordered.Count)).ToList();
            var index = GetDiversifiedFallbackIndex(topCandidates.Count, fallbackSeed, state.Calendar.Day);
            return topCandidates[index];
        }

        var canonicalFallback = GetSeasonalFallbackSupplyItems(state.Calendar.Season)
            .FirstOrDefault(validSupplyItems.Contains);
        if (!string.IsNullOrWhiteSpace(canonicalFallback))
            return canonicalFallback;

        var anyValid = validSupplyItems
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(anyValid) ? "parsnip" : anyValid;
    }

    private static string SelectFallbackMineResourceTarget(SaveState state, string? fallbackSeed = null)
    {
        var ordered = state.Economy.Crops
            .Where(kv => ValidResources.Contains(kv.Key))
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .ThenByDescending(kv => kv.Value.DemandFactor)
            .ThenByDescending(kv => kv.Value.SupplyPressureFactor)
            .Select(kv => NormalizeSupplyCandidate(kv.Key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        ordered.AddRange(ValidResources.OrderBy(v => v, StringComparer.OrdinalIgnoreCase));

        if (ordered.Count == 0)
            return "copper_ore";

        var topCandidates = ordered.Take(Math.Min(6, ordered.Count)).ToList();
        var index = GetDiversifiedFallbackIndex(topCandidates.Count, fallbackSeed, state.Calendar.Day);
        return topCandidates[index];
    }

    private static string SelectFallbackVisitTarget(SaveState state, string? fallbackSeed = null)
    {
        var sorted = ValidNpcTargets
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (sorted.Count == 0)
            return "lewis";

        var index = GetDiversifiedFallbackIndex(sorted.Count, fallbackSeed, state.Calendar.Day);
        return sorted[index];
    }

    private static int GetDiversifiedFallbackIndex(int count, string? seed, int day)
    {
        if (count <= 1)
            return 0;

        if (!string.IsNullOrWhiteSpace(seed))
        {
            var hash = Math.Abs(seed.GetHashCode());
            return hash % count;
        }

        return Math.Abs(day) % count;
    }

    private static string BuildSocialVisitFactKey(string questId)
    {
        return $"quest:{questId}:social_visit:visited";
    }

    private static bool HasRecordedSocialVisit(SaveState state, QuestEntry quest)
    {
        var key = BuildSocialVisitFactKey(quest.QuestId);
        return state.Facts.Facts.TryGetValue(key, out var fact) && fact.Value;
    }

    private static string NormalizeNpcTarget(string? rawTarget)
    {
        var cleaned = StripRequestedCountTokens(rawTarget);
        if (string.IsNullOrWhiteSpace(cleaned))
            return string.Empty;

        var normalized = cleaned
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace("'", string.Empty, StringComparison.Ordinal)
            .Trim('"', '\'', '.', ',', '!', '?', ';', ':');

        if (normalized.StartsWith("mr_", StringComparison.Ordinal))
            normalized = normalized[3..];
        else if (normalized.StartsWith("mrs_", StringComparison.Ordinal))
            normalized = normalized[4..];
        else if (normalized.StartsWith("ms_", StringComparison.Ordinal))
            normalized = normalized[3..];

        return normalized;
    }

    private static int? TryExtractRequestedCount(string? rawTarget)
    {
        if (string.IsNullOrWhiteSpace(rawTarget))
            return null;

        var match = Regex.Match(
            rawTarget,
            @"(?:^|[\s_])(?:x\s*)?(\d{1,3})(?:\s*x)?(?:$|[\s_])",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (!match.Success)
            return null;
        if (!int.TryParse(match.Groups[1].Value, out var parsed))
            return null;
        if (parsed <= 0)
            return null;

        return Math.Min(parsed, 99);
    }

    private static int ResolveRequestedCountForTemplate(string templateId, int requestedCount, int fallbackCount)
    {
        var clampedRequested = Math.Clamp(requestedCount, 1, 99);
        return templateId switch
        {
            "social_visit" => 1,
            "mine_resource" => Math.Clamp(clampedRequested, 5, 30),
            "deliver_item" => Math.Clamp(clampedRequested, 3, 25),
            "gather_crop" => Math.Clamp(clampedRequested, 5, 30),
            _ => fallbackCount
        };
    }

    private static string StripRequestedCountTokens(string? rawTarget)
    {
        if (string.IsNullOrWhiteSpace(rawTarget))
            return string.Empty;

        var cleaned = rawTarget
            .Trim()
            .ToLowerInvariant()
            .Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal);

        cleaned = Regex.Replace(
            cleaned,
            @"(?:^|[\s])(x\s*)?\d{1,3}(\s*x)?(?=$|[\s])",
            " ",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(
            cleaned,
            @"\b(?:about|around|roughly|approx|approximately|some|few|several|a|an|the|of|qty|quantity|amount|items?)\b",
            " ",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
        return cleaned;
    }

    private static HashSet<string> GetValidSupplyItems(SaveState state)
    {
        var valid = QuestAccessHelper.GetAccessibleSupplyItems(state);
        foreach (var supplemental in SupplementalSupplyItems)
        {
            if (QuestAccessHelper.IsLikelyAccessibleNow(supplemental, state))
                valid.Add(supplemental);
        }
        foreach (var resource in ValidResources)
            valid.Add(resource);

        return valid;
    }

    private static IEnumerable<string> GetSeasonalFallbackSupplyItems(string? season)
    {
        var s = (season ?? string.Empty).Trim().ToLowerInvariant();
        return s switch
        {
            "spring" => new[]
            {
                "parsnip", "potato", "cauliflower", "wild_horseradish", "daffodil", "leek", "dandelion", "sunfish"
            },
            "summer" => new[]
            {
                "tomato", "blueberry", "melon", "corn", "spice_berry", "sweet_pea", "tuna", "tilapia"
            },
            "fall" => new[]
            {
                "pumpkin", "cranberry", "wheat", "yam", "blackberry", "hazelnut", "salmon", "walleye"
            },
            _ => new[]
            {
                "wheat", "winter_root", "crystal_fruit", "snow_yam", "crocus", "holly", "squid", "tuna"
            }
        };
    }

    private static void ApplyRewards(SaveState state, QuestEntry quest)
    {
        // Minimal reward model for now: gold -> positive economy sentiment proxy + issuer rep.
        // (Wallet gold reward is paid to player in CompleteQuestWithChecks.)
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

public sealed class QuestCompletionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RewardGold { get; set; }
}

public sealed class QuestProgressResult
{
    public bool Exists { get; set; }
    public string QuestId { get; set; } = string.Empty;
    public QuestEntry? Quest { get; set; }
    public bool RequiresItems { get; set; }
    public int NeedCount { get; set; }
    public int HaveCount { get; set; }
    public bool IsReadyToComplete { get; set; }

    public static QuestProgressResult NotFound(string questId) => new()
    {
        Exists = false,
        QuestId = questId
    };
}
