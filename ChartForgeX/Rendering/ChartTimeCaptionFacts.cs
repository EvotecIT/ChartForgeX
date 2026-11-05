using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// The display-zone offsets a saved page needs to repeat calendar ticks for a zoomed window: the zone's UTC offset
/// from each captured instant onward, across the axis domain and a year either side.
/// </summary>
internal sealed class ChartTimeZoneFacts {
    private const double MarginDays = 400;
    private const double MaximumSpanDays = 200 * 365.25;
    private const long UnixEpochTicks = 621355968000000000;

    private ChartTimeZoneFacts(IReadOnlyList<(long FromMilliseconds, double OffsetMinutes)> offsets) => Offsets = offsets;

    /// <summary>Gets each offset in minutes and the UTC instant, in Unix milliseconds, from which it applies. The first applies to every earlier instant.</summary>
    internal IReadOnlyList<(long FromMilliseconds, double OffsetMinutes)> Offsets { get; }

    internal static ChartTimeZoneFacts? TryCapture(TimeZoneInfo zone, double minimum, double maximum) {
        var first = minimum - MarginDays;
        var last = maximum + MarginDays;
        if (!(first > 0) || !(last < 2958465) || last - first > MaximumSpanDays) return null;
        DateTime Utc(double value) => DateTime.SpecifyKind(DateTime.FromOADate(value), DateTimeKind.Utc);
        long Milliseconds(DateTime instant) => (instant.Ticks - UnixEpochTicks) / TimeSpan.TicksPerMillisecond;
        var start = Utc(first);
        var offsets = new List<(long, double)> { (Milliseconds(start), zone.GetUtcOffset(start).TotalMinutes) };
        var previous = start;
        var previousOffset = zone.GetUtcOffset(start);
        // Offsets change at most a few times a year, so daily samples find every change; a bisection then
        // locates the first second with the new offset.
        for (var day = Math.Floor(first) + 1; day <= last + 1; day++) {
            var instant = Utc(day);
            var offset = zone.GetUtcOffset(instant);
            if (offset == previousOffset) { previous = instant; continue; }
            var low = previous.Ticks;
            var high = instant.Ticks;
            while (high - low > TimeSpan.TicksPerSecond) {
                var middle = low + (high - low) / 2;
                if (zone.GetUtcOffset(new DateTime(middle, DateTimeKind.Utc)) == previousOffset) low = middle; else high = middle;
            }
            offsets.Add((Milliseconds(new DateTime(high / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, DateTimeKind.Utc)), offset.TotalMinutes));
            previous = instant;
            previousOffset = offset;
        }
        return new ChartTimeZoneFacts(offsets.AsReadOnly());
    }
}

/// <summary>
/// The culture patterns and names behind a time axis's default captions, captured only when a saved page can
/// format them exactly: a Gregorian calendar and patterns made of the day, month, year, hour, minute, second and
/// designator fields.
/// </summary>
internal sealed class ChartTimeCultureFacts {
    private ChartTimeCultureFacts(CultureInfo culture, ChartAxis axis) {
        var format = culture.DateTimeFormat;
        DayYearPattern = ChartTimeScale.DayPattern(axis, true);
        DayPattern = ChartTimeScale.DayPattern(axis, false);
        TimePattern = ChartTimeScale.TimePattern(axis, false);
        SecondsPattern = ChartTimeScale.TimePattern(axis, true);
        DateSeparator = format.DateSeparator;
        TimeSeparator = format.TimeSeparator;
        AMDesignator = format.AMDesignator;
        PMDesignator = format.PMDesignator;
        AbbreviatedMonths = Months(format.AbbreviatedMonthNames);
        AbbreviatedGenitiveMonths = Months(format.AbbreviatedMonthGenitiveNames);
        MonthNames = Months(format.MonthNames);
        GenitiveMonthNames = Months(format.MonthGenitiveNames);
        AbbreviatedDays = format.AbbreviatedDayNames;
        Days = format.DayNames;
    }

    internal string DayYearPattern { get; }
    internal string DayPattern { get; }
    internal string TimePattern { get; }
    internal string SecondsPattern { get; }
    internal string DateSeparator { get; }
    internal string TimeSeparator { get; }
    internal string AMDesignator { get; }
    internal string PMDesignator { get; }
    internal IReadOnlyList<string> AbbreviatedMonths { get; }
    internal IReadOnlyList<string> AbbreviatedGenitiveMonths { get; }
    internal IReadOnlyList<string> MonthNames { get; }
    internal IReadOnlyList<string> GenitiveMonthNames { get; }
    internal IReadOnlyList<string> AbbreviatedDays { get; }
    internal IReadOnlyList<string> Days { get; }

    internal static ChartTimeCultureFacts? TryCapture(ChartAxis axis) {
        var culture = axis.Culture;
        if (culture == null || culture.DateTimeFormat.Calendar is not GregorianCalendar) return null;
        var facts = new ChartTimeCultureFacts(culture, axis);
        return Supported(facts.DayYearPattern) && Supported(facts.DayPattern) && Supported(facts.TimePattern) && Supported(facts.SecondsPattern)
            ? facts : null;
    }

    // .NET month arrays carry a thirteenth, empty name for 13-month calendars.
    private static IReadOnlyList<string> Months(string[] names) => Array.AsReadOnly(names.Take(12).ToArray());

    // Fractions, eras and zone designators are outside the saved formatter; such a culture keeps zoom declined.
    private static bool Supported(string pattern) {
        for (var index = 0; index < pattern.Length; index++) {
            var character = pattern[index];
            if (character == '\\') { index++; continue; }
            if (character == '\'' || character == '"') {
                var close = pattern.IndexOf(character, index + 1);
                if (close < 0) return false;
                index = close;
                continue;
            }
            if ("fFgzK%".IndexOf(character) >= 0) return false;
        }
        return true;
    }
}
