using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class PairEmotionService
{
    private readonly int _maxDeltaPerCommand;
    private readonly int _maxDeltaPerDayPerAxis;

    public PairEmotionService(int maxDeltaPerCommand, int maxDeltaPerDayPerAxis)
    {
        _maxDeltaPerCommand = Math.Max(1, maxDeltaPerCommand);
        _maxDeltaPerDayPerAxis = Math.Max(_maxDeltaPerCommand, maxDeltaPerDayPerAxis);
    }

    public static string BuildPairKey(string npcIdA, string npcIdB)
    {
        var left = (npcIdA ?? string.Empty).Trim().ToLowerInvariant();
        var right = (npcIdB ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return string.Empty;

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{left}|{right}"
            : $"{right}|{left}";
    }

    public NpcPairEmotionEntry GetOrCreate(SaveState state, string npcIdA, string npcIdB)
    {
        var pairKey = BuildPairKey(npcIdA, npcIdB);
        if (!state.Social.PairEmotions.TryGetValue(pairKey, out var entry))
        {
            entry = new NpcPairEmotionEntry();
            state.Social.PairEmotions[pairKey] = entry;
        }

        return entry;
    }

    public bool TryAdjustAxis(
        SaveState state,
        string npcIdA,
        string npcIdB,
        string axis,
        int delta,
        out string reasonCode)
    {
        reasonCode = "ok";
        if (string.IsNullOrWhiteSpace(axis))
        {
            reasonCode = "axis_missing";
            return false;
        }

        if (delta < -_maxDeltaPerCommand || delta > _maxDeltaPerCommand)
        {
            reasonCode = "delta_out_of_bounds";
            return false;
        }

        var pairKey = BuildPairKey(npcIdA, npcIdB);
        if (string.IsNullOrWhiteSpace(pairKey))
        {
            reasonCode = "pair_missing";
            return false;
        }

        var dayPrefix = $"pair_emotion:{state.Calendar.Day}:{pairKey}:{axis.ToLowerInvariant()}:";
        var netToday = SumSignedDeltaFacts(state, dayPrefix);
        if (Math.Abs(netToday + delta) > _maxDeltaPerDayPerAxis)
        {
            reasonCode = "daily_cap";
            return false;
        }

        var entry = GetOrCreate(state, npcIdA, npcIdB);
        entry.EmotionAxes.TryGetValue(axis, out var current);
        entry.EmotionAxes[axis] = Math.Clamp(current + delta, 0, 100);
        entry.LastInteractionDay = state.Calendar.Day;
        entry.Affinity = Math.Clamp(entry.Affinity + (axis is "friendship" or "trust" or "admiration" ? delta : 0), -100, 100);
        entry.Tension = Math.Clamp(entry.Tension + (axis is "anger" or "jealousy" or "envy" ? Math.Max(0, delta) : 0), 0, 100);
        entry.Avoidance = Math.Clamp(entry.Avoidance + (axis is "anger" && delta > 0 ? delta : 0), 0, 100);
        entry.Familiarity = Math.Clamp(entry.Familiarity + 1, 0, 100);

        if (entry.Tension >= 60 && !entry.ActiveFlags.Contains("grudge", StringComparer.OrdinalIgnoreCase))
            entry.ActiveFlags.Add("grudge");
        if (entry.Familiarity >= 25 && !entry.ActiveFlags.Contains("frequent_visitors", StringComparer.OrdinalIgnoreCase))
            entry.ActiveFlags.Add("frequent_visitors");

        state.Facts.Facts[$"{dayPrefix}{delta}:{Guid.NewGuid():N}"] = new FactValue
        {
            Value = true,
            SetDay = state.Calendar.Day,
            Source = "npc_command"
        };

        return true;
    }

    public void Decay(SaveState state)
    {
        foreach (var key in state.Social.PairEmotions.Keys.ToArray())
        {
            var entry = state.Social.PairEmotions[key];
            if (entry is null)
                continue;

            foreach (var axis in entry.EmotionAxes.Keys.ToArray())
            {
                var current = entry.EmotionAxes[axis];
                var decay = axis switch
                {
                    "anger" => 2,
                    "jealousy" => 1,
                    "envy" => 1,
                    "awkwardness" => 2,
                    _ => 0
                };
                entry.EmotionAxes[axis] = Math.Max(0, current - decay);
            }

            entry.Tension = Math.Max(0, entry.Tension - 1);
            entry.Avoidance = Math.Max(0, entry.Avoidance - 1);
            entry.ActiveFlags = entry.ActiveFlags
                .Where(flag => !(flag.Equals("grudge", StringComparison.OrdinalIgnoreCase) && entry.Tension < 40))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (entry.EmotionAxes.Values.All(value => value == 0)
                && entry.Affinity == 0
                && entry.Tension == 0
                && entry.Avoidance == 0
                && state.Calendar.Day - entry.LastInteractionDay > 28)
            {
                state.Social.PairEmotions.Remove(key);
            }
        }
    }

    private static int SumSignedDeltaFacts(SaveState state, string prefix)
    {
        var net = 0;
        foreach (var key in state.Facts.Facts.Keys)
        {
            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var remainder = key[prefix.Length..];
            var separator = remainder.IndexOf(':');
            var token = separator < 0 ? remainder : remainder[..separator];
            if (int.TryParse(token, out var parsed))
                net += parsed;
        }

        return net;
    }
}
