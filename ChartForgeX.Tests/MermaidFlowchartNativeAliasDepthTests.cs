using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartNativeAliasDepthTests {
    [Theory]
    [InlineData("bang", "ScallopedBurst")]
    [InlineData("h-cyl", "HorizontalCylinder")]
    [InlineData("das", "HorizontalCylinder")]
    [InlineData("horizontal-cylinder", "HorizontalCylinder")]
    public void AuthoredAliasesReachExistingNativeSurfacesAndRetainSourceFacts(string alias, string family) {
        var statement = "notice@{ shape: " + alias + ", label: \"Stored notice\" }";
        var source = "flowchart LR\n" + statement;
        var parsed = new MermaidParser().ParseFlowchart(source);
        var artifact = parsed.Document!.ToVisualArtifact();
        Capture(artifact, "flowchart-alias-" + alias);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        var node = Assert.Single(document.Nodes);
        Assert.Equal(family, node.Shape.ToString());
        Assert.Equal(alias, node.Properties["shape"]);
        Assert.Equal("Stored notice", node.Text);
        Assert.Equal(2, node.Span.Line);
        Assert.Equal(1, node.Span.Column);
        var retained = Assert.Single(document.Statements);
        Assert.Equal(statement, retained.Text);
        Assert.Equal(2, retained.Span.Line);
        var topology = Assert.Single(document.ToTopologyChart().Nodes);
        Assert.Equal(family, topology.Shape.ToString());
        Assert.Equal(alias, topology.Metadata["mermaid.property.shape"]);
        Assert.Equal(family, topology.Metadata["mermaid.shape"]);
        var facts = artifact.ToInterchangeEnvelope();
        var portable = Assert.Single(facts.Nodes);
        Assert.Equal(family, portable.Topology!.Shape.ToString());
        Assert.Equal(alias, portable.Extensions["mermaid.property.shape"]);
        Assert.Equal(facts.ToJson(), VisualArtifactInterchangeEnvelope.FromUtf8Json(facts.ToUtf8Json()).ToJson());
        Assert.Contains("data-node-shape=\"" + family + "\"", artifact.ToSvg(), StringComparison.Ordinal);
        Assert.NotEmpty(artifact.ToPng());
    }

    [Theory]
    [InlineData("bang")]
    [InlineData("h-cyl")]
    [InlineData("das")]
    [InlineData("horizontal-cylinder")]
    public void DirectAndFenceArtifactsShareTheMappedStaticSurface(string alias) {
        var source = "flowchart LR\nfirst@{ shape: " + alias + ", label: \"Stored notice\" } --> second[Continue]";
        var direct = MermaidRenderer.Render(source);
        Assert.Empty(direct.Diagnostics);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.False(fence.HasErrors);
        var fenced = Assert.Single(fence.Artifacts);
        Capture(direct.Artifact!, "flowchart-alias-" + alias + "-direct"); Capture(fenced, "flowchart-alias-" + alias + "-fenced");
        Assert.Equal(direct.Artifact!.ToSvg(), fenced.ToSvg());
        Assert.Equal(direct.Artifact!.ToPng(), fenced.ToPng());
        var directFacts = direct.Artifact!.ToInterchangeEnvelope(); var fenceFacts = fenced.ToInterchangeEnvelope();
        Assert.Equal("mermaid", fenceFacts.Extensions["fence"]); Assert.Equal("1", fenceFacts.Extensions["sourceLine"]);
        Assert.Equal("2", fenceFacts.Extensions["payloadStartLine"]); Assert.Equal("3", fenceFacts.Extensions["payloadEndLine"]);
        foreach (var key in new[] { "fence", "sourceLine", "payloadStartLine", "payloadEndLine" }) {
            Assert.False(directFacts.Extensions.ContainsKey(key)); Assert.True(fenceFacts.Extensions.Remove(key));
        }
        Assert.Equal(directFacts.ToJson(), fenceFacts.ToJson());
        Assert.DoesNotContain(XDocument.Parse(direct.Artifact!.ToSvg()).Descendants(), element =>
            element.Name.LocalName is "script" or "foreignObject" or "image");
    }

    [Theory]
    [InlineData("bang")]
    [InlineData("h-cyl")]
    [InlineData("das")]
    [InlineData("horizontal-cylinder")]
    public void FlowchartAliasesDoNotExpandTheAgentflowShapeCatalog(string alias) {
        var source = "agentflow-beta\nnode@{ shape: " + alias + " }";
        var permissive = MermaidRenderer.Render(source);
        Assert.False(permissive.HasErrors);
        var warning = Assert.Single(permissive.Diagnostics);
        Assert.Equal(MermaidDiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, warning.Code);
        Assert.Equal(2, warning.Span.Line);
        var node = Assert.Single(Assert.IsType<TopologyChart>(permissive.Artifact!.Model).Nodes);
        Assert.Equal(TopologyNodeShape.Rounded, node.Shape);
        Assert.Equal("\"" + alias + "\"", node.Metadata["mermaid.metadata.shape"]);
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.True(strict.HasErrors);
        Assert.Null(strict.Artifact);
    }

    [Fact]
    public void OrdinaryShapesRetainTheExistingStaticContract() {
        const string source = "flowchart LR\nfirst[Ordinary] --> second[(Records)]\nsecond --> third((Complete))";
        var result = MermaidRenderer.Render(source);
        Capture(result.Artifact!, "flowchart-alias-ordinary-control");
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal(new[] { TopologyNodeShape.Rectangle, TopologyNodeShape.Cylinder, TopologyNodeShape.Circle }, chart.Nodes.Select(node => node.Shape!.Value));
    }

    private static void Capture(VisualArtifact artifact, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_FLOWCHART_ALIAS_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var stem = Path.Combine(output, name);
        File.WriteAllText(stem + ".svg", artifact.ToSvg());
        File.WriteAllBytes(stem + ".png", artifact.ToPng());
        File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
    }
}
