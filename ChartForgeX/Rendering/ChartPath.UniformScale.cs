using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartPath {
    /// <summary>Applies one uniform logical transform to endpoints and controls without reconstructing curves or joining subpaths.</summary>
    internal ChartPath UniformScale(ChartPoint origin, double factor) {
        ChartPrimitiveGuards.Finite(factor, nameof(factor));
        if (factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor));
        if (factor == 1) return this;
        var commands = new List<ChartPathCommand>(Commands.Count);
        foreach (var command in Commands) {
            var end = UniformScalePoint(new ChartPoint(command.X, command.Y), origin, factor);
            if (command.Kind == ChartPathCommandKind.MoveTo) commands.Add(ChartPathCommand.MoveTo(end.X, end.Y));
            else if (command.Kind == ChartPathCommandKind.LineTo) commands.Add(ChartPathCommand.LineTo(end.X, end.Y));
            else {
                var first = UniformScalePoint(new ChartPoint(command.Control1X, command.Control1Y), origin, factor);
                var second = UniformScalePoint(new ChartPoint(command.Control2X, command.Control2Y), origin, factor);
                commands.Add(ChartPathCommand.CubicTo(first.X, first.Y, second.X, second.Y, end.X, end.Y));
            }
        }
        var previous = default(ChartPoint);
        foreach (var command in commands) {
            var end = new ChartPoint(command.X, command.Y);
            if (command.Kind == ChartPathCommandKind.CubicTo) {
                var first = new ChartPoint(command.Control1X, command.Control1Y); var second = new ChartPoint(command.Control2X, command.Control2Y);
                Representable(previous, first); Representable(first, second); Representable(second, end);
            } else if (command.Kind == ChartPathCommandKind.LineTo) Representable(previous, end);
            previous = end;
        }
        return new ChartPath(commands);
    }
    private static void Representable(ChartPoint first, ChartPoint second) {
        var x = second.X - first.X; var y = second.Y - first.Y;
        if (!ChartMath.IsFinite(x * x + y * y))
            throw new ArgumentOutOfRangeException("factor", "The uniform transform cannot retain finite path lengths for native curve and stroke processing.");
    }
    /// <summary>Uses the same affine arithmetic for semantic source positions and typed path coordinates.</summary>
    internal static ChartPoint UniformScalePoint(ChartPoint point, ChartPoint origin, double factor) {
        if (factor == 1) return point;
        return new ChartPoint(origin.X + (point.X - origin.X) * factor, origin.Y + (point.Y - origin.Y) * factor, point.BreakBefore).WithColorValue(point.ColorValue);
    }
}
