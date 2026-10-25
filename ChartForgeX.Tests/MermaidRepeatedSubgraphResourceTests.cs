using System.Text;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidRepeatedSubgraphResourceTests {
    [Theory]
    [InlineData("flowchart")] [InlineData("swimlane-beta")]
    public void MergingDoesNotRefundOpeningOrActualNodeAllocationsAndUniqueInputKeepsItsExistingBoundary(string family) {
        var source = family + " LR\nsubgraph G[First]\nA\nend\nsubgraph G[Later]\nB\nend";
        var parser = new MermaidParser(); var accepted = parser.Parse(source, new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 4 } });
        Assert.Empty(accepted.Diagnostics); var acceptedDocument = Assert.IsAssignableFrom<MermaidFlowchartDocument>(accepted.Document); Assert.Single(acceptedDocument.Subgraphs); Assert.Equal(2, acceptedDocument.Nodes.Count);
        var rejected = parser.Parse(source, new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 3 } }); Assert.Null(rejected.Document);
        var diagnostic = Assert.Single(rejected.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        Assert.Contains(nameof(MermaidResourceLimits.MaximumElements), diagnostic.Message); Assert.Equal(6, diagnostic.Span.Line);
        var fence = new MermaidVisualMarkupParser(new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 3 } } }).Parse("```mermaid\n" + source + "\n```");
        Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts); Assert.Contains(fence.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Line == 7);
        // The unchanged maintained membership test also requires its historical exact8/over7 line11 result.
        var unique = MermaidFlowchartMembershipTests.Fixture(family);
        Assert.False(parser.Parse(unique, new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 8 } }).HasErrors);
        var previous = parser.Parse(unique, new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 7 } });
        Assert.Equal(11, Assert.Single(previous.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded).Span.Line);
    }

    [Theory]
    [InlineData("flowchart")] [InlineData("swimlane-beta")]
    public void MergedRequiredRawReferenceFactChecksExactAndOverPortableTextLimitsBeforeMaterializingNativeMetadata(string family) {
        static string Source(string header, int tailLength) {
            var source = new StringBuilder(header + " LR\n"); var repeated = "N" + new string('x', 511);
            for (var index = 0; index < 127; index++) source.Append("subgraph G[Scope]\n").Append(repeated).Append("\nend\n");
            return source.Append("subgraph G[Later]\nZ").Append(new string('y', tailLength - 1)).Append("\nend").ToString();
        }
        var parser = new MermaidParser(); var exact = parser.ParseFlowchart(Source(family, 385)); Assert.Empty(exact.Diagnostics);
        var document = exact.Document!; Assert.Single(document.Subgraphs); Assert.Equal(128, document.Subgraphs[0].NodeIds.Count);
        Assert.Equal(65536, document.ToTopologyChart().Groups[0].Metadata["mermaid.nodes"].Length);
        var over = parser.ParseFlowchart(Source(family, 386)); Assert.Null(over.Document);
        var diagnostic = Assert.Single(over.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        Assert.Contains("MaximumTextCharacters", diagnostic.Message); Assert.Equal(383, diagnostic.Span.Line);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + Source(family, 386) + "\n```"); Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
        Assert.Contains(fence.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Line == 384);
        document.Subgraphs[0].NodeIds.Add("extra"); var direct = Assert.Throws<MermaidResourceLimitException>(() => document.ToTopologyChart());
        Assert.Equal(2, direct.Span.Line); Assert.Contains("MaximumTextCharacters", direct.Message);
    }
}
