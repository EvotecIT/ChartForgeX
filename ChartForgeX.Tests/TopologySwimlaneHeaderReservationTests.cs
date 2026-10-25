using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologySwimlaneHeaderReservationTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongNamespaceGrowsTheSharedHorizontalLaneWhileReturningSourceCardsKeepTheirTimelineRanks(bool dark) {
        var name = string.Concat(Enumerable.Repeat("Operations", 20));
        var source = "eventmodeling\ntf 30 ui " + name + ".X\ntf 10 cmd Save\ntf 20 ui " + name + ".Y";
        var result = MermaidRenderer.Render(source, MermaidEventModelingDepthTests.Options(dark, 390)); Assert.False(result.HasErrors);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); var group = chart.Groups.Single(item => item.Label.Contains(name));
        var envelope = result.Artifact.ToInterchangeEnvelope(); var saved = envelope.Groups.Single(item => item.Id == group.Id);
        Assert.True(saved.Width!.Value > 800); var nodes = new[] { "30", "10", "20" }.Select(id => envelope.Nodes.Single(node => node.Id == id)).ToArray();
        Assert.True(nodes[0].X!.Value < nodes[1].X!.Value && nodes[1].X!.Value < nodes[2].X!.Value);
        var text = XDocument.Parse(result.Artifact.ToSvg()).Descendants().Where(node => node.Name.LocalName == "text").Select(node => node.Value);
        Assert.Contains(group.Label, text); Assert.NotEmpty(result.Artifact.ToPng());
        MermaidEventModelingDepthTests.Capture(result.Artifact, "event-long-lane-" + (dark ? "dark" : "light"));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AuthoredMultilineLaneHeadersReserveTheirCompleteHeightBeforeFactualCards(bool dark) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(600, 300, 24).WithLegend(null)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithLayout(TopologyLayoutMode.Swimlane, TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("lane", "Orders\nrecords", subtitle: "Observed\nfacts")
            .AddAutoNode("node", "Complete card", TopologyNodeKind.Generic, groupId: "lane", width: 144, height: 64);
        chart.Nodes[0].DisplayMode = TopologyNodeDisplayMode.Card; chart.Nodes[0].PreserveDisplayModeSize = true;
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, IncludeTitle = false, AllowMultilineNodeLabels = true });
        var header = Assert.Single(prepared.Analyze().Groups).HeaderBounds!.Value;
        var node = Assert.Single(prepared.ToInterchangeEnvelope().Nodes); Assert.True(node.Y!.Value >= header.Bottom + 12);
        var rows = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>()
            .Where(text => text.Role is "topology-group-label" or "topology-group-subtitle").SelectMany(text => text.Text.Lines.Select(line => line.Text));
        Assert.Equal(new[] { "Orders", "records", "Observed", "facts" }, rows);
        Assert.DoesNotContain(prepared.Analyze().RouteCrossings, crossing => crossing.Kind == TopologyLayoutRouteCrossingKind.GroupHeader);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight, 40, 72, 296, 160)]
    [InlineData(TopologyLayoutDirection.TopToBottom, 72, 40, 240, 216)]
    public void OrdinarySingleLineAndVerticalControlsRetainTheirExistingNativeArithmetic(TopologyLayoutDirection direction,
        double x, double y, double width, double height) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(800, 400, 24).WithLayout(TopologyLayoutMode.Swimlane, direction);
        chart.AddAutoGroup("lane", "Orders", TopologyHealthStatus.Unknown).AddAutoNode("node", "X", TopologyNodeKind.Generic, TopologyHealthStatus.Unknown, groupId: "lane", width: 144, height: 64);
        chart.Nodes[0].DisplayMode = TopologyNodeDisplayMode.Card; chart.Nodes[0].PreserveDisplayModeSize = true; chart.Nodes[0].Metadata["layer"] = "0";
        chart.DefaultRenderOptions = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false };
        var saved = chart.Prepare().ToInterchangeEnvelope(); var group = Assert.Single(saved.Groups); var node = Assert.Single(saved.Nodes);
        Assert.Equal(width, group.Width!.Value); Assert.Equal(height, group.Height!.Value); Assert.Equal(x, node.X!.Value - group.X!.Value); Assert.Equal(y, node.Y!.Value - group.Y!.Value);
    }
}
