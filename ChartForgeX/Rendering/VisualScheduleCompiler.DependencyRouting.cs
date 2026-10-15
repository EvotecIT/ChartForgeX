using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
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
