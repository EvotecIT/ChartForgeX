using System;
using System.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Current public capture/export workflows. Actual before/after results are recorded by the runtime owner.</summary>
public sealed class CapturedLayerSampleBoundaryTests {
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(255)]
    public void CapturedOffsetTintKeepsExactCompletedPixelsAcrossNativeVisualAndDecodedPng(int alpha) {
        var source = Source();
        var layer = source.CaptureLayer(new VisualDropShadow(ChartColor.FromRgba(11, 29, 248, 255),
            6, 0, 0, orderedDither: false, opacity: alpha / 255d), Unit);
        var raw = layer.ToImage();
        var visual = layer.Visual.ToRgba(Unit);
        var png = layer.Visual.ToPng(Unit);
        // Common artifact export uses its incumbent default raster coverage. The explicit
        // prepared export above owns the unit-coverage raw-pixel comparison.
        var artifactPng = layer.Visual.ToArtifact("captured-pixel-boundary").ToPng();
        var defaultVisual = layer.Visual.ToRgba();
        NativeEffectBeforeCapture.Write("captured-layer-unit-" + alpha, layer.Visual.ToSvg(), png, raw);
        NativeEffectBeforeCapture.Json("captured-layer-unit-" + alpha + "-pixels", new {
            raw = raw.Pixels, visual = visual.Pixels, raw.Width, raw.Height, layer.Bounds
        });
        Assert.True(RasterImageDecoder.TryDecode(png, out var decoded));
        Assert.True(RasterImageDecoder.TryDecode(artifactPng, out var artifactDecoded));
        Assert.Equal((byte)alpha, Pixel(raw, 9, 3)[3]);
        Assert.Equal(raw.Width, visual.Width); Assert.Equal(raw.Height, visual.Height);
        Assert.Equal(raw.Pixels, visual.Pixels);
        Assert.Equal(raw.Pixels, decoded.Pixels);
        Assert.Equal(defaultVisual.Pixels, artifactDecoded.Pixels);
        Assert.Equal(Pixel(raw, 9, 3), Pixel(artifactDecoded, 9, 3));
        var retained = layer.Visual.ToPng(Unit);
        Array.Clear(raw.Pixels, 0, raw.Pixels.Length);
        Assert.Equal(retained, layer.Visual.ToPng(Unit));
    }

    [Fact]
    public void OrdinaryExternalImageStillUsesItsExistingDataAndRasterPaths() {
        var image = new RgbaImage(1, 1, new byte[] { 11, 29, 248, 2 });
        var direct = new VisualSceneBuilder(new VisualSize(8, 6), FontSpec.SystemSans());
        direct.Image(image, new ChartRect(2, 2, 1, 1));
        var retained = new VisualSceneBuilder(new VisualSize(8, 6), FontSpec.SystemSans());
        retained.Image(new VisualSceneImageData(image), new ChartRect(2, 2, 1, 1));
        var first = new PreparedVisual(direct.Build());
        var second = new PreparedVisual(retained.Build());
        NativeEffectBeforeCapture.Write("captured-layer-ordinary-image-control", first.ToSvg(), first.ToPng(Unit), first.ToRgba(Unit));
        Assert.Equal(first.ToSvg(), second.ToSvg());
        Assert.Equal(first.ToRgba(Unit).Pixels, second.ToRgba(Unit).Pixels);
        Assert.Equal(first.ToPng(Unit), second.ToPng(Unit));
        Assert.True(RasterImageDecoder.TryDecode(first.ToPng(Unit), out var decoded));
        Assert.Equal(first.ToRgba(Unit).Pixels, decoded.Pixels);
    }

    private static readonly VisualRenderOptions Unit = new(supersampling: 1);
    private static PreparedVisual Source() {
        var builder = new VisualSceneBuilder(new VisualSize(16, 12), FontSpec.SystemSans());
        builder.Rect(new ChartRect(2, 2, 4, 4), ChartColor.FromRgba(255, 0, 0, 255));
        builder.AddRegion(new VisualSemanticRegion("source", "sample", new ChartRect(2, 2, 4, 4), "Original source"));
        return new PreparedVisual(builder.Build());
    }
    private static byte[] Pixel(RgbaImage image, int x, int y) => image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();
}
