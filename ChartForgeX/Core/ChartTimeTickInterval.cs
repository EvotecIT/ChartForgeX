using System;

namespace ChartForgeX.Core;

/// <summary>Defines the calendar boundary used by a fixed time tick interval.</summary>
public enum ChartTimeTickUnit {
    /// <summary>Milliseconds counted from the Unix epoch.</summary>
    Millisecond,
    /// <summary>Seconds within each minute.</summary>
    Second,
    /// <summary>Minutes within each hour.</summary>
    Minute,
    /// <summary>Hours within each day.</summary>
    Hour,
    /// <summary>Days within each month, starting at day one.</summary>
    Day,
    /// <summary>Weeks aligned to the selected weekday and counted from the Unix epoch's week.</summary>
    Week,
    /// <summary>Months within each year, starting at January.</summary>
    Month
}

/// <summary>An immutable calendar tick interval, independent of axis label formatting.</summary>
/// <remarks>Counts filter boundaries within the containing calendar unit: two days selects days 1, 3, 5, and so on.
/// Weeks and milliseconds use epoch alignment. Intervals exceeding the renderer's tick budget use automatic ticks.</remarks>
public sealed class ChartTimeTickInterval {
    /// <summary>Initializes a positive interval and, for weekly ticks, its starting weekday.</summary>
    public ChartTimeTickInterval(ChartTimeTickUnit unit, int count = 1, DayOfWeek weekStart = DayOfWeek.Monday) {
        if (!Enum.IsDefined(typeof(ChartTimeTickUnit), unit)) throw new ArgumentOutOfRangeException(nameof(unit));
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Tick interval count must be positive.");
        if (!Enum.IsDefined(typeof(DayOfWeek), weekStart)) throw new ArgumentOutOfRangeException(nameof(weekStart));
        Unit = unit; Count = count; WeekStart = weekStart;
    }

    /// <summary>Gets the calendar boundary unit.</summary>
    public ChartTimeTickUnit Unit { get; }
    /// <summary>Gets the positive boundary filter count.</summary>
    public int Count { get; }
    /// <summary>Gets the weekday used for weekly boundaries.</summary>
    public DayOfWeek WeekStart { get; }
}
