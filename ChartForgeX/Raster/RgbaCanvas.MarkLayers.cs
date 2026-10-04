using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    /// <summary>Applies element opacity once to a filled and stroked rectangle, including their overlap.</summary>
    internal void FillAndStrokeRoundedRect(double x, double y, double width, double height, double radius, ChartColor fill, ChartColor stroke, double thickness, double opacity) {
        if (!(width > 0) || !(height > 0) || !(opacity > 0)) return;
        if (opacity >= 1) {
            FillRoundedRect(x, y, width, height, radius, fill);
            StrokeRoundedRectCentered(x, y, width, height, radius, stroke, thickness);
            return;
        }
        var padding = Math.Max(0, thickness) / 2 + 1;
        var left = Math.Max(0, (int)Math.Floor((x - padding) * _scale));
        var top = Math.Max(0, (int)Math.Floor((y - padding) * _scale));
        var right = Math.Min(_pixelWidth, (int)Math.Ceiling((x + width + padding) * _scale));
        var bottom = Math.Min(_pixelHeight, (int)Math.Ceiling((y + height + padding) * _scale));
        if (right <= left || bottom <= top) return;
        // Only the clipped mark bounds are allocated. Coordinates and coverage stay at the parent resolution.
        var layer = new RgbaCanvas(right - left, bottom - top, 1, null, 1, useDefaultOutlineFont: false);
        layer.FillRoundedRect(x * _scale - left, y * _scale - top, width * _scale, height * _scale, radius * _scale, fill);
        layer.StrokeRoundedRectCentered(x * _scale - left, y * _scale - top, width * _scale, height * _scale, radius * _scale, stroke, thickness * _scale);
        var pixels = layer.Pixels;
        for (var yy = 0; yy < layer._pixelHeight; yy++) for (var xx = 0; xx < layer._pixelWidth; xx++) {
            var index = (yy * layer._pixelWidth + xx) * 4;
            if (pixels[index + 3] == 0) continue;
            BlendPixel(left + xx, top + yy, WithOpacity(ChartColor.FromRgba(pixels[index], pixels[index + 1], pixels[index + 2], pixels[index + 3]), opacity));
        }
    }
}
