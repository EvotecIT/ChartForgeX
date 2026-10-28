using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    /// <summary>Draws geographic point facts using the caller's one source-to-scene projection.</summary>
    private static void DrawPoints(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect map, ChartRect plot,
        double dot, double?[] weights, double min, double max, VisualThemeColors colors, List<MapLabel> labels,
        List<LabelObstacle> obstacles, MapDataset data, Func<ChartPoint, ChartPoint> project, Func<ChartPoint, bool> visiblePoint) {
        var series = data.Series;
        for (var index = 0; index < series.RenderPoints.Count; index++) {
            var source = series.RenderPoints[index]; var viewportVisible = visiblePoint(source);
            var clustered = data.IsClustered(index); var visible = viewportVisible && !clustered; var point = project(source);
            var category = series.MapPoints[index].Label;
            var formatted = !weights[index].HasValue ? "" : series.MapPointSize.HasOverrides
                ? data.SizeScale!.Presentation.Value(weights[index]!.Value) : ChartNumericFormatter.FormatValue(chart.Options, weights[index]!.Value);
            var full = category + (formatted.Length > 0 ? ": " + formatted : "") + "; " + N(source.X) + ", " + N(source.Y);
            if (data.Scale != null || data.ColorValues[index].HasValue) full += "; " + data.Presentation.Name + ": "
                + (data.ColorValues[index].HasValue ? data.Presentation.Value(data.ColorValues[index]!.Value) : data.Presentation.NoData);
            var id = data.Prefix + "-point-" + index; var radius = data.SizeScale!.Radius(weights[index], dot);
            var bounds = visible ? new ChartRect(point.X - radius, point.Y - radius, radius * 2, radius * 2) : map;
            builder.AddRegion(new VisualSemanticRegion(id, "dotted-map-point", bounds, full));
            var metadata = new Dictionary<string, string> {
                ["data-cfx-series"] = N(data.Index), ["data-cfx-point"] = N(index), ["data-cfx-label"] = category, ["data-cfx-longitude"] = N(source.X), ["data-cfx-latitude"] = N(source.Y),
                ["data-cfx-value"] = weights[index].HasValue ? N(weights[index]!.Value) : "", ["data-cfx-formatted-value"] = formatted,
                ["data-cfx-visible"] = visible ? "true" : "false", ["aria-label"] = full
            };
            data.AddColorFacts(metadata, index);
            AddFeatureFacts(metadata, series.MapPoints[index].Feature);
            if (clustered) {
                metadata["data-cfx-clustered"] = "true"; metadata["data-cfx-viewport-visible"] = viewportVisible ? "true" : "false";
                metadata["data-cfx-cluster-grid"] = data.ClusterFor(index)!.Key;
            }
            if (series.MapPointSize.HasOverrides && weights[index].HasValue) {
                metadata["data-cfx-label-value"] = data.SizeScale.Presentation.Name;
                metadata["data-cfx-size-mapping"] = data.SizeScale.Mapping;
                metadata["data-cfx-size-min-value"] = N(data.SizeScale.MinimumValue); metadata["data-cfx-size-max-value"] = N(data.SizeScale.MaximumValue);
            }
            using (builder.PushGroup(id, "dotted-map-point-source", metadata)) {
                if (!visible) continue;
                var (color, paint) = data.PointPaint(index, colors);
                var halo = Math.Max(dot * 2.45, radius * 1.85);
                builder.Ellipse(point.X, point.Y, halo, halo, ChartColorMath.WithOpacity(color, .18), role: "dotted-map-point-halo", paint: VisualChartPaint.Fill(paint.WithOpacity(ChartColorMath.WithOpacity(color, .18), .18)));
                builder.Ellipse(point.X, point.Y, radius, radius, color, colors.Surface, Math.Max(1, dot * .55), "dotted-map-point", paint: new VisualScenePaintBinding(paint, SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
                obstacles.Add(new LabelObstacle(id, new ChartRect(point.X - halo, point.Y - halo, halo * 2, halo * 2)));
                if (series.ShowDataLabels ?? (chart.Options.ShowDataLabels ?? false)) {
                    var text = index < series.PointLabels.Count && series.PointLabels[index] != null ? series.PointLabels[index]!
                        : category + (formatted.Length > 0 ? " " + formatted : "");
                    var labelClearance = Math.Max(radius + context.Theme.Spacing, halo + context.Theme.Spacing / 3);
                    AddMapLabel(builder, labels, text, text, point, MapDataStyle(chart, context, index, series: series), id + "-label", "dotted-map-data-label", plot, labelClearance, 50,
                        series.DataLabelPlacement ?? chart.Options.DataLabelPlacement, leaderColor: color, leaderPaint: paint,
                        leaderRadius: radius + Math.Max(1, dot * .55) / 2, callouts: chart.Options.MapCallouts);
                }
            }
        }
        DrawMapClusters(chart, context, builder, data, plot, colors, obstacles, dot);
    }
}
