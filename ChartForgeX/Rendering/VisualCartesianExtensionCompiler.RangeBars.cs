using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private const double RangeBarMinimumLength = 2;

    private static void DrawRangeBars(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles,
        IReadOnlyDictionary<double, string> categories) {
        var series = chart.Series[index]; var count = series.RenderPoints.Count / 2;
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart, series);
        var style = chart.Options.ResolvePreparedBarVisualStyle();
        var segmented = style.Kind == ChartBarStyle.SegmentedCapsule;
        var layout = ResolveRangeBarLayout(chart, context, plot, map, index, out var padding);
        var thickness = layout.Thickness;
        var endpoints = chart.Options.RangeBar.LabelContent == ChartRangeBarLabelContent.Endpoints;
        var endpointFormatter = chart.Options.RangeBar.EndpointLabelFormatter;
        for (var item = 0; item < count; item++) {
            var first = series.RenderPoints[item * 2]; var second = series.RenderPoints[item * 2 + 1];
            var category = (horizontal ? map.Y(first.X) : map.X(first.X)) + layout.Offset;
            var start = horizontal ? map.X(first.Y) : map.Y(first.Y);
            var end = horizontal ? map.X(second.Y) : map.Y(second.Y);
            var (minimum, length) = MinimumBodySpan(start, end, RangeBarMinimumLength);
            var bounds = horizontal ? new ChartRect(minimum, category - thickness / 2, length, thickness)
                : new ChartRect(category - thickness / 2, minimum, thickness, length);
            var color = PointColor(series, index, item, colors);
            var captions = ResolveRangeBarLabels(chart, context, series, item, colors, categories[first.X],
                new ChartInterval(first.X, first.Y, second.Y), endpoints, endpointFormatter);
            var label = captions.Observation;
            var metadata = RangeBarMetadata(chart, categories[first.X]);
            if (captions.Start != null) metadata["data-cfx-start-label"] = captions.Start;
            if (captions.End != null) metadata["data-cfx-end-label"] = captions.End;
            if (captions.NumericStart != null) {
                metadata["data-cfx-formatted-start"] = captions.NumericStart;
                metadata["data-cfx-formatted-y"] = captions.NumericStart;
            }
            if (captions.NumericEnd != null) metadata["data-cfx-formatted-end"] = captions.NumericEnd;
            using (ObservationGroup(builder, series, index, item, item * 2, 2, bounds, label, metadata,
                ("x", first.X), ("start", first.Y), ("end", second.Y))) {
                DrawBarSurface(chart, context, builder, series, item, bounds, color, colors, "range-bar", range: true, horizontal: horizontal);
                DrawCap(start); DrawCap(end);
            }
            var footprint = thickness * 1.5 + padding;
            var obstacle = segmented ? ChartSegmentedBarGeometry.RangePaintBounds(style, bounds, horizontal)
                : horizontal ? new ChartRect(minimum - padding / 2, category - footprint / 2, length + padding, footprint)
                    : new ChartRect(category - footprint / 2, minimum - padding / 2, footprint, length + padding);
            obstacles.Add(new LabelObstacle(PointId(index, item), obstacle));
            if (endpoints) {
                AddRangeBarEndpointLabels(chart, context, series, index, item, bounds, category, start, end,
                    horizontal, endpointFormatter != null, label, captions.Start, captions.End, labels);
                continue;
            }
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

    // Rendering and automatic value gutters measure the same grouped category thickness.
    private static ChartCategorySlotLayout ResolveRangeBarLayout(Chart chart, VisualRenderContext context, ChartRect plot,
        ChartMapper map, int index, out double padding) {
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart, chart.Series[index]);
        var style = chart.Options.ResolvePreparedBarVisualStyle();
        var participants = Enumerable.Range(0, chart.Series.Count).Where(i => chart.Series[i].Kind == ChartSeriesKind.RangeBar).ToArray();
        padding = participants.Max(i => chart.Series[i].HasExplicitStrokeWidth ? chart.Series[i].StrokeWidth : ChartVisualPrimitives.RangeBarCapStrokeWidth);
        return ChartCategorySlotLayout.Create(participants.SelectMany(i => chart.Series[i].RenderPoints.Select(point => horizontal ? map.Y(point.X) : map.X(point.X))),
            horizontal ? plot.Height : plot.Width, context.Theme.Spacing, participants.Length, Array.IndexOf(participants, index), requireMinimumFootprint: true,
            maximumThickness: 28, footprintRatio: 1.5, footprintPadding: padding,
            measuredFootprint: style.Kind == ChartBarStyle.SegmentedCapsule ? width => ChartSegmentedBarGeometry.RangeCategoryFootprint(style, width) : null);
    }

    private static double RangeBarValuePaintRadius(Chart chart, VisualRenderContext context, ChartRect plot, ChartMapper map, int index) {
        var series = chart.Series[index]; var style = chart.Options.ResolvePreparedBarVisualStyle();
        var layout = ResolveRangeBarLayout(chart, context, plot, map, index, out _);
        if (style.Kind != ChartBarStyle.SegmentedCapsule)
            return Math.Max(RangeBarMinimumLength, (series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.RangeBarCapStrokeWidth) / 2);
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart, series);
        var body = horizontal ? new ChartRect(0, 0, 0, layout.Thickness) : new ChartRect(0, 0, layout.Thickness, 0);
        var paint = ChartSegmentedBarGeometry.RangePaintBounds(style, body, horizontal);
        // Zero/tiny intervals retain a two-pixel body even when their cap effects are narrower.
        return Math.Max(RangeBarMinimumLength, horizontal ? Math.Max(-paint.Left, paint.Right) : Math.Max(-paint.Top, paint.Bottom));
    }

    private static Dictionary<string, string> RangeBarMetadata(Chart chart, string category) => new() {
        ["data-cfx-kind"] = "RangeBar", ["data-cfx-category-label"] = category,
        ["data-cfx-orientation"] = chart.Options.RangeBar.Orientation == ChartOrientation.Horizontal ? "horizontal" : "vertical",
        ["data-cfx-label-category"] = chart.Options.Labels.Category,
        ["data-cfx-label-start"] = chart.Options.Labels.Start, ["data-cfx-label-end"] = chart.Options.Labels.End
    };

    private static IReadOnlyList<VisualLegendEntry> RangeBarLegendEntries(Chart chart, ChartSeries series, VisualThemeColors colors, ChartAxisValueFormatter.Cache cache) {
        var entries = new List<VisualLegendEntry>();
        var categories = ChartCartesianCategoryLabels.Resolve(chart, series.RenderPoints.Select(point => point.X), cache);
        for (var item = 0; item < series.AuthoredObservationCount; item++) {
            var category = categories[series.RenderPoints[item * 2].X]; var pattern = ObservationPattern(series, item);
            var color = PointColor(series, 0, item, colors); var paint = VisualChartPaint.Series(series, color, item);
            entries.Add(new VisualLegendEntry(category, color, PointId(0, item), series.Kind, pattern, series.StateRole, series.InteractionIdentityKey,
                marker: VisualMarkerScene.Legend(chart, series, color, paint, item, pattern), paint: paint, metadata: RangeBarMetadata(chart, category)));
        }
        return entries;
    }
}
