using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2SceneImageTests {
    [Theory]
    [InlineData("xMidYMid meet", 30, 20, 20, 20, false)]
    [InlineData("xMinYMin meet", 10, 20, 20, 20, false)]
    [InlineData("none", 10, 20, 60, 20, false)]
    [InlineData("xMaxYMax slice", 10, -20, 60, 60, true)]
    public void ImageAspectPolicyResolvesOneSharedGeometryAndSliceClip(string aspect, double x, double y, double width, double height, bool slice) {
        var pixels = new byte[] { 220, 30, 40, 255 };
        var builder = Builder();
        builder.Image(new RgbaImage(1, 1, pixels), new ChartRect(10, 20, 60, 20), preserveAspectRatio: aspect);
        var scene = builder.Build(); pixels[0] = 0;
        var image = Assert.Single(scene.Nodes.OfType<VisualSceneImage>());
        Assert.Equal(new ChartRect(x, y, width, height), image.Bounds);
        Assert.Equal(slice, scene.Nodes.OfType<VisualSceneGroup>().Any(group => group.Clip.HasValue && group.Clip.Value.Equals(new ChartRect(10, 20, 60, 20))));
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        var element = Assert.Single(svg.Descendants(), node => node.Name.LocalName == "image");
        Assert.Equal("none", (string?)element.Attribute("preserveAspectRatio"));
        Assert.Equal(x, (double)element.Attribute("x")!); Assert.Equal(width, (double)element.Attribute("width")!);
        var raster = VisualSceneRasterRenderer.Render(scene, supersampling: 1);
        Assert.Equal(new byte[] { 220, 30, 40, 255 }, Pixel(raster, (int)(x + width / 2), 30));
        Assert.Equal(0, Pixel(raster, 5, 30)[3]); Assert.Equal(0, Pixel(raster, 40, 15)[3]);
        if (!slice && aspect != "none") Assert.Equal(0, Pixel(raster, 65, 30)[3]);
    }

    [Fact]
    public void ImageOpacityUsesDetachedSourceAlphaThroughRotationClippingAndDensity() {
        var pixels = new byte[] { 220, 30, 40, 128 };
        var builder = Builder();
        using (builder.PushRotation(90, 40, 30)) using (builder.PushClip(new ChartRect(20, 20, 40, 20)))
            builder.Image(new RgbaImage(1, 1, pixels), new ChartRect(20, 20, 40, 20), opacity: .5);
        var scene = builder.Build(); pixels[3] = 255;
        Assert.Contains("opacity=\"0.5\"", VisualSceneSvgRenderer.Render(scene));
        foreach (var density in new[] { 1, 2 }) {
            var image = VisualSceneRasterRenderer.Render(scene, scale: density, supersampling: 1);
            Assert.Equal(new byte[] { 220, 30, 40, 64 }, Pixel(image, 40 * density, 30 * density));
            Assert.Equal(0, Pixel(image, 20 * density, 30 * density)[3]);
        }
    }

    [Theory]
    [InlineData("/assets/device.png?x=1&y=2")]
    [InlineData("https://images.example.test/device.png")]
    public void HostedImageProjectsOnlyItsEscapedReferenceWhileRasterPaintsCanonicalFallback(string href) {
        var builder = Builder();
        using (builder.PushClip(new ChartRect(10, 10, 60, 40)))
        using (builder.PushImageResource(href, new ChartRect(10, 10, 60, 40), "xMidYMid meet", .3, "artwork")) {
            using (builder.PushGroup("fallback", "glyph")) builder.Rect(new ChartRect(20, 20, 40, 20), ChartColor.Black);
        }
        builder.Rect(new ChartRect(2, 2, 3, 3), ChartColor.White, role: "after-resource");
        var scene = builder.Build();
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        var image = Assert.Single(svg.Descendants(), node => node.Name.LocalName == "image");
        Assert.Equal(href, (string?)image.Attribute("href")); Assert.Equal("0.3", (string?)image.Attribute("opacity"));
        Assert.DoesNotContain(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "glyph");
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "after-resource");
        var raster = VisualSceneRasterRenderer.Render(scene, supersampling: 1);
        Assert.Equal(new byte[] { 0, 0, 0, 255 }, Pixel(raster, 30, 30));
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, Pixel(raster, 3, 3));
        Assert.Equal(VisualSceneSvgRenderer.Render(scene), VisualSceneSvgRenderer.Render(scene));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/secret.png")]
    [InlineData("data:text/html,anything")]
    [InlineData("https://example.test/\nimage.png")]
    public void HostedImageResourceRejectsExecutableLocalAndControlCharacterReferences(string href) =>
        Assert.Throws<ArgumentException>(() => Builder().PushImageResource(href, new ChartRect(0, 0, 10, 10)));

    private static VisualSceneBuilder Builder() => new(new VisualSize(80, 60), FontSpec.FromFamily("Missing image test font"));
    private static byte[] Pixel(RgbaImage image, int x, int y) => image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();
}
