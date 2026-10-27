using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidClassNotationReviewRegressionTests {
    [Theory]
    [InlineData("Box~T~ : +run()")]
    [InlineData("<<interface>> Box~T~")]
    [InlineData("class Box~T~")]
    [InlineData("Box~T~ --> Other")]
    public void GenericReferencesRetainAnExplicitDisplayAliasAndItsSourceLocation(string reference) {
        var result = MermaidRenderer.Render("classDiagram\nclass Box~T~[\"Service\"]\n" + reference);
        Assert.Empty(result.Diagnostics);
        var document = Assert.IsType<MermaidClassDocument>(result.Document);
        var owner = document.Classes.Single(node => node.Id == "Box");
        Assert.Equal("Service", owner.Label);
        var node = Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes.Single(node => node.Id == "Box");
        Assert.Equal("Service", node.Label);
        Assert.Equal("Service", node.Metadata["mermaid.source.label"]);
        Assert.Equal("Box~T~", node.Metadata["mermaid.generic.source"]);
        Assert.Equal(2, owner.Span.Line);
    }

    [Theory]
    [InlineData("Pair~K,V~ --> Other")]
    [InlineData("Other --> Pair~K,V~")]
    [InlineData("Port ()-- Pair~K,V~")]
    [InlineData("Pair~K,V~ --() Port")]
    [InlineData("Pair~K,V~ ()-- Other")]
    [InlineData("Other --() Pair~K,V~")]
    public void EveryGenericRelationshipEndpointHonorsTheExistingStrictWarningPolicy(string relationship) {
        var result = MermaidRenderer.Render("classDiagram\n" + relationship,
            new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ApproximateMapping && item.Span.Line == 2);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.RejectedWarning && item.Span.Line == 1);
    }

    [Theory]
    [InlineData("Pair~K,V~ --> Other")]
    [InlineData("Other --> Pair~K,V~")]
    [InlineData("Port ()-- Pair~K,V~")]
    [InlineData("Pair~K,V~ --() Port")]
    public void ConcreteGenericEndpointsRetainRawGenericFactsAndTheirReadableCaption(string relationship) {
        var result = MermaidRenderer.Render("classDiagram\n" + relationship);
        Assert.False(result.HasErrors);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ApproximateMapping && item.Span.Line == 2);
        var node = Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes.Single(node => node.Id == "Pair");
        Assert.Equal("Pair<K,V>", node.Label);
        Assert.Equal("Pair~K,V~", node.Metadata["mermaid.generic.source"]);
        Assert.DoesNotContain(Assert.IsType<MermaidClassDocument>(result.Document).Classes, item => item.Id.Contains('~'));
    }

    [Theory]
    [InlineData("Pair~T~ --> Other", "Pair", "Pair<T>")]
    [InlineData("`Pair~K,V~` --> Other", "Pair~K,V~", "Pair~K,V~")]
    public void StandardGenericsAndQuotedLiteralEndpointsRemainWarningFree(string relationship, string id, string label) {
        var result = MermaidRenderer.Render("classDiagram\n" + relationship,
            new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.Empty(result.Diagnostics);
        Assert.Equal(label, Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes.Single(node => node.Id == id).Label);
    }

    [Fact]
    public void ReopenedNamespaceCaptionsKeepReplacementInlineContentInDirectAndFencedInput() {
        const string source = "classDiagram\nnamespace N {\n class First\n}\n  namespace N[\"`**Updated**`\"] {\n class Second\n}";
        var result = MermaidRenderer.Render(source, BrowserProofFonts.MermaidOptions());
        Assert.False(result.HasErrors);
        Assert.Empty(result.Diagnostics);
        var group = Assert.Single(Assert.IsType<MermaidClassDocument>(result.Document).Namespaces);
        Assert.Equal(2, group.Span.Line);
        Assert.Equal("`**Updated**`", group.Label);
        var native = Assert.Single(Assert.IsType<TopologyChart>(result.Artifact!.Model).Groups);
        Assert.Equal("Updated", native.Label);
        Assert.Equal(ChartForgeX.Typography.TextInlineEmphasis.Bold, Assert.Single(native.LabelContent.Runs).Emphasis);
        var fence = new MermaidVisualMarkupParser(BrowserProofFonts.MermaidOptions()).Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.False(fence.HasErrors);
        Assert.Empty(fence.Diagnostics);
        Assert.Equal(result.Artifact.ToSvg(), Assert.Single(fence.Artifacts).ToSvg());
        Assert.Equal(result.Artifact.ToPng(), fence.Artifacts[0].ToPng());
    }

    [Fact]
    public void LoweredClassRelationshipBreaksPaintEveryAuthoredCaptionRow() {
        var result = MermaidRenderer.Render("classDiagram\nA --> B : owns<br>many");
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        Assert.Equal("owns\nmany", Assert.Single(Assert.IsType<TopologyChart>(artifact.Model).Edges).Label);
        var xml = XDocument.Parse(artifact.ToSvg());
        var label = Assert.Single(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-edge-label");
        Assert.Equal("2", (string?)label.Attribute("data-label-line-count"));
        var text = label.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        Assert.Equal(new[] { "owns", "many" }, text);
    }
}
