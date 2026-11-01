using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneRasterRenderer {
    private static void PaintFillImage(RgbaCanvas canvas, List<List<ChartPoint>> contours,
        VisualSceneGradient node, VisualSceneTransform transform, RgbaImage image) {
        var fill = node.AuthoredFill!; var bounds = node.FillBounds;
        var width = fill.Kind == ChartFillKind.Pattern ? fill.TileWidth : bounds.Width;
        var height = fill.Kind == ChartFillKind.Pattern ? fill.TileHeight : bounds.Height;
        var sx = width / image.Width; var sy = height / image.Height;
        var origin = transform.Apply(new ChartPoint(bounds.X, bounds.Y));
        var a = transform.A * sx; var b = transform.B * sx;
        var c = transform.C * sy; var d = transform.D * sy;
        var determinant = a * d - b * c;
        // The canvas pattern sampler maps logical destinations back into source pixels.
        canvas.FillContoursPattern(contours, image.Width, image.Height, image.Pixels,
            d / determinant, -b / determinant, -c / determinant, a / determinant,
            (c * origin.Y - d * origin.X) / determinant, (b * origin.X - a * origin.Y) / determinant, smooth: true);
    }
}
