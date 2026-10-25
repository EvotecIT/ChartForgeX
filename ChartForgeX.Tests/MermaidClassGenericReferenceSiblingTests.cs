using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Closes accepted Class reference paths over the existing generic warning/source-fact invariant.</summary>
public sealed class MermaidClassGenericReferenceSiblingTests {
    private const string Generic = "Pair~K,V~";
    public static TheoryData<string> References => new() {
        "note for $id \"caption\"",
        "link $id \"https://example.invalid/\"",
        "click $id href \"https://example.invalid/\"",
        "style $id fill:#fff",
        "classDef marked fill:#fff\ncssClass $id marked"
    };

    [Theory]
    [MemberData(nameof(References))]
    public void AcceptedCommaGenericReferencesReportOneLocatedExtensionAndRetainRawNativeFacts(string template) {
        var source = Source(template, Generic); var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors); var warning = Assert.Single(result.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.ApproximateMapping, warning.Code); Assert.Equal(MermaidDiagnosticSeverity.Warning, warning.Severity);
        var statement = source.Split('\n').Last();
        Assert.Equal((source.Split('\n').Length, 1, statement.Length), (warning.Span.Line, warning.Span.Column, warning.Span.Length));
        var document = Assert.IsType<MermaidClassDocument>(result.Document); Assert.Empty(document.RawStatements);
        var owner = Assert.Single(document.Classes); Assert.Equal("Pair", owner.Id); Assert.Equal(2, owner.Span.Line);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); var node = Assert.Single(chart.Nodes);
        Assert.Equal(("Pair", "Pair<K,V>", Generic), (node.Id, node.Label, node.Metadata["mermaid.generic.source"]));
        AssertEffect(template, document, chart);
        var facts = VisualArtifactInterchangeEnvelope.FromJson(result.Artifact.ToInterchangeJson());
        Assert.Equal(Generic, Assert.Single(facts.Nodes).Extensions["mermaid.generic.source"]);
        Assert.Equal(source, document.SourceText);
    }

    [Theory]
    [MemberData(nameof(References))]
    public void AcceptedCommaGenericReferencesHonorTheExistingStrictWarningPolicy(string template) {
        var source = Source(template, Generic); var result = MermaidRenderer.Render(source,
            new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.Null(result.Artifact);
        var warning = Assert.Single(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ApproximateMapping);
        Assert.Equal(source.Split('\n').Length, warning.Span.Line);
        var rejected = Assert.Single(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.RejectedWarning);
        Assert.Equal(1, rejected.Span.Line); Assert.Equal(MermaidDiagnosticSeverity.Error, rejected.Severity);
        Assert.Equal("Pair", Assert.Single(Assert.IsType<MermaidClassDocument>(result.Document).Classes).Id);
    }

    [Theory]
    [MemberData(nameof(References))]
    public void QuotedLiteralTildeCommaIdentityAndExplicitGenericAliasKeepTheirOwnMeaning(string template) {
        foreach (var quote in new[] { "`", "\"" }) {
            var identity = quote + Generic + quote;
            var source = Source(template, identity, "class " + identity + "[\"Literal alias\"]");
            var result = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
            Assert.Empty(result.Diagnostics); var document = Assert.IsType<MermaidClassDocument>(result.Document); Assert.Empty(document.RawStatements);
            var owner = Assert.Single(document.Classes); Assert.Equal(Generic, owner.Id); Assert.Equal("Literal alias", owner.Label);
            var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); var node = Assert.Single(chart.Nodes);
            Assert.Equal(Generic, node.Id); Assert.Equal("Literal alias", node.Label); Assert.False(node.Metadata.ContainsKey("mermaid.generic.source"));
            AssertEffect(template, document, chart);
        }
        var aliased = MermaidRenderer.Render(Source(template, "Pair~T~", "class Pair~T~[\"Service\"]"),
            new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.Empty(aliased.Diagnostics); var alias = Assert.Single(Assert.IsType<TopologyChart>(aliased.Artifact!.Model).Nodes);
        Assert.Equal(("Pair", "Service", "Pair~T~"), (alias.Id, alias.Label, alias.Metadata["mermaid.generic.source"]));
        Assert.Equal(2, Assert.Single(Assert.IsType<MermaidClassDocument>(aliased.Document).Classes).Span.Line);
    }

    internal static string Source(string template, string identity, string declaration = "class Pair") =>
        "classDiagram\n" + declaration + "\n" + template.Replace("$id", identity);

    private static void AssertEffect(string template, MermaidClassDocument document, TopologyChart chart) {
        var node = Assert.Single(chart.Nodes);
        if (template.StartsWith("note", StringComparison.Ordinal)) {
            Assert.Equal(node.Id, Assert.Single(document.Notes).NodeId); Assert.Equal(node.Id, Assert.Single(chart.Annotations).TargetId);
            Assert.Equal("caption", chart.Annotations[0].Text);
        } else if (template.StartsWith("link", StringComparison.Ordinal) || template.StartsWith("click", StringComparison.Ordinal))
            Assert.Equal("https://example.invalid/", node.Href);
        else Assert.Equal("#fff", node.BackgroundColor);
    }
}
