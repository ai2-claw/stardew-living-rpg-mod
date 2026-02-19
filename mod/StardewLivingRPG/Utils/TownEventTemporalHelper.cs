using System.Text.RegularExpressions;
using StardewLivingRPG.State;

namespace StardewLivingRPG.Utils;

public static class TownEventTemporalHelper
{
    private static readonly Regex ClockTimePattern = new(
        @"(?<!\d)(?<hour>1[0-2]|0?[1-9])(?::(?<minute>[0-5]\d))?\s*(?<ampm>[AaPp]\.?[Mm]\.?)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly string[] FutureCues =
    {
        "later today",
        "later this",
        "later tonight",
        "tomorrow",
        "upcoming",
        "scheduled",
        "planned",
        "this afternoon",
        "this evening",
        "tonight"
    };

    public static bool IsUpcoming(TownMemoryEvent ev, int currentDay, int currentTimeOfDay)
    {
        if (ev.Day > currentDay)
            return true;
        if (ev.Day < currentDay)
            return false;

        var now = NormalizeTimeOfDay(currentTimeOfDay);
        var summary = ev.Summary ?? string.Empty;
        if (TryExtractTimeOfDay(summary, out var eventTime))
            return eventTime > now + 10;

        var normalized = summary.ToLowerInvariant();
        if (normalized.Contains("this afternoon", StringComparison.Ordinal) && now >= 1700)
            return false;
        if ((normalized.Contains("this evening", StringComparison.Ordinal)
                || normalized.Contains("tonight", StringComparison.Ordinal))
            && now >= 2200)
            return false;

        return ContainsFutureCue(summary);
    }

    public static string BuildTemporalLabel(TownMemoryEvent ev, int currentDay, int currentTimeOfDay)
    {
        if (ev.Day > currentDay)
        {
            var daysAhead = ev.Day - currentDay;
            return daysAhead == 1 ? "tomorrow" : $"in {daysAhead} days";
        }

        if (ev.Day < currentDay)
        {
            var daysAgo = currentDay - ev.Day;
            return daysAgo == 1 ? "yesterday" : $"{daysAgo} days ago";
        }

        if (IsUpcoming(ev, currentDay, currentTimeOfDay))
        {
            if (TryExtractTimeOfDay(ev.Summary ?? string.Empty, out var eventTime))
                return $"later today ({FormatClockTime(eventTime)})";

            return "later today";
        }

        return "today";
    }

    private static bool TryExtractTimeOfDay(string text, out int hhmm)
    {
        hhmm = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var match = ClockTimePattern.Match(text);
        if (!match.Success)
            return false;

        if (!int.TryParse(match.Groups["hour"].Value, out var hour12))
            return false;
        var minuteText = match.Groups["minute"].Success ? match.Groups["minute"].Value : "00";
        if (!int.TryParse(minuteText, out var minute))
            return false;

        minute = Math.Clamp(minute, 0, 59);
        var ampm = match.Groups["ampm"].Value.ToLowerInvariant();
        var isPm = ampm.StartsWith("p", StringComparison.Ordinal);
        hour12 = Math.Clamp(hour12, 1, 12);
        var hour24 = hour12 % 12;
        if (isPm)
            hour24 += 12;

        hhmm = (hour24 * 100) + minute;
        return true;
    }

    private static string FormatClockTime(int hhmm)
    {
        var normalized = NormalizeTimeOfDay(hhmm);
        var hour24 = normalized / 100;
        var minute = normalized % 100;
        var ampm = hour24 >= 12 ? "PM" : "AM";
        var hour12 = hour24 % 12;
        if (hour12 == 0)
            hour12 = 12;

        return $"{hour12}:{minute:00} {ampm}";
    }

    private static int NormalizeTimeOfDay(int hhmm)
    {
        var clamped = Math.Clamp(hhmm, 0, 2600);
        var hour = clamped / 100;
        var minute = clamped % 100;
        minute = Math.Clamp(minute, 0, 59);
        return (hour * 100) + minute;
    }

    private static bool ContainsFutureCue(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return false;

        var normalized = summary.ToLowerInvariant();
        foreach (var cue in FutureCues)
        {
            if (normalized.Contains(cue, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
