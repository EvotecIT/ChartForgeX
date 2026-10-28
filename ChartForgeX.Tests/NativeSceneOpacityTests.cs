using System.Xml.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeSceneOpacityTests {
    [Theory]
    [InlineData(0)] [InlineData(.5)] [InlineData(1)]
    public void WholeGroupCompositesOverlappingChildrenOnceAndRetainsNativeSvg(double opacity) {
        var scene = Wrap(NativeOpacityDefaultControls.Scene(3), opacity); var prepared = new PreparedVisual(scene);
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        Capture(prepared, "group-" + opacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal((byte)Math.Round(255 * opacity), Alpha(image, 10, 10));
        Assert.Equal((byte)Math.Round(255 * opacity), Alpha(image, 3, 3));
        var imported = SvgRasterizer.Rasterize(prepared.ToSvg("native-opacity-oracle"), strict: true).Image;
        Assert.Equal(Alpha(imported, 10, 10), Alpha(image, 10, 10));
        var svg = XDocument.Parse(prepared.ToSvg("native-opacity"));
        var group = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "isolated");
        Assert.Equal(opacity == 1 ? null : opacity.ToString(System.Globalization.CultureInfo.InvariantCulture), (string?)group.Attribute("opacity"));
        if (opacity == .5) { Assert.Equal(128, Alpha(image, 10, 10)); Assert.NotEqual(192, Alpha(image, 10, 10)); }
    }

    [Theory]
    [InlineData("rect")] [InlineData("ellipse")] [InlineData("path")]
    public void OneElementCompositesItsOwnFillStrokeOverlapOnce(string kind) {
        var builder = new VisualSceneBuilder(new VisualSize(32, 24), FontSpec.SystemSans());
        var color = ChartColor.FromHex("#CC2030");
        if (kind == "rect") builder.Rect(new ChartRect(4, 4, 16, 16), color, color, 6, role: "element", opacity: .5);
        else if (kind == "ellipse") builder.Ellipse(12, 12, 8, 8, color, color, 6, "element", opacity: .5);
        else builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(4, 4), ChartPathCommand.LineTo(20, 4), ChartPathCommand.LineTo(20, 20), ChartPathCommand.LineTo(4, 20) }), color, color, 6, "element", close: true, opacity: .5);
        var prepared = new PreparedVisual(builder.Build()); var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        Capture(prepared, "element-" + kind);
        Assert.Equal(128, Alpha(image, kind == "ellipse" ? 18 : 5, kind == "ellipse" ? 12 : 5));
        Assert.Contains("opacity=\"0.5\"", prepared.ToSvg("native-opacity"));
        var imported = SvgRasterizer.Rasterize(prepared.ToSvg("native-opacity-oracle"), strict: true).Image;
        Assert.Equal(Alpha(imported, kind == "ellipse" ? 18 : 5, kind == "ellipse" ? 12 : 5), Alpha(image, kind == "ellipse" ? 18 : 5, kind == "ellipse" ? 12 : 5));
    }

    [Fact]
    public void SeparateElementsCompositeSeparatelyWhileNestedObjectOpacityCompositesEachCompletedObject() {
        var builder = new VisualSceneBuilder(new VisualSize(32, 24), FontSpec.SystemSans()); var red = ChartColor.FromHex("#CC2030");
        builder.Rect(new ChartRect(2, 2, 16, 16), red, opacity: .5); builder.Rect(new ChartRect(8, 4, 16, 16), red, opacity: .5);
        var separate = new PreparedVisual(builder.Build()); Capture(separate, "separate-elements");
        Assert.Equal(192, Alpha(separate.ToRgba(new VisualRenderOptions(supersampling: 1)), 10, 10));
        var nested = new PreparedVisual(Wrap(Wrap(NativeOpacityDefaultControls.Scene(3), .5), .5)); Capture(nested, "nested-groups");
        Assert.Equal(64, Alpha(nested.ToRgba(new VisualRenderOptions(supersampling: 1)), 10, 10));
    }

    [Theory]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void NativeClipTransformCapturedTextAndImagesShareTheSameCompletedGroupPaint(int sample) {
        var original = NativeOpacityDefaultControls.Scene(sample); var source = new PreparedVisual(original);
        var actual = new PreparedVisual(Wrap(original, .5)); var full = source.ToRgba(new VisualRenderOptions(supersampling: 1));
        var image = actual.ToRgba(new VisualRenderOptions(supersampling: 1)); Capture(actual, "native-owner-" + sample);
        for (var index = 0; index < full.Pixels.Length; index += 4) {
            Assert.Equal((byte)Math.Round(full.Pixels[index + 3] * .5), image.Pixels[index + 3]);
            if (image.Pixels[index + 3] != 0) for (var channel = 0; channel < 3; channel++) Assert.Equal(full.Pixels[index + channel], image.Pixels[index + channel]);
        }
    }

    [Fact]
    public void AncestorClipIsAppliedOnceWhenItsChildGroupIsIsolated() {
        var builder = new VisualSceneBuilder(new VisualSize(32, 24), FontSpec.SystemSans());
        using (builder.PushClip(new ChartRect(4, 4, 8, 8))) using (builder.PushGroup(null, "child-opacity", opacity: .5))
            builder.Rect(new ChartRect(0, 0, 24, 24), ChartColor.FromHex("#CC2030"));
        var prepared = new PreparedVisual(builder.Build()); var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        Capture(prepared, "ancestor-clip"); Assert.Equal(128, Alpha(image, 5, 5)); Assert.Equal(0, Alpha(image, 2, 2));
    }

    [Fact]
    public void DefaultAndZeroNeedNoPrivateLayerWhileAllDeclaredNestedAndSequentialAllocationsAreReserved() {
        var source = NativeOpacityDefaultControls.Scene(3); var allocation = VisualSceneRasterRenderer.CalculateAllocation(source.Size, 1, 1, 64000000);
        Assert.Equal((0L, 0L), VisualSceneRasterRenderer.OpacityCaptureExtras(source, allocation));
        Assert.Equal((0L, 0L), VisualSceneRasterRenderer.OpacityCaptureExtras(Wrap(source, 0), allocation));
        var nested = Wrap(Wrap(source, .5), .5); var extra = VisualSceneRasterRenderer.OpacityCaptureExtras(nested, allocation);
        Assert.Equal(2L * allocation.ByteCount, extra.PixelBytes);
        var options = new VisualRenderOptions(supersampling: 1);
        var normal = VisualSceneLayerCapturePlan.Create(source, new VisualLayerBlur(0), options);
        var isolated = VisualSceneLayerCapturePlan.Create(nested, new VisualLayerBlur(0), options);
        Assert.Equal(extra.PixelBytes, isolated.AdditionalPixelBytes - normal.AdditionalPixelBytes);
        Assert.Equal(extra.WorkSamples, isolated.AdditionalWorkSamples - normal.AdditionalWorkSamples);
        Assert.Equal(4, VisualSceneRasterRenderer.BorrowedNativeImageBytes(NativeOpacityDefaultControls.Scene(7)));
    }

    [Fact]
    public void CumulativeOffscreenReservationAndExcessiveIsolationDepthRejectBeforeCanvasAllocation() {
        var source = NativeOpacityDefaultControls.Scene(0); var nodes = new List<VisualSceneNode>();
        for (var index = 0; index < 65; index++) { nodes.Add(Group(.5)); nodes.AddRange(source.Nodes); nodes.Add(new VisualSceneEndGroup()); }
        var large = new PreparedVisual(new VisualScene(new VisualSize(1024, 1024), nodes, source.Diagnostics, source.Regions));
        Assert.Throws<ArgumentOutOfRangeException>(() => large.ToRgba(new VisualRenderOptions(supersampling: 1)));
        var nested = source; for (var index = 0; index < 65; index++) nested = Wrap(nested, .5);
        Assert.Throws<NotSupportedException>(() => new PreparedVisual(nested).ToRgba(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void DefaultNativeIdentityRemainsExactWhileNondefaultOpacityChangesIdentityAndRetainedValuesRejectInvalidNumbers() {
        var source = NativeOpacityDefaultControls.Scene(3); var a = new PreparedVisual(Wrap(source, 1)); var b = new PreparedVisual(Wrap(source, .5));
        Assert.NotEqual(a.ToSvg(), b.ToSvg()); Assert.Equal(a.ToSvg(), new PreparedVisual(Wrap(source, 1)).ToSvg());
        foreach (var opacity in new[] { -1d, 1.01, double.NaN, double.PositiveInfinity }) Assert.Throws<ArgumentOutOfRangeException>(() => Wrap(source, opacity));
    }

    private static VisualSceneGroup Group(double opacity) => new("isolated", null, null, null, null, null, null, null, null, null, null, opacity);
    private static VisualScene Wrap(VisualScene source, double opacity) => new(source.Size,
        new[] { Group(opacity) }.Cast<VisualSceneNode>().Concat(source.Nodes).Concat(new[] { new VisualSceneEndGroup() }).ToArray(), source.Diagnostics, source.Regions, source.DataProjection);
    private static byte Alpha(ChartForgeX.Raster.RgbaImage image, int x, int y) => image.Pixels[(y * image.Width + x) * 4 + 3];
    private static void Capture(PreparedVisual prepared, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, "native-opacity-after-" + name);
        File.WriteAllText(stem + ".svg", prepared.ToSvg()); File.WriteAllText(stem + ".fixed.svg", prepared.ToSvg("native-opacity"));
        File.WriteAllBytes(stem + ".png", prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }
}
