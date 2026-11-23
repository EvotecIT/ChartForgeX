using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>A hidden node draws nothing, so routes pass over its position instead of detouring around it.</summary>
public sealed class TopologyHiddenNodeRoutingTests {
    [Theory]
    [InlineData(TopologyEdgeRouting.ObstacleAvoidingOrthogonal, false)]
    [InlineData(TopologyEdgeRouting.ObstacleAvoidingOrthogonal, true)]
    [InlineData(TopologyEdgeRouting.Orthogonal, false)]
    public void RouteRunsStraightOverAHiddenNodeOnTheDirectPath(TopologyEdgeRouting routing, bool hiddenByDefault) {
        var chart = Chart(routing);
        var hidden = chart.Nodes.Single(node => node.Id == "hidden");
        var options = new TopologyRenderOptions { FitContentToViewport = false, IncludeLegend = false };
        if (hiddenByDefault) {
            // The render default hides every node that does not choose its own display mode.
            options.NodeDisplayMode = TopologyNodeDisplayMode.Hidden;
            foreach (var node in chart.Nodes.Where(node => node.Id != "hidden")) node.DisplayMode = TopologyNodeDisplayMode.Card;
        } else {
            hidden.DisplayMode = TopologyNodeDisplayMode.Hidden;
        }

        var prepared = chart.Prepare(options);
        Save("hidden-node-route-" + routing + (hiddenByDefault ? "-default" : string.Empty), prepared);
        var route = Assert.Single(prepared.Analyze().Edges).Points;

        Assert.True(route.Count >= 2);
        Assert.All(route, point => Assert.Equal(route[0].Y, point.Y, 3));
        Assert.True(route[0].X <= hidden.X && route[route.Count - 1].X >= hidden.X + hidden.Width, "The route does not span the hidden node's position.");
    }

    [Fact]
    public void RouteStillDetoursAroundTheSameNodeWhenItIsDrawn() {
        var chart = Chart(TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var options = new TopologyRenderOptions { FitContentToViewport = false, IncludeLegend = false };

        var route = Assert.Single(chart.Prepare(options).Analyze().Edges).Points;

        Assert.Contains(route, point => Math.Abs(point.Y - route[0].Y) > 1);
    }

    [Fact]
    public void DensePlanIsRebuiltWhenAHiddenNodeIsShownInPlace() {
        var chart = Chart(TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        chart.RenderOptions = new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false };
        var hidden = chart.Nodes.Single(node => node.Id == "hidden");
        hidden.DisplayMode = TopologyNodeDisplayMode.Hidden;
        var straight = TopologyDenseRoutePlanner.Plan(chart)![chart.Edges[0]]!;

        hidden.DisplayMode = TopologyNodeDisplayMode.Card;
        var detour = TopologyDenseRoutePlanner.Plan(chart)![chart.Edges[0]]!;

        Assert.All(straight, point => Assert.Equal(straight[0].Y, point.Y, 3));
        Assert.Contains(detour, point => Math.Abs(point.Y - detour[0].Y) > 1);
    }

    private static void Save(string name, PreparedTopology prepared) {
        var directory = Environment.GetEnvironmentVariable("CFX_TOPOLOGY_FIXTURE_OUTPUT");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng());
    }

    private static TopologyChart Chart(TopologyEdgeRouting routing) {
        var chart = TopologyChart.Create().Id("hidden-node-route").Viewport(new TopologyViewport(760, 320, 24)).LayoutMode(TopologyLayoutMode.Manual).Legend((TopologyLegend?)null)
            .AddNode("source", "Source", 60, 120, width: 140, height: 72)
            .AddNode("hidden", "Hidden", 310, 110, width: 140, height: 92)
            .AddNode("target", "Target", 560, 120, width: 140, height: 72)
            .AddEdge("link", "source", "target", routing: routing);
        foreach (var node in chart.Nodes) { node.PreserveDisplayModeSize = true; node.ShowStatusBadge = false; }
        return chart;
    }
}
