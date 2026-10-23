using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>A detached physical Cartesian axis captured after native layout and painted-extent fitting.</summary>
/// <remarks>Captures transforms, reversal, sign policy and full/window domains without retaining mutable charts or formatter callbacks.</remarks>
public sealed class ChartAxisProjection {
    private readonly ChartViewportAxisProjection _axis;
    internal ChartAxisProjection(double fullMinimum, double fullMaximum, double minimum, double maximum,
        ChartRect plotBounds, ChartAxis axis, ChartAxisDimension dimension, ChartAxisSide side = ChartAxisSide.Primary) {
        PlotBounds = plotBounds; Dimension = dimension; Side = side;
        _axis = new ChartViewportAxisProjection(fullMinimum, fullMaximum, minimum, maximum,
            dimension == ChartAxisDimension.X ? plotBounds.Left : plotBounds.Top,
            dimension == ChartAxisDimension.X ? plotBounds.Right : plotBounds.Bottom, axis, dimension == ChartAxisDimension.Y);
    }
    /// <summary>Gets the physical direction of the axis.</summary>
    public ChartAxisDimension Dimension { get; }
    /// <summary>Gets the primary or secondary slot.</summary>
    public ChartAxisSide Side { get; }
    /// <summary>Gets the resolved full lower source bound before the viewport.</summary>
    public double FullMinimum => _axis.FullMinimum;
    /// <summary>Gets the resolved full upper source bound before the viewport.</summary>
    public double FullMaximum => _axis.FullMaximum;
    /// <summary>Gets the lower source bound used by this scene.</summary>
    public double Minimum => _axis.Minimum;
    /// <summary>Gets the upper source bound used by this scene.</summary>
    public double Maximum => _axis.Maximum;
    /// <summary>Gets whether observations exactly at the current upper source bound belong to this window.</summary>
    public bool MaximumInclusive => _axis.MaximumInclusive;
    /// <summary>Gets the final logical plot rectangle.</summary>
    public ChartRect PlotBounds { get; }
    /// <summary>Gets whether the authored axis is reversed.</summary>
    public bool Reversed => _axis.Reversed;
    /// <summary>Gets the captured scale.</summary>
    public ChartScaleKind Scale => _axis.Scale;
    /// <summary>Gets the captured logarithm base.</summary>
    public double LogarithmBase => _axis.LogarithmBase;
    /// <summary>Gets the captured logarithmic sign policy.</summary>
    public ChartLogarithmicDomain LogarithmicDomain => _axis.LogarithmicDomain;
    /// <summary>Gets the immutable included-time policy, or null for continuous coordinates.</summary>
    public ChartTimeSessionDomain? TimeSessions => _axis.TimeSessions;
    /// <summary>Gets captured ordinal or calendar-band time spacing, or null for elapsed time.</summary>
    public ChartTimeSpacing? TimeSpacing => _axis.TimeDomain?.Spacing;
    internal ChartTimeSpacingDomain? TimeDomain => _axis.TimeDomain;
    internal double Threshold => _axis.Threshold;
    internal double TransformedMinimum => _axis.TransformedMinimum;
    internal double TransformedMaximum => _axis.TransformedMaximum;
    internal double TransformedFullMinimum => _axis.TransformedFullMinimum;
    internal double TransformedFullMaximum => _axis.TransformedFullMaximum;
    /// <summary>Projects a finite source value, extrapolating beyond the current window.</summary>
    public double Project(double source) => _axis.Project(source);
    /// <summary>Inverts a finite logical coordinate through the captured scale. Unit time selects the canonical lower calendar boundary.</summary>
    public double Unproject(double logical) => _axis.Unproject(logical);
    /// <summary>Confines a logical gesture, shifting a narrower crossing span intact into the full domain.</summary>
    /// <returns>The confined source window, or null when it covers the full domain. Calendar gestures use an exclusive canonical upper edge.</returns>
    public ChartAxisWindow? ConstrainViewport(double firstLogical, double secondLogical) => _axis.ConstrainLogical(firstLogical, secondLogical);
    /// <summary>Confines a source window, preserving exact contained bounds and their upper-endpoint policy.</summary>
    /// <remarks>Unit time spacing accepts exclusive maxima only at captured canonical calendar boundaries.</remarks>
    /// <returns>The confined window, or null for the exact complete raw domain.</returns>
    public ChartAxisWindow? ConstrainViewport(ChartAxisWindow requested) {
        if (requested == null) throw new ArgumentNullException(nameof(requested));
        return _axis.ConstrainSource(requested);
    }
}
