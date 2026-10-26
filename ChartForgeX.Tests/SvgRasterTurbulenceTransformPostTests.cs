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

/// <summary>Post-only after the provisional generator and this private coordinate-map continuation.</summary>
public sealed class SvgRasterTurbulenceTransformPostTests {
    [Theory]
    [InlineData("2 1", 2d, 1d)] [InlineData("1.5 .5", 1.5, .5)]
    public void PositiveAxisScaleSamplesTheSameLogicalLatticeAtTheActualPhysicalCenters(string scale, double sx, double sy) {
        var source = TurbulenceTransformSourceFixture.Source(scale);
        var actual = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        var effect = new VisualTurbulence(.071, .113, 3, 13, VisualTurbulenceType.FractalNoise, stitchTiles: true);
        var local = new RasterEffectRegion(0, 0, (int)(32 * sx), (int)(24 * sy));
        var plan = RasterEffectPlan.CreateTurbulence(effect, new ChartRect(0, 0, 32, 24), local, 1, CancellationToken.None,
            sampleStepX: 1 / sx, sampleStepY: 1 / sy);
        var generated = RasterEffects.Generate(RasterEffectPreflight.PrepareSourceFree(plan), 0, CancellationToken.None).Image;
        NativeEffectBeforeCapture.Write("turbulence-axis-after-" + scale.Replace(' ', '-'), source, actual.Image.ToPng(), actual.Image);
        Assert.Empty(actual.Diagnostics);
        for (var y = 0; y < local.Height; y++) for (var x = 0; x < local.Width; x++) {
            var at = ((y + 6) * 80 + x + 4) * 4;
            Assert.Equal(generated.Pixels.Skip((y * local.Width + x) * 4).Take(4), actual.Image.Pixels.Skip(at).Take(4));
        }
        Assert.Equal(0, actual.Image.Pixels[3]);
        Assert.Equal(effect.Seed, plan.TurbulenceData!.Effect.Seed);
        Assert.Equal(32, plan.TurbulenceData.Tile.Width); Assert.Equal(24, plan.TurbulenceData.Tile.Height);
    }
    [Fact]
    public void NullMappingKeepsTheOriginalUniformDensitySamplingArithmeticAndBytes() {
        var effect = new VisualTurbulence(.071, .113, 3, 13, VisualTurbulenceType.FractalNoise, stitchTiles: true);
        var first = effect.Generate(new VisualSize(32, 24), 3);
        var plan = RasterEffectPlan.CreateTurbulence(effect, new VisualSize(32, 24), 3, CancellationToken.None);
        var ordinary = RasterEffects.Generate(RasterEffectPreflight.PrepareSourceFree(plan), 0, CancellationToken.None).Image;
        NativeEffectBeforeCapture.Write("turbulence-axis-null-control", null, ordinary.ToPng(), ordinary);
        Assert.Null(plan.TurbulenceData!.SampleStepX); Assert.Null(plan.TurbulenceData.SampleStepY);
        Assert.Equal(first.Pixels, ordinary.Pixels);
    }
}
