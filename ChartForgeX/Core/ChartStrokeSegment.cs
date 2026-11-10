using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>Restyles a connected series stroke from a source X coordinate onward, such as a dashed forecast after the last actual.</summary>
/// <remarks>The segment runs until the next segment's <see cref="FromX"/>. Unset style members inherit the series
/// <see cref="ChartSeries.StrokeStyle"/>, then the series color and theme width, so a forecast usually sets only its dash. Its opacity multiplies the series stroke opacity.</remarks>
public sealed class ChartStrokeSegment {
    /// <summary>Creates a stroke segment starting at a numeric source X coordinate.</summary>
    /// <param name="fromX">The finite source X where the style starts. Stroke exactly at this X belongs to the segment.</param>
    /// <param name="style">The stroke overrides applied from <paramref name="fromX"/> onward.</param>
    public ChartStrokeSegment(double fromX, ChartStrokeStyle style) {
        ChartGuards.Finite(fromX, nameof(fromX));
        FromX = fromX;
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Creates a stroke segment starting at a date/time source X coordinate.</summary>
    /// <param name="fromX">The date/time where the style starts.</param>
    /// <param name="style">The stroke overrides applied from <paramref name="fromX"/> onward.</param>
    public ChartStrokeSegment(DateTime fromX, ChartStrokeStyle style) : this(ChartDateTime.ToOADate(fromX), style) {
    }

    /// <summary>Gets the source X where the style starts.</summary>
    public double FromX { get; }

    /// <summary>Gets the stroke overrides applied from <see cref="FromX"/> until the next segment.</summary>
    public ChartStrokeStyle Style { get; }
}
