using System.Globalization;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class DenseTopologyLayoutTests {
    private static readonly TopologyRenderOptions TileOptions = new() { ReadableDenseLayout = true, IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile };

    [Fact]
    public void LeftToRight_ManySites_WrapIntoRowsWithinViewportWidth() {
        var chart = Sites(12, 3);
        var prepared = TopologyLayoutEngine.Prepare(chart, options: TileOptions);
        Assert.True(prepared.Groups.Max(group => group.X + group.Width) <= chart.Viewport.Width);
        Assert.True(prepared.Groups.Select(group => group.Y).Distinct().Count() > 1, "Twelve sites should wrap onto several rows.");
        Assert.Equal(prepared.Groups.Select(group => group.Id), chart.Groups.Select(group => group.Id));
        var rows = prepared.Groups.GroupBy(group => group.Y).OrderBy(row => row.Key).ToArray();
        for (var i = 1; i < rows.Length; i++) Assert.True(rows[i].Key >= rows[i - 1].Max(group => group.Y + group.Height) + 40);
    }

    [Fact]
    public void LeftToRight_FewSites_KeepOneRow() {
        var prepared = TopologyLayoutEngine.Prepare(Sites(6, 3), options: TileOptions);
        Assert.Single(prepared.Groups.Select(group => group.Y).Distinct());
    }

    [Fact]
    public void DefaultOptions_KeepClassicDenseLayout() {
        var classic = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile };
        var wide = TopologyLayoutEngine.Prepare(Sites(12, 3), options: classic);
        Assert.Single(wide.Groups.Select(group => group.Y).Distinct());
        Assert.Equal(TopologyGroupLayoutPolicy.CollapsedDots, TopologyLayoutEngine.Prepare(Sites(1, 10), options: classic).Groups[0].AppliedLayoutPolicy);
        Assert.Equal(TopologyGroupLayoutPolicy.CollapsedDots, TopologyLayoutEngine.Prepare(Sites(1, 10)).Groups[0].AppliedLayoutPolicy);
        var pair = TopologyLayoutEngine.Prepare(Sites(1, 4), options: classic);
        Assert.Equal(0, TopologyNodeFootprint.Caption(pair, pair.Nodes[0]).Height);
    }

    [Theory]
    [InlineData(7, 2)]
    [InlineData(12, 4)]
    [InlineData(20, 6)]
    public void WrappedRows_LegendStaysBelowContent(int sites, int controllers) {
        var chart = Sites(sites, controllers).WithLegend(TopologyLegend.Default());
        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile });
        var legendTop = prepared.Viewport.Height - prepared.Viewport.Padding - (TopologyRenderPrimitives.LegendReservedHeight(prepared.Legend, prepared.Viewport) - 24);
        Assert.True(prepared.Groups.Max(group => group.Y + group.Height) + 20 <= legendTop, "The legend must not overlap wrapped site rows.");
    }

    [Theory]
    [InlineData(9, TopologyGroupLayoutPolicy.PairRows)]
    [InlineData(18, TopologyGroupLayoutPolicy.Grid)]
    [InlineData(24, TopologyGroupLayoutPolicy.Grid)]
    [InlineData(25, TopologyGroupLayoutPolicy.CollapsedDots)]
    public void AutoPolicy_KeepsCardsUpToTwentyFourNodes(int controllers, TopologyGroupLayoutPolicy expected) {
        var prepared = TopologyLayoutEngine.Prepare(Sites(1, controllers), options: TileOptions);
        Assert.Equal(expected, prepared.Groups[0].AppliedLayoutPolicy);
    }

    [Fact]
    public void GridCards_LeaveRoomForCaptionsAndRoutes() {
        var prepared = TopologyLayoutEngine.Prepare(Sites(1, 16), options: TileOptions);
        var nodes = prepared.Nodes.OrderBy(node => node.Y).ThenBy(node => node.X).ToArray();
        var rowPitch = nodes.Select(node => node.Y).Distinct().OrderBy(y => y).Take(2).ToArray();
        var caption = TopologyNodeFootprint.Caption(prepared, nodes[0]);
        Assert.True(rowPitch[1] - rowPitch[0] >= nodes[0].Height + caption.Height + 30, "Rows must fit the caption plus a routing gutter.");
        var sameRow = nodes.Where(node => Math.Abs(node.Y - nodes[0].Y) < 0.1).OrderBy(node => node.X).ToArray();
        var captionWidth = Math.Max(nodes[0].Width, caption.Width);
        for (var i = 1; i < sameRow.Length; i++) Assert.True(sameRow[i].X - sameRow[i - 1].X >= captionWidth + 20, "Card columns must fit the caption plus a routing gutter.");
    }

    [Fact]
    public void ObstacleAvoidingRoute_ReachesCardEnclosedOnThreeSides() {
        // B is boxed in on the left, top, and bottom, so only a route that wraps around to its right side is clear.
        var chart = TopologyChart.Create().WithId("cup").WithViewport(860, 440, 0).WithLegend(null)
            .AddNode("a", "A", 20, 200, width: 60, height: 40)
            .AddNode("b", "B", 640, 200, width: 60, height: 40)
            .AddNode("left", "L", 560, 130, width: 40, height: 180)
            .AddNode("top", "T", 560, 90, width: 190, height: 30)
            .AddNode("bottom", "D", 560, 320, width: 190, height: 30);
        chart.AddEdge("a-b", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var report = chart.Prepare(new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false }).Analyze();
        Assert.Equal(0, ReplicationTopologyFixture.NodeCardCrossings(report));
        Assert.Equal("maze", report.Edges.Single().Corridor);

        var classic = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false });
        Assert.NotEqual("maze", classic.Analyze().Edges.Single().Corridor);
        Assert.Equal(classic.ToSvg(), chart.Prepare(new TopologyRenderOptions { ReadableDenseLayout = false, IncludeLegend = false }).ToSvg());
    }

    [Fact]
    public void RightToLeft_ManySites_WrapWithinViewportWidth() {
        var chart = Sites(12, 3).WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.RightToLeft);
        var prepared = TopologyLayoutEngine.Prepare(chart, options: TileOptions);
        Assert.True(prepared.Groups.Max(group => group.X + group.Width) - prepared.Groups.Min(group => group.X) <= chart.Viewport.Width);
        Assert.True(prepared.Groups.Select(group => group.Y).Distinct().Count() > 1);
    }

    [Fact]
    public void MazeRoute_FractionalCoordinates_StayStrictlyOrthogonal() {
        var chart = TopologyChart.Create().WithId("cup-fraction").WithViewport(860, 440, 0).WithLegend(null)
            .AddNode("a", "A", 20.37, 200.21, width: 60.3, height: 40.1)
            .AddNode("b", "B", 640.13, 200.77, width: 60.3, height: 40.1)
            .AddNode("left", "L", 560, 130, width: 40, height: 180)
            .AddNode("top", "T", 560, 90, width: 190, height: 30)
            .AddNode("bottom", "D", 560, 320, width: 190, height: 30)
            .AddEdge("a-b", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var edge = chart.Prepare(new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false }).Analyze().Edges.Single();
        Assert.Equal("maze", edge.Corridor);
        for (var i = 1; i < edge.Points.Count; i++) {
            var horizontal = Math.Abs(edge.Points[i].Y - edge.Points[i - 1].Y) < 0.0001;
            var vertical = Math.Abs(edge.Points[i].X - edge.Points[i - 1].X) < 0.0001;
            Assert.True(horizontal || vertical, "Segment " + i.ToString(CultureInfo.InvariantCulture) + " is diagonal.");
        }
    }

    private static TopologyChart Sites(int sites, int controllers) {
        var chart = TopologyChart.Create().WithId("sites").WithViewport(1400, 900, 24).WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight);
        for (var site = 0; site < sites; site++) {
            var id = "s" + site.ToString("00", CultureInfo.InvariantCulture);
            chart.AddAutoGroup(id, "Site " + site.ToString(CultureInfo.InvariantCulture));
            for (var dc = 0; dc < controllers; dc++) chart.AddAutoNode(id + "-dc" + dc.ToString("00", CultureInfo.InvariantCulture), id.ToUpperInvariant() + "-DC" + dc.ToString("00", CultureInfo.InvariantCulture), TopologyNodeKind.Server, TopologyHealthStatus.Healthy, id, width: 88, height: 40);
        }

        return chart;
    }
}
