namespace StardewLivingRPG.Systems;

public sealed class CommandPolicyService
{
    private static readonly string[] KnownCommands =
    {
        "propose_quest",
        "adjust_reputation",
        "shift_interest_influence",
        "apply_market_modifier",
        "publish_rumor",
        "publish_article",
        "record_memory_fact",
        "record_town_event",
        "adjust_town_sentiment",
        "update_romance_profile",
        "propose_micro_date"
    };
    private static readonly string[] AmbientPrimaryAllowedCommands =
    {
        "record_town_event",
        "record_memory_fact",
        "publish_rumor"
    };
    private static readonly string[] AmbientConditionalCommands =
    {
        "adjust_reputation",
        "shift_interest_influence",
        "apply_market_modifier",
        "adjust_town_sentiment",
        "update_romance_profile",
        "propose_micro_date"
    };

    private readonly Dictionary<string, CommandPolicyRule> _rules = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _ambientConditionalEnabled = new(StringComparer.OrdinalIgnoreCase);

    public CommandPolicyService()
    {
        ConfigureRule("default", knownCommands: KnownCommands, defaultAllow: true);
        ConfigureRule("player_chat", knownCommands: KnownCommands, defaultAllow: true);
        ConfigureRule("player_request_board", knownCommands: KnownCommands, defaultAllow: true);
        ConfigureRule("manual_default", knownCommands: KnownCommands, defaultAllow: true);
        ConfigureRule("auto_default", knownCommands: KnownCommands, defaultAllow: true);
        ConfigureRule("npc_to_npc_ambient_dialogue", knownCommands: KnownCommands, explicitlyAllowed: Array.Empty<string>(), defaultAllow: false);
        SetAmbientConditionalCommandsEnabled(Array.Empty<string>());
    }

    public void ConfigureRule(
        string contextTag,
        IEnumerable<string>? knownCommands = null,
        IEnumerable<string>? explicitlyAllowed = null,
        IEnumerable<string>? explicitlyDenied = null,
        bool defaultAllow = true)
    {
        var key = NormalizeContextTag(contextTag);
        var rule = new CommandPolicyRule(defaultAllow);

        if (knownCommands is not null)
        {
            foreach (var command in knownCommands)
                rule.KnownCommands.Add(NormalizeCommand(command));
        }

        if (explicitlyAllowed is not null)
        {
            foreach (var command in explicitlyAllowed)
                rule.ExplicitlyAllowed.Add(NormalizeCommand(command));
        }

        if (explicitlyDenied is not null)
        {
            foreach (var command in explicitlyDenied)
                rule.ExplicitlyDenied.Add(NormalizeCommand(command));
        }

        _rules[key] = rule;
    }

    public CommandPolicyDecision Evaluate(string? contextTag, string? command)
    {
        var normalizedCommand = NormalizeCommand(command);
        if (string.IsNullOrWhiteSpace(normalizedCommand))
        {
            return new CommandPolicyDecision(
                Allowed: false,
                ContextTag: NormalizeContextTag(contextTag),
                Command: string.Empty,
                ReasonCode: "E_POLICY_COMMAND_MISSING");
        }

        var resolvedContext = ResolveContextTag(contextTag);
        var rule = _rules.TryGetValue(resolvedContext, out var foundRule)
            ? foundRule
            : _rules["default"];

        if (rule.KnownCommands.Count > 0 && !rule.KnownCommands.Contains(normalizedCommand))
        {
            return new CommandPolicyDecision(
                Allowed: false,
                ContextTag: resolvedContext,
                Command: normalizedCommand,
                ReasonCode: "E_POLICY_COMMAND_UNKNOWN");
        }

        if (rule.ExplicitlyDenied.Contains(normalizedCommand))
        {
            return new CommandPolicyDecision(
                Allowed: false,
                ContextTag: resolvedContext,
                Command: normalizedCommand,
                ReasonCode: "E_POLICY_DENY_RULE");
        }

        if (rule.ExplicitlyAllowed.Count > 0)
        {
            var allowedByRule = rule.ExplicitlyAllowed.Contains(normalizedCommand);
            return new CommandPolicyDecision(
                Allowed: allowedByRule,
                ContextTag: resolvedContext,
                Command: normalizedCommand,
                ReasonCode: allowedByRule ? "OK" : "E_POLICY_NOT_ALLOWED");
        }

        return new CommandPolicyDecision(
            Allowed: rule.DefaultAllow,
            ContextTag: resolvedContext,
            Command: normalizedCommand,
            ReasonCode: rule.DefaultAllow ? "OK" : "E_POLICY_NOT_ALLOWED");
    }

