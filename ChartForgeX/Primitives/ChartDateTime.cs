using System;

namespace ChartForgeX.Primitives;

/// <summary>
/// Converts date/time inputs to chart values with one rule: values are UTC instants stored as OLE Automation dates.
/// <see cref="DateTimeKind.Local"/> values are converted to UTC; <see cref="DateTimeKind.Unspecified"/> values are
/// treated as UTC.
/// </summary>
internal static class ChartDateTime {
    private static readonly long OaEpochTicks = new DateTime(1899, 12, 30, 0, 0, 0, DateTimeKind.Utc).Ticks;
    private const long DisplayResolutionTicks = 1_000; // 100 microseconds remain distinguishable across the DateTime range.

    public static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static double ToOADate(DateTime value) {
        DateTime utc = ToUtc(value);
        // Match DateTime axis labels at the documented display resolution. DateTime.ToOADate()
        // discards submillisecond ticks even when the resulting double can represent them.
        return utc.Ticks >= OaEpochTicks ? ToOrderedOaDate(utc, nameof(value)) : utc.ToOADate();
    }

    /// <summary>Converts an ordered UTC instant at the time-axis display resolution of 100 microseconds.</summary>
    public static double ToOrderedOaDate(DateTime value, string parameterName) {
        DateTime utc = ToUtc(value);
        if (utc.Ticks < OaEpochTicks) {
            throw new ArgumentOutOfRangeException(parameterName, value,
                "Time-axis DateTime values must be on or after 1899-12-30 UTC; earlier OLE dates are not chronologically ordered.");
        }

        return (RoundToDisplayResolution(utc.Ticks) - OaEpochTicks) / (double)TimeSpan.TicksPerDay;
    }

    /// <summary>Converts an interval end and reports when its two instants collapse at display resolution.</summary>
    public static double ToOrderedOaEnd(DateTime start, DateTime end, string parameterName) {
        double startValue = ToOrderedOaDate(start, nameof(start));
        double endValue = ToOrderedOaDate(end, parameterName);
        if (ToUtc(end).Ticks > ToUtc(start).Ticks && endValue <= startValue) {
            throw new ArgumentOutOfRangeException(parameterName, end,
                "The interval is shorter than the time axis's 100-microsecond DateTime resolution.");
        }
        return endValue;
    }

    /// <summary>Decodes an ordered OLE date to the time-axis display resolution.</summary>
    public static DateTime FromOrderedOaDate(double value) {
        long ticks = checked(OaEpochTicks + (long)Math.Round(value * TimeSpan.TicksPerDay));
        return new DateTime(RoundToDisplayResolution(ticks), DateTimeKind.Utc);
    }

    private static long RoundToDisplayResolution(long ticks) {
        long whole = ticks - ticks % DisplayResolutionTicks;
        return ticks - whole >= DisplayResolutionTicks / 2 && whole <= DateTime.MaxValue.Ticks - DisplayResolutionTicks
            ? whole + DisplayResolutionTicks : whole;
    }
}
