using System;
using System.Linq;
using System.Threading;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeStandardBlendTests {
    [Theory]
    [InlineData(VisualBlendMode.Overlay, 90, 33, 240, 126, 64, 219)]
    [InlineData(VisualBlendMode.Darken, 48, 32, 192, 112, 64, 203)]
    [InlineData(VisualBlendMode.Lighten, 240, 128, 224, 176, 96, 213)]
    [InlineData(VisualBlendMode.ColorDodge, 255, 146, 255, 181, 102, 224)]
    [InlineData(VisualBlendMode.ColorBurn, 35, 0, 214, 108, 53, 210)]
    [InlineData(VisualBlendMode.HardLight, 231, 32, 240, 173, 64, 219)]
    [InlineData(VisualBlendMode.SoftLight, 103, 80, 232, 130, 80, 216)]
    [InlineData(VisualBlendMode.Difference, 192, 96, 32, 160, 85, 149)]
    [InlineData(VisualBlendMode.Exclusion, 198, 128, 79, 162, 96, 165)]
    [InlineData(VisualBlendMode.Hue, 223, 47, 182, 170, 69, 199)]
    [InlineData(VisualBlendMode.Saturation, 36, 130, 244, 108, 97, 220)]
    [InlineData(VisualBlendMode.Color, 243, 35, 195, 177, 65, 204)]
    [InlineData(VisualBlendMode.Luminosity, 45, 125, 221, 111, 95, 213)]
    public void OpaquePartialAndTransparentInputsUseOneGammaChannelAndAlphaOwner(VisualBlendMode mode,
        int red, int green, int blue, int partialRed, int partialGreen, int partialBlue) {
        foreach (var alpha in new byte[] { 255, 128 }) {
            var layer = Layer(alpha);
            var sourceBytes = layer.ToImage().Pixels.ToArray();
            var actual = ImageComposition.Create(4, 3, ChartColor.FromRgba(48, 128, 224, alpha))
                .DrawLayer(layer, 1, 1, mode, orderedDither: false);
            CompositeOperatorFixture.Capture("standard-native-" + mode + "-" + alpha, actual, layer);
            var expected = alpha == 255 ? new[] { red, green, blue, 255 }
                : new[] { partialRed, partialGreen, partialBlue, 192 };
            Assert.Equal(expected.Select(item => (byte)item), CompositeOperatorFixture.Pixel(actual.ToImage(), 1, 1));
            Assert.Equal(new byte[] { 48, 128, 224, alpha }, CompositeOperatorFixture.Pixel(actual.ToImage(), 0, 0));
            Assert.Equal(new byte[] { 240, 32, 192, alpha }, CompositeOperatorFixture.Pixel(layer.ToImage(), 0, 0));
            Assert.Equal(sourceBytes, layer.ToImage().Pixels);
            Assert.Contains(layer.Source.Regions, region => region.Id == "source");
        }
        var transparent = Layer(0);
        var destination = ImageComposition.Create(4, 3, ChartColor.FromRgba(48, 128, 224, 128));
        var unchanged = destination.ToImage().Pixels;
        destination.DrawLayer(transparent, 1, 1, mode, orderedDither: false);
        Assert.Equal(unchanged, destination.ToImage().Pixels);
        var sourceOnly = ImageComposition.CreateTransparent(4, 3).DrawLayer(Layer(128), 1, 1, mode, orderedDither: false);
        Assert.Equal(new byte[] { 240, 32, 192, 128 }, CompositeOperatorFixture.Pixel(sourceOnly.ToImage(), 1, 1));
    }

    [Fact]
    public void NewModesKeepAtomicFailureAndTheExistingDeclaredPixelBudget() {
        var source = Layer(128);
        var actual = ImageComposition.Create(4, 3, ChartColor.FromRgba(48, 128, 224, 128));
        var original = actual.ToPng();
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.DrawLayer(source, 0, 0, (VisualBlendMode)17));
        Assert.Throws<OperationCanceledException>(() => actual.DrawLayer(source, 0, 0, VisualBlendMode.Hue,
            cancellationToken: new CancellationToken(true)));
        Assert.Equal(original, actual.ToPng());
        var back = new RgbaImage(1, 1, new byte[] { 48, 128, 224, 128 });
        var top = new RgbaImage(1, 1, new byte[] { 240, 32, 192, 128 });
        var plan = RasterEffectPlan.Composite(new RasterEffectRegion(0, 0, 1, 1), top, 0, 0, VisualBlendMode.SoftLight);
        // Two borrowed RGBA pixels plus the one result pixel; no per-mode scratch arrays.
        var accepted = RasterEffects.Execute(RasterEffectPreflight.Prepare(new[] { plan }, new[] { back },
            maximumLiveBytes: 12), 0, false, CancellationToken.None);
        Assert.Equal(new byte[] { 130, 80, 216, 192 }, accepted.Image.Pixels);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.Prepare(new[] { plan }, new[] { back }, maximumLiveBytes: 11));
    }

    internal static ChartForgeX.Rendering.CapturedVisualLayer Layer(byte alpha) {
        var source = CompositeOperatorFixture.Layer(ChartColor.FromRgb(240, 32, 192));
        if (alpha == 255) return source;
        var pixels = new byte[16];
        for (var at = 0; at < pixels.Length; at += 4) {
            pixels[at] = pixels[at + 1] = pixels[at + 2] = 255;
            pixels[at + 3] = alpha;
        }
        return source.WithMask(new VisualMask(new RgbaImage(2, 2, pixels), orderedDither: false));
    }
}
