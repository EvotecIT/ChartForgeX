using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeMaskLayerTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AlphaMaskChangesOnlyAlphaAndDetachesBothInputAndGetter(bool dither) {
        var source = MaskBlendFixture.Source(); var original = source.ToRgba(MaskBlendFixture.Options);
        var input = MaskBlendFixture.Solid(8, 6, new byte[] { 3, 179, 231, 64 });
        var mask = new VisualMask(input, orderedDither: dither); var stable = mask.ToImage().Pixels;
        Array.Clear(input.Pixels, 0, input.Pixels.Length); var getter = mask.ToImage(); Array.Clear(getter.Pixels, 0, getter.Pixels.Length);
        var layer = source.CaptureLayer(mask, MaskBlendFixture.Options); var actual = layer.ToImage();
        MaskBlendFixture.Capture("native-mask-alpha-rgb-" + dither, layer);
        Assert.Equal(stable, mask.ToImage().Pixels); Assert.Equal(new ChartRect(0, 0, 8, 6), mask.Bounds);
        var seen = 0;
        for (var at = 0; at < actual.Pixels.Length; at += 4) {
            if (actual.Pixels[at + 3] == 0) continue;
            seen++; Assert.Equal(original.Pixels.Skip(at).Take(3), actual.Pixels.Skip(at).Take(3));
            Assert.InRange(actual.Pixels[at + 3], 42, 44);
        }
        Assert.True(seen > 0); Assert.Same(source, layer.Source);
        Assert.Equal(original.Pixels, source.ToRgba(MaskBlendFixture.Options).Pixels);
    }
    [Theory]
    [InlineData(VisualMaskMode.Alpha, 255, 64, 64)] [InlineData(VisualMaskMode.Luminance, 255, 64, 14)]
    [InlineData(VisualMaskMode.Alpha, 1, 128, 1)]
    public void AlphaAndLuminanceHaveDistinctGoldenCoverageWithUnassociatedRgb(VisualMaskMode mode, byte sourceAlpha, byte maskAlpha, int alpha) {
        var source = new RgbaImage(1, 1, new byte[] { 37, 123, 219, sourceAlpha });
        var mask = new RgbaImage(1, 1, new byte[] { 255, 0, 0, maskAlpha });
        var plan = RasterEffectPlan.Mask(new RasterEffectRegion(0, 0, 1, 1), mask, 0, 0, mode);
        var result = RasterEffects.Execute(RasterEffectPreflight.Prepare(new[] { plan }, new[] { source }), 0, false, CancellationToken.None);
        MaskBlendFixture.CaptureImage("native-mask-mode-" + mode + "-" + sourceAlpha, result.Image);
        Assert.Equal(new byte[] { 37, 123, 219, (byte)alpha }, result.Image.Pixels);
    }
    [Fact]
    public void FullCoverageIsExactAndUncoveredPixelsAreTransparentWithoutShrinkingBounds() {
        var source = MaskBlendFixture.Source(); var raw = source.ToRgba(MaskBlendFixture.Options);
        var full = source.CaptureLayer(new VisualMask(MaskBlendFixture.Solid(8, 6, new byte[] { 0, 0, 0, 255 })), MaskBlendFixture.Options);
        var part = full.WithMask(new VisualMask(MaskBlendFixture.Solid(2, 2, new byte[] { 255, 0, 0, 255 }), new ChartPoint(3, 2), orderedDither: false));
        MaskBlendFixture.Capture("native-mask-full", full); MaskBlendFixture.Capture("native-mask-partial", part);
        Assert.Equal(raw.Pixels, full.ToImage().Pixels); Assert.Equal(full.Bounds, part.Bounds); Assert.Equal(full.SourceBounds, part.SourceBounds);
        Assert.Equal(new byte[4], MaskBlendFixture.Pixel(part.ToImage(), 2, 2));
        Assert.Equal(MaskBlendFixture.Pixel(raw, 3, 2), MaskBlendFixture.Pixel(part.ToImage(), 3, 2));
        Assert.Equal(new byte[4], MaskBlendFixture.Pixel(part.ToImage(), 5, 2)); Assert.Same(source, part.Source);
    }
    [Fact]
    public void MaskAfterShadowRetainsOverflowSourceFactsAndExactEmbeddedSnapshot() {
        var source = MaskBlendFixture.PublicSource(); var before = source.ToSvg();
        var shadow = source.CaptureLayer(new VisualDropShadow(ChartColor.Black, -2, 3, 1), MaskBlendFixture.Options);
        var mask = new VisualMask(MaskBlendFixture.Solid(160, 100, new byte[] { 0, 0, 0, 128 }), orderedDither: false);
        var layer = shadow.WithMask(mask); MaskBlendFixture.Capture("native-mask-after-shadow", layer);
        Assert.Equal(shadow.Bounds, layer.Bounds); Assert.Equal(shadow.SourceBounds, layer.SourceBounds); Assert.Same(source, layer.Source);
        Assert.Equal(source.Regions.Select(item => item.Id), layer.Visual.Regions.Select(item => item.Id)); Assert.Equal(before, source.ToSvg());
        var image = Assert.Single(XDocument.Parse(layer.Visual.ToSvg()).Descendants(), item => item.Name.LocalName == "image");
        var uri = image.Attribute("href")!.Value; var embedded = RasterImageDecoder.Decode(Convert.FromBase64String(uri.Substring(uri.IndexOf(',') + 1)));
        Assert.Equal(layer.ToImage().Pixels, embedded.Pixels); Assert.Empty(SvgRasterizer.Rasterize(layer.Visual.ToSvg(), strict: true).Diagnostics);
        Assert.DoesNotContain(XDocument.Parse(layer.Visual.ToSvg()).Descendants(), item => item.Name.LocalName is "script" or "filter" or "mask");
    }
    [Fact]
    public void RejectedDensityGridAndCanceledRequestsDoNotMutateOriginalOrPublishPartialPixels() {
        var source = MaskBlendFixture.PublicSource(); var before = source.ToSvg();
        var mask = new VisualMask(MaskBlendFixture.Solid(160, 100, new byte[] { 0, 0, 0, 128 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualMask(mask.ToImage(), new ChartPoint(.25, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualMask(mask.ToImage(), mode: (VisualMaskMode)99));
        Assert.Throws<ArgumentException>(() => source.CaptureLayer(mask, new VisualRenderOptions(scale: 2)));
        Assert.Throws<OperationCanceledException>(() => source.CaptureLayer(mask, cancellationToken: new CancellationToken(true)));
        var layer = source.CaptureLayer(mask, MaskBlendFixture.Options); var pixels = layer.ToImage().Pixels;
        Assert.Throws<OperationCanceledException>(() => layer.WithMask(mask, new CancellationToken(true)));
        Assert.Throws<ArgumentException>(() => layer.WithMask(new VisualMask(mask.ToImage(), pixelsPerUnit: 2)));
        Assert.Equal(before, source.ToSvg()); Assert.Equal(pixels, layer.ToImage().Pixels);
    }
    [Fact]
    public void AlphaDitherChangesNoRgbAndKeepsOriginalChildDevicePhase() {
        var source = MaskBlendFixture.Solid(8, 8, new byte[] { 37, 123, 219, 128 });
        var mask = MaskBlendFixture.Solid(8, 8, new byte[] { 1, 2, 3, 128 });
        var plan = RasterEffectPlan.Mask(new RasterEffectRegion(0, 0, 8, 8), mask, 0, 0, VisualMaskMode.Alpha);
        var translated = RasterEffectPlan.Mask(new RasterEffectRegion(0, 0, 8, 8), mask, 0, 0, VisualMaskMode.Alpha, 8, -8);
        var first = RasterEffects.Execute(RasterEffectPreflight.Prepare(new[] { plan }, new[] { source }), 0, true, CancellationToken.None).Image;
        var again = RasterEffects.Execute(RasterEffectPreflight.Prepare(new[] { translated }, new[] { source }), 0, true, CancellationToken.None).Image;
        MaskBlendFixture.CaptureImage("native-mask-alpha-dither", first);
        Assert.Equal(first.Pixels, again.Pixels);
        var alphas = new HashSet<byte>();
        for (var at = 0; at < first.Pixels.Length; at += 4) {
            Assert.Equal(new byte[] { 37, 123, 219 }, first.Pixels.Skip(at).Take(3)); alphas.Add(first.Pixels[at + 3]);
        }
        Assert.Equal(new byte[] { 64, 65 }, alphas.OrderBy(value => value));
    }

    [Fact]
    public void SecondaryBorrowedTailsAreCountedOnceAndWholeBatchRejectsBeforeExecution() {
        var borrowed = new byte[16]; borrowed[3] = 255;
        var source = new RgbaImage(1, 1, borrowed);
        var plan = RasterEffectPlan.Mask(new RasterEffectRegion(0, 0, 1, 1), source, 0, 0, VisualMaskMode.Alpha);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.Prepare(new[] { plan }, new[] { source }, maximumLiveBytes: 19));
        var exact = RasterEffectPreflight.Prepare(new[] { plan }, new[] { source }, maximumLiveBytes: 20);
        Assert.Equal(20, exact.ConservativeLiveBytes); // One borrowed16-byte allocation and one actual4-byte result.
        Assert.Equal(new byte[] { 0, 0, 0, 255 }, RasterEffects.Execute(exact, 0, false, CancellationToken.None).Image.Pixels);
        Assert.Throws<InvalidOperationException>(() => RasterEffects.Execute(exact, 0, false, CancellationToken.None));
        var other = new RgbaImage(1, 1, new byte[24]);
        var two = RasterEffectPlan.Mask(new RasterEffectRegion(0, 0, 1, 1), other, 0, 0, VisualMaskMode.Alpha);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.Prepare(new[] { two }, new[] { source }, maximumLiveBytes: 43));
        Assert.Equal(44, RasterEffectPreflight.Prepare(new[] { two }, new[] { source }, maximumLiveBytes: 44).ConservativeLiveBytes);
    }
}

internal static class MaskBlendFixture {
    internal static readonly VisualRenderOptions Options = new(supersampling: 1);
    internal static PreparedVisual Source(byte alpha = 173) {
        var builder = new VisualSceneBuilder(new VisualSize(8, 6), FontSpec.SystemSans());
        builder.Rect(new ChartRect(0, 0, 8, 6), ChartColor.FromRgba(37, 123, 219, alpha));
        builder.AddRegion(new VisualSemanticRegion("native-input", "sample", new ChartRect(0, 0, 8, 6), "Unassociated RGB"));
        return new PreparedVisual(builder.Build());
    }
    internal static PreparedVisual PublicSource() => Chart.Create().WithSize(160, 100).WithTitle("Requests")
        .AddLine("Ready", new[] { new ChartPoint(0, 1), new ChartPoint(1, 3), new ChartPoint(2, 2) })
        .Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(160, 100), 8),
            frame: new VisualFrame(showLegend: false, transparentBackground: true)));
    internal static RgbaImage Solid(int width, int height, byte[] pixel) {
        var bytes = new byte[checked(width * height * 4)];
        for (var at = 0; at < bytes.Length; at += 4) Buffer.BlockCopy(pixel, 0, bytes, at, 4);
        return new RgbaImage(width, height, bytes);
    }
    internal static byte[] Pixel(RgbaImage image, int x, int y) => image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();
    internal static void Capture(string stem, CapturedVisualLayer layer) => NativeEffectBeforeCapture.Write(stem, layer.Visual.ToSvg(), layer.Visual.ToPng(), layer.ToImage());
    internal static void CaptureImage(string stem, RgbaImage image) => NativeEffectBeforeCapture.Write(stem, null,
        ChartForgeX.Composition.ImageComposition.FromImage(image).ToPng(), image);
}
