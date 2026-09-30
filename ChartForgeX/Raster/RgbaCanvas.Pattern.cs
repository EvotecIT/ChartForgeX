using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    internal void FillContoursPattern(IReadOnlyList<List<ChartPoint>> contours, int tileWidth, int tileHeight, byte[] tilePixels, double tileA, double tileB, double tileC, double tileD, double tileE, double tileF, RasterFillRule fillRule = RasterFillRule.EvenOdd, double opacity = 1) {
        if (contours.Count == 0 || tileWidth <= 0 || tileHeight <= 0 || tilePixels.Length < tileWidth * tileHeight * 4) return;
        var scaledContours = new List<List<ChartPoint>>(contours.Count);
        foreach (var contour in contours) {
            if (contour.Count < 3) continue;
            var scaled = new List<ChartPoint>(contour.Count);
            foreach (var point in contour) scaled.Add(new ChartPoint(point.X * _scale, point.Y * _scale));
            scaledContours.Add(scaled);
        }

        if (scaledContours.Count == 0) return;
        FillContoursPatternPixels(scaledContours, tileWidth, tileHeight, tilePixels, tileA, tileB, tileC, tileD, tileE, tileF, fillRule, opacity);
    }

    private void FillContoursPatternPixels(IReadOnlyList<List<ChartPoint>> contours, int tileWidth, int tileHeight, byte[] tilePixels, double tileA, double tileB, double tileC, double tileD, double tileE, double tileF, RasterFillRule fillRule, double opacity) {
        ScanFillCoverage(contours, fillRule, (y, xStart, xEnd, rowCoverage) => {
            var logicalY = (y + 0.5) / _scale;
            for (var x = xStart; x <= xEnd; x++) {
                var coverage = rowCoverage[x];
                if (coverage <= 0) continue;
                var logicalX = (x + 0.5) / _scale;
                var sampleX = logicalX * tileA + logicalY * tileC + tileE;
                var sampleY = logicalX * tileB + logicalY * tileD + tileF;
                var color = WithOpacity(SamplePattern(tilePixels, tileWidth, tileHeight, sampleX, sampleY), opacity);
                if (color.A == 0) continue;
                BlendPixel(x, y, coverage >= FullCoverage ? color : WithOpacity(color, coverage));
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
