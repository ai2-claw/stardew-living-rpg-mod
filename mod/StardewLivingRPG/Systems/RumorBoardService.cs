using StardewLivingRPG.State;
using StardewLivingRPG.Utils;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class RumorBoardService
{
    private const int MaxAvailableBoardQuests = 4;
    private const int MaxActiveBoardQuests = 4;
    private const string TownHallStatusTriggered = "anchor:town_hall_crisis:status:triggered";
    private const string TownHallStatusResolved = "anchor:town_hall_crisis:status:resolved";

    private static readonly HashSet<string> ValidCrops = new(StringComparer.OrdinalIgnoreCase)
    {
        "parsnip", "potato", "cauliflower", "blueberry", "melon", "pumpkin", "cranberry", "corn", "wheat", "tomato"
    };

    private static readonly HashSet<string> ValidResources = new(StringComparer.OrdinalIgnoreCase)
    {
        "copper_ore", "iron_ore", "gold_ore", "coal", "quartz", "amethyst", "topaz"
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

    private static readonly Dictionary<string, string> CropAliases = new(StringComparer.OrdinalIgnoreCase)
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
        ["tomatoes"] = "tomato"
    };

    private static readonly Dictionary<string, int> ResourceUnitValues = new(StringComparer.OrdinalIgnoreCase)
    {
        ["copper_ore"] = 75,
        ["iron_ore"] = 150,
        ["gold_ore"] = 250,
        ["coal"] = 150,
        ["quartz"] = 50,
        ["amethyst"] = 100,
        ["topaz"] = 80
    };

    public void RefreshDailyRumors(SaveState state)
    {
        // Keep active quests untouched; rotate available list daily.
        state.Quests.Available.Clear();

        var allowParsnip = IsParsnipCrisisActive(state);
        var eventCropTarget = TrySelectEventDerivedCropTarget(state);
        var crop = SelectFreshCropTarget(
            state,
            allowParsnip,
            preferredTarget: eventCropTarget,
            fallbackSeed: $"daily_crop_{state.Calendar.Day}");

        var eventVisitTarget = TrySelectEventDerivedVisitTarget(state);
        var visitTarget = SelectFreshVisitTarget(
            state,
            preferredTarget: eventVisitTarget,
            fallbackSeed: $"daily_social_{state.Calendar.Day}");
        var visitName = QuestTextHelper.PrettyName(visitTarget);

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
            Summary = $"Rumor Mill: Check in with {visitName} and brighten their day.",
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
            return new QuestCompletionResult { Success = false, Message = $"Active quest not found: {questId}" };

        var quest = progress.Quest!;

        if (!progress.IsReadyToComplete)
        {
            if (string.Equals(quest.TemplateId, "social_visit", StringComparison.OrdinalIgnoreCase))
            {
                var visitTarget = QuestTextHelper.PrettyName(quest.TargetItem);
                return new QuestCompletionResult
                {
                    Success = false,
                    Message = $"Visit {visitTarget} first, then complete this request."
                };
            }

            return new QuestCompletionResult
            {
                Success = false,
                Message = $"Request not ready yet: {QuestTextHelper.BuildQuestTitle(quest)}."
            };
        }

        if (progress.RequiresItems && progress.HaveCount < progress.NeedCount)
        {
            return new QuestCompletionResult
            {
                Success = false,
                Message = $"Need {progress.NeedCount} {quest.TargetItem}, but only have {progress.HaveCount}."
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

        var consumedPart = progress.RequiresItems ? $", consumed {consumed} {quest.TargetItem}" : string.Empty;
        var title = QuestTextHelper.BuildQuestTitle(quest);
        return new QuestCompletionResult
        {
            Success = true,
            Message = $"Completed request: {title} (+{reward}g{consumedPart})",
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
        string intentKey)
    {
        if (state.Facts.ProcessedIntents.ContainsKey(intentKey))
            return QuestProposalResult.Duplicate;

        var safeTemplate = NormalizeTemplate(templateId);
        var safeTarget = NormalizeTargetForTemplate(state, safeTemplate, target, intentKey);
        var safeUrgency = NormalizeUrgency(urgency);

        var (count, minRewardGold, expiresDelta) = BoundsForTemplateAndUrgency(safeTemplate, safeUrgency);
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
            Issuer = "lewis",
            ExpiresDay = state.Calendar.Day + expiresDelta,
            Summary = $"Mayor request ({safeUrgency}): {BuildSummary(safeTemplate, safeTarget, count)}",
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
        foreach (var item in player.Items)
        {
            if (item is null)
                continue;

            var itemName = NormalizeItemKey(item.DisplayName);
            if (!string.Equals(itemName, normalizedTarget, StringComparison.Ordinal))
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

        for (var i = 0; i < player.Items.Count && needed > 0; i++)
        {
            var item = player.Items[i];
            if (item is null)
                continue;

            var itemName = NormalizeItemKey(item.DisplayName);
            if (!string.Equals(itemName, normalizedTarget, StringComparison.Ordinal))
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

        return value.Trim().ToLowerInvariant().Replace(" ", "_");
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
            "social_visit" => $"Visit {target} and bring a thoughtful gift.",
            _ => $"Supply {target} x{count} for the town market."
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
        var multiplier = ResolveRewardMultiplier(templateId, urgency);
        var scaledReward = (int)MathF.Round(marketValue * multiplier);
        var roundedReward = RoundToNearest(scaledReward, 25);

        return Math.Clamp(Math.Max(minRewardGold, roundedReward), minRewardGold, 12000);
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

        return 80;
    }

    private static int RoundToNearest(int value, int step)
    {
        if (step <= 1)
            return Math.Max(0, value);

        return (int)MathF.Round(value / (float)step) * step;
    }

    private static string NormalizeTargetForTemplate(SaveState state, string templateId, string rawTarget, string? fallbackSeed = null)
    {
        var cropTarget = NormalizeCropCandidate(rawTarget);
        var npcTarget = NormalizeNpcTarget(rawTarget);
        var allowParsnip = IsParsnipCrisisActive(state);

        return templateId switch
        {
            "gather_crop" => NormalizeCropTargetOrFallback(state, cropTarget, allowParsnip, fallbackSeed),
            "deliver_item" => NormalizeCropTargetOrFallback(state, cropTarget, allowParsnip, fallbackSeed),
            "mine_resource" => ValidResources.Contains(cropTarget) ? cropTarget : "copper_ore",
            "social_visit" => ValidNpcTargets.Contains(npcTarget) ? npcTarget : SelectFallbackVisitTarget(state, fallbackSeed),
            _ => NormalizeCropTargetOrFallback(state, cropTarget, allowParsnip, fallbackSeed)
        };
    }

    private static string NormalizeCropTargetOrFallback(SaveState state, string candidate, bool allowParsnip, string? fallbackSeed = null)
    {
        if (ValidCrops.Contains(candidate))
        {
            if (candidate.Equals("parsnip", StringComparison.OrdinalIgnoreCase) && !allowParsnip)
                return SelectFallbackCropTarget(state, allowParsnip: false, fallbackSeed);

            return candidate;
        }

        return SelectFallbackCropTarget(state, allowParsnip, fallbackSeed);
    }

    private static string NormalizeCropCandidate(string? rawTarget)
    {
        var t = (rawTarget ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "_", StringComparison.Ordinal)
            .Trim('"', '\'', '.', ',', '!', '?', ';', ':');

        if (CropAliases.TryGetValue(t, out var alias))
            return alias;

        if (t.EndsWith("ies", StringComparison.Ordinal) && t.Length > 3)
        {
            var singularY = t[..^3] + "y";
            if (ValidCrops.Contains(singularY))
                return singularY;
        }

        if (t.EndsWith("s", StringComparison.Ordinal) && t.Length > 1)
        {
            var singular = t[..^1];
            if (ValidCrops.Contains(singular))
                return singular;
        }

        return t;
    }

    private static string SelectFreshCropTarget(SaveState state, bool allowParsnip, string? preferredTarget, string? fallbackSeed = null)
    {
        var candidates = new List<string>();
        var preferred = NormalizeCropCandidate(preferredTarget);
        if (ValidCrops.Contains(preferred))
            candidates.Add(preferred);

        candidates.AddRange(state.Economy.Crops
            .Where(kv => ValidCrops.Contains(kv.Key))
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .ThenByDescending(kv => kv.Value.DemandFactor)
            .ThenByDescending(kv => kv.Value.SupplyPressureFactor)
            .Select(kv => kv.Key.ToLowerInvariant()));

        candidates.Add(GetSeasonalFallbackCrop(state.Calendar.Season, allowParsnip));

        var ordered = candidates
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Where(c => allowParsnip || !c.Equals("parsnip", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var candidate in ordered)
        {
            if (!IsQuestTargetRecentlyUsed(state, "gather_crop", candidate))
                return candidate;
        }

        if (ordered.Count > 0)
        {
            var topCandidates = ordered.Take(Math.Min(4, ordered.Count)).ToList();
            var index = GetDiversifiedFallbackIndex(topCandidates.Count, fallbackSeed, state.Calendar.Day);
            return topCandidates[index];
        }

        return SelectFallbackCropTarget(state, allowParsnip, fallbackSeed);
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

    private static string TrySelectEventDerivedCropTarget(SaveState state)
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
                var normalizedTag = NormalizeCropCandidate(tag);
                if (ValidCrops.Contains(normalizedTag))
                    return normalizedTag;
            }

            foreach (var crop in ValidCrops)
            {
                if (ev.Summary.Contains(crop.Replace("_", " ", StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase)
                    || ev.Summary.Contains(crop, StringComparison.OrdinalIgnoreCase))
                {
                    return crop.ToLowerInvariant();
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

    private static string SelectFallbackCropTarget(SaveState state, bool allowParsnip, string? fallbackSeed = null)
    {
        var ordered = state.Economy.Crops
            .Where(kv => ValidCrops.Contains(kv.Key))
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .ThenByDescending(kv => kv.Value.DemandFactor)
            .ThenByDescending(kv => kv.Value.SupplyPressureFactor)
            .Select(kv => kv.Key.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!allowParsnip)
            ordered = ordered.Where(c => !c.Equals("parsnip", StringComparison.OrdinalIgnoreCase)).ToList();

        if (ordered.Count > 0)
        {
            var topCandidates = ordered.Take(Math.Min(4, ordered.Count)).ToList();
            var index = GetDiversifiedFallbackIndex(topCandidates.Count, fallbackSeed, state.Calendar.Day);
            return topCandidates[index];
        }

        return GetSeasonalFallbackCrop(state.Calendar.Season, allowParsnip);
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
        if (string.IsNullOrWhiteSpace(rawTarget))
            return string.Empty;

        var normalized = rawTarget
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

    private static string GetSeasonalFallbackCrop(string? season, bool allowParsnip)
    {
        var s = (season ?? string.Empty).Trim().ToLowerInvariant();
        if (allowParsnip)
        {
            return s switch
            {
                "spring" => "parsnip",
                "summer" => "tomato",
                "fall" => "pumpkin",
                _ => "wheat"
            };
        }

        return s switch
        {
            "spring" => "potato",
            "summer" => "tomato",
            "fall" => "pumpkin",
            _ => "wheat"
        };
    }

    private static bool IsParsnipCrisisActive(SaveState state)
    {
        if (!state.Facts.Facts.TryGetValue(TownHallStatusTriggered, out var triggered) || !triggered.Value)
            return false;

        if (state.Facts.Facts.TryGetValue(TownHallStatusResolved, out var resolved) && resolved.Value)
            return false;

        if (!state.Economy.Crops.TryGetValue("parsnip", out var parsnip))
            return false;

        var topByScarcity = state.Economy.Crops
            .Where(kv => ValidCrops.Contains(kv.Key))
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .ThenByDescending(kv => kv.Value.DemandFactor)
            .Select(kv => kv.Key)
            .FirstOrDefault();

        if (!string.Equals(topByScarcity, "parsnip", StringComparison.OrdinalIgnoreCase))
            return false;

        var secondScarcity = state.Economy.Crops
            .Where(kv => ValidCrops.Contains(kv.Key) && !kv.Key.Equals("parsnip", StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Value.ScarcityBonus)
            .DefaultIfEmpty(0f)
            .Max();

        var scarcityLead = parsnip.ScarcityBonus - secondScarcity;
        return parsnip.DemandFactor >= 1.04f
            && parsnip.ScarcityBonus >= 0.04f
            && scarcityLead >= 0.01f;
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
