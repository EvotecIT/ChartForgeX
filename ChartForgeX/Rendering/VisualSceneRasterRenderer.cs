using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Paints compiled scene decisions through the existing dependency-free raster engine.</summary>
internal static partial class VisualSceneRasterRenderer {
    internal static RgbaImage Render(VisualScene scene, int scale = 1, int supersampling = 2, long pixelBudget = 64000000) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        if (scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        if (supersampling <= 0) throw new ArgumentOutOfRangeException(nameof(supersampling));
        if (pixelBudget <= 0) throw new ArgumentOutOfRangeException(nameof(pixelBudget));
        var width = Dimension(scene.Size.Width * scale); var height = Dimension(scene.Size.Height * scale);
        var allocation = RasterAllocationGuard.Calculate(width, height, supersampling, 1);
        if ((long)allocation.PixelWidth * allocation.PixelHeight > pixelBudget)
            throw new ArgumentOutOfRangeException(nameof(pixelBudget), "Prepared visual exceeds the configured raster pixel budget, including supersampling.");
        // Fonts are already retained by each text run. Do not resolve a canvas default font at export.
        var canvas = new RgbaCanvas(scene.Size.Width, scene.Size.Height, supersampling, null, scale, useDefaultOutlineFont: false);
        var groups = new Stack<IDisposable?>();
        var transforms = new Stack<VisualSceneTransform>();
        var transform = VisualSceneTransform.Identity;
        try {
            for (var nodeIndex = 0; nodeIndex < scene.Nodes.Count; nodeIndex++) {
                var node = scene.Nodes[nodeIndex];
                if (node is VisualSceneGroup group) {
                    transforms.Push(transform);
                    if (group.Rotation.HasValue) transform = transform.Rotate(group.Rotation.Value);
                    if (group.Translation.HasValue) transform = transform.Translate(group.Translation.Value);
                    groups.Push(GroupClip(canvas, group, transform));
                }
                else if (node is VisualSceneEndGroup) { groups.Pop()?.Dispose(); transform = transforms.Pop(); }
                else if (node is VisualSceneImage image) PaintImage(canvas, image, transform);
                else if (node is VisualSceneGradient gradient) PaintGradient(canvas, gradient, transform);
                else if (!transform.IsIdentity && node is VisualSceneMark transformedMark) PaintContours(canvas, transform.Apply(Contours(transformedMark, canvas.PixelsPerUnit)), transformedMark);
                else if (node is VisualSceneRectangle rect) {
                    var b = rect.Bounds;
                    // Only an un-clipped first opaque viewport paint can replace contour blending.
                    if (nodeIndex == 0 && !rect.Stroke.HasValue && rect.Radius == 0 &&
                        rect.Fill.HasValue && rect.Fill.Value.A == 255 && b.X == 0 && b.Y == 0 &&
                        scene.Size.Width == canvas.Width && scene.Size.Height == canvas.Height && b.Width == canvas.Width && b.Height == canvas.Height) {
                        canvas.Clear(rect.Fill.Value);
                        continue;
                    }
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
                else if (node is VisualSceneText text) {
                    if (transform.IsIdentity) PaintText(canvas, text);
                    else PaintTransformedText(canvas, text, transform);
                }
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
        StrokeContours(canvas, contours, mark);
    }

    private static void StrokeContours(RgbaCanvas canvas, List<List<ChartPoint>> contours, VisualSceneMark mark) {
        if (mark.Stroke.HasValue && mark.StrokeWidth > 0) {
            var lines = new List<IReadOnlyList<ChartPoint>>(contours.Count);
            foreach (var contour in contours) lines.Add(contour);
            canvas.StrokePolylines(lines, mark.Stroke.Value, mark.StrokeWidth,
                mark is VisualScenePath path ? (RasterLineCap)path.Cap : RasterLineCap.Round,
                mark is VisualScenePath joined ? (RasterLineJoin)joined.Join : RasterLineJoin.Round,
                mark is VisualSceneLine line ? line.Dash : mark is VisualScenePath dashed ? dashed.Dash : null);
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
