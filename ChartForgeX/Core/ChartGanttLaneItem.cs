using System;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one item in a Gantt lane, such as an incident: a start, an optional end, a category key resolved through
/// <see cref="ChartOptions.StateCategories"/> (for example a severity), a short label, and optional tooltip detail.
/// Times are OLE Automation dates holding UTC instants.
/// </summary>
public readonly struct ChartGanttLaneItem {
    /// <summary>Initializes an item from OLE Automation date values.</summary>
    /// <param name="start">The start instant.</param>
    /// <param name="end">The end instant, later than <paramref name="start"/>, or null while the item is still open;
    /// open items run to <see cref="ChartOptions.GanttToday"/> (a UTC instant, for example from <c>DateTime.UtcNow</c>)
    /// or, when that is not set, slightly past the latest time in the chart.</param>
    /// <param name="category">The category key, for example <c>critical</c>.</param>
    /// <param name="label">Optional short text drawn inside the bar when it fits.</param>
    /// <param name="detail">Optional detail text shown in tooltips.</param>
    public ChartGanttLaneItem(double start, double? end, string category, string? label = null, string? detail = null) {
        ChartGuards.Finite(start, nameof(start));
        if (end.HasValue) {
            ChartGuards.Finite(end.Value, nameof(end));
            if (end.Value <= start) throw new ArgumentOutOfRangeException(nameof(end), end, "Item end must be later than its start.");
        }

        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Item category must not be empty.", nameof(category));
        Start = start;
        End = end;
        Category = category;
        Label = string.IsNullOrWhiteSpace(label) ? null : label;
        Detail = string.IsNullOrWhiteSpace(detail) ? null : detail;
    }

    /// <summary>Initializes an item from date/time values. <see cref="DateTimeKind.Local"/> values are converted to UTC;
    /// <see cref="DateTimeKind.Unspecified"/> values are treated as UTC.</summary>
    /// <param name="start">The start.</param>
    /// <param name="end">The end, or null while the item is still open.</param>
    /// <param name="category">The category key, for example <c>critical</c>.</param>
    /// <param name="label">Optional short text drawn inside the bar when it fits.</param>
    /// <param name="detail">Optional detail text shown in tooltips.</param>
    public ChartGanttLaneItem(DateTime start, DateTime? end, string category, string? label = null, string? detail = null)
        : this(ToUtc(start).ToOADate(), end.HasValue ? ToUtc(end.Value).ToOADate() : null, category, label, detail) {
    }

    /// <summary>Gets the start instant as an OLE Automation date.</summary>
    public double Start { get; }

    /// <summary>Gets the end instant as an OLE Automation date, or null while the item is open.</summary>
    public double? End { get; }

    /// <summary>Gets a value indicating whether the item is still open.</summary>
    public bool IsOpen => !End.HasValue;

    /// <summary>Gets the category key.</summary>
    public string Category { get; }

    /// <summary>Gets optional short text drawn inside the bar.</summary>
    public string? Label { get; }

    /// <summary>Gets optional tooltip detail.</summary>
    public string? Detail { get; }

    private static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
}
