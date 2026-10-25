using System;
using System.Linq;
using System.Threading;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeBackdropReplacementTests {
    [Theory]
    [InlineData(0)] [InlineData(255)] [InlineData(128)]
    public void CoverageReplacesBothPremultipliedChannelsAndAlphaInsteadOfPaintingOverBackdrop(byte alpha) {
        var input = ImageComposition.CreateTransparent(5, 5)
            .FillRectangle(2, 2, 1, 1, ChartColor.FromRgba(255, 0, 0, 128)).ToImage();
        var mask = new VisualMask(AppearanceFixture.Solid(5, 5, alpha), orderedDither: false);
        var actual = ImageComposition.FromImage(input).BlurRegion(mask, new VisualLayerBlur(1, orderedDither: false));
        AppearanceFixture.Capture("appearance-backdrop-coverage-" + alpha, actual);
        var output = actual.ToImage();
        if (alpha == 0) Assert.Equal(input.Pixels, output.Pixels);
        else {
            // Sigma1's single nontrivial radius1 box spreads128 alpha over nine pixels: final alpha14.
            var expectedCenter = alpha == 255 ? (byte)14 : (byte)71;
            var expectedAdjacent = alpha == 255 ? (byte)14 : (byte)7;
            Assert.Equal(new byte[] { 255, 0, 0, expectedCenter }, AppearanceFixture.Pixel(output, 2, 2));
            Assert.Equal(new byte[] { 255, 0, 0, expectedAdjacent }, AppearanceFixture.Pixel(output, 1, 2));
            Assert.Equal(new byte[4], AppearanceFixture.Pixel(output, 0, 0));
        }
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void ZeroSigmaIsRawIdentityForIndependentBlurAndMaskDitherSelections(bool blurDither, bool maskDither) {
        var composition = ImageComposition.Create(8, 8, ChartColor.FromRgba(37, 123, 219, 128));
        var before = composition.ToImage();
        var mask = new VisualMask(AppearanceFixture.Solid(8, 8, 128), orderedDither: maskDither);
        composition.BlurRegion(mask, new VisualLayerBlur(0, orderedDither: blurDither));
        AppearanceFixture.Capture("appearance-backdrop-zero-" + blurDither + "-" + maskDither, composition);
        Assert.Equal(before.Pixels, composition.ToImage().Pixels);
    }

    [Fact]
    public void MaskDitherChangesOnlyFinalAlphaWhileBlurDitherKeepsItsOwnRgbaPolicy() {
        var source = ImageComposition.Create(32, 32, ChartColor.FromRgba(37, 123, 219, 128)).ToImage();
        var plain = ImageComposition.FromImage(source).BlurRegion(
            new VisualMask(AppearanceFixture.Solid(32, 32, 128), orderedDither: false), new VisualLayerBlur(1, orderedDither: false));
        var dithered = ImageComposition.FromImage(source).BlurRegion(
            new VisualMask(AppearanceFixture.Solid(32, 32, 128), orderedDither: true), new VisualLayerBlur(1, orderedDither: false));
        AppearanceFixture.Capture("appearance-backdrop-mask-dither-off", plain);
        AppearanceFixture.Capture("appearance-backdrop-mask-dither-on", dithered);
        var first = plain.ToImage();
        var second = dithered.ToImage();
        for (var at = 0; at < first.Pixels.Length; at += 4)
            Assert.Equal(first.Pixels.Skip(at).Take(3), second.Pixels.Skip(at).Take(3));
        Assert.False(first.Pixels.SequenceEqual(second.Pixels));
    }

    [Fact]
    public void CoverageOriginClipsLocallyAndCanceledOrContradictoryRequestsDoNotCommit() {
        var input = ImageComposition.CreateTransparent(8, 6)
            .FillRectangle(2, 2, 3, 2, ChartColor.FromRgba(37, 123, 219, 128)).ToImage();
        var mask = new VisualMask(AppearanceFixture.Solid(2, 2, 255), new ChartPoint(3, 2), orderedDither: false);
        var actual = ImageComposition.FromImage(input);
        actual.BlurRegion(mask, new VisualLayerBlur(1, orderedDither: false));
        AppearanceFixture.Capture("appearance-backdrop-local-origin", actual);
        Assert.Equal(AppearanceFixture.Pixel(input, 2, 2), AppearanceFixture.Pixel(actual.ToImage(), 2, 2));
        Assert.NotEqual(AppearanceFixture.Pixel(input, 3, 2), AppearanceFixture.Pixel(actual.ToImage(), 3, 2));
        var before = actual.ToPng();
        Assert.Throws<OperationCanceledException>(() => actual.BlurRegion(mask, new VisualLayerBlur(2), new CancellationToken(true)));
        Assert.Throws<ArgumentException>(() => actual.BlurRegion(
            new VisualMask(AppearanceFixture.Solid(2, 2, 255), pixelsPerUnit: 2), new VisualLayerBlur(1)));
        Assert.Equal(before, actual.ToPng());
    }

    [Fact]
    public void OneFinitePlanDebitsBorrowedMaskTailBothBlurArraysAndFinalReplacementBeforeExecution() {
        var source = new RgbaImage(1, 1, new byte[] { 37, 123, 219, 128 });
        var pixels = new byte[24]; pixels[3] = 255;
        var mask = new RgbaImage(1, 1, pixels);
        var blur = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 1, 1), 256, 256);
        var plan = RasterEffectPlan.BlurRegion(new RasterEffectRegion(0, 0, 1, 1), mask, 0, 0, VisualMaskMode.Alpha, 256, 256, false);
        // Concrete1x1 workflow has a3x3 RGBA16 ping-pong pair,3x3 RGBA8 blur,1x1 replacement and both borrowed arrays.
        Assert.Equal(180, blur.AllocatedBytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.Prepare(new[] { plan }, new[] { source }, maximumLiveBytes: 211));
        var batch = RasterEffectPreflight.Prepare(new[] { plan }, new[] { source }, maximumLiveBytes: 212);
        var result = RasterEffects.Execute(batch, 0, false, CancellationToken.None);
        NativeEffectBeforeCapture.Write("appearance-backdrop-exact-budget", null, result.Image.ToPng(), result.Image);
        Assert.Equal(212, batch.ConservativeLiveBytes);
        Assert.Equal(new byte[] { 37, 123, 219, 14 }, result.Image.Pixels);
        Assert.Throws<InvalidOperationException>(() => RasterEffects.Execute(batch, 0, false, CancellationToken.None));
    }
}
