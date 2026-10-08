using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public static partial class ChartPoints {
    /// <summary>Creates one-based finite points, preserving missing slots as horizontal gaps and segment breaks.</summary>
    public static ChartPoint[] FromValues(IEnumerable<double?> values, ChartMissingDataPolicy missingDataPolicy = ChartMissingDataPolicy.Gap) =>
        ChartSamplePoints.FromValues(values, missingDataPolicy);

    /// <summary>Creates finite points from matching positions and nullable observations. Missing slots retain their supplied positions.</summary>
    public static ChartPoint[] FromXY(IEnumerable<double> x, IEnumerable<double?> y, ChartMissingDataPolicy missingDataPolicy = ChartMissingDataPolicy.Gap) =>
        ChartSamplePoints.FromXY(x, y, missingDataPolicy);
}
