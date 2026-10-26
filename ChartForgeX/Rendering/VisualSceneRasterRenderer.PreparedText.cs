using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneRasterRenderer {
    /// <summary>Places a thin producer's flat prepared text/decorations through the incumbent native node painter.</summary>
    internal static void PaintPreparedTextScene(RgbaCanvas canvas, VisualScene scene, double x, double y) {
        foreach (var node in scene.Nodes) if (node is not VisualSceneText && node is not VisualSceneMark)
            throw new ArgumentException("Prepared text placement requires flat text/decorations.", nameof(scene));
        var transform = VisualSceneTransform.Identity.Translate(new ChartPoint(x, y));
        for (var index = 0; index < scene.Nodes.Count; index++) {
            if (scene.Nodes[index] is VisualSceneText text) {
                // Translation changes the common node origin, without introducing an intermediate text canvas.
                var placed = new VisualSceneText(text.Text, text.X + x, text.Baseline + y, text.Color, text.Alignment,
                    text.Role, text.Id, text.Paint, text.Stroke, text.StrokeWidth, text.StrokePaint);
                PaintNativeNode(scene, canvas, placed, index, VisualSceneTransform.Identity, null);
            } else PaintNativeNode(scene, canvas, scene.Nodes[index], index, transform, null);
        }
    }
}
