using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>A detached linear or time X projection captured from a supported prepared Cartesian chart.</summary>
/// <remarks>Bounds include resolved authored limits and automatic full-chart marker padding.
/// A requested viewport retains those full bounds while using its exact window for mapping and clipping.
/// This snapshot does not retain the chart, its axes, or formatter callbacks.</remarks>
public sealed class ChartXProjection {
    private readonly ChartViewportAxisProjection _axis;
    internal ChartXProjection(double fullMinimum, double fullMaximum, double minimum, double maximum, ChartRect plotBounds, bool reversed,
        ChartScaleKind scale = ChartScaleKind.Linear, ChartTimeSessionDomain? timeSessions = null) {
        PlotBounds = plotBounds;
        _axis = new ChartViewportAxisProjection(fullMinimum, fullMaximum, minimum, maximum, plotBounds.Left, plotBounds.Right,
            new ChartAxis { Scale = scale, Reversed = reversed, TimeSessions = timeSessions }, vertical: false);
    }

    /// <summary>Gets the lower resolved X bound before applying a viewport.</summary>
    public double FullMinimum => _axis.FullMinimum;
    /// <summary>Gets the upper resolved X bound before applying a viewport.</summary>
    public double FullMaximum => _axis.FullMaximum;
    /// <summary>Gets the lower X bound used by this prepared scene.</summary>
    public double Minimum => _axis.Minimum;
    /// <summary>Gets the upper X bound used by this prepared scene.</summary>
    public double Maximum => _axis.Maximum;
    /// <summary>Gets the final plot rectangle in logical scene units.</summary>
    public ChartRect PlotBounds { get; }
    /// <summary>Gets whether increasing source X runs from right to left.</summary>
    public bool Reversed => _axis.Reversed;
    /// <summary>Gets the source coordinate scale. Time coordinates use the chart's OLE Automation date units.</summary>
    public ChartScaleKind Scale => _axis.Scale;
    /// <summary>Gets the immutable included-time policy, or null for continuous linear/time coordinates.</summary>
    /// <remarks>Closed joins have one position for the preceding close and following opening. Inverse mapping
    /// chooses the following opening; raw authored window bounds and source identities remain unchanged.</remarks>
    public ChartTimeSessionDomain? TimeSessions => _axis.TimeSessions;
    internal double TransformedMinimum => _axis.TransformedMinimum;
    internal double TransformedMaximum => _axis.TransformedMaximum;
    internal double TransformedFullMinimum => _axis.TransformedFullMinimum;
    internal double TransformedFullMaximum => _axis.TransformedFullMaximum;
    /// <summary>Maps a finite source X coordinate into logical scene units.</summary>
    /// <param name="sourceX">A source coordinate, including coordinates outside the current window.</param>
    /// <returns>The mapped coordinate; values outside the window extrapolate beyond the plot.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The input or extrapolated result is not finite.</exception>
    public double Project(double sourceX) { return _axis.Project(sourceX); }

    /// <summary>Maps a finite logical X coordinate back into source coordinates.</summary>
    /// <param name="logicalX">A logical coordinate, including coordinates outside the plot.</param>
    /// <returns>The source coordinate; values outside the plot extrapolate beyond the current window.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The input or extrapolated result is not finite.</exception>
    public double Unproject(double logicalX) { return _axis.Unproject(logicalX); }

    /// <summary>Converts a logical gesture into an increasing X window contained in the captured full domain.</summary>
    /// <param name="firstLogicalX">The first logical X position, including positions outside the plot.</param>
    /// <param name="secondLogicalX">The second logical X position, including positions outside the plot.</param>
    /// <returns>The confined window, or null when the requested span covers the full domain and preparation should reset.</returns>
    /// <remarks>The captured axis reversal is honored. A narrower span that crosses an edge is shifted intact into
    /// the full domain rather than shortened. Explicit authored windows passed to Chart.Prepare remain strictly validated.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A position or mapped bound is not finite, or the requested span
    /// is empty or cannot be represented at the confined bounds.</exception>
    public ChartXViewport? ConstrainViewport(double firstLogicalX, double secondLogicalX) {
        var window = _axis.ConstrainLogical(firstLogicalX, secondLogicalX);
        return window.HasValue ? new ChartXViewport(window.Value.Minimum, window.Value.Maximum) : null;
    }

    /// <summary>Confines a source-coordinate window while preserving in-domain raw bounds exactly.</summary>
    /// <param name="requested">The increasing requested source bounds, including raw time bounds inside a closure.</param>
    /// <returns>The requested window when contained, a shifted confined window when crossing an edge,
    /// or null for the exact complete raw domain.</returns>
    /// <remarks>Included-time windows require positive projected width. An in-domain bound inside a closed gap
    /// is retained instead of silently replaced by the next opening. This matters for source observation visibility
    /// at a collapsed join, even when two raw windows paint the same axis span.</remarks>
    /// <exception cref="ArgumentNullException">The request is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The window contains no included time or cannot be mapped.</exception>
    public ChartXViewport? ConstrainViewport(ChartXViewport requested) {
        if (requested == null) throw new ArgumentNullException(nameof(requested));
        var window = _axis.ConstrainSource(requested.Minimum, requested.Maximum);
        return window.HasValue ? new ChartXViewport(window.Value.Minimum, window.Value.Maximum) : null;
    }
}
