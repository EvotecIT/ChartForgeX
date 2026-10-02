using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    /// <summary>
    /// Strokes a rounded rectangle with a dash pattern, inside its bounds as <see cref="StrokeRoundedRect"/> does. Each
    /// pixel is covered once, so a translucent colour stays even at the corners. The dashes start after the top-left
    /// corner of the stroke's centre line and run clockwise, where an SVG <c>rect</c> with the same centre line starts
    /// its <c>stroke-dasharray</c>.
    /// </summary>
    /// <param name="x">Left edge of the outer bounds.</param>
    /// <param name="y">Top edge of the outer bounds.</param>
    /// <param name="width">Width of the outer bounds.</param>
    /// <param name="height">Height of the outer bounds.</param>
    /// <param name="radius">Outer corner radius; the centre line uses it less half the thickness.</param>
    /// <param name="color">Stroke colour.</param>
    /// <param name="thickness">Stroke width.</param>
    /// <param name="dash">Dash length along the centre line.</param>
    /// <param name="gap">Gap length along the centre line.</param>
    public void StrokeRoundedRectDashed(double x, double y, double width, double height, double radius, ChartColor color, double thickness, double dash, double gap) {
        if (dash <= 0 || gap <= 0) {
            StrokeRoundedRect(x, y, width, height, radius, color, thickness);
            return;
        }

        StrokeRoundedRectContours(x, y, width, height, radius, color, thickness, new[] { dash, gap });
    }
}