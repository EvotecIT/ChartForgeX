using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyActorSubtitleRoleCompositionTests {
    [Fact]
    public void ActorSubtitleUsesItsExplicitSharedRoleWhileTheTitleKeepsItsBasePolicy() {
        var chart = TopologyChart.Create().WithViewport(700, 500, 24).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("actor", "Coordinator", 200, 50, width: 220, height: 120);
        var node = chart.Nodes[0]; node.Shape = TopologyNodeShape.Actor; node.Subtitle = "Primary reviewer";
        node.ShowStatusBadge = false; node.DisplayMode = TopologyNodeDisplayMode.Card; node.PreserveDisplayModeSize = true;
        node.TextStyle = new TextStyleOverride { FontSize = 12, FontFamily = BrowserProofFonts.Family, FontWeight = "600" };
        node.SubtitleTextStyle = new TextStyleOverride { FontSize = 18, FontFamily = BrowserProofFonts.Family, FontWeight = "500" };
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, WrapNodeLabels = true });
        var groups = new Stack<VisualSceneGroup>(); var title = new List<VisualSceneText>(); var subtitle = new List<VisualSceneText>();
        foreach (var mark in prepared.Visual.Scene.Nodes) {
            if (mark is VisualSceneGroup group) groups.Push(group);
            else if (mark is VisualSceneEndGroup) groups.Pop();
            else if (mark is VisualSceneText text) {
                if (groups.Any(group => group.Id == "actor-label")) title.Add(text);
                if (groups.Any(group => group.Id == "actor-subtitle")) subtitle.Add(text);
            }
        }
        Assert.NotEmpty(title); Assert.NotEmpty(subtitle);
        Assert.All(title, text => Assert.Equal(12, text.Text.Size));
        Assert.All(subtitle, text => { Assert.Equal(18, text.Text.Size); Assert.Equal(500, text.Text.Style.Font.Weight); Assert.Equal(BrowserProofFonts.Family, text.Text.Style.Font.Family); });
        var saved = VisualArtifactInterchangeEnvelope.FromJson(prepared.ToInterchangeEnvelope().ToJson());
        Assert.Equal(18, saved.Nodes[0].Topology!.SubtitleTextStyle!.FontSize); Assert.Equal("500", saved.Nodes[0].Topology!.SubtitleTextStyle!.FontWeight);
        var loaded = saved.ToTopologyChart(); Assert.NotSame(node.SubtitleTextStyle, loaded.Nodes[0].SubtitleTextStyle);
        Assert.NotEmpty(prepared.ToPng()); Assert.Contains("Primary reviewer", string.Join(" ", subtitle.SelectMany(text => text.Text.Lines.Select(line => line.Text))));
    }
}
