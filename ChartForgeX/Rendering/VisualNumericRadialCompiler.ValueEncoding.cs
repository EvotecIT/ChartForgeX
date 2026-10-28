using System;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class VisualNumericRadialCompiler {
    /// <summary>Projects canonical normalized endpoints for marks, grids and label anchors through one radius law.</summary>
    private static double ValueRadius(Chart chart, RadialSeriesGeometry geometry, double normalized) {
        // Preserve the existing arithmetic path for all default numeric radial output.
        if (chart.Options.RadialGeometry.ValueEncoding == ChartRadialValueEncoding.Radius)
            return geometry.Inner + normalized * (geometry.Outer - geometry.Inner);
        if (geometry.Outer <= 0) return 0;
        // Work in unit radii so large finite frames do not overflow when squared.
        var inner = geometry.Inner / geometry.Outer;
        return geometry.Outer * Math.Sqrt(inner * inner + normalized * (1 - inner * inner));
    }
}
