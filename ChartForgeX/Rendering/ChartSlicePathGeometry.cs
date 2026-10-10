using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>The closed slice outlines shared by SVG paths and raster separator strokes.</summary>
internal static class ChartSlicePathGeometry {
    /// <summary>Whether the serialized outline retains fill area, independent of colour or raster pixel coverage.</summary>
    internal static bool HasEncodedFillArea(double cx, double cy, double outer, double inner, double start, double sweep) {
        if (outer <= inner || sweep <= 0 || F(outer) == "0") return false;
        if (sweep >= Math.PI * 2 - 0.000001) {
            if (F(cx - outer) == F(cx + outer)) return false;
            return inner <= 0 || F(outer) != F(inner)
                || F(cx - outer) != F(cx - inner) || F(cx + outer) != F(cx + inner);
        }
        var end = start + sweep;
        // SVG omits an arc with identical encoded endpoints. Two omitted arcs leave only a retraced radial line.
        if (SamePoint(outer, start, outer, end) && (inner <= 0 || SamePoint(inner, start, inner, end))) return false;
        // Equal radii and equal endpoint pairs encode the same annular contour in opposite directions.
        return inner <= 0 || F(outer) != F(inner) || !SamePoint(outer, start, inner, start) || !SamePoint(outer, end, inner, end);

        bool SamePoint(double firstRadius, double firstAngle, double secondRadius, double secondAngle) =>
            F(cx + Math.Cos(firstAngle) * firstRadius) == F(cx + Math.Cos(secondAngle) * secondRadius)
            && F(cy + Math.Sin(firstAngle) * firstRadius) == F(cy + Math.Sin(secondAngle) * secondRadius);
    }

    /// <summary>Gets the actual annular sector envelope for semantic regions and label placement.</summary>
    internal static ChartRect Bounds(double cx, double cy, double outer, double inner, double start, double sweep) {
        var points = new List<ChartPoint>();
        Add(start); Add(start + sweep);
        for (var index = 0; index < 4; index++) {
            var angle = index * Math.PI / 2;
            angle += Math.Ceiling((start - angle) / (Math.PI * 2)) * Math.PI * 2;
            if (angle <= start + sweep) Add(angle);
        }
        var left = points.Min(point => point.X); var top = points.Min(point => point.Y);
        return new ChartRect(left, top, points.Max(point => point.X) - left, points.Max(point => point.Y) - top);

        void Add(double angle) {
            points.Add(new ChartPoint(cx + Math.Cos(angle) * outer, cy + Math.Sin(angle) * outer));
            points.Add(new ChartPoint(cx + Math.Cos(angle) * inner, cy + Math.Sin(angle) * inner));
        }
    }

    public static string BuildPath(double cx, double cy, double radius, double innerRadius, double start, double end) {
        if (end - start >= Math.PI * 2 - 0.000001) {
            return BuildFullSlicePath(cx, cy, radius, innerRadius);
        }

        var largeArc = end - start > Math.PI ? 1 : 0;
        var x1 = cx + Math.Cos(start) * radius;
        var y1 = cy + Math.Sin(start) * radius;
        var x2 = cx + Math.Cos(end) * radius;
        var y2 = cy + Math.Sin(end) * radius;

        if (innerRadius <= 0) {
            return $"M {F(cx)} {F(cy)} L {F(x1)} {F(y1)} A {F(radius)} {F(radius)} 0 {largeArc} 1 {F(x2)} {F(y2)} Z";
        }

        var ix1 = cx + Math.Cos(start) * innerRadius;
        var iy1 = cy + Math.Sin(start) * innerRadius;
        var ix2 = cx + Math.Cos(end) * innerRadius;
        var iy2 = cy + Math.Sin(end) * innerRadius;
        return $"M {F(x1)} {F(y1)} A {F(radius)} {F(radius)} 0 {largeArc} 1 {F(x2)} {F(y2)} L {F(ix2)} {F(iy2)} A {F(innerRadius)} {F(innerRadius)} 0 {largeArc} 0 {F(ix1)} {F(iy1)} Z";
    }

    private static string BuildFullSlicePath(double cx, double cy, double radius, double innerRadius) {
        var left = cx - radius;
        var right = cx + radius;
        if (innerRadius <= 0) {
            return $"M {F(cx)} {F(cy)} m {F(-radius)} 0 A {F(radius)} {F(radius)} 0 1 1 {F(right)} {F(cy)} A {F(radius)} {F(radius)} 0 1 1 {F(left)} {F(cy)} Z";
        }

        var innerLeft = cx - innerRadius;
        var innerRight = cx + innerRadius;
        return $"M {F(left)} {F(cy)} A {F(radius)} {F(radius)} 0 1 1 {F(right)} {F(cy)} A {F(radius)} {F(radius)} 0 1 1 {F(left)} {F(cy)} M {F(innerLeft)} {F(cy)} A {F(innerRadius)} {F(innerRadius)} 0 1 0 {F(innerRight)} {F(cy)} A {F(innerRadius)} {F(innerRadius)} 0 1 0 {F(innerLeft)} {F(cy)} Z";
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
