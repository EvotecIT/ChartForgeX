using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartPathBuilder {
    public static ChartPath FromPoints(IReadOnlyList<ChartPoint> points, ChartSeriesKind kind, bool smooth) {
        if (points == null) throw new ArgumentNullException(nameof(points));
        var commands = new List<ChartPathCommand>();
        if (points.Count == 0) return new ChartPath(commands);

        foreach (var segment in ChartPointSegments.Split(points)) {
            commands.Add(ChartPathCommand.MoveTo(segment[0].X, segment[0].Y));
            if (kind == ChartSeriesKind.StepLine || kind == ChartSeriesKind.StepArea) AddStepSegments(commands, segment);
            else if (smooth && segment.Count >= 3) AddSmoothSegments(commands, segment);
            else AddStraightSegments(commands, segment);
        }

        return new ChartPath(commands);
    }

    private static void AddStraightSegments(List<ChartPathCommand> commands, IReadOnlyList<ChartPoint> points) {
        for (var i = 1; i < points.Count; i++) commands.Add(ChartPathCommand.LineTo(points[i].X, points[i].Y));
    }

    private static void AddStepSegments(List<ChartPathCommand> commands, IReadOnlyList<ChartPoint> points) {
        for (var i = 1; i < points.Count; i++) {
            commands.Add(ChartPathCommand.LineTo(points[i].X, points[i - 1].Y));
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
}
