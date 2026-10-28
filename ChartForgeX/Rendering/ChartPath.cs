using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartPath {
    public ChartPath(IReadOnlyList<ChartPathCommand> commands) {
        Commands = commands ?? throw new ArgumentNullException(nameof(commands));
    }

    public IReadOnlyList<ChartPathCommand> Commands { get; }

    /// <summary>Swaps physical axes while preserving command order and disconnected subpaths.</summary>
    internal ChartPath Transpose() {
        var commands = new List<ChartPathCommand>(Commands.Count);
        foreach (var command in Commands) {
            commands.Add(command.Kind == ChartPathCommandKind.MoveTo ? ChartPathCommand.MoveTo(command.Y, command.X)
                : command.Kind == ChartPathCommandKind.LineTo ? ChartPathCommand.LineTo(command.Y, command.X)
                : ChartPathCommand.CubicTo(command.Control1Y, command.Control1X, command.Control2Y, command.Control2X, command.Y, command.X));
        }
        return new ChartPath(commands);
    }

    /// <summary>Measures endpoints and cubic extrema rather than their control-point hull.</summary>
    internal ChartRect Bounds() {
        if (Commands.Count == 0) return new ChartRect(0, 0, 0, 0);
        var left = double.PositiveInfinity; var top = double.PositiveInfinity;
        var right = double.NegativeInfinity; var bottom = double.NegativeInfinity;
        void Include(ChartPoint point) {
            left = Math.Min(left, point.X); top = Math.Min(top, point.Y);
            right = Math.Max(right, point.X); bottom = Math.Max(bottom, point.Y);
        }
        var current = default(ChartPoint);
        foreach (var command in Commands) {
            var end = new ChartPoint(command.X, command.Y);
            if (command.Kind == ChartPathCommandKind.CubicTo) {
                var start = current;
                var first = new ChartPoint(command.Control1X, command.Control1Y);
                var second = new ChartPoint(command.Control2X, command.Control2Y);
                void IncludeAt(double t) { if (t > 0 && t < 1) Include(CubicPoint(start, first, second, end, t)); }
                CubicExtrema(start.X, first.X, second.X, end.X, IncludeAt);
                CubicExtrema(start.Y, first.Y, second.Y, end.Y, IncludeAt);
            }
            Include(end); current = end;
        }
        return new ChartRect(left, top, right - left, bottom - top);
    }

    private static void CubicExtrema(double start, double first, double second, double end, Action<double> include) {
        var a = end - start + 3 * (first - second);
        var b = 2 * (start - 2 * first + second);
        var c = first - start;
        var scale = Math.Max(Math.Abs(a), Math.Max(Math.Abs(b), Math.Abs(c)));
        if (scale == 0) return;
        a /= scale; b /= scale; c /= scale;
        if (a == 0) { if (b != 0) include(-c / b); return; }
        var discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return;
        var root = Math.Sqrt(discriminant);
        var q = -.5 * (b + (b < 0 ? -root : root));
        if (q == 0) { include(-b / (2 * a)); return; }
        include(q / a); include(c / q);
    }

    public List<ChartPoint> Flatten(double maxSegmentLength = 18, int maximumPoints = int.MaxValue) {
        if (maximumPoints < 2) throw new ArgumentOutOfRangeException(nameof(maximumPoints));
        var points = new List<ChartPoint>();
        var current = default(ChartPoint);
        var hasCurrent = false;
        maxSegmentLength = Math.Max(1, maxSegmentLength);

        foreach (var command in Commands) {
            if (command.Kind == ChartPathCommandKind.MoveTo) {
                current = new ChartPoint(command.X, command.Y, hasCurrent);
                Check(1); points.Add(current);
                hasCurrent = true;
            } else if (command.Kind == ChartPathCommandKind.LineTo && hasCurrent) {
                current = new ChartPoint(command.X, command.Y);
                Check(1); points.Add(current);
            } else if (command.Kind == ChartPathCommandKind.CubicTo && hasCurrent) {
                var end = new ChartPoint(command.X, command.Y);
                var control1 = new ChartPoint(command.Control1X, command.Control1Y);
                var control2 = new ChartPoint(command.Control2X, command.Control2Y);
                var steps = CubicSteps(current, control1, control2, end, maxSegmentLength);
                Check(steps);
                for (var step = 1; step <= steps; step++) {
                    points.Add(CubicPoint(current, control1, control2, end, step / (double)steps));
                }

                current = end;
            }
        }

        return points;

        void Check(int added) { if (added > maximumPoints - points.Count) throw new ArgumentException("Flattened path exceeds its point budget."); }
    }

    private static int CubicSteps(ChartPoint start, ChartPoint control1, ChartPoint control2, ChartPoint end, double maxSegmentLength) {
        var length = Distance(start, control1) + Distance(control1, control2) + Distance(control2, end);
        return Math.Max(6, ChartCurveFlattening.LengthSegments(length, maxSegmentLength));
    }

    private static double Distance(ChartPoint a, ChartPoint b) {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    internal static ChartPoint CubicPoint(ChartPoint p0, ChartPoint c1, ChartPoint c2, ChartPoint p1, double t) {
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
