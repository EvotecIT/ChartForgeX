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
            var color = step.IsTotal ? colors.Status.Medium.Fill : step.Delta >= 0 ? colors.Status.Pass.Fill : colors.Status.Critical.Fill;
            if (HasSeriesPaint(series, item))
                color = PointColor(series, index, item, colors);
            var label = ResolveObservationLabel(chart, context, series, item, colors,
                () => (step.IsTotal || step.Delta < 0 ? string.Empty : "+") + Value(chart, step.Delta));
            var id = step.IsTotal ? SeriesId(index) + "-total" : PointId(index, item);
            var description = series.Name + ": " + label.DisplayedText + " start=" + Number(step.Start) + " end=" + Number(step.End) + " delta=" + Number(step.Delta);
            builder.AddRegion(new VisualSemanticRegion(id, step.IsTotal ? "waterfall-total" : "point", bounds, description));
            using (builder.PushGroup(id, step.IsTotal ? "waterfall-total" : "point", new Dictionary<string, string> {
                ["data-cfx-series"] = Number(index), ["data-cfx-point"] = Number(step.SourceIndex), ["data-cfx-source-point"] = Number(step.SourceIndex),
                ["data-cfx-x"] = Number(step.X), ["data-cfx-start"] = Number(step.Start), ["data-cfx-end"] = Number(step.End),
                ["data-cfx-delta"] = Number(step.Delta), ["data-cfx-label"] = label.DisplayedText, ["data-cfx-derived-total"] = step.IsTotal ? "true" : "false",
                ["data-cfx-source-count"] = Number(step.IsTotal ? series.Points.Count : 1), ["aria-label"] = description
            })) {
                if (previous.HasValue && !step.IsTotal && !series.Points[item].BreakBefore)
                    builder.Line(previous.Value.Right, startY, bounds.Left, startY, ChartColorMath.WithOpacity(colors.MutedForeground, ChartVisualPrimitives.WaterfallConnectorOpacity),
                        ChartVisualPrimitives.WaterfallConnectorStrokeWidth, role: "waterfall-connector", dash: new[] { ChartVisualPrimitives.WaterfallConnectorDash, ChartVisualPrimitives.WaterfallConnectorGap },
                        paint: VisualChartPaint.Stroke(SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text).WithOpacity(
                            ChartColorMath.WithOpacity(colors.MutedForeground, ChartVisualPrimitives.WaterfallConnectorOpacity), ChartVisualPrimitives.WaterfallConnectorOpacity)));
                DrawBarSurface(chart, context, builder, series, item, bounds, color, colors, "waterfall-bar", value: step.Delta, sourceRole: SemanticMarkPaintRole(series, item));
            }
            obstacles.Add(new LabelObstacle(id, bounds));
            AddLabel(chart, context, series, index, item, new ChartPoint(x, endY), bounds, label, labels, step.IsTotal ? step.End : step.Delta, id);
            previous = bounds;
        }
    }
}
