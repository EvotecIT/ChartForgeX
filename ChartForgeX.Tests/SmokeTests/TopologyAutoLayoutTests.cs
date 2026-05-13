using System;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyFitsRenderedTileAdornmentsIntoViewport() {
        var chart = TopologyChart.Create()
            .WithId("tile-fit")
            .WithViewport(280, 180, 20)
            .WithLegend(TopologyLegend.Default())
            .AddNode("site", "Long Site Name", 108, 120, TopologyNodeKind.Branch, TopologyHealthStatus.Warning, subtitle: "10.20.2.0/24", width: 64, height: 46, symbol: "S")
            .WithNodeBadge("site", "WAN");

        var options = new TopologyRenderOptions {
            IncludeLegend = false,
            IncludeTileSubtitles = true,
            NodeDisplayMode = TopologyNodeDisplayMode.Tile
        };
        var svg = chart.ToSvg(options);
        Assert(!svg.Contains("viewBox=\"0 0 280 180\"", StringComparison.Ordinal), "Topology normalization should expand the viewport for tile labels, subtitle chips, and badges.");
        Assert(svg.Contains("data-cfx-role=\"topology-node-subtitle\"", StringComparison.Ordinal), "Tile subtitle chips should render after viewport fitting.");
        Assert(svg.Contains("data-node-badge=\"WAN\"", StringComparison.Ordinal), "Node badges should still be present after viewport fitting.");
        Assert(!svg.Contains("data-cfx-role=\"topology-legend\"", StringComparison.Ordinal), "Hidden legends should not reserve layout space or render.");
        Assert(chart.ToPng(options).Length > 64, "Viewport-expanded tile adornments should render as PNG.");
    }

    private static void TopologyCanFitDenseContentIntoFixedViewport() {
        var chart = TopologyChart.Create()
            .WithId("fixed-fit")
            .WithViewport(320, 220, 20)
            .WithLegend(null)
            .AddGroup("region", "Very Wide Region", 40, 90, 620, 180, TopologyHealthStatus.Healthy, "many sites", symbol: "region")
            .AddNode("a", "Left Branch", 80, 156, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "region", width: 92, height: 52, symbol: "S")
            .AddNode("b", "Middle Branch", 304, 156, TopologyNodeKind.Branch, TopologyHealthStatus.Warning, "region", width: 92, height: 52, symbol: "S")
            .AddNode("c", "Right Branch", 528, 156, TopologyNodeKind.Branch, TopologyHealthStatus.Critical, "region", width: 92, height: 52, symbol: "S")
            .AddEdge("a-c", "a", "c", "188 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "MPLS");

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle()
            .WithFitContentToViewport();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("width=\"320\"", StringComparison.Ordinal), "Fit-to-viewport topology should preserve the requested SVG width.");
        Assert(svg.Contains("height=\"220\"", StringComparison.Ordinal), "Fit-to-viewport topology should preserve the requested SVG height.");
        Assert(!svg.Contains("viewBox=\"0 0 320 220\"", StringComparison.Ordinal), "Fit-to-viewport topology should preserve proportional rendering by scaling the expanded viewBox, not by shrinking only geometry.");
        Assert(svg.Contains("preserveAspectRatio=\"xMinYMin meet\"", StringComparison.Ordinal), "Fit-to-viewport topology should use proportional SVG scaling.");
        Assert(svg.Contains("data-fit-content-to-viewport=\"true\"", StringComparison.Ordinal), "Fit-to-viewport mode should be visible to host validation.");
        var png = chart.ToPng(options);
        ReadPngRgba(png, out var pngWidth, out var pngHeight);
        Assert(pngWidth == 320 && pngHeight == 220, "Fit-to-viewport topology PNG should preserve the requested raster dimensions.");
    }

    private static void TopologyAutoPlacementHelpersBuildReusableRegionalLayouts() {
        var chart = TopologyChart.Create()
            .WithId("auto-regional")
            .WithViewport(820, 360, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("amer", "AMER", TopologyHealthStatus.Healthy, "47 sites", symbol: "region", color: "#16A34A")
            .AddAutoGroup("emea", "EMEA", TopologyHealthStatus.Healthy, "56 sites", symbol: "region", color: "#2563EB")
            .AddAutoGroup("apac", "APAC", TopologyHealthStatus.Critical, "39 sites", symbol: "region", color: "#8B5CF6")
            .AddAutoNode("amer-hub", "AMER Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "amer", "10.0.0.0/16", symbol: "H")
            .AddAutoNode("amer-west", "NAM West", TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "amer", "10.1.0.0/24", symbol: "S")
            .AddAutoNode("emea-hub", "EMEA Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "emea", "10.10.0.0/16", symbol: "H")
            .AddAutoNode("emea-east", "EU East", TopologyNodeKind.Branch, TopologyHealthStatus.Warning, "emea", "10.10.3.0/24", symbol: "S")
            .AddAutoNode("apac-hub", "APAC Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "apac", "10.20.0.0/16", symbol: "H")
            .AddAutoNode("anz", "ANZ", TopologyNodeKind.Branch, TopologyHealthStatus.Critical, "apac", "10.20.2.0/24", symbol: "S")
            .AddEdge("amer-emea", "amer-hub", "emea-hub", "24 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .AddEdge("emea-apac", "emea-hub", "apac-hub", "82 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);

        var options = new TopologyRenderOptions {
            IncludeLegend = false,
            IncludeTileSubtitles = true,
            NodeDisplayMode = TopologyNodeDisplayMode.Tile,
            IncludeEdgeLabelBackplates = false
        };
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-layout-mode=\"DenseGrouped\"", StringComparison.Ordinal), "Auto-placed regional builders should still use the requested deterministic layout.");
        Assert(svg.Contains("data-group-id=\"amer\"", StringComparison.Ordinal), "Auto-placed regional layouts should render the first group.");
        Assert(svg.Contains("data-node-id=\"amer-hub\"", StringComparison.Ordinal), "Auto-placed regional layouts should render grouped hub nodes.");
        Assert(svg.Contains("data-node-id=\"anz\"", StringComparison.Ordinal), "Auto-placed regional layouts should render grouped branch nodes.");
        Assert(svg.Contains("data-edge-layout-inference=\"source-port target-port\"", StringComparison.Ordinal), "Dense grouped auto placement should infer outside-facing ports for inter-group links.");
        Assert(chart.ToPng(options).Length > 64, "Auto-placed regional layouts should render as PNG.");
    }

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
            .AddEdge("amer-emea", "amer", "emea", "68 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Curved)
            .AddEdge("emea-apac", "emea", "apac", "92 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Curved);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeGroups = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-visual-style=\"MonitoringDashboard\"", StringComparison.Ordinal), "Monitoring style should be emitted as reusable SVG metadata.");
        Assert(svg.Contains("data-cfx-map-background-style=\"SoftSilhouette\"", StringComparison.Ordinal), "Monitoring geographic topology should use soft silhouettes by default.");
        Assert(svg.Contains("data-cfx-role=\"topology-geographic-land-area\"", StringComparison.Ordinal), "World geographic topology should render reusable land silhouettes.");
        Assert(svg.Contains("data-cfx-role=\"topology-geographic-region-hulls\"", StringComparison.Ordinal), "Monitoring geographic topology should render reusable regional hulls.");
        Assert(!svg.Contains("data-cfx-role=\"topology-geographic-land-dot\"", StringComparison.Ordinal), "Soft silhouette map style should not fall back to dotted land when world boundaries are available.");
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
        Assert(svg.Contains("id=\"monitoring-map-callouts-geo-callout-APAC\"", StringComparison.Ordinal), "Selected geographic groups should be prioritized when callout count is capped.");
        Assert(svg.Contains("data-callout-placement=\"right", StringComparison.Ordinal), "Monitoring geographic callouts should prefer dashboard-style map-edge placements.");
        Assert(!svg.Contains("id=\"monitoring-map-callouts-geo-callout-EMEA\"", StringComparison.Ordinal), "Non-selected middle groups should yield to selected groups when callout count is capped.");
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
        Assert(svg.Contains("data-cfx-role=\"topology-geographic-callout-mini-topology\"", StringComparison.Ordinal), "Dashboard geographic callouts should include a compact topology preview.");
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
            .AddEdge("wan", "amer-hub", "apac-hub", "142 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Curved, "backup")
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
        Assert(svg.Contains("data-cfx-role=\"topology-geographic-route-halo\"", StringComparison.Ordinal), "Monitoring geographic route arcs should render a clean underlay for dashboard map layering.");
        Assert(svg.Contains("data-cfx-role=\"topology-geographic-route-halo\" d=\"", StringComparison.Ordinal) && svg.Contains("fill=\"none\" stroke=\"#FFFFFF\"", StringComparison.Ordinal), "Geographic route halos should explicitly disable path fill so open map arcs never render as filled wedges.");
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
        var radius = GetAttribute(svg, "data-cfx-role=\"topology-geographic-region-hulls\"", "r");

        Assert(radius <= 82, "Monitoring geographic region hulls should use a tighter max radius than generic report maps.");
        Assert(svg.Contains("data-hull-padding=\"16\"", StringComparison.Ordinal), "Region hull padding should be exposed as reusable SVG metadata.");
        Assert(chart.ToPng(options).Length > 64, "Tighter monitoring region hulls should render as PNG.");
    }

    private static void TopologyMonitoringStyleCompactsHierarchyEdges() {
        var chart = TopologyChart.Create()
            .WithId("monitoring-edges")
            .WithViewport(360, 180, 20)
            .WithLegend(null)
            .AddNode("hub", "Hub", 60, 64, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 58, height: 46, symbol: "H")
            .AddNode("branch", "Branch", 230, 64, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, width: 58, height: 46, symbol: "S")
            .AddEdge("hierarchy", "hub", "branch", null, TopologyEdgeKind.Link, TopologyHealthStatus.Unknown, TopologyDirection.None, TopologyEdgeRouting.Straight)
            .WithEdgeMuted("hierarchy");

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("stroke=\"#CBD5E1\"", StringComparison.Ordinal), "Monitoring style should use a lighter hierarchy edge color.");
        Assert(svg.Contains("stroke-width=\"1.05\"", StringComparison.Ordinal), "Monitoring style should render muted hierarchy edges thinner than primary paths.");
        Assert(chart.ToPng(options).Length > 64, "Compact monitoring hierarchy edges should render as PNG.");
    }

    private static void TopologySubtleEdgesPreserveStatusColor() {
        var chart = TopologyChart.Create()
            .WithId("subtle-edge")
            .WithViewport(300, 180, 20)
            .WithLegend(null)
            .AddNode("a", "A", 60, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "B", 200, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddEdge("fan", "a", "b", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.None, TopologyEdgeRouting.Straight)
            .WithEdgeLineStyle("fan", TopologyEdgeLineStyle.Dashed)
            .WithEdgeEmphasis("fan", TopologyEdgeEmphasis.Subtle);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabels = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-edge-emphasis=\"Subtle\"", StringComparison.Ordinal), "Subtle edges should expose reusable emphasis metadata.");
        Assert(svg.Contains("stroke=\"#16A34A\"", StringComparison.Ordinal), "Subtle edges should preserve health status color instead of becoming structural gray.");
        Assert(svg.Contains("stroke-width=\"1.05\"", StringComparison.Ordinal), "Subtle monitoring edges should render with a lower visual weight.");
        Assert(svg.Contains("opacity=\"0.48\"", StringComparison.Ordinal), "Subtle monitoring edges should render below normal opacity.");
        Assert(chart.ToPng(options).Length > 64, "Subtle status-preserving edges should render as PNG.");
    }

    private static void TopologyEdgesCanBeStyledByKind() {
        var chart = TopologyChart.Create()
            .WithId("kind-edge-style")
            .WithViewport(360, 180, 20)
            .WithLegend(null)
            .AddNode("hub", "Hub", 60, 64, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 58, height: 46, symbol: "H")
            .AddNode("site", "Site", 230, 64, TopologyNodeKind.Branch, TopologyHealthStatus.Warning, width: 58, height: 46, symbol: "S")
            .AddEdge("dependency", "hub", "site", null, TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.None, TopologyEdgeRouting.Curved)
            .WithEdgesOfKind(TopologyEdgeKind.Dependency, lineStyle: TopologyEdgeLineStyle.Dashed, emphasis: TopologyEdgeEmphasis.Subtle, color: "#64748B");

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-edge-line-style=\"Dashed\"", StringComparison.Ordinal), "Bulk edge-kind styling should apply reusable line style metadata.");
        Assert(svg.Contains("data-edge-emphasis=\"Subtle\"", StringComparison.Ordinal), "Bulk edge-kind styling should apply reusable edge emphasis metadata.");
        Assert(svg.Contains("data-edge-color=\"#64748B\"", StringComparison.Ordinal), "Bulk edge-kind styling should apply reusable relationship colors.");
        Assert(svg.Contains("stroke=\"#64748B\"", StringComparison.Ordinal), "Bulk edge-kind colors should drive SVG route color.");
        Assert(chart.ToPng(options).Length > 64, "Bulk edge-kind styling should render as PNG.");

        chart.Edges[0].IsMuted = true;
        var mutedSvg = chart.ToSvg(options);
        Assert(mutedSvg.Contains("data-edge-color=\"#CBD5E1\"", StringComparison.Ordinal), "Muted monitoring edges should render neutral even when the caller supplied an explicit edge color.");
        Assert(!mutedSvg.Contains("stroke=\"#64748B\"", StringComparison.Ordinal), "Muted monitoring edges should not keep explicit relationship colors on the visible route.");
    }

    private static void TopologyEdgesRenderByVisualPriority() {
        var chart = TopologyChart.Create()
            .WithId("edge-render-priority")
            .WithViewport(360, 180, 20)
            .WithLegend(null)
            .AddNode("a", "A", 70, 70, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 58, height: 46, symbol: "H")
            .AddNode("b", "B", 230, 70, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 58, height: 46, symbol: "H")
            .AddEdge("selected-healthy", "a", "b", "selected", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Curved)
            .AddEdge("dependency-context", "a", "b", null, TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.None, TopologyEdgeRouting.Curved)
            .AddEdge("critical-route", "a", "b", "critical", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Curved)
            .WithEdgesOfKind(TopologyEdgeKind.Dependency, emphasis: TopologyEdgeEmphasis.Subtle);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle()
            .WithSelectedEdge("selected-healthy");
        var svg = chart.ToSvg(options);
        var dependencyIndex = svg.IndexOf("data-edge-id=\"dependency-context\"", StringComparison.Ordinal);
        var criticalIndex = svg.IndexOf("data-edge-id=\"critical-route\"", StringComparison.Ordinal);
        var selectedIndex = svg.IndexOf("data-edge-id=\"selected-healthy\"", StringComparison.Ordinal);

        Assert(dependencyIndex >= 0 && criticalIndex >= 0 && selectedIndex >= 0, "Priority render test should include all routes.");
        Assert(dependencyIndex < criticalIndex, "Subtle dependency context should render below critical routes.");
        Assert(criticalIndex < selectedIndex, "Selected routes should render above critical routes even when declared first.");
        Assert(svg.Contains("data-edge-render-order=\"2\"", StringComparison.Ordinal), "SVG should expose route render order metadata for host validation.");
        var criticalLabelIndex = svg.IndexOf("data-edge-id=\"critical-route\"", selectedIndex + 1, StringComparison.Ordinal);
        var selectedLabelIndex = svg.LastIndexOf("data-edge-id=\"selected-healthy\"", StringComparison.Ordinal);
        Assert(criticalLabelIndex >= 0 && selectedLabelIndex >= 0 && criticalLabelIndex < selectedLabelIndex, "Edge labels should render in the same priority order as routes.");
        Assert(svg.Contains("data-edge-label-render-order=\"2\"", StringComparison.Ordinal), "SVG should expose edge-label render order metadata for host validation.");
        Assert(chart.ToPng(options).Length > 64, "Priority-ordered edges should render as PNG.");
    }

    private static void TopologyEdgeLabelsPlaceByVisualPriority() {
        var chart = TopologyChart.Create()
            .WithId("edge-label-placement-priority")
            .WithViewport(420, 180, 20)
            .WithLegend(null)
            .AddNode("a", "A", 60, 70, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 52, height: 42, symbol: "H")
            .AddNode("b", "B", 310, 70, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 52, height: 42, symbol: "H")
            .AddEdge("context", "a", "b", "context", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("critical", "a", "b", "critical", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .WithEdgesOfKind(TopologyEdgeKind.Dependency, emphasis: TopologyEdgeEmphasis.Subtle);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = true }
            .WithMonitoringDashboardStyle();
        options.IncludeEdgeLabelBackplates = true;
        var svg = chart.ToSvg(options);
        var criticalY = GetAttribute(svg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"critical\"", "data-label-y");
        var contextY = GetAttribute(svg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"context\"", "data-label-y");

        Assert(Math.Abs(criticalY - contextY) > 20, "Priority-placed labels should keep critical and subtle context labels on separate readable lanes.");
        Assert(svg.IndexOf("data-edge-id=\"context\"", StringComparison.Ordinal) < svg.LastIndexOf("data-edge-id=\"critical\"", StringComparison.Ordinal), "Critical edge labels should render after subtle context labels.");
        Assert(chart.ToPng(options).Length > 64, "Priority-placed edge labels should render as PNG.");
    }

    private static void TopologyEdgeLabelsSupportReusableOffsets() {
        var chart = TopologyChart.Create()
            .WithId("label-offsets")
            .WithViewport(360, 200, 20)
            .WithLegend(null)
            .AddNode("source", "Source", 56, 80, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 62, height: 46, symbol: "H")
            .AddNode("target", "Target", 242, 80, TopologyNodeKind.Branch, TopologyHealthStatus.Warning, width: 62, height: 46, symbol: "S")
            .AddEdge("link", "source", "target", "82 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "MPLS")
            .WithEdgeLabelOffset("link", 24, -18);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-label-offset-x=\"24\"", StringComparison.Ordinal), "Edge label offsets should be emitted for host validation.");
        Assert(svg.Contains("data-label-offset-y=\"-18\"", StringComparison.Ordinal), "Edge label offsets should support vertical placement control.");
        Assert(svg.Contains(">82 ms<", StringComparison.Ordinal), "Offset edge labels should still render their primary text.");
        Assert(chart.ToPng(options).Length > 64, "Offset edge labels should render as PNG.");
    }

    private static void TopologyEdgeLabelsAvoidGroupHeaders() {
        var chart = TopologyChart.Create()
            .WithId("label-group-headers")
            .WithViewport(360, 220, 20)
            .WithLegend(null)
            .AddGroup("site", "HQ-NYC", 50, 48, 250, 140, TopologyHealthStatus.Healthy, "32 DCs")
            .AddNode("dc1", "DC1", 82, 72, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "site", width: 46, height: 40, symbol: "DC")
            .AddNode("dc2", "DC2", 220, 72, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "site", width: 46, height: 40, symbol: "DC")
            .AddEdge("rep", "dc1", "dc2", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "Q:312", tertiaryLabel: "7m ago");

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }.WithMonitoringDashboardStyle());
        var y = GetAttribute(svg, "data-edge-id=\"rep\"", "data-label-y");
        Assert(y > 124, "Edge labels should avoid grouped-card headers so site titles remain readable.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle()).Length > 64, "Header-aware edge labels should render as PNG.");
    }

    private static void TopologyMultilineEdgeLabelsAvoidOwnRoute() {
        var chart = TopologyChart.Create()
            .WithId("label-own-route")
            .WithViewport(420, 180, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 42, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("right", "Right", 320, 70, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddEdge("rep", "left", "right", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "Q:312", tertiaryLabel: "7m ago");

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        var y = GetAttribute(svg, "data-edge-id=\"rep\"", "data-label-y");

        Assert(Math.Abs(y - 92) > 30, "Monitoring multi-line edge labels should reserve enough clearance from their own route instead of sitting directly on the line.");
        Assert(svg.Contains("data-cfx-role=\"topology-edge-label-clearance\"", StringComparison.Ordinal), "No-plate stacked monitoring labels should still mask routes behind the text block.");
        Assert(chart.ToPng(options).Length > 64, "Own-route-aware multi-line labels should render as PNG.");
    }

    private static void TopologyTwoLineEdgeLabelsReserveRouteClearance() {
        var chart = TopologyChart.Create()
            .WithId("label-two-line-route")
            .WithViewport(420, 180, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 42, 70, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "H")
            .AddNode("right", "Right", 320, 70, TopologyNodeKind.Hub, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "H")
            .AddEdge("wan", "left", "right", "142 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "MPLS");

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        var y = GetAttribute(svg, "data-edge-id=\"wan\"", "data-label-y");

        Assert(Math.Abs(y - 92) > 30, "Two-line monitoring labels should not allow the route to pass between the primary and secondary text.");
        Assert(svg.Contains("data-cfx-role=\"topology-edge-label-clearance\"", StringComparison.Ordinal), "Two-line no-plate monitoring labels should reserve a subtle route clearance mask.");
        Assert(chart.ToPng(options).Length > 64, "Two-line route-clearance labels should render as PNG.");
    }

    private static void TopologyEdgeLabelClearanceUsesGroupSurface() {
        var chart = TopologyChart.Create()
            .WithId("label-clearance-surface")
            .WithViewport(420, 220, 20)
            .WithLegend(null)
            .AddGroup("region", "Region", 50, 44, 320, 130, TopologyHealthStatus.Healthy, "2 sites", symbol: "region")
            .AddNode("left", "Left", 92, 92, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "region", width: 44, height: 44, symbol: "H")
            .AddNode("right", "Right", 278, 92, TopologyNodeKind.Hub, TopologyHealthStatus.Warning, "region", width: 44, height: 44, symbol: "H")
            .AddEdge("wan", "left", "right", "142 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "MPLS");

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-edge-label-clearance\"", StringComparison.Ordinal), "Grouped stacked labels should still render route clearance.");
        Assert(svg.Contains("data-clearance-surface=\"group\"", StringComparison.Ordinal), "Route clearance masks inside a group should use the group surface instead of a white page patch.");
        Assert(svg.Contains("data-clearance-group-id=\"region\"", StringComparison.Ordinal), "Route clearance masks should expose the matched group for host validation.");
        Assert(chart.ToPng(options).Length > 64, "Group-surface route clearance should render as PNG.");
    }

    private static void TopologyMonitoringEdgeLabelBackplatesAreVisible() {
        var chart = TopologyChart.Create()
            .WithId("label-plates")
            .WithViewport(420, 180, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 42, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("right", "Right", 320, 70, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddEdge("rep", "left", "right", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "Q:312", tertiaryLabel: "7m ago");

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = true }
            .WithMonitoringDashboardStyle();
        options.IncludeEdgeLabelBackplates = true;
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-edge-label-backplate\"", StringComparison.Ordinal), "Monitoring edge label backplates should expose reusable SVG roles.");
        Assert(svg.Contains("stroke-opacity=\"0.72\"", StringComparison.Ordinal), "Monitoring edge label backplates should render as visible lightweight plates.");
        Assert(chart.ToPng(options).Length > 64, "Visible monitoring edge label backplates should render as PNG.");
    }

    private static void TopologyMonitoringEdgeLabelsUseTextHalos() {
        var chart = TopologyChart.Create()
            .WithId("label-halos")
            .WithViewport(360, 180, 20)
            .WithLegend(null)
            .AddNode("amer", "AMER Hub", 54, 72, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 66, height: 48, symbol: "H")
            .AddNode("emea", "EMEA Hub", 240, 72, TopologyNodeKind.Hub, TopologyHealthStatus.Warning, width: 66, height: 48, symbol: "H")
            .AddEdge("wan", "amer", "emea", "82 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "MPLS");

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-edge-label-text\"", StringComparison.Ordinal), "Monitoring edge labels should expose reusable text roles.");
        Assert(svg.Contains("data-cfx-halo=\"true\"", StringComparison.Ordinal), "Monitoring labels without backplates should render a halo for readability over links.");
        Assert(svg.Contains("paint-order=\"stroke\"", StringComparison.Ordinal), "SVG label halos should use stroke paint order instead of opaque cards.");
        Assert(chart.ToPng(options).Length > 64, "Monitoring halo edge labels should render as PNG.");
    }

    private static void TopologyMonitoringRoutesUseHalosForCrossingPaths() {
        var chart = TopologyChart.Create()
            .WithId("route-halos")
            .WithViewport(360, 220, 20)
            .WithLegend(null)
            .AddNode("a", "A", 70, 60, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "B", 246, 60, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddNode("c", "C", 70, 150, TopologyNodeKind.Server, TopologyHealthStatus.Critical, width: 44, height: 44, symbol: "DC")
            .AddNode("d", "D", 246, 150, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddEdge("a-d", "a", "d", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("c-b", "c", "b", "238 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Straight);

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-edge-route-halo\"", StringComparison.Ordinal), "Monitoring topology routes should render route halos so crossing paths remain separated.");
        Assert(chart.ToPng(options).Length > 64, "Monitoring route halos should render as PNG.");
    }

    private static void TopologyMonitoringEdgeLabelsReserveReadableGaps() {
        var chart = TopologyChart.Create()
            .WithId("label-gaps")
            .WithViewport(360, 240, 20)
            .WithLegend(null)
            .AddNode("a", "A", 64, 62, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 40, height: 40, symbol: "DC")
            .AddNode("b", "B", 256, 150, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 40, height: 40, symbol: "DC")
            .AddNode("c", "C", 64, 150, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 40, height: 40, symbol: "DC")
            .AddNode("d", "D", 256, 62, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 40, height: 40, symbol: "DC")
            .AddEdge("first", "a", "b", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("second", "c", "d", "107 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight);

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        var firstX = GetAttribute(svg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"first\"", "data-label-x");
        var firstY = GetAttribute(svg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"first\"", "data-label-y");
        var secondX = GetAttribute(svg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"second\"", "data-label-x");
        var secondY = GetAttribute(svg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"second\"", "data-label-y");
        var distance = Math.Sqrt(Math.Pow(firstX - secondX, 2) + Math.Pow(firstY - secondY, 2));
        Assert(distance >= 28, "Monitoring edge labels with the same natural midpoint should reserve a readable gap instead of stacking on top of each other.");
        Assert(chart.ToPng(options).Length > 64, "Monitoring edge label gap placement should render as PNG.");
    }

    private static void TopologyTextFitsIconRowsAndCards() {
        var chart = TopologyChart.Create()
            .WithId("text-fit")
            .WithViewport(420, 240, 20)
            .WithLegend(null)
            .AddGroup("app", "Application Tier", 48, 70, 190, 120, TopologyHealthStatus.Warning, "Services", symbol: "service")
            .AddNode("team", "Payments Team With Extra Label", 92, 126, TopologyNodeKind.Team, TopologyHealthStatus.Healthy, "app", "Owner", width: 120, height: 58, symbol: "TM");

        var options = new TopologyRenderOptions { IncludeLegend = false };
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("text-anchor=\"start\"", StringComparison.Ordinal), "Group icon/title rows should render as a measured row instead of overlapping centered text.");
        Assert(!svg.Contains(">Payments Team With Extra Label<", StringComparison.Ordinal), "Node card titles should trim to the available card width.");
        Assert(svg.Contains("Pay", StringComparison.Ordinal), "Trimmed node labels should keep their useful prefix.");
        Assert(chart.ToPng(options).Length > 64, "Icon row and fitted-card text should render as PNG.");

        var wrapped = TopologyChart.Create()
            .WithId("wrapped-text-fit")
            .WithViewport(320, 200, 20)
            .WithLegend(null)
            .AddNode("wrapped", "Authentication Relationship Service", 84, 96, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 180, height: 64, symbol: "S");
        var wrappedSvg = wrapped.ToSvg(new TopologyRenderOptions { IncludeLegend = false, WrapNodeLabels = true, MaxNodeLabelLines = 2 });
        Assert(wrappedSvg.Contains("font-size=\"12.5\"", StringComparison.Ordinal), "Wrapped node labels should fit against rendered line content instead of shrinking from the unwrapped label width.");
    }

    private static void TopologyIconLabelsRemainReusableAndFitted() {
        var chart = TopologyChart.Create()
            .WithId("icon-labels")
            .WithViewport(220, 150, 20)
            .WithLegend(null)
            .AddNode("dc1", "NYC-DC1", 60, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 46, height: 42, symbol: "DC")
            .AddNode("dc2", "LON-DC2-VeryLongName", 140, 70, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 46, height: 42, symbol: "DC")
            .AddEdge("rep", "dc1", "dc2", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight);

        var options = new TopologyRenderOptions {
            IncludeLegend = false,
            IncludeIconLabels = true,
            NodeDisplayMode = TopologyNodeDisplayMode.Icon
        };
        var svg = chart.ToSvg(options);
        Assert(svg.Contains(">NYC-DC1<", StringComparison.Ordinal), "Icon-mode labels should render when enabled.");
        Assert(svg.Contains("data-cfx-role=\"topology-node-icon-label\"", StringComparison.Ordinal), "Icon-mode labels should render with a readable label plate.");
        Assert(!svg.Contains(">LON-DC2-VeryLongName<", StringComparison.Ordinal), "Icon-mode labels should fit to their compact visual width.");
        Assert(chart.ToPng(options).Length > 64, "Icon-mode labels should render as PNG.");
    }

    private static void TopologyMonitoringStyleSupportsNeutralGroupSurfaces() {
        var chart = TopologyChart.Create()
            .WithId("neutral-groups")
            .WithViewport(360, 220, 20)
            .WithLegend(null)
            .AddGroup("site", "HQ-NYC (32 DCs)", 64, 72, 220, 92, TopologyHealthStatus.Healthy, symbol: "globe")
            .AddNode("dc1", "DC 1", 96, 118, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "site", width: 14, height: 14)
            .WithNodeDisplay("dc1", TopologyNodeDisplayMode.Dot);

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot }
            .WithMonitoringDashboardStyle()
            .WithNeutralGroupSurfaces();
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-visual-style=\"MonitoringDashboard\"", StringComparison.Ordinal), "Neutral group surfaces should compose with monitoring style.");
        Assert(svg.Contains("data-group-symbol=\"globe\"", StringComparison.Ordinal), "Neutral group surfaces should keep reusable group symbols.");
        Assert(svg.Contains("data-cfx-role=\"topology-group-status\"", StringComparison.Ordinal), "Monitoring group headers should expose compact reusable status dots.");
        Assert(svg.Contains("fill=\"#FFFFFF\" stroke=\"#16A34A\"", StringComparison.Ordinal), "Neutral group surfaces should render white cards with status-colored borders.");
        Assert(chart.ToPng(options).Length > 64, "Neutral group surfaces should render as PNG.");
    }

    private static void TopologyGroupStatusDotsUseHealthStatusColor() {
        var chart = TopologyChart.Create()
            .WithId("group-status-color")
            .WithViewport(360, 220, 20)
            .WithLegend(null)
            .AddGroup("site", "Blue Warning Site", 64, 72, 220, 92, TopologyHealthStatus.Warning, symbol: "globe", color: "#2563EB")
            .AddNode("dc1", "DC 1", 96, 118, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "site", width: 14, height: 14)
            .WithNodeDisplay("dc1", TopologyNodeDisplayMode.Dot);

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot }
            .WithMonitoringDashboardStyle()
            .WithNeutralGroupSurfaces();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-group-status\"", StringComparison.Ordinal), "Monitoring group headers should expose a reusable status dot.");
        Assert(svg.Contains("data-cfx-status=\"Warning\"", StringComparison.Ordinal), "The compact group status dot should preserve health state metadata.");
        Assert(svg.Contains("fill=\"#F97316\"", StringComparison.Ordinal), "Group status dots should use the health status color, not the group accent color.");
        Assert(chart.ToPng(options).Length > 64, "Group status dots should render as PNG.");
    }

    private static void TopologyGroupHeadersReserveIconAndStatusSpace() {
        const string longTitle = "Application Tier With Extremely Long Service Owner Title";
        var chart = TopologyChart.Create()
            .WithId("group-header-fit")
            .WithViewport(360, 220, 20)
            .WithLegend(null)
            .AddGroup("app", longTitle, 52, 72, 260, 92, TopologyHealthStatus.Critical, subtitle: "Services", symbol: "application", color: "#F97316")
            .AddNode("svc", "Payments Team", 112, 126, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, "app", width: 76, height: 44, symbol: "TM");

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithMonitoringDashboardStyle()
            .WithNeutralGroupSurfaces();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-group-status\"", StringComparison.Ordinal), "Monitoring group headers should keep the right-side status marker.");
        Assert(!svg.Contains($">{longTitle}<", StringComparison.Ordinal), "Group header labels should be fitted before they can collide with the symbol or status marker.");
        Assert(svg.Contains("Application", StringComparison.Ordinal), "Fitted group headers should keep the meaningful start of the label.");
        Assert(chart.ToPng(options).Length > 64, "Fitted group headers should render as PNG.");
    }

    private static void TopologyGroupHeadersFitMediumTitlesBeforeTrimming() {
        const string mediumTitle = "SFO-SanFrancisco (11 DCs)";
        var chart = TopologyChart.Create()
            .WithId("group-header-medium-fit")
            .WithViewport(520, 220, 20)
            .WithLegend(null)
            .AddGroup("sfo", mediumTitle, 72, 72, 330, 100, TopologyHealthStatus.Healthy, symbol: "globe")
            .AddNode("dc1", "DC 1", 112, 124, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "sfo", width: 18, height: 18)
            .WithNodeDisplay("dc1", TopologyNodeDisplayMode.Dot);

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot }
            .WithMonitoringDashboardStyle()
            .WithNeutralGroupSurfaces();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains($">{mediumTitle}<", StringComparison.Ordinal), "Medium-length monitoring group titles should fit by reducing font size before trimming.");
        Assert(chart.ToPng(options).Length > 64, "Medium fitted group headers should render as PNG.");
    }

    private static void TopologyMonitoringDotNodesRenderCompactSymbols() {
        var chart = TopologyChart.Create()
            .WithId("dot-symbols")
            .WithViewport(240, 160, 20)
            .WithLegend(null)
            .AddNode("dc1", "DC 1", 90, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 18, height: 18, symbol: "DC")
            .WithNodeDisplay("dc1", TopologyNodeDisplayMode.Dot);

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-node-dot-symbol\"", StringComparison.Ordinal), "Monitoring dot nodes should render compact reusable symbols when a node has a symbol.");
        Assert(chart.ToPng(options).Length > 64, "Monitoring dot node symbols should render as PNG.");
    }

    private static void TopologyHiddenNodesWorkAsReusableEdgeAnchors() {
        var chart = TopologyChart.Create()
            .WithId("hidden-anchors")
            .WithViewport(320, 160, 20)
            .WithLegend(null)
            .AddNode("source", "Source", 42, 64, TopologyNodeKind.Cloud, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "CL")
            .AddNode("anchor", "Group Edge Anchor", 238, 84, TopologyNodeKind.Generic, TopologyHealthStatus.Unknown, width: 1, height: 1)
            .AddEdge("source-anchor", "source", "anchor", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .WithNodeDisplay("source", TopologyNodeDisplayMode.Icon)
            .WithNodeDisplay("anchor", TopologyNodeDisplayMode.Hidden);

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-edge-id=\"source-anchor\"", StringComparison.Ordinal), "Hidden anchors should still be usable by edges.");
        Assert(!svg.Contains("data-node-id=\"anchor\"", StringComparison.Ordinal), "Hidden anchors should not render visible node markup.");
        Assert(chart.ToPng(options).Length > 64, "Hidden edge anchors should render as PNG without visible node artifacts.");
    }

    private static void TopologySharedPortsSpreadFanRoutes() {
        var chart = TopologyChart.Create()
            .WithId("port-fan")
            .WithViewport(360, 220, 20)
            .WithLegend(null)
            .AddNode("hub", "Inter-Forest", 88, 86, TopologyNodeKind.Cloud, TopologyHealthStatus.Healthy, width: 56, height: 56, symbol: "CL")
            .AddNode("a", "Site A", 244, 42, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "Site B", 244, 92, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddNode("c", "Site C", 244, 142, TopologyNodeKind.Server, TopologyHealthStatus.Critical, width: 44, height: 44, symbol: "DC")
            .AddEdge("hub-a", "hub", "a", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("hub-b", "hub", "b", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("hub-c", "hub", "c", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .WithEdgePorts("hub-a", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("hub-b", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("hub-c", TopologyEdgePort.Right, TopologyEdgePort.Left);

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        var firstY = GetAttribute(svg, "data-edge-id=\"hub-a\"", "data-route-start-y");
        var secondY = GetAttribute(svg, "data-edge-id=\"hub-b\"", "data-route-start-y");
        var thirdY = GetAttribute(svg, "data-edge-id=\"hub-c\"", "data-route-start-y");
        Assert(firstY < secondY && secondY < thirdY, "Edges sharing one explicit hub port should spread along that side instead of all starting from the same midpoint.");
        Assert(chart.ToPng(options).Length > 64, "Port-spread fan routes should render as PNG.");
    }

    private static void TopologyOrthogonalReciprocalRoutesKeepPortAnchors() {
        var chart = TopologyChart.Create()
            .WithId("reciprocal-orthogonal")
            .WithViewport(360, 260, 20)
            .WithLegend(null)
            .AddNode("top", "Top", 150, 56, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 56, height: 44, symbol: "DC")
            .AddNode("bottom", "Bottom", 150, 176, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 56, height: 44, symbol: "DC")
            .AddEdge("top-bottom", "top", "bottom", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("bottom-top", "bottom", "top", "112 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgePorts("top-bottom", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgePorts("bottom-top", TopologyEdgePort.Top, TopologyEdgePort.Bottom)
            .WithEdgeRouteLane("top-bottom", -18)
            .WithEdgeRouteLane("bottom-top", 18);

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        var downStartY = GetAttribute(svg, "data-edge-id=\"top-bottom\"", "data-route-start-y");
        var upStartY = GetAttribute(svg, "data-edge-id=\"bottom-top\"", "data-route-start-y");
        Assert(Math.Abs(downStartY - (56 + 44 + 7)) < 0.01, "Orthogonal reciprocal routes should keep source endpoints anchored to explicit ports.");
        Assert(Math.Abs(upStartY - (176 - 7)) < 0.01, "Orthogonal reciprocal routes should not receive a whole-route parallel offset that pulls them off their ports.");
        Assert(chart.ToPng(options).Length > 64, "Port-anchored reciprocal orthogonal routes should render as PNG.");
    }

    private static void TopologyEdgeRouteBundlesAssignCenteredLanes() {
        var chart = TopologyChart.Create()
            .WithId("route-bundle")
            .WithViewport(380, 260, 20)
            .WithLegend(null)
            .AddNode("a", "A", 80, 66, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "B", 256, 66, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("c", "C", 168, 176, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddEdge("a-c", "a", "c", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("b-c", "b", "c", "112 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("c-a", "c", "a", "107 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgePorts("a-c", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgePorts("b-c", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgePorts("c-a", TopologyEdgePort.Top, TopologyEdgePort.Bottom)
            .WithEdgeRouteBundle(0, 18, "a-c", "b-c", "c-a");

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle());

        Assert(svg.Contains("data-edge-id=\"a-c\"", StringComparison.Ordinal), "Bundled route lanes should preserve the first edge.");
        Assert(svg.Contains("data-route-lane=\"-18\"", StringComparison.Ordinal), "Centered route bundles should assign the first lane below center.");
        Assert(svg.Contains("data-route-lane=\"0\"", StringComparison.Ordinal), "Centered route bundles should assign the middle lane at the center.");
        Assert(svg.Contains("data-route-lane=\"18\"", StringComparison.Ordinal), "Centered route bundles should assign the last lane above center.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle()).Length > 64, "Bundled route lanes should render as PNG.");
    }

    private static void TopologyReciprocalEdgeRouteBundlesInferCenteredLanes() {
        var chart = TopologyChart.Create()
            .WithId("reciprocal-route-bundle")
            .WithViewport(340, 220, 20)
            .WithLegend(null)
            .AddNode("a", "A", 82, 86, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "B", 228, 86, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddEdge("a-b", "a", "b", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("b-a", "b", "a", "107 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgePorts("a-b", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("b-a", TopologyEdgePort.Left, TopologyEdgePort.Right)
            .WithReciprocalEdgeRouteBundles(20);

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle());

        Assert(svg.Contains("data-route-lane=\"-10\"", StringComparison.Ordinal), "Reciprocal route bundling should assign the first unconfigured edge below center.");
        Assert(svg.Contains("data-route-lane=\"10\"", StringComparison.Ordinal), "Reciprocal route bundling should assign the second unconfigured edge above center.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle()).Length > 64, "Inferred reciprocal bundles should render as PNG.");
    }
}
