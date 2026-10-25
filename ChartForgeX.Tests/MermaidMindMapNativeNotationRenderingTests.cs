using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidMindMapNativeNotationRenderingTests {
    [Theory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public void AllAuthoredSurfacesKeepReadableCaptionsNativeRasterAndPortableFacts(bool dark, int width) {
        var source = MermaidMindMapNativeNotationTests.Source; var options = MermaidMindMapNativeNotationTests.Options(dark, width);
        var result = MermaidRenderer.Render(source, options); Assert.False(result.HasErrors); var artifact = result.Artifact!;
        var chart = Assert.IsType<TopologyChart>(artifact.Model); var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fenced.HasErrors);
        var fence = Assert.Single(fenced.Artifacts); Assert.Equal(svg, fence.ToSvg()); Assert.Equal(png, fence.ToPng());
        var xml = XDocument.Parse(svg);
        var nodes = xml.Descendants().Where(element => Role(element) == "topology-node").ToArray(); Assert.Equal(7, nodes.Length);
        Assert.Equal(6, xml.Descendants().Count(element => Role(element) == "topology-edge"));
        Assert.Single(nodes, node => (string?)node.Attribute("data-node-shape") == "Burst");
        Assert.Equal(2, nodes.Count(node => !string.IsNullOrWhiteSpace((string?)node.Attribute("data-node-icon-id"))));
        Assert.Single(nodes, node => (string?)node.Attribute("data-cfx-selected") == "true");
        Assert.DoesNotContain(xml.Root!.Descendants(), element => element.Name.LocalName is "script" or "image" or "foreignObject" or "svg");
        Assert.DoesNotContain(xml.Descendants().Attributes(), attribute => attribute.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase));
        var texts = xml.Descendants().Where(element => element.Name.LocalName == "text").ToArray();
        Assert.Contains(texts, element => element.Value == "Roadmap"); Assert.Contains(texts, element => element.Value == "service & data");
        Assert.DoesNotContain(texts, element => element.Value.Contains("**", StringComparison.Ordinal) || element.Value.Contains("<br", StringComparison.Ordinal));
        var image = RasterImageDecoder.Decode(png); var size = artifact.MeasureRasterSize();
        Assert.Equal(((int)size.Width, (int)size.Height), (image.Width, image.Height));
        var facts = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(facts.ToJson(), VisualArtifactInterchangeEnvelope.FromUtf8Json(facts.ToUtf8Json()).ToJson());
        Assert.Equal("Native mindmap notation", facts.AccessibleName);
        Assert.Equal(chart.Nodes.Select(node => (node.Id, node.Label, node.Shape, node.IconId)), facts.Nodes.Select(node => (node.Id, node.Label, node.Topology!.Shape, node.IconId)));
        Assert.Equal(chart.Edges.Select(edge => (edge.SourceNodeId, edge.TargetNodeId)), facts.Edges.Select(edge => (edge.SourceId, edge.TargetId)));
        Assert.Equal("highlight", facts.Nodes.Single(node => node.Extensions.TryGetValue("mermaid.source.id", out var id) && id == "plan").Extensions["mermaid.classes"]);
        var svgWidth = double.Parse((string)xml.Root!.Attribute("width")!, CultureInfo.InvariantCulture);
        var svgHeight = double.Parse((string)xml.Root!.Attribute("height")!, CultureInfo.InvariantCulture);
        var regions = artifact.Regions.Where(region => region.Kind == "topology-node").ToArray(); Assert.Equal(7, regions.Length);
        foreach (var region in regions) {
            var bounds = Assert.IsType<ChartRect>(region.Bounds);
            Assert.True(bounds.Left >= 0 && bounds.Top >= 0 && bounds.Right <= svgWidth && bounds.Bottom <= svgHeight);
            AssertNodeHasInk(bounds.Left, bounds.Top, bounds.Width, bounds.Height, image, svgWidth, svgHeight);
        }
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, "mindmap-notation-" + width + (dark ? "-dark" : "-light"));
        File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png); File.WriteAllText(path + ".interchange.json", facts.ToJson());
    }

    [Fact]
    public void HtmlAndMathRemainLiteralTextWithLocatedWarningsAndNoExternalPaintAssets() {
        const string source = "mindmap\nroot[\"<img src='https://example.invalid/icon.svg' onerror='alert(1)'><br>$$x$$\"]";
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement && item.Span.Line == 2);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); Assert.Contains("<img", Assert.Single(chart.Nodes).Label, StringComparison.Ordinal);
        var xml = XDocument.Parse(result.Artifact.ToSvg()); Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "image" or "foreignObject" or "script" or "img");
        Assert.NotEmpty(result.Artifact.ToPng());
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }); Assert.Null(strict.Artifact);
    }

    private static void AssertNodeHasInk(double x, double y, double width, double height, RgbaImage image, double svgWidth, double svgHeight) {
        var left = Math.Max(0, (int)Math.Ceiling((x + width * .25) * image.Width / svgWidth));
        var right = Math.Min(image.Width - 1, (int)Math.Floor((x + width * .75) * image.Width / svgWidth));
        var top = Math.Max(0, (int)Math.Ceiling((y + height * .25) * image.Height / svgHeight));
        var bottom = Math.Min(image.Height - 1, (int)Math.Floor((y + height * .75) * image.Height / svgHeight));
        var distinct = new HashSet<int>();
        for (var row = top; row <= bottom; row++) for (var column = left; column <= right; column++) {
            var offset = (row * image.Width + column) * 4;
            distinct.Add((image.Pixels[offset] << 16) | (image.Pixels[offset + 1] << 8) | image.Pixels[offset + 2]);
        }
        Assert.True(distinct.Count > 3, "Every native node interior must retain raster caption or explicitly requested icon ink.");
    }
    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
}
