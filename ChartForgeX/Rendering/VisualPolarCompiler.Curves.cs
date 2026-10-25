using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualPolarCompiler {
    /// <summary>Reserves actual curve overshoot while keeping every series and guide on its selected shared radial scale.</summary>
    private static PolarLayout FitPolarCurves(Chart chart, PolarLayout geometry, ChartBarCoordinateMap? coordinates,
        ChartBarCoordinateKey[]? categories, IReadOnlyDictionary<ChartAxisSide, RadialValueScale> scales, bool radar) {
        var extent = 1d;
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            var tension = (radar ? series.Radar.CurveTension : series.Polar.CurveTension) ?? 0;
            if (tension == 0 || series.RenderPoints.Count == 0) continue;
            var scale = scales[series.ValueAxis];
            var sources = radar ? coordinates!.SourcesFor(seriesIndex, categories!) : Enumerable.Range(0, series.RenderPoints.Count).ToArray();
            var points = sources.Select((source, index) => {
                var value = source < 0 ? 0 : series.RenderPoints[source].Y;
                // Fit the original full-domain curve. The requested annulus crops later without moving its tangents or endpoints.
                var radius = radar ? scale.Normalize(value) : chart.PolarPreparation?.BaseNormalize(series.ValueAxis, scale, value) ?? scale.Normalize(value);
                var angle = radar ? CategoryAngle(chart, index, categories!.Length) : PolarAngle(chart, series.RenderPoints[source].X);
                return new ChartPoint(Math.Cos(angle) * radius, Math.Sin(angle) * radius, !radar && series.RenderPoints[source].BreakBefore);
            }).ToArray();
            var paths = radar ? ChartRadarGeometry.Paths(series.Radar, points, sources) : ChartPolarPathGeometry.Paths(series.Polar, points);
            foreach (var path in paths) {
                var bounds = path.Path.Bounds();
                extent = Math.Max(extent, Math.Max(Math.Max(Math.Abs(bounds.Left), Math.Abs(bounds.Right)),
                    Math.Max(Math.Abs(bounds.Top), Math.Abs(bounds.Bottom))));
            }
        }
        var fit = geometry.AvailableRadius <= 0 ? 1 : Math.Max(1, extent * geometry.Radius / geometry.AvailableRadius);
        return fit <= 1 ? geometry : new PolarLayout(geometry.Cx, geometry.Cy, geometry.Radius / fit, geometry.GridRadius / fit, geometry.AvailableRadius);
    }
}
