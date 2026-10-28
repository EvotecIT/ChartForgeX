// Copyright ©2011/2018 W3C®. See Fixtures/Effects/W3C-Turbulence-NOTICE.txt.
using System;
using System.Linq;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Fixed independent mathematical vectors for the selected native law, not browser readback goldens.</summary>
public sealed class TurbulenceSelectedLawPostTests {
    [Theory]
    [InlineData(1, 1, false, VisualTurbulenceType.Turbulence, VisualFilterColorSpace.Srgb, 14, 58, 10, 41)]
    [InlineData(-7.9, 3, false, VisualTurbulenceType.FractalNoise, VisualFilterColorSpace.Srgb, 134, 153, 66, 162)]
    [InlineData(13, 2, true, VisualTurbulenceType.Turbulence, VisualFilterColorSpace.LinearRgb, 88, 147, 109, 51)]
    [InlineData(1735542951, 1, false, VisualTurbulenceType.FractalNoise, VisualFilterColorSpace.Srgb, 156, 137, 147, 132)]
    public void PhysicalCellCenterHasTheIndependentSquareStreamVector(double seed, int octaves, bool stitch,
        VisualTurbulenceType type, VisualFilterColorSpace space, int r, int g, int b, int alpha) {
        // Independent scalar evaluation of SVG1.1's square-normalized stream, at logical (3.5,5.5),
        // with zero-vector retry, native 16-bit-to-byte quantization and no premultiplied browser surface.
        // The last seed emits the exact zero pair (256,4302592) modulo512 before its first retry.
        var image = new VisualTurbulence(.071, .113, octaves, seed, type, stitch, space).Generate(new VisualSize(64, 48));
        NativeEffectBeforeCapture.Write("turbulence-selected-vector-" + seed, null, image.ToPng(), image);
        Assert.Equal(new[] { r, g, b, alpha }, image.Pixels.Skip((5 * 64 + 3) * 4).Take(4).Select(value => (int)value));
    }

    [Fact]
    public void ParkMillerKnownTenThousandthValueProtectsTheSeededStream() {
        var state = 1;
        for (var index = 0; index < 10000; index++) RasterTurbulenceLattice.Next(ref state);
        // Published independent Park-Miller sanity vector in the retained algorithm sources.
        Assert.Equal(1043618065, state);
    }

    [Fact]
    public void ReachableZeroVectorRetryIsAdmittedByTheSameWorkBudgetBeforePixelsAllocate() {
        const double zeroPairSeed = 1735542951;
        var policy = new VisualTurbulence(.071, .113, seed: zeroPairSeed);
        var plan = RasterEffectPlan.CreateTurbulence(policy, new VisualSize(8, 6), 1, CancellationToken.None);
        // One extra two-number gradient attempt is charged once in scalar admission and once in construction.
        Assert.Equal(53272L, plan.TurbulenceData!.SetupWorkSamples);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.PrepareSourceFree(plan,
            maximumWorkSamples: plan.WorkSamples - 1));
        var admitted = RasterEffectPreflight.PrepareSourceFree(plan, maximumWorkSamples: plan.WorkSamples);
        var result = RasterEffects.Generate(admitted, 0, CancellationToken.None).Image;
        Assert.Equal(8 * 6 * 4, result.Pixels.Length);
        Assert.Throws<OperationCanceledException>(() => policy.Generate(new VisualSize(8, 6),
            cancellationToken: new CancellationToken(true)));
    }
}
