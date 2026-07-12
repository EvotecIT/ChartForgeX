using System;
using System.IO;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void RasterFormatsIncludeCommonWebExports() {
        Assert(RasterImageFormat.Png.GetFileExtension() == ".png", "Generic raster format metadata should include PNG.");
        Assert(RasterImageFormat.Gif.GetFileExtension() == ".gif", "Generic raster format metadata should include GIF.");
        Assert(RasterImageFormat.Jpeg.GetMimeType() == "image/jpeg", "Generic raster format metadata should include JPEG.");
        Assert(RasterImageFormat.Gif.GetMimeType() == "image/gif", "Generic raster format metadata should include GIF MIME metadata.");
        Assert(RasterImageFormatExtensions.TryFromFileExtension("wallpaper.jpeg", out var jpeg) && jpeg == RasterImageFormat.Jpeg, "JPEG extension inference should support .jpeg paths.");
        Assert(RasterImageFormatExtensions.TryFromFileExtension("wallpaper.gif", out var gif) && gif == RasterImageFormat.Gif, "GIF extension inference should support .gif paths.");
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

    private static void ImageCompositionStrokesRectanglesForReusableAnnotations() {
        var composition = ImageComposition.FromImage(SolidImage(24, 18, 20, 30, 40, 255))
            .StrokeRectangle(3, 3, 12, 8, ChartColors.Red, 2)
            .StrokeRoundedRectangle(8, 6, 12, 9, 3, ChartColors.White, 1.5)
            .DrawLine(2, 16, 22, 16, ChartColors.Yellow, 2)
            .DrawDashedLine(2, 14, 22, 14, ChartColors.Cyan, 1, 2, 2)
            .DrawCallout(20, 4, 2, 9, 10, 7, "C", 6, ChartColors.White, ChartColors.Black, ChartColors.Yellow, 2, 1, 2);

        var output = composition.ToImage();
        var squareStroke = PixelAt(output, 4, 3);
        var roundedStroke = PixelAt(output, 10, 6);
        var linePixel = PixelAt(output, 12, 16);
        Assert(squareStroke.R > 180 && squareStroke.G < 80 && squareStroke.B < 80, "ImageComposition.StrokeRectangle should render reusable annotation outlines.");
        Assert(roundedStroke.R > 180 && roundedStroke.G > 180 && roundedStroke.B > 180, "ImageComposition.StrokeRoundedRectangle should render reusable rounded annotation outlines.");
        Assert(linePixel.R > 180 && linePixel.G > 180 && linePixel.B < 80, "ImageComposition.DrawLine should expose reusable annotation leader lines.");
    }

    private static void ImageCompositionExportsStillGifByExtension() {
        var composition = ImageComposition.FromImage(CheckerImage(18, 12))
            .StrokeRectangle(1, 1, 16, 10, ChartColors.Red, 2)
            .DrawText(3, 3, 12, "G", 8, ChartColors.White, emphasized: true);

        var gif = composition.ToGif();
        Assert(IsGif(gif), "ImageComposition.ToGif should emit GIF bytes through the reusable raster encoder.");

        var temp = Path.Combine(Path.GetTempPath(), "chartforgex-composition-" + Guid.NewGuid().ToString("N") + ".gif");
        try {
            composition.Save(temp);
            var saved = File.ReadAllBytes(temp);
            Assert(IsGif(saved), "ImageComposition.Save should infer GIF output from the file extension.");
        } finally {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private static void ImageCompositionExposesExplicitOutputAndNonThrowingLoaders() {
        var source = CheckerImage(16, 12);
        var sourcePng = source.ToPng(new RasterImageOptions { PngCompressionLevel = 9 });
        Assert(ImageComposition.TryFromBytes(sourcePng, out var loaded) && loaded != null && loaded.Width == 16 && loaded.Height == 12, "ImageComposition.TryFromBytes should expose non-throwing decoded-image composition.");
        var loadedComposition = loaded!;
        Assert(!ImageComposition.TryFromBytes(new byte[] { 1, 2, 3, 4 }, out var invalid) && invalid == null, "ImageComposition.TryFromBytes should reject unsupported image data without throwing.");

        using var stream = new MemoryStream();
        loadedComposition.Write(stream, RasterImageFormat.Png, new RasterImageOptions { PngCompressionLevel = 0 });
        Assert(stream.ToArray().Length > 64 && stream.ToArray()[0] == 137, "ImageComposition.Write should support explicit stream raster output.");

        var explicitPath = Path.Combine(Path.GetTempPath(), "chartforgex-composition-explicit-" + Guid.NewGuid().ToString("N") + ".ignored");
        var sourcePath = Path.Combine(Path.GetTempPath(), "chartforgex-composition-source-" + Guid.NewGuid().ToString("N") + ".png");
        try {
            loadedComposition.Save(explicitPath, RasterImageFormat.Gif);
            Assert(IsGif(File.ReadAllBytes(explicitPath)), "ImageComposition.Save should support explicit format output independent of the file extension.");

            File.WriteAllBytes(sourcePath, sourcePng);
            Assert(ImageComposition.TryFromFile(sourcePath, out var fileLoaded) && fileLoaded != null && fileLoaded.Width == 16, "ImageComposition.TryFromFile should expose non-throwing file composition.");
            Assert(!ImageComposition.TryFromFile(Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N") + ".png"), out var missing) && missing == null, "ImageComposition.TryFromFile should reject missing files without throwing.");
        } finally {
            if (File.Exists(explicitPath)) File.Delete(explicitPath);
            if (File.Exists(sourcePath)) File.Delete(sourcePath);
        }
    }

    private static void ImageCompositionAnnotatesScreenshotLikeRasterForPsParseHtml() {
        var screenshot = GradientImage(96, 56).ToPng(new RasterImageOptions { PngCompressionLevel = 6 });
        var composition = ImageComposition.FromBytes(screenshot)
            .StrokeRectangle(8, 7, 38, 22, ChartColor.FromRgb(255, 0, 0), 3)
            .DrawCallout(46, 18, 54, 8, 34, 18, "HTML", 9, ChartColors.Yellow, ChartColor.FromRgba(0, 0, 0, 208), ChartColors.White, 4, 2, 4)
            .DrawText(8, 34, 70, "selector", 9, ChartColors.White, emphasized: true);

        var annotated = composition.ToImage();
        var highlight = PixelAt(annotated, 8, 7);
        var callout = PixelAt(annotated, 82, 20);
        Assert(highlight.R > 180 && highlight.G < 80 && highlight.B < 80, "Screenshot-style annotation should render a red highlight rectangle.");
        Assert(callout.A > 180 && callout.R < 80 && callout.G < 80 && callout.B < 80, "Screenshot-style annotation should render a filled callout box.");

        Assert(composition.ToPng(new RasterImageOptions { PngCompressionLevel = 9 }).Length > 64, "Screenshot-style annotation should export PNG bytes.");
        Assert(IsGif(composition.ToRasterImage(RasterImageFormat.Gif)), "Screenshot-style annotation should export GIF bytes.");
        Assert(composition.ToJpeg(new RasterImageOptions { JpegQuality = 86, Background = ChartColors.White }).Length > 128, "Screenshot-style annotation should export JPEG bytes.");
        Assert(composition.ToRasterImage(RasterImageFormat.Bmp).Length > 128, "Screenshot-style annotation should export BMP bytes.");
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
        Assert(canvas.ToSvg() == canvas.ToSvg(), "Visual canvas SVG output should be deterministic by default.");
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

    private static bool IsGif(byte[] data) =>
        data.Length > 16 &&
        data[0] == (byte)'G' &&
        data[1] == (byte)'I' &&
        data[2] == (byte)'F' &&
        data[data.Length - 1] == 0x3B;
}
