using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>A detached primary Y projection captured from supported vertical Cartesian preparation.</summary>
/// <remarks>Increasing values run upward unless the authored Y axis is reversed. Source transforms and confinement
/// share the X projection owner. This snapshot retains no mutable chart, axis, or formatter callback.</remarks>
public sealed class ChartYProjection {
    private readonly ChartViewportAxisProjection _axis;
    internal ChartYProjection(double fullMinimum, double fullMaximum, double minimum, double maximum, ChartRect plotBounds, ChartAxis axis) {
        PlotBounds = plotBounds;
        _axis = new ChartViewportAxisProjection(fullMinimum, fullMaximum, minimum, maximum, plotBounds.Top, plotBounds.Bottom, axis, vertical: true);
    }
    /// <summary>Gets the resolved full lower source bound before applying a window.</summary>
    public double FullMinimum => _axis.FullMinimum;
    /// <summary>Gets the resolved full upper source bound before applying a window.</summary>
    public double FullMaximum => _axis.FullMaximum;
    /// <summary>Gets the lower source bound used by this scene.</summary>
    public double Minimum => _axis.Minimum;
    /// <summary>Gets the upper source bound used by this scene.</summary>
    public double Maximum => _axis.Maximum;
    /// <summary>Gets the final logical plot rectangle.</summary>
    public ChartRect PlotBounds { get; }
    /// <summary>Gets whether increasing source Y runs downward.</summary>
    public bool Reversed => _axis.Reversed;
    /// <summary>Gets the captured source scale.</summary>
    public ChartScaleKind Scale => _axis.Scale;
    internal double Threshold => _axis.Threshold;
    internal double TransformedMinimum => _axis.TransformedMinimum;
    internal double TransformedMaximum => _axis.TransformedMaximum;
    internal double TransformedFullMinimum => _axis.TransformedFullMinimum;
    internal double TransformedFullMaximum => _axis.TransformedFullMaximum;
    /// <summary>Projects a finite source value, extrapolating outside the current window.</summary>
    /// <param name="sourceY">The source value; logarithmic values must be positive.</param>
    /// <returns>The logical Y coordinate.</returns>
    public double Project(double sourceY) => _axis.Project(sourceY);
    /// <summary>Inverts a finite logical coordinate through the captured source scale.</summary>
    /// <param name="logicalY">The logical Y coordinate, including positions outside the plot.</param>
    /// <returns>The source value.</returns>
    public double Unproject(double logicalY) => _axis.Unproject(logicalY);
    /// <summary>Confines a logical gesture while preserving its transformed span.</summary>
    /// <param name="firstLogicalY">The first logical position.</param>
    /// <param name="secondLogicalY">The second logical position.</param>
    /// <returns>The confined increasing source window, or null when it covers the full domain.</returns>
    public ChartYViewport? ConstrainViewport(double firstLogicalY, double secondLogicalY) {
        var window = _axis.ConstrainLogical(firstLogicalY, secondLogicalY);
        return window.HasValue ? new ChartYViewport(window.Value.Minimum, window.Value.Maximum) : null;
    }
    /// <summary>Confines a source window, preserving exact in-domain bounds.</summary>
    /// <param name="requested">The requested source window.</param>
    /// <returns>The confined window, or null for the exact full domain.</returns>
    public ChartYViewport? ConstrainViewport(ChartYViewport requested) {
        if (requested == null) throw new ArgumentNullException(nameof(requested));
        var window = _axis.ConstrainSource(requested.Minimum, requested.Maximum);
        return window.HasValue ? new ChartYViewport(window.Value.Minimum, window.Value.Maximum) : null;
    }
}
