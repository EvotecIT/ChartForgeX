using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private static void Markers(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        IReadOnlyList<RangeItem> items, Func<double, double> project, double min, double max, VisualThemeColors colors, List<LabelObstacle> obstacles) {
        using (builder.PushClip(plot)) {
            VisualAnnotationCompiler.Draw(chart.Annotations, context, builder, plot, project, _ => 0, colors, false, obstacles,
                annotation => annotation.Kind == ChartAnnotationKind.VerticalLine && annotation.Value >= min && annotation.Value <= max,
                "schedule.annotation-label-overflow");
            foreach (var item in items.Where(item => item.Marker && item.Start >= min && item.Start <= max)) {
                var series = chart.Series[item.Index];
                var color = series.Color ?? colors.MutedForeground;
                var bounds = new ChartRect(project(item.Start), plot.Top, 0, plot.Height);
                var id = VisualStateSceneTools.SourceId(item.Index, 0);
                using (VisualStateSceneTools.Mark(builder, id, "gantt-vertical-marker", bounds, series.Name,
                    new Dictionary<string, string> {
                        ["data-cfx-series"] = item.Index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["data-cfx-start"] = VisualStateSceneTools.Number(item.Start),
                        ["data-cfx-end"] = VisualStateSceneTools.Number(item.End),
                        ["data-cfx-series-key"] = series.InteractionIdentityKey
                    })) {
                    var annotation = new ChartAnnotation(ChartAnnotationKind.VerticalLine, item.Start, null, series.Name, color, 1);
                    VisualAnnotationCompiler.Draw(new[] { annotation }, context, builder, plot, project, _ => 0, colors, false, obstacles,
                        overflowCode: "schedule.annotation-label-overflow", idPrefix: id + "-annotation-");
                }
            }
        }
    }
}
