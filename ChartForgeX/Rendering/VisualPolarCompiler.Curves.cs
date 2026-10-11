using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualPolarCompiler {
    /// <summary>Reserves curve overshoot while keeping every series and its guides on one radial scale.</summary>
    private static PolarLayout FitRadarCurves(Chart chart, PolarLayout geometry, double[] categories, RadialValueScale scale) {
        var extent = 1d;
        foreach (var series in chart.Series) {
            var tension = series.Radar.CurveTension ?? 0;
            if (tension == 0 || series.Points.Count == 0) continue;
            var points = categories.Select((category, index) => {
                var source = Find(series, category);
                var radius = scale.Normalize(source < 0 ? 0 : series.Points[source].Y);
                var angle = RadarAngle(index, categories.Length);
                return new ChartPoint(Math.Cos(angle) * radius, Math.Sin(angle) * radius);
            }).ToArray();
            var bounds = ChartPathBuilder.ClosedCurve(points, tension).Bounds();
            extent = Math.Max(extent, Math.Max(Math.Max(Math.Abs(bounds.Left), Math.Abs(bounds.Right)),
                Math.Max(Math.Abs(bounds.Top), Math.Abs(bounds.Bottom))));
        }
        return extent <= 1 ? geometry : new PolarLayout(geometry.Cx, geometry.Cy, geometry.Radius / extent);
    }
}
