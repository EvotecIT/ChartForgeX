using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualNumericRadialCompiler {
    /// <summary>Draws radar forms over the category centers and selected radius scale of the numeric radial snapshot.</summary>
    private static void DrawRadar(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        RadialSeriesGeometry geometry, int seriesIndex, ChartBarCoordinateMap coordinates, ChartBarCoordinateKey[] categories,
        string[] categoryLabels, Dictionary<ChartAxisSide, RadialValueScale> scales, VisualThemeColors colors,
        List<RadialSeriesLabel> labels, List<LabelObstacle>? obstacles) {
        var series = chart.Series[seriesIndex];
        if (series.RenderPoints.Count == 0) return;
        var scale = scales[series.ValueAxis]; var color = ChartSeriesColours.Resolve(series, seriesIndex, colors);
        var (mapped, raw) = RadarPoints(chart, geometry, seriesIndex, coordinates, categories, scale);
        var paths = ChartRadarGeometry.Paths(series.Radar, mapped, raw, geometry.Sweep == Math.PI * 2);
        ChartRadarGeometry.DiagnoseOpenArea(builder, series, paths);
        using (builder.PushGroup(SeriesId(seriesIndex), "radar-series", new Dictionary<string, string> {
            ["data-cfx-series"] = N(seriesIndex), ["data-cfx-series-key"] = series.InteractionIdentityKey,
            ["data-cfx-label"] = series.Name, ["data-cfx-axis"] = series.ValueAxis.ToString().ToLowerInvariant(),
            ["data-cfx-form"] = series.Radar.Form == ChartLineAreaForm.Area ? "area" : "line", ["data-cfx-missing-policy"] = ChartRadarGeometry.MissingPolicy(series.Radar),
            ["data-cfx-min-value"] = N(scale.Minimum), ["data-cfx-max-value"] = N(scale.Maximum)
        })) {
            // Shared typed clipping keeps the annular hole and partial angular limits in both exporters.
            using (builder.PushClip(RadarClip(geometry, plot))) {
                if (geometry.Outer > 0) foreach (var path in paths)
                    VisualPolarSeriesScene.Draw(chart, context, builder, series, path.Path, color, true, path.Closed);
                for (var category = 0; category < categories.Length; category++) {
                    var source = raw[category]; var value = source >= 0 ? series.RenderPoints[source].Y : 0;
                    if (source < 0 && series.Radar.MissingCategoryPolicy != ChartRadarMissingCategoryPolicy.Zero) continue;
                    var angle = CategoryAngle(chart.Options.XAxis, category, categories.Length, geometry.Start, geometry.Sweep, false);
                    var text = source >= 0 && source < series.PointLabels.Count && series.PointLabels[source] != null
                        ? series.PointLabels[source]! : ChartNumericFormatter.FormatValue(chart.Options, value);
                    var id = source >= 0 ? PointId(seriesIndex, source) : SeriesId(seriesIndex) + "-missing-category-" + category;
                    var marker = VisualMarkerScene.Radius(series, context);
                    var bounds = geometry.Outer > 0 ? VisualMarkerScene.Bounds(series, mapped[category].X, mapped[category].Y, marker)
                        : new ChartRect(mapped[category].X, mapped[category].Y, 0, 0);
                    builder.AddRegion(new VisualSemanticRegion(id, "radar-point", bounds, categoryLabels[category] + ": " + text));
                    var facts = new Dictionary<string, string> {
                        ["data-cfx-series"] = N(seriesIndex), ["data-cfx-series-key"] = series.InteractionIdentityKey,
                        ["data-cfx-point"] = N(source), ["data-cfx-category"] = N(categories[category].Value), ["data-cfx-angle"] = N(angle),
                        ["data-cfx-value"] = N(value), ["data-cfx-full-label"] = text, ["data-cfx-missing"] = source < 0 ? "true" : "false",
                        ["data-cfx-axis"] = series.ValueAxis.ToString().ToLowerInvariant(),
                        ["data-cfx-clipped"] = value < scale.Minimum || value > scale.Maximum ? "true" : "false"
                    };
                    VisualPolarSeriesScene.DescribePoint(facts, series, source, categoryLabels[category], text);
                    if (source < 0) {
                        facts["data-cfx-derived"] = "missing-category-zero"; facts["data-cfx-source-count"] = "0"; facts["data-cfx-source-points"] = string.Empty;
                    } else facts["data-cfx-source-point"] = N(source < series.SourcePointIndices.Count ? series.SourcePointIndices[source] : source);
                    if (geometry.Outer <= 0) facts["data-cfx-geometry-status"] = "layout-collapse";
                    using (builder.PushGroup(id, "radar-point-source", facts)) {
                        var pointColor = source >= 0 ? ChartSeriesColours.Point(series, seriesIndex, source, colors) : color;
                        var pattern = source >= 0 && source < series.PointFillPatterns.Count && series.PointFillPatterns[source].HasValue
                            ? series.PointFillPatterns[source]!.Value : series.FillPattern;
                        if (geometry.Outer > 0) {
                            VisualMarkerScene.Draw(builder, series, source, mapped[category].X, mapped[category].Y, marker, pointColor,
                                VisualChartPaint.Series(series, pointColor, source), "radar-point", colors.Surface, 1,
                                SvgPaint.Of(colors.Surface, SvgColorRole.Surface), pattern, ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "radar-point-pattern");
                            if (obstacles != null && VisualMarkerScene.Extent(series, marker) > 0) {
                                var contour = new VisualScenePath(ChartMarkerGeometry.Path(series.Markers, mapped[category].X, mapped[category].Y, marker),
                                    true, null, null, 0, null, null);
                                obstacles.Add(new LabelObstacle(id, new LabelMarkShape(VisualSceneGeometry.Flatten(contour, 8),
                                    true, VisualMarkerScene.StrokeWidth(series, true), plot)));
                            }
                        }
                    }
                    if (geometry.Outer > 0 && (series.ShowDataLabels ?? (chart.Options.ShowDataLabels ?? false)))
                        RadarDataLabel(chart, context, builder, plot, series, source, id, text, mapped[category], angle, labels);
                }
            }
        }
        if (geometry.Outer > 0 && obstacles != null) {
            var stroke = VisualPolarSeriesScene.StrokeExtent(chart, context, series, color);
            if (stroke > 0) foreach (var path in paths) {
                var contour = path.Path.Flatten();
                if (path.Closed && contour.Count > 0 && !contour[contour.Count - 1].Equals(contour[0])) contour.Add(contour[0]);
                obstacles.Add(new LabelObstacle(SeriesId(seriesIndex), new LabelMarkShape(new[] { contour }, false, stroke, plot)));
            }
        }
    }

    private static (ChartPoint[] Points, int[] Sources) RadarPoints(Chart chart, RadialSeriesGeometry geometry, int seriesIndex,
        ChartBarCoordinateMap coordinates, ChartBarCoordinateKey[] categories, RadialValueScale scale) {
        var series = chart.Series[seriesIndex];
        var raw = coordinates.SourcesFor(seriesIndex, categories);
        return (raw.Select((source, category) => On(geometry,
            CategoryAngle(chart.Options.XAxis, category, categories.Length, geometry.Start, geometry.Sweep, false),
            geometry.Inner + scale.Normalize(source >= 0 ? series.RenderPoints[source].Y : 0) * (geometry.Outer - geometry.Inner))).ToArray(), raw);
    }

    private static void RadarDataLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartSeries series,
        int source, string id, string text, ChartPoint point, double angle, List<RadialSeriesLabel> labels) {
        var style = series.DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = context.Theme.Resolve(context.ThemeMode).Foreground
        }));
        if (source >= 0 && source < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[source] != null)
            style = series.PointDataLabelStyles[source]!.Resolve(style);
        var gap = context.Theme.Spacing;
        var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        var candidate = placement switch {
            ChartDataLabelPlacement.Center or ChartDataLabelPlacement.Inside => new LabelCandidate(0, 0, .5, .5),
            ChartDataLabelPlacement.Above => new LabelCandidate(0, -gap, .5, 1),
            ChartDataLabelPlacement.Below => new LabelCandidate(0, gap, .5, 0),
            ChartDataLabelPlacement.Left => new LabelCandidate(-gap, 0, 1, .5),
            ChartDataLabelPlacement.Right or ChartDataLabelPlacement.Outside => new LabelCandidate(gap, 0, 0, .5),
            _ => new LabelCandidate(-Math.Cos(angle) * gap, -Math.Sin(angle) * gap, .5, .5)
        };
        var request = AddLabel(builder, labels, text, point, style, id + "-label", "radar-data-label", plot, candidate, 40,
            VisualMarkLabel.Metadata(text, source >= 0 ? id : null));
        request.AssociatedMarkId = id;
    }
}
