using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class NpcIntentResolver
{
    private static readonly HashSet<string> AllowedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "propose_quest",
        "adjust_reputation",
        "shift_interest_influence",
        "apply_market_modifier",
        "publish_rumor",
        "publish_article",
        "record_memory_fact",
        "record_town_event",
        "adjust_town_sentiment"
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

    private static readonly HashSet<string> AllowedMemoryCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "preference", "promise", "event", "relationship"
    };

    private readonly RumorBoardService _rumorBoardService;
    private readonly NpcMemoryService _npcMemoryService;
    private readonly TownMemoryService _townMemoryService;
    private readonly bool _strictTemplateValidation;

    public NpcIntentResolver(RumorBoardService rumorBoardService, NpcMemoryService npcMemoryService, TownMemoryService townMemoryService, bool strictTemplateValidation = false)
    {
        _rumorBoardService = rumorBoardService;
        _npcMemoryService = npcMemoryService;
        _townMemoryService = townMemoryService;
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

        if (TryResolveEmbeddedMessageQuestPayload(state, root, out var embeddedResult))
            return embeddedResult;

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
            "record_memory_fact" => ResolveRecordMemoryFact(state, npcId, intentId, args),
            "record_town_event" => ResolveRecordTownEvent(state, npcId, intentId, args),
            "adjust_town_sentiment" => ResolveAdjustTownSentiment(state, npcId, intentId, args),
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
        if (HasUnexpectedArgs(args, "topic", "confidence", "target_group", "title", "content"))
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

        var title = args.TryGetProperty("title", out var titleEl) && titleEl.ValueKind == JsonValueKind.String
            ? (titleEl.GetString() ?? string.Empty).Trim()
            : topic;
        var content = args.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String
            ? (contentEl.GetString() ?? string.Empty).Trim()
            : topic;
        if (string.IsNullOrWhiteSpace(title))
            title = topic;
        if (string.IsNullOrWhiteSpace(content))
            content = topic;

        // Create article instead of full issue
        var article = new NewspaperArticle
        {
            Title = title,
            Content = content,
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

        // Create article
        var article = new NewspaperArticle
        {
            Title = title,
            Content = content,
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

    private NpcIntentResolveResult ResolveRecordMemoryFact(SaveState state, string npcId, string intentId, JsonElement args)
    {
        if (HasUnexpectedArgs(args, "category", "text", "weight", "reason", "target", "summary", "message", "note", "context"))
            return NpcIntentResolveResult.Rejected("record_memory_fact contains unexpected argument fields", "E_ARGUMENTS_UNEXPECTED");

        var category = ResolveMemoryCategory(args);
        if (!AllowedMemoryCategories.Contains(category))
            return NpcIntentResolveResult.Rejected("record_memory_fact missing category", "E_MEMORY_CATEGORY_INVALID");

        var text = ResolveMemoryText(args);
        if (text.Length < 8 || text.Length > 140)
            return NpcIntentResolveResult.Rejected("record_memory_fact text length out of range (8..140)", "E_MEMORY_TEXT_INVALID");

        var weight = 2;
        if (args.TryGetProperty("weight", out var weightEl))
        {
            if (weightEl.ValueKind != JsonValueKind.Number || !weightEl.TryGetInt32(out weight))
                return NpcIntentResolveResult.Rejected("record_memory_fact weight must be an integer", "E_MEMORY_WEIGHT_RANGE");
            if (weight < 1 || weight > 5)
                return NpcIntentResolveResult.Rejected("record_memory_fact weight out of range (1..5)", "E_MEMORY_WEIGHT_RANGE");
        }

        if (!state.NpcMemory.Profiles.TryGetValue(npcId, out var existingProfile))
            existingProfile = null;

        var memoryDailyPrefix = $"memory:fact:day:{state.Calendar.Day}:{npcId}:";
        var todaysMemoryCount = CountFactKeysWithPrefix(state, memoryDailyPrefix);
        if (todaysMemoryCount >= 2)
            return NpcIntentResolveResult.Rejected("record_memory_fact daily cap reached (2)", "E_MEMORY_DAILY_CAP");

        var isDuplicate = existingProfile?.Facts.Any(f =>
            string.Equals(f.Text, text, StringComparison.OrdinalIgnoreCase)) ?? false;
        if (isDuplicate)
            return NpcIntentResolveResult.Rejected("record_memory_fact duplicate text", "E_MEMORY_DUPLICATE");

        _npcMemoryService.WriteFact(state, npcId, category, text, state.Calendar.Day, weight);
        MarkIntentProcessed(state, intentId, npcId, "record_memory_fact");
        state.Facts.Facts[$"memory:fact:{intentId}"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "npc_command"
        };
        state.Facts.Facts[$"memory:fact:day:{state.Calendar.Day}:{npcId}:{intentId}"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "npc_command"
        };

        state.Telemetry.Daily.WorldMutations += 1;
        var outcome = $"memory:{npcId}:{category}";
        var fallbackUsed = !args.TryGetProperty("category", out var categoryEl)
            || categoryEl.ValueKind != JsonValueKind.String
            || !args.TryGetProperty("text", out var textEl)
            || textEl.ValueKind != JsonValueKind.String;
        return NpcIntentResolveResult.Applied(intentId, "record_memory_fact", outcome, fallbackUsed, proposal: null);
    }

    private static string ResolveMemoryCategory(JsonElement args)
    {
        if (args.TryGetProperty("category", out var categoryEl) && categoryEl.ValueKind == JsonValueKind.String)
        {
            var explicitCategory = NormalizeMemoryCategory(categoryEl.GetString());
            if (!string.IsNullOrWhiteSpace(explicitCategory))
                return explicitCategory;
        }

        var source = BuildMemoryInferenceSource(args);
        if (ContainsAnyToken(source,
                "like", "likes", "liked", "love", "loves", "favorite", "favourite", "prefer", "prefers", "enjoy", "enjoys", "dislike", "dislikes", "hate", "hates", "allergic"))
        {
            return "preference";
        }

        if (ContainsAnyToken(source,
                "promise", "promised", "pledge", "pledged", "swore", "will", "i'll", "ill", "going to", "plan", "planned", "intend", "intends", "agreed", "commit", "committed", "owes", "owe"))
        {
            return "promise";
        }

        if (ContainsAnyToken(source,
                "friend", "friends", "trust", "trusted", "trusts", "respect", "respects", "upset", "angry", "apologized", "apologised", "forgave", "forgiven", "close", "distant", "relationship", "dispute", "argument"))
        {
            return "relationship";
        }

        return "event";
    }

    private static string ResolveMemoryText(JsonElement args)
    {
        if (TryReadStringArg(args, "text", out var text))
            return ClampMemoryText(text);
        if (TryReadStringArg(args, "message", out var message))
            return ClampMemoryText(message);
        if (TryReadStringArg(args, "summary", out var summary))
            return ClampMemoryText(summary);
        if (TryReadStringArg(args, "note", out var note))
            return ClampMemoryText(note);

        var hasReason = TryReadStringArg(args, "reason", out var reason);
        var hasTarget = TryReadStringArg(args, "target", out var target);
        if (hasReason && hasTarget)
            return ClampMemoryText($"Regarding {target}: {reason}");
        if (hasReason)
            return ClampMemoryText(reason);
        if (hasTarget)
            return ClampMemoryText($"Note about {target}");

        return string.Empty;
    }

    private static string BuildMemoryInferenceSource(JsonElement args)
    {
        var parts = new List<string>();
        if (TryReadStringArg(args, "text", out var text))
            parts.Add(text);
        if (TryReadStringArg(args, "message", out var message))
            parts.Add(message);
        if (TryReadStringArg(args, "summary", out var summary))
            parts.Add(summary);
        if (TryReadStringArg(args, "note", out var note))
            parts.Add(note);
        if (TryReadStringArg(args, "reason", out var reason))
            parts.Add(reason);
        if (TryReadStringArg(args, "target", out var target))
            parts.Add(target);
        if (TryReadStringArg(args, "context", out var context))
            parts.Add(context);

        return string.Join(' ', parts)
            .Trim()
            .ToLowerInvariant();
    }

    private static string NormalizeMemoryCategory(string? raw)
    {
        var value = (raw ?? string.Empty).Trim().ToLowerInvariant();
        return value switch
        {
            "preference" => "preference",
            "promise" => "promise",
            "relationship" => "relationship",
            "event" => "event",
            "pref" => "preference",
            "rel" => "relationship",
            _ => string.Empty
        };
    }

    private static bool TryReadStringArg(JsonElement args, string key, out string value)
    {
        value = string.Empty;
        if (!args.TryGetProperty(key, out var element) || element.ValueKind != JsonValueKind.String)
            return false;

        value = (element.GetString() ?? string.Empty).Trim();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool ContainsAnyToken(string text, params string[] tokens)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token))
                continue;
            if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string ClampMemoryText(string value)
    {
        var text = (value ?? string.Empty).Trim();
        if (text.Length <= 140)
            return text;

        return text[..140].TrimEnd();
    }

    private NpcIntentResolveResult ResolveRecordTownEvent(SaveState state, string npcId, string intentId, JsonElement args)
    {
        if (!args.TryGetProperty("kind", out var kindEl) || kindEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("record_town_event missing kind", "E_TOWN_EVENT_KIND_INVALID");
        if (!args.TryGetProperty("summary", out var summaryEl) || summaryEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("record_town_event missing summary", "E_TOWN_EVENT_SUMMARY_INVALID");
        if (!args.TryGetProperty("location", out var locationEl) || locationEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("record_town_event missing location", "E_TOWN_EVENT_LOCATION_INVALID");
        if (!args.TryGetProperty("severity", out var severityEl) || severityEl.ValueKind != JsonValueKind.Number || !severityEl.TryGetInt32(out var severity))
            return NpcIntentResolveResult.Rejected("record_town_event missing integer severity", "E_TOWN_EVENT_SEVERITY_RANGE");
        if (!args.TryGetProperty("visibility", out var visibilityEl) || visibilityEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("record_town_event missing visibility", "E_TOWN_EVENT_VISIBILITY_INVALID");
        if (HasUnexpectedArgs(args, "kind", "summary", "location", "severity", "visibility", "tags"))
            return NpcIntentResolveResult.Rejected("record_town_event contains unexpected argument fields", "E_ARGUMENTS_UNEXPECTED");

        var kind = (kindEl.GetString() ?? string.Empty).Trim().ToLowerInvariant();
        if (kind is not ("market" or "social" or "nature" or "incident" or "community"))
            return NpcIntentResolveResult.Rejected($"invalid kind '{kind}'", "E_TOWN_EVENT_KIND_INVALID");

        var summary = (summaryEl.GetString() ?? string.Empty).Trim();
        if (summary.Length < 12 || summary.Length > 160)
            return NpcIntentResolveResult.Rejected("record_town_event summary length out of range (12..160)", "E_TOWN_EVENT_SUMMARY_INVALID");

        var location = (locationEl.GetString() ?? string.Empty).Trim();
        if (location.Length < 2 || location.Length > 40)
            return NpcIntentResolveResult.Rejected("record_town_event location length out of range (2..40)", "E_TOWN_EVENT_LOCATION_INVALID");

        if (severity < 1 || severity > 5)
            return NpcIntentResolveResult.Rejected("record_town_event severity out of range (1..5)", "E_TOWN_EVENT_SEVERITY_RANGE");

        var visibility = (visibilityEl.GetString() ?? string.Empty).Trim().ToLowerInvariant();
        if (visibility is not ("local" or "public"))
            return NpcIntentResolveResult.Rejected($"invalid visibility '{visibility}'", "E_TOWN_EVENT_VISIBILITY_INVALID");

        var townEventDailyPrefix = $"town:event:day:{state.Calendar.Day}:";
        var todayTownEventCount = CountFactKeysWithPrefix(state, townEventDailyPrefix);
        if (todayTownEventCount >= 2)
            return NpcIntentResolveResult.Rejected("record_town_event daily cap reached (2)", "E_TOWN_EVENT_DAILY_CAP");

        var tags = Array.Empty<string>();
        if (args.TryGetProperty("tags", out var tagsEl))
        {
            if (tagsEl.ValueKind != JsonValueKind.Array)
                return NpcIntentResolveResult.Rejected("record_town_event tags must be an array", "E_TOWN_EVENT_TAGS_INVALID");

            var parsed = new List<string>();
            foreach (var tagEl in tagsEl.EnumerateArray())
            {
                if (tagEl.ValueKind != JsonValueKind.String)
                    return NpcIntentResolveResult.Rejected("record_town_event tags must be strings", "E_TOWN_EVENT_TAGS_INVALID");

                var tag = (tagEl.GetString() ?? string.Empty).Trim().ToLowerInvariant();
                if (tag.Length < 2 || tag.Length > 24)
                    return NpcIntentResolveResult.Rejected("record_town_event tag length out of range (2..24)", "E_TOWN_EVENT_TAGS_INVALID");

                parsed.Add(tag);
                if (parsed.Count > 5)
                    return NpcIntentResolveResult.Rejected("record_town_event tags maxItems exceeded (5)", "E_TOWN_EVENT_TAGS_INVALID");
            }

            tags = parsed.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        _townMemoryService.RecordEvent(
            state,
            kind,
            summary,
            location,
            state.Calendar.Day,
            severity,
            visibility,
            tags);

        MarkIntentProcessed(state, intentId, npcId, "record_town_event");
        state.Facts.Facts[$"town:event:{intentId}"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "npc_command"
        };
        state.Facts.Facts[$"town:event:day:{state.Calendar.Day}:{intentId}"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "npc_command"
        };

        state.Telemetry.Daily.WorldMutations += 1;
        var outcome = $"town_event:{kind}";
        return NpcIntentResolveResult.Applied(intentId, "record_town_event", outcome, fallbackUsed: false, proposal: null);
    }

    private static NpcIntentResolveResult ResolveAdjustTownSentiment(SaveState state, string npcId, string intentId, JsonElement args)
    {
        if (!args.TryGetProperty("axis", out var axisEl) || axisEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("adjust_town_sentiment missing axis", "E_SENTIMENT_AXIS_INVALID");
        if (!args.TryGetProperty("delta", out var deltaEl) || deltaEl.ValueKind != JsonValueKind.Number || !deltaEl.TryGetInt32(out var delta))
            return NpcIntentResolveResult.Rejected("adjust_town_sentiment missing integer delta", "E_SENTIMENT_DELTA_RANGE");
        if (HasUnexpectedArgs(args, "axis", "delta", "reason"))
            return NpcIntentResolveResult.Rejected("adjust_town_sentiment contains unexpected argument fields", "E_ARGUMENTS_UNEXPECTED");

        var axis = (axisEl.GetString() ?? string.Empty).Trim().ToLowerInvariant();
        if (axis is not ("economy" or "community" or "environment"))
            return NpcIntentResolveResult.Rejected($"invalid axis '{axis}'", "E_SENTIMENT_AXIS_INVALID");

        if (delta < -5 || delta > 5)
            return NpcIntentResolveResult.Rejected("adjust_town_sentiment delta out of range (-5..5)", "E_SENTIMENT_DELTA_RANGE");

        if (args.TryGetProperty("reason", out var reasonEl))
        {
            if (reasonEl.ValueKind != JsonValueKind.String)
                return NpcIntentResolveResult.Rejected("adjust_town_sentiment reason must be a string", "E_SENTIMENT_REASON_INVALID");
            var reason = (reasonEl.GetString() ?? string.Empty).Trim();
            if (reason.Length > 120)
                return NpcIntentResolveResult.Rejected("adjust_town_sentiment reason length exceeds 120", "E_SENTIMENT_REASON_INVALID");
        }

        var day = state.Calendar.Day;
        var npcAxisPrefix = $"sentiment:npc-axis:{day}:{npcId}:{axis}:";
        var npcAxisAlreadyAppliedToday = HasFactKeyWithPrefix(state, npcAxisPrefix);
        if (npcAxisAlreadyAppliedToday)
            return NpcIntentResolveResult.Rejected("adjust_town_sentiment npc-axis daily cap reached (1)", "E_SENTIMENT_NPC_AXIS_CAP");

        var axisDeltaPrefix = $"sentiment:axis:{day}:{axis}:delta:";
        var runningNet = SumSignedDeltaFacts(state, axisDeltaPrefix);

        var proposedNet = runningNet + delta;
        if (Math.Abs(proposedNet) > 10)
            return NpcIntentResolveResult.Rejected("adjust_town_sentiment daily axis cap exceeded (abs net > 10)", "E_SENTIMENT_DAILY_AXIS_CAP");

        switch (axis)
        {
            case "economy":
                state.Social.TownSentiment.Economy = Math.Clamp(state.Social.TownSentiment.Economy + delta, -100, 100);
                break;
            case "community":
                state.Social.TownSentiment.Community = Math.Clamp(state.Social.TownSentiment.Community + delta, -100, 100);
                break;
            case "environment":
                state.Social.TownSentiment.Environment = Math.Clamp(state.Social.TownSentiment.Environment + delta, -100, 100);
                break;
        }

        MarkIntentProcessed(state, intentId, npcId, "adjust_town_sentiment");
        state.Facts.Facts[$"sentiment:axis:{day}:{axis}:delta:{delta}:{intentId}"] = new FactValue
        {
            Value = true,
            SetDay = day,
            Source = "npc_command"
        };
        state.Facts.Facts[$"sentiment:npc-axis:{day}:{npcId}:{axis}:{intentId}"] = new FactValue
        {
            Value = true,
            SetDay = day,
            Source = "npc_command"
        };

        state.Telemetry.Daily.WorldMutations += 1;
        return NpcIntentResolveResult.Applied(intentId, "adjust_town_sentiment", axis, fallbackUsed: false, proposal: null);
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

    private static int CountFactKeysWithPrefix(SaveState state, string prefix)
    {
        return state.Facts.Facts.Keys.Count(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasFactKeyWithPrefix(SaveState state, string prefix)
    {
        return state.Facts.Facts.Keys.Any(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static int SumSignedDeltaFacts(SaveState state, string prefix)
    {
        var net = 0;
        foreach (var key in state.Facts.Facts.Keys)
        {
            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var rest = key[prefix.Length..];
            var firstColon = rest.IndexOf(':');
            var deltaToken = firstColon < 0 ? rest : rest[..firstColon];
            if (int.TryParse(deltaToken, out var existingDelta))
                net += existingDelta;
        }

        return net;
    }

    private bool TryResolveEmbeddedMessageQuestPayload(SaveState state, JsonElement root, out NpcIntentResolveResult result)
    {
        result = NpcIntentResolveResult.None;

        if (!root.TryGetProperty("npc_id", out var npcIdEl) || npcIdEl.ValueKind != JsonValueKind.String)
            return false;
        if (!root.TryGetProperty("message", out var messageEl) || messageEl.ValueKind != JsonValueKind.String)
            return false;

        var npcId = npcIdEl.GetString() ?? "unknown";
        var message = messageEl.GetString() ?? string.Empty;
        if (!TryExtractEmbeddedJsonObject(message, out var payloadJson))
            return false;

        try
        {
            using var payloadDoc = JsonDocument.Parse(payloadJson);
            if (payloadDoc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            var payload = payloadDoc.RootElement;
            if (!payload.TryGetProperty("template_id", out var templateEl) || templateEl.ValueKind != JsonValueKind.String)
                return false;
            if (!payload.TryGetProperty("target", out var targetEl) || targetEl.ValueKind != JsonValueKind.String)
                return false;
            if (!payload.TryGetProperty("urgency", out var urgencyEl) || urgencyEl.ValueKind != JsonValueKind.String)
                return false;

            var templateId = (templateEl.GetString() ?? string.Empty).Trim();
            var target = (targetEl.GetString() ?? string.Empty).Trim();
            var urgency = (urgencyEl.GetString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(templateId) || string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(urgency))
                return false;

            var intentId = BuildSyntheticIntentId(npcId, "propose_quest", templateId, target, urgency);
            var envelope = BuildEnvelopeFromSynthetic(
                npcId,
                intentId,
                "propose_quest",
                new Dictionary<string, object?>
                {
                    ["template_id"] = templateId,
                    ["target"] = target,
                    ["urgency"] = urgency
                });

            result = ResolveEnvelope(state, envelope);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryExtractEmbeddedJsonObject(string message, out string json)
    {
        json = string.Empty;
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var trimmed = message.Trim();
        var start = trimmed.IndexOf('{');
        if (start < 0)
            return false;

        var candidate = trimmed[start..].Trim();
        if (!candidate.StartsWith("{", StringComparison.Ordinal) || !candidate.EndsWith("}", StringComparison.Ordinal))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(candidate);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            json = candidate;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildSyntheticIntentId(string npcId, string command, params string[] parts)
    {
        var keyBuilder = new StringBuilder()
            .Append((npcId ?? "unknown").Trim().ToLowerInvariant())
            .Append('|')
            .Append((command ?? "unknown").Trim().ToLowerInvariant());

        foreach (var part in parts)
        {
            keyBuilder.Append('|').Append((part ?? string.Empty).Trim().ToLowerInvariant());
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyBuilder.ToString()));
        var token = Convert.ToHexString(hash).ToLowerInvariant();
        return $"synth_{token[..24]}";
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

    private static JsonElement BuildEnvelopeFromSynthetic(string npcId, string intentId, string commandName, Dictionary<string, object?> args)
    {
        var obj = new Dictionary<string, object?>
        {
            ["intent_id"] = intentId,
            ["npc_id"] = npcId,
            ["command"] = commandName,
            ["arguments"] = args
        };

        var json = JsonSerializer.Serialize(obj);
        return JsonDocument.Parse(json).RootElement.Clone();
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
