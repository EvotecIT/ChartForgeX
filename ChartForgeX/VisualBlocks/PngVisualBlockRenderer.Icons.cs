using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawIcon(RgbaCanvas canvas, VisualIcon icon, double x, double y, double size, ChartColor color) {
        var stroke = Math.Max(1.6, size * 0.16);
        if (icon == VisualIcon.ForkKnife) {
            canvas.DrawLine(x - size * 0.42, y - size * 0.54, x - size * 0.42, y + size * 0.48, color, stroke);
            canvas.DrawLine(x - size * 0.66, y - size * 0.56, x - size * 0.66, y - size * 0.12, color, stroke);
            canvas.DrawLine(x - size * 0.42, y - size * 0.56, x - size * 0.42, y - size * 0.12, color, stroke);
            canvas.DrawLine(x - size * 0.18, y - size * 0.56, x - size * 0.18, y - size * 0.12, color, stroke);
            canvas.DrawLine(x + size * 0.34, y + size * 0.48, x + size * 0.34, y - size * 0.52, color, stroke);
            canvas.DrawArc(x + size * 0.48, y - size * 0.22, size * 0.25, -Math.PI / 2, Math.PI / 2, color, stroke);
            return;
        }

        if (icon == VisualIcon.Flame) {
            canvas.FillPolygon(new[] {
                new ChartPoint(x, y + size * 0.70),
                new ChartPoint(x - size * 0.43, y + size * 0.36),
                new ChartPoint(x - size * 0.48, y - size * 0.12),
                new ChartPoint(x - size * 0.10, y - size * 0.78),
                new ChartPoint(x + size * 0.10, y - size * 0.30),
                new ChartPoint(x + size * 0.46, y - size * 0.72),
                new ChartPoint(x + size * 0.56, y - size * 0.12),
                new ChartPoint(x + size * 0.43, y + size * 0.36)
            }, color);
            return;
        }

        if (icon == VisualIcon.Droplet) {
            canvas.FillPolygon(new[] {
                new ChartPoint(x, y - size * 0.92),
                new ChartPoint(x - size * 0.28, y - size * 0.56),
                new ChartPoint(x - size * 0.52, y - size * 0.12),
                new ChartPoint(x - size * 0.58, y + size * 0.24),
                new ChartPoint(x - size * 0.45, y + size * 0.64),
                new ChartPoint(x - size * 0.16, y + size * 0.90),
                new ChartPoint(x, y + size * 0.98),
                new ChartPoint(x + size * 0.16, y + size * 0.90),
                new ChartPoint(x + size * 0.45, y + size * 0.64),
                new ChartPoint(x + size * 0.58, y + size * 0.24),
                new ChartPoint(x + size * 0.52, y - size * 0.12),
                new ChartPoint(x + size * 0.28, y - size * 0.56)
            }, color);
            return;
        }

        if (icon == VisualIcon.Runner) {
            canvas.DrawCircleOutline(x + size * 0.12, y - size * 0.70, size * 0.16, color, stroke);
            canvas.DrawLine(x + size * 0.02, y - size * 0.42, x - size * 0.18, y - size * 0.02, color, stroke);
            canvas.DrawLine(x - size * 0.18, y - size * 0.02, x + size * 0.10, y + size * 0.16, color, stroke);
            canvas.DrawLine(x, y - size * 0.34, x + size * 0.40, y - size * 0.18, color, stroke);
            canvas.DrawLine(x - size * 0.18, y - size * 0.02, x - size * 0.50, y + size * 0.36, color, stroke);
            canvas.DrawLine(x + size * 0.10, y + size * 0.16, x + size * 0.48, y + size * 0.50, color, stroke);
            return;
        }

        if (icon == VisualIcon.Bicycle) {
            canvas.DrawCircleOutline(x - size * 0.50, y + size * 0.34, size * 0.28, color, stroke);
            canvas.DrawCircleOutline(x + size * 0.50, y + size * 0.34, size * 0.28, color, stroke);
            canvas.DrawLine(x - size * 0.50, y + size * 0.34, x - size * 0.12, y - size * 0.12, color, stroke);
            canvas.DrawLine(x - size * 0.12, y - size * 0.12, x + size * 0.18, y + size * 0.34, color, stroke);
            canvas.DrawLine(x + size * 0.18, y + size * 0.34, x - size * 0.50, y + size * 0.34, color, stroke);
            canvas.DrawLine(x - size * 0.12, y - size * 0.12, x + size * 0.46, y - size * 0.12, color, stroke);
            canvas.DrawLine(x + size * 0.46, y - size * 0.12, x + size * 0.50, y + size * 0.34, color, stroke);
            canvas.DrawLine(x - size * 0.02, y - size * 0.28, x - size * 0.24, y - size * 0.28, color, stroke);
            return;
        }

        if (icon == VisualIcon.Person) {
            canvas.DrawCircle(x, y - size * 0.36, size * 0.28, color);
            canvas.FillRoundedRect(x - size * 0.58, y + size * 0.18, size * 1.16, size * 0.55, size * 0.26, color);
            return;
        }

        canvas.DrawLine(x - size * 0.52, y - size * 0.32, x + size * 0.10, y - size * 0.92, color, stroke);
        canvas.DrawLine(x + size * 0.10, y - size * 0.92, x, y - size * 0.26, color, stroke);
        canvas.DrawLine(x, y - size * 0.26, x + size * 0.58, y - size * 0.08, color, stroke);
        canvas.DrawLine(x + size * 0.58, y - size * 0.08, x - size * 0.20, y + size * 0.82, color, stroke);
        canvas.DrawLine(x - size * 0.20, y + size * 0.82, x - size * 0.04, y + size * 0.08, color, stroke);
    }
}
