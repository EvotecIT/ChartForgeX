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
