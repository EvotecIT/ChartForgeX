using System.Xml.Linq;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartEndpointTests {
    [Theory]
    [InlineData("A---ox-->|after|B", "x", "---o")]
    [InlineData("A---xo-->|after|B", "o", "---x")]
    [InlineData("A---ox --o|after|B", "x", "---o")]
    [InlineData("A---xo --x|after|B", "o", "---x")]
    public void PipeLabelsDoNotConsumeAnEarlierCompactTarget(string statement, string intermediate, string firstOperator) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\n" + statement);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", intermediate, "B" }, parsed.Document!.Nodes.Select(node => node.Id));
        Assert.Equal(2, parsed.Document.Edges.Count);
        Assert.Equal(("A", intermediate, firstOperator, (string?)null),
            (parsed.Document.Edges[0].SourceId, parsed.Document.Edges[0].TargetId, parsed.Document.Edges[0].Operator, parsed.Document.Edges[0].Label));
        Assert.Equal((intermediate, "B", "after"),
            (parsed.Document.Edges[1].SourceId, parsed.Document.Edges[1].TargetId, parsed.Document.Edges[1].Label));
    }

    [Theory]
    [InlineData("-- \"x < y\" --o", VisualLinkDirection.Forward, "Circle")]
    [InlineData("-- \"x < y\" ---", VisualLinkDirection.None, "None")]
    [InlineData("-- \"x > y\" --x", VisualLinkDirection.Forward, "Cross")]
    [InlineData("<---", VisualLinkDirection.Backward, "None")]
    public async Task LayerOrderUsesEndpointsInsteadOfCaptionComparisons(string op, VisualLinkDirection direction, string targetMarker) {
        var source = "flowchart LR\nA " + op + " B";
        var parsed = new MermaidParser().ParseFlowchart(source);
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart();
        var edge = Assert.Single(chart.Edges);
        Assert.Equal(direction, edge.Direction);
        Assert.Equal(targetMarker, edge.TargetMarker.ToString());
        Assert.Equal(direction == VisualLinkDirection.Backward ? "1" : "0", chart.Nodes.Single(node => node.Id == "A").Metadata["mermaid.layer"]);
        Assert.Equal(direction == VisualLinkDirection.Backward ? "0" : "1", chart.Nodes.Single(node => node.Id == "B").Metadata["mermaid.layer"]);
        if (!InteractiveChartBrowser.Enabled) return;
        var artifact = MermaidRenderer.Render(source).Artifact!;
        var svg = artifact.ToSvg();
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = Path.Combine(captures, "label-layers-" + direction + "-" + targetMarker);
        File.WriteAllText(name + ".svg", svg); File.WriteAllBytes(name + ".native.png", artifact.ToPng());
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = name + ".browser.png", FullPage = true });
    }

    [Fact]
    public void VersionedFixtureRetainsEveryEndpointAndTargetIdentity() {
        var fixture = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "flowchart-endpoints.mmd");
        var parsed = new MermaidParser().ParseFlowchart(File.ReadAllText(fixture));
        Assert.Empty(parsed.Diagnostics);
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(fixture, ".expected.json")));
        var chart = parsed.Document!.ToTopologyChart();
        Assert.Equal(expected.RootElement.GetProperty("nodes").EnumerateArray().Select(node => node.GetString()), chart.Nodes.Select(node => node.Id));
        Assert.Equal(expected.RootElement.GetProperty("edges").EnumerateArray().Select(edge => (edge[0].GetString()!, edge[1].GetString()!)),
            chart.Edges.Select(edge => (edge.SourceNodeId, edge.TargetNodeId)));
        Assert.Equal(expected.RootElement.GetProperty("endpointKinds").EnumerateArray().Select(edge => (edge[0].GetString()!, edge[1].GetString()!)),
            chart.Edges.Select(edge => (edge.SourceMarker.ToString()!, edge.TargetMarker.ToString()!)));
    }

    [Theory]
    [InlineData("---o", "None", "Circle", VisualLinkDirection.Forward)]
    [InlineData("---x", "None", "Cross", VisualLinkDirection.Forward)]
    [InlineData("o--o", "Circle", "Circle", VisualLinkDirection.Bidirectional)]
    [InlineData("x--x", "Cross", "Cross", VisualLinkDirection.Bidirectional)]
    [InlineData("<-->", "Arrow", "Arrow", VisualLinkDirection.Bidirectional)]
    [InlineData("-->|outcome|", "None", "Arrow", VisualLinkDirection.Forward)]
    [InlineData("-- connection ---", "None", "None", VisualLinkDirection.None)]
    [InlineData("-- exception --o", "None", "Circle", VisualLinkDirection.Forward)]
    public void OperatorsKeepTheirEndpointKinds(string op, string source, string target, VisualLinkDirection direction) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nA " + op + " B");
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart();
        var edge = Assert.Single(chart.Edges);
        Assert.Equal(source, (edge.SourceMarker ?? TopologyMarkerKind.None).ToString());
        Assert.Equal(target, (edge.TargetMarker ?? TopologyMarkerKind.None).ToString());
        Assert.Equal(direction, edge.Direction);
    }

    [Theory]
    [InlineData("A---oxTarget", "xTarget", "---o")]
    [InlineData("A---xoTarget", "oTarget", "---x")]
    [InlineData("A--- xTarget", "xTarget", "---")]
    [InlineData("A--- oTarget", "oTarget", "---")]
    public void ACompactEndpointConsumesOnlyItsOwnMarker(string statement, string target, string op) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\n" + statement);
        Assert.Empty(parsed.Diagnostics);
        var edge = Assert.Single(parsed.Document!.Edges);
        Assert.Equal(target, edge.TargetId);
        Assert.Equal(op, edge.Operator);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CircleAndCrossEndpointsSurviveNativeExportsAndInterchange(bool dark) {
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + "flowchart LR\nA o--o B\nC x--x D";
        var result = MermaidRenderer.Render(source);
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        Assert.IsType<TopologyChart>(artifact.Model);
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var xml = XDocument.Parse(svg);
        var markers = xml.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-marker").ToArray();
        Assert.Equal(2, markers.Count(element => element.Name.LocalName == "ellipse"));
        Assert.Equal(4, markers.Count(element => element.Name.LocalName == "path"));
        var roundTrip = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal("Circle", roundTrip.Edges[0].Topology!.SourceMarker.ToString());
        Assert.Equal("Cross", roundTrip.Edges[1].Topology!.TargetMarker.ToString());
        Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = Path.Combine(captures, dark ? "endpoints-dark" : "endpoints-light");
        File.WriteAllText(name + ".svg", svg); File.WriteAllBytes(name + ".native.png", png);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = name + ".browser.png", FullPage = true });
    }
}
