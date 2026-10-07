using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyBoundaryLaneTests {
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void BoundaryCorridorSeparatesRoutesInwardWithoutAWholeObstacleDetour(int rotation) {
        const double width = 700, height = 600;
        var chart = TopologyChart.Create().WithLegend(null).WithViewport(rotation % 2 == 0 ? width : height, rotation % 2 == 0 ? height : width);
        void Node(string id, double x, double y, double w, double h) {
            var rect = rotation switch {
                1 => new ChartRect(height - y - h, x, h, w),
                2 => new ChartRect(width - x - w, height - y - h, w, h),
                3 => new ChartRect(y, width - x - w, h, w),
                _ => new ChartRect(x, y, w, h)
            };
            chart.AddNode(id, id, rect.X, rect.Y, width: rect.Width, height: rect.Height);
        }
        Node("a1", 80, 80, 80, 40); Node("b1", 500, 80, 80, 40);
        Node("a2", 80, 140, 80, 40); Node("b2", 500, 140, 80, 40);
        // The search has only the boundary grid line above this obstacle. Lane separation can still move inward
        // within the clear corridor; a repair should not send a relationship around the far end of the tall obstacle.
        Node("wall", 250, 35, 180, 500);
        var source = new[] { TopologyEdgePort.Right, TopologyEdgePort.Bottom, TopologyEdgePort.Left, TopologyEdgePort.Top }[rotation];
        var target = new[] { TopologyEdgePort.Left, TopologyEdgePort.Top, TopologyEdgePort.Right, TopologyEdgePort.Bottom }[rotation];
        for (var i = 1; i <= 2; i++) chart.AddEdge("e" + i, "a" + i, "b" + i, routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .WithEdgePorts("e" + i, source, target);
        var report = chart.Prepare(new TopologyRenderOptions {
            ReadableDenseLayout = true, IncludeTitle = false, IncludeLegend = false, IncludeNodeLabels = false, IncludeGroups = false
        }).Analyze();
        Assert.Empty(report.RouteOverlaps);
        Assert.Empty(report.RouteCrossings);
        Assert.All(report.Edges, edge => {
            Assert.Equal("maze", edge.Corridor);
            Assert.True(edge.SourceAttached && edge.TargetAttached);
            Assert.All(edge.Points, point => {
                var y = rotation switch { 1 => height - point.X, 2 => height - point.Y, 3 => point.X, _ => point.Y };
                Assert.InRange(y, 24, 190);
            });
        });
    }
}
