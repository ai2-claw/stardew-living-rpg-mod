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
        "publish_rumor"
    };

    private static readonly HashSet<string> AllowedTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        "gather_crop", "deliver_item", "mine_resource", "social_visit"
    };

    private static readonly HashSet<string> AllowedUrgency = new(StringComparer.OrdinalIgnoreCase)
    {
        "low", "medium", "high"
    };

    private readonly RumorBoardService _rumorBoardService;

    public NpcIntentResolver(RumorBoardService rumorBoardService)
    {
        _rumorBoardService = rumorBoardService;
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
            return NpcIntentResolveResult.Rejected($"unknown command '{command}'");

        var npcId = envelope.TryGetProperty("npc_id", out var nEl) ? (nEl.GetString() ?? "unknown") : "unknown";
        var intentId = envelope.TryGetProperty("intent_id", out var iEl) ? (iEl.GetString() ?? string.Empty) : string.Empty;
        if (string.IsNullOrWhiteSpace(intentId))
            return NpcIntentResolveResult.Rejected("missing intent_id");

        if (!envelope.TryGetProperty("arguments", out var args) || args.ValueKind != JsonValueKind.Object)
            return NpcIntentResolveResult.Rejected("missing arguments object");

        if (!string.Equals(command, "propose_quest", StringComparison.OrdinalIgnoreCase))
            return NpcIntentResolveResult.Rejected($"command '{command}' not implemented yet");

        if (!args.TryGetProperty("template_id", out var tEl) || tEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("propose_quest missing template_id");
        if (!args.TryGetProperty("target", out var tarEl) || tarEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("propose_quest missing target");
        if (!args.TryGetProperty("urgency", out var uEl) || uEl.ValueKind != JsonValueKind.String)
            return NpcIntentResolveResult.Rejected("propose_quest missing urgency");

        var templateId = tEl.GetString() ?? string.Empty;
        var target = tarEl.GetString() ?? string.Empty;
        var urgency = uEl.GetString() ?? string.Empty;

        if (!AllowedTemplates.Contains(templateId))
            return NpcIntentResolveResult.Rejected($"invalid template_id '{templateId}'");
        if (!AllowedUrgency.Contains(urgency))
            return NpcIntentResolveResult.Rejected($"invalid urgency '{urgency}'");

        if (HasUnexpectedProposeQuestArgs(args))
            return NpcIntentResolveResult.Rejected("propose_quest contains unexpected argument fields");

        var result = _rumorBoardService.CreateQuestFromNpcProposal(state, npcId, templateId, target, urgency, intentId);
        if (result.IsDuplicate || string.IsNullOrWhiteSpace(result.CreatedQuestId))
            return NpcIntentResolveResult.Duplicate(intentId);

        var fallbackUsed =
            !string.Equals(result.RequestedTemplate, result.AppliedTemplate, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals((result.RequestedTarget ?? string.Empty).Trim(), result.AppliedTarget, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(result.RequestedUrgency, result.AppliedUrgency, StringComparison.OrdinalIgnoreCase);

        return NpcIntentResolveResult.Applied(intentId, command, result.CreatedQuestId!, fallbackUsed, result);
    }

    private static bool HasUnexpectedProposeQuestArgs(JsonElement args)
    {
        foreach (var p in args.EnumerateObject())
        {
            if (p.NameEquals("template_id") || p.NameEquals("target") || p.NameEquals("urgency") || p.NameEquals("reward_hint"))
                continue;
            return true;
        }

        return false;
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
    public string QuestId { get; set; } = string.Empty;
    public bool FallbackUsed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public QuestProposalResult? Proposal { get; set; }

    public static NpcIntentResolveResult None => new() { HasIntent = false };
    public static NpcIntentResolveResult Rejected(string reason) => new() { HasIntent = true, IsRejected = true, Reason = reason };
    public static NpcIntentResolveResult Duplicate(string intentId) => new() { HasIntent = true, IsDuplicate = true, IntentId = intentId };

    public static NpcIntentResolveResult Applied(string intentId, string command, string questId, bool fallbackUsed, QuestProposalResult proposal)
        => new()
        {
            HasIntent = true,
            AppliedOk = true,
            IntentId = intentId,
            Command = command,
            QuestId = questId,
            FallbackUsed = fallbackUsed,
            Proposal = proposal
        };
}
