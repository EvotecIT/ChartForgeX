using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyGeographicLayoutProjectsCoordinates() {
        var chart = TopologyChart.Create()
            .WithId("geo-topology")
            .WithViewport(820, 420, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .AddGroup("amer", "AMER", 0, 0, 0, 0, TopologyHealthStatus.Healthy, "2 sites", symbol: "region")
            .AddGroup("emea", "EMEA", 0, 0, 0, 0, TopologyHealthStatus.Warning, "1 site", symbol: "region")
            .AddNode("nyc", "New York", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Healthy, "amer", width: 64, height: 46, symbol: "NY")
            .AddNode("chi", "Chicago", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Critical, "amer", width: 64, height: 46, symbol: "CHI")
            .AddNode("lon", "London", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Healthy, "emea", width: 64, height: 46, symbol: "LDN")
            .AddNode("sin", "Singapore", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Warning, width: 64, height: 46, symbol: "SIN")
            .AddNode("south", "South Pole", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Unknown, width: 64, height: 46, symbol: "SP")
            .AddEdge("nyc-lon", "nyc", "lon", "72 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Curved)
            .AddEdge("lon-sin", "lon", "sin", "165 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Curved)
            .WithGroupCoordinates("amer", -98.5795, 39.8283)
            .WithGroupCoordinates("emea", 12.4964, 41.9028)
            .WithNodeCoordinates("nyc", -74.006, 40.7128)
            .WithNodeCoordinates("chi", -87.6298, 41.8781)
            .WithNodeCoordinates("lon", -0.1276, 51.5072)
            .WithNodeCoordinates("sin", 103.8198, 1.3521)
            .WithNodeCoordinates("south", 0, -80);

        chart.Nodes[0].Metrics["latency.p95"] = "72 ms";
        chart.Groups[0].Metadata["calloutSubtitle"] = "2 sites / 1 critical";
        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeGeographicCallouts = true };
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-layout-mode=\"Geographic\"", StringComparison.Ordinal), "Geographic topology should expose the layout mode.");
        Assert(svg.Contains("data-cfx-projection=\"equirectangular\"", StringComparison.Ordinal), "Geographic topology should expose the projection.");
        Assert(svg.Contains("data-cfx-viewport=\"World\"", StringComparison.Ordinal), "Geographic topology should expose the map viewport.");
        Assert(svg.Contains("data-cfx-role=\"topology-geographic-frame\"", StringComparison.Ordinal), "Geographic topology should render a map frame.");
        Assert(svg.Contains("data-cfx-role=\"topology-geographic-graticule\"", StringComparison.Ordinal), "Geographic topology should render graticule lines.");
        Assert(svg.Contains("data-cfx-role=\"topology-geographic-land-dot\"", StringComparison.Ordinal), "Geographic topology should render a land-dot background layer.");
        Assert(svg.Contains("data-route-curve=\"geographic\"", StringComparison.Ordinal), "Geographic curved topology links should expose map-arc route diagnostics.");
        Assert(svg.Contains("data-route-control-x=", StringComparison.Ordinal) && svg.Contains("data-route-control-y=", StringComparison.Ordinal), "Geographic curved topology links should expose their map-arc control point.");
        Assert(svg.Contains(" Q ", StringComparison.Ordinal), "Geographic curved topology links should render as quadratic map arcs in SVG.");
        Assert(svg.Contains("data-node-id=\"nyc\" data-node-kind=\"Location\" data-node-display-mode=\"Tile\" data-cfx-status=\"Healthy\" data-cfx-selected=\"false\" data-node-longitude=\"-74.006\" data-node-latitude=\"40.713\" data-node-geo-visible=\"true\"", StringComparison.Ordinal), "Geographic topology should expose projected node coordinates.");
        Assert(svg.Contains("data-node-id=\"south\"", StringComparison.Ordinal) && svg.Contains("data-node-geo-visible=\"false\"", StringComparison.Ordinal), "Geographic topology should mark clamped out-of-viewport coordinates.");
        Assert(svg.Contains("data-group-id=\"amer\" data-group-layout-policy=\"Auto\" data-group-applied-layout-policy=\"Auto\" data-cfx-status=\"Healthy\" data-cfx-selected=\"false\" data-group-longitude=\"-98.58\" data-group-latitude=\"39.828\" data-group-geo-visible=\"true\"", StringComparison.Ordinal), "Geographic topology should expose group coordinates.");
        Assert(svg.Contains("data-cfx-metric-latency-p95=\"72 ms\"", StringComparison.Ordinal), "Geographic topology should preserve node metrics for host inspectors.");
        Assert(svg.Contains("data-cfx-visual-role=\"topology-geographic-callout\"", StringComparison.Ordinal), "Geographic topology should render opt-in region callouts.");
        Assert(svg.Contains("data-callout-node-count=\"2\"", StringComparison.Ordinal), "Geographic callouts should expose grouped node counts.");
        Assert(svg.Contains("data-callout-critical-count=\"1\"", StringComparison.Ordinal), "Geographic callouts should expose status counts.");
        Assert(svg.Contains("data-cfx-role=\"topology-geographic-callout-status\"", StringComparison.Ordinal), "Geographic callouts should render status chips.");

        var html = chart.ToHtmlPage(options);
        Assert(html.Contains("longitude: attr(element, 'data-node-longitude')", StringComparison.Ordinal), "Topology HTML selection details should expose node longitude.");
        Assert(html.Contains("geoVisible: attr(element, 'data-group-geo-visible')", StringComparison.Ordinal), "Topology HTML selection details should expose group geographic visibility.");
        Assert(html.Contains("nodeCount: attr(element, 'data-callout-node-count')", StringComparison.Ordinal), "Topology HTML selection details should expose callout counts.");
        Assert(chart.ToPng(options).Length > 64, "Geographic topology should render as PNG.");

        var europe = TopologyChart.Create()
            .WithId("geo-europe")
            .WithViewport(520, 320, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.Europe())
            .AddNode("warsaw", "Warsaw", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Healthy, width: 54, height: 40)
            .WithNodeCoordinates("warsaw", 21.0122, 52.2297);
        var europeSvg = europe.ToSvg(options);
        Assert(europeSvg.Contains("data-cfx-role=\"topology-geographic-land-area\"", StringComparison.Ordinal), "Regional geographic topology should render filled land areas.");
        Assert(europeSvg.Contains("data-cfx-role=\"topology-geographic-boundary\"", StringComparison.Ordinal), "Regional geographic topology should render boundary outlines.");

        var invalid = TopologyChart.Create()
            .AddNode("partial", "Partial", 0, 0);
        invalid.Nodes[0].Latitude = 12;
        var validation = new TopologyChartValidator().Validate(invalid);
        Assert(validation.Errors.Any(error => error.Code == "node-geo-coordinate-pair"), "Topology validator should reject partial geographic coordinates.");
    }
}
