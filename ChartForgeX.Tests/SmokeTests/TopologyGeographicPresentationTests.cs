using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyMonitoringStyleUsesSoftWorldMapSilhouette() {
        var chart = TopologyChart.Create()
            .WithId("monitoring-map")
            .WithViewport(720, 420, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .AddNode("amer", "AMER", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 56, height: 44, symbol: "H")
            .AddNode("emea", "EMEA", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 56, height: 44, symbol: "H")
            .AddNode("apac", "APAC", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Critical, width: 56, height: 44, symbol: "H")
            .WithNodeCoordinates("amer", -98.5795, 39.8283)
            .WithNodeCoordinates("emea", 10, 50)
            .WithNodeCoordinates("apac", 103.8198, 1.3521)
            .AddEdge("amer-emea", "amer", "emea", "68 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Curved)
            .AddEdge("emea-apac", "emea", "apac", "92 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Critical, VisualLinkDirection.Forward, TopologyEdgeRouting.Curved);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeGroups = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-visual-style=\"MonitoringDashboard\"", StringComparison.Ordinal), "Monitoring style should be emitted as reusable SVG metadata.");
        Assert(svg.Contains("data-cfx-map-background-style=\"SoftSilhouette\"", StringComparison.Ordinal), "Monitoring geographic topology should use soft silhouettes by default.");
        Assert(svg.Contains("data-cfx-role=\"topology-map-boundary\"", StringComparison.Ordinal), "World geographic topology should render reusable land silhouettes.");
        Assert(svg.Contains("data-cfx-role=\"topology-map-surface\"", StringComparison.Ordinal), "Monitoring geographic topology should render a native map surface beneath its silhouettes.");
        Assert(!svg.Contains("data-cfx-role=\"topology-map-land\"", StringComparison.Ordinal), "Soft silhouette map style should not fall back to dotted land when world boundaries are available.");
        Assert(chart.ToPng(options).Length > 64, "Soft silhouette geographic topology should render as PNG.");
    }

    private static void TopologyGeographicCalloutsPrioritizeSelectedGroupsAndMapEdges() {
        var chart = TopologyChart.Create()
            .WithId("monitoring-map-callouts")
            .WithViewport(720, 420, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .AddGroup("AMER", "AMER", 0, 0, 100, 80, TopologyHealthStatus.Healthy, "47 sites")
            .AddGroup("EMEA", "EMEA", 0, 0, 100, 80, TopologyHealthStatus.Warning, "56 sites")
            .AddGroup("APAC", "APAC", 0, 0, 100, 80, TopologyHealthStatus.Critical, "39 sites")
            .WithGroupCoordinates("AMER", -98.5795, 39.8283)
            .WithGroupCoordinates("EMEA", 10, 50)
            .WithGroupCoordinates("APAC", 103.8198, 1.3521)
            .AddNode("amer-hub", "AMER Hub", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "AMER", width: 56, height: 44, symbol: "H")
            .AddNode("emea-hub", "EMEA Hub", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Warning, "EMEA", width: 56, height: 44, symbol: "H")
            .AddNode("apac-hub", "APAC Hub", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Critical, "APAC", width: 56, height: 44, symbol: "H")
            .WithNodeCoordinates("amer-hub", -74.006, 40.7128)
            .WithNodeCoordinates("emea-hub", 0.1276, 51.5072)
            .WithNodeCoordinates("apac-hub", 103.8198, 1.3521);

        var options = new TopologyRenderOptions {
            IncludeLegend = false,
            IncludeGroups = false,
            IncludeGeographicCallouts = true,
            GeographicCalloutMaxItems = 2
        }
            .WithMonitoringDashboardStyle()
            .WithSelectedGroup("APAC");
        var svg = chart.ToSvg(options);
        var callouts = System.Xml.Linq.XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-visual-role") == "topology-geographic-callout").ToArray();
        Assert(callouts.Length == 2 && callouts.Any(element => (string?)element.Attribute("data-group-id") == "APAC"), "Selected geographic groups should be prioritized when callout count is capped.");
        Assert(svg.Contains("data-callout-placement=\"right", StringComparison.Ordinal), "Monitoring geographic callouts should prefer dashboard-style map-edge placements.");
        Assert(callouts.All(element => (string?)element.Attribute("data-group-id") != "EMEA"), "Non-selected middle groups should yield to selected groups when callout count is capped.");
        Assert(chart.ToPng(options).Length > 64, "Corner-prioritized geographic callouts should render as PNG.");
    }

    private static void TopologyGeographicCalloutsUseDashboardSlots() {
        var chart = TopologyChart.Create()
            .WithId("monitoring-map-callout-slots")
            .WithViewport(960, 520, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .AddGroup("AMER", "AMER", 0, 0, 100, 80, TopologyHealthStatus.Healthy, "47 sites")
            .AddGroup("EMEA", "EMEA", 0, 0, 100, 80, TopologyHealthStatus.Warning, "56 sites")
            .AddGroup("APAC", "APAC", 0, 0, 100, 80, TopologyHealthStatus.Critical, "39 sites")
            .WithGroupCoordinates("AMER", -98.5795, 39.8283)
            .WithGroupCoordinates("EMEA", 10, 50)
            .WithGroupCoordinates("APAC", 103.8198, 1.3521)
            .AddNode("amer-hub", "AMER Hub", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "AMER", width: 56, height: 44, symbol: "H")
            .AddNode("emea-hub", "EMEA Hub", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Warning, "EMEA", width: 56, height: 44, symbol: "H")
            .AddNode("apac-hub", "APAC Hub", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Critical, "APAC", width: 56, height: 44, symbol: "H")
            .WithNodeCoordinates("amer-hub", -74.006, 40.7128)
            .WithNodeCoordinates("emea-hub", 0.1276, 51.5072)
            .WithNodeCoordinates("apac-hub", 103.8198, 1.3521);

        var options = new TopologyRenderOptions {
            IncludeLegend = false,
            IncludeGroups = false,
            IncludeGeographicCallouts = true,
            GeographicCalloutMaxItems = 3
        }.WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-callout-placement=\"left-corner\"", StringComparison.Ordinal), "Dashboard geographic callouts should place the western region in the left edge slot.");
        Assert(svg.Contains("data-callout-placement=\"top\"", StringComparison.Ordinal), "Dashboard geographic callouts should place middle regions in the top context slot.");
        Assert(svg.Contains("data-callout-placement=\"right-corner\"", StringComparison.Ordinal), "Dashboard geographic callouts should place the eastern region in the right edge slot.");
        Assert(svg.Contains("data-cfx-role=\"topology-callout-preview-node\"", StringComparison.Ordinal), "Dashboard geographic callouts should include a compact topology preview.");
        Assert(chart.ToPng(options).Length > 64, "Dashboard geographic callout slots should render as PNG.");
    }

    private static void TopologyGeographicCalloutsAvoidRouteObstacles() {
        var chart = TopologyChart.Create()
            .WithId("monitoring-map-callout-routes")
            .WithViewport(820, 440, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .AddGroup("AMER", "AMER", 0, 0, 100, 80, TopologyHealthStatus.Healthy)
            .AddGroup("EMEA", "EMEA", 0, 0, 100, 80, TopologyHealthStatus.Healthy)
            .AddGroup("APAC", "APAC", 0, 0, 100, 80, TopologyHealthStatus.Critical)
            .WithGroupCoordinates("AMER", -98.5795, 39.8283)
            .WithGroupCoordinates("EMEA", 10, 50)
            .WithGroupCoordinates("APAC", 103.8198, 1.3521)
            .AddNode("amer-hub", "AMER", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "AMER", width: 64, height: 44, symbol: "H")
            .AddNode("emea-hub", "EMEA", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "EMEA", width: 64, height: 44, symbol: "H")
            .AddNode("apac-hub", "APAC", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Critical, "APAC", width: 64, height: 44, symbol: "H")
            .WithNodeCoordinates("amer-hub", -98.5795, 39.8283)
            .WithNodeCoordinates("emea-hub", 10, 50)
            .WithNodeCoordinates("apac-hub", 103.8198, 1.3521)
            .AddEdge("wan", "amer-hub", "apac-hub", "142 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Warning, VisualLinkDirection.Bidirectional, TopologyEdgeRouting.Curved, "backup")
            .WithEdgeLabelOffset("wan", 120, 58);

        var options = new TopologyRenderOptions {
            IncludeLegend = false,
            IncludeGroups = false,
            IncludeGeographicCallouts = true,
            IncludeEdgeLabelBackplates = false,
            GeographicCalloutMaxItems = 3
        }.WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-visual-role=\"topology-geographic-callout\"", StringComparison.Ordinal), "Route-aware geographic maps should still render dashboard callouts.");
        Assert(svg.Contains("data-cfx-role=\"topology-edge-label-text\"", StringComparison.Ordinal), "Route-aware callout placement should account for rendered edge label boxes.");
        var routeHalos = System.Xml.Linq.XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-route-halo").ToArray();
        Assert(routeHalos.Length > 0, "Monitoring geographic route arcs should render a clean underlay for dashboard map layering.");
        Assert(routeHalos.All(element => (string?)element.Attribute("fill") == "none" && element.RenderedColor("stroke").ToHex() == "#FFFFFF"), "Geographic route halos should explicitly disable path fill so open map arcs never render as filled wedges.");
        Assert(svg.Contains("data-cfx-role=\"topology-geographic-callout-leader-halo\"", StringComparison.Ordinal), "Monitoring geographic callout leaders should render a clean underlay above map routes and silhouettes.");
        Assert(svg.Contains("data-cfx-role=\"topology-geographic-callout-leader\"", StringComparison.Ordinal), "Monitoring geographic callouts should use routed leaders instead of raw diagonal connector lines.");
        Assert(chart.ToPng(options).Length > 64, "Route-aware geographic callouts should render as PNG.");
    }

    private static void TopologyMonitoringRegionHullsUseTighterDefaults() {
        var chart = TopologyChart.Create()
            .WithId("monitoring-region-hulls")
            .WithViewport(720, 420, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .AddGroup("APAC", "APAC", 0, 0, 100, 80, TopologyHealthStatus.Critical)
            .WithGroupCoordinates("APAC", 103.8198, 1.3521)
            .AddNode("sin", "Singapore", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "APAC", width: 52, height: 40, symbol: "H")
            .AddNode("syd", "Sydney", 0, 0, TopologyNodeKind.Branch, TopologyHealthStatus.Critical, "APAC", width: 52, height: 40, symbol: "S")
            .WithNodeCoordinates("sin", 103.8198, 1.3521)
            .WithNodeCoordinates("syd", 151.2093, -33.8688);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeGroups = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        var radius = GetAttribute(svg, "data-cfx-role=\"topology-geographic-hull\"", "rx");

        Assert(radius <= 82, "Monitoring geographic region hulls should use a tighter max radius than generic report maps.");
        Assert(svg.Contains("data-hull-padding=\"16\"", StringComparison.Ordinal), "Region hull padding should be exposed as reusable SVG metadata.");
        Assert(chart.ToPng(options).Length > 64, "Tighter monitoring region hulls should render as PNG.");
    }

}
