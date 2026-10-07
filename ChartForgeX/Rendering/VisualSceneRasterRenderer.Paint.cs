using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneRasterRenderer {
    private static void PaintImage(RgbaCanvas canvas, VisualSceneImage node, VisualSceneTransform transform) {
        var origin = transform.Apply(new ChartPoint(node.Bounds.X, node.Bounds.Y));
        var scaleX = node.Bounds.Width / node.Image.Width; var scaleY = node.Bounds.Height / node.Image.Height;
        canvas.DrawImageTransformed(node.Image.Width, node.Image.Height, node.Image.Pixels,
            transform.A * scaleX, transform.B * scaleX, transform.C * scaleY, transform.D * scaleY, origin.X, origin.Y);
    }

    private static IDisposable? GroupClip(RgbaCanvas canvas, VisualSceneGroup group, VisualSceneTransform transform) {
        if (group.PathClip != null) return canvas.PushContoursClip(transform.Apply(VisualSceneGeometry.Flatten(group.PathClip, canvas.PixelsPerUnit)));
        if (!group.Clip.HasValue) return null;
        if (transform.IsIdentity) return canvas.PushClipBounds(group.Clip.Value);
        var bounds = group.Clip.Value;
        return canvas.PushContoursClip(transform.Apply(new List<List<ChartPoint>> { RectanglePoints(bounds, 0, canvas.PixelsPerUnit) }));
    }

    private static List<List<ChartPoint>> Contours(VisualSceneMark shape, double density) {
        if (shape is VisualScenePath path) return VisualSceneGeometry.Flatten(path, density);
        if (shape is VisualSceneSlice slice) return VisualSceneGeometry.Flatten(slice, density);
        if (shape is VisualSceneRectangle rect) return new List<List<ChartPoint>> { RectanglePoints(rect.Bounds, rect.Radius, density) };
        if (shape is VisualSceneEllipse ellipse) {
            var points = ChartCurveFlattening.Arc(0, 0, 1, 0, Math.PI * 2, density * Math.Max(ellipse.Rx, ellipse.Ry));
            for (var i = 0; i < points.Count; i++) points[i] = new ChartPoint(ellipse.Cx + points[i].X * ellipse.Rx, ellipse.Cy + points[i].Y * ellipse.Ry);
            if (points.Count > 0) points.Add(points[0]);
            return new List<List<ChartPoint>> { points };
        }
        if (shape is VisualSceneLine line) return new List<List<ChartPoint>> { new() { line.Start, line.End } };
        throw new NotSupportedException("Unsupported numeric scene shape.");
    }

    private static List<ChartPoint> RectanglePoints(ChartRect bounds, double radius, double density) =>
        ChartCurveFlattening.RoundedRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, radius, radius, density);

    private static void PaintGradient(RgbaCanvas canvas, VisualSceneGradient gradient, VisualSceneTransform transform) {
        var contours = transform.Apply(Contours(gradient.Shape, canvas.PixelsPerUnit));
        var stops = new RasterGradientStop[gradient.Stops.Count];
        for (var i = 0; i < stops.Length; i++) stops[i] = new RasterGradientStop(gradient.Stops[i].Offset, gradient.Stops[i].Color);
        canvas.FillContoursLinearGradient(contours, transform.Apply(gradient.Start), transform.Apply(gradient.End), stops, RasterGradientSpreadMethod.Pad);
        if (gradient.Shape.Stroke.HasValue && gradient.Shape.StrokeWidth > 0) {
            var lines = new List<IReadOnlyList<ChartPoint>>();
            foreach (var contour in contours) lines.Add(contour);
            canvas.StrokePolylines(lines, gradient.Shape.Stroke.Value, gradient.Shape.StrokeWidth, RasterLineCap.Round, RasterLineJoin.Round);
        }
    }

    private static void PaintTransformedText(RgbaCanvas canvas, VisualSceneText node, VisualSceneTransform transform) {
        var text = node.Text;
        // Retain shaped glyphs and faces. Only their ink is buffered for rigid rotation; no font lookup or reshaping occurs.
        var margin = Math.Max(4, text.Size);
        var left = node.Alignment == Typography.TextAlignment.Center ? node.X - text.Metrics.Width / 2
            : node.Alignment == Typography.TextAlignment.Right ? node.X - text.Metrics.Width : node.X;
        left -= margin;
        var top = node.Baseline - text.Ascent - margin;
        var width = Dimension(text.Metrics.Width + margin * 2);
        var height = Dimension(text.Metrics.Height + margin * 2);
        var density = Math.Max(1, canvas.PixelsPerUnit);
        var buffer = new RgbaCanvas(width, height, 1, null, density, useDefaultOutlineFont: false);
        PaintText(buffer, new VisualSceneText(text, node.X - left, node.Baseline - top, node.Color, node.Alignment, null, null));
        var image = buffer.ToImage();
        var origin = transform.Apply(new ChartPoint(left, top));
        canvas.DrawImageTransformed(image.Width, image.Height, image.Pixels,
            transform.A / density, transform.B / density, transform.C / density, transform.D / density, origin.X, origin.Y);
    }
}
