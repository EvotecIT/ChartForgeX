using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets or sets immutable stroke overrides for connected lines, area edges, comparison stems, error bars and box outlines.</summary>
    /// <remarks>The style width takes precedence over an explicitly authored <see cref="StrokeWidth"/>, then the family/theme default.
    /// Null restores the legacy width and family paint. Radar and polar OutlineStroke settings take precedence over this series style.
    /// Fills and point markers retain their independent styling.</remarks>
    public ChartStrokeStyle? StrokeStyle { get; set; }

    /// <summary>Gets the stroke segments that restyle a line, step line, spline or area outline from a source X onward.</summary>
    /// <remarks>Segments apply in <see cref="ChartStrokeSegment.FromX"/> order; stroke before the first one uses <see cref="StrokeStyle"/>.
    /// Markers, fills and labels keep their own styling. Other series kinds reject segments during preparation.</remarks>
    public List<ChartStrokeSegment> StrokeSegments { get; } = new();

    /// <summary>Sets the series line stroke, or clears it to restore legacy width and family paint.</summary>
    /// <param name="style">The immutable stroke overrides, or null to clear them.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithStrokeStyle(ChartStrokeStyle? style) {
        StrokeStyle = style;
        return this;
    }

    /// <summary>Restyles the line from a source X onward, for example dashing a forecast after the last actual point.</summary>
    /// <param name="fromX">The source X where the style starts.</param>
    /// <param name="style">The stroke overrides; unset members inherit <see cref="StrokeStyle"/>.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithStrokeStyleFrom(double fromX, ChartStrokeStyle style) {
        StrokeSegments.Add(new ChartStrokeSegment(fromX, style));
        return this;
    }

    /// <summary>Restyles the line from a date/time X onward, for example dashing a forecast after the last actual point.</summary>
    /// <param name="fromX">The date/time where the style starts.</param>
    /// <param name="style">The stroke overrides; unset members inherit <see cref="StrokeStyle"/>.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithStrokeStyleFrom(DateTime fromX, ChartStrokeStyle style) {
        StrokeSegments.Add(new ChartStrokeSegment(fromX, style));
        return this;
    }

    internal void ValidateStrokeSegments() {
        if (StrokeSegments.Count == 0) return;
        if (StrokeSegments.Contains(null!)) throw new InvalidOperationException("Series '" + Name + "' stroke segments must not contain null.");
        if (Kind is not (ChartSeriesKind.Line or ChartSeriesKind.StepLine or ChartSeriesKind.Area or ChartSeriesKind.StepArea or ChartSeriesKind.StackedArea))
            throw new InvalidOperationException("Series '" + Name + "' stroke segments are supported only for line, step-line, area, step-area and stacked-area series.");
    }
}
