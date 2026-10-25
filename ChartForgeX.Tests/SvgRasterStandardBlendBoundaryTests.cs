using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgRasterStandardBlendBoundaryTests {
    [Theory]
    [InlineData("overlay")]
    [InlineData("soft-light")]
    [InlineData("hue")]
    [InlineData("luminosity")]
    public void StandardModeMembershipDoesNotAdmitAnotherSpaceInputGraphOrNoComposite(string mode) {
        var source = StandardBlendBeforeFixture.Source(mode);
        var actual = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        StandardBlendBeforeFixture.Capture("standard-blend-source-boundary-" + mode, source, actual);
        Assert.Empty(actual.Diagnostics);
        foreach (var rejected in new[] {
            source.Replace("color-interpolation-filters='sRGB'", "color-interpolation-filters='linearRGB'"),
            source.Replace(" color-interpolation-filters='sRGB'", ""),
            source.Replace("in2='backdrop'", "in2='BackgroundImage'"),
            source.Replace("<feBlend", "<feBlend no-composite='no-composite'"),
            source.Replace("mode='" + mode + "'", "mode='plus-lighter'"),
            source.Replace("mode='" + mode + "'", "mode='not-a-mode'") }) {
            Assert.Contains(SvgRasterizer.Rasterize(rejected, filters: new SvgRasterFilterOptions()).Diagnostics,
                item => item.Code == "SFR002");
            Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(rejected, strict: true, filters: new SvgRasterFilterOptions()));
        }
    }
}
