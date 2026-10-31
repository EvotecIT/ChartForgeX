using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private static void Dependencies(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        IReadOnlyDictionary<int, RangeItem> items, IReadOnlyList<ChartGanttOrdinalDependency> links, Func<double, double> project, double slot, double height, double minimum, double maximum, IReadOnlyList<ChartRect>? otherSurfaces = null) {
        // Connectors are context, not data: a light 1px secondary-ink route with a small chevron, so bars stay the subject.
        var ink = context.Theme.Resolve(context.ThemeMode).MutedForeground;
        var color = ChartColorMath.WithOpacity(ink, DependencyOpacity);
        var paint = VisualChartPaint.Stroke(SvgPaint.Of(ink, SvgColorRole.Text).WithOpacity(color, DependencyOpacity));
        var surfaces = items.Values.Where(item => !item.Marker && item.End >= minimum && item.Start <= maximum)
            .Select(item => RangeBounds(chart, item, plot, project, slot, height)).Concat(otherSurfaces ?? Array.Empty<ChartRect>())
            .Select(surface => ScheduleCanonicalX(chart, plot, surface)).ToArray();
        using (builder.PushClip(plot)) foreach (var link in links) {
            var previous = items[link.PredecessorIndex];
            var item = items[link.SuccessorIndex];
            if (previous.Marker || item.Marker) continue;
            if ((previous.End < minimum && item.End < minimum) || (previous.Start > maximum && item.Start > maximum)) continue;
            var source = ScheduleCanonicalX(chart, plot, RangeBounds(chart, previous, plot, project, slot, height));
            var target = ScheduleCanonicalX(chart, plot, RangeBounds(chart, item, plot, project, slot, height));
            var arrow = DependencyArrowExtent(height);
            var start = new ChartPoint(source.Right + DependencyClearance(context), source.Top + source.Height / 2);
            var end = new ChartPoint(target.Left - DependencyClearance(context), target.Top + target.Height / 2);
            var halfStroke = context.Theme.AxisStrokeWidth / 2;
            var commands = DependencyRoute(start, end, plot, slot, arrow, DependencyStubLength(context, arrow),
                DependencyClearance(context), halfStroke, surfaces);
            var arrowCommands = new[] { ChartPathCommand.MoveTo(end.X - arrow, end.Y - arrow), ChartPathCommand.LineTo(end.X, end.Y),
                ChartPathCommand.LineTo(end.X - arrow, end.Y + arrow) };
            if (!DependencyPaintFits(arrowCommands, surfaces, plot, halfStroke)) commands = null;
            if (commands != null) commands = SchedulePhysicalPath(chart, plot, commands);
            arrowCommands = SchedulePhysicalPath(chart, plot, arrowCommands);
            if (chart.Options.XAxis.Reversed) { start = new ChartPoint(plot.Left + plot.Right - start.X, start.Y); end = new ChartPoint(plot.Left + plot.Right - end.X, end.Y); }
            var points = (commands ?? new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(end.X, end.Y) }).Concat(arrowCommands).ToArray();
            var left = points.Min(point => point.X) - halfStroke; var top = points.Min(point => point.Y) - halfStroke;
            var bounds = new ChartRect(left, top, points.Max(point => point.X) + halfStroke - left, points.Max(point => point.Y) + halfStroke - top);
            var id = "gantt-dependency-" + previous.Index + "-" + item.Index;
            var metadata = new Dictionary<string, string> { ["data-cfx-source"] = "series-" + previous.Index, ["data-cfx-target"] = "series-" + item.Index,
                ["data-cfx-predecessor-id"] = chart.Series[previous.Index].Id.Value, ["data-cfx-successor-id"] = chart.Series[item.Index].Id.Value };
            if (commands == null) {
                metadata["data-cfx-geometry-status"] = "insufficient-space";
                builder.AddDiagnostic(new VisualDiagnostic("gantt.dependency-insufficient-space", "Dependency '" + id + "' ("
                    + chart.Series[previous.Index].Id.Value + " → " + chart.Series[item.Index].Id.Value
                    + ") cannot fit its declared stroke between the task surfaces; its endpoint facts remain in semantics."));
            }
            using (VisualStateSceneTools.Mark(builder, id, "gantt-dependency", bounds, chart.Series[previous.Index].Name + " → " + chart.Series[item.Index].Name,
                metadata)) {
                if (commands == null) continue;
                builder.Path(new ChartPath(commands), stroke: color,
                    strokeWidth: context.Theme.AxisStrokeWidth, role: "gantt-dependency-line", paint: paint);
                builder.Path(new ChartPath(arrowCommands), stroke: color,
                    strokeWidth: context.Theme.AxisStrokeWidth, role: "gantt-dependency-arrow", paint: paint);
            }
        }
    }

    private static double DependencyClearance(VisualRenderContext context) => context.Theme.AxisStrokeWidth / 2 + Math.Max(2, context.Theme.Spacing / 4);
    private const double DependencyOpacity = .55;
    private static double DependencyArrowExtent(double height) => Math.Min(3, height / 4);
    private static double DependencyStubLength(VisualRenderContext context, double arrow) => Math.Max(arrow, context.Theme.Spacing);

}
