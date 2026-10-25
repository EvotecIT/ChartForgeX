using System;
using System.IO;
using System.Linq;
using System.Threading;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeCompositeOperatorTests {
    [Theory]
    [InlineData(VisualCompositeOperator.SourceOver, 170, 0, 85, 192, true)]
    [InlineData(VisualCompositeOperator.SourceIn, 255, 0, 0, 64, false)]
    [InlineData(VisualCompositeOperator.SourceOut, 255, 0, 0, 64, false)]
    [InlineData(VisualCompositeOperator.SourceAtop, 128, 0, 127, 128, true)]
    [InlineData(VisualCompositeOperator.DestinationOver, 85, 0, 170, 192, true)]
    [InlineData(VisualCompositeOperator.DestinationIn, 0, 0, 255, 64, false)]
    [InlineData(VisualCompositeOperator.DestinationOut, 0, 0, 255, 64, true)]
    [InlineData(VisualCompositeOperator.DestinationAtop, 127, 0, 128, 128, false)]
    [InlineData(VisualCompositeOperator.Xor, 128, 0, 128, 127, true)]
    [InlineData(VisualCompositeOperator.Copy, 255, 0, 0, 128, false)]
    [InlineData(VisualCompositeOperator.Clear, 0, 0, 0, 0, false)]
    public void AllOperatorsApplyTheDeclaredAlphaLawAcrossTheWholeDestinationFrame(VisualCompositeOperator operation,
        int red, int green, int blue, int alpha, bool retainsOutside) {
        var layer = CompositeOperatorFixture.Layer(ChartColor.FromRgba(255, 0, 0, 128));
        var composition = ImageComposition.Create(4, 3, ChartColor.FromRgba(0, 0, 255, 128));
        composition.DrawLayer(layer, 1, 1, operation, orderedDither: false);
        var image = composition.ToImage();
        CompositeOperatorFixture.Capture("composite-operator-" + operation, composition, layer);
        Assert.Equal(new byte[] { (byte)red, (byte)green, (byte)blue, (byte)alpha }, CompositeOperatorFixture.Pixel(image, 1, 1));
        var outside = retainsOutside ? new byte[] { 0, 0, 255, 128 } : new byte[4];
        Assert.Equal(outside, CompositeOperatorFixture.Pixel(image, 0, 0)); // Outside the placed bitmap.
        Assert.Equal(outside, CompositeOperatorFixture.Pixel(image, 2, 1)); // Transparent pixel inside that bitmap.
        Assert.Equal(4, image.Width);
        Assert.Equal(3, image.Height);
        Assert.Equal(new byte[] { 255, 0, 0, 128 }, CompositeOperatorFixture.Pixel(layer.ToImage(), 0, 0));
    }

    [Fact]
    public void ExplicitSourceOverRetainsTheExistingNormalBlendBytesIncludingOutsidePixels() {
        var layer = CompositeOperatorFixture.Layer(ChartColor.FromRgba(89, 37, 204, 1));
        var expected = ImageComposition.Create(4, 3, ChartColor.FromRgba(41, 73, 113, 23))
            .DrawLayer(layer, 1, 1, VisualBlendMode.Normal, orderedDither: false);
        var actual = ImageComposition.Create(4, 3, ChartColor.FromRgba(41, 73, 113, 23))
            .DrawLayer(layer, 1, 1, VisualCompositeOperator.SourceOver, orderedDither: false);
        CompositeOperatorFixture.Capture("composite-source-over-normal-control", actual, layer);
        Assert.Equal(expected.ToImage().Pixels, actual.ToImage().Pixels);
    }

    [Fact]
    public void CopyPreservesCapturedRawRgbaAndClearsEveryPixelOutsideItsPlacedExtent() {
        var layer = CompositeOperatorFixture.Layer(ChartColor.FromRgba(89, 37, 204, 1));
        var input = layer.ToImage();
        var actual = ImageComposition.Create(4, 3, ChartColor.White)
            .DrawLayer(layer, 1, 1, VisualCompositeOperator.Copy, orderedDither: true);
        CompositeOperatorFixture.Capture("composite-copy-raw", actual, layer);
        var image = actual.ToImage();
        for (var y = 0; y < input.Height; y++) for (var x = 0; x < input.Width; x++)
            Assert.Equal(CompositeOperatorFixture.Pixel(input, x, y), CompositeOperatorFixture.Pixel(image, x + 1, y + 1));
        Assert.Equal(new byte[4], CompositeOperatorFixture.Pixel(image, 0, 0));
        Assert.Equal(new byte[4], CompositeOperatorFixture.Pixel(image, 3, 2));
    }

    [Fact]
    public void RejectedGridDensityOperatorAndCancelledOperationsLeaveBothInputsUnchanged() {
        var layer = CompositeOperatorFixture.Layer(ChartColor.FromRgba(89, 37, 204, 128));
        var source = layer.ToImage().Pixels.ToArray();
        var actual = ImageComposition.Create(4, 3, ChartColor.White);
        var before = actual.ToPng();
        CompositeOperatorFixture.Capture("composite-atomic-rejection", actual, layer);
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.DrawLayer(layer, 0, 0, (VisualCompositeOperator)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.DrawLayer(layer, .5, 0, VisualCompositeOperator.SourceIn));
        var denser = layer.Source.CaptureLayer(new VisualLayerBlur(0), new VisualRenderOptions(scale: 2, supersampling: 1));
        Assert.Throws<ArgumentException>(() => actual.DrawLayer(denser, 0, 0, VisualCompositeOperator.SourceIn));
        Assert.Throws<OperationCanceledException>(() => actual.DrawLayer(layer, 0, 0, VisualCompositeOperator.DestinationOut,
            cancellationToken: new CancellationToken(true)));
        Assert.Equal(before, actual.ToPng());
        Assert.Equal(source, layer.ToImage().Pixels);
    }
}

internal static class CompositeOperatorFixture {
    internal static CapturedVisualLayer Layer(ChartColor color) {
        var builder = new VisualSceneBuilder(new VisualSize(2, 2), FontSpec.SystemSans());
        builder.Rect(new ChartRect(0, 0, 1, 1), color);
        builder.AddRegion(new VisualSemanticRegion("source", "composition-part", new ChartRect(0, 0, 1, 1), "Native source"));
        return new PreparedVisual(builder.Build()).CaptureLayer(new VisualLayerBlur(0), new VisualRenderOptions(supersampling: 1));
    }

    internal static byte[] Pixel(RgbaImage image, int x, int y) =>
        image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();

    internal static void Capture(string name, ImageComposition actual, CapturedVisualLayer source) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), actual.ToPng());
        File.WriteAllBytes(Path.Combine(output, name + ".source.png"), source.Visual.ToPng(new VisualRenderOptions(supersampling: 1)));
        File.WriteAllText(Path.Combine(output, name + ".source.svg"), source.Source.ToSvg());
    }
}
