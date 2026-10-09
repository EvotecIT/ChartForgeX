using ChartForgeX.Mermaid;
using Xunit;
using static ChartForgeX.Tests.TreemapHierarchyTests;

namespace ChartForgeX.Tests;

public sealed class TreemapMermaidTests {
    [Fact]
    public void ParsedSectionsKeepContainedRepeatedLabelsRatherThanPathFlattening() {
        var result = new MermaidParser().ParseTreemap("""
            treemap-beta
            "North"
                "Services"
                    "Support": 5
            "South"
                "Services"
                    "Support": 8
            "Independent": 3
            """
        );
        Assert.False(result.HasErrors);
        var chart = result.Document!.ToChart(); var nodes = Targets(Prepare(chart)).ToDictionary(node => (string)node.Attribute("data-cfx-target-id")!);
        Assert.Equal(7, chart.Series[0].TreemapItems.Count); Assert.Empty(chart.Series[0].Points);
        Assert.Equal("Support", (string?)nodes["node-2"].Attribute("data-cfx-label")); Assert.Equal("Support", (string?)nodes["node-5"].Attribute("data-cfx-label"));
        Assert.Equal("node-1", (string?)nodes["node-2"].Attribute("data-cfx-parent")); Assert.Equal("node-4", (string?)nodes["node-5"].Attribute("data-cfx-parent"));
        Assert.Equal(5, Number(nodes["node-0"], "data-cfx-value")); Assert.Equal(8, Number(nodes["node-3"], "data-cfx-value"));
        Assert.Contains(nodes["node-2"].Ancestors(), node => (string?)node.Attribute("data-cfx-target-id") == "node-0");
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void EmptyUnvaluedSectionIsDiagnosedBeforeConversionWithItsSourceSpan() {
        var result = new MermaidParser().ParseTreemap("treemap-beta\n\"Empty\"\n\"Measured leaf\": 3");
        Assert.True(result.HasErrors);
        var diagnostic = Assert.Single(result.Diagnostics, item => item.Message.Contains("sections require child nodes", StringComparison.Ordinal));
        Assert.Equal(2, diagnostic.Span.Line); Assert.Equal(1, diagnostic.Span.Column);
    }
}
