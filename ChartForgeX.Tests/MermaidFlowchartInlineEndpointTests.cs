using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartInlineEndpointTests {
    [Theory]
    [InlineData("o-- publishes --o", "publishes", "Circle", "Solid")]
    [InlineData("x-- fails --x", "fails", "Cross", "Solid")]
    [InlineData("<-- returns -->", "returns", "Arrow", "Solid")]
    [InlineData("o== publishes ==o", "publishes", "Circle", "Solid")]
    [InlineData("x== fails ==x", "fails", "Cross", "Solid")]
    [InlineData("<== returns ==>", "returns", "Arrow", "Solid")]
    [InlineData("o-. publishes .-o", "publishes", "Circle", "Dotted")]
    [InlineData("x-. fails .-x", "fails", "Cross", "Dotted")]
    [InlineData("<-. returns .->", "returns", "Arrow", "Dotted")]
    [InlineData("o-- \"x --> y\" --o", "x --> y", "Circle", "Solid")]
    [InlineData("o-- oxford --o", "oxford", "Circle", "Solid")]
    [InlineData("o-- \"version 1.2\" --o", "version 1.2", "Circle", "Solid")]
    [InlineData("o--o|publishes|", "publishes", "Circle", "Solid")]
    public void InlineLabelsPreserveNodeIdentityAndEndpointSemantics(string op, string caption, string marker, string style) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nA " + op + " B");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", "B" }, parsed.Document!.Nodes.Select(node => node.Id));
        Assert.Equal(caption, Assert.Single(parsed.Document.Edges).Label);
        var edge = Assert.Single(parsed.Document.ToTopologyChart().Edges);
        Assert.Equal(marker, edge.SourceMarker.ToString());
        Assert.Equal(marker, edge.TargetMarker.ToString());
        Assert.Equal(VisualLinkDirection.Bidirectional, edge.Direction);
        Assert.Equal(style, edge.LineStyle.ToString());
    }

    [Theory]
    [InlineData("A o-- publishes --o B --> C", "publishes", "")]
    [InlineData("A -- first --o B -- second --> C", "first", "second")]
    public void InlineSuffixStopsAtItsOwnEdge(string statement, string first, string second) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\n" + statement);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", "B", "C" }, parsed.Document!.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { ("A", "B", first), ("B", "C", second) },
            parsed.Document.Edges.Select(edge => (edge.SourceId, edge.TargetId, edge.Label ?? "")));
    }

    [Fact]
    public void VersionedFixturePreservesLabelsAndEndpoints() {
        var fixture = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "flowchart-inline-endpoints.mmd");
        var parsed = new MermaidParser().ParseFlowchart(File.ReadAllText(fixture));
        Assert.Empty(parsed.Diagnostics);
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(fixture, ".expected.json")));
        var chart = parsed.Document!.ToTopologyChart();
        Assert.Equal(expected.RootElement.GetProperty("nodes").EnumerateArray().Select(node => node.GetString()), chart.Nodes.Select(node => node.Id));
        Assert.Equal(expected.RootElement.GetProperty("edges").EnumerateArray().Select(edge => (edge[0].GetString()!, edge[1].GetString()!)),
            chart.Edges.Select(edge => (edge.SourceNodeId, edge.TargetNodeId)));
        Assert.Equal(expected.RootElement.GetProperty("edgeLabels").EnumerateArray().Select(label => label.GetString()),
            parsed.Document!.Edges.Select(edge => edge.Label ?? ""));
        Assert.Equal(expected.RootElement.GetProperty("endpointKinds").EnumerateArray().Select(edge => (edge[0].GetString()!, edge[1].GetString()!)),
            chart.Edges.Select(edge => (edge.SourceMarker.ToString()!, edge.TargetMarker.ToString()!)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InlineEndpointsAndCaptionsRenderInNativeAndBrowserOutput(bool dark) {
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "")
            + "flowchart LR\nA o-- publishes --o B\nC x-. fails .-x D\nE <== returns ==> F";
        var result = MermaidRenderer.Render(source);
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        var chart = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.Equal(6, chart.Nodes.Count);
        Assert.Equal(new[] { "publishes", "fails", "returns" }, chart.Edges.Select(edge => edge.Label));
        Assert.All(chart.Edges, edge => Assert.Equal(VisualLinkDirection.Bidirectional, edge.Direction));
        var svg = artifact.ToSvg();
        var png = artifact.ToPng();
        foreach (var caption in new[] { "publishes", "fails", "returns" }) Assert.Contains(caption, svg);
        Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = Path.Combine(captures, dark ? "inline-endpoints-dark" : "inline-endpoints-light");
        File.WriteAllText(name + ".svg", svg);
        File.WriteAllBytes(name + ".native.png", png);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = name + ".browser.png", FullPage = true });
    }
}
