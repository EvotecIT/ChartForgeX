using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Canvas resources are emitted only for layers that actually reference them.</summary>
public sealed class VisualCanvasSvgFilterAdmissionTests {
    [Fact]
    public void BakedImageOnlyCanvasKeepsItsSingleImageWithoutNativeFilterResources() {
        var image = new RgbaImage(2, 2, new byte[] {
            17, 34, 51, 255, 17, 34, 51, 255, 17, 34, 51, 255, 17, 34, 51, 255
        });
        var xml = XDocument.Parse(VisualCanvas.Create(80, 60)
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent).AddImage(image, 0, 0, 80, 60).ToSvg());
        Assert.Single(xml.Descendants(), node => node.Name.LocalName == "image");
        Assert.DoesNotContain(xml.Descendants(), node => node.Name.LocalName is "filter" or "script" or "foreignObject");
        Assert.Empty(xml.Descendants().Attributes("filter"));
    }

    [Theory]
    [InlineData(false, VisualCanvasInfoTileSurfaceStyle.Glass)]
    [InlineData(false, VisualCanvasInfoTileSurfaceStyle.Outline)]
    [InlineData(false, VisualCanvasInfoTileSurfaceStyle.Raised)]
    [InlineData(true, VisualCanvasInfoTileSurfaceStyle.Glass)]
    [InlineData(true, VisualCanvasInfoTileSurfaceStyle.Outline)]
    [InlineData(true, VisualCanvasInfoTileSurfaceStyle.Raised)]
    public void NativeEffectLayersKeepOnlyTheirReferencedDefinitions(bool badge, VisualCanvasInfoTileSurfaceStyle tileStyle) {
        var canvas = VisualCanvas.Create(400, 240).WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddInfoTile(12, 12, 220, 120, "", "Capacity", "42", surfaceStyle: tileStyle);
        if (badge) canvas.AddHeroBadge(260, 20, 100, 70, "OK");
        var xml = XDocument.Parse(canvas.ToSvg());
        var ids = xml.Descendants().Where(node => node.Name.LocalName == "filter")
            .Select(node => node.Attribute("id")!.Value).ToArray();
        var references = xml.Descendants().Attributes("filter").Select(attribute => attribute.Value).Distinct().ToArray();
        Assert.Equal((badge ? 1 : 0) + (tileStyle == VisualCanvasInfoTileSurfaceStyle.Raised ? 1 : 0), ids.Length);
        Assert.Equal(ids.Select(id => "url(#" + id + ")").OrderBy(value => value), references.OrderBy(value => value));
        Assert.Equal(badge, ids.Any(id => id.EndsWith("-soft-glow", System.StringComparison.Ordinal)));
        Assert.Equal(tileStyle == VisualCanvasInfoTileSurfaceStyle.Raised,
            ids.Any(id => id.EndsWith("-raised-depth", System.StringComparison.Ordinal)));
    }
}
