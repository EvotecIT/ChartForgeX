using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    private static ChartColor SampleImageFiltered(byte[] rgba, int width, int height, double x, double y, double footprintX, double footprintY) {
        if (footprintX <= 1 && footprintY <= 1) return SampleImageBilinear(rgba, width, height, x, y);
        bool reduceX = footprintX > 1, reduceY = footprintY > 1;
        double left = reduceX ? x + .5 - footprintX / 2 : Math.Floor(x);
        double top = reduceY ? y + .5 - footprintY / 2 : Math.Floor(y);
        double right = reduceX ? x + .5 + footprintX / 2 : Math.Floor(x) + 2;
        double bottom = reduceY ? y + .5 + footprintY / 2 : Math.Floor(y) + 2;
        int x0 = (int)Math.Max(0, Math.Min(width - 1, Math.Floor(left)));
        int y0 = (int)Math.Max(0, Math.Min(height - 1, Math.Floor(top)));
        int x1 = (int)Math.Max(0, Math.Min(width - 1, Math.Ceiling(right) - 1));
        int y1 = (int)Math.Max(0, Math.Min(height - 1, Math.Ceiling(bottom) - 1));
        double red = 0, green = 0, blue = 0, alpha = 0, total = 0;
        for (int sy = y0; sy <= y1; sy++) {
            double wy = reduceY ? Math.Max(0, Math.Min(bottom, sy + 1) - Math.Max(top, sy)) : Math.Max(0, 1 - Math.Abs(sy - y));
            for (int sx = x0; sx <= x1; sx++) {
                double wx = reduceX ? Math.Max(0, Math.Min(right, sx + 1) - Math.Max(left, sx)) : Math.Max(0, 1 - Math.Abs(sx - x));
                double weight = wx * wy;
                ImageSample sample = ReadSample(rgba, width, sx, sy);
                red += sample.Red * weight; green += sample.Green * weight; blue += sample.Blue * weight;
                alpha += sample.Alpha * weight; total += weight;
            }
        }
        return total <= 0 ? ChartColor.Transparent : ToColor(new ImageSample(red / total, green / total, blue / total, alpha / total));
    }
}
