using System;
using System.Linq;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>POST-API only. These contracts are not a current-API causal cohort or algorithm-selection proof.</summary>
public sealed class TurbulenceGenerationPostTests {
    [Theory]
    [InlineData(VisualTurbulenceType.Turbulence, VisualFilterColorSpace.Srgb, 0, 0)]
    [InlineData(VisualTurbulenceType.Turbulence, VisualFilterColorSpace.LinearRgb, 0, 0)]
    [InlineData(VisualTurbulenceType.FractalNoise, VisualFilterColorSpace.Srgb, 128, 128)]
    [InlineData(VisualTurbulenceType.FractalNoise, VisualFilterColorSpace.LinearRgb, 188, 128)]
    public void ZeroOctavesAndZeroFrequencyAreRealConstantFieldsWithLinearCoverageAlpha(
        VisualTurbulenceType type, VisualFilterColorSpace space, byte channel, byte alpha) {
        var none = new VisualTurbulence(.1, numberOfOctaves: 0, type: type, colorSpace: space).Generate(new VisualSize(4, 3));
        var zero = new VisualTurbulence(0, numberOfOctaves: 7, type: type, colorSpace: space).Generate(new VisualSize(4, 3));
        NativeEffectBeforeCapture.Write("turbulence-constant-" + type + "-" + space, null, none.ToPng(), none);
        Assert.Equal(none.Pixels, zero.Pixels);
        for (var index = 0; index < none.Pixels.Length; index += 4)
            Assert.Equal(new[] { channel, channel, channel, alpha }, none.Pixels.Skip(index).Take(4));
    }

    [Theory]
    [InlineData(0, 1)] [InlineData(1.9, 1)] [InlineData(-7.9, 8)] [InlineData(double.MaxValue, 2147483646)]
    public void RawSeedsAreRetainedWhileTheirSelectedFiniteIntegerNormalizationIsDeterministic(double seed, int normalized) {
        var effect = new VisualTurbulence(.071, .113, 3, seed, VisualTurbulenceType.FractalNoise);
        var image = effect.Generate(new VisualSize(32, 24));
        var equivalent = new VisualTurbulence(.071, .113, 3, normalized, VisualTurbulenceType.FractalNoise).Generate(new VisualSize(32, 24));
        NativeEffectBeforeCapture.Write("turbulence-seed-" + normalized + "-" + seed, null, image.ToPng(), image);
        Assert.Equal(seed, effect.Seed);
        Assert.Equal(normalized, RasterTurbulenceLattice.NormalizeSeed(seed));
        Assert.Equal(image.Pixels, equivalent.Pixels);
    }

    [Fact]
    public void TheLargestNegativeFiniteSeedNeverOverflowsItsIntegerBoundary() {
        var seed = RasterTurbulenceLattice.NormalizeSeed(double.MinValue);
        var raw = new VisualTurbulence(.071, .113, seed: double.MinValue).Generate(new VisualSize(8, 6));
        var equivalent = new VisualTurbulence(.071, .113, seed: seed).Generate(new VisualSize(8, 6));
        NativeEffectBeforeCapture.Write("turbulence-negative-finite-boundary", null, raw.ToPng(), raw);
        Assert.InRange(seed, 1, 2147483646);
        Assert.Equal(raw.Pixels, equivalent.Pixels);
    }

    [Fact]
    public void RepeatedGenerationOwnsSeparateArraysAndAllFourChannelsHaveIndependentLattices() {
        var effect = new VisualTurbulence(.071, .113, 3, 13, VisualTurbulenceType.FractalNoise);
        var first = effect.Generate(new VisualSize(32, 24)); var second = effect.Generate(new VisualSize(32, 24));
        NativeEffectBeforeCapture.Write("turbulence-independent-channels", null, first.ToPng(), first);
        Assert.NotSame(first.Pixels, second.Pixels); Assert.Equal(first.Pixels, second.Pixels);
        Assert.Contains(Enumerable.Range(0, first.Width * first.Height), index =>
            first.Pixels[index * 4] != first.Pixels[index * 4 + 1] && first.Pixels[index * 4 + 2] != first.Pixels[index * 4 + 3]);
        var old = second.Pixels[0]; first.Pixels[0] ^= 255;
        Assert.Equal(old, second.Pixels[0]);
    }

