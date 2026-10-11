using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>A detached linear X projection captured from a prepared Line or StepLine chart.</summary>
/// <remarks>Bounds include resolved authored limits and automatic full-chart marker padding.
/// A requested viewport retains those full bounds while using its exact window for mapping and clipping.
/// This snapshot does not retain the chart, its axes, or formatter callbacks.</remarks>
public sealed class ChartXProjection {
    internal ChartXProjection(double fullMinimum, double fullMaximum, double minimum, double maximum, ChartRect plotBounds, bool reversed) {
        FullMinimum = fullMinimum; FullMaximum = fullMaximum;
        Minimum = minimum; Maximum = maximum; PlotBounds = plotBounds; Reversed = reversed;
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
}
