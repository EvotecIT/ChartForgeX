using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidStateFloatingNoteRenderingTests {
    [Theory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public void NativeAndFencedNotePanelsRetainIdentityConcurrencyPaintsTextAndRasterInk(bool dark, int width) {
        var source = MermaidStateFloatingNoteTests.Source; var options = MermaidStateFloatingNoteTests.Options(dark, width);
        var rendered = MermaidRenderer.Render(source, options); Assert.Empty(rendered.Diagnostics);
        var document = Assert.IsType<MermaidStateDocument>(rendered.Document); var artifact = rendered.Artifact!;
        var chart = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.Equal((7, 3, 5, 4), (chart.Nodes.Count, chart.Groups.Count, chart.Edges.Count, chart.Annotations.Count));
        Assert.Equal(new[] { "Overview", "RegionMemo", "FollowUp" }, document.Notes.Where(note => note.Id != null).Select(note => note.Id));
        Assert.Equal("Independent state notes", document.Accessibility.Name);
        Assert.Equal(document.Transitions.Select(edge => (edge.SourceId, edge.TargetId)), chart.Edges.Select(edge => (edge.SourceNodeId, edge.TargetNodeId)));
        Assert.Equal(2, chart.Groups.Count(group => group.Presentation == TopologyGroupPresentation.Compartment));
        Assert.All(new[] { "Ready", "Waiting" }, id => Assert.Equal("#dcfce7", chart.Nodes.Single(node => node.Id == id).BackgroundColor));
        Assert.Equal("#fef3c7", chart.Groups.Single(group => group.Id == "Active").BackgroundColor);
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fence.Diagnostics); var fenced = Assert.Single(fence.Artifacts);
        Assert.Equal(svg, fenced.ToSvg()); Assert.Equal(png, fenced.ToPng());
        Capture(svg, png, VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()), dark, width);
        var xml = XDocument.Parse(svg); var panels = xml.Descendants().Where(element => Role(element) == "topology-annotation").ToArray();
        Assert.Equal(4, panels.Length);
        Assert.Single(xml.Descendants(), element => Role(element) == "topology-annotation-leader");
        Assert.Single(xml.Descendants(), element => Role(element) == "topology-compartment-separator");
        Assert.DoesNotContain(xml.Root!.Descendants(), element => element.Name.LocalName is "script" or "image" or "foreignObject" or "svg");
        Assert.DoesNotContain(xml.Descendants().Attributes(), attribute => attribute.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase));
        var image = RasterImageDecoder.Decode(png); var size = artifact.MeasureRasterSize();
        Assert.Equal(((int)size.Width, (int)size.Height), (image.Width, image.Height));
        var svgWidth = Number(xml.Root!, "width"); var svgHeight = Number(xml.Root!, "height");
        foreach (var panel in panels) {
            var surface = Assert.Single(panel.Descendants(), element => Role(element) == "topology-annotation-surface");
            var x = Number(surface, "x"); var y = Number(surface, "y"); var w = Number(surface, "width"); var h = Number(surface, "height");
            var halfStroke = Number(surface, "stroke-width") / 2;
            Assert.True(x - halfStroke >= chart.Viewport.Padding && x + w + halfStroke <= svgWidth - chart.Viewport.Padding);
            Assert.True(y - halfStroke >= chart.Viewport.Padding && y + h + halfStroke <= svgHeight - chart.Viewport.Padding);
            AssertPanelHasTextInk(surface, image, svgWidth, svgHeight);
        }
        var notes = artifact.Regions.Where(region => region.Kind == "topology-annotation").ToArray();
        Assert.Equal(4, notes.Length);
        foreach (var note in document.Notes.Where(note => note.Id != null)) {
            var region = notes.Single(region => region.Id == note.Id);
            Assert.Equal(note.Text, region.Label); var bounds = Assert.IsType<ChartForgeX.Primitives.ChartRect>(region.Bounds);
            Assert.True(bounds.Width > 0 && bounds.Height > 0);
        }
        var facts = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(facts.ToJson(), VisualArtifactInterchangeEnvelope.FromUtf8Json(facts.ToUtf8Json()).ToJson());
        Assert.Equal(chart.Annotations.Select(note => (note.Id, note.Text)), facts.Annotations.Select(note => (note.Id, note.Text)));
        Assert.All(facts.Annotations.Where(note => note.Id is "Overview" or "RegionMemo" or "FollowUp"), note => Assert.Empty(note.TargetIds));
        Assert.Equal("Ready", Assert.Single(facts.Annotations.Single(note => note.TargetIds.Count > 0).TargetIds));
        Assert.Equal("Independent state notes", facts.AccessibleName);
        Capture(svg, png, facts, dark, width);
    }

    private static void AssertPanelHasTextInk(XElement surface, RgbaImage image, double svgWidth, double svgHeight) {
        var sx = image.Width / svgWidth; var sy = image.Height / svgHeight;
        var left = (int)Math.Ceiling((Number(surface, "x") + 4) * sx); var right = (int)Math.Floor((Number(surface, "x") + Number(surface, "width") - 4) * sx);
        var top = (int)Math.Ceiling((Number(surface, "y") + 4) * sy); var bottom = (int)Math.Floor((Number(surface, "y") + Number(surface, "height") - 4) * sy);
        Assert.InRange(left, 0, image.Width - 1); Assert.InRange(right, left, image.Width - 1);
        Assert.InRange(top, 0, image.Height - 1); Assert.InRange(bottom, top, image.Height - 1);
        var background = (top * image.Width + left) * 4; var ink = false;
        for (var y = top; y <= bottom && !ink; y++) for (var x = left; x <= right; x++) {
            var pixel = (y * image.Width + x) * 4;
            if (Enumerable.Range(0, 3).Sum(channel => Math.Abs(image.Pixels[pixel + channel] - image.Pixels[background + channel])) > 24) { ink = true; break; }
        }
        Assert.True(ink, "The interior of each SVG note surface must contain native raster text ink.");
    }

    private static void Capture(string svg, byte[] png, VisualArtifactInterchangeEnvelope facts, bool dark, int width) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, "state-floating-" + width + (dark ? "-dark" : "-light"));
        File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png); File.WriteAllText(path + ".interchange.json", facts.ToJson());
    }
    private static string? Role(XElement element) => MermaidStateFloatingNoteTests.Role(element);
    private static double Number(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
}