    [Fact]
    public void TheEightBitResolutionPolicyRetainsTheAuthoredOctaveCountWithoutIntegerDoublingOverflow() {
        var authored = new VisualTurbulence(.071, .113, int.MaxValue, 13, VisualTurbulenceType.FractalNoise);
        var bounded = new VisualTurbulence(.071, .113, 9, 13, VisualTurbulenceType.FractalNoise);
        var image = authored.Generate(new VisualSize(16, 12));
        NativeEffectBeforeCapture.Write("turbulence-authored-octave-policy", null, image.ToPng(), image);
        Assert.Equal(int.MaxValue, authored.NumberOfOctaves);
        Assert.Equal(image.Pixels, bounded.Generate(new VisualSize(16, 12)).Pixels);
    }

    [Fact]
    public void StitchingRepeatsLogicalBoundariesRatherThanEquatingDifferentPixelCenters() {
        var effect = new VisualTurbulence(.125, .0625, 3, 13, VisualTurbulenceType.FractalNoise, stitchTiles: true);
        var plan = RasterEffectPlan.CreateTurbulence(effect, new VisualSize(64, 48), 1, CancellationToken.None);
        var parameters = plan.TurbulenceData!;
        var lattice = new RasterTurbulenceLattice(effect.Seed, CancellationToken.None);
        var image = effect.Generate(new VisualSize(64, 48));
        NativeEffectBeforeCapture.Write("turbulence-stitch-period", null, image.ToPng(), image);
        for (var channel = 0; channel < 4; channel++) {
            var at = RasterEffects.SampleTurbulence(parameters, lattice, channel, 13.5, 5.5);
            Assert.Equal(at, RasterEffects.SampleTurbulence(parameters, lattice, channel, 77.5, 5.5));
            Assert.Equal(at, RasterEffects.SampleTurbulence(parameters, lattice, channel, 13.5, 53.5));
        }
        Assert.Equal(64, image.Width); Assert.Equal(48, image.Height);
    }

    [Fact]
    public void TheRealConstantWorkflowFitsItsExactOutputBudgetWithoutAnAllocatedDummySource() {
        var effect = new VisualTurbulence(0, type: VisualTurbulenceType.FractalNoise);
        var plan = RasterEffectPlan.CreateTurbulence(effect, new VisualSize(4, 2), 1, CancellationToken.None);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.PrepareSourceFree(plan, maximumLiveBytes: 31));
        Assert.Throws<ArgumentException>(() => RasterEffectPreflight.Prepare(new[] { plan },
            new[] { new RgbaImage(4, 2, new byte[32]) }));
        var exact = RasterEffectPreflight.PrepareSourceFree(plan, maximumLiveBytes: 32);
        Assert.Throws<InvalidOperationException>(() => RasterEffects.Execute(exact, 0, false, CancellationToken.None));
        var image = RasterEffects.Generate(exact, 0, CancellationToken.None).Image;
        NativeEffectBeforeCapture.Write("turbulence-source-free-exact-budget", null, image.ToPng(), image);
        Assert.Equal(32, exact.ConservativeLiveBytes); Assert.Equal(effect.Generate(new VisualSize(4, 2)).Pixels, image.Pixels);
        Assert.Throws<InvalidOperationException>(() => RasterEffects.Generate(exact, 0, CancellationToken.None));
    }

    [Fact]
    public void CancellationGridAndFiniteCoordinateLimitsRejectBeforePublishingAnyNewPixels() {
        var effect = new VisualTurbulence(.071, .113);
        var before = effect.Generate(new VisualSize(8, 6)); var saved = (byte[])before.Pixels.Clone();
        NativeEffectBeforeCapture.Write("turbulence-boundary-control", null, before.ToPng(), before);
        Assert.Throws<OperationCanceledException>(() => effect.Generate(new VisualSize(8, 6), cancellationToken: new CancellationToken(true)));
        Assert.Throws<ArgumentOutOfRangeException>(() => effect.Generate(new VisualSize(8.25, 6)));
        Assert.Throws<ArgumentOutOfRangeException>(() => effect.Generate(new VisualSize(8193, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualTurbulence(double.MaxValue).Generate(new VisualSize(8, 6)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualTurbulence(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualTurbulence(.1, numberOfOctaves: -1));
        Assert.Equal(saved, before.Pixels);
    }
}
