using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Native scene paint retains detached gradients, holes and rigid transformed geometry.</summary>
public sealed class V2ScenePaintTests {
    [Fact]
    public void ChildScenePlacementTranslatesPaintAndNamespacedRegionsWithoutChangingSnapshot() {
        var child = Builder();
        child.Rect(new ChartRect(2, 3, 8, 6), ChartColor.Black);
        child.AddRegion(new VisualSemanticRegion("value", "datum", new ChartRect(2, 3, 8, 6), "Source value"));
        var scene = child.Build(); var before = VisualSceneSvgRenderer.Render(scene);
        var parent = Builder();
        using (parent.PushClip(new ChartRect(12, 13, 5, 6))) parent.Append(scene, 10, 10, "left");
        parent.Append(scene, 25, 20, "right");
        var combined = parent.Build(); var image = VisualSceneRasterRenderer.Render(combined, supersampling: 1);
        Assert.Equal(255, Pixel(image, 13, 14)[3]); Assert.Equal(0, Pixel(image, 18, 14)[3]);
        Assert.Equal(255, Pixel(image, 28, 24)[3]); Assert.Equal(0, Pixel(image, 2, 3)[3]);
        Assert.Equal(new[] { "left/value", "right/value" }, combined.Regions.Select(region => region.Id));
        Assert.Equal(27, combined.Regions[1].Bounds.X);
        Assert.Contains("translate(25 20)", VisualSceneSvgRenderer.Render(combined));
        Assert.Equal(before, VisualSceneSvgRenderer.Render(scene));
    }
    [Fact]
    public void ResolvedImagesDetachPixelsAndShareLosslessSvgAndNativePaint() {
        var pixels = new byte[] { 200, 20, 30, 255, 20, 200, 30, 255 };
        var builder = Builder();
        builder.Image(new RgbaImage(2, 1, pixels), new ChartRect(10, 10, 20, 10));
        var scene = builder.Build(); pixels[0] = 0;
        var image = VisualSceneRasterRenderer.Render(scene, supersampling: 1);
        Assert.InRange(Pixel(image, 11, 15)[0], 180, 200);
        Assert.InRange(Pixel(image, 28, 15)[1], 180, 200);
        var element = XDocument.Parse(VisualSceneSvgRenderer.Render(scene)).Descendants().Single(node => node.Name.LocalName == "image");
        var embedded = PngReader.Decode(Convert.FromBase64String(element.Attribute("href")!.Value.Substring("data:image/png;base64,".Length)));
        Assert.Equal(new byte[] { 200, 20, 30, 255, 20, 200, 30, 255 }, embedded.Pixels);
    }

