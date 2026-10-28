using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private const double BoxPlotMinimumLength = 2;

    /// <summary>Contains line strokes and the minimum-width quartile body extending beyond a collapsed value.</summary>
    private static double BoxPlotValuePaintRadius(ChartSeries series) {
        var bodyStroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.BoxPlotStrokeWidth;
        var medianStroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.BoxPlotMedianStrokeWidth;
        var result = Math.Max(Math.Max(bodyStroke, medianStroke) / 2, BoxPlotMinimumLength + bodyStroke / 2);
        return series.BoxPlotDistributions.Any(value => value.OutlierSourceIndices.Count > 0)
            ? Math.Max(result, VisualMarkerScene.Extent(series, 4.5)) : result;
    }

    private static void DrawBoxPlots(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles,
        IReadOnlyDictionary<double, string> categories) {
        var series = chart.Series[index]; var horizontal = ChartCartesianOrientation.IsHorizontal(chart, series);
        var labelOptions = chart.Options.BoxPlot.Clone();
        var statisticLabels = labelOptions.LabelContent == ChartBoxPlotLabelContent.Statistics;
        var participants = Enumerable.Range(0, chart.Series.Count).Where(i => chart.Series[i].Kind == ChartSeriesKind.BoxPlot && (!chart.Series[i].IsVisibilityPreparation || chart.Series[i].HasSourceData)).ToArray();
        var padding = participants.Max(i => chart.Series[i].HasExplicitStrokeWidth ? chart.Series[i].StrokeWidth
            : Math.Max(ChartVisualPrimitives.BoxPlotStrokeWidth, ChartVisualPrimitives.BoxPlotMedianStrokeWidth));
        var layout = ChartCategorySlotLayout.Create(participants.SelectMany(i => chart.Series[i].RenderPoints.Select(point => horizontal ? map.Y(point.X) : map.X(point.X))),
            horizontal ? plot.Height : plot.Width, context.Theme.Spacing, participants.Length, Array.IndexOf(participants, index), requireMinimumFootprint: true,
            maximumThickness: 46, footprintPadding: padding, maximumPitch: map.CalendarBandPitch(horizontal));
        for (var item = 0; item < series.AuthoredObservationCount; item++) {
            var raw = item * 5; var x = series.RenderPoints[raw].X;
            var minimum = series.RenderPoints[raw].Y; var q1 = series.RenderPoints[raw + 1].Y; var median = series.RenderPoints[raw + 2].Y;
            var q3 = series.RenderPoints[raw + 3].Y; var maximum = series.RenderPoints[raw + 4].Y;
            var category = (horizontal ? map.Y(x) : map.X(x)) + layout.Offset;
            ChartPoint Project(double value) => horizontal ? new ChartPoint(map.X(value), category) : new ChartPoint(category, map.Y(value));
            var first = Project(q1); var third = Project(q3); var min = Project(minimum); var max = Project(maximum);
            var thickness = layout.Thickness;
            var span = MinimumBodySpan(horizontal ? first.X : first.Y, horizontal ? third.X : third.Y, BoxPlotMinimumLength);
            var box = horizontal ? new ChartRect(span.Minimum, category - thickness / 2, span.Length, thickness)
                : new ChartRect(category - thickness / 2, span.Minimum, thickness, span.Length);
            var bounds = horizontal ? Extents(Math.Min(Math.Min(min.X, max.X), box.Left), box.Top, Math.Max(Math.Max(min.X, max.X), box.Right), box.Bottom)
                : Extents(box.Left, Math.Min(Math.Min(min.Y, max.Y), box.Top), box.Right, Math.Max(Math.Max(min.Y, max.Y), box.Bottom));
            var color = PointColor(series, index, item, colors); var sourcePaint = VisualChartPaint.Series(series, color, item);
            var summary = new ChartBoxPlot(x, minimum, q1, median, q3, maximum);
            var captions = statisticLabels ? ResolveBoxPlotLabels(chart, context, series, item, colors, categories[x], summary, labelOptions) : default;
            var label = statisticLabels ? captions.Observation : ResolveObservationLabel(chart, context, series, item, colors, format => format("median", median));
            var facts = BoxPlotMetadata(chart, series, item, categories[x]);
            if (statisticLabels) for (var member = 0; member < captions.Captions.Length; member++) {
                var name = BoxPlotStatisticNames[member];
                if (captions.Captions[member] != null) facts["data-cfx-" + name + "-label"] = captions.Captions[member]!;
                facts["data-cfx-formatted-" + name] = captions.Numeric[member];
                if (member == 0) facts["data-cfx-formatted-y"] = captions.Numeric[member];
            }
            using (ObservationGroup(builder, series, index, item, raw, 5, bounds, label, facts,
                ("x", x), ("minimum", minimum), ("q1", q1), ("median", median), ("q3", q3), ("maximum", maximum))) {
                var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.BoxPlotStrokeWidth;
                var whisker = ChartColorMath.WithOpacity(color, ChartVisualPrimitives.BoxPlotWhiskerOpacity);
                builder.Line(min.X, min.Y, max.X, max.Y, whisker, stroke, role: "boxplot-whisker",
                    paint: VisualChartPaint.Stroke(sourcePaint.WithOpacity(whisker, ChartVisualPrimitives.BoxPlotWhiskerOpacity)));
                CrossLine(minimum, thickness * .74, stroke, "boxplot-cap");
                CrossLine(maximum, thickness * .74, stroke, "boxplot-cap");
                var fill = ChartColorMath.WithOpacity(color, ChartVisualPrimitives.BoxPlotBodyFillOpacity);
                var radius = Math.Min(ChartVisualPrimitives.BoxPlotBodyRadius, Math.Min(box.Width, box.Height) / 2);
                builder.Rect(box, fill, color, stroke, radius, role: "boxplot-body",
                    paint: new VisualScenePaintBinding(fill: sourcePaint.WithOpacity(fill, ChartVisualPrimitives.BoxPlotBodyFillOpacity), stroke: sourcePaint));
                DrawPattern(builder, RoundedRectanglePath(box, radius), ObservationPattern(series, item), fill,
                    ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "boxplot-pattern");
                CrossLine(median, thickness, series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.BoxPlotMedianStrokeWidth, "boxplot-median");

                void CrossLine(double value, double span, double width, string role) {
                    var center = Project(value);
                    builder.Line(horizontal ? center.X : center.X - span / 2, horizontal ? center.Y - span / 2 : center.Y,
                        horizontal ? center.X : center.X + span / 2, horizontal ? center.Y + span / 2 : center.Y,
                        color, width, role: role, paint: VisualChartPaint.Stroke(sourcePaint));
                }
            }
            obstacles.Add(new LabelObstacle(PointId(index, item), new ChartRect(bounds.Left - padding / 2, bounds.Top - padding / 2,
                bounds.Width + padding, bounds.Height + padding)));
            if (statisticLabels) AddBoxPlotStatisticLabels(chart, context, series, index, item, bounds, horizontal, Project, summary,
                label, captions.Captions, labelOptions, labels);
            else if (horizontal) AddHorizontalLabel(chart, context, series, index, item, Project(median), bounds, median,
                max.X >= min.X ? 1 : -1, label, labels);
            else AddLabel(chart, context, series, index, item, Project(median), bounds, label, labels, median);
            DrawBoxPlotOutliers(chart, builder, series, index, item, categories[x], Project, color, sourcePaint, obstacles);
        }
    }

    private static Dictionary<string, string> BoxPlotMetadata(Chart chart, ChartSeries series, int item, string category) {
        var metadata = new Dictionary<string, string> {
        ["data-cfx-kind"] = "BoxPlot", ["data-cfx-category-label"] = category,
        ["data-cfx-orientation"] = chart.Options.BoxPlot.Orientation == ChartOrientation.Horizontal ? "horizontal" : "vertical",
        ["data-cfx-label-category"] = chart.Options.Labels.Category, ["data-cfx-label-minimum"] = chart.Options.Labels.Minimum,
        ["data-cfx-label-q1"] = chart.Options.Labels.Q1, ["data-cfx-label-median"] = chart.Options.Labels.Median,
        ["data-cfx-label-q3"] = chart.Options.Labels.Q3, ["data-cfx-label-maximum"] = chart.Options.Labels.Maximum
        };
        if (item < series.BoxPlotDistributions.Count) {
            AddBoxPlotDistributionMetadata(chart, metadata, series.BoxPlotDistributions[item]);
            if (series.BoxPlotGroupIds.TryGetValue(series.IsVisibilityPreparation ? series.BoxPlotDistributions[item].Index : item, out var id)) metadata["data-cfx-group-id"] = id.Value;
        }
        return metadata;
    }

    private static IReadOnlyList<VisualLegendEntry> BoxPlotLegendEntries(Chart chart, ChartSeries series, VisualThemeColors colors, ChartAxisValueFormatter.Cache cache) {
        var entries = new List<VisualLegendEntry>();
        var categories = ChartCartesianCategoryLabels.Resolve(chart, series.RenderPoints.Select(point => point.X), cache);
        for (var item = 0; item < series.AuthoredObservationCount; item++) {
            var category = categories[series.RenderPoints[item * 5].X]; var pattern = ObservationPattern(series, item);
            var color = PointColor(series, 0, item, colors); var paint = VisualChartPaint.Series(series, color, item);
            var metadata = BoxPlotMetadata(chart, series, item, category);
            if (series.IsVisibilityPreparation) {
                if (series.BoxPlotQuartileEstimator.HasValue) {
                    var original = series.BoxPlotDistributions[item].Index;
                    if (series.BoxPlotGroupIds.TryGetValue(original, out var id)) metadata["data-cfx-group-id"] = id.Value;
                } else foreach (var pair in VisibilityLegendObservationFacts(series, item)) metadata[pair.Key] = pair.Value;
            }
            entries.Add(new VisualLegendEntry(category, color, PointId(0, item), series.Kind, pattern, series.StateRole, series.InteractionIdentityKey,
                marker: VisualMarkerScene.Legend(chart, series, color, paint, item, pattern), paint: paint, metadata: metadata));
        }
        return entries;
    }
}
