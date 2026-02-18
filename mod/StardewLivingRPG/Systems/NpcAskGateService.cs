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

        // Repeated asks reduce willingness.
        if (state.NpcMemory.Profiles.TryGetValue(npcName, out var memory))
        {
            var repeatedRecentAsks = memory.RecentTurns.Count(t =>
                t.Day >= state.Calendar.Day - 1 &&
                t.Tags.Any(tag => TopicTagMatches(tag, topic)));
            if (repeatedRecentAsks >= 2)
                score -= 2;
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
                PlayerFacingMessage = BuildDeferMessage(profile, topic)
            };
        }

        return new NpcAskGateResult
        {
            Decision = NpcAskDecision.Reject,
            ReasonCode = "REJECT_CONTEXT",
            PlayerFacingMessage = BuildRejectMessage(profile, topic)
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

    private static string BuildRejectMessage(NpcVerbalProfile profile, string topic)
    {
        return profile switch
        {
            NpcVerbalProfile.Professional => "Let's leave that for now. I need to focus on today's work.",
            NpcVerbalProfile.Traditionalist => "Not right now, friend. Let's pick this up another time.",
            NpcVerbalProfile.Intellectual => "I do not think this is the right moment for that discussion.",
            NpcVerbalProfile.Enthusiast => topic == "manual_market"
                ? "Ah, not now! Too many moving pieces at once."
                : "Maybe not right this second.",
            NpcVerbalProfile.Recluse => "...No. Not now.",
            _ => "Not right now."
        };
    }

    private static string BuildDeferMessage(NpcVerbalProfile profile, string topic)
    {
        return profile switch
        {
            NpcVerbalProfile.Professional => topic == "manual_market"
                ? "I can review that in a bit. Catch me later today."
                : "Give me a little time, then ask again.",
            NpcVerbalProfile.Traditionalist => "Maybe later today, once things settle down.",
            NpcVerbalProfile.Intellectual => "Let us revisit that shortly, when I can consider it properly.",
            NpcVerbalProfile.Enthusiast => "Give me a little bit, then ask me again!",
            NpcVerbalProfile.Recluse => "...Later. Not now.",
            _ => "Ask again a little later."
        };
    }
}
