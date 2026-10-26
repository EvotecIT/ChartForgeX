using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgRasterEmptyFilterSourceTests {
    [Theory]
    [InlineData("<g/>")]
    [InlineData("<g clip-path='url(#clip)'/>")]
    [InlineData("<g transform='translate(1 1)'/>")]
    [InlineData("<a clip-path='url(#clip)'/>")]
    public void EmptyContainersDoNotChangeTheAdmittedSourcePaint(string container) {
        var result = SvgRasterizer.Rasterize(Source(container), strict: true, filters: new SvgRasterFilterOptions());
        Assert.Empty(result.Diagnostics);
        Assert.Equal(4, result.Image.Width);
        Assert.Equal(3, result.Image.Height);
        Assert.Equal(Enumerable.Range(0, 12).SelectMany(_ => new byte[] { 255, 0, 0, 255 }), result.Image.Pixels);
    }

    [Theory]
    [InlineData("<g clip-path='url(#clip)'><rect width='1' height='1'/></g>")]
    [InlineData("<g filter='url(#flood)'/>")]
    public void PaintedClippingAndAnEmptyFilterThatCanPaintKeepTheirStrictRejection(string container) {
        var source = Source(container);
        Assert.Contains(SvgRasterizer.Rasterize(source, filters: new SvgRasterFilterOptions()).Diagnostics, diagnostic => diagnostic.Code == "SFR002");
        Assert.Contains("SFR002", Assert.Throws<NotSupportedException>(() =>
            SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions())).Message);
    }

    private static string Source(string container) =>
        "<svg xmlns='http://www.w3.org/2000/svg' width='4' height='3'><defs>"
        + "<clipPath id='clip'><rect width='4' height='3'/></clipPath>"
        + "<filter id='flood' filterUnits='userSpaceOnUse' x='0' y='0' width='4' height='3'><feFlood flood-color='blue'/></filter>"
        + "<filter id='identity' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='4' height='3' color-interpolation-filters='sRGB'>"
        + "<feColorMatrix type='saturate' values='1'/></filter></defs>"
        + "<g filter='url(#identity)'><rect width='4' height='3' fill='red'/>" + container + "</g></svg>";
}
