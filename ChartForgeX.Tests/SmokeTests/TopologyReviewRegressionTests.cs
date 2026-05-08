using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyIconLabelFittingUsesRenderedPlateWidth() {
        var chart = TopologyChart.Create()
            .WithId("icon-label-fit")
            .WithViewport(160, 110, 12)
            .WithLegend(null)
            .AddNode("edge", "OK", 0, 28, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 24, height: 24, symbol: "S")
            .WithNodeDisplay("edge", TopologyNodeDisplayMode.Icon);

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false, IncludeIconLabels = true });
        var plateX = GetAttribute(svg, "data-cfx-role=\"topology-node-icon-label\"", "x");

        Assert(plateX >= 0, "Viewport fitting should reserve the rendered minimum icon-label plate width.");
    }

    private static void TopologyEdgeLabelsTolerateDuplicateEdgeIds() {
        var chart = TopologyChart.Create()
            .WithId("duplicate-edge-labels")
            .WithViewport(360, 180, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 40, 72, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 48, height: 42)
            .AddNode("right", "Right", 260, 72, TopologyNodeKind.Database, TopologyHealthStatus.Warning, width: 48, height: 42)
            .AddEdge("dup", "left", "right", "primary", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("dup", "right", "left", "backup", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight);

        var options = new TopologyRenderOptions { IncludeLegend = false };
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-edge-id=\"dup\"", StringComparison.Ordinal), "Duplicate edge ids should not crash edge-label ordering.");
        Assert(chart.ToPng(options).Length > 64, "Duplicate edge ids should not crash PNG edge-label ordering.");
    }

    private static void TopologyDuplicatePortPeersUseEdgeInstanceIdentity() {
        var chart = TopologyChart.Create()
            .WithId("duplicate-port-peers")
            .WithViewport(340, 220, 20)
            .WithLegend(null)
            .AddNode("hub", "Hub", 70, 82, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 52, height: 52)
            .AddNode("a", "A", 230, 48, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 42, height: 38)
            .AddNode("b", "B", 230, 122, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 42, height: 38)
            .AddEdge("dup", "hub", "a", "A", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("dup", "hub", "b", "B", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal);
        foreach (var edge in chart.Edges) {
            edge.SourcePort = TopologyEdgePort.Right;
            edge.TargetPort = TopologyEdgePort.Left;
        }

        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var first = TopologyRenderPrimitives.EdgePoints(chart, chart.Edges[0], nodes);
        var second = TopologyRenderPrimitives.EdgePoints(chart, chart.Edges[1], nodes);

        Assert(Math.Abs(first[0].Y - second[0].Y) > 0.01, "Duplicate edge ids sharing one explicit port should still receive distinct fan slots.");
    }

    private static void TopologyOrthogonalDuplicateEdgesUseInferredRouteLanes() {
        var chart = TopologyChart.Create()
            .WithId("orthogonal-duplicates")
            .WithViewport(420, 260, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 70, 64, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 52, height: 42)
            .AddNode("right", "Right", 286, 160, TopologyNodeKind.Database, TopologyHealthStatus.Warning, width: 52, height: 42)
            .AddEdge("primary", "left", "right", "primary", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("backup", "left", "right", "backup", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal);

        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var primary = TopologyRenderPrimitives.EdgePoints(chart, chart.Edges[0], nodes);
        var backup = TopologyRenderPrimitives.EdgePoints(chart, chart.Edges[1], nodes);

        Assert(Math.Abs(primary[1].X - backup[1].X) > 0.01, "Default duplicate orthogonal edges should infer distinct route lanes instead of collapsing.");
    }

    private static void TopologyEdgeLabelObstaclesFollowRenderedGroupOptions() {
        var headerChart = TopologyChart.Create()
            .WithId("headerless-label")
            .WithViewport(420, 260, 20)
            .WithLegend(null)
            .AddGroup("middle", "Hidden Header", 150, 118, 100, 80, TopologyHealthStatus.Warning)
            .AddNode("left", "Left", 40, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 40, height: 40)
            .AddNode("right", "Right", 320, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 40, height: 40)
            .AddEdge("link", "left", "right", "64 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight);

        var headerless = TopologyRenderPrimitives.EdgeLabelLayouts(headerChart, new TopologyRenderOptions { IncludeLegend = false, IncludeGroupLabels = false }).Single();
        Assert(Math.Abs(headerless.CenterY - 150) < 0.01, "Hidden group headers should not reserve phantom edge-label obstacles.");

        var groupChart = TopologyChart.Create()
            .WithId("groupless-label")
            .WithViewport(420, 260, 20)
            .WithLegend(null)
            .AddGroup("left-group", "Left Group", 95, 110, 90, 90, TopologyHealthStatus.Healthy)
            .AddGroup("right-group", "Right Group", 205, 110, 90, 90, TopologyHealthStatus.Warning)
            .AddNode("left", "Left", 40, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "left-group", width: 40, height: 40)
            .AddNode("right", "Right", 320, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "right-group", width: 40, height: 40)
            .AddEdge("link", "left", "right", "64 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight);

        var groupless = TopologyRenderPrimitives.EdgeLabelLayouts(groupChart, new TopologyRenderOptions { IncludeLegend = false, IncludeGroups = false, IncludeGroupLabels = false }).Single();
        Assert(Math.Abs(groupless.CenterY - 150) < 0.01, "Hidden group surfaces should not reserve phantom edge-label obstacles.");
    }

    private static void TopologyHiddenNodesDoNotAffectViewportFitOrEdgeLabels() {
        var viewportChart = TopologyChart.Create()
            .WithId("hidden-fit")
            .WithViewport(220, 160, 18)
            .WithLegend(null)
            .AddNode("visible", "Visible", 34, 58, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 52, height: 40)
            .AddNode("anchor", "Anchor", 1200, 740, TopologyNodeKind.Location, TopologyHealthStatus.Critical, width: 60, height: 44)
            .WithNodeDisplay("anchor", TopologyNodeDisplayMode.Hidden);

        var fitSvg = viewportChart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(GetAttribute(fitSvg, string.Empty, "width") < 400, "Hidden anchor nodes should not expand normalized viewport bounds.");

        var labelChart = TopologyChart.Create()
            .WithId("hidden-label-obstacle")
            .WithViewport(420, 260, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 40, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 40, height: 40)
            .AddNode("right", "Right", 320, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 40, height: 40)
            .AddNode("anchor", "Anchor", 176, 139, TopologyNodeKind.Location, TopologyHealthStatus.Critical, width: 48, height: 22)
            .AddEdge("link", "left", "right", "64 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .WithNodeDisplay("anchor", TopologyNodeDisplayMode.Hidden);

        var label = TopologyRenderPrimitives.EdgeLabelLayouts(labelChart, new TopologyRenderOptions { IncludeLegend = false }).Single();
        Assert(Math.Abs(label.CenterY - 150) < 0.01, "Hidden anchor nodes should not reserve phantom edge-label obstacles.");
    }

    private static void TopologyEdgeLabelClearanceUsesBackgroundWhenGroupsAreHidden() {
        var theme = TopologyTheme.Light();
        var group = new TopologyGroup { Id = "g", Label = "Group", X = 40, Y = 40, Width = 160, Height = 120, Status = TopologyHealthStatus.Warning };
        var fill = TopologyRenderPrimitives.EdgeLabelClearanceFill(group, theme, new TopologyRenderOptions { IncludeGroups = false }.WithMonitoringDashboardStyle());

        Assert(string.Equals(fill, theme.Background, StringComparison.Ordinal), "Monitoring edge-label clearance should use the actual background when group cards are hidden.");
    }

    private static void TopologyGeographicRegionHullsIgnoreHiddenNodes() {
        var chart = TopologyChart.Create()
            .WithId("hidden-hull")
            .WithViewport(640, 340, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .AddGroup("amer", "AMER", 0, 0, 0, 0, TopologyHealthStatus.Healthy, "1 site", symbol: "region")
            .AddNode("visible", "Visible", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Healthy, "amer", width: 48, height: 36)
            .AddNode("anchor", "Anchor", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Critical, "amer", width: 24, height: 24)
            .WithGroupCoordinates("amer", -98.5795, 39.8283)
            .WithNodeCoordinates("visible", -98.5795, 39.8283)
            .WithNodeCoordinates("anchor", -10, 52)
            .WithNodeDisplay("anchor", TopologyNodeDisplayMode.Hidden);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeGroups = false, IncludeGeographicRegionHulls = true };
        var svg = chart.ToSvg(options);
        var radius = GetAttribute(svg, "data-cfx-role=\"topology-geographic-region-hulls\"", "r");

        Assert(Math.Abs(radius - options.GeographicRegionHullMinRadius) < 0.01, "Hidden geographic anchor nodes should not enlarge rendered region hulls.");
        Assert(chart.ToPng(options).Length > 64, "Hidden geographic anchor nodes should not break PNG region hull rendering.");
    }
}
