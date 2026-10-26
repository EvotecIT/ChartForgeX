using System;
using System.Linq;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Post-only geometry/source membership contracts. The independent procedural algorithm reference remains separate.</summary>
public sealed class SvgRasterTurbulenceAffinePostTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReflectionAndQuarterTurnAreExactPermutationsOfThePublicGeneratedField(bool quarterTurn) {
        var transform = quarterTurn ? "matrix(0,1,-1,0,48,8)" : "matrix(-1,0,0,1,48,8)";
        var source = TurbulenceAffineSourceFixture.Source(transform);
        var actual = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        var raw = Policy().Generate(new VisualSize(24, 16));
        var expected = new byte[96 * 72 * 4];
        for (var y = 0; y < 16; y++) for (var x = 0; x < 24; x++) {
            var destinationX = quarterTurn ? 47 - y : 47 - x;
            var destinationY = quarterTurn ? 8 + x : 8 + y;
            Buffer.BlockCopy(raw.Pixels, (y * 24 + x) * 4, expected, (destinationY * 96 + destinationX) * 4, 4);
        }
        NativeEffectBeforeCapture.Write("turbulence-affine-permutation-" + quarterTurn, source, actual.Image.ToPng(), actual.Image);
        Assert.Empty(actual.Diagnostics);
        Assert.Equal(expected, actual.Image.Pixels);
    }

    [Theory]
    [InlineData("rotate", "matrix(.8,.6,-.6,.8,32,8)")]
    [InlineData("skew", "matrix(1,.25,.5,1,8,8)")]
    [InlineData("fractional", "matrix(1.5,0,0,.75,8.25,8.5)")]
    public void GeneralAffineUsesLogicalSamplesAndTheIncumbentUnfilteredQuadCoverage(string name, string transform) {
        var source = TurbulenceAffineSourceFixture.Source(transform);
        var actual = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        // The ordinary native shape is an independent coverage consumer, not a second clip formula.
        var coverage = SvgRasterizer.Rasterize(source.Replace(" filter='url(#noise)'", string.Empty, StringComparison.Ordinal), strict: true).Image;
        var matrix = SvgRasterMatrix.ParseTransform(transform);
        Assert.True(matrix.TryInvert(out var inverse));
        var policy = Policy();
        var parameters = new RasterTurbulenceParameters(policy, new ChartRect(0, 0, 24, 16),
            new RasterEffectRegion(0, 0, 24, 16), 1, CancellationToken.None);
        var lattice = new RasterTurbulenceLattice(policy.Seed, CancellationToken.None);
        var expected = new byte[96 * 72 * 4];
        var unmasked = new byte[4];
        for (var y = 0; y < 72; y++) for (var x = 0; x < 96; x++) {
            var offset = (y * 96 + x) * 4;
            if (coverage.Pixels[offset + 3] == 0) continue;
            var logical = inverse.Transform(new ChartPoint(x + .5, y + .5));
            RasterEffectPixels.WriteGenerated(unmasked, 0,
                RasterEffects.SampleTurbulence(parameters, lattice, 0, logical.X, logical.Y),
                RasterEffects.SampleTurbulence(parameters, lattice, 1, logical.X, logical.Y),
                RasterEffects.SampleTurbulence(parameters, lattice, 2, logical.X, logical.Y),
                RasterEffects.SampleTurbulence(parameters, lattice, 3, logical.X, logical.Y));
            RasterEffectPixels.WriteMasked(expected, offset, unmasked, 0, coverage.Pixels[offset + 3] * 257, x, y, false);
        }
        NativeEffectBeforeCapture.Write("turbulence-affine-general-" + name,
            source, actual.Image.ToPng(), actual.Image);
        Assert.Empty(actual.Diagnostics);
        Assert.Equal(expected, actual.Image.Pixels);
        Assert.All(Enumerable.Range(0, 96 * 72).Where(pixel => coverage.Pixels[pixel * 4 + 3] == 0),
            pixel => Assert.Equal(new byte[4], actual.Image.Pixels.Skip(pixel * 4).Take(4)));
        Assert.Contains(coverage.Pixels.Where((_, index) => index % 4 == 3), alpha => alpha is > 0 and < 255);
    }

    [Fact]
    public void ExactPublicArrayLimitIncludesCoverageBeforeTheSourceFreeKernelAndKeepsCancellation() {
        var source = TurbulenceAffineSourceFixture.Source("matrix(-1,0,0,1,48,8)");
        var ordinary = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        // Actual workflow: root96x72 + coverage24x16 + generated24x16 + lattice18432 + output96x72.
        const long exactBytes = 76800;
        var exact = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions(maximumLiveBytes: exactBytes));
        NativeEffectBeforeCapture.Write("turbulence-affine-exact-array-budget", source, exact.Image.ToPng(), exact.Image);
        Assert.Equal(ordinary.Image.Pixels, exact.Image.Pixels);
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true,
            filters: new SvgRasterFilterOptions(maximumLiveBytes: exactBytes - 1)));
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true,
            filters: new SvgRasterFilterOptions(maximumWorkSamples: 1)));
        Assert.Throws<OperationCanceledException>(() => SvgRasterizer.Rasterize(source, strict: true,
            filters: new SvgRasterFilterOptions(cancellationToken: new CancellationToken(true))));
    }

    [Theory]
    [InlineData("matrix(1,2,2,4,8,8)")]
    [InlineData("matrix(1000,0,0,1000,8,8)")]
    public void SingularAndOverBudgetOutputTransformsRemainLocatedUnsupportedSource(string transform) {
        var source = TurbulenceAffineSourceFixture.Source(transform);
        var result = SvgRasterizer.Rasterize(source, filters: new SvgRasterFilterOptions());
        NativeEffectBeforeCapture.Write("turbulence-affine-rejected-" + (transform.Contains("1000", StringComparison.Ordinal) ? "size" : "singular"),
            source, result.Image.ToPng(), result.Image);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SFR002");
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions()));
    }

    private static VisualTurbulence Policy() => new(.071, .113, 3, 13, VisualTurbulenceType.FractalNoise, stitchTiles: true);
}
