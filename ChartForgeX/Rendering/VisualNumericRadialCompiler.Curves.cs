using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualNumericRadialCompiler {
    /// <summary>Fits radar overshoot without changing the relative geometry of column marks, axes or the annular hole.</summary>
    private static RadialSeriesGeometry FitRadarCurves(Chart chart, RadialSeriesGeometry geometry, ChartBarCoordinateMap coordinates,
        ChartBarCoordinateKey[] categories, IReadOnlyDictionary<ChartAxisSide, RadialValueScale> scales) {
        if (geometry.Outer <= 0) return geometry;
        var unit = new RadialSeriesGeometry(0, 0, 1, geometry.Inner / geometry.Outer, geometry.Start, geometry.Sweep, 0);
        var extent = 1d;
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            var tension = series.Radar.CurveTension ?? 0;
            if (series.Kind != ChartSeriesKind.Radar || tension == 0 || series.RenderPoints.Count == 0) continue;
            var (points, _) = RadarPoints(chart, unit, index, coordinates, categories, scales[series.YAxis]);
            var bounds = ChartPathBuilder.ClosedCurve(points, tension).Bounds();
            extent = Math.Max(extent, Math.Max(Math.Max(Math.Abs(bounds.Left), Math.Abs(bounds.Right)),
                Math.Max(Math.Abs(bounds.Top), Math.Abs(bounds.Bottom))));
        }
        return extent <= 1 ? geometry : new RadialSeriesGeometry(geometry.Cx, geometry.Cy, geometry.Outer / extent,
            geometry.Inner / extent, geometry.Start, geometry.Sweep, geometry.TickLane);
    }
}
