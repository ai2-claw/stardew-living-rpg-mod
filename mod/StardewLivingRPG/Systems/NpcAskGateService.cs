using StardewLivingRPG.Config;
using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public enum NpcAskDecision
{
    Accept,
    Defer,
    Reject
}

public sealed class NpcAskGateResult
{
    public NpcAskDecision Decision { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public string PlayerFacingMessage { get; init; } = string.Empty;
}

public sealed class NpcAskGateService
{
    public NpcAskGateResult Evaluate(
        SaveState state,
        string npcName,
        NpcVerbalProfile profile,
        int heartLevel,
        string askTopic,
        int timeOfDay,
        bool isRaining)
    {
        var topic = NormalizeTopic(askTopic);
        var score = 0;
        var reputation = GetNpcReputation(state, npcName);

        // Relationship baseline.
        score += heartLevel switch
        {
            >= 8 => 3,
            >= 5 => 2,
            >= 3 => 1,
            _ => -1
        };

        // Mod trust/reliability baseline (separate from vanilla hearts).
        score += reputation switch
        {
            >= 50 => 2,
            >= 20 => 1,
            <= -40 => -2,
            <= -15 => -1,
            _ => 0
        };

        // Personality baseline independent of topic.
        score += profile switch
        {
            NpcVerbalProfile.Traditionalist => 1,
            NpcVerbalProfile.Enthusiast => 1,
            NpcVerbalProfile.Recluse => -1,
            _ => 0
        };

        // Heart+reputation interaction: trust debt matters more at low hearts,
        // while strong relationship plus trust slightly improves ask acceptance.
        if (heartLevel <= 2 && reputation <= -15)
            score -= 2;
        else if (heartLevel >= 6 && reputation >= 20)
            score += 1;

        // Topic burden.
        score += topic switch
        {
            "manual_market" => -1,
            "manual_interest" => 0,
            "manual_relationship" => 1,
            _ => 0
        };

        // Personality preference for topic.
        score += (profile, topic) switch
        {
            (NpcVerbalProfile.Professional, "manual_market") => 2,
            (NpcVerbalProfile.Professional, "manual_relationship") => -1,

            (NpcVerbalProfile.Traditionalist, "manual_relationship") => 1,
            (NpcVerbalProfile.Traditionalist, "manual_interest") => 1,

            (NpcVerbalProfile.Intellectual, "manual_interest") => 1,
            (NpcVerbalProfile.Intellectual, "manual_market") => 1,

            (NpcVerbalProfile.Enthusiast, "manual_relationship") => 1,
            (NpcVerbalProfile.Enthusiast, "manual_interest") => 1,
            (NpcVerbalProfile.Enthusiast, "manual_market") => 1,

            (NpcVerbalProfile.Recluse, "manual_relationship") => -2,
            (NpcVerbalProfile.Recluse, "manual_interest") => -1,
            _ => 0
        };

        // Context pressure.
        if (timeOfDay >= 2200 || timeOfDay < 700)
            score -= 2;
        if (isRaining && topic == "manual_market")
            score -= 1;
        score += GetDayOfWeekContextScore(state.Calendar.Day, topic, profile);

        // Repeated asks reduce willingness.
        if (state.NpcMemory.Profiles.TryGetValue(npcName, out var memory))
        {
            var repeatedRecentAsks = memory.RecentTurns.Count(t =>
                t.Day >= state.Calendar.Day - 1 &&
                t.Tags.Any(tag => TopicTagMatches(tag, topic)));
            if (repeatedRecentAsks >= 2)
                score -= 2;

            var recentInteractionCount = memory.RecentTurns.Count(t => t.Day >= state.Calendar.Day - 3);
            if (recentInteractionCount >= 4)
                score += 1;
            else if (recentInteractionCount == 0 && heartLevel <= 2)
                score -= 1;
        }

        // Forgiveness floor: high-heart NPCs stay warm even when trust is currently low.
        // They may defer, but should not hard-reject solely on reputation debt.
        if (heartLevel >= 8 && reputation <= -15 && score < 0)
            score = 0;

        if (score >= 2)
        {
            return new NpcAskGateResult
            {
                Decision = NpcAskDecision.Accept,
                ReasonCode = "ACCEPT",
                PlayerFacingMessage = string.Empty
            };
        }

        if (score >= 0)
        {
            return new NpcAskGateResult
            {
                Decision = NpcAskDecision.Defer,
                ReasonCode = "DEFER_CONTEXT",
                PlayerFacingMessage = BuildDeferMessage(profile, topic, npcName, state.Calendar.Day)
            };
        }

        return new NpcAskGateResult
        {
            Decision = NpcAskDecision.Reject,
            ReasonCode = "REJECT_CONTEXT",
            PlayerFacingMessage = BuildRejectMessage(profile, topic, npcName, state.Calendar.Day)
        };
    }

