using System;
using ChartForgeX.Raster;
using ChartForgeX.Typography;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    /// <summary>Uses one attached context before each ordinary compositor canvas allocation.</summary>
    private static RgbaCanvas AllocateCanvas(int width, int height, SvgRasterDefinitions definitions) {
        if (definitions.Resources != null) definitions.Resources.ReserveCanvas(width, height);
        else definitions.Filters?.ReserveCanvas(width, height);
        return new RgbaCanvas(width, height, 1);
    }

    /// <summary>Accounts for existing text intermediates when a compilation capture scope is attached.</summary>
    private static RgbaCanvas AllocateTextCanvas(int width, int height, SvgRasterDefinitions definitions, TrueTypeFont? font = null) {
        definitions.Resources?.ReserveCanvas(width, height);
        return new RgbaCanvas(width, height, 1, font);
    }

    private static void ReservePixelArray(SvgRasterDefinitions definitions, long bytes) {
        if (definitions.Resources != null) definitions.Resources.ReservePixelArray(bytes);
        else definitions.Filters?.ReservePixelArray(bytes);
    }

    private static void ReserveWorkSamples(SvgRasterDefinitions definitions, long samples) {
        if (definitions.Resources != null) definitions.Resources.ReserveWorkSamples(samples);
        else definitions.Filters?.ReserveWorkSamples(samples);
    }

    private static void PaintTextStroke(byte[] target, byte[] mask, int width, int height, int radius,
        ChartForgeX.Primitives.ChartColor color, SvgRasterDefinitions definitions) {
        if (radius > 0 && color.A > 0 && definitions.Resources != null) {
            var pixels = checked((long)width * height); var axis = Math.Max(width, height);
            // Two existing extrema calls each create two alpha arrays and one int deque; no RGBA duplicate or radius-sized allocation.
            definitions.Resources.Reserve(checked(pixels * 4 + axis * 8L), checked(pixels * 68 + axis * 2L));
        }
        RasterTextStroke.Paint(target, mask, width, height, radius, color);
    }
}
