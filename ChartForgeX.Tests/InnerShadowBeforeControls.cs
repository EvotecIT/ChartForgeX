using System;
using System.Globalization;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Incumbent public controls plus finite source causal rows; no new inner-shadow symbols appear here.</summary>
public sealed class InnerShadowBeforeControls {
    [Fact]
    public void OrdinaryOriginalAndOuterShadowKeepTheirDistinctAlphaAndBounds() {
        var source = InnerShadowFixture.Source(128);
        var ordinary = source.CaptureLayer(new VisualLayerBlur(0), InnerShadowFixture.Unit);
        var outer = source.CaptureLayer(new VisualDropShadow(ChartColor.FromRgb(0, 0, 255), 0, 0, 0,
            orderedDither: false), InnerShadowFixture.Unit);
        AppearanceFixture.Capture("inner-before-ordinary", ordinary);
        AppearanceFixture.Capture("inner-before-outer", outer);
        Assert.Equal(new byte[] { 255, 0, 0, 128 }, AppearanceFixture.WorldPixel(ordinary, 8, 8));
        Assert.Equal(new byte[] { 170, 0, 85, 192 }, AppearanceFixture.WorldPixel(outer, 8, 8));
        Assert.Equal(new ChartRect(0, 0, 16, 16), ordinary.Bounds);
        Assert.Equal(ordinary.Bounds, outer.Bounds);
        Assert.Equal("inner-source", Assert.Single(ordinary.Visual.Regions).Id);
    }

    [Fact]
    public void ExistingExplicitSourceAtopPreservesDestinationAlphaAndSourceOverDoesNot() {
        var tint = CompositeOperatorFixture.Layer(ChartColor.FromRgba(0, 0, 255, 128));
        var atop = ImageComposition.Create(2, 2, ChartColor.FromRgba(255, 0, 0, 128))
            .DrawLayer(tint, 0, 0, VisualCompositeOperator.SourceAtop, orderedDither: false);
        var over = ImageComposition.Create(2, 2, ChartColor.FromRgba(255, 0, 0, 128))
            .DrawLayer(tint, 0, 0, VisualCompositeOperator.SourceOver, orderedDither: false);
        AppearanceFixture.Capture("inner-before-atop-control", atop);
        AppearanceFixture.Capture("inner-before-over-control", over);
        Assert.Equal(new byte[] { 127, 0, 128, 128 }, AppearanceFixture.Pixel(atop.ToImage(), 0, 0));
        Assert.Equal(new byte[] { 85, 0, 170, 192 }, AppearanceFixture.Pixel(over.ToImage(), 0, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void FiniteComplementThenAtopSourceChainRetainsSourceAlpha(double sigma) {
        var svg = InnerShadowFixture.FilterSource(sigma);
        var imported = SvgRasterizer.Rasterize(svg, strict: false, filters: new SvgRasterFilterOptions());
        NativeEffectBeforeCapture.Write("inner-source-before-" + sigma, svg, imported.Image.ToPng(), imported.Image);
        NativeEffectBeforeCapture.Json("inner-source-before-" + sigma + "-diagnostics", imported.Diagnostics);
        Assert.Empty(imported.Diagnostics);
        Assert.Equal(imported.Image.Pixels, SvgRasterizer.Rasterize(svg, strict: true, filters: new SvgRasterFilterOptions()).Image.Pixels);
        Assert.Equal(new byte[] { 128, 0, 127, 128 }, AppearanceFixture.Pixel(imported.Image, 8, 8));
        Assert.Equal(new byte[4], AppearanceFixture.Pixel(imported.Image, 3, 8));
    }
}

internal static class InnerShadowFixture {
    internal static readonly VisualRenderOptions Unit = new(supersampling: 1);
    internal static PreparedVisual Source(byte alpha) {
        var builder = new VisualSceneBuilder(new VisualSize(16, 16), FontSpec.SystemSans());
        builder.Rect(new ChartRect(4, 4, 8, 8), ChartColor.FromRgba(255, 0, 0, alpha));
        builder.AddRegion(new VisualSemanticRegion("inner-source", "sample", new ChartRect(4, 4, 8, 8), "Original source"));
        return new PreparedVisual(builder.Build());
    }

    internal static string FilterSource(double sigma) =>
        "<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'><defs>"
        + "<filter id='inner' x='0' y='0' width='16' height='16' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' color-interpolation-filters='sRGB'>"
        + "<feMorphology in='SourceAlpha' operator='erode' radius='0' result='spread'/>"
        + "<feGaussianBlur in='spread' stdDeviation='" + sigma.ToString("R", CultureInfo.InvariantCulture) + "' edgeMode='none' result='soft'/>"
        + "<feOffset in='soft' dx='0' dy='0' result='shifted'/><feFlood flood-color='#0000ff' result='tint'/>"
        + "<feComposite in='tint' in2='shifted' operator='out' result='innerColor'/>"
        + "<feComposite in='innerColor' in2='SourceGraphic' operator='atop'/></filter></defs>"
        + "<rect id='inner-source' x='4' y='4' width='8' height='8' fill='#ff0000' fill-opacity='0.5019607843137255' filter='url(#inner)'/></svg>";
}
