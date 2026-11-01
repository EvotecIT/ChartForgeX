using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawPreparedWaterfall(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles,
        IReadOnlyDictionary<double, string> categories) {
        var series = chart.Series[index]; var steps = ChartWaterfallSteps.Create(series);
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart, series);
        var coordinates = steps.Select(step => horizontal ? map.Y(step.X) : map.X(step.X)).OrderBy(value => value).ToArray();
        var spacing = Math.Min(horizontal ? plot.Height : plot.Width, map.CalendarBandPitch(horizontal));
        for (var item = 1; item < coordinates.Length; item++) if (coordinates[item] > coordinates[item - 1]) spacing = Math.Min(spacing, coordinates[item] - coordinates[item - 1]);
        var width = Math.Min(spacing * .8, ChartMarkSurface.WaterfallBarWidth(spacing));
        ChartRect? previous = null;
        for (var item = 0; item < steps.Count; item++) {
            var step = steps[item]; var category = horizontal ? map.Y(step.X) : map.X(step.X);
            var start = horizontal ? map.XOrBaseline(step.Start) : map.YOrBaseline(step.Start);
            var end = horizontal ? map.XOrBaseline(step.End) : map.YOrBaseline(step.End);
            var bounds = horizontal ? new ChartRect(Math.Min(start, end), category - width / 2, Math.Abs(start - end), width)
                : new ChartRect(category - width / 2, Math.Min(start, end), width, Math.Abs(start - end));
            var color = WaterfallColor(series, index, step, colors);
            var label = ResolveObservationLabel(chart, context, series, item, colors,
                format => (step.Kind != ChartWaterfallItemKind.Delta || step.Value < 0 ? string.Empty : "+") + format("value", step.Value));
            var id = WaterfallId(index, step);
            var axis = ChartCartesianOrientation.ValueAxis(chart, series.ValueAxis);
            var direction = MappedBarDirection(axis, step.Value, start, end, horizontal);
            if (horizontal) bounds = VisibleSegmentBounds(chart, bounds, direction, horizontal: true);
            var fillRole = WaterfallPaintRole(series, step, item);
            var description = series.Name + ": " + categories[step.X] + " " + label.DisplayedText + " start=" + Number(step.Start) + " end=" + Number(step.End)
                + " " + WaterfallValueName(chart, step) + "=" + Number(step.Value);
            builder.AddRegion(new VisualSemanticRegion(id, WaterfallRole(step), bounds, description));
            var metadata = WaterfallMetadata(chart, index, step, categories[step.X]); metadata["data-cfx-label"] = label.DisplayedText; metadata["aria-label"] = description;
            if (label.NumericCaption("value") is { } numericCaption) {
                metadata["data-cfx-formatted-value"] = numericCaption;
                if (step.Start == step.Value) metadata["data-cfx-formatted-start"] = numericCaption;
                if (step.End == step.Value) metadata["data-cfx-formatted-end"] = numericCaption;
            }
            using (builder.PushGroup(id, WaterfallRole(step), metadata)) {
                if (previous.HasValue && step.Kind != ChartWaterfallItemKind.Total) {
                    var nextIncreasing = category >= (horizontal ? previous.Value.Top + previous.Value.Height / 2 : previous.Value.Left + previous.Value.Width / 2);
                    var connector = step.IsCheckpoint ? end : start;
                    var previousEdge = horizontal ? nextIncreasing ? previous.Value.Bottom : previous.Value.Top : nextIncreasing ? previous.Value.Right : previous.Value.Left;
                    var nextEdge = horizontal ? nextIncreasing ? bounds.Top : bounds.Bottom : nextIncreasing ? bounds.Left : bounds.Right;
                    builder.Line(horizontal ? connector : previousEdge, horizontal ? previousEdge : connector,
                        horizontal ? connector : nextEdge, horizontal ? nextEdge : connector,
                        ChartColorMath.WithOpacity(colors.MutedForeground, ChartVisualPrimitives.WaterfallConnectorOpacity),
                        ChartVisualPrimitives.WaterfallConnectorStrokeWidth, role: "waterfall-connector", dash: new[] { ChartVisualPrimitives.WaterfallConnectorDash, ChartVisualPrimitives.WaterfallConnectorGap },
                        paint: VisualChartPaint.Stroke(SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text).WithOpacity(
                            ChartColorMath.WithOpacity(colors.MutedForeground, ChartVisualPrimitives.WaterfallConnectorOpacity), ChartVisualPrimitives.WaterfallConnectorOpacity)));
                }
                DrawBarSurface(chart, context, builder, series, item, bounds, color, colors, "waterfall-bar",
                    horizontal: horizontal, direction: direction, sourceRole: fillRole);
            }
            obstacles.Add(new LabelObstacle(id, bounds));
            if (horizontal) AddHorizontalLabel(chart, context, series, index, item, new ChartPoint(end, category), bounds, step.Value, direction, label, labels,
                id, color, fillRole);
            else AddLabel(chart, context, series, index, item, new ChartPoint(category, end), bounds, label, labels, step.Value, id,
                barDirection: direction, markFill: color, markFillRole: fillRole);
            previous = bounds;
        }
    }
}
