using System;
using System.Linq;

namespace ChartForgeX.Core;

internal static partial class ChartGuards {
    /// <summary>Rejects a dedicated-polar orientation on a different physical coordinate owner rather than silently ignoring it.</summary>
    private static void ValidatePolarOrientation(Chart chart) {
        if (chart.Options.PolarRotationDegrees != 0 && chart.Series.Any(series => series.Kind is not
            (ChartSeriesKind.Radar or ChartSeriesKind.Polar or ChartSeriesKind.PolarArea)))
            throw new NotSupportedException("Polar rotation requires dedicated Radar, Polar or PolarArea series; numeric radial compositions use RadialGeometry.");
    }
}
