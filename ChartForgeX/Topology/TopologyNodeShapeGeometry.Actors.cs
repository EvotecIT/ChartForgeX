using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    internal static bool IsActor(TopologyNode node) => node.Shape is TopologyNodeShape.Actor or TopologyNodeShape.ActorHollow
        or TopologyNodeShape.ActorBusiness or TopologyNodeShape.ActorBusinessHollow or TopologyNodeShape.ActorSilhouette or TopologyNodeShape.ActorIcon;

    private static IReadOnlyList<ScenePart> ActorParts(TopologyNode node) {
        var parts = new List<ScenePart>(); var cx = node.X + node.Width / 2; var y = node.Y; var s = ActorScale(node);
        if (node.Shape is TopologyNodeShape.ActorHollow or TopologyNodeShape.ActorBusinessHollow) {
            parts.Add(ActorEllipse(cx, y + 15 * s, 9 * s, 9 * s, false));
            parts.Add(ActorPolygon(cx, y, s, new[] { -22d, 28, 22, 28, 22, 38, 6, 38, 22, 55, 13, 66, 0, 51, -13, 66, -22, 55, -6, 38, -22, 38 }, false));
        } else if (node.Shape == TopologyNodeShape.ActorSilhouette) {
            parts.Add(ActorEllipse(cx, y + 15 * s, 10 * s, 10 * s, true));
            ChartPoint P(double x, double dy) => new(cx + x * s, y + dy * s);
            var start = P(-22, 64); var a = P(-22, 43); var b = P(-13, 30); var c = P(0, 30);
            var d = P(13, 30); var e = P(22, 43); var f = P(22, 64);
            parts.Add(new ScenePart(new ChartPath(new[] { ChartPathCommand.MoveTo(start.X, start.Y),
                ChartPathCommand.CubicTo(a.X, a.Y, b.X, b.Y, c.X, c.Y), ChartPathCommand.CubicTo(d.X, d.Y, e.X, e.Y, f.X, f.Y),
                ChartPathCommand.LineTo(start.X, start.Y) }), true, true));
        } else if (node.Shape == TopologyNodeShape.ActorIcon) {
            var left = cx - 24 * s; var top = y + 8 * s; var r = 4 * s; const double q = 2d / 3;
            var right = left + 48 * s; var bottom = top + 48 * s;
            parts.Add(new ScenePart(new ChartPath(new[] { ChartPathCommand.MoveTo(left + r, top), ChartPathCommand.LineTo(right - r, top),
                ChartPathCommand.CubicTo(right - r + q * r, top, right, top + r - q * r, right, top + r),
                ChartPathCommand.LineTo(right, bottom - r), ChartPathCommand.CubicTo(right, bottom - r + q * r, right - r + q * r, bottom, right - r, bottom),
                ChartPathCommand.LineTo(left + r, bottom), ChartPathCommand.CubicTo(left + r - q * r, bottom, left, bottom - r + q * r, left, bottom - r),
                ChartPathCommand.LineTo(left, top + r), ChartPathCommand.CubicTo(left, top + r - q * r, left + r - q * r, top, left + r, top) }), true, true));
        } else {
            parts.Add(ActorEllipse(cx, y + 15 * s, 9 * s, 9 * s, true));
            parts.Add(ActorOpen(cx, y + 24 * s, cx, y + 50 * s));
            parts.Add(ActorOpen(cx - 17 * s, y + 34 * s, cx + 17 * s, y + 34 * s));
            parts.Add(ActorOpen(cx - 15 * s, y + 68 * s, cx, y + 50 * s, cx + 15 * s, y + 68 * s));
        }
        if (node.Shape is TopologyNodeShape.ActorBusiness or TopologyNodeShape.ActorBusinessHollow)
            parts.Add(ActorBusinessMarker(cx, y + 15 * s, 9 * s));
        return parts;
    }
    private static IReadOnlyList<ScenePart> BusinessEllipseParts(TopologyNode node) {
        var cx = node.X + node.Width / 2; var cy = node.Y + node.Height / 2; var rx = node.Width / 2; var ry = node.Height / 2;
        // A short inward chord leaves the caption's measured ellipse interior intact.
        var startX = rx * .65; var endX = rx * .9;
        return new[] { ActorEllipse(cx, cy, rx, ry, true), ActorOpen(cx + startX, cy + ry * Math.Sqrt(1 - .65 * .65),
            cx + endX, cy - ry * Math.Sqrt(1 - .9 * .9)) };
    }
    private static ScenePart ActorBusinessMarker(double cx, double cy, double radius) {
        var directionX = .5; var directionY = -Math.Sqrt(3) / 2; var offset = radius * .6; var chord = radius * .8;
        var x = cx - offset * directionY; var y = cy + offset * directionX;
        return ActorOpen(x - chord * directionX, y - chord * directionY, x + chord * directionX, y + chord * directionY);
    }
    private static ScenePart ActorPolygon(double cx, double y, double scale, double[] coordinates, bool fill) {
        var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(cx + coordinates[0] * scale, y + coordinates[1] * scale) };
        for (var i = 2; i < coordinates.Length; i += 2) commands.Add(ChartPathCommand.LineTo(cx + coordinates[i] * scale, y + coordinates[i + 1] * scale));
        return new ScenePart(new ChartPath(commands), true, fill);
    }
    private static ScenePart ActorOpen(params double[] points) {
        var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(points[0], points[1]) };
        for (var i = 2; i < points.Length; i += 2) commands.Add(ChartPathCommand.LineTo(points[i], points[i + 1]));
        return new ScenePart(new ChartPath(commands), false, false);
    }
    private static ScenePart ActorEllipse(double cx, double cy, double rx, double ry, bool fill) {
        const double k = 0.5522847498307936;
        return new ScenePart(new ChartPath(new[] { ChartPathCommand.MoveTo(cx, cy - ry),
            ChartPathCommand.CubicTo(cx + k * rx, cy - ry, cx + rx, cy - k * ry, cx + rx, cy),
            ChartPathCommand.CubicTo(cx + rx, cy + k * ry, cx + k * rx, cy + ry, cx, cy + ry),
            ChartPathCommand.CubicTo(cx - k * rx, cy + ry, cx - rx, cy + k * ry, cx - rx, cy),
            ChartPathCommand.CubicTo(cx - rx, cy - k * ry, cx - k * rx, cy - ry, cx, cy - ry) }), true, fill);
    }
}
