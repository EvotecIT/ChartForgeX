using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

using static ChartForgeX.VisualBlocks.PngVisualBlockRenderer;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngFactBlockRenderer {
    private static void DrawIcon(RgbaCanvas canvas, VisualIcon icon, double x, double y, double size, ChartColor color) {
        var stroke = Math.Max(1.6, size * 0.16);
        if (icon == VisualIcon.ForkKnife) {
            canvas.StrokePathData(VisualIconGeometry.ForkKnife(x, y, size), color, stroke, RasterLineCap.Round, RasterLineJoin.Round);
            return;
        }

        if (icon == VisualIcon.Flame) {
            canvas.FillPathData(VisualIconGeometry.Flame(x, y, size), color);
            return;
        }

        if (icon == VisualIcon.Droplet) {
            canvas.FillPathData(VisualIconGeometry.Droplet(x, y, size), color);
            return;
        }

        if (icon == VisualIcon.Runner) {
            canvas.DrawCircleOutline(x + size * 0.12, y - size * 0.70, size * 0.16, color, stroke);
            canvas.StrokePolylines(new[] {
                Points(x + size * 0.02, y - size * 0.42, x - size * 0.18, y - size * 0.02, x + size * 0.10, y + size * 0.16),
                Points(x, y - size * 0.34, x + size * 0.40, y - size * 0.18),
                Points(x - size * 0.18, y - size * 0.02, x - size * 0.50, y + size * 0.36),
                Points(x + size * 0.10, y + size * 0.16, x + size * 0.48, y + size * 0.50)
            }, color, stroke, RasterLineCap.Round, RasterLineJoin.Round);
            return;
        }

        if (icon == VisualIcon.Bicycle) {
            canvas.DrawCircleOutline(x - size * 0.50, y + size * 0.34, size * 0.28, color, stroke);
            canvas.DrawCircleOutline(x + size * 0.50, y + size * 0.34, size * 0.28, color, stroke);
            canvas.StrokePolylines(new[] {
                Points(x - size * 0.50, y + size * 0.34, x - size * 0.12, y - size * 0.12, x + size * 0.18, y + size * 0.34, x - size * 0.50, y + size * 0.34),
                Points(x - size * 0.12, y - size * 0.12, x + size * 0.46, y - size * 0.12, x + size * 0.50, y + size * 0.34),
                Points(x - size * 0.02, y - size * 0.28, x - size * 0.24, y - size * 0.28)
            }, color, stroke, RasterLineCap.Round, RasterLineJoin.Round);
            return;
        }

        if (icon == VisualIcon.Person) {
            canvas.DrawCircle(x, y - size * 0.36, size * 0.28, color);
            canvas.FillRoundedRect(x - size * 0.58, y + size * 0.18, size * 1.16, size * 0.55, size * 0.26, color);
            return;
        }

        // The SVG bolt is one closed path, so its last corner is joined back to the first.
        canvas.StrokeClosedPolyline(Points(x - size * 0.52, y - size * 0.32, x + size * 0.10, y - size * 0.92, x, y - size * 0.26, x + size * 0.58, y - size * 0.08, x - size * 0.20, y + size * 0.82, x - size * 0.04, y + size * 0.08), color, stroke, RasterLineJoin.Round);
    }

    private static ChartPoint[] Points(params double[] coordinates) {
        var points = new ChartPoint[coordinates.Length / 2];
        for (var i = 0; i < points.Length; i++) points[i] = new ChartPoint(coordinates[i * 2], coordinates[i * 2 + 1]);
        return points;
    }
}
