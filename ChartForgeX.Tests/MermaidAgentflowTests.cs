using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidAgentflowTests {
    [Fact]
    public void TypedWorkflowKeepsNestedMetadataGlobalContainmentAndEveryEdgeMeaning() {
        var result = new MermaidParser().Parse(Fixture());
        Assert.Empty(result.Diagnostics);
        var document = Assert.IsType<MermaidAgentflowDocument>(result.Document);
        Assert.False(document.IsDiagnosticOnly);
        Assert.Equal("LR", document.Direction);
        Assert.Equal(new[] { "team", "worker" }, document.Flows.Select(flow => flow.Id));
        Assert.Equal("team", document.Flows[1].ParentId);
        Assert.True(document.Flows[1].Collapsed);
        var shared = document.Nodes.Single(node => node.Id == "shared");
        Assert.True(shared.IsGlobal);
        Assert.Null(shared.ParentFlowId);
        var tool = document.Nodes.Single(node => node.Id == "read");
        Assert.Equal("worker", tool.ParentFlowId);
        Assert.Equal("tool", tool.Shape);
        Assert.Equal("store.read", tool.Metadata["connectorRef"].Text);
        Assert.Equal(2d, tool.Metadata["policy"].Properties["retries"].Number);
        Assert.True(tool.Metadata["policy"].Properties["enabled"].Boolean);
        Assert.Equal(new[] { "safe", "deterministic" }, tool.Metadata["policy"].Properties["tags"].Items.Select(item => item.Text));
        Assert.False(tool.Metadata["policy"].Properties.ContainsKey("constructor"));
        Assert.Equal(new[] { MermaidAgentflowEdgeKind.Sequence, MermaidAgentflowEdgeKind.Sequence, MermaidAgentflowEdgeKind.Reference,
            MermaidAgentflowEdgeKind.Sequence, MermaidAgentflowEdgeKind.Failure }, document.Edges.Select(edge => edge.Kind));
        Assert.Equal("Only after validation", document.Edges[0].Metadata["instruction"].Text);
        Assert.DoesNotContain(document.Nodes, node => node.Id is "worker" or "team" or "edge1");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CollapsedFlowsPreserveCrossingEdgesAndDirectFenceNativeOutput(bool dark) {
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + Fixture();
        var direct = MermaidRenderer.Render(source);
        Assert.Empty(direct.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(direct.Artifact!.Model);
        Assert.Contains(chart.Nodes, node => node.Id == "worker" && node.Metadata["mermaid.collapsed"] == "true");
        Assert.DoesNotContain(chart.Nodes, node => node.Id is "read" or "gate");
        Assert.Equal(3, chart.Edges.Count);
        Assert.Contains(chart.Edges, edge => edge.SourceNodeId == "worker" && edge.TargetNodeId == "shared" && edge.LineStyle == TopologyEdgeLineStyle.Dashed && edge.Direction == VisualLinkDirection.None);
        Assert.Contains(chart.Edges, edge => edge.SourceNodeId == "brief" && edge.TargetNodeId == "worker");
        Assert.Contains(chart.Edges, edge => edge.SourceNodeId == "worker" && edge.TargetNodeId == "publish" && edge.TargetMarker == TopologyMarkerKind.Cross);
        var connector = chart.Nodes.Single(node => node.Id == "store");
        Assert.Null(connector.Href);
        using var inertEndpoint = JsonDocument.Parse(connector.Metadata["mermaid.metadata.endpoint"]);
        Assert.Equal("https://example.invalid/data", inertEndpoint.RootElement.GetString());
        var svg = direct.Artifact.ToSvg(); var png = direct.Artifact.ToPng();
        var parsed = XDocument.Parse(svg);
        Assert.DoesNotContain(parsed.Descendants(), item => item.Name.LocalName is "script" or "foreignObject" or "a");
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.False(fence.HasErrors);
        Assert.Equal(svg, Assert.Single(fence.Artifacts).ToSvg());
        Assert.Equal(png, Assert.Single(fence.Artifacts).ToPng());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(direct.Artifact.ToInterchangeJson());
        Assert.Equal(chart.Edges.Select(edge => edge.Metadata["mermaid.agentflow.kind"]), envelope.Edges.Select(edge => edge.Extensions["mermaid.agentflow.kind"]));
        Capture("agentflow-collapsed" + (dark ? "-dark" : "-light"), svg, png);
    }

    [Theory]
    [InlineData("task", TopologyNodeShape.Rounded)]
    [InlineData("tool", TopologyNodeShape.Subroutine)]
    [InlineData("input", TopologyNodeShape.Parallelogram)]
    [InlineData("decision", TopologyNodeShape.Diamond)]
    [InlineData("refdoc", TopologyNodeShape.LinedDocument)]
    [InlineData("action", TopologyNodeShape.Hexagon)]
    public void DomainAliasesUseCanonicalNativeShapes(string shape, TopologyNodeShape expected) {
        var result = MermaidRenderer.Render("agentflow-beta\na[\"Node\"]@{ shape: " + shape + " }");
        Assert.Empty(result.Diagnostics);
        Assert.Equal(expected, Assert.Single(Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes).Shape);
        Assert.NotEmpty(result.Artifact.ToPng());
    }

    [Theory]
    [InlineData("flow A[Flow]\na --> b")]
    [InlineData("end")]
    [InlineData("global named\na")]
    [InlineData("a@{ value: .inf }")]
    [InlineData("a@{ repeated: 1, repeated: 2 }")]
    [InlineData("a@{ instruction: \"Never closed\"")]
    public void MalformedWorkflowProducesLocatedErrorsAndNoDirectOrFenceArtifact(string body) {
        var source = "agentflow-beta\n" + body;
        var result = MermaidRenderer.Render(source);
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Span.Line > 1 && diagnostic.Severity == MermaidDiagnosticSeverity.Error);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
    }

    [Fact]
    public void UnsupportedPresentationIsDiagnosedAndRejectedByTheExistingStrictPolicy() {
        const string source = "agentflow-beta\na --> b\na@{ shape: imaginary, animate: true }";
        var permissive = MermaidRenderer.Render(source);
        Assert.False(permissive.HasErrors);
        Assert.Equal(2, permissive.Diagnostics.Count);
        Assert.All(permissive.Diagnostics, item => Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, item.Code));
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.True(strict.HasErrors); Assert.Null(strict.Artifact);
    }

    [Fact]
    public void MetadataDepthValuesAndCooperativeCancellationUseTheCapturedPolicy() {
        var bounded = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumNestingDepth = 2 } };
        var result = new MermaidParser().Parse("agentflow-beta\na@{ policy: { inner: { value: true } } }", bounded);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => new MermaidParser().Parse(Fixture(), null, cancellation.Token));
        Assert.Throws<ArgumentOutOfRangeException>(() => MermaidMetadataValue.FromNumber(double.PositiveInfinity, new MermaidSourceSpan(1, 1, 1)));
    }

    [Fact]
    public void LiteralInstructionsDoNotBecomeSharedConfigurationOrAccessibilityDeclarations() {
        const string source = "agentflow-beta\na@{\n  shape: task\n  instruction: |\n    %%{init: {'theme':'dark'}}%%\n    accTitle: Literal title\n    } stays literal\n}";
        var parsed = new MermaidParser().Parse(source);
        Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidAgentflowDocument>(parsed.Document);
        Assert.Null(document.Theme);
        Assert.Null(document.Accessibility.Name);
        var instruction = Assert.Single(document.Nodes).Metadata["instruction"].Text;
        Assert.Contains("%%{init: {'theme':'dark'}}%%", instruction);
        Assert.Contains("accTitle: Literal title", instruction);
        Assert.Contains("} stays literal", instruction);
    }

    private static string Fixture() => File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "agentflow-native-workflow.mmd"));
    private static void Capture(string name, string svg, byte[] png) {
        var root = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(root)) return;
        Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, name + ".native.svg"), svg); File.WriteAllBytes(Path.Combine(root, name + ".native.png"), png);
    }
}
