using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Shared straight-leader clearance in the same logical coordinates as labels and marks.</summary>
internal static class LabelLeaderGeometry {
    internal static double? IntersectionRatio(ChartPoint a, ChartPoint b, ChartPoint c, ChartPoint d) {
        var x = b.X - a.X; var y = b.Y - a.Y; var u = d.X - c.X; var v = d.Y - c.Y;
        var cross = x * v - y * u;
        if (Math.Abs(cross) < .00000001) return null;
        var t = ((c.X - a.X) * v - (c.Y - a.Y) * u) / cross;
        var s = ((c.X - a.X) * y - (c.Y - a.Y) * x) / cross;
        return t > .000001 && t <= 1 && s >= 0 && s <= 1 ? t : null;
    }
    internal static bool Intersects(ChartPoint a, ChartPoint b, ChartPoint c, ChartPoint d, double clearance = 1) {
        double Cross(ChartPoint p, ChartPoint q, ChartPoint r) => (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
        var abC = Cross(a, b, c); var abD = Cross(a, b, d);
        var cdA = Cross(c, d, a); var cdB = Cross(c, d, b);
        if ((abC > 0 && abD < 0 || abC < 0 && abD > 0) && (cdA > 0 && cdB < 0 || cdA < 0 && cdB > 0)) return true;
        var squared = clearance * clearance;
        return DistanceSquared(a, c, d) <= squared || DistanceSquared(b, c, d) <= squared
            || DistanceSquared(c, a, b) <= squared || DistanceSquared(d, a, b) <= squared;
    }

    private static double DistanceSquared(ChartPoint point, ChartPoint a, ChartPoint b) {
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        var length = dx * dx + dy * dy;
        var t = length <= 0 ? 0 : Math.Max(0, Math.Min(1, ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / length));
        var x = point.X - a.X - dx * t; var y = point.Y - a.Y - dy * t;
        return x * x + y * y;
    }
}
