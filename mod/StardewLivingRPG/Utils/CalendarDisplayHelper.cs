namespace StardewLivingRPG.Utils;

public static class CalendarDisplayHelper
{
    private const int DaysPerSeason = 28;
    private const int DaysPerYear = 112;

    private static readonly string[] SeasonNames = { "Spring", "Summer", "Fall", "Winter" };
    private static readonly string[] WeekdayNames = { "Mon.", "Tue.", "Wed.", "Thu.", "Fri.", "Sat.", "Sun." };

    public static string FormatSeasonYearWeekdayDay(int absoluteDay)
    {
        var parts = Resolve(absoluteDay);
        return $"{parts.Season} Year {parts.Year} {parts.Weekday} {parts.DayOfSeason}";
    }

    public static string FormatWeekdayDay(int absoluteDay)
    {
        var parts = Resolve(absoluteDay);
        return $"{parts.Weekday} {parts.DayOfSeason}";
    }

    public static string FormatWeekdayDayWithSeasonYear(int absoluteDay)
    {
        var parts = Resolve(absoluteDay);
        return $"{parts.Weekday} {parts.DayOfSeason} ({parts.Season} Year {parts.Year})";
    }

    public static string FormatSeasonDayYearShort(int absoluteDay)
    {
        var parts = Resolve(absoluteDay);
        return $"{parts.Season} {parts.DayOfSeason}, Yr {parts.Year}";
    }

    private static CalendarDisplayParts Resolve(int absoluteDay)
    {
        var safeAbsoluteDay = Math.Max(1, absoluteDay);
        var zeroBasedAbsoluteDay = safeAbsoluteDay - 1;
        var year = (zeroBasedAbsoluteDay / DaysPerYear) + 1;
        var dayInYear = zeroBasedAbsoluteDay % DaysPerYear;
        var seasonIndex = dayInYear / DaysPerSeason;
        var dayOfSeason = (dayInYear % DaysPerSeason) + 1;
        var weekdayIndex = (dayOfSeason - 1) % WeekdayNames.Length;
        return new CalendarDisplayParts(
            SeasonNames[seasonIndex],
            year,
            WeekdayNames[weekdayIndex],
            dayOfSeason);
    }

    private readonly record struct CalendarDisplayParts(string Season, int Year, string Weekday, int DayOfSeason);
}
