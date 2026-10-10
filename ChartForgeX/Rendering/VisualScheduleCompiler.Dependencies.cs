using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private static void Dependencies(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        IReadOnlyList<RangeItem> items, IReadOnlyList<ChartGanttDependency> links, Func<double, double> project, double slot, double height) {
        var color = context.Theme.Resolve(context.ThemeMode).MutedForeground;
        var paint = VisualChartPaint.Stroke(color, SvgColorRole.Text);
        using (builder.PushClip(plot)) foreach (var link in links) {
            var previous = items[link.PredecessorIndex];
            var item = items[link.SuccessorIndex];
            if (previous.Marker || item.Marker) continue;
            var milestoneExtent = Math.Min(height, plot.Width) / 2;
            var start = new ChartPoint(project(previous.End) + (previous.Milestone ? milestoneExtent : 0), plot.Top + (previous.Row + .5) * slot);
            var end = new ChartPoint(project(item.Start) + (item.Milestone ? milestoneExtent : 0), plot.Top + (item.Row + .5) * slot);
            var elbow = Math.Max(start.X, end.X) + Math.Min(context.Theme.Spacing, plot.Right - Math.Max(start.X, end.X));
            var id = "gantt-dependency-" + previous.Index + "-" + item.Index;
            using (VisualStateSceneTools.Mark(builder, id, "gantt-dependency", new ChartRect(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y),
                Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y)), chart.Series[previous.Index].Name + " → " + chart.Series[item.Index].Name,
                new Dictionary<string, string> { ["data-cfx-source"] = "series-" + previous.Index, ["data-cfx-target"] = "series-" + item.Index })) {
                builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(elbow, start.Y),
                    ChartPathCommand.LineTo(elbow, end.Y), ChartPathCommand.LineTo(end.X, end.Y) }), stroke: color,
                    strokeWidth: context.Theme.AxisStrokeWidth, role: "gantt-dependency-line", paint: paint);
                var direction = end.X < elbow ? -1d : 1d; var arrow = Math.Min(5, height / 3);
                builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(end.X - direction * arrow, end.Y - arrow), ChartPathCommand.LineTo(end.X, end.Y),
                    ChartPathCommand.LineTo(end.X - direction * arrow, end.Y + arrow) }), stroke: color,
                    strokeWidth: context.Theme.AxisStrokeWidth, role: "gantt-dependency-arrow", paint: paint);
            }
        }
    }
}
