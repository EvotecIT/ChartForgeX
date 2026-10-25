using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeLayerBlendTests {
    [Theory]
    [InlineData(VisualBlendMode.Normal, 170, 0, 85, 192)]
    [InlineData(VisualBlendMode.Multiply, 85, 0, 85, 192)]
    [InlineData(VisualBlendMode.Screen, 170, 0, 170, 192)]
    [InlineData(VisualBlendMode.PlusLighter, 128, 0, 128, 255)]
    public void HalfAlphaRedOverBlueDistinguishesBlendFromAdditiveCompositing(VisualBlendMode mode, int r, int g, int b, int a) {
        var builder = new VisualSceneBuilder(new VisualSize(8, 6), FontSpec.SystemSans());
        builder.Rect(new ChartRect(0, 0, 8, 6), ChartColor.FromRgba(255, 0, 0, 128));
        var source = new PreparedVisual(builder.Build());
        var layer = source.CaptureLayer(new VisualLayerBlur(0), MaskBlendFixture.Options);
        var original = layer.ToImage().Pixels;
        var composition = ImageComposition.Create(12, 10, ChartColor.FromRgba(0, 0, 255, 128));
        composition.DrawLayer(layer, 2, 2, mode, orderedDither: false); var actual = composition.ToImage();
        MaskBlendFixture.CaptureImage("native-blend-half-alpha-" + mode, actual);
        Assert.Equal(new byte[] { (byte)r, (byte)g, (byte)b, (byte)a }, MaskBlendFixture.Pixel(actual, 4, 4));
        Assert.Equal(new byte[] { 0, 0, 255, 128 }, MaskBlendFixture.Pixel(actual, 0, 0));
        Assert.Equal(original, layer.ToImage().Pixels);
    }
    [Fact]
    public void PlusLighterSaturatesAlphaAndChannelsWithoutSourceOverDarkening() {
        var source = new RgbaImage(1, 1, new byte[] { 255, 255, 255, 192 });
        var backdrop = new RgbaImage(1, 1, new byte[] { 255, 255, 255, 192 });
        var plan = RasterEffectPlan.Composite(new RasterEffectRegion(0, 0, 1, 1), source, 0, 0, VisualBlendMode.PlusLighter);
        var actual = RasterEffects.Execute(RasterEffectPreflight.Prepare(new[] { plan }, new[] { backdrop }), 0, false, CancellationToken.None).Image;
        MaskBlendFixture.CaptureImage("native-blend-plus-lighter-saturation", actual);
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, actual.Pixels);
    }
    [Fact]
    public void ClippedTranslatedSourceAndTransparentBackdropKeepPlacementAndOneSource() {
        var source = MaskBlendFixture.Source(255); var layer = source.CaptureLayer(new VisualLayerBlur(0), MaskBlendFixture.Options);
        var composition = ImageComposition.CreateTransparent(6, 6).DrawLayer(layer, -4, 2, VisualBlendMode.Screen, orderedDither: false);
        var actual = composition.ToImage(); MaskBlendFixture.CaptureImage("native-blend-clipped", actual);
        Assert.Equal(new byte[4], MaskBlendFixture.Pixel(actual, 0, 1));
        Assert.Equal(new byte[] { 37, 123, 219, 255 }, MaskBlendFixture.Pixel(actual, 0, 2));
        Assert.Equal(new byte[4], MaskBlendFixture.Pixel(actual, 4, 2));
    }
    [Fact]
    public void OldThreeArgumentDrawRetainsItsSelectedArithmeticAndFailuresAreAtomic() {
        var source = MaskBlendFixture.Source(128); var layer = source.CaptureLayer(new VisualLayerBlur(0), MaskBlendFixture.Options);
        var composition = ImageComposition.Create(16, 12, ChartColor.FromRgba(0, 0, 255, 128)); var before = (byte[])composition.ToImage().Pixels.Clone();
        Assert.Throws<OperationCanceledException>(() => composition.DrawLayer(layer, 0, 0, VisualBlendMode.Screen,
            cancellationToken: new CancellationToken(true)));
        Assert.Throws<ArgumentOutOfRangeException>(() => composition.DrawLayer(layer, .25, 0, VisualBlendMode.Normal));
        Assert.Throws<ArgumentOutOfRangeException>(() => composition.DrawLayer(layer, 0, 0, (VisualBlendMode)99));
        var dense = source.CaptureLayer(new VisualLayerBlur(0), new VisualRenderOptions(scale: 2, supersampling: 1));
        Assert.Throws<ArgumentException>(() => composition.DrawLayer(dense, 0, 0, VisualBlendMode.Normal)); Assert.Equal(before, composition.ToImage().Pixels);
        var old = composition.DrawLayer(layer, 2, 2).ToImage();
        var expected = ImageComposition.FromImage(new RgbaImage(16, 12, before)).DrawImage(layer.ToImage(), 2, 2, 8, 6).ToImage();
        MaskBlendFixture.CaptureImage("native-blend-legacy-control", old); Assert.Equal(expected.Pixels, old.Pixels);
    }
}
