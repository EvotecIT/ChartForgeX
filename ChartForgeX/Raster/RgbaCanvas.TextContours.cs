using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    /// <summary>Paints captured built-in text stroke ink through the incumbent stroke coverage policy.</summary>
    internal void FillCapturedTextStrokeContours(IReadOnlyList<List<ChartPoint>> contours, ChartColor color) =>
        FillContours(contours, color, RasterFillRule.NonZero, StrokeSubScanlines);

    internal void FillTextContours(IReadOnlyList<List<ChartPoint>> contours, ChartColor color, double boldOffset) {
        if (boldOffset == 0) { FillContours(contours, color, RasterFillRule.NonZero); return; }
        var scaled = new List<List<ChartPoint>>(contours.Count * 2);
        foreach (List<ChartPoint> contour in contours) {
            var points = new List<ChartPoint>(contour.Count);
            foreach (ChartPoint point in contour) points.Add(new ChartPoint(point.X * _scale, point.Y * _scale));
            scaled.Add(points);
        }
        int second = scaled.Count;
        foreach (List<ChartPoint> contour in contours) {
            var points = new List<ChartPoint>(contour.Count);
            foreach (ChartPoint point in contour) points.Add(new ChartPoint((point.X + boldOffset) * _scale, point.Y * _scale));
            scaled.Add(points);
        }
        FillContoursPixels(scaled, color, RasterFillRule.NonZero, secondContourStart: second);
    }
}
