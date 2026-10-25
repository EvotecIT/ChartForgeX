using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>One explicit prepared child is captured atomically through existing native/effect/image owners.</summary>
public sealed class CapturedVisualLayerTests {
    [Fact]
    public void CompleteMarkTextAndDecorationPaintIsFilteredTogetherExactlyOnce() {
        var source = Source(); var options = new VisualRenderOptions(supersampling: 1); var pixels = source.ToRgba(options);
        var expected = RasterEffects.Execute(RasterEffectPreflight.Prepare(new[] {
            RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, pixels.Width, pixels.Height), 512, 256)
        }, new[] { pixels }), 0, true, CancellationToken.None);
        var layer = source.CaptureLayer(new VisualLayerBlur(2, 1), options);
        Assert.Equal(expected.Image.Pixels, layer.ToImage().Pixels);
        Assert.Equal((double)expected.Region.X, layer.Bounds.X); Assert.Equal((double)expected.Region.Y, layer.Bounds.Y);
        Assert.Same(source, layer.Source); Assert.Equal(source.Regions.Count, layer.Visual.Regions.Count);
        Assert.DoesNotContain("filter=", layer.Visual.ToSvg(), StringComparison.Ordinal);
    }
    [Theory]
    [InlineData(1, 1)] [InlineData(2, 3)] [InlineData(0, 0)]
    public void CompleteSourceFrameAndPropagatedSupportAreDistinct(double x, double y) {
        var source = Source(); var layer = source.CaptureLayer(new VisualLayerBlur(x, y), new VisualRenderOptions(supersampling: 1));
        var expected = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 80, 48), (int)(x * 256), (int)(y * 256)).ResultRegion;
        Assert.Equal(new ChartRect(0, 0, 80, 48), layer.SourceBounds);
        Assert.Equal(new ChartRect(expected.X, expected.Y, expected.Width, expected.Height), layer.Bounds);
        Assert.Equal(expected.Width, layer.ToImage().Width); Assert.Equal(expected.Height, layer.ToImage().Height);
        Assert.Equal(source.Accessibility.Name, layer.Visual.Accessibility.Name);
    }
    [Theory]
    [InlineData(0)] [InlineData(.001)]
    public void RoundedZeroSigmaKeepsExactCapturedPixelsAndOriginalExports(double sigma) {
        var source = Source(); var options = new VisualRenderOptions(supersampling: 1); var svg = source.ToSvg(); var png = source.ToPng(options);
        var layer = source.CaptureLayer(new VisualLayerBlur(sigma), options);
        Assert.Null(layer.Plan.Effect); Assert.Equal(source.ToRgba(options).Pixels, layer.ToImage().Pixels);
        Assert.Equal(svg, source.ToSvg()); Assert.Equal(png, source.ToPng(options));
    }
    [Fact]
    public void LogicalSigmaIsConvertedToCaptureDensityExactlyOnce() {
        var source = Source(); var options = new VisualRenderOptions(scale: 2, supersampling: 1); var pixels = source.ToRgba(options);
        var expected = RasterEffects.Execute(RasterEffectPreflight.Prepare(new[] {
            RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 160, 96), 512, 1024)
        }, new[] { pixels }), 0, false, CancellationToken.None);
        var layer = source.CaptureLayer(new VisualLayerBlur(1, 2, orderedDither: false), options);
        Assert.Equal(2, layer.PixelsPerUnit); Assert.Equal(expected.Image.Pixels, layer.ToImage().Pixels);
        Assert.Equal((double)expected.Region.X / 2, layer.Bounds.X); Assert.Equal((double)expected.Region.Width / 2, layer.Bounds.Width);
    }
    [Fact]
    public void CaptureBudgetAndCancellationPublishNothingAndDoNotReplaceTheSource() {
        var source = Source(); var before = source.ToSvg(); var pixels = source.ToPng();
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CaptureLayer(new VisualLayerBlur(65), new VisualRenderOptions(scale: 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CaptureLayer(new VisualLayerBlur(1), new VisualRenderOptions(pixelBudget: 10)));
        var tooWide = new PreparedVisual(new VisualScene(new VisualSize(8193, 1), Array.Empty<VisualSceneNode>(), Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => tooWide.CaptureLayer(new VisualLayerBlur(1), new VisualRenderOptions(supersampling: 1)));
        var plan = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 1, 1), 0, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterEffectPreflight.ValidateCapture(plan, 4, 21, 0, out _, out _, maximumLiveBytes: 44));
        Assert.Throws<OperationCanceledException>(() => source.CaptureLayer(new VisualLayerBlur(2), cancellationToken: new CancellationToken(true)));
        Assert.Equal(before, source.ToSvg()); Assert.Equal(pixels, source.ToPng());
    }
    [Fact]
    public void PrivateSnapshotPixelsStayDetachedAndTheSvgEmbedsTheirExactPng() {
        var source = Source(); var layer = source.CaptureLayer(new VisualLayerBlur(2), new VisualRenderOptions(supersampling: 1));
        var svg = layer.Visual.ToSvg(); var image = layer.ToImage(); var expected = (byte[])image.Pixels.Clone();
        Array.Clear(image.Pixels, 0, image.Pixels.Length); Assert.Equal(expected, layer.ToImage().Pixels); Assert.Equal(svg, layer.Visual.ToSvg());
        var element = Assert.Single(XDocument.Parse(svg).Descendants(), node => node.Name.LocalName == "image");
        var uri = element.Attribute("href")!.Value; Assert.StartsWith("data:image/png;base64,", uri);
        Assert.Equal(expected, RasterImageDecoder.Decode(Convert.FromBase64String(uri.Substring(uri.IndexOf(',') + 1))).Pixels);
        Assert.DoesNotContain(XDocument.Parse(svg).Descendants(), node => node.Name.LocalName is "filter" or "script" or "foreignObject");
        Assert.Null(layer.Visual.XProjection); Assert.Equal(source.Regions[0].Id, layer.Visual.Regions[0].Id);
    }
    [Fact]
    public void BakedImageImportIsStrictWhileAnActualUnsupportedFilterStillRejects() {
        var layer = Source().CaptureLayer(new VisualLayerBlur(1), new VisualRenderOptions(supersampling: 1));
        Assert.Empty(SvgRasterizer.Rasterize(layer.Visual.ToSvg(), strict: true).Diagnostics);
        const string unsupported = "<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><defs><filter id='b'><feGaussianBlur stdDeviation='1'/></filter></defs><rect width='10' height='10' filter='url(#b)'/></svg>";
        Assert.Contains(SvgRasterizer.Rasterize(unsupported).Diagnostics, item => item.Code == "SFR002");
        Assert.Contains("SFR002", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(unsupported, strict: true)).Message);
    }
    [Theory]
    [InlineData(1, 1)] [InlineData(2, .5)]
    public void CompositionPlacesTheOriginalSourceFrameAndRetainsBlurOverflow(int density, double sigma) {
        var builder = new VisualSceneBuilder(new VisualSize(3, 3), FontSpec.SystemSans()); builder.Rect(new ChartRect(0, 0, 3, 3), ChartColor.FromHex("#2166AC"));
        var source = new PreparedVisual(builder.Build()); var layer = source.CaptureLayer(new VisualLayerBlur(sigma), new VisualRenderOptions(scale: density, supersampling: 1));
        var control = new VisualSceneBuilder(new VisualSize(12, 12), FontSpec.SystemSans());
        control.Image(layer.ToImage(), new ChartRect(4 + layer.Bounds.X, 4 + layer.Bounds.Y, layer.Bounds.Width, layer.Bounds.Height));
        var expected = new PreparedVisual(control.Build()).ToRgba(new VisualRenderOptions(scale: density, supersampling: 1));
        var actual = ImageComposition.CreateScaled(12, 12, ChartColor.Transparent, density).DrawLayer(layer, 4, 4).ToImage();
        Assert.Equal(expected.Pixels, actual.Pixels);
        var edge = density == 1 ? 3 : 7;
        Assert.True(actual.Pixels[(edge * actual.Width + edge) * 4 + 3] > 0); Assert.Equal(0, actual.Pixels[3]);
    }
    [Fact]
    public void SharedSceneImagesCountOnceAndCaptureEffectSnapshotArraysAreReservedTogether() {
        var data = new VisualSceneImageData(new RgbaImage(1, 1, new byte[] { 255, 0, 0, 255 }));
        var builder = new VisualSceneBuilder(new VisualSize(8, 8), FontSpec.SystemSans()); builder.Image(data, new ChartRect(0, 0, 1, 1)); builder.Image(data, new ChartRect(2, 0, 1, 1));
        var plan = VisualSceneLayerCapturePlan.Create(builder.Build(), new VisualLayerBlur(1), new VisualRenderOptions(supersampling: 1));
        var effect = plan.Effect!;
        Assert.Equal(8 * 8 * 4, plan.SourcePixelBytes);
        Assert.Equal(8 * 8 * 4 + effect.ResultRegion.Width * effect.ResultRegion.Height * 4 + 4, plan.AdditionalPixelBytes);
        Assert.Equal(plan.SourcePixelBytes + plan.AdditionalPixelBytes + effect.AllocatedBytes, plan.ConservativePixelBytes);
        Assert.Equal(plan.AdditionalWorkSamples + effect.WorkSamples, plan.WorkSamples);
    }
    private static PreparedVisual Source() {
        var builder = new VisualSceneBuilder(new VisualSize(80, 48), FontSpec.SystemSans());
        builder.Rect(new ChartRect(4, 4, 24, 24), ChartColor.FromRgba(37, 123, 219, 173), radius: 4);
        var style = new TextStyle { FontSize = 15, Color = ChartColor.FromHex("#B2182B"), Underline = true };
        builder.Text("Layer", 30, 28, style, "layer-caption", "caption");
        builder.AddRegion(new VisualSemanticRegion("content", "composition-part", new ChartRect(4, 4, 70, 30), "Layer"));
        return new PreparedVisual(builder.Build(), new ChartForgeX.Accessibility.VisualAccessibility { Name = "Captured native layer" });
    }
}
