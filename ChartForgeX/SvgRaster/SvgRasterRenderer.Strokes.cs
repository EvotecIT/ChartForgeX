using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    private static void Stroke(RgbaCanvas canvas, IReadOnlyList<List<ChartPoint>> contours, SvgRasterStyle style,
        SvgRasterMatrix matrix, SvgRasterDefinitions definitions, SvgRasterViewport viewport) {
        if (style.Stroke.IsNone || style.StrokeWidth <= 0 || contours.Count == 0 || !matrix.TryInvert(out var inverse)) return;
        var local = new List<IReadOnlyList<ChartPoint>>(contours.Count);
        var objectContours = new List<List<ChartPoint>>(contours.Count);
        foreach (var contour in contours) {
            var ring = TransformRing(contour, inverse);
            local.Add(ring); objectContours.Add(ring);
        }
        double scale = canvas.PixelsPerUnit * Math.Sqrt(matrix.A*matrix.A + matrix.B*matrix.B + matrix.C*matrix.C + matrix.D*matrix.D);
        var fill = style.Inherit();
        fill.Fill = style.Stroke; fill.FillOpacity = style.StrokeOpacity; fill.Opacity = style.Opacity; fill.FillRule = "nonzero";
        var objectPaint = new SvgRasterObjectPaint(SvgRasterGradientValues.Bounds(objectContours),matrix);
        canvas.PaintStrokeTiles(style.StrokeDashArray != null && style.StrokeDashArray.Count > 0, (bounds, budget) => {
            var corners = new List<List<ChartPoint>> { new List<ChartPoint> {
                inverse.Transform(new ChartPoint(bounds.Left,bounds.Top)),inverse.Transform(new ChartPoint(bounds.Right,bounds.Top)),
                inverse.Transform(new ChartPoint(bounds.Left,bounds.Bottom)),inverse.Transform(new ChartPoint(bounds.Right,bounds.Bottom)) } };
            var clip = SvgRasterGradientValues.Bounds(corners);
            var outlines = RasterStroker.CreateOutline(local, style.StrokeWidth, LineCap(style.StrokeLineCap), LineJoin(style.StrokeLineJoin),
                style.StrokeMiterLimit, style.StrokeDashArray, style.StrokeDashOffset, scale, clip.Left, clip.Top, clip.Left+clip.Width, clip.Top+clip.Height, budget);
            for (int i=0;i<outlines.Count;i++) outlines[i] = TransformRing(outlines[i],matrix);
            Fill(canvas,outlines,fill,matrix,definitions,viewport,objectPaint);
        });
    }
}
