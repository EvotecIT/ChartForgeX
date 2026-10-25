using ChartForgeX.Composition;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>A finite source chain exercises the existing public SVG importer before new native effect APIs exist.</summary>
public sealed class SvgRasterFiniteShadowCausalTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ExplicitFiniteAlphaMorphologyShadowChainRetainsSourceOnceAndVisibleShadowCoverage(bool erode) {
        var morphology = erode ? "erode" : "dilate";
        var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='48'><defs>"
            + "<filter id='shadow' x='0' y='0' width='64' height='48' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' color-interpolation-filters='sRGB'>"
            + "<feMorphology in='SourceAlpha' operator='" + morphology + "' radius='1' result='spread'/>"
            + "<feGaussianBlur in='spread' stdDeviation='0' edgeMode='none' result='soft'/>"
            + "<feOffset in='soft' dx='4' dy='2' result='shifted'/>"
            + "<feFlood flood-color='#0000ff' flood-opacity='0.5' result='tint'/>"
            + "<feComposite in='tint' in2='shifted' operator='in' result='shadowColor'/>"
            + "<feMerge><feMergeNode in='shadowColor'/><feMergeNode in='SourceGraphic'/></feMerge></filter></defs>"
            + "<rect id='source' x='24' y='16' width='8' height='8' fill='#ff0000' fill-opacity='0.5019607843137255' filter='url(#shadow)'/></svg>";
        var observed = SvgRasterizer.Rasterize(svg, filters: new SvgRasterFilterOptions());
        // Source and actual current fallback/diagnostics are saved before the intended unsupported-boundary failure.
        NativeEffectBeforeCapture.Write("finite-shadow-causal-before-" + morphology, svg,
            ImageComposition.FromImage(observed.Image).ToPng(), observed.Image);
        NativeEffectBeforeCapture.Json("finite-shadow-causal-before-" + morphology + "-diagnostics",
            observed.Diagnostics.Select(item => new { item.Code, item.Message, item.ElementName, item.ElementId }));
        Assert.Empty(observed.Diagnostics);
        Assert.Equal(new byte[] { 255, 0, 0, 128 }, Pixel(observed.Image, 24, 17));
        Assert.Equal(erode ? new byte[4] : new byte[] { 0, 0, 255, 64 }, Pixel(observed.Image, 35, 21));
        Assert.Equal(new byte[4], Pixel(observed.Image, 38, 21));
        // At a shared interior point, one original source over one half-opacity shadow gives alpha160.
        Assert.Equal(160, Pixel(observed.Image, 29, 20)[3]);
        var strict = SvgRasterizer.Rasterize(svg, strict: true, filters: new SvgRasterFilterOptions());
        Assert.Equal(observed.Image.Pixels, strict.Image.Pixels);
    }
    private static byte[] Pixel(RgbaImage image, int x, int y) => image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();
}
