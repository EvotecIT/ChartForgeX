using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Current-public-API causal source only; no future enum, native generator or setup dependency.</summary>
public sealed class SvgRasterTurbulenceTransformBeforeTests {
    [Theory]
    [InlineData("2 1")] [InlineData("1.5 .5")]
    public void PositiveAxisTransformsNeedActualProceduralSupportInsteadOfAnUnfilteredFallback(string scale) {
        var source = TurbulenceTransformSourceFixture.Source(scale);
        var actual = SvgRasterizer.Rasterize(source, filters: new SvgRasterFilterOptions());
        NativeEffectBeforeCapture.Write("turbulence-axis-before-" + scale.Replace(' ', '-'), source, actual.Image.ToPng(), actual.Image);
        Assert.Empty(actual.Diagnostics);
    }
}

internal static class TurbulenceTransformSourceFixture {
    internal static string Source(string scale) =>
        "<svg xmlns='http://www.w3.org/2000/svg' width='80' height='64'><defs><filter id='noise' x='0' y='0' width='32' height='24' " +
        "filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' color-interpolation-filters='sRGB'>" +
        "<feTurbulence type='fractalNoise' baseFrequency='.071 .113' seed='13' numOctaves='3' stitchTiles='stitch'/></filter></defs>" +
        "<g transform='translate(4 6) scale(" + scale + ")'><rect width='32' height='24' fill='none' filter='url(#noise)'/></g></svg>";
}
