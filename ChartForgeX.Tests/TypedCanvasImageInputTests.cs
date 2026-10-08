using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TypedCanvasImageInputTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BitmapInputSnapshotsCallerPixelsForBothOutputs(bool badge) {
        byte[] pixels = [255, 0, 0, 255, 0, 128, 0, 255];
        var image = new RgbaImage(2, 1, pixels);
        var canvas = VisualCanvas.Create(64, 64).WithBackdrop(VisualCanvasBackdropStyle.Transparent);
        if (badge) canvas.AddHeroBadge(image, new VisualCanvasPlacement(VisualCanvasAnchor.Center), 48, 48, padding: 0, fit: VisualCanvasImageFit.Stretch);
        else canvas.AddImage(image, new VisualCanvasPlacement(VisualCanvasAnchor.Center), 48, 48);
        var expected = (byte[])pixels.Clone();
        var before = canvas.ToPng();
        Array.Clear(pixels, 0, pixels.Length);

        var embedded = RasterImageDecoder.Decode(EmbeddedPng(canvas.ToSvg()));
        Assert.Equal(expected, embedded.Pixels);
        var after = canvas.ToPng();
        Assert.Equal(before, after);
        var png = RasterImageDecoder.Decode(after);
        Assert.InRange(Pixel(png, 20, 32)[0], 230, 255);
        Assert.InRange(Pixel(png, 44, 32)[1], 110, 128);
    }

    [Fact]
    public void RasterExtensionUsesSnapshotWhileLegacyRgbaOnlyEmbedsPixels() {
        byte[] pixels = [255, 0, 0, 255];
        var snapshot = VisualCanvas.Create(32, 32).AddRasterImage(0, 0, 32, 32, new RgbaImage(1, 1, pixels));
        pixels[0] = 0;
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, RasterImageDecoder.Decode(EmbeddedPng(snapshot.ToSvg())).Pixels);

        var legacy = VisualCanvas.Create(32, 32).AddImage(0, 0, 32, 32, rgba: pixels, sourceWidth: 1, sourceHeight: 1);
        Assert.Equal(pixels, RasterImageDecoder.Decode(EmbeddedPng(legacy.ToSvg())).Pixels);
    }

    [Fact]
    public void VectorProducerRetainsItsSvgRepresentation() {
        var chart = Chart.Create().WithSize(80, 60).AddLine("Capacity", [new ChartPoint(0, 1), new ChartPoint(1, 2)]);
        var canvas = VisualCanvas.Create(100, 80).AddChart(0, 0, 80, 60, chart);
        var document = XDocument.Parse(canvas.ToSvg());
        Assert.StartsWith("data:image/svg+xml;", document.Descendants().Single(node => node.Name.LocalName == "image").Attribute("href")!.Value);
        Assert.NotEmpty(canvas.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EncodedImageHelpersCaptureOriginalStaticContainerAndDecodedPixels(bool jpeg) {
        var source = ImageComposition.Create(3, 2, ChartColor.FromHex("#e02030"));
        var encoded = jpeg ? source.ToJpeg() : source.ToPng(new RasterImageOptions { Dpi = 123 });
        var original = (byte[])encoded.Clone();
        var decoded = RasterImageDecoder.Decode(original);
        var path = Path.Combine(Path.GetTempPath(), "cfx-encoded-image-" + Guid.NewGuid().ToString("N"));
        var options = new RasterDecodeOptions { MaximumEncodedBytes = encoded.Length, MaximumPixels = 6 };
        try {
            File.WriteAllBytes(path, encoded);
            var canvases = EncodedCanvases(encoded, path, options);
            Array.Clear(encoded, 0, encoded.Length);
            File.Delete(path);
            foreach (var canvas in canvases) {
                var href = ImageHref(canvas.ToSvg());
                Assert.StartsWith(jpeg ? "data:image/jpeg;base64," : "data:image/png;base64,", href);
                Assert.Equal(original, Convert.FromBase64String(href.Substring(href.IndexOf(',') + 1)));
                Assert.Equal(Pixel(decoded, 1, 1), Pixel(RasterImageDecoder.Decode(canvas.ToPng()), 8, 8));
            }
        } finally {
            File.Delete(path);
        }
    }

    [Fact]
    public void AnimatedPngHelpersEmbedOnlyTheDecodedStaticImage() {
        var first = ImageComposition.Create(3, 2, ChartColor.FromHex("#ff0000")).ToImage();
        var second = ImageComposition.Create(3, 2, ChartColor.FromHex("#00ff00")).ToImage();
        var encoded = RasterAnimationEncoder.Encode([
            new RasterAnimationFrame(first, TimeSpan.FromMilliseconds(100)),
            new RasterAnimationFrame(second, TimeSpan.FromMilliseconds(100))
        ], RasterAnimationFormat.Apng);
        Assert.True(PngReader.IsAnimatedPng(encoded));
        var path = Path.Combine(Path.GetTempPath(), "cfx-static-apng-" + Guid.NewGuid().ToString("N"));
        try {
            File.WriteAllBytes(path, encoded);
            foreach (var canvas in EncodedCanvases(encoded, path, new RasterDecodeOptions())) {
                var embedded = EmbeddedPng(canvas.ToSvg());
                Assert.False(PngReader.IsAnimatedPng(embedded));
                Assert.Equal(first.Pixels, RasterImageDecoder.Decode(embedded).Pixels);
                Assert.Equal(Pixel(first, 1, 1), Pixel(RasterImageDecoder.Decode(canvas.ToPng()), 8, 8));
            }
        } finally {
            File.Delete(path);
        }
    }

    [Fact]
    public void InvalidDefaultImageIsRejectedBeforeTheCanvasChanges() {
        var canvas = VisualCanvas.Create(32, 32);
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.AddImage(default(RgbaImage), 0, 0, 20, 20));
        Assert.Throws<ArgumentOutOfRangeException>(() => canvas.AddHeroBadge(default(RgbaImage), 0, 0, 20, 20));
        Assert.Empty(canvas.Layers);
    }

    [Theory]
    [InlineData(VisualCanvasImageFit.Stretch)]
    [InlineData(VisualCanvasImageFit.Contain)]
    [InlineData(VisualCanvasImageFit.Cover)]
    [InlineData(VisualCanvasImageFit.Center)]
    [InlineData(VisualCanvasImageFit.Tile)]
    public async Task SvgAndPngPaintTheSameBitmapPlacementInBrowser(VisualCanvasImageFit fit) {
        if (!InteractiveChartBrowser.Enabled) return;
        var pixels = new byte[16 * 8 * 4];
        for (var y = 0; y < 8; y++) {
            for (var x = 0; x < 16; x++) {
                var i = (y * 16 + x) * 4;
                pixels[i + (x < 8 ? 0 : 1)] = 255;
                pixels[i + 3] = 255;
            }
        }
        var canvas = VisualCanvas.Create(64, 64).WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddImage(new RgbaImage(16, 8, pixels), 8, 8, 48, 48, opacity: .5, fit: fit);
        var svg = canvas.ToSvg();
        var png = canvas.ToPng();
        var html = "<!doctype html><html><body style='margin:0;background:#fff'><img id='svg' width='256' height='256' src='data:image/svg+xml;base64,"
            + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg)) + "'><img id='png' width='256' height='256' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 540, 290);
        var samples = await session.Page.EvaluateAsync<int[][]>("""
            async () => {
                const results=[];
                for (const id of ['svg','png']) {
                    const image=document.getElementById(id); await image.decode();
                    const canvas=document.createElement('canvas');canvas.width=64;canvas.height=64;
                    const ctx=canvas.getContext('2d');ctx.drawImage(image,0,0,64,64);
                    results.push(...[[2,2],[12,12],[20,32],[28,32],[36,32],[44,32],[52,52]].map(([x,y])=>Array.from(ctx.getImageData(x,y,1,1).data)));
                }
                return results;
            }
            """);
        for (var sample = 0; sample < samples.Length / 2; sample++) {
            for (var channel = 0; channel < 4; channel++) Assert.InRange(Math.Abs(samples[sample][channel] - samples[sample + samples.Length / 2][channel]), 0, 2);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "typed-image-" + fit + ".png"), FullPage = true });
        }
    }

    private static byte[] EmbeddedPng(string svg) {
        var href = ImageHref(svg);
        Assert.StartsWith("data:image/png;base64,", href);
        return Convert.FromBase64String(href.Substring("data:image/png;base64,".Length));
    }

    private static string ImageHref(string svg) => XDocument.Parse(svg).Descendants().Single(node => node.Name.LocalName == "image").Attribute("href")!.Value;

    private static VisualCanvas[] EncodedCanvases(byte[] encoded, string path, RasterDecodeOptions options) {
        var placement = new VisualCanvasPlacement(VisualCanvasAnchor.Center);
        VisualCanvas Empty() => VisualCanvas.Create(16, 16).WithBackdrop(VisualCanvasBackdropStyle.Transparent);
        return [
            Empty().AddImageBytes(0, 0, 16, 16, encoded, options: options),
            Empty().AddImageBytes(placement, 16, 16, encoded, options: options),
            Empty().AddImageFile(0, 0, 16, 16, path, options: options),
            Empty().AddImageFile(placement, 16, 16, path, options: options),
            Empty().AddHeroBadgeImageFile(0, 0, 16, 16, path, fit: VisualCanvasImageFit.Stretch, padding: 0, options: options),
            Empty().AddHeroBadgeImageFile(placement, 16, 16, path, fit: VisualCanvasImageFit.Stretch, padding: 0, options: options)
        ];
    }

    private static byte[] Pixel(RgbaImage image, int x, int y) => image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();
}
