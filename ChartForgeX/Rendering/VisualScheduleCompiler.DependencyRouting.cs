using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    // A later row receives forward-time links through its top surface. Aligned finish/start
    // instants can leave the predecessor's bottom instead of looping around the successor.
    private static (ChartPathCommand[] Route, ChartPoint Start, ChartPoint End)? ForwardDependencyRoute(
        ChartRect source, ChartRect target, bool sourceMilestone, bool targetMilestone, ChartRect plot,
        IReadOnlyList<ChartRect> surfaces, double arrow, double halfStroke, double terminal, double clearance, double radius) {
        if (arrow <= 0 || target.Top - source.Bottom <= halfStroke * 2 + arrow) return null;
        var start = new ChartPoint(source.Right + terminal, source.Y + source.Height / 2);
        var targetInset = Math.Max(arrow, Math.Min(radius, Math.Min(target.Width, target.Height) / 2));
        var end = new ChartPoint(targetMilestone ? target.X + target.Width / 2
            : Math.Min(target.Right - targetInset, Math.Max(start.X + arrow * 2, target.Left + targetInset)), target.Top - terminal);
        // A successor that starts where the predecessor finishes takes a short elbow off the finish into its
        // top, so the arrowhead lands clearly on the successor rather than in a step between the two bars.
        if (end.X >= start.X + arrow) {
            var elbow = new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(end.X, start.Y), ChartPathCommand.LineTo(end.X, end.Y) };
            if (DependencyPaintFits(elbow, surfaces, plot, halfStroke)) return (elbow, start, end);
        }
        var sourceFinish = sourceMilestone ? source.X + source.Width / 2 : source.Right;
        var targetStart = targetMilestone ? target.X + target.Width / 2 : target.Left;
        if (Math.Abs(sourceFinish - targetStart) <= clearance * 2) {
            var sourceInset = Math.Min(radius, Math.Min(source.Width, source.Height) / 2);
            var bottom = new ChartPoint(sourceMilestone ? sourceFinish
                : Math.Max(source.Left + sourceInset, Math.Min(source.Right - sourceInset, end.X)), source.Bottom + terminal);
            if (end.X >= bottom.X && end.Y > bottom.Y) {
                var gutter = (bottom.Y + end.Y) / 2;
                var direct = new[] { ChartPathCommand.MoveTo(bottom.X, bottom.Y), ChartPathCommand.LineTo(bottom.X, gutter),
                    ChartPathCommand.LineTo(end.X, gutter), ChartPathCommand.LineTo(end.X, end.Y) };
                if (DependencyPaintFits(direct, surfaces, plot, halfStroke)) return (direct, bottom, end);
            }
        }
        if (end.X < start.X) return null;
        var shortRoute = new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(end.X, start.Y), ChartPathCommand.LineTo(end.X, end.Y) };
        if (DependencyPaintFits(shortRoute, surfaces, plot, halfStroke)) return (shortRoute, start, end);
        var crossedRows = surfaces.Where(bounds => bounds.Top < target.Top && bounds.Bottom > source.Top).ToArray();
        var corridor = Math.Max(start.X + arrow, crossedRows.Select(bounds => bounds.Right + clearance).DefaultIfEmpty(start.X).Max());
        var topGutter = target.Top - Math.Max(clearance, arrow + halfStroke + .0001);
        var bypass = new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(corridor, start.Y),
            ChartPathCommand.LineTo(corridor, topGutter), ChartPathCommand.LineTo(end.X, topGutter), ChartPathCommand.LineTo(end.X, end.Y) };
        return DependencyPaintFits(bypass, surfaces, plot, halfStroke) ? (bypass, start, end) : null;
    }

    /// <summary>Keeps short finish-to-start links when clear, and bypasses occupied rows before the final left approach.</summary>
    private static ChartPathCommand[]? DependencyRoute(ChartPoint start, ChartPoint end, ChartRect plot, double slot,
        double arrow, double stub, double clearance, double halfStroke, IReadOnlyList<ChartRect> surfaces) {
        // Automatic windows already reserve the outgoing stub. A one-pixel instant task can consume its last
        // fraction; use the remaining space without changing an authored window or an off-plot endpoint.
        var exit = start.X < plot.Right - halfStroke
            ? Math.Max(start.X + arrow, Math.Min(start.X + stub, plot.Right - halfStroke)) : start.X + stub;
        ChartPathCommand[] shortRoute;
        if (end.X - start.X >= arrow * 2) {
            var elbow = start.X / 2 + end.X / 2;
            shortRoute = new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(elbow, start.Y),
                ChartPathCommand.LineTo(elbow, end.Y), ChartPathCommand.LineTo(end.X, end.Y) };
        } else {
            var gutter = start.Y + Math.Sign(end.Y - start.Y) * slot / 2;
            shortRoute = new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(exit, start.Y),
                ChartPathCommand.LineTo(exit, gutter), ChartPathCommand.LineTo(end.X - arrow, gutter),
                ChartPathCommand.LineTo(end.X - arrow, end.Y), ChartPathCommand.LineTo(end.X, end.Y) };
        }
        if (DependencyPaintFits(shortRoute, surfaces, plot, halfStroke)) return shortRoute;

        // A midpoint or successor-side vertical leg can cut through a task on a skipped row. Move that long leg
        // beyond every visible task/diamond, then cross only the gutter immediately beside the target row.
        var corridor = surfaces.Count == 0 ? exit : Math.Max(exit, surfaces.Max(bounds => bounds.Right) + clearance);
        var targetGutter = end.Y - Math.Sign(end.Y - start.Y) * slot / 2;
        var bypass = new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(corridor, start.Y),
            ChartPathCommand.LineTo(corridor, targetGutter), ChartPathCommand.LineTo(end.X - arrow, targetGutter),
            ChartPathCommand.LineTo(end.X - arrow, end.Y), ChartPathCommand.LineTo(end.X, end.Y) };
        return DependencyPaintFits(bypass, surfaces, plot, halfStroke) ? bypass : null;
    }

    /// <summary>Uses the actual stroke extent; an interior route may span more than one row without touching a mark.</summary>
    private static bool DependencyPaintFits(IReadOnlyList<ChartPathCommand> route, IReadOnlyList<ChartRect> surfaces, ChartRect plot, double radius) =>
        radius <= 0 || route.All(point => point.Y - radius >= plot.Top && point.Y + radius <= plot.Bottom)
            && DependencyRouteClears(route, surfaces, radius);

    /// <summary>Checks each orthogonal run against the actual painted surfaces, including the connector's radius.</summary>
    private static bool DependencyRouteClears(IReadOnlyList<ChartPathCommand> route, IReadOnlyList<ChartRect> surfaces, double radius) {
        if (radius <= 0) return true;
        for (var index = 1; index < route.Count; index++) {
            var first = route[index - 1]; var last = route[index];
            var left = Math.Min(first.X, last.X) - radius; var right = Math.Max(first.X, last.X) + radius;
            var top = Math.Min(first.Y, last.Y) - radius; var bottom = Math.Max(first.Y, last.Y) + radius;
            foreach (var bounds in surfaces)
                if (left < bounds.Right && right > bounds.Left && top < bounds.Bottom && bottom > bounds.Top) return false;
        }
        return true;
    }
}
