using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Core;
using ChartForgeX.Composition;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GifReaderDecodesDependencyFreeRasterInput() {
        var source = new RgbaImage(5, 3, SolidPixels(5, 3, ChartColors.DarkGreen));
        var gif = RasterImageEncoder.Encode(source, RasterImageFormat.Gif);
        var decoded = RasterImageDecoder.Decode(gif);

        Assert(decoded.Width == 5 && decoded.Height == 3, "GIF input should preserve logical dimensions.");
        Assert(decoded.Pixels[1] > 70 && decoded.Pixels[3] == 255, "GIF input should decode palette colors and opacity.");
        var composition = ImageComposition.FromBytes(gif);
        Assert(composition.Width == 5 && composition.Height == 3, "Image composition should accept GIF wallpaper input through the shared decoder.");

        var partialFrameGif = new byte[] {
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61,
            0x02, 0x00, 0x02, 0x00, 0x80, 0x00, 0x00,
            0x00, 0xFF, 0x00, 0xFF, 0x00, 0x00,
            0x2C, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
            0x02, 0x02, 0x4C, 0x01, 0x00, 0x3B
        };
        var partialFrame = RasterImageDecoder.Decode(partialFrameGif);
        Assert(partialFrame.Pixels[0] == 0 && partialFrame.Pixels[1] == 255 && partialFrame.Pixels[2] == 0 && partialFrame.Pixels[3] == 255, "GIF pixels outside a partial first frame should use the logical-screen background color.");
        var partialFrameOffset = (1 * partialFrame.Width + 1) * 4;
        Assert(partialFrame.Pixels[partialFrameOffset] == 255 && partialFrame.Pixels[partialFrameOffset + 1] == 0 && partialFrame.Pixels[partialFrameOffset + 2] == 0 && partialFrame.Pixels[partialFrameOffset + 3] == 255, "GIF partial-frame pixels should composite over the logical-screen background.");
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
