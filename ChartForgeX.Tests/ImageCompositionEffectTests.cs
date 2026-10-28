using System.Threading;
using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Public opt-in image effects retain complete captures, fixed frames, source ordering and existing SVG image lowering.</summary>
public sealed class ImageCompositionEffectTests {
    [Fact]
    public void PublicBlurHasAnalyticTransparentSupportAndKeepsItsFrame() {
        var image = ImageComposition.CreateTransparent(5, 5).FillRectangle(2, 2, 1, 1, ChartColor.FromRgba(255, 0, 0, 255)).Blur(1).ToImage();
        Assert.Equal(5, image.Width); Assert.Equal(5, image.Height);
        var alpha = new byte[,] { { 28, 29, 28 }, { 29, 28, 29 }, { 28, 28, 28 } };
        for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
            Assert.Equal(x is >= 1 and <= 3 && y is >= 1 and <= 3 ? new byte[] { 255, 0, 0, alpha[y - 1, x - 1] } : new byte[4], Pixel(image, x, y));
    }

    [Fact]
    public void ShadowUsesAlphaAndLiteralColorOnceThenCompositesTheOriginalSourceOnce() {
        var source = ImageComposition.CreateTransparent(7, 5).FillRectangle(2, 2, 1, 1, ChartColor.FromRgba(255, 0, 0, 128));
        var image = source.DrawShadow(ChartColor.FromRgba(0, 0, 255, 64), 2, 0, 0).ToImage();
        Assert.Equal(new byte[] { 255, 0, 0, 128 }, Pixel(image, 2, 2));
        Assert.Equal(new byte[] { 0, 0, 255, 32 }, Pixel(image, 4, 2)); Assert.Equal(new byte[4], Pixel(image, 3, 2));
        var overlap = ImageComposition.CreateTransparent(5, 5).FillRectangle(2, 2, 1, 1, ChartColor.FromRgba(255, 0, 0, 128))
            .DrawShadow(ChartColor.FromRgba(0, 0, 255, 64), 0, 0, 0).ToImage();
        Assert.Equal(144, Pixel(overlap, 2, 2)[3]);
    }

    [Fact]
    public void WholeCompositionTextAndMarkAreCapturedTogetherBeforeAnyBlurredPixelChanges() {
        var original = ImageComposition.CreateTransparent(96, 48).FillRoundedRectangle(5, 10, 20, 20, 3, ChartColor.FromHex("#2166AC"))
            .DrawText(35, 10, 55, "Ready", 16, ChartColor.FromHex("#B2182B"));
        var before = original.ToImage();
        var originalPixels = (byte[])before.Pixels.Clone();
        var expected = ImageComposition.FromImage(before).Blur(3).ToPng();
        Assert.Equal(expected, original.Blur(3).ToPng()); Assert.False(originalPixels.SequenceEqual(original.ToImage().Pixels));
    }

    [Fact]
    public void RejectionCancellationAndZeroSigmaPreserveTheOriginalPixelBytes() {
        var image = ImageComposition.CreateTransparent(24, 16).FillRectangle(4, 4, 10, 8, ChartColor.FromRgba(37, 123, 219, 173));
        var before = image.ToPng();
        Assert.Throws<ArgumentOutOfRangeException>(() => image.Blur(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.Blur(129));
        Assert.Throws<OverflowException>(() => image.DrawShadow(ChartColor.Black, int.MaxValue, 0, 0));
        Assert.Throws<OperationCanceledException>(() => image.Blur(4, cancellationToken: new CancellationToken(true)));
        Assert.Throws<OperationCanceledException>(() => image.DrawShadow(ChartColor.Black, 2, -3, 4, cancellationToken: new CancellationToken(true)));
        Assert.Equal(before, image.Blur(0).ToPng()); Assert.Equal(before, image.ToPng());
        var tooWide = ImageComposition.CreateTransparent(8193, 1); var wideBefore = tooWide.ToPng();
        Assert.Throws<ArgumentOutOfRangeException>(() => tooWide.Blur(1)); Assert.Equal(wideBefore, tooWide.ToPng());
    }

    [Fact]
    public void ExistingCanvasSnapshotEmbedsTheExactComputedPixelsAndStrictSvgImportRemainsHonest() {
        var image = ImageComposition.CreateTransparent(64, 48).FillRoundedRectangle(12, 12, 32, 20, 5, ChartColor.FromHex("#2166AC"))
            .Blur(2).DrawShadow(ChartColor.FromRgba(0, 0, 0, 144), 2, 4, 3).ToImage();
        var expected = (byte[])image.Pixels.Clone();
        var canvas = VisualCanvas.Create(64, 48).WithPngOutputScale(1).WithBackdrop(VisualCanvasBackdropStyle.Transparent).AddImage(image, 0, 0, 64, 48);
        var svg = canvas.ToSvg(); var png = canvas.ToPng(); var document = XDocument.Parse(svg);
        var href = Assert.Single(document.Descendants(), node => node.Name.LocalName == "image").Attribute("href")!.Value;
        Assert.StartsWith("data:image/png;base64,", href);
        Assert.Equal(expected, RasterImageDecoder.Decode(Convert.FromBase64String(href.Substring(href.IndexOf(',') + 1))).Pixels);
        Assert.DoesNotContain(document.Descendants(), node => node.Attribute("filter") != null);
        var strict = SvgRasterizer.Rasterize(svg, strict: true); Assert.Empty(strict.Diagnostics);
        Assert.Equal(64, strict.Image.Width); Assert.Equal(48, strict.Image.Height);
        Assert.Equal(64, RasterImageDecoder.Decode(png).Width);
        Array.Clear(image.Pixels, 0, image.Pixels.Length); Assert.Equal(svg, canvas.ToSvg()); Assert.Equal(png, canvas.ToPng());
        Capture(svg, png);
    }

    [Fact]
    public void EffectLengthsRemainDeviceUnitsOnTheExistingInternallyScaledCompositionPath() {
        var composition = ImageComposition.CreateScaled(4, 4, ChartColor.Transparent, 2).FillRectangle(1, 1, 1, 1, ChartColor.FromRgba(255, 0, 0, 255));
        var deviceSource = composition.ToImage(); Assert.Equal(8, deviceSource.Width);
        var expected = ImageComposition.FromImage(deviceSource).Blur(1).ToImage();
        Assert.Equal(expected.Pixels, composition.Blur(1).ToImage().Pixels);
    }

    private static byte[] Pixel(RgbaImage image, int x, int y) => image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();
    private static void Capture(string svg, byte[] png) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "composition-effects-baked.svg"), svg);
        File.WriteAllBytes(Path.Combine(directory, "composition-effects-baked.png"), png);
    }
}
