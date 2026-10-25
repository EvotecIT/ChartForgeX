using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidAgentflowShapePolicyTests {
    [Theory]
    [InlineData("task", TopologyNodeShape.Rounded)]
    [InlineData("round", TopologyNodeShape.Rounded)]
    [InlineData("rect", TopologyNodeShape.Rounded)]
    [InlineData("connector", TopologyNodeShape.Rounded)]
    [InlineData("tool", TopologyNodeShape.Subroutine)]
    [InlineData("subroutine", TopologyNodeShape.Subroutine)]
    [InlineData("subprocess", TopologyNodeShape.Subroutine)]
    [InlineData("subproc", TopologyNodeShape.Subroutine)]
    [InlineData("framed-rectangle", TopologyNodeShape.Subroutine)]
    [InlineData("input", TopologyNodeShape.Parallelogram)]
    [InlineData("lean-right", TopologyNodeShape.Parallelogram)]
    [InlineData("decision", TopologyNodeShape.Diamond)]
    [InlineData("diamond", TopologyNodeShape.Diamond)]
    [InlineData("refdoc", TopologyNodeShape.LinedDocument)]
    [InlineData("lin-doc", TopologyNodeShape.LinedDocument)]
    [InlineData("lined-document", TopologyNodeShape.LinedDocument)]
    [InlineData("action", TopologyNodeShape.Hexagon)]
    [InlineData("hexagon", TopologyNodeShape.Hexagon)]
    [InlineData("hex", TopologyNodeShape.Hexagon)]
    public void FamilyDefaultsAndCanonicalAliasesUseExistingNativeOwners(string authored, TopologyNodeShape expected) {
        var source = "agentflow-beta\nitem[\"Work\"]@{ shape: " + authored + " }";
        var parsed = new MermaidParser().ParseAgentflow(source);
        var artifact = parsed.Document!.ToVisualArtifact();
        Capture(artifact, "agentflow-policy-" + authored);
        Assert.Empty(parsed.Diagnostics);
        var item = Assert.Single(parsed.Document!.Nodes);
        Assert.Equal(authored, item.Shape);
        Assert.Equal(authored, item.Metadata["shape"].Text);
        Assert.Equal(source, parsed.Document!.SourceText);
        Assert.Equal(expected, Assert.Single(parsed.Document!.ToTopologyChart().Nodes).Shape);
        var facts = artifact.ToInterchangeEnvelope();
        Assert.Equal(expected, Assert.Single(facts.Nodes).Topology!.Shape);
        Assert.Equal("\"" + authored + "\"", facts.Nodes[0].Extensions["mermaid.metadata.shape"]);
    }

    [Theory]
    [InlineData("doc")] [InlineData("stadium")] [InlineData("terminal")] [InlineData("circle")]
    [InlineData("trapezoid")] [InlineData("inv_trapezoid")] [InlineData("inv-trapezoid")]
    [InlineData("doublecircle")] [InlineData("double-circle")] [InlineData("typeDeclaration")]
    [InlineData("procs")] [InlineData("lean_left")] [InlineData("lean-left")] [InlineData("in-out")]
    [InlineData("cylinder")] [InlineData("ellipse")] [InlineData("odd")] [InlineData("tag-rect")]
    [InlineData("tagged-rectangle")] [InlineData("delay")] [InlineData("half-rounded-rectangle")]
    [InlineData("lin-rect")] [InlineData("lined-rectangle")] [InlineData("win-pane")]
    [InlineData("window-pane")] [InlineData("curv-trap")] [InlineData("curved-trapezoid")]
    public void RemovedFamilyShapesAreLocatedSourceErrorsWithRawMetadataAndNoArtifact(string authored) {
        var source = "agentflow-beta\nitem@{ shape: " + authored + " }";
        var before = MermaidRenderer.Render(source);
        if (before.Artifact != null) Capture(before.Artifact, "agentflow-policy-removed-" + authored);
        var parsed = new MermaidParser().ParseAgentflow(source);
        Assert.True(parsed.HasErrors);
        Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement
            && diagnostic.Severity == MermaidDiagnosticSeverity.Error && diagnostic.Span.Line == 2 && diagnostic.Span.Column > 0);
        Assert.Equal(authored, Assert.Single(parsed.Document!.Nodes).Metadata["shape"].Text);
        Assert.Null(before.Artifact);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fence.HasErrors);
        Assert.Empty(fence.Artifacts);
    }

    [Theory]
    [InlineData("rectangle")] [InlineData("proc")] [InlineData("process")] [InlineData("rounded")]
    [InlineData("event")] [InlineData("pill")] [InlineData("fr-rect")] [InlineData("cyl")]
    [InlineData("db")] [InlineData("database")] [InlineData("circ")] [InlineData("dbl-circ")]
    [InlineData("diam")] [InlineData("question")] [InlineData("prepare")] [InlineData("lean-r")]
    [InlineData("lean-l")] [InlineData("out-in")] [InlineData("trap-b")] [InlineData("trapezoid-bottom")]
    [InlineData("priority")] [InlineData("trap-t")] [InlineData("trapezoid-top")] [InlineData("manual")]
    [InlineData("cloud")] [InlineData("bang")] [InlineData("h-cyl")] [InlineData("das")]
    [InlineData("horizontal-cylinder")] [InlineData("imaginary")]
    public void FlowchartOnlyAndUnknownShapesStayInertWithTheFamilyDefault(string authored) {
        var source = "agentflow-beta\nitem@{ shape: " + authored + " }";
        var rendered = MermaidRenderer.Render(source);
        if (rendered.Artifact != null) Capture(rendered.Artifact, "agentflow-policy-unsupported-" + authored);
        Assert.False(rendered.HasErrors);
        var warning = Assert.Single(rendered.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, warning.Code);
        Assert.Equal(MermaidDiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal(2, warning.Span.Line);
        var node = Assert.Single(Assert.IsType<TopologyChart>(rendered.Artifact!.Model).Nodes);
        Assert.Equal(TopologyNodeShape.Rounded, node.Shape);
        Assert.Equal("\"" + authored + "\"", node.Metadata["mermaid.metadata.shape"]);
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.True(strict.HasErrors);
        Assert.Null(strict.Artifact);
    }

    [Fact]
    public void DocumentedSixAliasesAndInertMetadataRetainTheirOrdinaryContract() {
        const string source = "agentflow-beta LR\na@{ shape: task, labels: [safe, manual] } --> b@{ shape: tool }\n"
            + "b --> c@{ shape: input }\nc --> d@{ shape: decision }\nd -.- e@{ shape: refdoc }\nd --> f@{ shape: action }";
        var rendered = MermaidRenderer.Render(source);
        Capture(rendered.Artifact!, "agentflow-policy-ordinary-control");
        Assert.Empty(rendered.Diagnostics);
        var document = Assert.IsType<MermaidAgentflowDocument>(rendered.Document);
        Assert.Equal(new[] { "a", "b", "c", "d", "e", "f" }, document.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { "task", "tool", "input", "decision", "refdoc", "action" }, document.Nodes.Select(node => node.Shape));
        Assert.Equal(new[] { "safe", "manual" }, document.Nodes[0].Metadata["labels"].Items.Select(value => value.Text));
        Assert.Equal(new[] { TopologyNodeShape.Rounded, TopologyNodeShape.Subroutine, TopologyNodeShape.Parallelogram,
            TopologyNodeShape.Diamond, TopologyNodeShape.LinedDocument, TopologyNodeShape.Hexagon },
            Assert.IsType<TopologyChart>(rendered.Artifact!.Model).Nodes.Select(node => node.Shape!.Value));
    }

    [Theory]
    [InlineData("circle")] [InlineData("cloud")] [InlineData("imaginary")]
    public void UnsupportedProgrammaticShapesFailClearlyAtThePublicConverter(string shape) {
        var document = new MermaidAgentflowDocument();
        document.Nodes.Add(new MermaidAgentflowNode("work", new MermaidSourceSpan(1, 1, 4)) { Shape = shape });
        Assert.Throws<ArgumentException>(() => document.ToTopologyChart());
    }

    [Theory]
    [InlineData("squareRect", "Rounded")]
    [InlineData("roundedRect", "Rounded")]
    [InlineData("collapsedGroup", "CollapsedContainer")]
    public void InternalCanonicalNamesRemainProgrammaticWhileAuthoredNamesStayLowercase(string shape, string native) {
        var document = new MermaidAgentflowDocument();
        document.Nodes.Add(new MermaidAgentflowNode("work", new MermaidSourceSpan(1, 1, 4)) { Shape = shape });
        Assert.Equal(native, Assert.Single(document.ToTopologyChart().Nodes).Shape.ToString());
        var source = "agentflow-beta\nwork@{ shape: " + shape + " }";
        var parsed = new MermaidParser().ParseAgentflow(source);
        Assert.True(parsed.HasErrors);
        Assert.Equal(shape, Assert.Single(parsed.Document!.Nodes).Metadata["shape"].Text);
        Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement && diagnostic.Span.Line == 2);
        Assert.Null(MermaidRenderer.Render(source).Artifact);
    }

    [Fact]
    public void FullFamilyFixtureUsesTheSameStaticDirectFenceAndPortableContract() {
        var source = Fixture();
        var direct = MermaidRenderer.Render(source);
        Assert.Empty(direct.Diagnostics);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.False(fence.HasErrors);
        var artifact = direct.Artifact!;
        Assert.Equal(artifact.ToSvg(), Assert.Single(fence.Artifacts).ToSvg());
        Assert.Equal(artifact.ToPng(), Assert.Single(fence.Artifacts).ToPng());
        var facts = artifact.ToInterchangeEnvelope();
        Assert.Equal(facts.ToJson(), VisualArtifactInterchangeEnvelope.FromUtf8Json(facts.ToUtf8Json()).ToJson());
        Assert.Equal(8, facts.Nodes.Count);
        Assert.Equal(7, facts.Edges.Count);
        Assert.Single(facts.Groups);
        Assert.DoesNotContain(XDocument.Parse(artifact.ToSvg()).Descendants(), element => element.Name.LocalName is "script" or "foreignObject" or "image" or "a");
    }

    internal static string Fixture() => File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "agentflow-family-shapes.mmd"));
    private static void Capture(VisualArtifact artifact, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_AGENTFLOW_POLICY_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var stem = Path.Combine(output, name);
        File.WriteAllText(stem + ".svg", artifact.ToSvg());
        File.WriteAllBytes(stem + ".png", artifact.ToPng());
        File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
    }
}
