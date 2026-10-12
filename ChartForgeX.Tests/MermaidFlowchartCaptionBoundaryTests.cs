using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartCaptionBoundaryTests {
    [Theory]
    [InlineData("flowchart-caption-boundaries")]
    [InlineData("flowchart-metadata-scalars")]
    public void VersionedFixturesPreserveCaptionsAndStatementBoundaries(string name) {
        var path = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", name + ".mmd");
        var parsed = new MermaidParser().ParseFlowchart(File.ReadAllText(path));
        Assert.Empty(parsed.Diagnostics);
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(path, ".expected.json")));
        var document = parsed.Document!;
        Assert.Equal(expected.RootElement.GetProperty("nodes").EnumerateArray().Select(node => node.GetString()), document.Nodes.Select(node => node.Id));
        foreach (var label in expected.RootElement.GetProperty("nodeLabels").EnumerateObject())
            Assert.Equal(label.Value.GetString(), Assert.Single(document.Nodes, node => node.Id == label.Name).Text ?? label.Name);
        Assert.Equal(expected.RootElement.GetProperty("edges").EnumerateArray().Select(edge => (edge[0].GetString()!, edge[1].GetString()!)),
            document.Edges.Select(edge => (edge.SourceId, edge.TargetId)));
        Assert.Equal(expected.RootElement.GetProperty("edgeLabels").EnumerateArray().Select(label => label.GetString()),
            document.Edges.Select(edge => edge.Label ?? ""));
    }

    [Theory]
    [InlineData("A -->|\"a|b\"| B", "a|b")]
    [InlineData("A o--o|\"a|b; c\"| B", "a|b; c")]
    [InlineData("A -- x>0 --> B", "x>0")]
    [InlineData("A o-- x>0 --o B", "x>0")]
    [InlineData("A <-. x>0 .-> B", "x>0")]
    [InlineData("A == x>0 ==> B", "x>0")]
    [InlineData("A -- text; more --> B", "text; more")]
    [InlineData("A -- x[0] --> B", "x[0]")]
    public void CaptionsKeepTheirOwnDelimitersAndFollowingCompactEdges(string first, string caption) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\n" + first + "; B --> C");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", "B", "C" }, parsed.Document!.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { ("A", "B", caption), ("B", "C", "") },
            parsed.Document.Edges.Select(edge => (edge.SourceId, edge.TargetId, edge.Label ?? "")));
    }

    [Theory]
    [InlineData("label: Don't stop, shape: rect", "Don't stop", TopologyNodeShape.Rectangle)]
    [InlineData("label: 'API', shape: 'circle'", "API", TopologyNodeShape.Circle)]
    [InlineData("label: 'Don''t; stop', shape: 'circle'", "Don't; stop", TopologyNodeShape.Circle)]
    [InlineData("label: 'C:\\', shape: 'circle'", "C:\\", TopologyNodeShape.Circle)]
    public void MetadataUsesScalarQuotesWithoutConsumingFollowingEdges(string properties, string caption, TopologyNodeShape shape) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nA@{" + properties + "} --> B; B --> C");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", "B", "C" }, parsed.Document!.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { ("A", "B"), ("B", "C") }, parsed.Document.Edges.Select(edge => (edge.SourceId, edge.TargetId)));
        var node = Assert.Single(parsed.Document.ToTopologyChart().Nodes, node => node.Id == "A");
        Assert.Equal(caption, node.Label);
        Assert.Equal(shape, node.Shape);
    }

    [Theory]
    [InlineData("A>Caption]")]
    [InlineData("A[\"x>0; then proceed\"]")]
    public void NodeShapeAndQuotedCaptionBoundariesRemainSeparate(string node) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\n" + node + " --> B; B --> C");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", "B", "C" }, parsed.Document!.Nodes.Select(item => item.Id));
        Assert.Equal(2, parsed.Document.Edges.Count);
    }

    [Theory]
    [InlineData("---")]
    [InlineData("-->")]
    [InlineData("==>")]
    [InlineData("-.->")]
    public void CompleteOperatorsDoNotStartInlineCaptionsInsideTheirTokens(string op) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nA " + op + " B; C -- compact --> D");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "A", "B", "C", "D" }, parsed.Document!.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { ("A", "B", ""), ("C", "D", "compact") },
            parsed.Document.Edges.Select(edge => (edge.SourceId, edge.TargetId, edge.Label ?? "")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CaptionAndMetadataBoundariesSurviveNativeBrowserAndInterchange(bool dark) {
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "")
            + "flowchart LR\nA@{label: 'API', shape: 'circle'} -->|\"a|b\"| B[Don't stop]; B -- x>0 --> C";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!;
        var chart = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.Equal(TopologyNodeShape.Circle, chart.Nodes[0].Shape);
        Assert.Equal("API", chart.Nodes[0].Label);
        Assert.Equal(new[] { "a|b", "x>0" }, chart.Edges.Select(edge => edge.Label));
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var xml = XDocument.Parse(svg);
        var captions = xml.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        foreach (var caption in new[] { "API", "Don't stop", "a|b", "x>0" }) Assert.Contains(caption, captions);
        Assert.NotEmpty(png);
        var roundTrip = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(new[] { "a|b", "x>0" }, roundTrip.Edges.Select(edge => edge.Label));
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = Path.Combine(captures, dark ? "caption-boundaries-dark" : "caption-boundaries-light");
        File.WriteAllText(name + ".svg", svg); File.WriteAllBytes(name + ".native.png", png);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = name + ".browser.png", FullPage = true });
    }

    [Theory]
    [InlineData("A@{label: 'can' t} --> B", "'can' t")]
    [InlineData("A@{shape: rect, label:   'can' t} --> B", "'can' t")]
    [InlineData("X --> Y; A@{label: 'can' t} --> B", "'can' t")]
    [InlineData("A@{'label' x: Value} --> B", "'label' x")]
    public void InvalidMetadataScalarReportsItsOriginalLocation(string line, string scalar) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\n" + line);
        var diagnostic = Assert.Single(parsed.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error);
        Assert.Equal(2, diagnostic.Span.Line);
        Assert.Equal(line.IndexOf(scalar, StringComparison.Ordinal) + 1, diagnostic.Span.Column);
        Assert.Equal(scalar.Length, diagnostic.Span.Length);
    }
}
