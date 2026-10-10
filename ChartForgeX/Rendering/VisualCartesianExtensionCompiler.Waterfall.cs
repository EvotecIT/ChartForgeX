using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawPreparedWaterfall(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        var series = chart.Series[index]; var steps = ChartWaterfallSteps.Create(series);
        var coordinates = steps.Select(step => map.X(step.X)).OrderBy(value => value).ToArray(); var spacing = plot.Width;
        for (var item = 1; item < coordinates.Length; item++) if (coordinates[item] > coordinates[item - 1]) spacing = Math.Min(spacing, coordinates[item] - coordinates[item - 1]);
        var width = Math.Min(spacing * .8, ChartMarkSurface.WaterfallBarWidth(spacing));
        ChartRect? previous = null;
        for (var item = 0; item < steps.Count; item++) {
            var step = steps[item]; var x = map.X(step.X); var startY = map.YOrBaseline(step.Start); var endY = map.YOrBaseline(step.End);
            var bounds = new ChartRect(x - width / 2, Math.Min(startY, endY), width, Math.Abs(startY - endY));
            var color = WaterfallColor(series, index, step, colors);
            var label = ResolveObservationLabel(chart, context, series, item, colors,
                () => (step.IsCheckpoint || step.Value < 0 ? string.Empty : "+") + Value(chart, step.Value));
            var id = WaterfallId(index, step);
            var axis = series.YAxis == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
            var direction = MappedBarDirection(axis, step.Value, startY, endY);
            var fillRole = SemanticMarkPaintRole(series, item);
            var description = series.Name + ": " + label.DisplayedText + " start=" + Number(step.Start) + " end=" + Number(step.End)
                + " " + WaterfallValueName(chart, step) + "=" + Number(step.Value);
            builder.AddRegion(new VisualSemanticRegion(id, WaterfallRole(step), bounds, description));
            var metadata = WaterfallMetadata(chart, index, step); metadata["data-cfx-label"] = label.DisplayedText; metadata["aria-label"] = description;
            using (builder.PushGroup(id, WaterfallRole(step), metadata)) {
                if (previous.HasValue && step.Kind != ChartWaterfallItemKind.Total) {
                    var nextOnRight = bounds.Left + bounds.Width / 2 >= previous.Value.Left + previous.Value.Width / 2;
                    var connectorY = step.IsCheckpoint ? endY : startY;
                    builder.Line(nextOnRight ? previous.Value.Right : previous.Value.Left, connectorY, nextOnRight ? bounds.Left : bounds.Right, connectorY,
                        ChartColorMath.WithOpacity(colors.MutedForeground, ChartVisualPrimitives.WaterfallConnectorOpacity),
                        ChartVisualPrimitives.WaterfallConnectorStrokeWidth, role: "waterfall-connector", dash: new[] { ChartVisualPrimitives.WaterfallConnectorDash, ChartVisualPrimitives.WaterfallConnectorGap },
                        paint: VisualChartPaint.Stroke(SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text).WithOpacity(
                            ChartColorMath.WithOpacity(colors.MutedForeground, ChartVisualPrimitives.WaterfallConnectorOpacity), ChartVisualPrimitives.WaterfallConnectorOpacity)));
                }
                DrawBarSurface(chart, context, builder, series, item, bounds, color, colors, "waterfall-bar",
                    direction: direction, sourceRole: fillRole);
            }
            obstacles.Add(new LabelObstacle(id, bounds));
            AddLabel(chart, context, series, index, item, new ChartPoint(x, endY), bounds, label, labels, step.Value, id,
                barDirection: direction, markFill: color, markFillRole: fillRole);
            previous = bounds;
        }
    }
}
