using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyInterchangeEventModelingTests {
    [Theory]
    [InlineData(false, 390)] [InlineData(true, 1200)]
    public void SavedEventModelingTypedFactsLoadIntoEditableNativeOwnersWithoutReparsingSource(bool dark, int width) {
        var rendered = MermaidRenderer.Render(MermaidEventModelingDepthTests.Source, MermaidEventModelingDepthTests.Options(dark, width));
        Assert.False(rendered.HasErrors); var original = rendered.Artifact!; var originalSvg = original.ToSvg();
        var json = original.ToInterchangeJson(); var envelope = VisualArtifactInterchangeEnvelope.FromJson(json);
        var chart = envelope.ToTopologyChart();
        Assert.Equal(TopologyLayoutMode.Manual, chart.LayoutMode); Assert.Equal(9, chart.Nodes.Count); Assert.Equal(6, chart.Groups.Count); Assert.Equal(8, chart.Edges.Count);
        Assert.Equal(envelope.Nodes.Select(node => node.Id), chart.Nodes.Select(node => node.Id));
        Assert.Equal(envelope.Edges.Select(edge => (edge.Id, edge.SourceId, edge.TargetId)), chart.Edges.Select(edge => (edge.Id, edge.SourceNodeId, edge.TargetNodeId)));
        var saved = envelope.Nodes.Single(node => node.Id == "20"); var loaded = chart.Nodes.Single(node => node.Id == "20");
        Assert.Equal(9, loaded.Details.Count); Assert.Equal(saved.Extensions["mermaid.data.reference.source"], loaded.Metadata["mermaid.data.reference.source"]);
        Assert.Equal(saved.Details.Select(detail => (detail.Label, detail.Value)), loaded.Details.Select(detail => (detail.Label, detail.Value)));
        foreach (var pair in saved.Details.Zip(loaded.Details)) Assert.Equal(pair.First.Extensions.OrderBy(entry => entry.Key), pair.Second.Metadata.OrderBy(entry => entry.Key));
        loaded.Details[0].Value = "String: RESTORED EDIT";
        var svg = chart.ToSvg(); var png = chart.ToPng(); Assert.Contains("RESTORED EDIT", svg); Assert.NotEmpty(png);
        Assert.Equal(json, envelope.ToJson()); Assert.Equal(originalSvg, original.ToSvg()); Assert.DoesNotContain("RESTORED EDIT", originalSvg);
        var xml = XDocument.Parse(svg); Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "foreignObject" or "image");
        TopologyInterchangeRestorationTests.Save("event-import-" + width + (dark ? "-dark" : "-light"), svg, png);
    }
}
