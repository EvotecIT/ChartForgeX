using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawRangeBars(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles,
        IReadOnlyDictionary<double, string> categories) {
        var series = chart.Series[index]; var count = series.Points.Count / 2;
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart, series);
        var style = chart.Options.ResolvePreparedBarVisualStyle();
        var participants = Enumerable.Range(0, chart.Series.Count).Where(i => chart.Series[i].Kind == ChartSeriesKind.RangeBar).ToArray();
        var segmented = style.Kind == ChartBarStyle.SegmentedCapsule;
        var padding = participants.Max(i => chart.Series[i].HasExplicitStrokeWidth ? chart.Series[i].StrokeWidth : ChartVisualPrimitives.RangeBarCapStrokeWidth);
        var layout = ChartCategorySlotLayout.Create(participants.SelectMany(i => chart.Series[i].Points.Select(point => horizontal ? map.Y(point.X) : map.X(point.X))),
            horizontal ? plot.Height : plot.Width, context.Theme.Spacing, participants.Length, Array.IndexOf(participants, index),
            maximumThickness: 28, footprintRatio: 1.5, footprintPadding: padding,
            measuredFootprint: segmented ? width => ChartSegmentedBarGeometry.RangeCategoryFootprint(style, width) : null);
        var thickness = layout.Thickness;
        for (var item = 0; item < count; item++) {
            var first = series.Points[item * 2]; var second = series.Points[item * 2 + 1];
            var category = (horizontal ? map.Y(first.X) : map.X(first.X)) + layout.Offset;
            var start = horizontal ? map.X(first.Y) : map.Y(first.Y);
            var end = horizontal ? map.X(second.Y) : map.Y(second.Y);
            var minimum = Math.Min(start, end); var length = Math.Max(2, Math.Abs(start - end));
            var bounds = horizontal ? new ChartRect(minimum, category - thickness / 2, length, thickness)
                : new ChartRect(category - thickness / 2, minimum, thickness, length);
            var color = PointColor(series, index, item, colors);
            var label = ResolveObservationLabel(chart, context, series, item, colors,
                () => Value(chart, Math.Min(first.Y, second.Y)) + "–" + Value(chart, Math.Max(first.Y, second.Y)));
            using (ObservationGroup(builder, series, index, item, item * 2, 2, bounds, label, RangeBarMetadata(chart, categories[first.X]),
                ("x", first.X), ("start", first.Y), ("end", second.Y))) {
                DrawBarSurface(chart, context, builder, series, item, bounds, color, colors, "range-bar", range: true, horizontal: horizontal);
                DrawCap(start); DrawCap(end);
            }
            var footprint = thickness * 1.5 + padding;
            var obstacle = segmented ? ChartSegmentedBarGeometry.RangePaintBounds(style, bounds, horizontal)
                : horizontal ? new ChartRect(minimum - padding / 2, category - footprint / 2, length + padding, footprint)
                    : new ChartRect(category - footprint / 2, minimum - padding / 2, footprint, length + padding);
            obstacles.Add(new LabelObstacle(PointId(index, item), obstacle));
            var high = horizontal ? map.X(Math.Max(first.Y, second.Y)) : map.Y(Math.Max(first.Y, second.Y));
            var low = horizontal ? map.X(Math.Min(first.Y, second.Y)) : map.Y(Math.Min(first.Y, second.Y));
            var direction = horizontal ? high >= low ? 1 : -1 : high <= low ? 1 : -1;
            if (horizontal) AddHorizontalLabel(chart, context, series, index, item, new ChartPoint(high, category), bounds,
                Math.Max(first.Y, second.Y), direction, label, labels);
            else AddLabel(chart, context, series, index, item, new ChartPoint(category, high), bounds, label, labels,
                Math.Max(first.Y, second.Y), barDirection: direction);

            void DrawCap(double value) {
                if (style.Kind == ChartBarStyle.SegmentedCapsule) {
                    var geometry = ChartSegmentedBarGeometry.RangeCap(style, horizontal ? value : category,
                        horizontal ? category : value, thickness, horizontal);
                    DrawSegmentedCap(builder, geometry, style, color, "range-bar", VisualChartPaint.Series(series, color, item));
                } else {
                    var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.RangeBarCapStrokeWidth;
                    builder.Line(horizontal ? value : category - thickness * .75, horizontal ? category - thickness * .75 : value,
                        horizontal ? value : category + thickness * .75, horizontal ? category + thickness * .75 : value,
                        color, stroke, role: "range-bar-cap", paint: VisualChartPaint.Stroke(VisualChartPaint.Series(series, color, item)));
                }
            }
        }
    }

    private static Dictionary<string, string> RangeBarMetadata(Chart chart, string category) => new() {
        ["data-cfx-kind"] = "RangeBar", ["data-cfx-category-label"] = category,
        ["data-cfx-orientation"] = chart.Options.RangeBar.Orientation == ChartOrientation.Horizontal ? "horizontal" : "vertical",
        ["data-cfx-label-category"] = chart.Options.Labels.Category,
        ["data-cfx-label-start"] = chart.Options.Labels.Start, ["data-cfx-label-end"] = chart.Options.Labels.End
    };

    private static IReadOnlyList<VisualLegendEntry> RangeBarLegendEntries(Chart chart, ChartSeries series, VisualThemeColors colors, ChartAxisValueFormatter.Cache cache) {
        var entries = new List<VisualLegendEntry>();
        var categories = ChartCartesianCategoryLabels.Resolve(chart, series.Points.Select(point => point.X), cache);
        for (var item = 0; item < series.AuthoredObservationCount; item++) {
            var category = categories[series.Points[item * 2].X]; var pattern = ObservationPattern(series, item);
            var color = PointColor(series, 0, item, colors); var paint = VisualChartPaint.Series(series, color, item);
            entries.Add(new VisualLegendEntry(category, color, PointId(0, item), series.Kind, pattern, series.StateRole, series.InteractionIdentityKey,
                marker: VisualMarkerScene.Legend(chart, series, color, paint, item, pattern), paint: paint, metadata: RangeBarMetadata(chart, category)));
        }
        return entries;
    }
}
