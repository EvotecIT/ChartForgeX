using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private static void Dependencies(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        IReadOnlyDictionary<int, RangeItem> items, IReadOnlyList<ChartGanttOrdinalDependency> links, Func<double, double> project, double slot, double height, double minimum, double maximum) {
        var color = context.Theme.Resolve(context.ThemeMode).MutedForeground;
        var paint = VisualChartPaint.Stroke(color, SvgColorRole.Text);
        using (builder.PushClip(plot)) foreach (var link in links) {
            var previous = items[link.PredecessorIndex];
            var item = items[link.SuccessorIndex];
            if (previous.Marker || item.Marker) continue;
            if ((previous.End < minimum && item.End < minimum) || (previous.Start > maximum && item.Start > maximum)) continue;
            var source = RangeBounds(previous, plot, project, slot, height);
            var target = RangeBounds(item, plot, project, slot, height);
            var arrow = DependencyArrowExtent(height);
            var start = new ChartPoint(source.Right + DependencyClearance(context), source.Top + source.Height / 2);
            var end = new ChartPoint(target.Left - DependencyClearance(context), target.Top + target.Height / 2);
            var commands = DependencyRoute(start, end, slot, arrow, DependencyStubLength(context, arrow));
            var arrowCommands = new[] { ChartPathCommand.MoveTo(end.X - arrow, end.Y - arrow), ChartPathCommand.LineTo(end.X, end.Y),
                ChartPathCommand.LineTo(end.X - arrow, end.Y + arrow) };
            var points = commands.Concat(arrowCommands).ToArray(); var halfStroke = context.Theme.AxisStrokeWidth / 2;
            var left = points.Min(point => point.X) - halfStroke; var top = points.Min(point => point.Y) - halfStroke;
            var bounds = new ChartRect(left, top, points.Max(point => point.X) + halfStroke - left, points.Max(point => point.Y) + halfStroke - top);
            var id = "gantt-dependency-" + previous.Index + "-" + item.Index;
            using (VisualStateSceneTools.Mark(builder, id, "gantt-dependency", bounds, chart.Series[previous.Index].Name + " → " + chart.Series[item.Index].Name,
                new Dictionary<string, string> { ["data-cfx-source"] = "series-" + previous.Index, ["data-cfx-target"] = "series-" + item.Index,
                    ["data-cfx-predecessor-id"] = chart.Series[previous.Index].Id.Value, ["data-cfx-successor-id"] = chart.Series[item.Index].Id.Value })) {
                builder.Path(new ChartPath(commands), stroke: color,
                    strokeWidth: context.Theme.AxisStrokeWidth, role: "gantt-dependency-line", paint: paint);
                builder.Path(new ChartPath(arrowCommands), stroke: color,
                    strokeWidth: context.Theme.AxisStrokeWidth, role: "gantt-dependency-arrow", paint: paint);
            }
        }
    }

    private static double DependencyClearance(VisualRenderContext context) => context.Theme.AxisStrokeWidth / 2 + Math.Max(2, context.Theme.Spacing / 4);
    private static double DependencyArrowExtent(double height) => Math.Min(5, height / 3);
    private static double DependencyStubLength(VisualRenderContext context, double arrow) => Math.Max(arrow, context.Theme.Spacing);

    /// <summary>Exits the predecessor's finish and enters the successor's start; overlapping spans turn in the row gutter.</summary>
    private static ChartPathCommand[] DependencyRoute(ChartPoint start, ChartPoint end, double slot, double arrow, double stub) {
        if (end.X - start.X >= arrow * 2) {
            var elbow = start.X / 2 + end.X / 2;
            return new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(elbow, start.Y),
                ChartPathCommand.LineTo(elbow, end.Y), ChartPathCommand.LineTo(end.X, end.Y) };
        }
        var gutter = start.Y + Math.Sign(end.Y - start.Y) * slot / 2;
        return new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(start.X + stub, start.Y),
            ChartPathCommand.LineTo(start.X + stub, gutter), ChartPathCommand.LineTo(end.X - arrow, gutter),
            ChartPathCommand.LineTo(end.X - arrow, end.Y), ChartPathCommand.LineTo(end.X, end.Y) };
    }
}
