using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Reachable selector-list and forward-note siblings of the same retained generic-reference contract.</summary>
public sealed class MermaidClassGenericReferenceSiblingBoundaryTests {
    [Fact]
    public void QuotedWholeListRetainsItsGenericOccurrenceButAnExactQuotedIdentityAlwaysWins() {
        const string source = "classDiagram\nclass Pair\nclass Other\nclassDef marked fill:#fff\ncssClass \"Pair~K,V~,Other\" marked";
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        var warning = Assert.Single(result.Diagnostics); Assert.Equal((MermaidDiagnosticCodes.ApproximateMapping, 5, 1), (warning.Code, warning.Span.Line, warning.Span.Column));
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); Assert.Equal(new[] { "Pair", "Other" }, chart.Nodes.Select(node => node.Id));
        Assert.Equal(("Pair<K,V>", "Pair~K,V~"), (chart.Nodes[0].Label, chart.Nodes[0].Metadata["mermaid.generic.source"]));
        Assert.All(chart.Nodes, node => Assert.Equal("#fff", node.BackgroundColor));
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.Null(strict.Artifact); Assert.Single(strict.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ApproximateMapping);
        Assert.Single(strict.Diagnostics, item => item.Code == MermaidDiagnosticCodes.RejectedWarning);
        const string literal = "classDiagram\nclass Pair\nclass Other\nclass `Pair~K,V~,Other`[\"Literal list identity\"]\nclassDef marked fill:#fff\ncssClass \"Pair~K,V~,Other\" marked";
        var atomic = MermaidRenderer.Render(literal, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }); Assert.Empty(atomic.Diagnostics);
        var nodes = Assert.IsType<TopologyChart>(atomic.Artifact!.Model).Nodes;
        Assert.Equal(new[] { "Pair", "Other", "Pair~K,V~,Other" }, nodes.Select(node => node.Id));
        Assert.All(nodes.Take(2), node => Assert.Null(node.BackgroundColor));
        Assert.Equal(("Literal list identity", "#fff"), (nodes[2].Label, nodes[2].BackgroundColor)); Assert.False(nodes[2].Metadata.ContainsKey("mermaid.generic.source"));
    }

    [Fact]
    public void RepeatedOccurrencesWarnOnceEachAndForwardNotesDoNotInventOrDuplicateDeclaredClasses() {
        const string repeated = "classDiagram\nclass Pair\nclassDef marked fill:#fff\ncssClass Pair~K,V~,Pair~K,V~ marked";
        var result = MermaidRenderer.Render(repeated); Assert.False(result.HasErrors); Assert.Equal(2, result.Diagnostics.Count);
        Assert.All(result.Diagnostics, warning => Assert.Equal((MermaidDiagnosticCodes.ApproximateMapping, 4), (warning.Code, warning.Span.Line)));
        Assert.Equal("Pair~K,V~", Assert.Single(Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes).Metadata["mermaid.generic.source"]);
        const string forward = "classDiagram\nnote for Pair~K,V~ \"caption\"\nclass Pair";
        var accepted = MermaidRenderer.Render(forward); Assert.False(accepted.HasErrors);
        Assert.Equal((MermaidDiagnosticCodes.ApproximateMapping, 2), (Assert.Single(accepted.Diagnostics).Code, accepted.Diagnostics[0].Span.Line));
        var node = Assert.Single(Assert.IsType<TopologyChart>(accepted.Artifact!.Model).Nodes);
        Assert.Equal(("Pair", "Pair<K,V>", "Pair~K,V~"), (node.Id, node.Label, node.Metadata["mermaid.generic.source"]));
        Assert.Equal(3, Assert.Single(Assert.IsType<MermaidClassDocument>(accepted.Document).Classes).Span.Line);
        var strict = MermaidRenderer.Render(forward, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }); Assert.Null(strict.Artifact);
        var undeclared = MermaidRenderer.Render("classDiagram\nclass Other\nnote for Pair~K,V~ \"caption\"");
        Assert.Null(undeclared.Artifact); Assert.True(undeclared.HasErrors);
        Assert.Equal("Other", Assert.Single(Assert.IsType<MermaidClassDocument>(undeclared.Document).Classes).Id);
    }
}
