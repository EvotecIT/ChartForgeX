using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class TopologyCrossingPaintPlan {
    private ChartPath? Paint(Stroke stroke, TopologyCrossingPaint policy, Dictionary<(int Stroke, int Segment), List<double>> hops) {
        var originalCrossings = CrossingCount;
        var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(stroke.Points[0].X, stroke.Points[0].Y) };
        for (var index = 1; index < stroke.Points.Count; index++) {
            var first = stroke.Points[index - 1]; var last = stroke.Points[index];
            if (!hops.TryGetValue((stroke.Key, index - 1), out var rows)) { Line(last); continue; }
            var segment = new Segment(stroke, 0, index - 1, first, last);
            var tangent = new ChartPoint(Math.Sign(last.X - first.X), Math.Sign(last.Y - first.Y));
            // Stable screen-facing bridge orientation, independent of the route's direction.
            var normal = segment.Horizontal ? new ChartPoint(0, -1) : new ChartPoint(1, 0);
            rows.Sort();
            for (var hop = 0; hop < rows.Count; hop++) {
                var distance = rows[hop];
                var startClearance = index == 1 ? stroke.StartClearance : 0;
                var endClearance = index == stroke.Points.Count - 1 ? stroke.EndClearance : 0;
                var radius = Math.Min(policy.Radius, Math.Min(distance - Math.Max(policy.BendClearance, startClearance), segment.Length - Math.Max(policy.BendClearance, endClearance) - distance));
                if (hop > 0) radius = Math.Min(radius, (distance - rows[hop - 1]) / 2 - .001 * policy.PreparedUnits);
                if (hop + 1 < rows.Count) radius = Math.Min(radius, (rows[hop + 1] - distance) / 2 - .001 * policy.PreparedUnits);
                if (radius < policy.MinimumRadius) { CrowdedCrossingCount++; continue; }
                var center = Along(first, tangent, distance);
                var start = Along(center, tangent, -radius); var end = Along(center, tangent, radius);
                Line(start);
                if (policy.Style == TopologyCrossingPaintStyle.Gap) commands.Add(ChartPathCommand.MoveTo(end.X, end.Y));
                else {
                    const double quarterCircle = .5522847498307936;
                    var apex = Along(center, normal, radius);
                    var a = Along(start, normal, quarterCircle * radius); var b = Along(apex, tangent, -quarterCircle * radius);
                    var c = Along(apex, tangent, quarterCircle * radius); var d = Along(end, normal, quarterCircle * radius);
                    Validate(a); Validate(b); Validate(apex); Validate(c); Validate(d);
                    commands.Add(ChartPathCommand.CubicTo(a.X, a.Y, b.X, b.Y, apex.X, apex.Y));
                    commands.Add(ChartPathCommand.CubicTo(c.X, c.Y, d.X, d.Y, end.X, end.Y));
                }
                CrossingCount++;
                if (commands.Count > 4096) throw new NotSupportedException("Crossing paint exceeds 4096 commands in one path.");
            }
            Line(last);
        }
        if (commands.Count > 4096) throw new NotSupportedException("Crossing paint exceeds 4096 commands in one path.");
        if (CrossingCount == originalCrossings) return null; // No replacement path when every requested hop is excluded.
        _generatedCommands = checked(_generatedCommands + commands.Count - stroke.SourcePointCount);
        if (_generatedCommands > TopologyPathDecoration.MaximumTotalCommands)
            throw new NotSupportedException("Crossing paint exceeds the native 65536 total-command limit.");
        return new ChartPath(commands);

        void Line(ChartPoint point) { Validate(point); commands.Add(ChartPathCommand.LineTo(point.X, point.Y)); }
    }
    private static ChartPoint Along(ChartPoint point, ChartPoint direction, double distance) =>
        new(point.X + direction.X * distance, point.Y + direction.Y * distance);
}