    private static int GetNpcReputation(SaveState state, string npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return 0;

        if (state.Social.NpcReputation.TryGetValue(npcName, out var direct))
            return Math.Clamp(direct, -100, 100);

        var normalized = npcName.Trim().ToLowerInvariant();
        if (state.Social.NpcReputation.TryGetValue(normalized, out var normalizedValue))
            return Math.Clamp(normalizedValue, -100, 100);

        return 0;
    }

    private static string NormalizeTopic(string topic)
    {
        var value = (topic ?? string.Empty).Trim().ToLowerInvariant();
        return value switch
        {
            "manual_relationship" => value,
            "manual_interest" => value,
            "manual_market" => value,
            _ => "manual_relationship"
        };
    }

    private static bool TopicTagMatches(string tag, string topic)
    {
        var t = (tag ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(t))
            return false;

        return topic switch
        {
            "manual_relationship" => t.Contains("friend", StringComparison.Ordinal)
                || t.Contains("trust", StringComparison.Ordinal)
                || t.Contains("relationship", StringComparison.Ordinal),
            "manual_interest" => t.Contains("group", StringComparison.Ordinal)
                || t.Contains("community", StringComparison.Ordinal)
                || t.Contains("town", StringComparison.Ordinal),
            "manual_market" => t.Contains("market", StringComparison.Ordinal)
                || t.Contains("price", StringComparison.Ordinal)
                || t.Contains("crop", StringComparison.Ordinal),
            _ => false
        };
    }

    private static int GetDayOfWeekContextScore(int day, string topic, NpcVerbalProfile profile)
    {
        var dayOfWeek = ResolveDayOfWeek(day);

        if (topic == "manual_market" && dayOfWeek is "Fri" or "Sat")
            return -1;
        if (topic == "manual_relationship" && dayOfWeek is "Sun" or "Mon")
            return 1;
        if (profile == NpcVerbalProfile.Recluse && dayOfWeek == "Fri")
            return -1;

        return 0;
    }

    private static string ResolveDayOfWeek(int day)
    {
        var index = ((Math.Max(1, day) - 1) % 7 + 7) % 7;
        return index switch
        {
            0 => "Mon",
            1 => "Tue",
            2 => "Wed",
            3 => "Thu",
            4 => "Fri",
            5 => "Sat",
            _ => "Sun"
        };
    }

    private static string BuildRejectMessage(NpcVerbalProfile profile, string topic, string npcName, int day)
    {
        var options = profile switch
        {
            NpcVerbalProfile.Professional => new[]
            {
                "Let's leave that for now. I need to focus on today's work.",
                "I cannot take that on right now. Timing is too tight."
            },
            NpcVerbalProfile.Traditionalist => new[]
            {
                "Not right now, friend. Let's pick this up another time.",
                "Best leave that be for today, alright?"
            },
            NpcVerbalProfile.Intellectual => new[]
            {
                "I do not think this is the right moment for that discussion.",
                "I would prefer not to proceed with that at this time."
            },
            NpcVerbalProfile.Enthusiast => topic == "manual_market"
                ? new[]
                {
                    "Ah, not now! Too many moving pieces at once.",
                    "Not this second. The market is all over the place!"
                }
                : new[]
                {
                    "Maybe not right this second.",
                    "I cannot do that right now, sorry!"
                },
            NpcVerbalProfile.Recluse => new[]
            {
                "...No. Not now.",
                "Busy. Ask someone else."
            },
            _ => new[] { "Not right now." }
        };

        return SelectTemplate(options, npcName, day, topic);
    }

    private static string BuildDeferMessage(NpcVerbalProfile profile, string topic, string npcName, int day)
    {
        var options = profile switch
        {
            NpcVerbalProfile.Professional => topic == "manual_market"
                ? new[]
                {
                    "I can review that in a bit. Catch me later today.",
                    "Give me a little time to check the numbers first."
                }
                : new[]
                {
                    "Give me a little time, then ask again.",
                    "Not yet. Come back once I finish this round."
                },
            NpcVerbalProfile.Traditionalist => new[]
            {
                "Maybe later today, once things settle down.",
                "Later on, once chores ease up, alright?"
            },
            NpcVerbalProfile.Intellectual => new[]
            {
                "Let us revisit that shortly, when I can consider it properly.",
                "Allow me a bit of time to think before we continue."
            },
            NpcVerbalProfile.Enthusiast => new[]
            {
                "Give me a little bit, then ask me again!",
                "Soon! I just need a moment to sort things out."
            },
            NpcVerbalProfile.Recluse => new[]
            {
                "...Later. Not now.",
                "Later. Maybe."
            },
            _ => new[] { "Ask again a little later." }
        };

        return SelectTemplate(options, npcName, day, topic);
    }

    private static string SelectTemplate(string[] options, string npcName, int day, string topic)
    {
        if (options is null || options.Length == 0)
            return string.Empty;
        if (options.Length == 1)
            return options[0];

        var seedText = $"{npcName}|{day}|{topic}";
        var hash = 17;
        foreach (var ch in seedText)
            hash = unchecked((hash * 31) + char.ToLowerInvariant(ch));
        var index = Math.Abs(hash) % options.Length;
        return options[index];
    }
}
