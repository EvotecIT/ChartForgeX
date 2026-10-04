using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Preserves linear-gradient colour planes when gradient coordinates are scaled or sheared.</summary>
internal static class ChartLinearGradientGeometry {
    internal static void Transform(ChartPoint start, ChartPoint end, ChartPoint origin, ChartPoint axisX, ChartPoint axisY, out ChartPoint mappedStart, out ChartPoint mappedEnd) {
        var a = axisX.X - origin.X;
        var b = axisX.Y - origin.Y;
        var c = axisY.X - origin.X;
        var d = axisY.Y - origin.Y;
        mappedStart = new ChartPoint(origin.X + a * start.X + c * start.Y, origin.Y + b * start.X + d * start.Y);
        mappedEnd = mappedStart;
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var lengthSquared = dx * dx + dy * dy;
        var determinant = a * d - b * c;
        if (lengthSquared <= 0.000000001 || Math.Abs(determinant) <= 0.000000001) return;
        // A gradient's normals transform with its geometry. Mapping only its endpoints would
        // incorrectly keep those normals perpendicular after non-uniform scaling or a shear.
        var normalX = (d * dx - b * dy) / (determinant * lengthSquared);
        var normalY = (a * dy - c * dx) / (determinant * lengthSquared);
        var normalLengthSquared = normalX * normalX + normalY * normalY;
        if (!(normalLengthSquared > 0)) return;
        mappedEnd = new ChartPoint(mappedStart.X + normalX / normalLengthSquared, mappedStart.Y + normalY / normalLengthSquared);
    }
}
