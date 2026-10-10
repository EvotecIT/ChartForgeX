using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private static bool IsMarker(ChartSeries series) => series.Kind == ChartSeriesKind.Gantt && series.Points.Count > 2 && series.Points[2].Y >= .5;
    private static ChartColor MarkerColor(ChartSeries series, VisualThemeColors colors) => ChartSeriesColours.Point(series, 0, colors, colors.MutedForeground);
    private static SvgColorRole MarkerColorRole(ChartSeries series) => VisualChartPaint.SeriesRole(series, 0, fallbackRole: SvgColorRole.Text);

    private static void Markers(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        IReadOnlyList<RangeItem> items, Func<double, double> project, Func<double, string> format, double min, double max, VisualThemeColors colors, List<LabelObstacle> obstacles, IReadOnlyDictionary<int, int[]> predecessors) {
        using (builder.PushClip(plot)) {
            VisualAnnotationCompiler.Draw(chart.Annotations, context, builder, plot, project, _ => 0, colors, false, obstacles,
                annotation => annotation.Kind == ChartAnnotationKind.VerticalLine && annotation.Value >= min && annotation.Value <= max,
                "schedule.annotation-label-overflow");
            foreach (var item in items.Where(item => item.Marker && item.Start >= min && item.Start <= max)) {
                var series = chart.Series[item.Index];
                var color = MarkerColor(series, colors);
                var colorRole = MarkerColorRole(series);
                var bounds = new ChartRect(project(item.Start), plot.Top, 0, plot.Height);
                var id = VisualStateSceneTools.SourceId(item.Index, 0);
                var metadata = new Dictionary<string, string> {
                        ["data-cfx-series"] = item.Index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["data-cfx-start"] = VisualStateSceneTools.Number(item.Start),
                        ["data-cfx-end"] = VisualStateSceneTools.Number(item.End),
                        ["data-cfx-value"] = VisualStateSceneTools.Number(item.Start),
                        ["data-cfx-meta-position"] = format(item.Start),
                        ["data-cfx-series-key"] = series.InteractionIdentityKey
                    };
                DependencyMetadata(metadata, predecessors, item.Index);
                using (VisualStateSceneTools.Mark(builder, id, "gantt-vertical-marker", bounds, series.Name + ": " + format(item.Start), metadata)) {
                    var annotation = new ChartAnnotation(ChartAnnotationKind.VerticalLine, item.Start, null, series.Name, color, 1);
                    VisualAnnotationCompiler.Draw(new[] { annotation }, context, builder, plot, project, _ => 0, colors, false, obstacles,
                        overflowCode: "schedule.annotation-label-overflow", idPrefix: id + "-annotation-", describeAnnotations: false, colorRole: colorRole);
                }
            }
        }
    }
}
