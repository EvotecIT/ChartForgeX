using System;

namespace ChartForgeX.Primitives;

/// <summary>
/// Converts date/time inputs to chart values with one rule: values are UTC instants stored as OLE Automation dates.
/// <see cref="DateTimeKind.Local"/> values are converted to UTC; <see cref="DateTimeKind.Unspecified"/> values are
/// treated as UTC.
/// </summary>
internal static class ChartDateTime {
    public static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static double ToOADate(DateTime value) => ToUtc(value).ToOADate();
}
