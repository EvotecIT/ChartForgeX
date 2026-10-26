using System;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Existing public SVG workflow only. Captures precede admission assertions; no future native effect symbols.</summary>
public sealed class SvgRasterTurbulenceAffineBeforeTests {
    [Theory]
    [InlineData("rotate", "matrix(.8660254037844386,.5,-.5,.8660254037844386,32,8)")]
    [InlineData("skew", "matrix(1,.25,.5,1,8,8)")]
    [InlineData("reflect", "matrix(-1,0,0,1,48,8)")]
    [InlineData("quarter-turn", "matrix(0,1,-1,0,48,8)")]
    public void ExplicitTransformedProceduralRegionIsAnActualSupportedSourceWorkflow(string name, string transform) {
        var source = TurbulenceAffineSourceFixture.Source(transform);
        var result = SvgRasterizer.Rasterize(source, filters: new SvgRasterFilterOptions());
        NativeEffectBeforeCapture.Write("turbulence-affine-before-" + name, source, result.Image.ToPng(), result.Image);
        NativeEffectBeforeCapture.Json("turbulence-affine-before-" + name + "-diagnostics", result.Diagnostics);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void NullOptionsPreserveTheIncumbentUnsupportedFilterAndScriptFreeSourceBoundary() {
        var source = TurbulenceAffineSourceFixture.Source("matrix(1,.25,.5,1,8,8)");
        var omitted = SvgRasterizer.Rasterize(source);
        var explicitNull = SvgRasterizer.Rasterize(source, filters: null);
        NativeEffectBeforeCapture.Write("turbulence-affine-null-before", source, omitted.Image.ToPng(), omitted.Image);
        Assert.Equal(omitted.Image.Pixels, explicitNull.Image.Pixels);
        Assert.Contains(omitted.Diagnostics, item => item.Code == "SFR002");
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true));
    }
}

internal static class TurbulenceAffineSourceFixture {
    internal static string Source(string transform, string? extra = null, string space = "sRGB") =>
        "<svg xmlns='http://www.w3.org/2000/svg' width='96' height='72'><defs>" +
        "<filter id='noise' x='0' y='0' width='24' height='16' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' color-interpolation-filters='" + space + "'>" +
        "<feTurbulence baseFrequency='.071 .113' numOctaves='3' seed='13' type='fractalNoise' stitchTiles='stitch'/></filter></defs>" +
        "<rect width='24' height='16' transform='" + transform + "' fill='white' filter='url(#noise)'" + (extra ?? "") + "/></svg>";
}
