using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed class ChartPath {
    public ChartPath(IReadOnlyList<ChartPathCommand> commands) {
        Commands = commands ?? throw new ArgumentNullException(nameof(commands));
    }

    public IReadOnlyList<ChartPathCommand> Commands { get; }

    public List<ChartPoint> Flatten(double maxSegmentLength = 18) {
        var points = new List<ChartPoint>();
        var current = default(ChartPoint);
        var hasCurrent = false;
        maxSegmentLength = Math.Max(1, maxSegmentLength);

        foreach (var command in Commands) {
            if (command.Kind == ChartPathCommandKind.MoveTo) {
                current = new ChartPoint(command.X, command.Y, hasCurrent);
                points.Add(current);
                hasCurrent = true;
            } else if (command.Kind == ChartPathCommandKind.LineTo && hasCurrent) {
                current = new ChartPoint(command.X, command.Y);
                points.Add(current);
            } else if (command.Kind == ChartPathCommandKind.CubicTo && hasCurrent) {
                var end = new ChartPoint(command.X, command.Y);
                var control1 = new ChartPoint(command.Control1X, command.Control1Y);
                var control2 = new ChartPoint(command.Control2X, command.Control2Y);
                var steps = CubicSteps(current, control1, control2, end, maxSegmentLength);
                for (var step = 1; step <= steps; step++) {
                    points.Add(CubicPoint(current, control1, control2, end, step / (double)steps));
                }

                current = end;
            }
        }

        return points;
    }

    private static int CubicSteps(ChartPoint start, ChartPoint control1, ChartPoint control2, ChartPoint end, double maxSegmentLength) {
        var length = Distance(start, control1) + Distance(control1, control2) + Distance(control2, end);
        return Math.Max(6, (int)Math.Ceiling(length / maxSegmentLength));
    }

    private static double Distance(ChartPoint a, ChartPoint b) {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static ChartPoint CubicPoint(ChartPoint p0, ChartPoint c1, ChartPoint c2, ChartPoint p1, double t) {
        var inverse = 1 - t;
        var a = inverse * inverse * inverse;
        var b = 3 * inverse * inverse * t;
        var c = 3 * inverse * t * t;
        var d = t * t * t;
        return new ChartPoint(
            p0.X * a + c1.X * b + c2.X * c + p1.X * d,
            p0.Y * a + c1.Y * b + c2.Y * c + p1.Y * d);
    }
}
