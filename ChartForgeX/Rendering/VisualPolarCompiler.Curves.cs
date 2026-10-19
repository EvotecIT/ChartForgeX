using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualPolarCompiler {
    /// <summary>Reserves curve overshoot while keeping every series and its guides on one radial scale.</summary>
    private static PolarLayout FitRadarCurves(Chart chart, PolarLayout geometry, ChartBarCoordinateMap coordinates,
        ChartBarCoordinateKey[] categories, IReadOnlyDictionary<ChartAxisSide, RadialValueScale> scales) {
        var extent = 1d;
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            var tension = series.Radar.CurveTension ?? 0;
            if (tension == 0 || series.RenderPoints.Count == 0) continue;
            var scale = scales[series.YAxis];
            var sources = coordinates.SourcesFor(seriesIndex, categories);
            var points = categories.Select((category, index) => {
                var source = sources[index];
                var radius = scale.Normalize(source < 0 ? 0 : series.RenderPoints[source].Y);
                var angle = CategoryAngle(chart.Options.XAxis, index, categories.Length);
                return new ChartPoint(Math.Cos(angle) * radius, Math.Sin(angle) * radius);
            }).ToArray();
            foreach (var path in ChartRadarGeometry.Paths(series.Radar, points, sources)) {
                var bounds = path.Path.Bounds();
                extent = Math.Max(extent, Math.Max(Math.Max(Math.Abs(bounds.Left), Math.Abs(bounds.Right)),
                    Math.Max(Math.Abs(bounds.Top), Math.Abs(bounds.Bottom))));
            }
        }
        var fit = geometry.AvailableRadius <= 0 ? 1 : Math.Max(1, extent * geometry.Radius / geometry.AvailableRadius);
        return fit <= 1 ? geometry : new PolarLayout(geometry.Cx, geometry.Cy, geometry.Radius / fit, geometry.GridRadius / fit, geometry.AvailableRadius);
    }
}
