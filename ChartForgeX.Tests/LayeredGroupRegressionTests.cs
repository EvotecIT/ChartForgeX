using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class LayeredGroupRegressionTests {
    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight)]
    [InlineData(TopologyLayoutDirection.RightToLeft)]
    [InlineData(TopologyLayoutDirection.TopToBottom)]
    [InlineData(TopologyLayoutDirection.BottomToTop)]
    public void GroupDeclarationOrderDoesNotReverseDependencyFlow(TopologyLayoutDirection direction) {
        var chart = TopologyChart.Create().WithViewport(300, 330).WithLayout(TopologyLayoutMode.Layered).WithLayoutDirection(direction)
            .AddAutoGroup("b", "B").AddAutoGroup("a", "A")
            .AddAutoNode("source", "Source", groupId: "a").AddAutoNode("target", "Target", groupId: "b")
            .AddEdge("link", "source", "target");
        var prepared = TopologyLayoutEngine.Prepare(chart);
        var source = prepared.Nodes[0]; var target = prepared.Nodes[1];
        Assert.True(direction switch {
            TopologyLayoutDirection.LeftToRight => source.X + source.Width < target.X,
            TopologyLayoutDirection.RightToLeft => target.X + target.Width < source.X,
            TopologyLayoutDirection.TopToBottom => source.Y + source.Height < target.Y,
            _ => target.Y + target.Height < source.Y
        });
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight)]
    [InlineData(TopologyLayoutDirection.TopToBottom)]
    public void GroupCompactionPreservesAuthoredRankAndNodeSpacing(TopologyLayoutDirection direction) {
        var chart = TopologyChart.Create().WithViewport(500, 500).WithLayout(TopologyLayoutMode.Layered).WithLayoutDirection(direction)
            .AddAutoGroup("a", "A").AddAutoGroup("b", "B")
            .AddAutoNode("one", "One", groupId: "a").AddAutoNode("peer", "Peer", groupId: "a")
            .AddAutoNode("far", "Far", groupId: "a").AddAutoNode("other", "Other", groupId: "b")
            .AddEdge("span", "one", "far");
        chart.Edges[0].MinimumRankSpan = 20;
        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { LayeredNodeSpacing = 80, LayeredRankSpacing = 50 });
        var one = prepared.Nodes[0]; var peer = prepared.Nodes[1]; var far = prepared.Nodes[2];
        if (direction == TopologyLayoutDirection.LeftToRight) {
            Assert.True(far.X - one.X - one.Width >= 1000);
            Assert.True(Math.Abs(peer.Y - one.Y) - one.Height >= 80);
        } else {
            Assert.True(far.Y - one.Y - one.Height >= 1000);
            Assert.True(Math.Abs(peer.X - one.X) - one.Width >= 80);
        }
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight)]
    [InlineData(TopologyLayoutDirection.TopToBottom)]
    public void PackedGroupLongEdgeAvoidsItsIntermediateNode(TopologyLayoutDirection direction) {
        var chart = TopologyChart.Create().WithViewport(1200, 800).WithLayout(TopologyLayoutMode.Layered).WithLayoutDirection(direction)
            .AddAutoGroup("a", "A").AddAutoGroup("b", "B")
            .AddAutoNode("one", "One", groupId: "a").AddAutoNode("middle", "Middle", groupId: "a")
            .AddAutoNode("last", "Last", groupId: "a").AddAutoNode("other", "Other", groupId: "b")
            .AddEdge("first", "one", "middle").AddEdge("second", "middle", "last").AddEdge("skip", "one", "last");
        var prepared = TopologyLayoutEngine.Prepare(chart);
        var edge = prepared.Edges.Single(e => e.Id == "skip");
        var middle = prepared.Nodes.Single(n => n.Id == "middle");
        var route = TopologyEdgeRouter.Route(prepared, edge, prepared.Nodes[0], prepared.Nodes[2]).Points;
        Assert.True(route.Count > 2);
        for (var i = 1; i < route.Count; i++) {
            var a = route[i - 1]; var b = route[i];
            var crosses = Math.Abs(a.X - b.X) < .001
                ? a.X > middle.X && a.X < middle.X + middle.Width && Math.Max(a.Y, b.Y) > middle.Y && Math.Min(a.Y, b.Y) < middle.Y + middle.Height
                : a.Y > middle.Y && a.Y < middle.Y + middle.Height && Math.Max(a.X, b.X) > middle.X && Math.Min(a.X, b.X) < middle.X + middle.Width;
            Assert.False(crosses, "The rank-skipping route must avoid the middle card.");
        }
        Assert.NotEmpty(chart.ToPng());
    }
}
