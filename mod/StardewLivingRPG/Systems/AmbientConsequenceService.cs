using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class AmbientConsequenceService
{
    public IReadOnlyList<TownMemoryEvent> ReadRecentEvents(
        SaveState state,
        int maxAgeDays = 2,
        int minSeverity = 1,
        int maxCount = 24)
    {
        var currentDay = state.Calendar.Day;
        var filtered = state.TownMemory.Events
            .Where(ev =>
                ev is not null
                && !string.IsNullOrWhiteSpace(ev.Kind)
                && !string.IsNullOrWhiteSpace(ev.Summary)
                && currentDay - ev.Day >= 0
                && currentDay - ev.Day <= maxAgeDays
                && ev.Severity >= minSeverity)
            .OrderByDescending(ev => ev.Day)
            .ThenByDescending(ev => ev.Severity)
            .ThenBy(ev => ev.EventId, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maxCount))
            .ToList();

        return filtered;
    }

    public AmbientEventSignalSnapshot BuildSignalSnapshot(
        SaveState state,
        int maxAgeDays = 2,
        int minSeverity = 1,
        int maxCount = 24)
    {
        var recent = ReadRecentEvents(state, maxAgeDays, minSeverity, maxCount);
        var snapshot = new AmbientEventSignalSnapshot
        {
            CurrentDay = state.Calendar.Day,
            WindowDays = maxAgeDays,
            TotalEvents = recent.Count
        };

        foreach (var ev in recent)
        {
            IncrementCounter(snapshot.KindCounts, ev.Kind);
            IncrementCounter(snapshot.VisibilityCounts, ev.Visibility);
            foreach (var tag in ev.Tags ?? Array.Empty<string>())
                IncrementCounter(snapshot.TagCounts, tag);
        }

        return snapshot;
    }

    private static void IncrementCounter(Dictionary<string, int> counters, string? key)
    {
        var normalized = string.IsNullOrWhiteSpace(key)
            ? "(none)"
            : key.Trim().ToLowerInvariant();
        counters.TryGetValue(normalized, out var current);
        counters[normalized] = current + 1;
    }
}

public sealed class AmbientEventSignalSnapshot
{
    public int CurrentDay { get; set; }
    public int WindowDays { get; set; }
    public int TotalEvents { get; set; }
    public Dictionary<string, int> KindCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> VisibilityCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> TagCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
}
