using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    private bool _hasClip;
    private int _clipLeft;
    private int _clipTop;
    private int _clipRight;
    private int _clipBottom;

    /// <summary>
    /// Restricts subsequent paint operations to pixels whose centers fall inside the supplied logical bounds.
    /// </summary>
    internal void SetClipBounds(double x, double y, double width, double height) {
        if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y) ||
            double.IsNaN(width) || double.IsInfinity(width) || double.IsNaN(height) || double.IsInfinity(height) ||
            width < 0 || height < 0) {
            throw new ArgumentOutOfRangeException(nameof(width), "Clip bounds must be finite and non-negative.");
        }

        _clipLeft = Math.Max(0, (int)Math.Ceiling(x * _scale - 0.5));
        _clipTop = Math.Max(0, (int)Math.Ceiling(y * _scale - 0.5));
        _clipRight = Math.Min(_pixelWidth, (int)Math.Ceiling((x + width) * _scale - 0.5));
        _clipBottom = Math.Min(_pixelHeight, (int)Math.Ceiling((y + height) * _scale - 0.5));
        _hasClip = true;
    }

    private bool IsInsideClip(int x, int y) => !_hasClip || x >= _clipLeft && x < _clipRight && y >= _clipTop && y < _clipBottom;

    public void FillRectClippedToRoundedRect(double x, double y, double width, double height, double clipX, double clipY, double clipWidth, double clipHeight, double clipRadius, ChartColor color) {
        FillRectClippedToRoundedRectPixels(x * _scale, y * _scale, width * _scale, height * _scale, clipX * _scale, clipY * _scale, clipWidth * _scale, clipHeight * _scale, clipRadius * _scale, color);
    }

    private void FillRectClippedToRoundedRectPixels(double x, double y, double width, double height, double clipX, double clipY, double clipWidth, double clipHeight, double clipRadius, ChartColor color) {
        if (width <= 0 || height <= 0 || clipWidth <= 0 || clipHeight <= 0 || color.A == 0) return;
        var feather = 1.0;
        var left = Math.Max(x, clipX);
        var top = Math.Max(y, clipY);
        var right = Math.Min(x + width, clipX + clipWidth);
        var bottom = Math.Min(y + height, clipY + clipHeight);
        var x1 = Math.Max(0, (int)Math.Floor(left - feather));
        var y1 = Math.Max(0, (int)Math.Floor(top - feather));
        var x2 = Math.Min(_pixelWidth, (int)Math.Ceiling(right + feather));
        var y2 = Math.Min(_pixelHeight, (int)Math.Ceiling(bottom + feather));
        for (var yy = y1; yy < y2; yy++) for (var xx = x1; xx < x2; xx++) {
            var px = xx + 0.5;
            var py = yy + 0.5;
            if (px < x || py < y || px >= x + width || py >= y + height) continue;
            var distance = RoundedRectSignedDistance(px, py, clipX, clipY, clipWidth, clipHeight, clipRadius);
            if (distance <= 0) {
                BlendPixel(xx, yy, color);
            } else if (distance < feather) {
                BlendPixel(xx, yy, WithOpacity(color, feather - distance));
            }
        }
    }
}
