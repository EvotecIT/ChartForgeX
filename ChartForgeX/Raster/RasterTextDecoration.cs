using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

internal static class RasterTextDecoration {
    internal static void Draw(RgbaCanvas canvas, double x1, double x2, double y, TextDecorationStyle style, ChartColor color, double thickness) {
        if (style == TextDecorationStyle.None || x2 <= x1) return;
        var width = Math.Max(1, thickness);
        switch (style) {
            case TextDecorationStyle.Single:
                canvas.DrawLine(x1, y, x2, y, color, width);
                break;
            case TextDecorationStyle.Double:
                canvas.DrawLine(x1, y - width, x2, y - width, color, width);
                canvas.DrawLine(x1, y + width, x2, y + width, color, width);
                break;
            case TextDecorationStyle.Dotted:
                canvas.DrawDashedLine(x1, y, x2, y, color, width, Math.Max(1, width), Math.Max(1, width * 1.8));
                break;
            case TextDecorationStyle.Dashed:
                canvas.DrawDashedLine(x1, y, x2, y, color, width, Math.Max(2, width * 4), Math.Max(1, width * 2.5));
                break;
            case TextDecorationStyle.Wavy:
                DrawWave(canvas, x1, x2, y, color, width);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown text decoration style.");
        }
    }

    private static void DrawWave(RgbaCanvas canvas, double x1, double x2, double y, ChartColor color, double thickness) {
        var amplitude = Math.Max(1, thickness * 1.4);
        var step = Math.Max(2, thickness * 2.2);
        var previousX = x1;
        var previousY = y;
        var index = 1;
        for (var x = Math.Min(x2, x1 + step); x <= x2; x = Math.Min(x2, x + step)) {
            var nextY = y + (index % 2 == 0 ? -amplitude : amplitude);
            canvas.DrawLine(previousX, previousY, x, nextY, color, thickness);
            if (x >= x2) break;
            previousX = x;
            previousY = nextY;
            index++;
        }
    }
}
