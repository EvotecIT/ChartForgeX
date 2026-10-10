using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartPathBuilder {
    /// <summary>Appends signed circular cubic segments, bounded to a quarter turn by default for native path parity.</summary>
    internal static void AddCircularArc(List<ChartPathCommand> commands, double cx, double cy, double radius, double start, double sweep, double maximumSweep = Math.PI / 2) {
        if (sweep == 0) return;
        var segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(sweep) / maximumSweep));
        var step = sweep / segments;
        var tangent = 4d / 3 * Math.Tan(step / 4);
        for (var index = 0; index < segments; index++) {
            var from = start + index * step; var to = from + step;
            var x1 = cx + Math.Cos(from) * radius; var y1 = cy + Math.Sin(from) * radius;
            var x2 = cx + Math.Cos(to) * radius; var y2 = cy + Math.Sin(to) * radius;
            commands.Add(ChartPathCommand.CubicTo(x1 - Math.Sin(from) * radius * tangent, y1 + Math.Cos(from) * radius * tangent,
                x2 + Math.Sin(to) * radius * tangent, y2 - Math.Cos(to) * radius * tangent, x2, y2));
        }
    }

    public static ChartPath FromPoints(IReadOnlyList<ChartPoint> points, ChartInterpolation interpolation = ChartInterpolation.Linear, ChartStepPosition stepPosition = ChartStepPosition.End) {
        if (points == null) throw new ArgumentNullException(nameof(points));
        var commands = new List<ChartPathCommand>();
        if (points.Count == 0) return new ChartPath(commands);

        foreach (var segment in ChartPointSegments.Split(points)) {
            commands.Add(ChartPathCommand.MoveTo(segment[0].X, segment[0].Y));
            if (segment.Count == 1) {
                // A round-capped zero-length segment keeps an isolated observation visible.
                commands.Add(ChartPathCommand.LineTo(segment[0].X, segment[0].Y));
            } else if (interpolation == ChartInterpolation.Step) AddStepSegments(commands, segment, stepPosition);
            else if (interpolation == ChartInterpolation.Smooth && segment.Count >= 3) AddSmoothSegments(commands, segment);
            else AddStraightSegments(commands, segment);
        }

        return new ChartPath(commands);
    }

    private static void AddStraightSegments(List<ChartPathCommand> commands, IReadOnlyList<ChartPoint> points) {
        for (var i = 1; i < points.Count; i++) commands.Add(ChartPathCommand.LineTo(points[i].X, points[i].Y));
    }

    private static void AddStepSegments(List<ChartPathCommand> commands, IReadOnlyList<ChartPoint> points, ChartStepPosition position) {
        for (var i = 1; i < points.Count; i++) {
            var transition = position == ChartStepPosition.Start ? points[i - 1].X
                : position == ChartStepPosition.Middle ? points[i - 1].X / 2 + points[i].X / 2 : points[i].X;
            if (position != ChartStepPosition.Start) commands.Add(ChartPathCommand.LineTo(transition, points[i - 1].Y));
            if (position != ChartStepPosition.End) commands.Add(ChartPathCommand.LineTo(transition, points[i].Y));
            commands.Add(ChartPathCommand.LineTo(points[i].X, points[i].Y));
        }
    }

    private static void AddSmoothSegments(List<ChartPathCommand> commands, IReadOnlyList<ChartPoint> points) {
        for (var i = 0; i < points.Count - 1; i++) {
            var p0 = points[Math.Max(0, i - 1)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Math.Min(points.Count - 1, i + 2)];
            commands.Add(ChartPathCommand.CubicTo(
                p1.X + (p2.X - p0.X) / 6,
                p1.Y + (p2.Y - p0.Y) / 6,
                p2.X - (p3.X - p1.X) / 6,
                p2.Y - (p3.Y - p1.Y) / 6,
                p2.X,
                p2.Y));
        }
    }
    internal static ChartPath RoundedRectangle(ChartRect bounds, double radius) {
        radius = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2);
        if (radius <= 0) return new ChartPath(new[] { ChartPathCommand.MoveTo(bounds.Left, bounds.Top), ChartPathCommand.LineTo(bounds.Right, bounds.Top), ChartPathCommand.LineTo(bounds.Right, bounds.Bottom), ChartPathCommand.LineTo(bounds.Left, bounds.Bottom) });
        const double k = .5522847498307936;
        var r = radius; var c = r * k;
        var x = bounds.Left; var y = bounds.Top; var right = bounds.Right; var bottom = bounds.Bottom;
        return new ChartPath(new[] {
            ChartPathCommand.MoveTo(x + r, y), ChartPathCommand.LineTo(right - r, y),
            ChartPathCommand.CubicTo(right - r + c, y, right, y + r - c, right, y + r), ChartPathCommand.LineTo(right, bottom - r),
            ChartPathCommand.CubicTo(right, bottom - r + c, right - r + c, bottom, right - r, bottom), ChartPathCommand.LineTo(x + r, bottom),
            ChartPathCommand.CubicTo(x + r - c, bottom, x, bottom - r + c, x, bottom - r), ChartPathCommand.LineTo(x, y + r),
            ChartPathCommand.CubicTo(x, y + r - c, x + r - c, y, x + r, y)
        });
    }

    internal static ChartPath Ellipse(double x, double y, double rx, double ry) {
        const double k = .5522847498307936;
        return new ChartPath(new[] {
            ChartPathCommand.MoveTo(x + rx, y), ChartPathCommand.CubicTo(x + rx, y + ry * k, x + rx * k, y + ry, x, y + ry),
            ChartPathCommand.CubicTo(x - rx * k, y + ry, x - rx, y + ry * k, x - rx, y),
            ChartPathCommand.CubicTo(x - rx, y - ry * k, x - rx * k, y - ry, x, y - ry),
            ChartPathCommand.CubicTo(x + rx * k, y - ry, x + rx, y - ry * k, x + rx, y)
        });
    }

}
