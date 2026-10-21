using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class TopologyDenseGridContainmentTests {
    [Theory]
    [InlineData(TopologyLayoutDirection.RightToLeft, 360, TopologyCanvasSurfaceStyle.Plain)]
    [InlineData(TopologyLayoutDirection.RightToLeft, 760, TopologyCanvasSurfaceStyle.Plain)]
    [InlineData(TopologyLayoutDirection.RightToLeft, 1100, TopologyCanvasSurfaceStyle.Plain)]
    [InlineData(TopologyLayoutDirection.BottomToTop, 360, TopologyCanvasSurfaceStyle.Plain)]
    [InlineData(TopologyLayoutDirection.BottomToTop, 760, TopologyCanvasSurfaceStyle.Plain)]
    [InlineData(TopologyLayoutDirection.BottomToTop, 1100, TopologyCanvasSurfaceStyle.Plain)]
    [InlineData(TopologyLayoutDirection.RightToLeft, 760, TopologyCanvasSurfaceStyle.Panel)]
    [InlineData(TopologyLayoutDirection.RightToLeft, 760, TopologyCanvasSurfaceStyle.PanelGrid)]
    [InlineData(TopologyLayoutDirection.BottomToTop, 760, TopologyCanvasSurfaceStyle.Panel)]
    [InlineData(TopologyLayoutDirection.BottomToTop, 760, TopologyCanvasSurfaceStyle.PanelGrid)]
    public void ExpandedReverseGridsKeepAuthoredAnchorsAndPrepareDeterministically(TopologyLayoutDirection direction, int width, TopologyCanvasSurfaceStyle surface) {
        var chart = ReverseGrid(direction, width);
        var options = new TopologyRenderOptions { IncludeLegend = false, CanvasSurfaceStyle = surface };
        var first = chart.Prepare(options); var second = chart.Prepare(options);
        var facts = first.ToInterchangeEnvelope();
        CaptureReverse("reverse-grid-" + direction + "-" + width + (surface == TopologyCanvasSurfaceStyle.Plain ? "" : "-" + surface), first);
        AssertAnchor(facts, direction);
        Assert.All(facts.Groups.Where(group => group.Id != "g0"), group => {
            Assert.True(group.X >= 28); Assert.True(group.Y >= 28);
        });
        Assert.Equal(facts.ToJson(), second.ToInterchangeEnvelope().ToJson());
        Assert.Equal(first.ToSvg(), second.ToSvg());
        Assert.Equal(first.ToPng(), second.ToPng());
        Assert.Equal(direction == TopologyLayoutDirection.RightToLeft ? 530 : 80, chart.Groups[0].X);
        Assert.Equal(90, chart.Groups[0].Y);
    }

    [Fact]
    public void FittingFractionalReverseFramesKeepTheirNativePositions() {
        foreach (var direction in new[] { TopologyLayoutDirection.RightToLeft, TopologyLayoutDirection.BottomToTop }) {
            var chart = TopologyChart.Create().WithViewport(760.5, 570.5, 28).WithLayout(TopologyLayoutMode.DenseGrouped, direction);
            foreach (var id in new[] { "first", "second" })
                chart.AddAutoGroup(id, id).WithGroupLayout(id, TopologyGroupLayoutPolicy.Grid)
                    .AddAutoNode(id + "-node", id, groupId: id, width: 132, height: 58);
            var groups = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false }).ToInterchangeEnvelope().Groups;
            var first = groups.Single(group => group.Id == "first");
            var second = groups.Single(group => group.Id == "second");
            if (direction == TopologyLayoutDirection.RightToLeft) {
                Assert.Equal(28, second.X); Assert.Equal(392.25, first.X);
            } else {
                Assert.Equal(28, second.Y); Assert.Equal(297.25, first.Y);
            }
        }
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.RightToLeft, TopologyCanvasSurfaceStyle.Plain)]
    [InlineData(TopologyLayoutDirection.BottomToTop, TopologyCanvasSurfaceStyle.Plain)]
    [InlineData(TopologyLayoutDirection.RightToLeft, TopologyCanvasSurfaceStyle.Panel)]
    [InlineData(TopologyLayoutDirection.BottomToTop, TopologyCanvasSurfaceStyle.PanelGrid)]
    public void ExpandedReverseGridsRestoreManualRoutesLabelsAndPortInference(TopologyLayoutDirection direction, TopologyCanvasSurfaceStyle surface) {
        var chart = ReverseGrid(direction, 760);
        var horizontal = direction == TopologyLayoutDirection.RightToLeft;
        var pinned = chart.Groups[0];
        var center = new ChartPoint(pinned.X + pinned.Width / 2, pinned.Y + pinned.Height / 2);
        chart.AddNode("fixed-node", "Fixed card", pinned.X + (horizontal ? 10 : 20), pinned.Y + (horizontal ? 100 : 30),
                groupId: "g0", width: 36, height: 24)
            .AddEdge("manual", "fixed-node", "g1-n0", "Manual", routing: TopologyEdgeRouting.Orthogonal)
            .WithEdgeWaypoints("manual", center).WithEdgeLabelAnchor("manual", center).WithEdgeRouteLane("manual", 18);
        var options = new TopologyRenderOptions { IncludeLegend = false, CanvasSurfaceStyle = surface };
        var prepared = chart.Prepare(options); var facts = prepared.ToInterchangeEnvelope();
        CaptureReverse("reverse-manual-" + direction + (surface == TopologyCanvasSurfaceStyle.Plain ? "" : "-" + surface), prepared);
        AssertAnchor(facts, direction);
        var edge = Assert.IsType<VisualArtifactInterchangeTopologyEdge>(Assert.Single(facts.Edges).Topology);
        var waypoint = Assert.Single(edge.Waypoints);
        Assert.Equal((center.X, center.Y), (waypoint.X, waypoint.Y));
        Assert.NotNull(edge.LabelAnchor);
        Assert.Equal((center.X, center.Y), (edge.LabelAnchor.X, edge.LabelAnchor.Y));
        Assert.Equal(-18, edge.RouteLane);
        Assert.Equal(horizontal ? TopologyEdgePort.Left : TopologyEdgePort.Right, edge.SourcePort);
        Assert.Equal(horizontal ? TopologyEdgePort.Right : TopologyEdgePort.Left, edge.TargetPort);
        Assert.Equal(TopologyEdgeLayoutInference.SourcePort | TopologyEdgeLayoutInference.TargetPort, edge.LayoutInference);
        var route = Assert.Single(prepared.Analyze().Edges);
        Assert.True(route.SourceAttached); Assert.True(route.TargetAttached);
        Assert.Equal(facts.ToJson(), chart.Prepare(options).ToInterchangeEnvelope().ToJson());
        Assert.Equal(center, Assert.Single(chart.Edges[0].Waypoints));
        Assert.Equal(18, chart.Edges[0].RouteLane);
    }

    private static TopologyChart ReverseGrid(TopologyLayoutDirection direction, int width) {
        var horizontal = direction == TopologyLayoutDirection.RightToLeft;
        var chart = TopologyChart.Create().WithViewport(width, 570, 28).WithLayout(TopologyLayoutMode.DenseGrouped, direction)
            .AddGroup("g0", "Fixed", horizontal ? 530 : 80, 90, horizontal ? 190 : 500, horizontal ? 150 : 170)
            .WithGroupLayout("g0", TopologyGroupLayoutPolicy.Grid);
        for (var groupIndex = 1; groupIndex < (horizontal ? 3 : 5); groupIndex++) {
            var id = "g" + groupIndex;
            chart.AddAutoGroup(id, "Auto " + groupIndex).WithGroupLayout(id, TopologyGroupLayoutPolicy.Grid);
            if (!horizontal) { chart.Groups[groupIndex].Width = 500; chart.Groups[groupIndex].Height = 170; }
            for (var nodeIndex = 0; nodeIndex < (groupIndex == 1 ? horizontal ? 2 : 5 : 1); nodeIndex++)
                chart.AddAutoNode(id + "-n" + nodeIndex, "Card " + nodeIndex, groupId: id, width: 132, height: 58);
        }
        return chart;
    }

    private static void AssertAnchor(VisualArtifactInterchangeEnvelope facts, TopologyLayoutDirection direction) {
        var pinned = facts.Groups.Single(group => group.Id == "g0");
        Assert.Equal(direction == TopologyLayoutDirection.RightToLeft ? 530 : 80, pinned.X);
        Assert.Equal(90, pinned.Y);
    }

    private static void CaptureReverse(string name, PreparedTopology prepared) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var path = Path.Combine(output, name);
        File.WriteAllText(path + ".svg", prepared.ToSvg());
        File.WriteAllBytes(path + ".native.png", prepared.ToPng());
        File.WriteAllText(path + ".facts.json", prepared.ToInterchangeEnvelope().ToJson());
    }
}
