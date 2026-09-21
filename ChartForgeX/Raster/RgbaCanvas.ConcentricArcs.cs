using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    /// <summary>
    /// Paints uniformly spaced concentric progress arcs in one bounded annulus pass.
    /// The work scales with the output pixels in the ring band instead of rescanning
    /// the full bounding square once or twice for every ring.
    /// </summary>
    internal void DrawConcentricArcs(
        double cx,
        double cy,
        double firstRadius,
        double radiusStep,
        double startAngle,
        IReadOnlyList<double> ratios,
        IReadOnlyList<ChartColor> colors,
        ChartColor trackColor,
        double thickness) {
        if (ratios == null) throw new ArgumentNullException(nameof(ratios));
        if (colors == null) throw new ArgumentNullException(nameof(colors));
        if (ratios.Count != colors.Count) throw new ArgumentException("Concentric arc ratios and colors must have the same count.");
        if (ratios.Count == 0 || firstRadius <= 0 || radiusStep <= 0 || thickness <= 0) return;

        var pixelCx = cx * _scale;
        var pixelCy = cy * _scale;
        var pixelFirstRadius = firstRadius * _scale;
        var pixelRadiusStep = radiusStep * _scale;
        var strokeRadius = Math.Max(0.5, thickness * _scale / 2.0);
        var feather = 1.0;
        var outer = pixelFirstRadius + strokeRadius + feather;
        var inner = Math.Max(0, pixelFirstRadius - (ratios.Count - 1) * pixelRadiusStep - strokeRadius - feather);
        var x1 = Math.Max(0, (int)Math.Floor(pixelCx - outer));
        var y1 = Math.Max(0, (int)Math.Floor(pixelCy - outer));
        var x2 = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(pixelCx + outer));
        var y2 = Math.Min(_pixelHeight - 1, (int)Math.Ceiling(pixelCy + outer));
        var normalizedStart = NormalizeAngle(startAngle);

        for (var y = y1; y <= y2; y++) for (var x = x1; x <= x2; x++) {
            var dx = x + 0.5 - pixelCx;
            var dy = y + 0.5 - pixelCy;
            var distanceFromCenter = Math.Sqrt(dx * dx + dy * dy);
            if (distanceFromCenter < inner || distanceFromCenter > outer) continue;

            var ringPosition = (pixelFirstRadius - distanceFromCenter) / pixelRadiusStep;
            var ringIndex = (int)Math.Floor(ringPosition + 0.5);
            if (ringIndex < 0 || ringIndex >= ratios.Count) continue;

            var ringRadius = pixelFirstRadius - ringIndex * pixelRadiusStep;
            var distance = Math.Abs(distanceFromCenter - ringRadius);
            if (distance >= strokeRadius + feather) continue;

            var ratio = Math.Max(0, Math.Min(1, ratios[ringIndex]));
            var progress = ratio >= 1 - 0.000001;
            if (!progress && ratio > 0) {
                var end = NormalizeAngle(startAngle + Math.PI * 2 * ratio);
                progress = AngleInArc(Math.Atan2(dy, dx), normalizedStart, end);
            }

            var color = progress ? colors[ringIndex] : trackColor;
            var coverage = distance <= strokeRadius ? 1 : strokeRadius + feather - distance;
            BlendPixel(x, y, coverage >= 1 ? color : WithOpacity(color, coverage));
        }

        // Restore the rounded progress caps without turning the dense path back into
        // a radius-squared pass per ring. Each cap touches only its local stroke box.
        for (var index = 0; index < ratios.Count; index++) {
            var ratio = Math.Max(0, Math.Min(1, ratios[index]));
            if (ratio <= 0 || ratio >= 1 - 0.000001) continue;
            var radius = pixelFirstRadius - index * pixelRadiusStep;
            var end = startAngle + Math.PI * 2 * ratio;
            DrawSoftCirclePixels(pixelCx + Math.Cos(startAngle) * radius, pixelCy + Math.Sin(startAngle) * radius, strokeRadius, colors[index]);
            DrawSoftCirclePixels(pixelCx + Math.Cos(end) * radius, pixelCy + Math.Sin(end) * radius, strokeRadius, colors[index]);
        }
    }
}
