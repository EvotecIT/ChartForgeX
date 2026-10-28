using System;
using System.Linq;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeInnerShadowTests {
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ComplementaryCoverageUsesAtopOnceAndPreservesEveryOriginalAlpha(int density) {
        var source = InnerShadowFixture.Source(128);
        var options = new VisualRenderOptions(scale: density, supersampling: 1);
        var original = source.ToRgba(options);
        var effect = new VisualInnerShadow(ChartColor.FromRgb(0, 0, 255), 0, 0, 0, orderedDither: false);
        var layer = source.CaptureLayer(effect, options);
        AppearanceFixture.Capture("inner-zero-geometry-" + density, layer);
        Assert.Equal(new byte[] { 128, 0, 127, 128 }, AppearanceFixture.WorldPixel(layer, 8, 8));
        Assert.Equal(new byte[4], AppearanceFixture.WorldPixel(layer, 3, 8));
        Assert.Equal(new ChartRect(0, 0, 16, 16), layer.Bounds);
        Assert.Same(source, layer.Source);
        var pixels = layer.ToImage().Pixels;
        for (var at = 3; at < pixels.Length; at += 4) Assert.Equal(original.Pixels[at], pixels[at]);
        var detached = layer.ToImage();
        Array.Clear(detached.Pixels, 0, detached.Pixels.Length);
        Assert.Equal(new byte[] { 128, 0, 127, 128 }, AppearanceFixture.WorldPixel(layer, 8, 8));
        Assert.Equal("inner-source", Assert.Single(layer.Visual.Regions).Id);
    }

    [Theory]
    [InlineData(1, 0, 4, 5)]
    [InlineData(0, 2, 5, 6)]
    [InlineData(-1, 2, 4, 5)]
    public void PositiveInsetSpreadErodesOnceAndOffsetsUseConstantOneComplementExterior(int spread, int offset,
        int shadowX, int clearX) {
        var source = InnerShadowFixture.Source(255);
        var layer = source.CaptureLayer(new VisualInnerShadow(ChartColor.FromRgb(0, 0, 255), offset, 0, 0,
            spread: spread, orderedDither: false), InnerShadowFixture.Unit);
        AppearanceFixture.Capture("inner-spread-" + spread + "-offset-" + offset, layer);
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, AppearanceFixture.WorldPixel(layer, shadowX, 8));
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, AppearanceFixture.WorldPixel(layer, clearX, 8));
        Assert.Equal(new byte[4], AppearanceFixture.WorldPixel(layer, 3, 8));
        Assert.Equal(new ChartRect(0, 0, 16, 16), layer.Bounds);
    }

    [Fact]
    public void OpaqueZeroCoverageAndZeroTintOpacityKeepExactOrdinaryBytesAndNoWrapper() {
        var source = InnerShadowFixture.Source(255);
        var ordinary = source.CaptureLayer(new VisualLayerBlur(0), InnerShadowFixture.Unit);
        var identity = source.CaptureLayer(new VisualInnerShadow(ChartColor.FromRgb(11, 29, 248), 0, 0, 0), InnerShadowFixture.Unit);
        var noTint = new VisualInnerShadow(ChartColor.Transparent, 2, 3, 2, spread: 1);
        var noOpacity = new VisualInnerShadow(ChartColor.Black, 2, 3, 2, spread: 1, opacity: 0);
        AppearanceFixture.Capture("inner-zero-coverage", identity);
        Assert.Equal(ordinary.ToImage().Pixels, identity.ToImage().Pixels);
        Assert.Equal(ordinary.Visual.ToSvg(), identity.Visual.ToSvg());
        Assert.Same(source, source.WithInnerShadow(noTint));
        Assert.Same(source, source.WithInnerShadow(noOpacity));
        Assert.Equal(ordinary.ToImage().Pixels, source.CaptureLayer(noOpacity, InnerShadowFixture.Unit).ToImage().Pixels);
    }

    [Fact]
    public void RawTintOpacityIsAppliedBeforeFinalQuantizationAndAlphaRemainsOriginal() {
        var source = InnerShadowFixture.Source(25);
        var layer = source.CaptureLayer(new VisualInnerShadow(ChartColor.FromRgb(11, 29, 248), 0, 0, 0,
            orderedDither: false, opacity: .06), InnerShadowFixture.Unit);
        AppearanceFixture.Capture("inner-raw-opacity", layer);
        Assert.Equal(new byte[] { 242, 2, 13, 25 }, AppearanceFixture.WorldPixel(layer, 8, 8));
        Assert.Equal(new byte[] { 255, 0, 0, 25 }, AppearanceFixture.Pixel(source.ToRgba(InnerShadowFixture.Unit), 8, 8));
        var dithered = source.CaptureLayer(new VisualInnerShadow(ChartColor.Black, 1, 2, 1), InnerShadowFixture.Unit).ToImage();
        var original = source.ToRgba(InnerShadowFixture.Unit);
        for (var at = 3; at < dithered.Pixels.Length; at += 4) Assert.Equal(original.Pixels[at], dithered.Pixels[at]);
    }

    [Fact]
    public void ExistingCaptureBudgetsCancellationAndSourceImmutabilityApplyBeforePublication() {
        var source = InnerShadowFixture.Source(128);
        var svg = source.ToSvg();
        var effect = new VisualInnerShadow(ChartColor.Black, 2, 2, 1, spread: 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualInnerShadow(ChartColor.Black, 0, 0, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualInnerShadow(ChartColor.Black, 0, 0, 1, spread: int.MinValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualInnerShadow(ChartColor.Black, 0, 0, 1, opacity: 1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CaptureLayer(effect,
            new VisualRenderOptions(supersampling: 1, pixelBudget: 255)));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CaptureLayer(new VisualInnerShadow(ChartColor.Black, 0, 0, 65),
            new VisualRenderOptions(scale: 2, supersampling: 1)));
        Assert.Throws<OperationCanceledException>(() => source.CaptureLayer(effect,
            cancellationToken: new CancellationToken(true)));
        var admitted = source.CaptureLayer(effect, new VisualRenderOptions(supersampling: 1, pixelBudget: 256));
        AppearanceFixture.Capture("inner-admitted-native-cap", admitted);
        Assert.Equal(16, admitted.ToImage().Width);
        Assert.Equal(svg, source.ToSvg());
    }
}
