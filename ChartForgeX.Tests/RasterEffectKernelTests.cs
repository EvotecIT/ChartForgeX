using System;
using System.Linq;
using System.Threading;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Analytic contracts for the shared opt-in effect kernels; no family or default output opts into it.</summary>
public sealed class RasterEffectKernelTests {
    [Fact]
    public void OnePixelImpulseHasExactThreeByThreeBoxAlphaAndTransparentSupport() {
        var source = new RgbaImage(1, 1, new byte[] { 255, 0, 0, 255 });
        var plan = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 1, 1), 256, 256);
        var actual = Run(source, plan);
        Assert.Equal(-1, actual.Region.X); Assert.Equal(-1, actual.Region.Y); Assert.Equal(3, actual.Image.Width); Assert.Equal(3, actual.Image.Height);
        for (int offset = 0; offset < actual.Image.Pixels.Length; offset += 4)
            Assert.Equal(new byte[] { 255, 0, 0, 28 }, actual.Image.Pixels.Skip(offset).Take(4));
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, source.Pixels);
    }

    [Fact]
    public void ExplicitCaptureOmitsNeighbourPaintAndHiddenTransparentRgbCannotBleed() {
        var source = new RgbaImage(3, 1, new byte[] { 0, 0, 255, 255, 255, 0, 0, 255, 0, 255, 0, 255 });
        var selected = RasterEffectPlan.Blur(new RasterEffectRegion(1, 0, 1, 1), 256, 256);
        var actual = Run(source, selected);
        for (int offset = 0; offset < actual.Image.Pixels.Length; offset += 4) {
            Assert.Equal(255, actual.Image.Pixels[offset]); Assert.Equal(0, actual.Image.Pixels[offset + 1]); Assert.Equal(0, actual.Image.Pixels[offset + 2]);
        }
        var hidden = new RgbaImage(2, 1, new byte[] { 255, 0, 0, 255, 0, 0, 255, 0 });
        var empty = new RgbaImage(2, 1, new byte[] { 255, 0, 0, 255, 0, 0, 0, 0 });
        var whole = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 2, 1), 256, 256);
        Assert.Equal(Run(hidden, whole).Image.Pixels, Run(empty, whole).Image.Pixels);
    }

    [Fact]
    public void AlphaShadowUsesOnlyCoverageWithOneColorAlphaAndExplicitDeviceOffset() {
        var source = new RgbaImage(1, 1, new byte[] { 12, 34, 56, 128 });
        var shadow = RasterEffectPlan.AlphaShadow(new RasterEffectRegion(0, 0, 1, 1), 0, 0, 2, -3, ChartColor.FromRgba(200, 80, 40, 64));
        var actual = Run(source, shadow);
        Assert.Equal(2, actual.Region.X); Assert.Equal(-3, actual.Region.Y);
        Assert.Equal(new byte[] { 200, 80, 40, 32 }, actual.Image.Pixels);
        Assert.Equal(source.Pixels, Run(source, RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 1, 1), 0, 0)).Image.Pixels);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ScalarOutputIsDeterministicAndEachPreflightedOperationIsOneUse(bool dither) {
        var source = new RgbaImage(1, 1, new byte[] { 37, 123, 219, 173 });
        var plan = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 1, 1), 512, 768);
        var batch = RasterEffectPreflight.Prepare(new[] { plan, plan }, new[] { source, source });
        var first = RasterEffects.Execute(batch, 0, dither, CancellationToken.None);
        var second = RasterEffects.Execute(batch, 1, dither, CancellationToken.None);
        Assert.Equal(first.Image.Pixels, second.Image.Pixels);
        Assert.Throws<InvalidOperationException>(() => RasterEffects.Execute(batch, 0, dither, CancellationToken.None));
    }

    [Fact]
    public void FullBatchWorkAndEveryPlannedPixelBufferAreBoundedBeforeAnyExecution() {
        var source = new RgbaImage(1, 1, new byte[] { 255, 0, 0, 255 });
        var plan = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 1, 1), 0, 0);
        Assert.Equal(20, plan.AllocatedBytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.Prepare(new[] { plan, plan }, new[] { source, source }, maximumLiveBytes: 43));
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.Prepare(new[] { plan, plan }, new[] { source, source }, maximumWorkSamples: plan.WorkSamples * 2 - 1));
        var accepted = RasterEffectPreflight.Prepare(new[] { plan, plan }, new[] { source, source });
        Assert.Equal(44, accepted.ConservativeLiveBytes); Assert.Equal(plan.WorkSamples * 2, accepted.WorkSamples);
    }

    [Fact]
    public void CancellationAndInvalidDimensionsNeverMutateTheBorrowedSource() {
        var source = new RgbaImage(1, 1, new byte[] { 12, 34, 56, 128 }); var before = source.Pixels.ToArray();
        var plan = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 1, 1), 256, 256);
        var batch = RasterEffectPreflight.Prepare(new[] { plan }, new[] { source });
        Assert.Throws<OperationCanceledException>(() => RasterEffects.Execute(batch, 0, false, new CancellationToken(true)));
        Assert.Equal(before, source.Pixels);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 1, 1), -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RasterEffectRegion(0, 0, 8192, 8192));
        Assert.Throws<OverflowException>(() => new RasterEffectRegion(int.MaxValue, 0, 1, 1));
    }

    [Fact]
    public void OrderedDitherUsesGlobalDevicePhaseAndOnlyFinalOutputIsQuantized() {
        var first = new byte[4]; var last = new byte[4]; var translated = new byte[4];
        RasterEffectPixels.Write(first, 0, 128, 128, 128, 65535, 0, 0, true);
        RasterEffectPixels.Write(last, 0, 128, 128, 128, 65535, 7, 0, true);
        RasterEffectPixels.Write(translated, 0, 128, 128, 128, 65535, 15, 8, true);
        Assert.Equal(new byte[] { 0, 0, 0, 255 }, first);
        Assert.Equal(new byte[] { 1, 1, 1, 255 }, last); Assert.Equal(last, translated);
    }

    [Fact]
    public void ExplicitCanvasSeamBorrowsDevicePixelsWithoutDownsamplingOrFontDiscovery() {
        var canvas = new RgbaCanvas(2, 1, 2, null, 1, false); canvas.Clear(ChartColor.FromRgba(12, 34, 56, 128));
        var source = canvas.BorrowDeviceSurfaceForEffects();
        Assert.Equal(4, source.Width); Assert.Equal(2, source.Height); Assert.Same(canvas.Pixels, source.Pixels);
        var result = Run(source, RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 4, 2), 0, 0));
        Assert.Equal(source.Pixels, result.Image.Pixels); Assert.NotSame(source.Pixels, result.Image.Pixels);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPlan.Blur(default, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 1, 1), RasterEffectPlan.MaximumSigmaQ8 + 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.Prepare(new[] { RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 1, 1), 0, 0) },
            new[] { source }, maximumLiveBytes: RasterEffectPreflight.MaximumLiveBytes + 1));
    }

    private static RasterEffectResult Run(RgbaImage source, RasterEffectPlan plan) =>
        RasterEffects.Execute(RasterEffectPreflight.Prepare(new[] { plan }, new[] { source }), 0, false, CancellationToken.None);
}