    public string BuildPromptRule(string? contextTag)
    {
        var resolvedContext = ResolveContextTag(contextTag);
        var rule = _rules.TryGetValue(resolvedContext, out var foundRule)
            ? foundRule
            : _rules["default"];

        var allowed = ResolveAllowedCommands(rule).ToArray();
        var blocked = rule.KnownCommands
            .Where(command => !allowed.Contains(command, StringComparer.OrdinalIgnoreCase))
            .OrderBy(command => command, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return string.Join(" ",
            $"COMMAND_POLICY_RULE: Context '{resolvedContext}'.",
            allowed.Length == 0
                ? "AllowedCommands=[none]."
                : $"AllowedCommands=[{string.Join(", ", allowed)}].",
            blocked.Length == 0
                ? "BlockedCommands=[none]."
                : $"BlockedCommands=[{string.Join(", ", blocked)}].",
            "If a command is blocked, do not emit it; reply in-character without that command.");
    }

    public void SetAmbientConditionalCommandsEnabled(IEnumerable<string>? commandsToEnable)
    {
        _ambientConditionalEnabled.Clear();
        if (commandsToEnable is not null)
        {
            foreach (var command in commandsToEnable)
            {
                var normalized = NormalizeCommand(command);
                if (AmbientConditionalCommands.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                    _ambientConditionalEnabled.Add(normalized);
            }
        }

        var allowed = AmbientPrimaryAllowedCommands
            .Concat(_ambientConditionalEnabled)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var denied = AmbientConditionalCommands
            .Where(command => !_ambientConditionalEnabled.Contains(command))
            .ToArray();

        ConfigureRule(
            "npc_to_npc_ambient",
            knownCommands: KnownCommands,
            explicitlyAllowed: allowed,
            explicitlyDenied: denied,
            defaultAllow: false);
    }

    public IReadOnlyList<string> GetAmbientConditionalCommandsEnabled()
    {
        return _ambientConditionalEnabled
            .OrderBy(command => command, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> ResolveAllowedCommands(CommandPolicyRule rule)
    {
        if (rule.ExplicitlyAllowed.Count > 0)
        {
            foreach (var command in rule.ExplicitlyAllowed
                         .Where(command => !rule.ExplicitlyDenied.Contains(command))
                         .OrderBy(command => command, StringComparer.OrdinalIgnoreCase))
            {
                yield return command;
            }

            yield break;
        }

        if (!rule.DefaultAllow)
            yield break;

        foreach (var command in rule.KnownCommands
                     .Where(command => !rule.ExplicitlyDenied.Contains(command))
                     .OrderBy(command => command, StringComparer.OrdinalIgnoreCase))
        {
            yield return command;
        }
    }

    private string ResolveContextTag(string? contextTag)
    {
        var normalized = NormalizeContextTag(contextTag);
        if (_rules.ContainsKey(normalized))
            return normalized;

        if (normalized.StartsWith("manual_", StringComparison.OrdinalIgnoreCase) && _rules.ContainsKey("manual_default"))
            return "manual_default";
        if (normalized.StartsWith("auto_", StringComparison.OrdinalIgnoreCase) && _rules.ContainsKey("auto_default"))
            return "auto_default";

        return "default";
    }

    private static string NormalizeContextTag(string? contextTag)
    {
        var normalized = (contextTag ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "player_chat" : normalized.ToLowerInvariant();
    }

    private static string NormalizeCommand(string? command)
    {
        return (command ?? string.Empty).Trim().ToLowerInvariant();
    }

    private sealed class CommandPolicyRule
    {
        public CommandPolicyRule(bool defaultAllow)
        {
            DefaultAllow = defaultAllow;
        }

        public bool DefaultAllow { get; }
        public HashSet<string> KnownCommands { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ExplicitlyAllowed { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ExplicitlyDenied { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

public readonly record struct CommandPolicyDecision(
    bool Allowed,
    string ContextTag,
    string Command,
    string ReasonCode);
