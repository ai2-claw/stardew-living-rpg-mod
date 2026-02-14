using System.Text.Json;
using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class NpcIntentResolver
{
    private const int MaxNpcPublishCombinedCharacters = 100;

    private static readonly HashSet<string> AllowedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "propose_quest",
        "adjust_reputation",
        "shift_interest_influence",
        "apply_market_modifier",
        "publish_rumor",
        "publish_article"
    };

    private static readonly HashSet<string> AllowedTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        "gather_crop", "deliver_item", "mine_resource", "social_visit"
    };

    private static readonly HashSet<string> AllowedUrgency = new(StringComparer.OrdinalIgnoreCase)
    {
        "low", "medium", "high"
    };

    private static readonly HashSet<string> AllowedInterests = new(StringComparer.OrdinalIgnoreCase)
    {
        "farmers_circle", "shopkeepers_guild", "adventurers_club", "nature_keepers"
    };

    private readonly RumorBoardService _rumorBoardService;
    private readonly bool _strictTemplateValidation;

    public NpcIntentResolver(RumorBoardService rumorBoardService, bool strictTemplateValidation = false)
    {
        _rumorBoardService = rumorBoardService;
        _strictTemplateValidation = strictTemplateValidation;
    }

    public NpcIntentResolveResult ResolveFromStreamLine(SaveState state, string line)
    {
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;

        // Preferred schema envelope
        if (root.TryGetProperty("intent_id", out _)
            && root.TryGetProperty("command", out var commandNameEl)
            && commandNameEl.ValueKind == JsonValueKind.String)
        {
            return ResolveEnvelope(state, root);
        }

        // Backward-compatible Player2 command array format
        if (root.TryGetProperty("command", out var commandArray) && commandArray.ValueKind == JsonValueKind.Array)
        {
            var npcId = root.TryGetProperty("npc_id", out var npcIdEl) ? (npcIdEl.GetString() ?? "unknown") : "unknown";

            foreach (var cmd in commandArray.EnumerateArray())
            {
                if (!cmd.TryGetProperty("name", out var nameEl) || nameEl.ValueKind != JsonValueKind.String)
                    continue;

                if (!cmd.TryGetProperty("arguments", out var argsEl))
                    continue;

                var envelope = BuildEnvelopeFromLegacy(npcId, nameEl.GetString() ?? string.Empty, argsEl);
                return ResolveEnvelope(state, envelope);
            }
        }

        return NpcIntentResolveResult.None;
    }

    private NpcIntentResolveResult ResolveEnvelope(SaveState state, JsonElement envelope)
    {
        var command = envelope.TryGetProperty("command", out var cEl) ? (cEl.GetString() ?? string.Empty) : string.Empty;
        if (!AllowedCommands.Contains(command))
            return NpcIntentResolveResult.Rejected($"unknown command '{command}'", "E_COMMAND_UNKNOWN");

        var npcId = envelope.TryGetProperty("npc_id", out var nEl) ? (nEl.GetString() ?? "unknown") : "unknown";
        var intentId = envelope.TryGetProperty("intent_id", out var iEl) ? (iEl.GetString() ?? string.Empty) : string.Empty;
        if (string.IsNullOrWhiteSpace(intentId))
            return NpcIntentResolveResult.Rejected("missing intent_id", "E_INTENT_ID_MISSING");

        if (state.Facts.ProcessedIntents.ContainsKey(intentId))
            return NpcIntentResolveResult.Duplicate(intentId);

        if (!envelope.TryGetProperty("arguments", out var args) || args.ValueKind != JsonValueKind.Object)
            return NpcIntentResolveResult.Rejected("missing arguments object", "E_ARGUMENTS_MISSING");

        return command.ToLowerInvariant() switch
        {
            "propose_quest" => ResolveProposeQuest(state, npcId, intentId, args),
            "adjust_reputation" => ResolveAdjustReputation(state, npcId, intentId, args),
            "shift_interest_influence" => ResolveShiftInterestInfluence(state, npcId, intentId, args),
            "apply_market_modifier" => ResolveApplyMarketModifier(state, npcId, intentId, args),
            "publish_rumor" => ResolvePublishRumor(state, npcId, intentId, args),
            "publish_article" => ResolvePublishArticle(state, npcId, intentId, args),
            _ => NpcIntentResolveResult.Rejected($"unhandled command '{command}'")
        };
    }

    private NpcIntentResolveResult ResolveProposeQuest(SaveState state, string npcId, string intentId, JsonElement args)
    {
        if (!args.TryGetProperty("template_id", out var tEl) || tEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("propose_quest missing template_id");
        if (!args.TryGetProperty("target", out var tarEl) || tarEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("propose_quest missing target");
        if (!args.TryGetProperty("urgency", out var uEl) || uEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("propose_quest missing urgency");

        var rawTemplateId = tEl.GetString() ?? string.Empty;
        var target = tarEl.GetString() ?? string.Empty;
        var urgency = uEl.GetString() ?? string.Empty;

        var templateId = (!_strictTemplateValidation && TryRepairTemplateId(rawTemplateId, out var repairedTemplate))
            ? repairedTemplate
            : rawTemplateId;

        if (!AllowedTemplates.Contains(templateId))
            return NpcIntentResolveResult.Rejected($"invalid template_id '{rawTemplateId}'", "E_TEMPLATE_INVALID");
        if (!AllowedUrgency.Contains(urgency))
            return NpcIntentResolveResult.Rejected($"invalid urgency '{urgency}'", "E_URGENCY_INVALID");
        if (HasUnexpectedArgs(args, "template_id", "target", "urgency", "reward_hint"))
            return NpcIntentResolveResult.Rejected("propose_quest contains unexpected argument fields", "E_ARGUMENTS_UNEXPECTED");

        var result = _rumorBoardService.CreateQuestFromNpcProposal(state, npcId, templateId, target, urgency, intentId);
        if (result.IsDuplicate || string.IsNullOrWhiteSpace(result.CreatedQuestId))
            return NpcIntentResolveResult.Duplicate(intentId);

        var fallbackUsed =
            !string.Equals(result.RequestedTemplate, result.AppliedTemplate, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals((result.RequestedTarget ?? string.Empty).Trim(), result.AppliedTarget, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(result.RequestedUrgency, result.AppliedUrgency, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(rawTemplateId, templateId, StringComparison.OrdinalIgnoreCase);

        return NpcIntentResolveResult.Applied(intentId, "propose_quest", result.CreatedQuestId!, fallbackUsed, result);
    }

    private static NpcIntentResolveResult ResolveAdjustReputation(SaveState state, string npcId, string intentId, JsonElement args)
    {
        if (!args.TryGetProperty("target", out var tEl) || tEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("adjust_reputation missing target");
        if (!args.TryGetProperty("delta", out var dEl) || dEl.ValueKind != JsonValueKind.Number || !dEl.TryGetInt32(out var delta))
            return NpcIntentResolveResult.Rejected("adjust_reputation missing integer delta");
        if (delta < -10 || delta > 10)
            return NpcIntentResolveResult.Rejected("adjust_reputation delta out of range (-10..10)", "E_DELTA_RANGE");
        if (HasUnexpectedArgs(args, "target", "delta", "reason"))
            return NpcIntentResolveResult.Rejected("adjust_reputation contains unexpected argument fields");

        var target = (tEl.GetString() ?? "unknown").Trim().ToLowerInvariant();
        state.Social.NpcReputation.TryGetValue(target, out var current);
        state.Social.NpcReputation[target] = Math.Clamp(current + delta, -100, 100);

        MarkIntentProcessed(state, intentId, npcId, "adjust_reputation");
        state.Facts.Facts[$"npc:{target}:rep_adjusted:{intentId}"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "npc_command"
        };

        state.Telemetry.Daily.WorldMutations += 1;
        return NpcIntentResolveResult.Applied(intentId, "adjust_reputation", target, fallbackUsed: false, proposal: null);
    }

    private static NpcIntentResolveResult ResolveShiftInterestInfluence(SaveState state, string npcId, string intentId, JsonElement args)
    {
        if (!args.TryGetProperty("interest", out var iEl) || iEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("shift_interest_influence missing interest");
        if (!args.TryGetProperty("delta", out var dEl) || dEl.ValueKind != JsonValueKind.Number || !dEl.TryGetInt32(out var delta))
            return NpcIntentResolveResult.Rejected("shift_interest_influence missing integer delta");
        if (delta < -5 || delta > 5)
            return NpcIntentResolveResult.Rejected("shift_interest_influence delta out of range (-5..5)");
        if (HasUnexpectedArgs(args, "interest", "delta", "reason"))
            return NpcIntentResolveResult.Rejected("shift_interest_influence contains unexpected argument fields");

        var interest = (iEl.GetString() ?? "").Trim().ToLowerInvariant();
        if (!AllowedInterests.Contains(interest))
            return NpcIntentResolveResult.Rejected($"invalid interest '{interest}'", "E_INTEREST_INVALID");

        if (!state.Social.Interests.TryGetValue(interest, out var interestState))
        {
            interestState = new InterestState();
            state.Social.Interests[interest] = interestState;
        }

        interestState.Influence = Math.Clamp(interestState.Influence + delta, -100, 100);

        MarkIntentProcessed(state, intentId, npcId, "shift_interest_influence");
        state.Facts.Facts[$"interest:{interest}:shifted:{intentId}"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "npc_command"
        };

        state.Telemetry.Daily.WorldMutations += 1;
        return NpcIntentResolveResult.Applied(intentId, "shift_interest_influence", interest, fallbackUsed: false, proposal: null);
    }

    private static NpcIntentResolveResult ResolveApplyMarketModifier(SaveState state, string npcId, string intentId, JsonElement args)
    {
        if (!args.TryGetProperty("crop", out var cEl) || cEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("apply_market_modifier missing crop");
        if (!args.TryGetProperty("delta_pct", out var dEl) || dEl.ValueKind != JsonValueKind.Number || !dEl.TryGetSingle(out var deltaPct))
            return NpcIntentResolveResult.Rejected("apply_market_modifier missing numeric delta_pct");
        if (!args.TryGetProperty("duration_days", out var durEl) || durEl.ValueKind != JsonValueKind.Number || !durEl.TryGetInt32(out var durationDays))
            return NpcIntentResolveResult.Rejected("apply_market_modifier missing integer duration_days");
        if (deltaPct < -0.15f || deltaPct > 0.15f)
            return NpcIntentResolveResult.Rejected("apply_market_modifier delta_pct out of range (-0.15..0.15)", "E_DELTA_PCT_RANGE");
        if (durationDays < 1 || durationDays > 7)
            return NpcIntentResolveResult.Rejected("apply_market_modifier duration_days out of range (1..7)");
        if (HasUnexpectedArgs(args, "crop", "delta_pct", "duration_days", "reason"))
            return NpcIntentResolveResult.Rejected("apply_market_modifier contains unexpected argument fields");

        var crop = (cEl.GetString() ?? "parsnip").Trim().ToLowerInvariant();
        state.Economy.MarketEvents.Add(new MarketEventEntry
        {
            Id = $"evt_{intentId[..Math.Min(12, intentId.Length)]}",
            Type = "npc_market_modifier",
            Crop = crop,
            DeltaPct = deltaPct,
            StartDay = state.Calendar.Day,
            EndDay = state.Calendar.Day + durationDays
        });

        MarkIntentProcessed(state, intentId, npcId, "apply_market_modifier");
        state.Facts.Facts[$"market:modifier:{intentId}"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "npc_command"
        };

        state.Telemetry.Daily.WorldMutations += 1;
        return NpcIntentResolveResult.Applied(intentId, "apply_market_modifier", crop, fallbackUsed: false, proposal: null);
    }

    private static NpcIntentResolveResult ResolvePublishRumor(SaveState state, string npcId, string intentId, JsonElement args)
    {
        if (!args.TryGetProperty("topic", out var tEl) || tEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("publish_rumor missing topic");
        if (!args.TryGetProperty("confidence", out var cEl) || cEl.ValueKind != JsonValueKind.Number || !cEl.TryGetSingle(out var confidence))
            return NpcIntentResolveResult.Rejected("publish_rumor missing numeric confidence");
        if (!args.TryGetProperty("target_group", out var gEl) || gEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("publish_rumor missing target_group");
        if (confidence < 0f || confidence > 1f)
            return NpcIntentResolveResult.Rejected("publish_rumor confidence out of range (0..1)", "E_CONFIDENCE_RANGE");
        if (HasUnexpectedArgs(args, "topic", "confidence", "target_group"))
            return NpcIntentResolveResult.Rejected("publish_rumor contains unexpected argument fields");

        var topic = (tEl.GetString() ?? "town news").Trim();
        var group = (gEl.GetString() ?? "town").Trim();
        if (string.IsNullOrWhiteSpace(topic))
            return NpcIntentResolveResult.Rejected("publish_rumor topic cannot be empty");
        if (string.IsNullOrWhiteSpace(group))
            group = "town";

        // Check daily rumor limit (1 per day)
        var todayRumorCount = state.Newspaper.Articles.Count(a =>
            a.Day == state.Calendar.Day &&
            a.Category.Equals("social", StringComparison.OrdinalIgnoreCase));
        if (todayRumorCount >= 1)
            return NpcIntentResolveResult.Rejected("rumor limit reached for today (1)", "E_RUMOR_LIMIT");

        var title = $"Rumor: {topic}";
        var content = $"Word on the street among {group}: {topic}.";
        if (!TryClampNpcPublishContent(title, content, out var clampedContent))
            return NpcIntentResolveResult.Rejected("publish_rumor exceeds max 100 characters (title + content)", "E_PUBLISH_LENGTH");

        // Create article instead of full issue
        var article = new NewspaperArticle
        {
            Title = title,
            Content = clampedContent,
            Category = "social",
            SourceNpc = npcId,
            IsNpcPublished = true,
            Day = state.Calendar.Day,
            ExpirationDay = state.Calendar.Day + 3
        };

        state.Newspaper.Articles.Add(article);

        MarkIntentProcessed(state, intentId, npcId, "publish_rumor");
        state.Facts.Facts[$"rumor:published:{intentId}"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "npc_command"
        };

        state.Telemetry.Daily.WorldMutations += 1;
        return NpcIntentResolveResult.Applied(intentId, "publish_rumor", title, fallbackUsed: false, proposal: null);
    }

    private static NpcIntentResolveResult ResolvePublishArticle(SaveState state, string npcId, string intentId, JsonElement args)
    {
        if (!args.TryGetProperty("title", out var tEl) || tEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("publish_article missing title");
        if (!args.TryGetProperty("content", out var cEl) || cEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("publish_article missing content");
        if (!args.TryGetProperty("category", out var catEl) || catEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("publish_article missing category");

        var category = catEl.GetString() ?? "community";

        // Validate category
        var allowedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "community", "market", "social", "nature"
        };

        if (!allowedCategories.Contains(category))
            return NpcIntentResolveResult.Rejected($"invalid category '{category}'", "E_CATEGORY_INVALID");

        if (HasUnexpectedArgs(args, "title", "content", "category"))
            return NpcIntentResolveResult.Rejected("publish_article contains unexpected argument fields");

        var title = (tEl.GetString() ?? string.Empty).Trim();
        var content = (cEl.GetString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            return NpcIntentResolveResult.Rejected("publish_article title/content cannot be empty");

        // Check daily NPC article limit (2 per day, separate from rumors)
        var todayNpcArticleCount = state.Newspaper.Articles.Count(a =>
            a.Day == state.Calendar.Day &&
            !a.Category.Equals("social", StringComparison.OrdinalIgnoreCase));
        if (todayNpcArticleCount >= 2)
            return NpcIntentResolveResult.Rejected("NPC article limit reached for today (2)", "E_ARTICLE_LIMIT");

        if (!TryClampNpcPublishContent(title, content, out var clampedContent))
            return NpcIntentResolveResult.Rejected("publish_article exceeds max 100 characters (title + content)", "E_PUBLISH_LENGTH");

        // Create article
        var article = new NewspaperArticle
        {
            Title = title,
            Content = clampedContent,
            Category = category,
            SourceNpc = npcId,
            IsNpcPublished = true,
            Day = state.Calendar.Day,
            ExpirationDay = state.Calendar.Day + 14 // 2 weeks
        };

        // Add to Articles collection (not Issues)
        state.Newspaper.Articles.Add(article);

        MarkIntentProcessed(state, intentId, npcId, "publish_article");

        return NpcIntentResolveResult.Applied(intentId, "publish_article", title, fallbackUsed: false, proposal: null);
    }

    private static bool TryClampNpcPublishContent(string title, string content, out string clampedContent)
    {
        clampedContent = (content ?? string.Empty).Trim();
        var cleanTitle = (title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleanTitle) || string.IsNullOrWhiteSpace(clampedContent))
            return false;

        var remaining = MaxNpcPublishCombinedCharacters - cleanTitle.Length;
        if (remaining <= 0)
            return false;

        if (clampedContent.Length > remaining)
            clampedContent = clampedContent[..remaining].Trim();

        return !string.IsNullOrWhiteSpace(clampedContent);
    }

    private static bool HasUnexpectedArgs(JsonElement args, params string[] allowedKeys)
    {
        var set = new HashSet<string>(allowedKeys, StringComparer.OrdinalIgnoreCase);
        foreach (var p in args.EnumerateObject())
        {
            if (set.Contains(p.Name))
                continue;
            return true;
        }

        return false;
    }

    private static bool TryRepairTemplateId(string rawTemplateId, out string repaired)
    {
        repaired = rawTemplateId;
        var t = (rawTemplateId ?? string.Empty).Trim().ToLowerInvariant();

        if (AllowedTemplates.Contains(t))
        {
            repaired = t;
            return true;
        }

        if (!t.StartsWith("quest_", StringComparison.OrdinalIgnoreCase))
            return false;

        if (t.Contains("gather", StringComparison.OrdinalIgnoreCase))
        {
            repaired = "gather_crop";
            return true;
        }

        if (t.Contains("deliver", StringComparison.OrdinalIgnoreCase))
        {
            repaired = "deliver_item";
            return true;
        }

        if (t.Contains("mine", StringComparison.OrdinalIgnoreCase))
        {
            repaired = "mine_resource";
            return true;
        }

        if (t.Contains("social", StringComparison.OrdinalIgnoreCase) || t.Contains("visit", StringComparison.OrdinalIgnoreCase))
        {
            repaired = "social_visit";
            return true;
        }

        return false;
    }

    private static void MarkIntentProcessed(SaveState state, string intentId, string npcId, string command)
    {
        state.Facts.ProcessedIntents[intentId] = new ProcessedIntentValue
        {
            Day = state.Calendar.Day,
            NpcId = npcId,
            Command = command,
            Status = "applied"
        };
    }

    private static JsonElement BuildEnvelopeFromLegacy(string npcId, string commandName, JsonElement args)
    {
        var intentId = Guid.NewGuid().ToString("N");

        using var doc = JsonDocument.Parse(args.ValueKind switch
        {
            JsonValueKind.String => args.GetString() ?? "{}",
            JsonValueKind.Object => args.GetRawText(),
            _ => "{}"
        });

        var obj = new Dictionary<string, object?>
        {
            ["intent_id"] = intentId,
            ["npc_id"] = npcId,
            ["command"] = commandName,
            ["arguments"] = JsonSerializer.Deserialize<object>(doc.RootElement.GetRawText())
        };

        var json = JsonSerializer.Serialize(obj);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}

public sealed class NpcIntentResolveResult
{
    public bool HasIntent { get; set; }
    public bool AppliedOk { get; set; }
    public bool IsDuplicate { get; set; }
    public bool IsRejected { get; set; }
    public string IntentId { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string OutcomeId { get; set; } = string.Empty;
    public bool FallbackUsed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public QuestProposalResult? Proposal { get; set; }

    public static NpcIntentResolveResult None => new() { HasIntent = false };
    public static NpcIntentResolveResult Rejected(string reason, string code = "E_REJECT") => new() { HasIntent = true, IsRejected = true, Reason = reason, ReasonCode = code };
    public static NpcIntentResolveResult Duplicate(string intentId) => new() { HasIntent = true, IsDuplicate = true, IntentId = intentId };

    public static NpcIntentResolveResult Applied(string intentId, string command, string outcomeId, bool fallbackUsed, QuestProposalResult? proposal)
        => new()
        {
            HasIntent = true,
            AppliedOk = true,
            IntentId = intentId,
            Command = command,
            OutcomeId = outcomeId,
            FallbackUsed = fallbackUsed,
            Proposal = proposal
        };
}
