using System;
using System.IO;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void RasterFormatsIncludePngAndJpeg() {
        Assert(RasterImageFormat.Png.GetFileExtension() == ".png", "Generic raster format metadata should include PNG.");
        Assert(RasterImageFormat.Jpeg.GetMimeType() == "image/jpeg", "Generic raster format metadata should include JPEG.");
        Assert(RasterImageFormatExtensions.TryFromFileExtension("wallpaper.jpeg", out var jpeg) && jpeg == RasterImageFormat.Jpeg, "JPEG extension inference should support .jpeg paths.");
        Assert(RasterImageFormatExtensions.TryFromFileExtension(".png", out var png) && png == RasterImageFormat.Png, "PNG extension inference should support direct extension values.");
    }

    private static void JpegWriterRoundTripsReadablePixels() {
        var image = GradientImage(24, 18);
        var jpeg = image.ToJpeg(new RasterImageOptions { JpegQuality = 92, Background = ChartColors.White });
        Assert(jpeg.Length > 128 && jpeg[0] == 0xFF && jpeg[1] == 0xD8 && jpeg[jpeg.Length - 2] == 0xFF && jpeg[jpeg.Length - 1] == 0xD9, "JPEG writer should emit a complete baseline JPEG stream.");

        var decoded = RasterImageDecoder.Decode(jpeg);
        Assert(decoded.Width == image.Width && decoded.Height == image.Height, "JPEG round-trip should preserve dimensions.");
        Assert(AverageColorDistance(image, decoded) < 28, "JPEG round-trip should preserve image color closely enough for wallpaper composition.");
    }

    private static void ImageCompositionComposesAnchoredOverlaysAndSavesByExtension() {
        var background = SolidImage(80, 50, 20, 30, 40, 255);
        var overlay = SolidImage(10, 8, 230, 40, 30, 192);
        var composition = ImageComposition.FromImage(background)
            .DrawImage(overlay, VisualCanvasPlacement.At(VisualCanvasAnchor.BottomRight, 6, 5), 10, 8, VisualCanvasImageFit.Stretch)
            .DrawText(4, 8, 50, "BG", 12, ChartColors.White, emphasized: true);

        var output = composition.ToImage();
        var overlayPixel = PixelAt(output, 72, 42);
        Assert(overlayPixel.R > 160 && overlayPixel.G < 45 && overlayPixel.B < 45, "Anchored image composition should alpha-blend overlays at the resolved bottom-right position.");

        var temp = Path.Combine(Path.GetTempPath(), "chartforgex-composition-" + Guid.NewGuid().ToString("N") + ".jpg");
        try {
            composition.Save(temp, new RasterImageOptions { JpegQuality = 88, Background = ChartColors.Black });
            var saved = File.ReadAllBytes(temp);
            Assert(saved.Length > 128 && saved[0] == 0xFF && saved[1] == 0xD8, "ImageComposition.Save should infer JPEG from the output extension.");
        } finally {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private static void ImageCompositionClearReplacesPixels() {
        var composition = ImageComposition.FromImage(SolidImage(6, 4, 20, 30, 40, 255))
            .Clear(ChartColors.Transparent);
        var transparent = composition.ToImage();
        Assert(PixelAt(transparent, 2, 2).A == 0, "ImageComposition.Clear should replace existing pixels with transparent pixels.");

        composition.Clear(ChartColor.FromRgba(90, 80, 70, 128));
        var semiTransparent = composition.ToImage();
        var pixel = PixelAt(semiTransparent, 2, 2);
        Assert(pixel.R == 90 && pixel.G == 80 && pixel.B == 70 && pixel.A == 128, "ImageComposition.Clear should replace, not blend, semi-transparent fills.");
    }

    private static void VisualCanvasTileFitRepeatsAcrossSvgAndPng() {
        var tile = SolidImage(4, 4, 240, 20, 40, 255);
        var canvas = VisualCanvas.Create(12, 4)
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddRasterImage(0, 0, 12, 4, tile, VisualCanvasImageFit.Tile);

        var svg = canvas.ToSvg();
        Assert(svg.Contains("<pattern", StringComparison.Ordinal) && svg.Contains("visual-canvas-image", StringComparison.Ordinal), "SVG visual canvas tile fit should emit a reusable image pattern.");

        var png = RasterImageDecoder.Decode(canvas.ToPng());
        Assert(PixelAt(png, 0, 0).R > 200 && PixelAt(png, 4, 0).R > 200 && PixelAt(png, 8, 0).R > 200, "PNG visual canvas tile fit should repeat the source image across the destination rectangle.");
    }

    private static void VisualCanvasImageIdsStayDeterministicForFixedScope() {
        var tile = SolidImage(4, 4, 240, 20, 40, 255);
        var canvas = VisualCanvas.Create(12, 8)
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddRasterImage(0, 0, 12, 4, tile, VisualCanvasImageFit.Tile)
            .AddRasterImage(2, 4, 6, 4, tile, VisualCanvasImageFit.Center);

        var first = canvas.ToSvg("fixed-scope");
        var second = canvas.ToSvg("fixed-scope");
        Assert(first == second, "Visual canvas SVG image resource IDs should stay deterministic for a fixed render scope.");
        Assert(first.Contains("-image-0-pattern", StringComparison.Ordinal) && first.Contains("-image-1-clip", StringComparison.Ordinal), "Visual canvas SVG image resource IDs should be derived from stable layer positions.");
    }

    private static RgbaImage GradientImage(int width, int height) {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var offset = (y * width + x) * 4;
                pixels[offset] = (byte)(20 + x * 180 / Math.Max(1, width - 1));
                pixels[offset + 1] = (byte)(40 + y * 160 / Math.Max(1, height - 1));
                pixels[offset + 2] = (byte)(220 - x * 90 / Math.Max(1, width - 1));
                pixels[offset + 3] = 255;
            }
        }

        return new RgbaImage(width, height, pixels);
    }

    private static RgbaImage SolidImage(int width, int height, byte red, byte green, byte blue, byte alpha) {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = red;
            pixels[i + 1] = green;
            pixels[i + 2] = blue;
            pixels[i + 3] = alpha;
        }

        return new RgbaImage(width, height, pixels);
    }

    private static RgbaImage CheckerImage(int width, int height) {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var red = (x + y) % 2 == 0;
                var offset = (y * width + x) * 4;
                pixels[offset] = red ? (byte)240 : (byte)20;
                pixels[offset + 1] = red ? (byte)20 : (byte)220;
                pixels[offset + 2] = 40;
                pixels[offset + 3] = 255;
            }
        }

        return new RgbaImage(width, height, pixels);
    }

    private static (byte R, byte G, byte B, byte A) PixelAt(RgbaImage image, int x, int y) {
        var offset = (y * image.Width + x) * 4;
        return (image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3]);
    }

    private static double AverageColorDistance(RgbaImage expected, RgbaImage actual) {
        var total = 0.0;
        for (var i = 0; i < expected.Pixels.Length; i += 4) {
            total += Math.Abs(expected.Pixels[i] - actual.Pixels[i]);
            total += Math.Abs(expected.Pixels[i + 1] - actual.Pixels[i + 1]);
            total += Math.Abs(expected.Pixels[i + 2] - actual.Pixels[i + 2]);
        }

        return total / (expected.Width * expected.Height * 3);
    }
}
