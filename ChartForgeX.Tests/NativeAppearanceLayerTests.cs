using System;
using System.Linq;
using System.Threading;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeAppearanceLayerTests {
    [Fact]
    public void OuterBandSubtractsOriginalAlphaAndAddsCoverageInsteadOfSourceOver() {
        var image = ImageComposition.CreateTransparent(7, 5)
            .FillRectangle(2, 2, 1, 1, ChartColor.FromRgba(255, 0, 0, 128))
            .FillRectangle(3, 2, 1, 1, ChartColor.FromRgba(255, 0, 0, 255))
            .Outline(new VisualOutline(ChartColor.FromRgba(0, 0, 255, 255), 1, orderedDither: false));
        AppearanceFixture.Capture("appearance-outline-coverage", image);
        Assert.Equal(new byte[] { 128, 0, 127, 255 }, AppearanceFixture.Pixel(image.ToImage(), 2, 2));
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, AppearanceFixture.Pixel(image.ToImage(), 3, 2));
        Assert.Equal(new byte[] { 0, 0, 255, 128 }, AppearanceFixture.Pixel(image.ToImage(), 1, 2));
        Assert.Equal(new byte[4], AppearanceFixture.Pixel(image.ToImage(), 0, 2));
    }

    [Theory]
    [InlineData(1)] [InlineData(2)]
    public void CompleteNativeOutlineRetainsUniformHalfAlphaInteriorSupportIdentityAndSnapshots(int density) {
        var source = AppearanceFixture.Source();
        var options = new VisualRenderOptions(scale: density, supersampling: 1);
        var raw = source.ToRgba(options);
        var layer = source.CaptureLayer(new VisualOutline(ChartColor.FromRgba(0, 0, 255, 255), 1, false), options);
        AppearanceFixture.Capture("appearance-outline-native-" + density, layer);
        Assert.Equal(new ChartRect(-1, -1, 10, 8), layer.Bounds);
        Assert.Equal(new byte[] { 255, 0, 0, 128 }, AppearanceFixture.WorldPixel(layer, 3, 3));
        Assert.Equal(new byte[] { 0, 0, 255, 128 }, AppearanceFixture.WorldPixel(layer, 1, 3));
        Assert.Same(source, layer.Source);
        Assert.Equal("source-mark", Assert.Single(layer.Visual.Regions).Id);
        Assert.Equal(raw.Pixels, source.ToRgba(options).Pixels);
        var detached = layer.ToImage();
        Array.Clear(detached.Pixels, 0, detached.Pixels.Length);
        Assert.Contains(layer.ToImage().Pixels, value => value != 0);
    }

    [Theory]
    [InlineData(0, 255)] [InlineData(2, 0)]
    public void ZeroRadiusAndTransparentTintUseExactOriginalCaptureBytes(double radius, byte alpha) {
        var source = AppearanceFixture.Source();
        var options = new VisualRenderOptions(supersampling: 1);
        var expected = source.CaptureLayer(new VisualLayerBlur(0), options);
        var actual = source.CaptureLayer(new VisualOutline(ChartColor.FromRgba(0, 0, 255, alpha), radius), options);
        AppearanceFixture.Capture("appearance-outline-noop-" + radius + "-" + alpha, actual);
        Assert.Equal(expected.Bounds, actual.Bounds);
        Assert.Equal(expected.ToImage().Pixels, actual.ToImage().Pixels);
        Assert.Equal(expected.Visual.ToSvg(), actual.Visual.ToSvg());
    }

    [Theory]
    [InlineData(0, 1)] [InlineData(.5, 0)] [InlineData(1, 1)]
    public void BloomRampAndStrengthHaveDiscriminatingZeroSigmaGoldens(double threshold, double strength) {
        var composition = ImageComposition.Create(1, 1, ChartColor.FromRgba(128, 128, 128, 255))
            .Bloom(new VisualBloom(0, threshold, strength, orderedDither: false));
        AppearanceFixture.Capture("appearance-bloom-ramp-" + threshold + "-" + strength, composition);
        var expected = threshold == 0 && strength == 1 ? (byte)160 : (byte)128;
        Assert.Equal(new byte[] { expected, expected, expected, 255 }, composition.ToImage().Pixels);
    }

    [Fact]
    public void BloomNoBrightSamplesRetainsSourceBoundsAndSubsequentMaskUsesThoseActualBounds() {
        var source = AppearanceFixture.Source();
        var options = new VisualRenderOptions(supersampling: 1);
        var baseline = source.CaptureLayer(new VisualLayerBlur(0), options);
        var bloom = source.CaptureLayer(new VisualBloom(2, .9, 1), options);
        var fullMask = new VisualMask(AppearanceFixture.Solid(8, 6, 255), orderedDither: false);
        var masked = bloom.WithMask(fullMask);
        AppearanceFixture.Capture("appearance-bloom-no-bright", bloom);
        AppearanceFixture.Capture("appearance-bloom-no-bright-masked", masked);
        Assert.Equal(baseline.Bounds, bloom.Bounds);
        Assert.Equal(baseline.Bounds, masked.Bounds);
        Assert.Equal(baseline.ToImage().Pixels, bloom.ToImage().Pixels);
        Assert.Equal(baseline.ToImage().Pixels, masked.ToImage().Pixels);
        Assert.Equal(baseline.Visual.ToSvg(), bloom.Visual.ToSvg());
    }

    [Fact]
    public void TransparentBrightRgbCannotCreateBloomAndNoOpRawKernelReturnsOriginalAllocation() {
        var source = new RgbaImage(1, 1, new byte[] { 255, 255, 255, 0 });
        var plan = RasterEffectPlan.Bloom(new RasterEffectRegion(0, 0, 1, 1), 512, 512, 0, 65535);
        var result = RasterEffects.Execute(RasterEffectPreflight.Prepare(new[] { plan }, new[] { source }), 0, true, CancellationToken.None);
        NativeEffectBeforeCapture.Write("appearance-bloom-transparent-bright", null, result.Image.ToPng(), result.Image);
        Assert.Same(source.Pixels, result.Image.Pixels);
        Assert.Equal(new byte[] { 255, 255, 255, 0 }, result.Image.Pixels);
        Assert.Equal(1, result.Region.Width);
        Assert.Equal(0, result.Region.X);
    }

    [Fact]
    public void BloomExtendsSupportForBrightMarksAndKeepsSourceFactsOutsideTheBakedImage() {
        var builder = new VisualSceneBuilder(new VisualSize(8, 6), FontSpec.SystemSans());
        builder.Rect(new ChartRect(2, 2, 2, 2), ChartColor.White);
        builder.AddRegion(new VisualSemanticRegion("bright", "sample", new ChartRect(2, 2, 2, 2), "Bright source"));
        var source = new PreparedVisual(builder.Build());
        var before = source.ToSvg();
        var layer = source.CaptureLayer(new VisualBloom(1, .5, 1, orderedDither: false), new VisualRenderOptions(supersampling: 1));
        AppearanceFixture.Capture("appearance-bloom-bright-support", layer);
        Assert.True(layer.Bounds.X < 0 && layer.Bounds.Width > source.Size.Width);
        Assert.True(AppearanceFixture.WorldPixel(layer, 1, 2)[3] > 0);
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, AppearanceFixture.WorldPixel(layer, 2, 2));
        Assert.Equal("bright", Assert.Single(layer.Visual.Regions).Id);
        Assert.Same(source, layer.Source);
        Assert.Equal(before, source.ToSvg());
    }

    [Fact]
    public void NewRequestsRejectInvalidDensityBoundsAndCancellationWithoutChangingSource() {
        var source = AppearanceFixture.Source();
        var before = source.ToSvg();
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualOutline(ChartColor.Black, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualBloom(1, -.1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualBloom(1, .5, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CaptureLayer(new VisualOutline(ChartColor.Black, .5),
            new VisualRenderOptions(supersampling: 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CaptureLayer(new VisualBloom(65, .5, 1),
            new VisualRenderOptions(scale: 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CaptureLayer(new VisualOutline(ChartColor.Black, 8192)));
        Assert.Throws<OperationCanceledException>(() => source.CaptureLayer(new VisualBloom(1, .5, 1),
            cancellationToken: new CancellationToken(true)));
        Assert.Equal(before, source.ToSvg());
    }
}

internal static class AppearanceFixture {
    internal static PreparedVisual Source() {
        var builder = new VisualSceneBuilder(new VisualSize(8, 6), FontSpec.SystemSans());
        builder.Rect(new ChartRect(2, 2, 3, 2), ChartColor.FromRgba(255, 0, 0, 128));
        builder.AddRegion(new VisualSemanticRegion("source-mark", "sample", new ChartRect(2, 2, 3, 2), "Retained mark"));
        return new PreparedVisual(builder.Build());
    }
    internal static RgbaImage Solid(int width, int height, byte alpha) {
        var pixels = new byte[width * height * 4];
        for (var at = 0; at < pixels.Length; at += 4) pixels[at + 3] = alpha;
        return new RgbaImage(width, height, pixels);
    }
    internal static byte[] Pixel(RgbaImage image, int x, int y) => image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();
    internal static byte[] WorldPixel(CapturedVisualLayer layer, int x, int y) => Pixel(layer.ToImage(),
        (int)((x - layer.Bounds.X) * layer.PixelsPerUnit), (int)((y - layer.Bounds.Y) * layer.PixelsPerUnit));
    internal static void Capture(string stem, CapturedVisualLayer layer) =>
        NativeEffectBeforeCapture.Write(stem, layer.Visual.ToSvg(), layer.Visual.ToPng(), layer.ToImage());
    internal static void Capture(string stem, ImageComposition composition) =>
        NativeEffectBeforeCapture.Write(stem, null, composition.ToPng(), composition.ToImage());
}
