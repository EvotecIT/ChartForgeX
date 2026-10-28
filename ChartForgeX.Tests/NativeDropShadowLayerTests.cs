using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeDropShadowLayerTests {
    [Theory]
    [InlineData(1)] [InlineData(-1)]
    public void WholeChildAlphaSpreadTintAndOriginalSourceCompositeExactlyOnce(int spread) {
        var source = ShadowFilterFixture.Source(); var before = source.ToSvg(); var png = source.ToPng(ShadowFilterFixture.Options);
        var layer = source.CaptureLayer(new VisualDropShadow(ChartColor.FromRgba(0, 0, 255, 128), 4, 2, 0,
            spread: spread, orderedDither: false), ShadowFilterFixture.Options);
        NativeEffectBeforeCapture.Write("native-shadow-spread-" + spread, layer.Visual.ToSvg(), layer.Visual.ToPng(), layer.ToImage());
        Assert.Equal(new byte[] { 255, 0, 0, 128 }, ShadowFilterFixture.WorldPixel(layer, 24, 17));
        Assert.Equal(spread < 0 ? new byte[4] : new byte[] { 0, 0, 255, 64 }, ShadowFilterFixture.WorldPixel(layer, 35, 21));
        Assert.Equal(160, ShadowFilterFixture.WorldPixel(layer, 29, 20)[3]);
        Assert.Equal(new byte[4], ShadowFilterFixture.WorldPixel(layer, 38, 21));
        Assert.Equal(new ChartRect(0, 0, 64, 48), layer.SourceBounds);
        Assert.Equal(spread > 0 ? new ChartRect(0, 0, 69, 51) : new ChartRect(0, 0, 68, 50), layer.Bounds);
        Assert.Same(source, layer.Source); Assert.Equal(before, source.ToSvg()); Assert.Equal(png, source.ToPng(ShadowFilterFixture.Options));
        Assert.Contains(layer.Visual.Diagnostics, item => item.Code == "effects.layer-baked");
        Assert.DoesNotContain("<filter", layer.Visual.ToSvg(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1)] [InlineData(2)]
    public void LogicalNegativeOffsetsAndPositiveSpreadUseOneDensityConversionAndDetachedSnapshots(int density) {
        var source = ShadowFilterFixture.Source(); var options = new VisualRenderOptions(scale: density, supersampling: 1);
        var effect = new VisualDropShadow(ChartColor.FromRgba(0, 0, 255, 128), -4, -2, 0, spread: 1, orderedDither: false);
        var layer = source.CaptureLayer(effect, options); var before = layer.Visual.ToSvg(); var pixels = layer.ToImage();
        NativeEffectBeforeCapture.Write("native-shadow-negative-density-" + density, before, layer.Visual.ToPng(options), pixels);
        Assert.Equal(new ChartRect(-5, -3, 69, 51), layer.Bounds); Assert.Equal(69 * density, pixels.Width); Assert.Equal(51 * density, pixels.Height);
        Assert.Equal(-4, effect.OffsetX); Assert.Equal(-2, effect.OffsetY); Assert.Equal(1, effect.Spread); Assert.Equal(density, layer.PixelsPerUnit);
        Assert.Equal("native-source", Assert.Single(layer.Visual.Regions).Id);
        Assert.Equal(5, layer.Visual.Regions[0].Bounds.X); Assert.Equal(3, layer.Visual.Regions[0].Bounds.Y);
        Array.Clear(pixels.Pixels, 0, pixels.Pixels.Length); Assert.Equal(before, layer.Visual.ToSvg());
        Assert.Contains(layer.ToImage().Pixels, value => value > 0);
        Assert.NotEmpty(SvgRasterizer.Rasterize(layer.Visual.ToSvg(), strict: true).Image.Pixels);
    }

    [Fact]
    public void TransparentTintSkipsConversionWhileCancellationAndRejectedCaptureNeverMutateSource() {
        var source = ShadowFilterFixture.Source(); var before = source.ToSvg(); var pixels = source.ToRgba(ShadowFilterFixture.Options).Pixels;
        var layer = source.CaptureLayer(new VisualDropShadow(ChartColor.Transparent, 4, -2, 3, spread: 2), ShadowFilterFixture.Options);
        NativeEffectBeforeCapture.Write("native-shadow-transparent", layer.Visual.ToSvg(), layer.Visual.ToPng(), layer.ToImage());
        Assert.Equal(new ChartRect(0, 0, 64, 48), layer.Bounds); Assert.Equal(pixels, layer.ToImage().Pixels);
        Assert.Throws<OperationCanceledException>(() => source.CaptureLayer(new VisualDropShadow(ChartColor.Black, 2, 3, 1),
            cancellationToken: new CancellationToken(true)));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CaptureLayer(new VisualDropShadow(ChartColor.Black, 2, 3, 1),
            new VisualRenderOptions(pixelBudget: 1)));
        Assert.Equal(before, source.ToSvg()); Assert.Equal(pixels, source.ToRgba(ShadowFilterFixture.Options).Pixels);
    }

    [Fact]
    public void InvalidLogicalRequestsAndUnionBoundsFailAtExistingFinitePreflightBoundary() {
        var source = ShadowFilterFixture.Source();
        Assert.Throws<ArgumentNullException>(() => source.CaptureLayer((VisualDropShadow)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualDropShadow(ChartColor.Black, 0, 0, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualDropShadow(ChartColor.Black, 0, 0, 0, spread: int.MinValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CaptureLayer(new VisualDropShadow(ChartColor.Black, 0, 0, 65),
            new VisualRenderOptions(scale: 2)));
        Assert.Throws<OverflowException>(() => source.CaptureLayer(new VisualDropShadow(ChartColor.Black, int.MaxValue, 0, 0),
            new VisualRenderOptions(scale: 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CaptureLayer(new VisualDropShadow(ChartColor.Black, 8192, 0, 0), ShadowFilterFixture.Options));
    }
}

internal static class ShadowFilterFixture {
    internal static readonly VisualRenderOptions Options = new(supersampling: 1);
    internal static PreparedVisual Source() {
        var builder = new VisualSceneBuilder(new VisualSize(64, 48), FontSpec.SystemSans());
        builder.Rect(new ChartRect(24, 16, 8, 8), ChartColor.FromRgba(255, 0, 0, 128));
        builder.AddRegion(new VisualSemanticRegion("native-source", "composition-part", new ChartRect(0, 0, 64, 48), "One original source"));
        return new PreparedVisual(builder.Build());
    }
    internal static byte[] WorldPixel(CapturedVisualLayer layer, int x, int y) {
        var image = layer.ToImage(); var px = checked((int)((x - layer.Bounds.X) * layer.PixelsPerUnit));
        var py = checked((int)((y - layer.Bounds.Y) * layer.PixelsPerUnit));
        return image.Pixels.Skip((py * image.Width + px) * 4).Take(4).ToArray();
    }
    internal static string Svg(bool erode = false, string sigma = "0", bool ancestor = false) {
        var node = "<rect id='source' x='24' y='16' width='8' height='8' fill='#ff0000' fill-opacity='0.5019607843137255' filter='url(#shadow)'/>";
        if (ancestor) node = "<g opacity='.5'>" + node + "</g>";
        return "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='48'><defs>"
            + "<filter id='shadow' x='0' y='0' width='64' height='48' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' color-interpolation-filters='sRGB'>"
            + "<feMorphology in='SourceAlpha' operator='" + (erode ? "erode" : "dilate") + "' radius='1' result='spread'/>"
            + "<feGaussianBlur in='spread' stdDeviation='" + sigma + "' edgeMode='none' result='soft'/>"
            + "<feOffset in='soft' dx='4' dy='2' result='shifted'/><feFlood flood-color='#0000ff' flood-opacity='.5' result='tint'/>"
            + "<feComposite in='tint' in2='shifted' operator='in' result='shadowColor'/>"
            + "<feMerge><feMergeNode in='shadowColor'/><feMergeNode in='SourceGraphic'/></feMerge></filter></defs>" + node + "</svg>";
    }
}
