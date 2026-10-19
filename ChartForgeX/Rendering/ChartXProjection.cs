using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>A detached linear or time X projection captured from a supported prepared Cartesian chart.</summary>
/// <remarks>Bounds include resolved authored limits and automatic full-chart marker padding.
/// A requested viewport retains those full bounds while using its exact window for mapping and clipping.
/// This snapshot does not retain the chart, its axes, or formatter callbacks.</remarks>
public sealed class ChartXProjection {
    internal ChartXProjection(double fullMinimum, double fullMaximum, double minimum, double maximum, ChartRect plotBounds, bool reversed,
        ChartScaleKind scale = ChartScaleKind.Linear) {
        FullMinimum = fullMinimum; FullMaximum = fullMaximum;
        Minimum = minimum; Maximum = maximum; PlotBounds = plotBounds; Reversed = reversed; Scale = scale;
    }

    /// <summary>Gets the lower resolved X bound before applying a viewport.</summary>
    public double FullMinimum { get; }
    /// <summary>Gets the upper resolved X bound before applying a viewport.</summary>
    public double FullMaximum { get; }
    /// <summary>Gets the lower X bound used by this prepared scene.</summary>
    public double Minimum { get; }
    /// <summary>Gets the upper X bound used by this prepared scene.</summary>
    public double Maximum { get; }
    /// <summary>Gets the final plot rectangle in logical scene units.</summary>
    public ChartRect PlotBounds { get; }
    /// <summary>Gets whether increasing source X runs from right to left.</summary>
    public bool Reversed { get; }
    /// <summary>Gets the source coordinate scale. Time coordinates use the chart's OLE Automation date units.</summary>
    public ChartScaleKind Scale { get; }

    /// <summary>Maps a finite source X coordinate into logical scene units.</summary>
    /// <param name="sourceX">A source coordinate, including coordinates outside the current window.</param>
    /// <returns>The mapped coordinate; values outside the window extrapolate beyond the plot.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The input or extrapolated result is not finite.</exception>
    public double Project(double sourceX) {
        ChartPrimitiveGuards.Finite(sourceX, nameof(sourceX));
        var result = ChartMapper.ProjectX(ChartScaleTransform.Normalize(sourceX, Minimum, Maximum, Reversed), PlotBounds);
        ChartPrimitiveGuards.Finite(result, nameof(sourceX));
        return result;
    }

    /// <summary>Maps a finite logical X coordinate back into source coordinates.</summary>
    /// <param name="logicalX">A logical coordinate, including coordinates outside the plot.</param>
    /// <returns>The source coordinate; values outside the plot extrapolate beyond the current window.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The input or extrapolated result is not finite.</exception>
    public double Unproject(double logicalX) {
        ChartPrimitiveGuards.Finite(logicalX, nameof(logicalX));
        var ratio = ChartMath.Normalize(logicalX, PlotBounds.Left, PlotBounds.Right);
        var result = ChartMath.InterpolateOrExtrapolateRange(Minimum, Maximum, Reversed ? 1 - ratio : ratio);
        ChartPrimitiveGuards.Finite(result, nameof(logicalX));
        return result;
    }

    /// <summary>Converts a logical gesture into an increasing X window contained in the captured full domain.</summary>
    /// <param name="firstLogicalX">The first logical X position, including positions outside the plot.</param>
    /// <param name="secondLogicalX">The second logical X position, including positions outside the plot.</param>
    /// <returns>The confined window, or null when the requested span covers the full domain and preparation should reset.</returns>
    /// <remarks>The captured axis reversal is honored. A narrower span that crosses an edge is shifted intact into
    /// the full domain rather than shortened. Explicit authored windows passed to Chart.Prepare remain strictly validated.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A position or mapped bound is not finite, or the requested span
    /// is empty or cannot be represented at the confined bounds.</exception>
    public ChartXViewport? ConstrainViewport(double firstLogicalX, double secondLogicalX) {
        var first = Unproject(firstLogicalX);
        var second = Unproject(secondLogicalX);
        var minimum = Math.Min(first, second);
        var maximum = Math.Max(first, second);
        if (maximum <= minimum)
            throw new ArgumentOutOfRangeException(nameof(secondLogicalX), secondLogicalX, "The gesture requires a nonempty X span.");
        var span = maximum - minimum;
        var fullSpan = FullMaximum - FullMinimum;
        // Halving opposite-sign extremes avoids overflowing either span during the comparison.
        var spanIsFinite = ChartMath.IsFinite(span);
        var fullSpanIsFinite = ChartMath.IsFinite(fullSpan);
        var halfSpan = maximum / 2 - minimum / 2;
        var fullHalfSpan = FullMaximum / 2 - FullMinimum / 2;
        if (spanIsFinite && fullSpanIsFinite ? span >= fullSpan
            : halfSpan >= fullHalfSpan) return null;
        if (minimum < FullMinimum) {
            minimum = FullMinimum;
            maximum = spanIsFinite ? Math.Min(FullMaximum, minimum + span)
                : ChartMath.InterpolateRange(FullMinimum, FullMaximum, halfSpan / fullHalfSpan);
        } else if (maximum > FullMaximum) {
            maximum = FullMaximum;
            minimum = spanIsFinite ? Math.Max(FullMinimum, maximum - span)
                : ChartMath.InterpolateRange(FullMinimum, FullMaximum, 1 - halfSpan / fullHalfSpan);
        }
        return new ChartXViewport(minimum, maximum);
    }
}
