using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static List<ChartPoint> MapSeriesPathPoints(ChartSeries series, ChartMapper map) {
        var mapped = new List<ChartPoint>(series.Points.Count);
        foreach (var point in series.Points) mapped.Add(new ChartPoint(map.X(point.X), map.Y(point.Y), point.BreakBefore));
        return ChartPathBuilder.FromPoints(mapped, series.Kind, series.Smooth).Flatten();
    }
}
