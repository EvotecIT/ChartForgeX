using System;
using System.Linq;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Independent integer/rational contracts for the shared surface seam, not graph-loop snapshots.</summary>
public sealed class RasterEffectSurfaceContracts {
    [Fact]
    public void ByteCaptureHasOneRealAllocationAndAOneUsePermit() {
        var frame = new RasterEffectRegion(0, 0, 1, 1); var image = new RgbaImage(1, 1, new byte[] { 255, 0, 128, 128 });
        var plan = RasterEffectPlan.SurfaceCapture(frame);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.Prepare(new[] { plan }, new[] { image }, maximumLiveBytes: 11));
        var permit = RasterEffectPreflight.Prepare(new[] { plan }, new[] { image }, maximumLiveBytes: 12);
        var result = RasterEffects.CaptureSurface(permit, CancellationToken.None);
        Assert.False(result.LinearRgb); Assert.Equal(new ushort[] { 32896, 0, 16513, 32896 }, result.Pixels);
        Assert.Throws<InvalidOperationException>(() => RasterEffects.CaptureSurface(permit, CancellationToken.None));
        Assert.Equal(new byte[] { 255, 0, 128, 128 }, image.Pixels);
    }

    [Fact]
    public void BlurRetainsOneNinthCoverageWithoutAnIntermediateByteRoundTrip() {
        var pixels = new ushort[36]; pixels[16] = pixels[19] = 65535;
        var frame = new RasterEffectRegion(0, 0, 3, 3); var source = new RasterEffectSurface(frame, pixels, false);
        var plan = RasterEffectPlan.SurfaceBlur(frame, 256, 256, false);
        // Two expanded 5x5 RGBA16 arrays, one 3x3 result and the borrowed 3x3 surface: 400 + 72 + 72.
        Assert.Equal(472L, plan.AllocatedBytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.PrepareSurface(plan, source, maximumLiveBytes: 543));
        var result = RasterEffects.ExecuteSurface(RasterEffectPreflight.PrepareSurface(plan, source, maximumLiveBytes: 544), CancellationToken.None);
        for (var at = 0; at < result.Pixels.Length; at += 4)
            Assert.Equal(new ushort[] { 7282, 0, 0, 7282 }, result.Pixels.Skip(at).Take(4));
        Assert.Equal((ushort)65535, pixels[19]);
    }

    [Fact]
    public void NamedSurfaceColorStepsRetainSubByteInformationAndClampEveryPrimitive() {
        var frame = new RasterEffectRegion(0, 0, 1, 1);
        var source = new RasterEffectSurface(frame, new ushort[] { 64, 64, 64, 65535 }, false);
        var functions = new[] { Scale(8), Scale(8), Scale(8), default(RasterComponentTransfer) };
        var transform = new RasterColorTransform(functions, false);
        var first = RasterEffects.ExecuteSurface(RasterEffectPreflight.PrepareSurface(RasterEffectPlan.SurfaceColor(frame, transform), source), CancellationToken.None);
        var second = RasterEffects.ExecuteSurface(RasterEffectPreflight.PrepareSurface(RasterEffectPlan.SurfaceColor(frame, transform), first), CancellationToken.None);
        NativeEffectBeforeCapture.Json("ordered-surface-sub-byte", new { input = source.Pixels, first = first.Pixels, second = second.Pixels });
        Assert.Equal(new ushort[] { 512, 512, 512, 65535 }, first.Pixels);
        Assert.Equal(new ushort[] { 4096, 4096, 4096, 65535 }, second.Pixels);
        var clamped = new ushort[4]; RasterEffectPixels.Store16(clamped, 0, new RasterEffectPixel16(65535, -1, 32768, 12345));
        Assert.Equal(new ushort[] { 12345, 0, 12345, 12345 }, clamped);
        RasterEffectPixels.Store16(clamped, 0, new RasterEffectPixel16(100, 200, 300, 0)); Assert.Equal(new ushort[4], clamped);
    }

    [Fact]
    public void WorkingSpaceConversionChangesRgbOnlyAndAlphaSurfacesRetagWithoutArrays() {
        var frame = new RasterEffectRegion(0, 0, 1, 1);
        var source = new RasterEffectSurface(frame, new ushort[] { 32768, 32768, 32768, 65535 }, false);
        var linear = RasterEffects.ExecuteSurface(RasterEffectPreflight.PrepareSurface(RasterEffectPlan.SurfaceConvert(frame, true), source), CancellationToken.None);
        Assert.Equal(new ushort[] { 14028, 14028, 14028, 65535 }, linear.Pixels);
        var alpha = new RasterEffectSurface(frame, new ushort[] { 0, 0, 0, 32768 }, false, true);
        Assert.Same(alpha.Pixels, alpha.RetagAlpha(true).Pixels); Assert.Equal((ushort)32768, alpha.RetagAlpha(true).Pixels[3]);
    }

    [Fact]
    public void BorrowedSurfaceAliasingCountsOnceAndEveryPermitExecutesOnce() {
        var frame = new RasterEffectRegion(0, 0, 1, 1);
        var source = new RasterEffectSurface(frame, new ushort[] { 32768, 0, 0, 32768 }, false);
        var plan = RasterEffectPlan.SurfaceComposite(frame, source, VisualCompositeOperator.SourceOver, false);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.PrepareSurface(plan, source, maximumLiveBytes: 15));
        var permit = RasterEffectPreflight.PrepareSurface(plan, source, maximumLiveBytes: 16);
        var output = RasterEffects.ExecuteSurface(permit, CancellationToken.None);
        Assert.Equal(new ushort[] { 49152, 0, 0, 49152 }, output.Pixels);
        Assert.Throws<InvalidOperationException>(() => RasterEffects.ExecuteSurface(permit, CancellationToken.None));
        Assert.Throws<OperationCanceledException>(() => RasterEffects.ExecuteSurface(RasterEffectPreflight.PrepareSurface(plan, source), new CancellationToken(true)));
    }

    [Theory]
    [InlineData(VisualBlendMode.Overlay, 90, 33, 240)]
    [InlineData(VisualBlendMode.Hue, 223, 47, 182)]
    [InlineData(VisualBlendMode.Luminosity, 45, 125, 221)]
    public void HighPrecisionConsumersReachTheSameDirectionalSeparableAndNonseparableBlendLaw(VisualBlendMode mode, byte red, byte green, byte blue) {
        var frame = new RasterEffectRegion(0, 0, 1, 1);
        var source = new RasterEffectSurface(frame, new ushort[] { 61680, 8224, 49344, 65535 }, false);
        var destination = new RasterEffectSurface(frame, new ushort[] { 12336, 32896, 57568, 65535 }, false);
        var result = RasterEffects.ExecuteSurface(RasterEffectPreflight.PrepareSurface(
            RasterEffectPlan.SurfaceBlend(frame, source, mode, false), destination, maximumLiveBytes: 24), CancellationToken.None);
        var pixel = RasterEffectPixels.Read16(result.Pixels, 0); var rgba = new byte[4];
        RasterEffectPixels.Write(rgba, 0, pixel.R, pixel.G, pixel.B, pixel.A, 0, 0, false);
        Assert.Equal(new byte[] { red, green, blue, 255 }, rgba);
    }

    [Fact]
    public void ArithmeticClampsCoverageAndFiniteOverflowCancellationHasADefinedResult() {
        var white = new RasterEffectPixel16(65535, 65535, 65535, 65535);
        var generated = RasterEffectPixels.Arithmetic16(default, default, 0, 0, 0, .25);
        Assert.Equal(16384, generated.R); Assert.Equal(16384, generated.A);
        var canceled = RasterEffectPixels.Arithmetic16(white, white, double.MaxValue, double.MaxValue, -double.MaxValue, -double.MaxValue);
        Assert.Equal(0, canceled.R); Assert.Equal(0, canceled.A);
        var clipped = RasterEffectPixels.Arithmetic16(new RasterEffectPixel16(65535, 0, 0, 65535),
            new RasterEffectPixel16(0, 65535, 0, 65535), 0, 1, -1, .25);
        Assert.Equal(16384, clipped.R); Assert.Equal(0, clipped.G); Assert.Equal(16384, clipped.B); Assert.Equal(16384, clipped.A);
    }

    [Fact]
    public void FullColorMorphologySharesBoundedExtremaAndChargesEveryUshortArray() {
        var frame = new RasterEffectRegion(0, 0, 1, 1);
        var source = new RasterEffectSurface(frame, new ushort[] { 1000, 2000, 3000, 32768 }, true);
        var plan = RasterEffectPlan.SurfaceMorphology(frame, int.MaxValue, int.MaxValue, true, true);
        Assert.Equal(40L, plan.AllocatedBytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.PrepareSurface(plan, source, maximumLiveBytes: 47));
        var result = RasterEffects.ExecuteSurface(RasterEffectPreflight.PrepareSurface(plan, source, maximumLiveBytes: 48), CancellationToken.None);
        Assert.True(result.LinearRgb); Assert.Equal(source.Pixels, result.Pixels); Assert.NotSame(source.Pixels, result.Pixels);
        var eroded = RasterEffects.ExecuteSurface(RasterEffectPreflight.PrepareSurface(
            RasterEffectPlan.SurfaceMorphology(frame, 1, 0, false, true), source), CancellationToken.None);
        Assert.Equal(new ushort[4], eroded.Pixels);
    }

    [Fact]
    public void ColorMorphologyCombinesEachPremultipliedChannelRatherThanRetintingOnlyAlpha() {
        var frame = new RasterEffectRegion(0, 0, 3, 1);
        var pixels = new ushort[] { 65535, 0, 0, 65535, 0, 32768, 0, 32768, 0, 0, 16384, 16384 };
        var source = new RasterEffectSurface(frame, pixels, false);
        var plan = RasterEffectPlan.SurfaceMorphology(frame, 1, 0, true, false);
        Assert.Equal(120L, plan.AllocatedBytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.PrepareSurface(plan, source, maximumLiveBytes: 143));
        var result = RasterEffects.ExecuteSurface(RasterEffectPreflight.PrepareSurface(plan, source, maximumLiveBytes: 144), CancellationToken.None);
        Assert.Equal(new ushort[] { 65535, 32768, 0, 65535, 65535, 32768, 16384, 65535, 0, 32768, 16384, 32768 }, result.Pixels);
        Assert.Equal(new ushort[] { 65535, 0, 0, 65535, 0, 32768, 0, 32768, 0, 0, 16384, 16384 }, pixels);
    }

    private static RasterComponentTransfer Scale(double amount) => new(RasterTransferKind.Linear, first: amount);
}
