using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Paints the same immutable fit samples used for full and visible native domains.</summary>
    private static void DrawTrendLineFit(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles,
        ChartAxisWindow? xViewport, ChartAxisWindow? yViewport) {
        var series = chart.Series[index]; var samples = series.TrendLineSamples;
        var options = series.CapturedTrendLineOptions;
        var curve = new List<ChartPoint>(samples.Count); var lower = new List<ChartPoint>(samples.Count); var upper = new List<ChartPoint>(samples.Count);
        foreach (var sample in samples) {
            curve.Add(new ChartPoint(map.X(sample.X), map.Y(sample.Value)));
            if (sample.Lower.HasValue) {
                lower.Add(new ChartPoint(map.X(sample.X), map.Y(sample.Lower.Value)));
                upper.Add(new ChartPoint(map.X(sample.X), map.Y(sample.Upper!.Value)));
            }
        }
        var color = Color(series, index, colors); var paint = VisualChartPaint.Series(series, color);
        if (lower.Count > 0) {
            var polygon = ChartPairedBoundaryGeometry.Envelope(upper, lower);
            var opacity = context.Theme.AreaWash(context.ThemeMode); var fill = ChartColorMath.WithOpacity(color, opacity);
            builder.Path(polygon, fill, role: "trend-line-interval", close: true,
                paint: VisualChartPaint.Fill(paint.WithOpacity(fill, opacity)));
            var outline = new List<ChartPoint>(upper);
            for (var point = lower.Count - 1; point >= 0; point--) outline.Add(lower[point]);
            obstacles.Add(new LabelObstacle(SeriesId(index) + "-interval", new LabelMarkShape(new[] { outline }, true, 0, plot)));
        }
        var path = ChartPathBuilder.FromPoints(curve, ChartInterpolation.Linear);
        var stroke = Math.Max(ChartVisualPrimitives.TrendLineMinStrokeWidth, SeriesStroke(series, context));
        DrawLayeredPath(chart, builder, path, color, paint, stroke, "trend-line", new[] { 8d, 6d });
        obstacles.Add(new LabelObstacle(SeriesId(index) + "-line", new LabelMarkShape(new[] { curve }, false, stroke, plot)));
        var metadata = new Dictionary<string, string> {
            ["data-cfx-source-count"] = Number(series.TrendLineSourcePoints.Count),
            ["data-cfx-regression-model"] = options.Model.ToString().ToLowerInvariant(),
            ["data-cfx-fit-target"] = series.TrendLineLogResponse ? "median" : "mean",
            ["data-cfx-fit-response-scale"] = series.TrendLineLogResponse ? "log" : "identity",
            ["data-cfx-pointwise-interval"] = options.Interval.ToString().ToLowerInvariant()
        };
        if (options.Model == ChartTrendLineModel.Polynomial) metadata["data-cfx-polynomial-degree"] = Number(options.PolynomialDegree);
        if (options.Interval != ChartTrendLineIntervalKind.None) metadata["data-cfx-confidence-level"] = Number(options.ConfidenceLevel);
        if (options.Model == ChartTrendLineModel.Linear) {
            var first = samples[0]; var last = samples[samples.Count - 1];
            var slope = (last.Value - first.Value) / (last.X - first.X);
            var intercept = first.Value - slope * first.X;
            if (!double.IsNaN(slope) && !double.IsInfinity(slope) && !double.IsNaN(intercept) && !double.IsInfinity(intercept)) {
                metadata["data-cfx-slope"] = Number(slope); metadata["data-cfx-intercept"] = Number(intercept);
            } else metadata["data-cfx-source-coefficients"] = "not-representable";
        }
        builder.PushGroup(SeriesId(index) + "-regression", "regression", metadata).Dispose();
        // The fitted path and ribbon do not create source observations or hundreds of new keyboard targets.
        for (var endpoint = 0; endpoint < 2; endpoint++) {
            var sample = samples[endpoint == 0 ? 0 : samples.Count - 1]; var point = curve[endpoint == 0 ? 0 : curve.Count - 1];
            var bounds = Extents(point.X, point.Y, point.X, point.Y, 0);
            var member = series.TrendLineLogResponse ? "median" : "value";
            var label = ResolveObservationLabel(chart, context, series, endpoint, colors, format => format(member, sample.Value));
            var visible = ViewportObservationVisible(xViewport, yViewport, sample.X, sample.Value, sample.Value, map);
            using (ObservationGroup(builder, series, index, endpoint, endpoint, 1, bounds, label, visible,
                new Dictionary<string, string> { ["data-cfx-fit-target"] = series.TrendLineLogResponse ? "median" : "mean" },
                ("x", sample.X), (member, sample.Value))) { }
            if (visible) ObservationLabel(chart, context, series, index, endpoint, point, bounds, sample.Value, label, labels, obstacles);
        }
    }
}
