using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyArtworkTests {
    [Theory]
    [InlineData("xMidYMid meet", 1)]
    [InlineData("none", 1.875)]
    public void BitmapArtworkUsesItsAuthoredAspectPolicyInTheNativeScene(string aspect, double ratio) {
        var artwork = Bitmap().WithPreserveAspectRatio(aspect);
        var chart = TopologyChart.Create().WithViewport(220, 140, 10).WithLegend(null)
            .AddArtworkNode("image", "Image", artwork, 40, 30, width: 120, height: 64);
        var prepared = chart.Prepare(Options());
        var image = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneImage>());
        Assert.Equal(ratio, image.Bounds.Width / image.Bounds.Height, 6);
        var node = Assert.Single(prepared.ToInterchangeEnvelope().Nodes);
        Assert.Equal(node.X!.Value + node.Width!.Value / 2, image.Bounds.X + image.Bounds.Width / 2, 6);
        Assert.Equal(node.Y!.Value + node.Height!.Value / 2, image.Bounds.Y + image.Bounds.Height / 2, 6);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Single(svg.Descendants(), element => element.Name.LocalName == "image");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void BitmapSliceKeepsAlignedSourcePixelsAndClipsToTheNativeNodeViewport() {
        var artwork = TopologyIconArtwork.Image("data:image/png;base64," + Convert.ToBase64String(PngWriter.WriteRgba(
            new RgbaImage(2, 1, new byte[] { 220, 30, 40, 255, 30, 40, 220, 255 })))).WithPreserveAspectRatio("xMaxYMid slice");
        var chart = TopologyChart.Create().WithViewport(220, 140, 10).WithLegend(null)
            .AddArtworkNode("image", "Image", artwork, 80, 30, width: 40, height: 64);
        var prepared = chart.Prepare(Options()); var scene = prepared.Visual.Scene;
        var image = Assert.Single(scene.Nodes.OfType<VisualSceneImage>());
        var node = Assert.Single(prepared.ToInterchangeEnvelope().Nodes);
        var viewport = new ChartRect(node.X!.Value, node.Y!.Value, node.Width!.Value, node.Height!.Value);
        Assert.True(image.Bounds.Width > viewport.Width); Assert.Equal(viewport.Right, image.Bounds.Right, 6);
        Assert.Contains(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Clip.HasValue && group.Clip.Value.Equals(viewport));
        var clippedScene = new VisualScene(scene.Size, scene.Nodes.Where(item => item is VisualSceneImage or VisualSceneGroup or VisualSceneEndGroup).ToArray(),
            Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        var raster = VisualSceneRasterRenderer.Render(clippedScene, supersampling: 1);
        var x = (int)(viewport.X + viewport.Width / 2); var y = (int)(viewport.Y + viewport.Height / 2);
        Assert.Equal(new byte[] { 30, 40, 220, 255 }, raster.Pixels.Skip((y * raster.Width + x) * 4).Take(4));
        Assert.Equal(0, raster.Pixels[(y * raster.Width + (int)viewport.Left - 2) * 4 + 3]);
        var svg = XDocument.Parse(prepared.ToSvg());
        var element = Assert.Single(svg.Descendants(), item => item.Name.LocalName == "image");
        Assert.Contains(element.Ancestors(), parent => parent.Attribute("clip-path") != null);
    }

    [Fact]
    public void EmbeddedSvgDataUrlsUseTheCanonicalImageDecoderForBase64AndPercentEncodedPayloads() {
        const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 2 1\"><rect width=\"2\" height=\"1\" fill=\"#DC2626\"/></svg>";
        foreach (var href in new[] { "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg)),
            "data:image/svg+xml," + Uri.EscapeDataString(svg) }) {
            var chart = TopologyChart.Create().WithViewport(220, 140, 10).WithLegend(null)
                .AddArtworkNode("image", "Image", TopologyIconArtwork.Image(href), 40, 30, width: 120, height: 64);
            var prepared = chart.Prepare(Options());
            var image = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneImage>());
            var center = ((image.Image.Height / 2) * image.Image.Width + image.Image.Width / 2) * 4;
            Assert.Equal(new byte[] { 220, 38, 38, 255 }, image.Image.Pixels.Skip(center).Take(4));
            Assert.DoesNotContain(prepared.Visual.Scene.Diagnostics, diagnostic => diagnostic.Code == "topology.artwork-fallback");
            Assert.Contains("data:image/png;base64,", prepared.ToSvg()); Assert.NotEmpty(prepared.ToPng());
        }
    }

    [Fact]
    public void HighlightDimsResolvedArtworkAndCardGlyphPixelsWhileSelectionKeepsItsNativeOutline() {
        var chart = TopologyChart.Create().WithViewport(400, 180, 10).WithLegend(null)
            .AddArtworkNode("active", "Active", Bitmap(), 20, 40, width: 60, height: 60)
            .AddArtworkNode("dim", "Dim", Bitmap(), 130, 40, width: 60, height: 60, color: "var(--selection, #22AA44)")
            .AddArtworkNode("card", "Card", Bitmap(), 240, 40, width: 120, height: 64, displayMode: TopologyNodeDisplayMode.Card);
        var options = Options(); options.HighlightNodeIds.Add("active"); options.SelectedNodeIds.Add("dim"); options.DimmedOpacity = .25;
        var prepared = chart.Prepare(options);
        var images = prepared.Visual.Scene.Nodes.OfType<VisualSceneImage>().ToArray();
        Assert.Equal(new[] { 1d, .25, .25 }, images.Select(image => image.Opacity));
        var outline = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "topology-node-artwork-selection");
        Assert.True(outline.StrokeWidth >= 2.8);
        var svg = XDocument.Parse(prepared.ToSvg());
        var imageElements = svg.Descendants().Where(element => element.Name.LocalName == "image").ToArray();
        Assert.Equal(2, imageElements.Count(element => (string?)element.Attribute("opacity") == "0.25"));
        var selection = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "topology-node-artwork-selection");
        Assert.Contains("var(--selection,", (string?)selection.Attribute("stroke"));
        var imageScene = new VisualScene(prepared.Visual.Size,
            prepared.Visual.Scene.Nodes.Where(node => node is VisualSceneImage or VisualSceneGroup or VisualSceneEndGroup).ToArray(),
            Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        var raster = VisualSceneRasterRenderer.Render(imageScene, supersampling: 1);
        Assert.Equal(255, Alpha(raster, images[0])); Assert.Equal(64, Alpha(raster, images[1])); Assert.Equal(64, Alpha(raster, images[2]));
        var outlinedScene = new VisualScene(prepared.Visual.Size, new VisualSceneNode[] { outline }, Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        var outlined = VisualSceneRasterRenderer.Render(outlinedScene, supersampling: 1);
        Assert.True(outlined.Pixels[((int)Math.Round(outline.Bounds.Top) * outlined.Width + (int)Math.Round(outline.Bounds.X + outline.Bounds.Width / 2)) * 4 + 3] > 0);
    }

    [Fact]
    public void EmbeddedAndHostedLegendArtworkPrepareBeforeTopologyStateAndKeepExplicitLegendOpacity() {
        var catalog = TopologyIconCatalog.Default().AddPack(new TopologyIconPack("legend-art", "Legend Artwork")
            .AddIcon(new TopologyIconDefinition("legend-art", "bitmap", "Bitmap", TopologyNodeKind.Server) { Artwork = Bitmap() })
            .AddIcon(new TopologyIconDefinition("legend-art", "hosted", "Hosted", TopologyNodeKind.Server) { Artwork = TopologyIconArtwork.Image("/assets/legend-server.png") }));
        var chart = TopologyChart.Create().WithViewport(600, 360, 20)
            .WithLegend(TopologyLegend.Create("Artwork legend")
                .AddNodeKind("Embedded", TopologyNodeKind.Server, iconId: "legend-art:bitmap")
                .AddNodeKind("Hosted", TopologyNodeKind.Server, iconId: "legend-art:hosted"))
            .AddNode("source", "Source", 60, 60, width: 100, height: 60);
        var options = Options(); options.IncludeLegend = true; options.IconCatalog = catalog;
        options.HighlightNodeIds.Add("source"); options.DimmedOpacity = .15;
        var prepared = chart.Prepare(options);
        var bitmap = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneImage>());
        var resource = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.ImageResource != null).ImageResource!;
        Assert.Equal(1, bitmap.Opacity); Assert.Equal(1, resource.Opacity);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(2, svg.Descendants().Count(element => element.Name.LocalName == "image"));
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "image" && (string?)element.Attribute("href") == resource.Href);
        Assert.Contains(prepared.Visual.Diagnostics, diagnostic => diagnostic.Code == "topology.artwork-external-raster-fallback");
        var raster = VisualSceneRasterRenderer.Render(prepared.Visual.Scene, supersampling: 1);
        var center = ((int)(bitmap.Bounds.Y + bitmap.Bounds.Height / 2) * raster.Width + (int)(bitmap.Bounds.X + bitmap.Bounds.Width / 2)) * 4;
        Assert.Equal(new byte[] { 220, 30, 40, 255 }, raster.Pixels.Skip(center).Take(4));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void CatalogImageGlyphsAndHostedArtworkRetainDetachedResourcesWithNetworkFreeNativeFallback() {
        var icon = new TopologyIconDefinition("custom", "bitmap", "Bitmap", TopologyNodeKind.Server) { Artwork = Bitmap(), DisplayMode = TopologyNodeDisplayMode.Card };
        var catalog = TopologyIconCatalog.Default().AddPack(new TopologyIconPack("custom", "Custom").AddIcon(icon));
        var chart = TopologyChart.Create().WithViewport(360, 180, 10).WithLegend(null)
            .AddNode("card", "Card", 20, 40, width: 120, height: 64).WithNodeIcon("card", "custom:bitmap", catalog)
            .AddArtworkNode("external", "External", TopologyIconArtwork.Image("/assets/device.png?x=1&y=2"), 200, 40, width: 120, height: 64);
        var options = Options(); options.IconCatalog = catalog;
        var prepared = chart.Prepare(options); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneImage>());
        var resource = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.ImageResource != null).ImageResource!;
        Assert.Equal("/assets/device.png?x=1&y=2", resource.Href);
        Assert.Contains(prepared.Visual.Scene.Diagnostics, diagnostic => diagnostic.Code == "topology.artwork-external-raster-fallback");
        var external = XDocument.Parse(svg).Descendants().Single(element => element.Name.LocalName == "image" && (string?)element.Attribute("href") == resource.Href);
        Assert.Equal("xMidYMid meet", (string?)external.Attribute("preserveAspectRatio"));
        Assert.Contains("&amp;y=2", svg);
        chart.Nodes.Single(node => node.Id == "external").Artwork!.ImageHref = "https://images.example.test/another.png";
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Equal(png, chart.ToPng(options));
        Assert.DoesNotContain(XDocument.Parse(svg).Descendants(), element => element.Name.LocalName != "image" &&
            element.Ancestors().Any(parent => (string?)parent.Attribute("data-node-id") == "external") &&
            (string?)element.Attribute("data-cfx-role") == "topology-node-icon");
    }

    private static TopologyIconArtwork Bitmap() => TopologyIconArtwork.Image("data:image/png;base64," + Convert.ToBase64String(
        PngWriter.WriteRgba(new RgbaImage(1, 1, new byte[] { 220, 30, 40, 255 }))));
    private static TopologyRenderOptions Options() => new() { IncludeLegend = false, IncludeTitle = false, IncludeNodeLabels = false, IncludeStatusBadges = false };
    private static byte Alpha(RgbaImage raster, VisualSceneImage image) => raster.Pixels[((int)(image.Bounds.Y + image.Bounds.Height / 2) * raster.Width + (int)(image.Bounds.X + image.Bounds.Width / 2)) * 4 + 3];
}
