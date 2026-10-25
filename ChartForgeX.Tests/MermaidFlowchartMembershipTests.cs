using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class MermaidFlowchartMembershipTests {
    internal static string Fixture(string family) => File.ReadAllText(Path.Combine(TestRepository.Root,
        "ChartForgeX.Tests", "Fixtures", "Mermaid", "flowchart-membership.mmd")).Replace("flowchart LR", family + " LR", StringComparison.Ordinal);
    internal static MermaidRenderOptions Options(bool dark, int width) => new() {
        Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family },
        Flowchart = new MermaidFlowchartRenderOptions { Width = width, Height = 700 }, Swimlane = new MermaidFlowchartRenderOptions { Width = width, Height = 700 }
    };

    [Theory]
    [InlineData("flowchart")] [InlineData("swimlane-beta")]
    public void CompleteSourceResolvesFirstClosedMembershipWithoutChangingRawReferencesSpansOrderOrLabelUpdates(string family) {
        var source = Fixture(family); var parsed = new MermaidParser().ParseFlowchart(source); Assert.Empty(parsed.Diagnostics); var document = parsed.Document!;
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "flowchart-membership.expected.json")));
        Assert.Equal(source, document.SourceText); Assert.Equal(expected.RootElement.GetProperty("sourceLines").EnumerateArray().Select(value => value.GetInt32()), document.Statements.Select(statement => statement.Span.Line));
        Assert.Equal(expected.RootElement.GetProperty("nodeIds").EnumerateArray().Select(value => value.GetString()), document.Nodes.Select(node => node.Id));
        Assert.Equal(expected.RootElement.GetProperty("nodeParents").EnumerateArray().Select(value => value.GetString()), document.Nodes.Select(node => node.SubgraphId));
        Assert.Equal(expected.RootElement.GetProperty("groupIds").EnumerateArray().Select(value => value.GetString()), document.Subgraphs.Select(group => group.Id));
        for (var index = 0; index < document.Subgraphs.Count; index++)
            Assert.Equal(expected.RootElement.GetProperty("rawGroupNodeReferences")[index].EnumerateArray().Select(value => value.GetString()), document.Subgraphs[index].NodeIds);
        var n = document.Nodes.Single(node => node.Id == "N"); var q = document.Nodes.Single(node => node.Id == "Q");
        Assert.Equal("Updated", n.Text); Assert.Equal(6, n.Span.Line); Assert.Equal("Persist", q.Text); Assert.Equal(4, q.Span.Line);
        Assert.Contains(document.Statements, statement => statement.Span.Line == 9 && statement.Text == "X --> N[Updated]");
        var chart = document.ToTopologyChart(); Assert.Equal(document.Nodes.Select(node => node.SubgraphId), chart.Nodes.Select(node => node.GroupId));
        Assert.Equal(expected.RootElement.GetProperty("edges").EnumerateArray().Select(value => (value[0].GetString(), value[1].GetString())), chart.Edges.Select(edge => ((string?)edge.SourceNodeId, (string?)edge.TargetNodeId)));
        var artifact = document.ToVisualArtifact(); Capture(artifact, "flowchart-membership-" + family); var json = artifact.ToInterchangeJson();
        var saved = VisualArtifactInterchangeEnvelope.FromJson(json); var restored = saved.ToTopologyChart();
        Assert.Equal(document.Nodes.Select(node => node.SubgraphId), saved.Nodes.Select(node => node.GroupId));
        Assert.Equal(saved.Nodes.Select(node => node.GroupId), restored.Nodes.Select(node => node.GroupId));
        Assert.Equal("6", saved.Nodes.Single(node => node.Id == "N").Extensions["mermaid.source.line"]);
        Assert.Equal("Updated", saved.Nodes.Single(node => node.Id == "N").Extensions["mermaid.label"]); Assert.True(artifact.ToPng().Length > 64);
    }

    [Theory]
    [InlineData("flowchart", false, 390)] [InlineData("flowchart", true, 1100)]
    [InlineData("swimlane-beta", false, 390)] [InlineData("swimlane-beta", true, 1100)]
    public void OrdinaryDirectAndMarkdownConsumersShareResolvedNativeIdentityAndSourceLabelPaint(string family, bool dark, int width) {
        var source = Fixture(family); var options = Options(dark, width); var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        var markup = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(markup.HasErrors);
        var artifact = direct.Artifact!; Capture(artifact, "flowchart-membership-host-" + family + "-" + width); var other = Assert.Single(markup.Artifacts);
        Assert.Equal(artifact.ToSvg(), other.ToSvg()); Assert.Equal(artifact.ToPng(), other.ToPng());
        var saved = artifact.ToInterchangeEnvelope(); Assert.Equal("Inner", saved.Nodes.Single(node => node.Id == "N").GroupId); Assert.Equal("Inner", saved.Nodes.Single(node => node.Id == "Q").GroupId);
        var xml = XDocument.Parse(artifact.ToSvg()); var text = xml.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        Assert.Contains("Updated", text); Assert.DoesNotContain("Original", text); Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "image" or "foreignObject");
    }

    [Theory]
    [InlineData("flowchart")] [InlineData("swimlane-beta")]
    public void RepeatedRawReferencesDoNotDoubleCountSemanticItemsAndTheExistingBudgetStillReportsItsLocatedBoundary(string family) {
        var source = Fixture(family); var parser = new MermaidParser();
        var exact = parser.Parse(source, new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 8 } }); Assert.False(exact.HasErrors); Assert.NotNull(exact.Document);
        var over = parser.Parse(source, new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 7 } }); Assert.Null(over.Document);
        var diagnostic = Assert.Single(over.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        Assert.Contains(nameof(MermaidResourceLimits.MaximumElements), diagnostic.Message); Assert.Equal(11, diagnostic.Span.Line);
        var fenced = new MermaidVisualMarkupParser(new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 7 } } })
            .Parse("```mermaid\n" + source + "\n```"); Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
        Assert.Contains(fenced.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && diagnostic.Line > 1);
    }

    [Theory]
    [InlineData("flowchart", "subgraph Outer[Outer]\nsubgraph Inner[Inner]\nN[Node]\nend", 2)]
    [InlineData("swimlane-beta", "subgraph Outer[Outer]\nsubgraph Inner[Inner]\nN[Node]\nend", 2)]
    [InlineData("flowchart", "subgraph G[Group]\nN\nend\nend", 5)]
    [InlineData("swimlane-beta", "subgraph G[Group]\nN\nend\nend", 5)]
    public void MissingAndUnmatchedClosuresKeepExistingLocatedInvalidInputBehavior(string family, string body, int line) {
        var source = family + " LR\n" + body; var direct = MermaidRenderer.Render(source); Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
        Assert.Contains(direct.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement && diagnostic.Span.Line == line);
        var markup = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.True(markup.HasErrors); Assert.Empty(markup.Artifacts);
    }

    internal static void Capture(VisualArtifact artifact, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), artifact.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), artifact.ToPng()); File.WriteAllText(Path.Combine(output, name + ".native.json"), artifact.ToInterchangeJson());
    }
}
