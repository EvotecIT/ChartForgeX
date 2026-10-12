using System.Globalization;
using System.Xml.Linq;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSankeyConfigurationTests {
    [Theory]
    [InlineData("0x320", "0x12", "0x0")]
    [InlineData("0o1440", "0o22", "0o0")]
    [InlineData("0b1100100000", "0b10010", "0b0")]
    public void TypedYamlRadixNumbersReachTheSameNativeSettings(string width, string nodeWidth, string padding) {
        var parsed = new MermaidParser().ParseSankey("---\nconfig:\n  sankey:\n    width: " + width
            + "\n    nodeWidth: " + nodeWidth + "\n    nodePadding: " + padding + "\n---\n" + Body);
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToChart();
        Assert.Equal(800, chart.Options.Size.Width);
        Assert.Equal(18, chart.Options.Sankey.NodeWidth);
        Assert.Equal(0, chart.Options.Sankey.NodeGap);
        Assert.Equal(width, parsed.Document!.Configuration.Settings.Single(setting => setting.Path == "sankey.width").Value);
    }

    private const string Body = "sankey-beta\nA,B,6\nB,C,6\nC,D,6\nLate,D,4\nA,Early,2";
    private const string Configured = "---\nconfig:\n  sankey:\n    width: 800\n    height: 480\n    nodeWidth: 18\n    nodePadding: 24\n    nodeAlignment: center\n    showValues: false\n    prefix: '$'\n    suffix: ' units'\n---\n" + Body;

    [Fact]
    public void VersionedFixturePreservesTheSameNodeAndFlowFacts() {
        var fixture = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "sankey-source-options.mmd");
        var document = new MermaidParser().ParseSankey(File.ReadAllText(fixture)).Document!;
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(fixture, ".expected.json")));
        var facts = expected.RootElement.GetProperty("sankey");
        var chart = document.ToChart();
        Assert.Equal(facts.GetProperty("nodes").EnumerateArray().Select(node => node.GetString()), chart.Series[0].Nodes.Select(node => node.Id));
        Assert.Equal(facts.GetProperty("links").EnumerateArray().Select(link => (link[0].GetString(), link[1].GetString(), link[2].GetDouble())),
            chart.Series[0].FlowLinks.Select(link => ((string?)link.SourceId, (string?)link.TargetId, link.Value)));
    }

    [Fact]
    public void SourceSettingsReachNativeGeometryWithoutChangingAuthoredFacts() {
        var parsed = new MermaidParser().ParseSankey(Configured);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        var chart = document.ToChart();
        Assert.Equal(800, chart.Options.Size.Width); Assert.Equal(480, chart.Options.Size.Height);
        Assert.Equal(18, chart.Options.Sankey.NodeWidth); Assert.Equal(24, chart.Options.Sankey.NodeGap);
        Assert.Equal(ChartSankeyAlignment.CenterSources, chart.Options.Sankey.Alignment);
        Assert.False(chart.Options.Sankey.ShowValues);
        Assert.Equal("$6 units", chart.Options.ValueFormat.Format(6));
        Assert.Equal(document.Links.Select(link => link.Value), chart.Series[0].FlowLinks.Select(link => link.Value));
        var svg = XDocument.Parse(chart.ToSvg());
        var nodes = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "sankey-node").ToArray();
        Assert.Equal(new[] { 0, 1, 2, 3, 2, 1 }, nodes.Select(node => int.Parse(node.Attribute("data-cfx-layer")!.Value, CultureInfo.InvariantCulture)));
        Assert.Contains(nodes, node => (string?)node.Attribute("data-cfx-target-id") == "A" && (string?)node.Attribute("data-cfx-full-label") == "A $8 units");
        Assert.DoesNotContain(svg.Descendants().Where(node => node.Name.LocalName == "text"), node => node.Value.Contains("units", StringComparison.Ordinal));
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void HostOverridesEachDimensionAndMarkupPreservesUnspecifiedSourceDimensions() {
        var document = new MermaidParser().ParseSankey(Configured).Document!;
        var direct = document.ToChart(new MermaidSankeyRenderOptions { Width = 720 });
        Assert.Equal(720, direct.Options.Size.Width); Assert.Equal(480, direct.Options.Size.Height);
        var markup = new MermaidVisualMarkupParser().Parse("```mermaid {width=\"720\"}\n" + Configured + "\n```");
        Assert.Empty(markup.Diagnostics);
        var artifact = Assert.Single(markup.Artifacts);
        Assert.Equal(720, artifact.NaturalSize!.Value.Width); Assert.Equal(480, artifact.NaturalSize.Value.Height);
        var defaultMarkup = new MermaidVisualMarkupParser().Parse("```mermaid\n" + Configured + "\n```");
        Assert.Equal(800, Assert.Single(defaultMarkup.Artifacts).NaturalSize!.Value.Width);
        var fallback = new MermaidParser().ParseSankey(Body).Document!.ToChart();
        Assert.Equal(960, fallback.Options.Size.Width); Assert.Equal(560, fallback.Options.Size.Height);
    }

    [Fact]
    public void LastDirectiveWinsAndAffixesRemainLiteralText() {
        var source = "---\nconfig:\n  sankey:\n    nodeWidth: 12\n---\n%%{init: {'sankey': {'nodeWidth': 20, 'prefix': '<b>', 'suffix': '& units'}}}%%\n" + Body;
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var chart = Assert.IsType<Chart>(rendered.Artifact!.Model);
        Assert.Equal(20, chart.Options.Sankey.NodeWidth);
        var svg = XDocument.Parse(rendered.Artifact.ToSvg());
        Assert.Contains(svg.Descendants().Where(node => node.Name.LocalName == "text"), node => node.Value.Contains("<b>8& units", StringComparison.Ordinal));
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName == "b");
    }

    [Theory]
    [InlineData("nodeWidth", "0")]
    [InlineData("nodePadding", "-1")]
    [InlineData("width", "640.5")]
    [InlineData("showValues", "'false'")]
    [InlineData("prefix", "false")]
    [InlineData("nodeAlignment", "'diagonal'")]
    [InlineData("nodeWidth", "{'nested': 18}")]
    public void InvalidAppliedSettingsReportOneConfigurationError(string key, string value) {
        var source = "%%{init: {'sankey': {'" + key + "': " + value + "}}}%%\n" + Body;
        var parsed = new MermaidParser().ParseSankey(source);
        var diagnostic = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, diagnostic.Code);
        Assert.Equal(1, diagnostic.Span.Line);
    }

    [Fact]
    public void UnmappedPaintAndHostPolicySettingsRemainExplicitWarnings() {
        var parsed = new MermaidParser().ParseSankey("%%{init: {'sankey': {'linkColor': 'gradient', 'useMaxWidth': true}}}%%\n" + Body);
        Assert.False(parsed.HasErrors);
        Assert.Equal(2, parsed.Diagnostics.Count);
        Assert.All(parsed.Diagnostics, diagnostic => Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, diagnostic.Code));
    }
}
