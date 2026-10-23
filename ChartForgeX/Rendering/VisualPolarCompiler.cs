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
            return !series.ShowInLegend ? Array.Empty<VisualLegendEntry>() : series.RenderPoints.Select((point, index) =>
                new VisualLegendEntry(Category(chart, point.X), ChartSeriesColours.CategoryPoint(series, index, colors), Id(0, index),
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
        if (kind == ChartSeriesKind.PolarArea && chart.Options.YAxis.Reversed)
            throw new NotSupportedException("Polar-area radius reversal is not supported.");
        if (chart.Series.SelectMany(series => series.RenderPoints).Any(point => !Finite(point.X) || !Finite(point.Y) || kind == ChartSeriesKind.PolarArea && point.Y < 0))
            throw new InvalidOperationException("Polar coordinates must be finite; area values must be non-negative.");
        if (!chart.Series.Any(series => series.RenderPoints.Count > 0)) { builder.AddDiagnostic(new VisualDiagnostic("polar.no-data", "The polar chart has no observations.")); return; }
        var colors = context.Theme.Resolve(context.ThemeMode);
        if (kind == ChartSeriesKind.PolarArea) { Area(chart, context, builder, plot, colors); return; }
        var radar = kind == ChartSeriesKind.Radar;
        var coordinates = radar ? ChartBarCoordinateMap.Create(chart) : null;
        var categoryKeys = coordinates?.Categories.ToArray();
        var categories = radar ? categoryKeys!.Select(key => key.Value).ToArray()
            : Enumerable.Range(0, Math.Max(4, Math.Min(12, chart.Options.XAxis.TickCount))).Select(index => Math.PI * 2 * index / Math.Max(4, Math.Min(12, chart.Options.XAxis.TickCount))).ToArray();
        if (radar && categories.Length < 3) { builder.AddDiagnostic(new VisualDiagnostic("radar.insufficient-categories", "Radar polygons need at least three distinct categories.")); return; }
        var scales = RadialValueScale.ForAxes(chart, index => chart.Series[index].RenderPoints.Select(point => point.Y), radar ? "Radar" : "Polar");
        var categoryCache = new Dictionary<double, string>();
        string CategoryText(double value) {
            if (!categoryCache.TryGetValue(value, out var text)) categoryCache.Add(value, text = Category(chart, value));
            return text;
        }
        var axes = categories.Select(CategoryText).ToArray();
        var geometry = Layout(chart, context, builder, plot, axes);
        if (radar) geometry = FitRadarCurves(chart, geometry, coordinates!, categoryKeys!, scales);
        if (geometry.Radius <= 0) { builder.AddDiagnostic(new VisualDiagnostic("polar.insufficient-space", "No radius remains after fitting polar axis labels.")); return; }
        var labels = new List<PolarLabel>();
        using (builder.PushClip(plot)) {
            Grid(chart, context, builder, plot, geometry, categories, axes, scales, radar, labels);
            foreach (var seriesIndex in ChartSeriesColours.DrawingOrder(chart)) {
                var series = chart.Series[seriesIndex];
                if (series.RenderPoints.Count == 0) continue;
                var scale = scales[series.ValueAxis];
                var color = ChartSeriesColours.Resolve(series, seriesIndex, colors);
                var raw = radar ? coordinates!.SourcesFor(seriesIndex, categoryKeys!) : Enumerable.Range(0, series.RenderPoints.Count).ToArray();
                var mapped = new ChartPoint[raw.Length];
                for (var index = 0; index < raw.Length; index++) {
                    var value = raw[index] >= 0 ? series.RenderPoints[raw[index]].Y : 0;
                    var angle = radar ? CategoryAngle(chart.Options.XAxis, index, categories.Length) : PolarAngle(chart.Options.XAxis, series.RenderPoints[raw[index]].X);
                    mapped[index] = On(geometry, angle, geometry.Radius * scale.Normalize(value));
                }
                var paths = radar ? ChartRadarGeometry.Paths(series.Radar, mapped, raw)
                    : new[] { new ChartRadarGeometry.RadarPath(ChartPathBuilder.FromPoints(mapped, ChartInterpolation.Linear), false) };
                if (radar) ChartRadarGeometry.DiagnoseOpenArea(builder, series, paths);
                using (builder.PushGroup(Id(seriesIndex), radar ? "radar-series" : "polar-series", new Dictionary<string, string> {
                    ["data-cfx-series"] = N(seriesIndex), ["data-cfx-series-key"] = series.InteractionIdentityKey, ["data-cfx-label"] = series.Name,
                    ["data-cfx-form"] = radar && series.Radar.Form == ChartLineAreaForm.Area ? "area" : "line",
                    ["data-cfx-missing-policy"] = radar ? ChartRadarGeometry.MissingPolicy(series.Radar) : "source"
                })) {
                    foreach (var path in paths) VisualPolarSeriesScene.Draw(chart, context, builder, series, path.Path, color, radar, path.Closed);
                    for (var index = 0; index < mapped.Length; index++) {
                        var source = raw[index]; var value = source >= 0 ? series.RenderPoints[source].Y : 0;
                        if (radar && source < 0 && series.Radar.MissingCategoryPolicy != ChartRadarMissingCategoryPolicy.Zero) continue;
                        var angle = radar ? CategoryAngle(chart.Options.XAxis, index, categories.Length) : PolarAngle(chart.Options.XAxis, series.RenderPoints[source].X);
                        var category = radar ? categories[index] : series.RenderPoints[source].X;
                        var formatted = source >= 0 && source < series.PointLabels.Count && series.PointLabels[source] != null
                            ? series.PointLabels[source]! : ChartNumericFormatter.FormatValue(chart.Options, value);
                        var pointId = source >= 0 ? Id(seriesIndex, source) : Id(seriesIndex) + "-missing-category-" + index;
                        var marker = VisualMarkerScene.Radius(series, context);
                        var bounds = VisualMarkerScene.Bounds(series, mapped[index].X, mapped[index].Y, marker);
                        builder.AddRegion(new VisualSemanticRegion(pointId, radar ? "radar-point" : "polar-point", bounds, CategoryText(category) + ": " + formatted));
                        var pointMetadata = new Dictionary<string, string> {
                            ["data-cfx-series"] = N(seriesIndex),
                            ["data-cfx-point"] = N(source), ["data-cfx-category"] = N(category), ["data-cfx-angle"] = N(radar ? angle : series.RenderPoints[source].X),
                            ["data-cfx-value"] = N(value), ["data-cfx-full-label"] = formatted, ["data-cfx-missing"] = source < 0 ? "true" : "false"
                        };
                        VisualPolarSeriesScene.DescribePoint(pointMetadata, series, source, CategoryText(category), formatted);
                        if (series.ValueAxis == ChartAxisSide.Secondary) pointMetadata["data-cfx-axis"] = "secondary";
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
                        if (series.ShowDataLabels ?? (chart.Options.ShowDataLabels ?? false))
                            AddDataLabel(chart, context, builder, plot, labels, series, source, pointId, formatted, mapped[index], angle, radar, seriesIndex, chart.Series.Count);
                    }
                }
            }
            DrawLabels(builder, plot, labels, context.Theme.Spacing);
        }
    }

    private static string Category(Chart chart, double value) => ChartAxisValueFormatter.FindExplicitLabel(chart.Options.XAxisLabels, value)
        ?? ChartAxisValueFormatter.Format(chart.Options.XAxis, value);
    private static ChartFillPattern Pattern(ChartSeries series, int index) => index >= 0 && index < series.PointFillPatterns.Count && series.PointFillPatterns[index].HasValue ? series.PointFillPatterns[index]!.Value : series.FillPattern;
    private static ChartPoint On(PolarLayout geometry, double angle, double radius) => new(geometry.Cx + Math.Cos(angle) * radius, geometry.Cy + Math.Sin(angle) * radius);
    /// <summary>Places an ordinal radar vertex or polar-area sector start without reordering source observations.</summary>
    private static double CategoryAngle(ChartAxis axis, int index, int count, double? sectorSweep = null) {
        var physical = axis.Reversed ? count - 1 - index : index;
        // Keep each family's existing arithmetic so unreversed prepared coordinates stay byte-for-byte stable.
        return -Math.PI / 2 + (sectorSweep.HasValue ? physical * sectorSweep.Value : Math.PI * 2 * physical / count);
    }
    /// <summary>Projects authored radians into screen coordinates, retaining the positive horizontal zero ray.</summary>
    private static double PolarAngle(ChartAxis axis, double radians) => axis.Reversed ? radians : -radians;
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static string Id(int series, int? point = null) => "series-" + series.ToString(CultureInfo.InvariantCulture) + (point.HasValue ? "-point-" + point.Value.ToString(CultureInfo.InvariantCulture) : "");
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
