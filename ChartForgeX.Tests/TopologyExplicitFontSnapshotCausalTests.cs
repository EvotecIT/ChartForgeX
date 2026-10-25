using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyExplicitFontSnapshotCausalTests {
    [Fact]
    public void FittedSnapshotScalesExplicitCaptionRolesWhileRawHierarchySourceRetainsAuthoredSizes() {
        var chart = TopologyChart.Create().WithId("font-snapshot").WithViewport(1200, 800).WithLegend(null).AddHierarchy(new[] {
            new TopologyHierarchyItem("lead", "Avery Quinn"), new TopologyHierarchyItem("team", "Engineering", "lead") });
        foreach (var node in chart.Nodes) {
            node.Width = 420; node.Height = 200; node.PreserveDisplayModeSize = true; node.ShowStatusBadge = false;
            node.TextStyle = new TextStyleOverride { FontFamily = BrowserProofFonts.Family, FontSize = 24 };
            node.Subtitle = "Platform"; node.SubtitleTextStyle = new TextStyleOverride { FontSize = 18 };
            node.Details.Add(new TopologyNodeDetail { Text = "Healthy", TextStyle = new TextStyleOverride { FontSize = 16 } });
        }
        var ordinary = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, HierarchyView = new TopologyHierarchyView() });
        var fitted = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(420, 300), 16)),
            new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true, HierarchyView = new TopologyHierarchyView() });
        var saved = fitted.ToArtifact().ToInterchangeEnvelope(); var scale = saved.Nodes[0].Width!.Value / ordinary.ToInterchangeEnvelope().Nodes[0].Width!.Value;
        Assert.InRange(scale, .001, .999);
        foreach (var node in saved.Nodes) {
            Assert.Equal(24 * scale, node.Topology!.TextStyle!.FontSize!.Value, 8);
            Assert.Equal(18 * scale, node.Topology!.SubtitleTextStyle!.FontSize!.Value, 8);
            Assert.Equal(16 * scale, node.Details[0].TextStyle!.FontSize!.Value, 8);
        }
        var loaded = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson());
        var raw = loaded.ToTopologyHierarchySource();
        Assert.All(raw.Nodes, node => { Assert.Equal(24, node.TextStyle!.FontSize); Assert.Equal(18, node.SubtitleTextStyle!.FontSize); Assert.Equal(16, node.Details[0].TextStyle!.FontSize); });
        var restored = loaded.ToTopologyChart().Prepare(new TopologyRenderOptions { IncludeLegend = false });
        Assert.Equal(TextRows(fitted), TextRows(restored.Visual));
        Assert.All(chart.Nodes, node => { Assert.Equal(24, node.TextStyle!.FontSize); Assert.Equal(18, node.SubtitleTextStyle!.FontSize); });
    }
    private static (string Role, string Text, double Size)[] TextRows(PreparedVisual prepared) =>
        prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role is "topology-node-label" or "topology-node-subtitle" or "topology-node-detail")
            .Select(text => (text.Role!, string.Join("\n", text.Text.Lines.Select(line => line.Text)), Math.Round(text.Text.Size, 7))).ToArray();
}
