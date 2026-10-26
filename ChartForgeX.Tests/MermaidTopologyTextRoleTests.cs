using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidTopologyTextRoleTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void DiagramNodeClusterAndEdgeTextKeepTheirDistinctLiteralRoleOwners(bool explicitHost) {
        var source = Source(); var options = MermaidTopologyPaletteRoleTests.Options(); if (explicitHost) options.Presentation!.Theme = VisualTheme.Graphite();
        var result = MermaidRenderer.Render(source, options); var artifact = Assert.IsType<VisualArtifact>(result.Artifact); Capture(explicitHost + "-direct", artifact);
        var chart = Assert.IsType<TopologyChart>(artifact.Model); var prepared = chart.Prepare(); var restored = artifact.ToInterchangeEnvelope().ToTopologyChart().Prepare(); Capture(explicitHost + "-restored", restored.Visual.ToArtifact());
        var expectedForeground = options.Presentation!.Theme?.Resolve(chart.ThemeMode).Foreground ?? ChartColor.FromHex("#123456");
        Assert.Empty(result.Diagnostics); Assert.Equal(expectedForeground, chart.Theme.Resolve(chart.ThemeMode).Foreground);
        Assert.Equal(ChartColor.FromHex("#654321"), ChartColor.Parse(Assert.Single(chart.Nodes, node => node.Id == "work").LabelColor!));
        Assert.Equal(ChartColor.FromHex("#654321"), ChartColor.Parse(Assert.Single(chart.Edges).LabelColor!)); Assert.Equal(ChartColor.FromHex("#2468ac"), ChartColor.Parse(Assert.Single(chart.Groups).LabelColor!));
        var heading = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneText>(), mark => mark.Text.Lines.Any(line => line.Text == "Source heading")); Assert.Equal(expectedForeground, heading.Color);
        Assert.Equal(artifact.ToPng(), restored.ToPng());
    }
    [Fact]
    public void DirectNodeAndEdgeTextStylesKeepPrecedenceOverLiteralRoles() {
        var result = MermaidRenderer.Render(Source() + "\nstyle work color:#abcdef\nlinkStyle 0 color:#fedcba", MermaidTopologyPaletteRoleTests.Options());
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact); Capture("direct-style", artifact); Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(artifact.Model); Assert.Equal("#abcdef", Assert.Single(chart.Nodes, node => node.Id == "work").LabelColor); Assert.Equal("#fedcba", Assert.Single(chart.Edges).LabelColor); Assert.Equal(ChartColor.FromHex("#123456"), chart.Theme.Resolve(chart.ThemeMode).Foreground);
        var restored = artifact.ToInterchangeEnvelope().ToTopologyChart().Prepare(); Capture("direct-style-restored", restored.Visual.ToArtifact()); Assert.Equal(artifact.ToPng(), restored.ToPng());
    }
    private static string Source() => "---\ntitle: Source heading\n---\n%%{init: {\"theme\":\"default\",\"themeVariables\":{\"textColor\":\"#123456\",\"primaryTextColor\":\"#abcdef\",\"nodeTextColor\":\"#654321\",\"titleColor\":\"#2468ac\"}}}%%\nflowchart LR\nsubgraph scope[Scope]\nwork[Work] -->|Route| done[Done]\nend";
    private static void Capture(string name, VisualArtifact artifact) {
        if (Environment.GetEnvironmentVariable("CFX_SOURCE_CONTOUR_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, "source-palette-text-" + name); File.WriteAllText(stem + ".svg", artifact.ToSvg()); File.WriteAllBytes(stem + ".png", artifact.ToPng()); File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
    }
}
