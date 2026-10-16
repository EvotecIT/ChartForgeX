using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class LayeredGroupRegressionTests {
    [Fact]
    public void AutomaticLeftToRightChainFollowsEdgesAcrossNodeKinds() {
        var chart = TopologyChart.Create().WithViewport(1100, 260).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddAutoNode("client", "Client", TopologyNodeKind.Endpoint)
            .AddAutoNode("api", "Application API", TopologyNodeKind.Service)
            .AddAutoNode("cluster", "Database cluster", TopologyNodeKind.Group)
            .AddEdge("call", "client", "api").AddEdge("store", "api", "cluster");

        var prepared = TopologyLayoutEngine.Prepare(chart);
        var nodes = prepared.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);

        Assert.True(nodes["client"].X < nodes["api"].X);
        Assert.True(nodes["api"].X < nodes["cluster"].X);
    }

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

    [Fact]
    public void InterleavedGroupPackingKeepsEveryDependencyInRankOrder() {
        var chart = TopologyChart.Create().WithViewport(1200, 500).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("a", "Group A").AddAutoGroup("b", "Group B")
            .AddAutoNode("a1", "A1", groupId: "a").AddAutoNode("b1", "B1", groupId: "b")
            .AddAutoNode("a2", "A2", groupId: "a")
            .AddEdge("first", "a1", "b1").AddEdge("second", "b1", "a2");

        var prepared = TopologyLayoutEngine.Prepare(chart);
        var nodes = prepared.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        Assert.True(nodes["a1"].X < nodes["b1"].X && nodes["b1"].X < nodes["a2"].X,
            $"Interleaved dependencies must keep rank order: A1={nodes["a1"].X}, B1={nodes["b1"].X}, A2={nodes["a2"].X}.");
        var groups = prepared.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        Assert.False(groups["a"].X < groups["b"].X + groups["b"].Width && groups["a"].X + groups["a"].Width > groups["b"].X
            && groups["a"].Y < groups["b"].Y + groups["b"].Height && groups["a"].Y + groups["a"].Height > groups["b"].Y);
    }

    [Fact]
    public void ThreeInterleavedGroupShellsAreRecheckedUntilAllAreSeparated() {
        var chart = TopologyChart.Create().WithViewport(1300, 700).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("a", "Group A").AddAutoGroup("b", "Group B").AddAutoGroup("c", "Group C")
            .AddAutoNode("a1", "A1", groupId: "a").AddAutoNode("b1", "B1", groupId: "b")
            .AddAutoNode("c1", "C1", groupId: "c").AddAutoNode("a2", "A2", groupId: "a")
            .AddAutoNode("c2", "C2", groupId: "c")
            .AddEdge("a-to-b", "a1", "b1").AddEdge("b-to-a", "b1", "a2")
            .AddEdge("c-to-b", "c1", "b1").AddEdge("b-to-c", "b1", "c2");

        var prepared = TopologyLayoutEngine.Prepare(chart);
        var groups = prepared.Groups.Where(group => group.Id is "a" or "b" or "c").ToArray();

        for (var first = 0; first < groups.Length; first++) {
            for (var second = first + 1; second < groups.Length; second++) {
                var overlaps = groups[first].X < groups[second].X + groups[second].Width
                    && groups[first].X + groups[first].Width > groups[second].X
                    && groups[first].Y < groups[second].Y + groups[second].Height
                    && groups[first].Y + groups[first].Height > groups[second].Y;
                Assert.False(overlaps, $"Automatic group shells {groups[first].Id} and {groups[second].Id} must not overlap.");
            }
        }

        var nodes = prepared.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        Assert.True(nodes["a1"].X < nodes["b1"].X && nodes["b1"].X < nodes["a2"].X);
        Assert.True(nodes["c1"].X < nodes["b1"].X && nodes["b1"].X < nodes["c2"].X);
    }

    [Fact]
    public void SameRankRelationshipLabelDoesNotWidenLayeredRankGaps() {
        static TopologyChart Create(bool longSameRankLabel) {
            var chart = TopologyChart.Create().WithViewport(900, 320).WithLayout(TopologyLayoutMode.Layered)
                .WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
                .AddAutoNode("a", "A").AddAutoNode("peer", "Peer").AddAutoNode("target", "Target")
                .AddEdge("forward", "a", "target", "short");
            if (longSameRankLabel) chart.AddEdge("peer-link", "peer", "peer", new string('L', 80));
            return chart;
        }

        var baseline = TopologyLayoutEngine.Prepare(Create(false));
        var labelled = TopologyLayoutEngine.Prepare(Create(true));
        Assert.Equal(baseline.Nodes.Single(node => node.Id == "target").X, labelled.Nodes.Single(node => node.Id == "target").X);
    }

    [Fact]
    public void PackedGroupsRebuildHierarchyBusRoutesAfterMovingTheirNodes() {
        var chart = TopologyChart.Create().WithViewport(1000, 700).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("parent-group", "Parent group").AddAutoGroup("child-group", "Child group")
            .AddAutoNode("peer", "Peer", groupId: "child-group")
            .AddHierarchy(new[] {
                new TopologyHierarchyItem("root", "Root") { GroupId = "parent-group" },
                new TopologyHierarchyItem("child-1", "Child 1", "root") { GroupId = "child-group" },
                new TopologyHierarchyItem("child-2", "Child 2", "root") { GroupId = "child-group" },
                new TopologyHierarchyItem("child-3", "Child 3", "root") { GroupId = "child-group" }
            });

        var prepared = TopologyLayoutEngine.Prepare(chart);
        var edges = prepared.Edges.Where(edge => edge.SourceNodeId == "root").ToArray();
        var nodes = prepared.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var groups = prepared.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);

        Assert.True(groups["parent-group"].Y + groups["parent-group"].Height <= groups["child-group"].Y
            || groups["child-group"].Y + groups["child-group"].Height <= groups["parent-group"].Y,
            $"Fixture must force a packing shift: parent={groups["parent-group"].X},{groups["parent-group"].Y},{groups["parent-group"].Width},{groups["parent-group"].Height}; child={groups["child-group"].X},{groups["child-group"].Y},{groups["child-group"].Width},{groups["child-group"].Height}.");
        Assert.All(edges, edge => Assert.Equal(0, TopologyEdgeRouter.Diagnose(prepared, edge, nodes).ObstacleHits));
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

    [Fact]
    public void PackedGroupRouteRepairPreservesSupportedDuplicateEdgeIds() {
        var chart = TopologyChart.Create().WithViewport(1200, 800).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("a", "A").AddAutoGroup("b", "B")
            .AddAutoNode("one", "One", groupId: "a").AddAutoNode("middle", "Middle", groupId: "a")
            .AddAutoNode("last", "Last", groupId: "a").AddAutoNode("other", "Other", groupId: "b")
            .AddEdge("shared", "one", "middle").AddEdge("shared", "middle", "last")
            .AddEdge("skip", "one", "last");

        var prepared = TopologyLayoutEngine.Prepare(chart);

        Assert.Equal(3, prepared.Edges.Count);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void OrthogonalSkipEdgesRouteAroundIntermediateCards() {
        var chart = TopologyChart.Create().WithViewport(900, 300, 0).WithLegend(null)
            .AddNode("source", "Source", 30, 110, width: 110, height: 64)
            .AddNode("middle", "Intermediate", 380, 92, width: 150, height: 100)
            .AddNode("target", "Target", 740, 110, width: 110, height: 64)
            .AddEdge("skip", "source", "target", routing: TopologyEdgeRouting.Orthogonal);
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);

        var route = TopologyEdgeRouter.Route(chart, chart.Edges[0], nodes["source"], nodes["target"]);

        Assert.Equal("ObstacleAvoidingOrthogonal", route.Diagnostics.Strategy);
        Assert.True(route.Diagnostics.ObstacleHits == 0);
        Assert.NotEmpty(chart.ToPng());
    }
}
