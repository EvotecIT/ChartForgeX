using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Paints compiled scene decisions through the existing dependency-free raster engine.</summary>
internal static class VisualSceneRasterRenderer {
    internal static RgbaImage Render(VisualScene scene, int scale = 1, int supersampling = 2, long pixelBudget = 64000000) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        if (scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        if (supersampling <= 0) throw new ArgumentOutOfRangeException(nameof(supersampling));
        if (pixelBudget <= 0) throw new ArgumentOutOfRangeException(nameof(pixelBudget));
        var width = Dimension(scene.Size.Width); var height = Dimension(scene.Size.Height);
        var allocation = RasterAllocationGuard.Calculate(width, height, supersampling, scale);
        if ((long)allocation.PixelWidth * allocation.PixelHeight > pixelBudget)
            throw new ArgumentOutOfRangeException(nameof(pixelBudget), "Prepared visual exceeds the configured raster pixel budget, including supersampling.");
        // Fonts are already retained by each text run. Do not resolve a canvas default font at export.
        var canvas = new RgbaCanvas(width, height, supersampling, null, scale, useDefaultOutlineFont: false);
        var groups = new Stack<IDisposable?>();
        try {
            foreach (var node in scene.Nodes) {
                if (node is VisualSceneGroup group) groups.Push(group.Clip.HasValue ? canvas.PushClipBounds(group.Clip.Value) : null);
                else if (node is VisualSceneEndGroup) groups.Pop()?.Dispose();
                else if (node is VisualSceneRectangle rect) {
                    var b = rect.Bounds;
                    if (rect.Fill.HasValue) canvas.FillRoundedRect(b.X, b.Y, b.Width, b.Height, rect.Radius, rect.Fill.Value);
                    if (rect.Stroke.HasValue && rect.StrokeWidth > 0) canvas.StrokeRoundedRectCentered(b.X, b.Y, b.Width, b.Height, rect.Radius, rect.Stroke.Value, rect.StrokeWidth);
                } else if (node is VisualSceneEllipse ellipse) {
                    if (ellipse.Fill.HasValue) canvas.FillEllipse(ellipse.Cx, ellipse.Cy, ellipse.Rx, ellipse.Ry, ellipse.Fill.Value);
                    if (ellipse.Stroke.HasValue && ellipse.StrokeWidth > 0) canvas.StrokeEllipse(ellipse.Cx, ellipse.Cy, ellipse.Rx, ellipse.Ry, ellipse.Stroke.Value, ellipse.StrokeWidth);
                } else if (node is VisualSceneLine line) {
                    if (line.StrokeWidth > 0) canvas.StrokePolylines(new IReadOnlyList<ChartPoint>[] { new[] { line.Start, line.End } },
                        line.Stroke!.Value, line.StrokeWidth, RasterLineCap.Round, RasterLineJoin.Round, line.Dash);
                } else if (node is VisualScenePath path) PaintContours(canvas, VisualSceneGeometry.Flatten(path, canvas.PixelsPerUnit), path);
                else if (node is VisualSceneSlice slice) PaintContours(canvas, VisualSceneGeometry.Flatten(slice, canvas.PixelsPerUnit), slice);
                else if (node is VisualSceneText text) PaintText(canvas, text);
            }
        } finally {
            while (groups.Count > 0) groups.Pop()?.Dispose();
        }
        return canvas.ToImage();
    }

    private static int Dimension(double value) {
        if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value) || value > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), "Raster dimensions must be finite positive integers after rounding.");
        return (int)Math.Ceiling(value);
    }

    private static void PaintContours(RgbaCanvas canvas, List<List<ChartPoint>> contours, VisualSceneMark mark) {
        if (mark.Fill.HasValue) canvas.FillContours(contours, mark.Fill.Value, RasterFillRule.EvenOdd);
        if (mark.Stroke.HasValue && mark.StrokeWidth > 0) {
            var lines = new List<IReadOnlyList<ChartPoint>>(contours.Count);
            foreach (var contour in contours) lines.Add(contour);
            canvas.StrokePolylines(lines, mark.Stroke.Value, mark.StrokeWidth, RasterLineCap.Round, RasterLineJoin.Round);
        }
    }

    private static void PaintText(RgbaCanvas canvas, VisualSceneText node) {
        var prepared = node.Text;
        var oldHinting = canvas.TextHinting; canvas.TextHinting = prepared.Style.Hinting;
        try {
            for (var i = 0; i < prepared.Lines.Count; i++) {
                var line = prepared.Lines[i];
                var y = node.Baseline + i * prepared.Metrics.LineHeight - prepared.Ascent;
                var boldOffset = prepared.Face.SynthesizeBold ? RgbaCanvas.EmphasisOffset(prepared.Size) : 0;
                if (prepared.Face.Font != null) prepared.Face.Font.DrawGlyphs(canvas, node.LineLeft(line), y, line.Glyphs,
                    node.Color, prepared.Size, prepared.Face.SynthesizeItalic, boldOffset);
                else canvas.DrawTextTiny(node.LineLeft(line), y, line.Text, node.Color,
                    VisualSceneTextFace.FallbackScale(prepared.Size), prepared.Face.SynthesizeItalic, boldOffset);
            }
        } finally { canvas.TextHinting = oldHinting; }
    }
}
