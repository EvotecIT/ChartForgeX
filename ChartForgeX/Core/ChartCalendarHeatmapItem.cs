using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one dated value in a calendar heatmap chart.
/// </summary>
public readonly struct ChartCalendarHeatmapItem {
    /// <summary>
    /// Gets the calendar date.
    /// </summary>
    public readonly DateTime Date;

    /// <summary>
    /// Gets the day value.
    /// </summary>
    public readonly double Value;

    /// <summary>
    /// Gets the optional day color.
    /// </summary>
    public readonly ChartColor? Color;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartCalendarHeatmapItem"/> struct.
    /// </summary>
    /// <param name="date">The calendar date. The time component is ignored.</param>
    /// <param name="value">The day value. Negative values are not allowed.</param>
    /// <param name="color">An optional color for this day.</param>
    public ChartCalendarHeatmapItem(DateTime date, double value, ChartColor? color = null) {
        ChartGuards.Finite(value, nameof(value));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Calendar heatmap values must be zero or greater.");
        // Calendar cells are wall-clock dates, not instants, so the kind is dropped before any UTC normalization.
        Date = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        Value = value;
        Color = color;
    }

    /// <summary>Gets optional immutable navigation, accessibility and host metadata.</summary>
    public ChartMarkOptions? MarkOptions { get; }

    /// <summary>Returns a copy with replacement mark metadata, retaining all authored values.</summary>
    /// <param name="options">Immutable metadata, or null to clear it.</param>
    /// <returns>A new observation. The current observation is unchanged.</returns>
    public ChartCalendarHeatmapItem WithMarkOptions(ChartMarkOptions? options) => new(this, options);

    private ChartCalendarHeatmapItem(ChartCalendarHeatmapItem source, ChartMarkOptions? options) {
        this = source;
        MarkOptions = options;
    }
}
