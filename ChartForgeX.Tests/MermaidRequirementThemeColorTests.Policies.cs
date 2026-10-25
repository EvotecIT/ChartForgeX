using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidRequirementThemeColorTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ExplicitHostThemesKeepAuthoredRequirementRolePaints(bool dark) {
        var options = Options(800, dark); options.Presentation!.Theme = VisualTheme.Report().WithFontFamily(BrowserProofFonts.Family);
        var plain = Assert.IsType<TopologyChart>(MermaidRenderer.Render(Body, options).Artifact!.Model);
        var result = MermaidRenderer.Render(ConfiguredSource, options); Assert.Empty(result.Diagnostics);
        var mapped = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Capture("requirement-explicit-role-host-" + dark, result);
        Assert.Equal(plain.Theme.ToJson(), mapped.Theme.ToJson());
        Assert.All(mapped.Nodes, node => Assert.Equal(("#F5DBE4", "#B71D62", "#28132B"), (node.BackgroundColor, node.Color, node.LabelColor)));
        var edge = Assert.Single(mapped.Edges);
        Assert.Equal(("#2F4B7C", "#0D5325"), (edge.Color, edge.LabelColor));
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AuthoredObjectStylesRemainMoreSpecificThanSourceOrHostTheme(bool hostTheme) {
        var options = Options(800, false); if (hostTheme) options.Presentation!.Theme = VisualTheme.Report();
        var result = MermaidRenderer.Render(ConfiguredSource + "\nstyle R fill:#3A8D6B,stroke:#224466,color:#112233", options);
        Assert.Empty(result.Diagnostics); var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var node = chart.Nodes.Single(item => item.Id == "R");
        Assert.Equal(("#3A8D6B", "#224466", "#112233"), (node.BackgroundColor, node.Color, node.LabelColor));
    }
    [Theory]
    [InlineData("{\"requirementBackground\":17}")]
    [InlineData("{\"requirementBorderColor\":null}")]
    [InlineData("{\"relationColor\":true}")]
    [InlineData("{\"requirementTextColor\":{\"nested\":\"#112233\"}}")]
    [InlineData("{\"relationLabelColor\":\"not-a-color\"}")]
    public void MalformedLiteralRolesFailAtTheLocatedConfigurationBoundary(string settings) {
        var result = MermaidRenderer.Render("%%{init: {\"themeVariables\":" + settings + "}}%%\n" + Body);
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error && item.Span.Line == 1);
    }
    [Fact]
    public void CapturedPaintAndPortableRoleValuesDetachFromTheMutableProducer() {
        var result = MermaidRenderer.Render(ConfiguredSource, Options(800, false)); Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); var prepared = chart.Prepare();
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var json = prepared.ToInterchangeEnvelope().ToJson();
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(json); var imported = envelope.ToTopologyChart();
        Assert.Equal(chart.Nodes.Select(node => (node.BackgroundColor, node.Color, node.LabelColor)), imported.Nodes.Select(node => (node.BackgroundColor, node.Color, node.LabelColor)));
        Assert.Equal(chart.Edges.Select(edge => (edge.Color, edge.LabelColor)), imported.Edges.Select(edge => (edge.Color, edge.LabelColor)));
        chart.Nodes[0].BackgroundColor = "#010203"; chart.Nodes[0].LabelColor = "#040506"; chart.Edges[0].Color = "#070809";
        envelope.Nodes.Clear(); envelope.Edges.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Equal(json, prepared.ToInterchangeEnvelope().ToJson());
        Assert.NotEqual(chart.Nodes[0].BackgroundColor, imported.Nodes[0].BackgroundColor);
    }
    private const string ConfiguredSource = "%%{init: {\"themeVariables\":{\"requirementBackground\":\"#F5DBE4\",\"requirementBorderColor\":\"#B71D62\",\"requirementTextColor\":\"#28132B\",\"relationColor\":\"#2F4B7C\",\"relationLabelColor\":\"#0D5325\"}}}%%\n" + Body;
}
