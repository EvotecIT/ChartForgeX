using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal static class RasterColorWriter {
    public static void FillRgb(byte[] destination, int offset, byte red, byte green, byte blue, byte alpha, ChartColor background) {
        Flatten(ref red, ref green, ref blue, alpha, background);
        destination[offset] = red;
        destination[offset + 1] = green;
        destination[offset + 2] = blue;
    }

    public static void FillBgr(byte[] destination, int offset, byte red, byte green, byte blue, byte alpha, ChartColor background) {
        Flatten(ref red, ref green, ref blue, alpha, background);
        destination[offset] = blue;
        destination[offset + 1] = green;
        destination[offset + 2] = red;
    }

    public static void Flatten(ref byte red, ref byte green, ref byte blue, byte alpha, ChartColor background) {
        if (alpha == 255) return;

        red = FlattenChannel(red, alpha, background.R);
        green = FlattenChannel(green, alpha, background.G);
        blue = FlattenChannel(blue, alpha, background.B);
    }

    private static byte FlattenChannel(byte source, byte alpha, byte background) {
        var inverse = 255 - alpha;
        return (byte)((source * alpha + background * inverse + 127) / 255);
    }
}
