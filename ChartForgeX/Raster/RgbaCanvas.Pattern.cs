using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    internal void FillContoursPattern(IReadOnlyList<List<ChartPoint>> contours, double originX, double originY, int tileWidth, int tileHeight, byte[] tilePixels, RasterFillRule fillRule = RasterFillRule.EvenOdd) {
        if (contours.Count == 0 || tileWidth <= 0 || tileHeight <= 0 || tilePixels.Length < tileWidth * tileHeight * 4) return;
        var scaledContours = new List<List<ChartPoint>>(contours.Count);
        foreach (var contour in contours) {
            if (contour.Count < 3) continue;
            var scaled = new List<ChartPoint>(contour.Count);
            foreach (var point in contour) scaled.Add(new ChartPoint(point.X * _scale, point.Y * _scale));
            scaledContours.Add(scaled);
        }

        if (scaledContours.Count == 0) return;
        FillContoursPatternPixels(scaledContours, originX * _scale, originY * _scale, tileWidth, tileHeight, tilePixels, fillRule);
    }

    private void FillContoursPatternPixels(IReadOnlyList<List<ChartPoint>> contours, double originX, double originY, int tileWidth, int tileHeight, byte[] tilePixels, RasterFillRule fillRule) {
        ScanFillSpans(contours, fillRule, (y, scanY, left, right) => {
            var xStart = Math.Max(0, (int)Math.Floor(left));
            var xEnd = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(right));
            for (var x = xStart; x <= xEnd; x++) {
                var coverage = Math.Min(x + 1.0, right) - Math.Max(x, left);
                if (coverage <= 0) continue;
                var color = SamplePattern(tilePixels, tileWidth, tileHeight, x + 0.5 - originX, scanY - originY);
                if (color.A == 0) continue;
                BlendPixel(x, y, coverage >= 1 ? color : WithOpacity(color, coverage));
            }
        });
    }

    private static ChartColor SamplePattern(byte[] tilePixels, int tileWidth, int tileHeight, double x, double y) {
        var tileX = PositiveModulo((int)Math.Floor(x), tileWidth);
        var tileY = PositiveModulo((int)Math.Floor(y), tileHeight);
        var index = (tileY * tileWidth + tileX) * 4;
        return ChartColor.FromRgba(tilePixels[index], tilePixels[index + 1], tilePixels[index + 2], tilePixels[index + 3]);
    }

    private static int PositiveModulo(int value, int modulus) {
        var result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}
