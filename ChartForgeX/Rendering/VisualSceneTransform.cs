using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>A rigid logical-space transform; rotation composes in scene group order.</summary>
internal readonly struct VisualSceneTransform {
    internal static VisualSceneTransform Identity => new(1, 0, 0, 1, 0, 0);
    private VisualSceneTransform(double a, double b, double c, double d, double e, double f) {
        A = a; B = b; C = c; D = d; E = e; F = f;
    }
    internal double A { get; }
    internal double B { get; }
    internal double C { get; }
    internal double D { get; }
    internal double E { get; }
    internal double F { get; }
    internal bool IsIdentity => A == 1 && B == 0 && C == 0 && D == 1 && E == 0 && F == 0;
    internal ChartPoint Apply(ChartPoint point) => new(A * point.X + C * point.Y + E, B * point.X + D * point.Y + F);
    internal VisualSceneTransform Translate(ChartPoint offset) => new(A, B, C, D,
        A * offset.X + C * offset.Y + E, B * offset.X + D * offset.Y + F);
    internal VisualSceneTransform Rotate(VisualRotation rotation) {
        var angle = rotation.Degrees * Math.PI / 180;
        var cos = Math.Cos(angle); var sin = Math.Sin(angle);
        var x = rotation.X * (1 - cos) + rotation.Y * sin;
        var y = rotation.Y * (1 - cos) - rotation.X * sin;
        return new VisualSceneTransform(A * cos + C * sin, B * cos + D * sin,
            -A * sin + C * cos, -B * sin + D * cos, A * x + C * y + E, B * x + D * y + F);
    }
    internal List<List<ChartPoint>> Apply(List<List<ChartPoint>> contours) {
        if (IsIdentity) return contours;
        foreach (var contour in contours) for (var i = 0; i < contour.Count; i++) contour[i] = Apply(contour[i]);
        return contours;
    }
}
