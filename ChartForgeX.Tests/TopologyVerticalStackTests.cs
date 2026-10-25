using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyVerticalStackTests {
    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight, false)]
    [InlineData(TopologyLayoutDirection.LeftToRight, true)]
    [InlineData(TopologyLayoutDirection.TopToBottom, false)]
    [InlineData(TopologyLayoutDirection.BottomToTop, true)]
    public void GenericStackKeepsFortyCardsAndEmptyPanelsWithoutAutomaticCollapseOrRowOverlap(TopologyLayoutDirection direction, bool dark) {
        var chart = TopologyChart.Create().WithViewport(700, 800, 16).WithLegend(null)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.LayoutMode = TopologyLayoutMode.DenseGrouped; chart.LayoutDirection = direction;
        for (var column = 0; column < 5; column++) {
            var id = "column-" + column;
            chart.AddAutoGroup(id, column == 0 ? "A very long explicit\ncolumn caption" : "Column " + column, subtitle: "Task facts\nremain visible");
            chart.Groups[column].LayoutPolicy = TopologyGroupLayoutPolicy.VerticalStack;
            var count = column == 0 ? 40 : column == 1 ? 0 : 2;
            for (var row = 0; row < count; row++) {
                chart.AddAutoNode(id + "-" + row, "Card " + row, TopologyNodeKind.Generic, groupId: id, width: 180, height: 64);
                chart.Nodes[chart.Nodes.Count - 1].DisplayMode = TopologyNodeDisplayMode.Card;
            }
        }
        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeGroupStatusDots = false, IncludeStatusBadges = false,
            AllowMultilineNodeLabels = true, WrapNodeLabels = true };
        var prepared = chart.Prepare(options); var facts = prepared.ToInterchangeEnvelope();
        Assert.Equal(46, facts.Nodes.Count); Assert.Equal(5, facts.Groups.Count);
        Assert.All(facts.Groups, group => {
            Assert.Equal(TopologyGroupLayoutPolicy.VerticalStack, group.Topology!.AppliedLayoutPolicy);
            var tasks = facts.Nodes.Where(node => node.GroupId == group.Id).ToArray();
            for (var row = 0; row < tasks.Length; row++) {
                Assert.Equal(180, tasks[row].Width); Assert.Equal(64, tasks[row].Height);
                Assert.True(tasks[row].Y >= group.Y && tasks[row].Y + tasks[row].Height <= group.Y + group.Height);
                if (row > 0) Assert.True(tasks[row].Y >= tasks[row - 1].Y + tasks[row - 1].Height + 15.9);
            }
        });
        var panels = facts.Groups.ToArray();
        for (var a = 0; a < panels.Length; a++) for (var b = a + 1; b < panels.Length; b++)
            Assert.True(panels[a].X + panels[a].Width <= panels[b].X || panels[b].X + panels[b].Width <= panels[a].X
                || panels[a].Y + panels[a].Height <= panels[b].Y || panels[b].Y + panels[b].Height <= panels[a].Y);
        var roundtrip = VisualArtifactInterchangeEnvelope.FromJson(facts.ToJson());
        Assert.All(roundtrip.Groups, group => Assert.Equal(TopologyGroupLayoutPolicy.VerticalStack, group.Topology!.LayoutPolicy));
        Assert.Equal(facts.ToJson(), roundtrip.ToJson());
        Assert.Contains("column caption", prepared.ToSvg()); Assert.True(prepared.ToPng().Length > 100);
    }
}
