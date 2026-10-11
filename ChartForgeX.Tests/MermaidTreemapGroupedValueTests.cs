using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidTreemapGroupedValueTests {
    [Theory]
    [InlineData("1,234.5", 1234.5)]
    [InlineData("1,23.5", 123.5)]
    [InlineData("1,,234", 1234)]
    [InlineData("0,000", 0)]
    public void CommaSeparatedValuesRetainReferenceMagnitudeAndHierarchyInDirectAndFencedOutput(string spelling, double value) {
        var source = "treemap-beta\n\"Root\"\n  \"Measured\": " + spelling + "\n  \"Measured\": 1\n";
        var culture = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var parsed = new MermaidParser().ParseTreemap(source);
            Assert.Empty(parsed.Diagnostics);
            Assert.Equal(value, parsed.Document!.Nodes[1].Value);
            var artifact = parsed.Document!.ToVisualArtifact();
            var chart = Assert.IsType<Chart>(artifact.Model);
            Assert.Equal(value, chart.Series[0].HierarchyItems[1].Value);
            Assert.Equal("node-0", chart.Series[0].HierarchyItems[1].ParentId);
            var svg = XDocument.Parse(artifact.ToSvg());
            var mark = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-target-id") == "node-1");
            Assert.Equal(value.ToString(CultureInfo.InvariantCulture), (string?)mark.Attribute("data-cfx-value"));
            var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
            Assert.Empty(fenced.Diagnostics);
            Assert.Equal(artifact.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
            Assert.Equal(artifact.ToPng(), Assert.Single(fenced.Artifacts).ToPng());
        } finally { CultureInfo.CurrentCulture = culture; }
    }
}
