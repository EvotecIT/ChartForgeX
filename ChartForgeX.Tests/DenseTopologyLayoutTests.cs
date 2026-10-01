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

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight, 660)]
    [InlineData(TopologyLayoutDirection.BottomToTop, 1400)]
    public void FewSites_OneRowOfCards_SizePanelsAndCanvasToTheirContent(TopologyLayoutDirection direction, int height) {
        // Two and one controllers per site in a viewport much taller than the content: the panels take the height of
        // their cards and captions, share one height per row, and the legend follows them instead of the viewport bottom.
        var chart = TopologyChart.Create().WithId("small").WithTitle("Replication").WithViewport(1180, height, 24)
            .WithLayout(TopologyLayoutMode.DenseGrouped, direction).WithLegend(TopologyLegend.Default());
        var counts = new[] { 2, 1, 2 };
        for (var site = 0; site < counts.Length; site++) {
            chart.AddAutoGroup("s" + site, "Site " + site);
            for (var dc = 0; dc < counts[site]; dc++) chart.AddAutoNode("s" + site + "-dc" + dc, "S" + site + "-DC" + dc, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "s" + site, width: 64, height: 40);
        }

        chart.AddEdge("e0", "s0-dc0", "s1-dc0", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .AddEdge("e1", "s1-dc0", "s2-dc1", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .AddEdge("e2", "s0-dc1", "s2-dc0", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeEdgeLabels = false };
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);

        Assert.Single(prepared.Groups.Select(group => group.Height).Distinct());
        foreach (var group in prepared.Groups) {
            var contentBottom = prepared.Nodes.Where(node => node.GroupId == group.Id).Max(node => node.Y + TopologyNodeFootprint.Height(prepared, node));
            Assert.InRange(group.Y + group.Height - contentBottom, 0, 120);
        }

        var legendTop = prepared.Viewport.Height - prepared.Viewport.Padding - (TopologyRenderPrimitives.LegendReservedHeight(prepared.Legend, prepared.Viewport) - 24);
        Assert.InRange(legendTop - prepared.Groups.Max(group => group.Y + group.Height), 20, 60);
        Assert.True(prepared.Viewport.Height < height, $"The canvas should shrink to its content, was {prepared.Viewport.Height}.");
        // The first row of panels sits under the title in both directions, bottom-to-top included.
        Assert.InRange(prepared.Groups.Min(group => group.Y), 90, 130);
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
    public void PlannedRoute_FractionalCoordinates_StayStrictlyOrthogonal() {
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PlannedRoute_BottomPort_DoesNotCrossItsTileCaption(bool sourceBottom) {
        var chart = CupChart().WithEdgePorts("a-b", sourceBottom ? TopologyEdgePort.Bottom : TopologyEdgePort.Right,
            sourceBottom ? TopologyEdgePort.Left : TopologyEdgePort.Bottom);
        var layout = TopologyLayoutEngine.Prepare(chart, options: TileOptions);
        var report = chart.Prepare(TileOptions).Analyze();
        var edge = report.Edges.Single();
        var node = layout.Nodes.Single(item => item.Id == (sourceBottom ? "a" : "b"));
        var caption = TopologyNodeFootprint.Caption(layout, node);
        Assert.True(caption.Height > 0);
        var endpoint = sourceBottom ? 0 : edge.Points.Count - 1;
        var neighbor = sourceBottom ? 1 : edge.Points.Count - 2;
        Assert.False(CrossesCaption(edge.Points[endpoint], edge.Points[neighbor], node, caption.Height));
        Assert.Equal(0, ReplicationTopologyFixture.NodeCardCrossings(report));
    }

    [Fact]
    public void PlannedRoute_StaysWithinRenderedViewport() {
        var chart = TopologyChart.Create().WithId("viewport-maze").WithViewport(360, 240, 0).WithLegend(null)
            .AddNode("a", "A", 20, 100, width: 50, height: 40)
            .AddNode("b", "B", 290, 100, width: 50, height: 40)
            .AddNode("barrier", "Barrier", 145, 10, width: 50, height: 220)
            .AddEdge("a-b", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var prepared = chart.Prepare(new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false });
        var edge = chart.Prepare(TileOptions).Analyze().Edges.Single();
        Assert.All(edge.Points, point => {
            Assert.InRange(point.X, 0, prepared.Width);
            Assert.InRange(point.Y, 0, prepared.Height);
        });
    }

    [Fact]
    public void PlannedRoute_InferredBlockedPort_UsesClearAlternateSide() {
        var chart = TopologyChart.Create().WithId("blocked-inferred-port").WithViewport(860, 440, 0).WithLegend(null)
            .AddNode("a", "Source", 20, 200, width: 60, height: 40)
            .AddNode("b", "Target", 640, 200, width: 60, height: 40)
            .AddNode("block", "Block", 570, 190, width: 50, height: 46)
            .AddEdge("a-b", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .WithEdgePorts("a-b", TopologyEdgePort.Right, TopologyEdgePort.Left);
        chart.Edges.Single().LayoutInference = TopologyEdgeLayoutInference.TargetPort;
        var report = chart.Prepare(TileOptions).Analyze();
        Assert.Equal("maze", report.Edges.Single().Corridor);
        Assert.Equal(0, ReplicationTopologyFixture.NodeCardCrossings(report));
    }

    [Fact]
    public void AutomaticVerticalRoute_DoesNotLeaveThroughTileCaption() {
        var chart = TopologyChart.Create().WithId("automatic-caption").WithViewport(400, 480, 0).WithLegend(null)
            .AddNode("a", "Source", 160, 80, width: 70, height: 40)
            .AddNode("b", "Target", 160, 320, width: 70, height: 40)
            .AddEdge("a-b", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var prepared = TopologyLayoutEngine.Prepare(chart, options: TileOptions);
        var edge = chart.Prepare(TileOptions).Analyze().Edges.Single();
        var source = prepared.Nodes.Single(node => node.Id == "a");
        var caption = TopologyNodeFootprint.Caption(prepared, source);
        Assert.True(caption.Height > 0);
        Assert.False(CrossesCaption(edge.Points[0], edge.Points[1], source, caption.Height));
    }

    [Fact]
    public void OrthogonalRoute_AvoidsSourceCaptionBeyondItsFirstLeg() {
        var chart = TopologyChart.Create().WithId("wide-endpoint-caption").WithViewport(600, 440, 0).WithLegend(null)
            .AddNode("a", "Source caption extends far beyond its narrow card", 80, 100, width: 60, height: 40)
            .AddNode("b", "Target", 145, 160, width: 60, height: 40)
            .AddEdge("a-b", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .WithEdgePorts("a-b", TopologyEdgePort.Right, TopologyEdgePort.Left);
        var prepared = TopologyLayoutEngine.Prepare(chart, options: TileOptions);
        var source = prepared.Nodes.Single(node => node.Id == "a");
        var caption = TopologyNodeFootprint.Caption(prepared, source);
        Assert.True(caption.Width > source.Width + 10);
        var captionLeft = source.X + source.Width / 2 - caption.Width / 2;
        var captionRight = captionLeft + caption.Width;
        var points = chart.Prepare(TileOptions).Analyze().Edges.Single().Points;
        for (var i = 0; i + 1 < points.Count; i++)
            Assert.False(Math.Max(points[i].X, points[i + 1].X) > captionLeft &&
                         Math.Min(points[i].X, points[i + 1].X) < captionRight &&
                         Math.Max(points[i].Y, points[i + 1].Y) > source.Y + source.Height &&
                         Math.Min(points[i].Y, points[i + 1].Y) < source.Y + source.Height + caption.Height,
                "The route crosses its source caption after the first segment.");
    }

    [Fact]
    public void NamedBottomPort_BeneathReadableTileCaption_IsRejected() {
        var chart = CupChart().AddNodePort("a", "underside", TopologyEdgePort.Bottom)
            .WithEdgeNamedPorts("a-b", "underside", null);
        var error = Assert.Throws<InvalidOperationException>(() => chart.Prepare(TileOptions).Analyze());
        Assert.Contains("named Bottom port", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TopologyEdgeRouting.Straight, false)]
    [InlineData(TopologyEdgeRouting.Orthogonal, true)]
    public void NamedBottomPort_OnAuthoredRoute_RemainsAvailable(TopologyEdgeRouting routing, bool manual) {
        var chart = CupChart().AddNodePort("a", "underside", TopologyEdgePort.Bottom)
            .WithEdgeNamedPorts("a-b", "underside", null);
        chart.Edges.Single().Routing = routing;
        if (manual) chart.WithEdgeWaypoints("a-b", new ChartForgeX.Primitives.ChartPoint(330, 360));
        var prepared = TopologyLayoutEngine.Prepare(chart, options: TileOptions);
        var points = chart.Prepare(TileOptions).Analyze().Edges.Single().Points;
        Assert.True(points.Count >= 2);
        Assert.True(points[0].Y > prepared.Nodes.Single(node => node.Id == "a").Y + 40);
    }

    [Fact]
    public void HiddenEdgeLabels_DoNotPenalizeRoutes() {
        var chart = TopologyChart.Create().WithId("hidden-label-route").WithViewport(450, 260, 0).WithLegend(null)
            .AddNode("a", "Source", 20, 100, width: 60, height: 40)
            .AddNode("b", "Target", 350, 100, width: 60, height: 40)
            .AddNode("obstacle", "Obstacle", 180, 90, width: 50, height: 60)
            .AddEdge("a-b", "a", "b", "Replication", routing: TopologyEdgeRouting.Straight);
        var shown = chart.Prepare(new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false }).Analyze().Edges.Single();
        var hidden = chart.Prepare(new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false, IncludeEdgeLabels = false }).Analyze().Edges.Single();
        Assert.True(shown.LabelObstacleHits > 0);
        Assert.Equal(0, hidden.LabelObstacleHits);
    }

    [Fact]
    public void WrappedGroups_DeclaredSmallSizes_AreRaisedBeforeRowPlacement() {
        var chart = Sites(7, 5).WithViewport(900, 1200, 24);
        foreach (var group in chart.Groups) { group.Width = 190; group.Height = 170; }
        var prepared = TopologyLayoutEngine.Prepare(chart, options: TileOptions);
        Assert.All(prepared.Groups, group => {
            Assert.True(group.Width > 190 && group.Height > 170);
            foreach (var node in prepared.Nodes.Where(node => node.GroupId == group.Id))
                Assert.True(node.Y + TopologyNodeFootprint.Height(prepared, node) <= group.Y + group.Height);
        });
        var rows = prepared.Groups.GroupBy(group => group.Y).OrderBy(row => row.Key).ToArray();
        Assert.True(rows.Length > 1);
        for (var i = 1; i < rows.Length; i++)
            Assert.True(rows[i].Key >= rows[i - 1].Max(group => group.Y + group.Height) + 40);
    }

    [Fact]
    public void SingleRowGroups_DeclaredSmallSizes_ContainCardsWithoutOverlappingNeighbors() {
        var chart = Sites(6, 5);
        foreach (var group in chart.Groups) { group.Width = 190; group.Height = 170; }

        var prepared = TopologyLayoutEngine.Prepare(chart, options: TileOptions);
        Assert.Single(prepared.Groups.Select(group => group.Y).Distinct());
        foreach (var group in prepared.Groups) {
            Assert.True(group.Width > 190 && group.Height > 170);
            foreach (var node in prepared.Nodes.Where(node => node.GroupId == group.Id)) {
                Assert.True(node.X + node.Width <= group.X + group.Width);
                Assert.True(node.Y + TopologyNodeFootprint.Height(prepared, node) <= group.Y + group.Height);
            }
        }
        for (var i = 1; i < prepared.Groups.Count; i++)
            Assert.True(prepared.Groups[i].X >= prepared.Groups[i - 1].X + prepared.Groups[i - 1].Width + 24);
    }

    [Fact]
    public void WrappedGroups_ReservePanelSurfaceInsetWithinViewport() {
        var chart = Sites(7, 1).WithViewport(490, 1800, 24);
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false,
            NodeDisplayMode = TopologyNodeDisplayMode.Tile, CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.Panel };
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        Assert.True(prepared.Viewport.Width <= chart.Viewport.Width,
            $"Surface inset expanded width from {chart.Viewport.Width} to {prepared.Viewport.Width}.");
        Assert.True(prepared.Groups[1].Y > prepared.Groups[0].Y);
    }

    [Fact]
    public void RoutePlan_RecomputesAfterViewportExpands() {
        var chart = CupChart().WithId("viewport-cache")
            .AddNode("roof", "Roof", 560, 0, width: 40, height: 100)
            .WithEdgePorts("a-b", TopologyEdgePort.Bottom, TopologyEdgePort.Left);
        chart.RenderOptions = TileOptions;
        var edge = chart.Edges.Single();
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        chart.Viewport.Height = 280;
        var narrow = TopologyEdgeRouter.Route(chart, edge, nodes["a"], nodes["b"]);
        chart.Viewport.Height = 440;
        var expanded = TopologyEdgeRouter.Route(chart, edge, nodes["a"], nodes["b"]);
        Assert.NotEqual("maze", narrow.Diagnostics.Corridor);
        Assert.Equal("maze", expanded.Diagnostics.Corridor);
        Assert.All(expanded.Points, point => Assert.InRange(point.Y, 0, chart.Viewport.Height));
    }

    [Fact]
    public void RoutePlan_DuplicateEdgeIds_KeepOppositeEndpoints() {
        var chart = CupChart().AddEdge("a-b", "b", "a", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        chart.RenderOptions = TileOptions;
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var forward = TopologyEdgeRouter.Route(chart, chart.Edges[0], nodes["a"], nodes["b"]);
        var reverse = TopologyEdgeRouter.Route(chart, chart.Edges[1], nodes["b"], nodes["a"]);
        Assert.Equal("maze", forward.Diagnostics.Corridor);
        Assert.Equal("maze", reverse.Diagnostics.Corridor);
        Assert.True(forward.Points[0].X < forward.Points[^1].X);
        Assert.True(reverse.Points[0].X > reverse.Points[^1].X);
    }

    [Fact]
    public void DuplicateEdgeIds_StillCountSiblingLabelsAndRouteCrossings() {
        var chart = TopologyChart.Create().WithId("duplicate-edge-obstacles").WithViewport(400, 260, 0).WithLegend(null)
            .AddNode("a", "A", 20, 100, width: 40, height: 40)
            .AddNode("b", "B", 320, 100, width: 40, height: 40)
            .AddEdge("link", "a", "b", routing: TopologyEdgeRouting.Straight)
            .AddEdge("link", "a", "b", "Sibling", routing: TopologyEdgeRouting.Straight);
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var plan = TopologyEdgeRouter.Route(chart, chart.Edges[0], nodes["a"], nodes["b"]);
        Assert.True(plan.Diagnostics.ObstacleHits > 0, "The sibling label must remain a route obstacle despite sharing an edge ID.");
        Assert.True(plan.Diagnostics.RouteOverlapScore > 0, "The sibling route must count as a crossing despite sharing an edge ID.");
        chart.RenderOptions = new TopologyRenderOptions { IncludeEdgeLabels = false };
        var hidden = TopologyEdgeRouter.Route(chart, chart.Edges[0], nodes["a"], nodes["b"]);
        Assert.Equal(0, hidden.Diagnostics.ObstacleHits);
    }

    [Fact]
    public void PlannedRoute_NamedOffset_AvoidsCardAfterEndpointPlacement() {
        var chart = TopologyChart.Create().WithId("named-maze").WithViewport(860, 440, 0).WithLegend(null)
            .AddNode("a", "A", 20, 200, width: 60, height: 40)
            .AddNode("b", "B", 640, 200, width: 60, height: 40)
            .AddNode("left", "L", 560, 130, width: 40, height: 180)
            .AddNode("top", "T", 560, 90, width: 190, height: 30)
            .AddNode("bottom", "D", 560, 320, width: 190, height: 30)
            .AddEdge("a-b", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .AddNodePort("a", "upper", TopologyEdgePort.Right, 0.15)
            .WithEdgeNamedPorts("a-b", "upper", null);
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false };
        var preparedChart = TopologyLayoutEngine.Prepare(chart, options: options);
        var routed = TopologyEdgeRouter.Route(preparedChart, preparedChart.Edges.Single(),
            preparedChart.Nodes.Single(node => node.Id == "a"), preparedChart.Nodes.Single(node => node.Id == "b"));
        Assert.Equal("maze", routed.Diagnostics.Corridor);
        Assert.Equal(preparedChart.Nodes.Single(node => node.Id == "a").Y + 40 * 0.15, routed.Points[0].Y, 3);
        var report = chart.Prepare(options).Analyze();
        Assert.Equal("maze", report.Edges.Single().Corridor);
        var points = report.Edges.Single().Points;
        for (var i = 1; i < points.Count; i++)
            Assert.True(Math.Abs(points[i].X - points[i - 1].X) < 0.001 ||
                        Math.Abs(points[i].Y - points[i - 1].Y) < 0.001,
                "Named-port placement introduced a diagonal maze segment.");
        Assert.Equal(0, ReplicationTopologyFixture.NodeCardCrossings(report));
        Assert.Equal(0, report.Edges.Single().ObstacleHits);
    }

    [Fact]
    public void PlannedRoute_FanSpreading_DoesNotIntroduceCardCrossing() {
        const double blockerX = 85;
        const double blockerY = 195;
        var chart = TopologyChart.Create().WithId("fan-obstacle").WithViewport(860, 440, 0).WithLegend(null)
            .AddNode("a", "A", 20, 200, width: 60, height: 60)
            .AddNode("b", "B", 640, 220, width: 60, height: 40)
            .AddNode("block", "Block", blockerX, blockerY, width: 25, height: 20)
            .AddNode("left", "L", 560, 130, width: 40, height: 180)
            .AddNode("top", "T", 560, 90, width: 190, height: 30)
            .AddNode("bottom", "D", 560, 320, width: 190, height: 30)
            .AddEdge("a-b", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .WithEdgePorts("a-b", TopologyEdgePort.Right, TopologyEdgePort.Left);
        for (var i = 0; i < 7; i++)
            chart.AddEdge("peer" + i, "a", "b", routing: TopologyEdgeRouting.Straight)
                .WithEdgePorts("peer" + i, TopologyEdgePort.Right, TopologyEdgePort.Left);
        chart.RenderOptions = new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false,
            NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeNodeLabels = false };
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var route = TopologyEdgeRouter.Route(chart, chart.Edges[0], nodes["a"], nodes["b"]);
        Assert.Equal("maze", route.Diagnostics.Corridor);
        var rendered = TopologyRenderPrimitives.EdgePoints(chart, chart.Edges[0], nodes);
        for (var i = 0; i + 1 < rendered.Count; i++)
            Assert.False(Math.Max(rendered[i].X, rendered[i + 1].X) > blockerX &&
                         Math.Min(rendered[i].X, rendered[i + 1].X) < blockerX + 25 &&
                         Math.Max(rendered[i].Y, rendered[i + 1].Y) > blockerY &&
                         Math.Min(rendered[i].Y, rendered[i + 1].Y) < blockerY + 20,
                "Fan spreading moved the maze endpoint through a foreign card.");
    }

    [Fact]
    public void NamedPortPlacement_NearCard_IsRejectedInsteadOfResetToSideMidpoint() {
        var chart = TopologyChart.Create().WithId("named-near-card").WithViewport(500, 300, 0).WithLegend(null)
            .AddNode("a", "A", 80, 100, width: 60, height: 40)
            .AddNode("b", "B", 330, 100, width: 60, height: 40)
            .AddNode("block", "Block", 150, 125, width: 10, height: 20)
            .AddEdge("a-b", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .AddNodePort("a", "lower", TopologyEdgePort.Right, 0.8)
            .WithEdgeNamedPorts("a-b", "lower", null);
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false,
            NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeNodeLabels = false };
        chart.RenderOptions = options;
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var plan = TopologyEdgeRouter.Route(chart, chart.Edges.Single(), nodes["a"], nodes["b"]);
        Assert.Equal("aligned-direct", plan.Diagnostics.Corridor);
        var error = Assert.Throws<InvalidOperationException>(() => TopologyRenderPrimitives.EdgePoints(chart, chart.Edges.Single(), nodes));
        Assert.Contains("named port", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AlignedRoute_TwoNamedPorts_RetainIndependentOffsets() {
        var chart = TopologyChart.Create().WithId("two-named-ports").WithViewport(500, 300, 0).WithLegend(null)
            .AddNode("a", "A", 80, 100, width: 60, height: 40)
            .AddNode("b", "B", 330, 100, width: 60, height: 40)
            .AddEdge("a-b", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .AddNodePort("a", "lower", TopologyEdgePort.Right, 0.8)
            .AddNodePort("b", "upper", TopologyEdgePort.Left, 0.2)
            .WithEdgeNamedPorts("a-b", "lower", "upper");
        chart.RenderOptions = new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false,
            NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeNodeLabels = false };
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var route = TopologyEdgeRouter.Route(chart, chart.Edges.Single(), nodes["a"], nodes["b"]);
        Assert.Equal(TopologyEdgeRouter.PlannedCorridor, route.Diagnostics.Corridor);

        var points = TopologyRenderPrimitives.EdgePoints(chart, chart.Edges.Single(), nodes);
        Assert.Equal(132, points[0].Y, 3);
        Assert.Equal(108, points[^1].Y, 3);
        Assert.All(Enumerable.Range(1, points.Count - 1), index =>
            Assert.True(Math.Abs(points[index].X - points[index - 1].X) < 0.001 ||
                        Math.Abs(points[index].Y - points[index - 1].Y) < 0.001,
                "Named-port placement introduced a diagonal segment."));
    }

    [Fact]
    public void PlannedRoute_RespectsTitleAndLegendContentBounds() {
        var chart = TopologyChart.Create().WithId("header-footer-maze").WithViewport(860, 440, 0)
            .WithTitle("Replication topology")
            .WithLegend(TopologyLegend.Default().AddNodeKind("Server", TopologyNodeKind.Server, symbol: "S"))
            .AddNode("a", "A", 20, 200, width: 60, height: 40)
            .AddNode("b", "B", 640, 200, width: 60, height: 40)
            .AddNode("left", "L", 560, 130, width: 40, height: 180)
            .AddNode("top", "T", 560, 80, width: 190, height: 30)
            .AddNode("bottom", "D", 560, 320, width: 190, height: 30)
            .AddEdge("a-b", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = true };
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var route = chart.Prepare(options).Analyze().Edges.Single();
        var top = prepared.Viewport.Padding + 72;
        var bottom = prepared.Viewport.Height - prepared.Viewport.Padding -
            TopologyRenderPrimitives.LegendReservedHeight(prepared.Legend, prepared.Viewport);
        Assert.NotEqual("maze", route.Corridor);
        Assert.All(route.Points, point => Assert.InRange(point.Y, top, bottom));
    }

    [Fact]
    public void PlannedRoute_HiddenTitle_UsesTheAvailableTopCorridor() {
        var chart = TopologyChart.Create().WithId("hidden-title-corridor").WithViewport(500, 220, 0)
            .WithTitle("This title is not rendered").WithLegend(null)
            .AddNode("a", "A", 30, 80, width: 60, height: 30)
            .AddNode("b", "B", 350, 80, width: 60, height: 30)
            .AddNode("wall", "Wall", 175, 36, width: 80, height: 184)
            .AddEdge("a-b", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, IncludeTitle = false,
            IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeNodeLabels = false };
        chart.RenderOptions = options;
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var route = TopologyEdgeRouter.Route(chart, chart.Edges.Single(), nodes["a"], nodes["b"]);
        Assert.Equal("maze", route.Diagnostics.Corridor);
        Assert.Equal(0, route.Diagnostics.ObstacleHits);
        Assert.Contains(route.Points, point => point.Y < 36);
    }

    [Fact]
    public void MixedCardAndCaptionHeights_StayInsideWrappedGroup() {
        var chart = Sites(7, 5);
        var firstGroupNodes = chart.Nodes.Where(node => node.GroupId == chart.Groups[0].Id).ToArray();
        firstGroupNodes[0].Height = 96;
        firstGroupNodes[1].Label = "A long controller name spanning three caption lines";
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false,
            NodeDisplayMode = TopologyNodeDisplayMode.Tile, WrapNodeLabels = true, MaxNodeLabelLines = 3 };
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var group = prepared.Groups[0];
        var nodes = prepared.Nodes.Where(node => node.GroupId == group.Id).ToArray();
        Assert.True(TopologyNodeFootprint.Caption(prepared, nodes[1]).Height >
            TopologyNodeFootprint.Caption(prepared, nodes[0]).Height);
        Assert.All(nodes, node => Assert.True(node.Y + node.Height + TopologyNodeFootprint.Caption(prepared, node).Height <=
            group.Y + group.Height - 16, node.Id + " exceeds its group."));
    }

    [Fact]
    public void TileCaption_RespectsNodeCharacterLimitUsedByRenderers() {
        var chart = TopologyChart.Create().WithId("long-tile-label").WithViewport(420, 280, 0).WithLegend(null)
            .AddNode("a", "Alpha Bravo Charlie Delta Echo Foxtrot Golf", 360, 70, width: 54, height: 36);
        chart.Nodes.Single().MaximumLabelCharacters = 64;
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false,
            NodeDisplayMode = TopologyNodeDisplayMode.Tile, WrapNodeLabels = true, MaxNodeLabelLines = 3 };
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var node = prepared.Nodes.Single();
        Assert.Equal(50, TopologyNodeFootprint.Caption(prepared, node).Height);
        var captionRight = node.X + node.Width / 2 + TopologyNodeFootprint.Caption(prepared, node).Width / 2;
        Assert.True(captionRight <= prepared.Viewport.Width,
            $"The rendered caption extends to {captionRight} beyond the normalized width {prepared.Viewport.Width}.");
        Assert.Contains("Alpha", chart.ToSvg(options), StringComparison.Ordinal);
        Assert.True(chart.ToPng(options).Length > 64);
    }

    [Theory]
    [InlineData(TopologyGroupLayoutPolicy.HubAndBranch)]
    [InlineData(TopologyGroupLayoutPolicy.MiniMesh)]
    public void DenseMixedPolicies_ReserveWrappedTileCaptions(TopologyGroupLayoutPolicy policy) {
        var chart = TopologyChart.Create().WithId("caption-policy").WithViewport(520, 360, 0)
            .WithLegend(null).WithLayout(TopologyLayoutMode.DenseGrouped)
            .AddAutoGroup("site", "Site").WithGroupLayout("site", policy)
            .AddAutoNode("hub", "Alpha Bravo Charlie Delta Echo Foxtrot Golf", TopologyNodeKind.Hub,
                TopologyHealthStatus.Healthy, "site", width: 64, height: 40);
        for (var i = 0; i < 5; i++) chart.AddAutoNode("branch-" + i, "Long branch caption for node " + i,
            TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "site", width: 64, height: 40);
        foreach (var node in chart.Nodes) node.MaximumLabelCharacters = 64;
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, IncludeLegend = false,
            NodeDisplayMode = TopologyNodeDisplayMode.Tile, WrapNodeLabels = true, MaxNodeLabelLines = 3 };
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var nodes = prepared.Nodes.ToArray();
        var group = prepared.Groups.Single();
        Assert.Equal(policy, group.AppliedLayoutPolicy);
        foreach (var node in nodes) {
            var captionWidth = TopologyNodeFootprint.Width(prepared, node);
            var captionLeft = node.X + node.Width / 2 - captionWidth / 2;
            Assert.True(captionLeft >= group.X + 10 && captionLeft + captionWidth <= group.X + group.Width - 10,
                node.Id + " caption extends beyond its group width.");
            Assert.True(node.Y + TopologyNodeFootprint.Height(prepared, node) <=
                group.Y + group.Height - 16, node.Id + " caption extends beyond its group height.");
        }
        foreach (var upper in nodes) foreach (var lower in nodes) {
            if (ReferenceEquals(upper, lower) || lower.Y <= upper.Y + 0.001) continue;
            Assert.True(upper.Y + TopologyNodeFootprint.Height(prepared, upper) < lower.Y,
                upper.Id + " caption overlaps " + lower.Id);
        }
    }

    private static TopologyChart CupChart() => TopologyChart.Create().WithId("cup-caption").WithViewport(860, 440, 0).WithLegend(null)
        .AddNode("a", "Source", 20, 200, width: 60, height: 40)
        .AddNode("b", "Target", 640, 200, width: 60, height: 40)
        .AddNode("left", "L", 560, 130, width: 40, height: 180)
        .AddNode("top", "T", 560, 90, width: 190, height: 30)
        .AddNode("bottom", "D", 560, 320, width: 190, height: 30)
        .AddEdge("a-b", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);

    private static bool CrossesCaption(ChartForgeX.Primitives.ChartPoint a, ChartForgeX.Primitives.ChartPoint b, TopologyNode node, double captionHeight) {
        var top = node.Y + node.Height;
        var bottom = top + captionHeight;
        return Math.Max(a.Y, b.Y) > top && Math.Min(a.Y, b.Y) < bottom &&
               Math.Max(a.X, b.X) > node.X && Math.Min(a.X, b.X) < node.X + node.Width;
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
