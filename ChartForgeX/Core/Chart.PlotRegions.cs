using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Gets the owned Cartesian background regions, painted in insertion order.</summary>
    public List<ChartPlotRegion> PlotRegions { get; } = new();

    /// <summary>Adds a passive background region without changing the chart's axis domains or series.</summary>
    /// <param name="region">The immutable region with data-coordinate bounds and an optional caption.</param>
    /// <returns>The current chart.</returns>
    public Chart AddPlotRegion(ChartPlotRegion region) {
        PlotRegions.Add(region ?? throw new ArgumentNullException(nameof(region)));
        return this;
    }

    internal void ValidatePlotRegions() {
        if (PlotRegions.Count == 0) return;
        if (Series.Any(series => !VisualCartesianCompiler.Supports(series.Kind)))
            throw new NotSupportedException("Plot regions require a Cartesian chart.");
        foreach (var region in PlotRegions) {
            if (region == null) throw new InvalidOperationException("Plot regions collection must not contain null entries.");
            if (region.YAxis == ChartAxisSide.Secondary && !Series.Any(series => series.YAxis == ChartAxisSide.Secondary))
                throw new InvalidOperationException("A secondary plot region requires a series on the secondary Y axis.");
            var yAxis = region.YAxis == ChartAxisSide.Secondary ? Options.SecondaryYAxis : Options.YAxis;
            PositiveLogBounds(Options.XAxis, region.XMinimum, region.XMaximum);
            // Horizontal charts project physical Y as linear category slots, regardless
            // of the stored category-axis scale, just as ForHorizontalBars does.
            if (!ChartCartesianOrientation.IsHorizontal(this)) PositiveLogBounds(yAxis, region.YMinimum, region.YMaximum);
        }
    }

    private static void PositiveLogBounds(ChartAxis axis, double? minimum, double? maximum) {
        if (axis.Scale == ChartScaleKind.Logarithmic && (minimum.HasValue && minimum.Value <= 0 || maximum.HasValue && maximum.Value <= 0))
            throw new InvalidOperationException("Logarithmic plot regions require positive authored bounds.");
    }
}
