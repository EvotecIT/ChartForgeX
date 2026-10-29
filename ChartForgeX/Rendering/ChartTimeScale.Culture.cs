using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Culture-aware time label patterns. An axis without a culture keeps the invariant ISO patterns.</summary>
internal static partial class ChartTimeScale {
    internal static CultureInfo LabelCulture(ChartAxis axis) => axis.Culture ?? CultureInfo.InvariantCulture;

    /// <summary>Returns the pattern for a calendar-day label, with or without the year.</summary>
    internal static string DayPattern(ChartAxis axis, bool includeYear) {
        if (axis.Culture == null) return includeYear ? "yyyy-MM-dd" : "MM-dd";
        return includeYear ? axis.Culture.DateTimeFormat.ShortDatePattern : ShortMonthDayPattern(axis.Culture);
    }

    /// <summary>Returns the pattern for a time-of-day label.</summary>
    internal static string TimePattern(ChartAxis axis, bool seconds) {
        if (axis.Culture == null) return seconds ? "HH:mm:ss" : "HH:mm";
        return seconds ? axis.Culture.DateTimeFormat.LongTimePattern : axis.Culture.DateTimeFormat.ShortTimePattern;
    }

    /// <summary>Returns the pattern for a full date and time.</summary>
    internal static string DateTimePattern(ChartAxis axis, bool seconds) => DayPattern(axis, true) + " " + TimePattern(axis, seconds);

    /// <summary>Returns the culture's date and long time pattern with fractional seconds after the seconds field.</summary>
    internal static string FractionalDateTimePattern(ChartAxis axis, string fraction) {
        var time = TimePattern(axis, true);
        var seconds = time.LastIndexOf("ss", System.StringComparison.Ordinal);
        var separator = "'" + LabelCulture(axis).NumberFormat.NumberDecimalSeparator + "'";
        time = seconds < 0 ? time + separator + fraction : time.Insert(seconds + 2, separator + fraction);
        return DayPattern(axis, true) + " " + time;
    }

    // The culture's month-day pattern with an abbreviated month: "27 Oct", "Oct 27", "27. Okt.", "27 paź".
    private static string ShortMonthDayPattern(CultureInfo culture) {
        var pattern = culture.DateTimeFormat.MonthDayPattern;
        return pattern.Contains("MMMM") ? pattern.Replace("MMMM", "MMM") : pattern;
    }
}
