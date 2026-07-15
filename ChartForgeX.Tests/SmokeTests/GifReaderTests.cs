using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GifReaderDecodesDependencyFreeRasterInput() {
        var source = new RgbaImage(5, 3, SolidPixels(5, 3, ChartColors.DarkGreen));
        var gif = RasterImageEncoder.Encode(source, RasterImageFormat.Gif);
        var decoded = RasterImageDecoder.Decode(gif);

        Assert(decoded.Width == 5 && decoded.Height == 3, "GIF input should preserve logical dimensions.");
        Assert(decoded.Pixels[1] > 70 && decoded.Pixels[3] == 255, "GIF input should decode palette colors and opacity.");
    }

    private static byte[] SolidPixels(int width, int height, ChartColor color) {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = color.R;
            pixels[i + 1] = color.G;
            pixels[i + 2] = color.B;
            pixels[i + 3] = color.A;
        }
        return pixels;
    }
}
