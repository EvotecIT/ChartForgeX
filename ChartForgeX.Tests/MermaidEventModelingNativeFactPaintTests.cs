using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidEventModelingNativeFactPaintTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void TypedDataContributesNativePngInkAndRetainsCompleteSvgFactRows(bool dark) {
        const string source = "eventmodeling\ntf 1 ui Screen `json`{\"name\":\"Alex Morgan\",\"present\":null}";
        var result = MermaidRenderer.Render(source, MermaidEventModelingDepthTests.Options(dark, 390)); Assert.False(result.HasErrors);
        var prepared = Assert.IsType<TopologyChart>(result.Artifact!.Model).Prepare(); var scene = prepared.Visual.Scene;
        var facts = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "topology-node-detail").ToArray();
        var text = facts.SelectMany(fact => fact.Text.Lines.Select(line => line.Text)).ToArray();
        Assert.Contains(text, line => line.Contains("String: Alex Morgan")); Assert.Contains(text, line => line.Contains("Null: null"));
        var xml = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(xml.Descendants(), node => node.Name.LocalName == "text" && node.Value.Contains("String: Alex Morgan"));
        Assert.Contains(xml.Descendants(), node => node.Name.LocalName == "text" && node.Value.Contains("Null: null"));
        var actual = PngReader.Decode(prepared.ToPng());
        var withoutData = new VisualScene(scene.Size, scene.Nodes.Where(node => node.Role != "topology-node-detail").ToArray(),
            scene.Diagnostics, scene.Regions, scene.DataProjection);
        var omitted = VisualSceneRasterRenderer.Render(withoutData);
        Assert.Equal(actual.Width, omitted.Width); Assert.Equal(actual.Height, omitted.Height);
        Assert.False(actual.Pixels.SequenceEqual(omitted.Pixels), "Typed facts must contribute pixels to the native PNG.");
    }
}