    [Fact]
    public void StaticLinksEscapeTheirTooltipAndRejectExecutableTargets() {
        var builder = Builder();
        using (builder.PushLink("https://example.com/?x=1&y=2", "source", "heatmap-cell", "State <known>"))
            builder.Rect(new ChartRect(5, 5, 10, 10), ChartColor.Black);
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(builder.Build()));
        var link = svg.Descendants().Single(node => node.Name.LocalName == "a");
        Assert.Equal("https://example.com/?x=1&y=2", link.Attribute("href")!.Value);
        Assert.Equal("0", link.Attribute("tabindex")!.Value);
        Assert.Equal("State <known>", link.Elements().First().Value);
        Assert.Throws<ArgumentException>(() => Builder().PushLink("javascript:alert(1)"));
    }

    [Fact]
    public void DashedPathPreservesCapsAndCallerDashMutationCannotChangePreparedPaint() {
        var builder = Builder(); var dash = new[] { 4d, 4d };
        builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(5, 20), ChartPathCommand.LineTo(35, 20) }),
            stroke: ChartColor.Black, strokeWidth: 4, dash: dash, cap: VisualStrokeCap.Butt, join: VisualStrokeJoin.Bevel);
        var scene = builder.Build(); dash[0] = 20;
        var image = VisualSceneRasterRenderer.Render(scene, supersampling: 1);
        Assert.Equal(255, Pixel(image, 6, 20)[3]); Assert.Equal(0, Pixel(image, 11, 20)[3]);
        Assert.Equal(0, Pixel(image, 4, 20)[3]);
        var path = XDocument.Parse(VisualSceneSvgRenderer.Render(scene)).Descendants().Single(node => node.Name.LocalName == "path");
        Assert.Equal("butt", path.Attribute("stroke-linecap")!.Value);
        Assert.Equal("bevel", path.Attribute("stroke-linejoin")!.Value);
        Assert.Equal("4 4", path.Attribute("stroke-dasharray")!.Value);
    }

    [Fact]
    public void MultiContourClippingPreservesHolesAndRestoresPreviousClip() {
        var builder = Builder();
        var clip = new ChartPath(Rectangle(5, 5, 30, 30).Concat(Rectangle(12, 12, 16, 16)).ToArray());
        using (builder.PushClip(clip)) builder.Rect(new ChartRect(0, 0, 40, 40), ChartColor.Black);
        builder.Rect(new ChartRect(16, 16, 4, 4), ChartColor.White);
        var scene = builder.Build();
        var image = VisualSceneRasterRenderer.Render(scene, supersampling: 1);
        Assert.Equal(255, Pixel(image, 8, 8)[3]);
        Assert.Equal(0, Pixel(image, 13, 13)[3]);
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, Pixel(image, 18, 18));
        Assert.Equal(0, Pixel(image, 2, 2)[3]);
        Assert.Contains("clip-rule=\"evenodd\"", VisualSceneSvgRenderer.Render(scene));
    }

    [Fact]
    public void GradientStopsAreDetachedAndPaintTheSameLogicalAxisAsSvg() {
        var builder = Builder();
        var stops = new[] { new VisualGradientStop(0, ChartColor.Black), new VisualGradientStop(1, ChartColor.White) };
        builder.RectGradient(new ChartRect(5, 5, 30, 30), new ChartPoint(5, 5), new ChartPoint(35, 5), stops);
        var scene = builder.Build();
        var expected = VisualSceneSvgRenderer.Render(scene);
        stops[0] = new VisualGradientStop(0, ChartColor.White);
        Assert.Equal(expected, VisualSceneSvgRenderer.Render(scene));
        var image = VisualSceneRasterRenderer.Render(scene, supersampling: 1);
        Assert.InRange(Pixel(image, 7, 15)[0], 10, 35);
        Assert.InRange(Pixel(image, 32, 15)[0], 220, 245);
        Assert.Equal(0, Pixel(image, 2, 2)[3]);
        var gradient = XDocument.Parse(expected).Descendants().Single(element => element.Name.LocalName == "linearGradient");
        Assert.Equal("userSpaceOnUse", gradient.Attribute("gradientUnits")!.Value);
        Assert.Equal("35", gradient.Attribute("x2")!.Value);
    }

    [Fact]
    public void CompoundGradientFillKeepsItsEvenOddHoleInSvgAndNativePixels() {
        var builder = Builder();
        var path = new ChartPath(Rectangle(5, 5, 30, 30).Concat(Rectangle(12, 12, 16, 16)).ToArray());
        builder.PathGradient(path, new ChartPoint(5, 5), new ChartPoint(35, 5),
            new[] { new VisualGradientStop(0, ChartColor.Black), new VisualGradientStop(1, ChartColor.White) }, role: "compound-gradient");
        var scene = builder.Build();
        var element = XDocument.Parse(VisualSceneSvgRenderer.Render(scene)).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "compound-gradient");
        Assert.StartsWith("url(#", element.Attribute("fill")!.Value);
        Assert.Equal("evenodd", element.Attribute("fill-rule")!.Value);
        var image = VisualSceneRasterRenderer.Render(scene, supersampling: 1);
        Assert.Equal(255, Pixel(image, 8, 8)[3]);
        Assert.Equal(0, Pixel(image, 20, 20)[3]);
    }

    [Fact]
    public void NestedRotationsTransformClipsMarksAndRestoreParentCoordinates() {
        var builder = Builder();
        using (builder.PushRotation(90, 20, 20)) {
            using (builder.PushClip(new ChartRect(4, 6, 8, 6))) builder.Rect(new ChartRect(0, 0, 40, 40), ChartColor.Black);
            using (builder.PushRotation(-90, 20, 20)) builder.Rect(new ChartRect(2, 2, 3, 3), ChartColor.White);
        }
        builder.Rect(new ChartRect(35, 35, 3, 3), ChartColor.Black);
        var scene = builder.Build(); var image = VisualSceneRasterRenderer.Render(scene, supersampling: 1);
        Assert.Equal(new byte[] { 0, 0, 0, 255 }, Pixel(image, 31, 8));
        Assert.Equal(0, Pixel(image, 6, 8)[3]);
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, Pixel(image, 3, 3));
        Assert.Equal(255, Pixel(image, 36, 36)[3]);
        Assert.Contains("rotate(90 20 20)", VisualSceneSvgRenderer.Render(scene));
    }

    [Fact]
    public void PatternPaintIsClippedToItsNumericShapeAndRemainsDeterministic() {
        var builder = Builder();
        var clip = new ChartPath(Rectangle(5, 5, 30, 30).Concat(Rectangle(12, 12, 16, 16)).ToArray());
        builder.Pattern(clip, ChartFillPattern.Crosshatch, ChartColor.Black, spacing: 5, strokeWidth: 2);
        var scene = builder.Build(); var image = VisualSceneRasterRenderer.Render(scene);
        Assert.Contains(image.Pixels, value => value > 0);
        Assert.Equal(0, Pixel(image, 20, 20)[3]); Assert.Equal(0, Pixel(image, 2, 2)[3]);
        Assert.Equal(VisualSceneSvgRenderer.Render(scene), VisualSceneSvgRenderer.Render(scene));
    }

    private static VisualSceneBuilder Builder() => new(new VisualSize(40, 40), FontSpec.FromFamily("Missing scene paint test font"));
    private static IEnumerable<ChartPathCommand> Rectangle(double x, double y, double width, double height) => new[] {
        ChartPathCommand.MoveTo(x, y), ChartPathCommand.LineTo(x + width, y), ChartPathCommand.LineTo(x + width, y + height),
        ChartPathCommand.LineTo(x, y + height), ChartPathCommand.LineTo(x, y)
    };
    private static byte[] Pixel(RgbaImage image, int x, int y) => image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();
}
