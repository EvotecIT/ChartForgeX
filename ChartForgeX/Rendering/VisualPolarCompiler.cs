using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Compiles angle/radius lines, category radar polygons and equal-angle area sectors.</summary>
internal static partial class VisualPolarCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        if (chart.Series.Count == 1 && chart.Series[0].Kind == ChartSeriesKind.PolarArea) {
            var series = chart.Series[0];
            return !series.ShowInLegend ? Array.Empty<VisualLegendEntry>() : series.Points.Select((point, index) =>
                new VisualLegendEntry(Category(chart, point.X), ChartSeriesColours.Point(series, index, index, colors), Id(0, index),
                    series.Kind, Pattern(series, index), series.StateRole, series.InteractionIdentityKey)).ToArray();
        }
        return chart.Series.Select((series, index) => new { series, index }).Where(item => item.series.ShowInLegend).Select(item => {
            var color = ChartSeriesColours.Resolve(item.series, item.index, colors); var paint = VisualChartPaint.Series(item.series, color);
            return new VisualLegendEntry(item.series.Name, color, Id(item.index), item.series.Kind,
                item.series.FillPattern, item.series.StateRole, item.series.InteractionIdentityKey,
                marker: VisualMarkerScene.Legend(chart, item.series, color, paint, pattern: item.series.FillPattern), paint: paint);
        }).ToArray();
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        var kind = chart.Series[0].Kind;
        if (chart.Series.Any(series => series.Kind != kind) || kind == ChartSeriesKind.PolarArea && chart.Series.Count != 1)
            throw new InvalidOperationException("Prepared polar charts require one family; polar area requires one series.");
        if (chart.Series.SelectMany(series => series.Points).Any(point => !Finite(point.X) || !Finite(point.Y) || kind == ChartSeriesKind.PolarArea && point.Y < 0))
            throw new InvalidOperationException("Polar coordinates must be finite; area values must be non-negative.");
        if (!chart.Series.Any(series => series.Points.Count > 0)) { builder.AddDiagnostic(new VisualDiagnostic("polar.no-data", "The polar chart has no observations.")); return; }
        var colors = context.Theme.Resolve(context.ThemeMode);
        if (kind == ChartSeriesKind.PolarArea) { Area(chart, context, builder, plot, colors); return; }
        var radar = kind == ChartSeriesKind.Radar;
        var categories = radar ? chart.Series.SelectMany(series => series.Points).Select(point => point.X).Distinct().OrderBy(value => value).ToArray()
            : Enumerable.Range(0, Math.Max(4, Math.Min(12, chart.Options.XAxis.TickCount))).Select(index => Math.PI * 2 * index / Math.Max(4, Math.Min(12, chart.Options.XAxis.TickCount))).ToArray();
        if (radar && categories.Length < 3) { builder.AddDiagnostic(new VisualDiagnostic("radar.insufficient-categories", "Radar polygons need at least three distinct categories.")); return; }
        var scale = RadialValueScale.Create(chart.Options.YAxis, chart.Series, radar ? "Radar" : "Polar");
        var categoryCache = new Dictionary<double, string>();
        string CategoryText(double value) {
            if (!categoryCache.TryGetValue(value, out var text)) categoryCache.Add(value, text = Category(chart, value));
            return text;
        }
        var axes = categories.Select(CategoryText).ToArray();
        var geometry = Layout(chart, context, builder, plot, axes);
        if (geometry.Radius <= 0) { builder.AddDiagnostic(new VisualDiagnostic("polar.insufficient-space", "No radius remains after fitting polar axis labels.")); return; }
        var labels = new List<PolarLabel>();
        using (builder.PushClip(plot)) {
            Grid(chart, context, builder, plot, geometry, categories, axes, scale, radar, labels);
            foreach (var seriesIndex in ChartSeriesColours.DrawingOrder(chart)) {
                var series = chart.Series[seriesIndex];
                if (series.Points.Count == 0) continue;
                var color = ChartSeriesColours.Resolve(series, seriesIndex, colors);
                var raw = radar ? categories.Select(value => Find(series, value)).ToArray() : Enumerable.Range(0, series.Points.Count).ToArray();
                var mapped = new ChartPoint[raw.Length];
                for (var index = 0; index < raw.Length; index++) {
                    var value = raw[index] >= 0 ? series.Points[raw[index]].Y : 0;
                    var angle = radar ? RadarAngle(index, categories.Length) : -series.Points[raw[index]].X;
                    mapped[index] = On(geometry, angle, geometry.Radius * scale.Normalize(value));
                }
                var path = ChartPathBuilder.FromPoints(mapped, ChartInterpolation.Linear);
                using (builder.PushGroup(Id(seriesIndex), radar ? "radar-series" : "polar-series", new Dictionary<string, string> {
                    ["data-cfx-series"] = N(seriesIndex), ["data-cfx-series-key"] = series.InteractionIdentityKey, ["data-cfx-label"] = series.Name,
                    ["data-cfx-form"] = radar && series.Radar.Form == ChartLineAreaForm.Area ? "area" : "line",
                    ["data-cfx-missing-policy"] = radar ? "zero" : "source"
                })) {
                    if (radar && series.Radar.Form == ChartLineAreaForm.Area) {
                        var opacity = series.Radar.FillOpacity ?? context.Theme.AreaOpacity;
                        var fill = ChartColorMath.WithOpacity(color, opacity);
                        builder.Path(path, fill, role: "radar-area", close: true,
                            paint: VisualChartPaint.Fill(VisualChartPaint.Series(series, color).WithOpacity(fill, opacity)));
                        if (series.FillPattern != ChartFillPattern.None && opacity > 0) {
                            var hatchOpacity = Math.Min(1, opacity / Math.Max(.000001, context.Theme.AreaOpacity));
                            builder.Pattern(path, series.FillPattern, ChartColorMath.WithOpacity(ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(80), hatchOpacity), role: "radar-pattern");
                        }
                    }
                    var width = series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth;
                    foreach (var layer in ChartLineVisualLayers.Build(color, width, chart.Options.LineVisualStyle))
                        if (layer.IsVisible) builder.Path(path, stroke: layer.ColorWithOpacity(), strokeWidth: layer.StrokeWidth, role: (radar ? "radar-outline" : "polar-line") + layer.RoleSuffix, close: radar,
                            paint: VisualChartPaint.Stroke(VisualChartPaint.LineLayer(series, color, layer)));
                    for (var index = 0; index < mapped.Length; index++) {
                        var source = raw[index]; var value = source >= 0 ? series.Points[source].Y : 0;
                        var angle = radar ? RadarAngle(index, categories.Length) : -series.Points[source].X;
                        var category = radar ? categories[index] : series.Points[source].X;
                        var formatted = source >= 0 && source < series.PointLabels.Count && series.PointLabels[source] != null
                            ? series.PointLabels[source]! : ChartNumericFormatter.FormatValue(chart.Options, value);
                        var pointId = source >= 0 ? Id(seriesIndex, source) : Id(seriesIndex) + "-missing-category-" + index;
                        var marker = VisualMarkerScene.Radius(series, context);
                        var bounds = VisualMarkerScene.Bounds(mapped[index].X, mapped[index].Y, Math.Max(marker, VisualMarkerScene.Extent(series, marker)));
                        builder.AddRegion(new VisualSemanticRegion(pointId, radar ? "radar-point" : "polar-point", bounds, CategoryText(category) + ": " + formatted));
                        var pointMetadata = new Dictionary<string, string> {
                            ["data-cfx-series"] = N(seriesIndex),
                            ["data-cfx-point"] = N(source), ["data-cfx-category"] = N(category), ["data-cfx-angle"] = N(radar ? angle : -angle),
                            ["data-cfx-value"] = N(value), ["data-cfx-full-label"] = formatted, ["data-cfx-missing"] = source < 0 ? "true" : "false"
                        };
                        if (source < 0) {
                            pointMetadata["data-cfx-derived"] = "missing-category-zero";
                            pointMetadata["data-cfx-source-count"] = "0";
                            pointMetadata["data-cfx-source-points"] = string.Empty;
                        }
                        using (builder.PushGroup(pointId, radar ? "radar-point-source" : "polar-point-source", pointMetadata)) {
                            var pointColor = source >= 0 ? ChartSeriesColours.Point(series, seriesIndex, source, colors) : color;
                            VisualMarkerScene.Draw(builder, series, source, mapped[index].X, mapped[index].Y, marker, pointColor,
                                VisualChartPaint.Series(series, pointColor, source), radar ? "radar-point" : "polar-point", colors.Surface, 1,
                                SvgPaint.Of(colors.Surface, SvgColorRole.Surface), Pattern(series, source),
                                ChartStateMark.Backdrop(chart.Options, colors, context.Frame), radar ? "radar-point-pattern" : "polar-point-pattern");
                        }
                        if (series.ShowDataLabels ?? chart.Options.ShowDataLabels)
                            AddDataLabel(chart, context, builder, plot, labels, series, source, pointId, formatted, mapped[index], angle, radar, seriesIndex, chart.Series.Count);
                    }
                }
            }
            DrawLabels(builder, plot, labels, context.Theme.Spacing);
        }
    }

    private static string Category(Chart chart, double value) => ChartAxisValueFormatter.FindExplicitLabel(chart.Options.XAxisLabels, value)
        ?? ChartAxisValueFormatter.Format(chart.Options.XAxis, value);
    private static int Find(ChartSeries series, double category) {
        for (var index = 0; index < series.Points.Count; index++) if (ChartMath.SameCoordinate(series.Points[index].X, category)) return index;
        return -1;
    }
    private static ChartFillPattern Pattern(ChartSeries series, int index) => index >= 0 && index < series.PointFillPatterns.Count && series.PointFillPatterns[index].HasValue ? series.PointFillPatterns[index]!.Value : series.FillPattern;
    private static ChartPoint On(PolarLayout geometry, double angle, double radius) => new(geometry.Cx + Math.Cos(angle) * radius, geometry.Cy + Math.Sin(angle) * radius);
    private static double RadarAngle(int index, int count) => -Math.PI / 2 + Math.PI * 2 * index / count;
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static string Id(int series, int? point = null) => "series-" + series.ToString(CultureInfo.InvariantCulture) + (point.HasValue ? "-point-" + point.Value.ToString(CultureInfo.InvariantCulture) : "");
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
