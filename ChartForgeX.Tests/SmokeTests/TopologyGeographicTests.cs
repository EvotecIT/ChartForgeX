using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
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
            .AddEdge("nyc-lon", "nyc", "lon", "72 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, VisualLinkDirection.Bidirectional, TopologyEdgeRouting.Curved)
            .AddEdge("lon-sin", "lon", "sin", "165 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, VisualLinkDirection.Bidirectional, TopologyEdgeRouting.Curved)
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
        Assert(svg.Contains("data-cfx-role=\"topology-map-land\"", StringComparison.Ordinal), "Geographic topology should render a land-dot background layer.");
        Assert(svg.Contains("data-route-curve=\"geographic\"", StringComparison.Ordinal), "Geographic curved topology links should expose map-arc route diagnostics.");
        Assert(svg.Contains("data-route-control-x=", StringComparison.Ordinal) && svg.Contains("data-route-control-y=", StringComparison.Ordinal), "Geographic curved topology links should expose their map-arc control point.");
        var arc = chart.Prepare(options).ToInterchangeEnvelope().Edges.Single(edge => edge.Id == "nyc-lon").ResolvedRoute;
        var start = arc.First(); var end = arc.Last();
        Assert(arc.Count > 3 && arc.Skip(1).Take(arc.Count - 2).Any(point => Math.Abs((end.X - start.X) * (point.Y - start.Y) - (end.Y - start.Y) * (point.X - start.X)) > 1), "Geographic links should retain a sampled curved map arc shared by SVG, PNG and semantic interchange.");
        var nyc = TopologyEntity(svg, "node", "nyc");
        Assert((string?)nyc.Attribute("data-node-kind") == "Location" && (string?)nyc.Attribute("data-node-display-mode") == "Tile" && (string?)nyc.Attribute("data-cfx-status") == "Healthy"
            && (string?)nyc.Attribute("data-cfx-selected") == "false" && (string?)nyc.Attribute("data-node-geo-visible") == "true"
            && (double?)nyc.Attribute("data-node-longitude") == -74.006 && (double?)nyc.Attribute("data-node-latitude") == 40.7128, "Geographic topology should preserve full source coordinates and projected visibility metadata.");
        Assert(svg.Contains("data-node-id=\"south\"", StringComparison.Ordinal) && svg.Contains("data-node-geo-visible=\"false\"", StringComparison.Ordinal), "Geographic topology should mark clamped out-of-viewport coordinates.");
        var amer = XDocument.Parse(svg).Descendants().First(element => (string?)element.Attribute("data-cfx-role") == "topology-group" && (string?)element.Attribute("data-group-id") == "amer");
        Assert((double?)amer.Attribute("data-group-longitude") == -98.5795 && (double?)amer.Attribute("data-group-latitude") == 39.8283
            && (string?)amer.Attribute("data-group-geo-visible") == "true", "Geographic topology should preserve full group coordinates and projected visibility metadata.");
        Assert(svg.Contains("data-cfx-metric-latency-p95=\"72 ms\"", StringComparison.Ordinal), "Geographic topology should preserve node metrics for host inspectors.");
        Assert(svg.Contains("data-cfx-visual-role=\"topology-geographic-callout\"", StringComparison.Ordinal), "Geographic topology should render opt-in region callouts.");
        Assert(svg.Contains("data-callout-node-count=\"2\"", StringComparison.Ordinal), "Geographic callouts should expose grouped node counts.");
        Assert(svg.Contains("data-callout-critical-count=\"1\"", StringComparison.Ordinal), "Geographic callouts should expose status counts.");
        Assert(svg.Contains("data-cfx-role=\"topology-callout-status\"", StringComparison.Ordinal), "Geographic callouts should render status chips.");

        var html = chart.ToInteractiveHtmlPage(new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeGeographicCallouts = true });
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
        Assert(europeSvg.Contains("data-cfx-role=\"topology-map-boundary\"", StringComparison.Ordinal), "Regional geographic topology should render filled land areas.");
        var regionalBoundaries = XDocument.Parse(europeSvg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-map-boundary").ToArray();
        Assert(regionalBoundaries.Any(element => element.Attribute("d") != null && (double?)element.Attribute("stroke-width") > 0
            && element.RenderedColor("stroke").A > 0), "Regional geographic topology should paint visible native boundary outlines.");
        Assert(XDocument.Parse(europeSvg).Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "topology-map-land"
            && element.Name.LocalName == "ellipse" && element.RenderedColor("fill").A > 0),
            "Dotted regional maps should paint their land dots alongside the boundary outlines.");
        var silhouetteOptions = options.Clone(); silhouetteOptions.MapBackgroundStyle = TopologyMapBackgroundStyle.SoftSilhouette;
        var silhouetteSvg = europe.ToSvg(silhouetteOptions);
        Assert(XDocument.Parse(silhouetteSvg).Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "topology-map-boundary"
            && (string?)element.Attribute("fill") != "none" && element.RenderedColor("fill").A > 0 && element.RenderedColor("stroke").A > 0),
            "Soft regional maps should paint both land fill and visible outlines from the same native boundary geometry.");

        var invalid = TopologyChart.Create()
            .AddNode("partial", "Partial", 0, 0);
        invalid.Nodes[0].Latitude = 12;
        var validation = new TopologyChartValidator().Validate(invalid);
        Assert(validation.Errors.Any(error => error.Code == "node-geo-coordinate-pair"), "Topology validator should reject partial geographic coordinates.");
    }

    private static void TopologyGeographicCalloutsIgnoreHiddenNodesAndDisabledLabels() {
        var chart = TopologyChart.Create()
            .WithId("geo-callout-hidden")
            .WithViewport(640, 340, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .AddGroup("amer", "AMER", 0, 0, 0, 0, TopologyHealthStatus.Healthy, "1 site", symbol: "region")
            .AddNode("visible", "Visible Site", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Healthy, "amer", width: 58, height: 42, symbol: "S")
            .AddNode("anchor", "Route Anchor", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Critical, "amer", width: 24, height: 24, symbol: "A")
            .AddNode("remote", "Remote", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Warning, width: 58, height: 42, symbol: "R")
            .AddEdge("visible-remote", "visible", "remote", "142 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, VisualLinkDirection.Bidirectional, TopologyEdgeRouting.Curved)
            .WithGroupCoordinates("amer", -98.5795, 39.8283)
            .WithNodeCoordinates("visible", -96.797, 32.7767)
            .WithNodeCoordinates("anchor", -92.0, 34.5)
            .WithNodeCoordinates("remote", -0.1276, 51.5072)
            .WithNodeDisplay("anchor", TopologyNodeDisplayMode.Hidden);

        var options = new TopologyRenderOptions {
            IncludeLegend = false,
            IncludeEdgeLabels = false,
            IncludeGeographicCallouts = true,
            NodeDisplayMode = TopologyNodeDisplayMode.Tile
        };
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-visual-role=\"topology-geographic-callout\"", StringComparison.Ordinal), "Geographic callouts should still render when edge labels are disabled.");
        Assert(!svg.Contains("data-cfx-role=\"topology-edge-label\"", StringComparison.Ordinal), "Disabled edge labels should not render visible label markup.");
        Assert(svg.Contains("data-callout-node-count=\"1\"", StringComparison.Ordinal), "Hidden geographic anchor nodes should not inflate callout node totals.");
        Assert(svg.Contains("data-callout-critical-count=\"0\"", StringComparison.Ordinal), "Hidden geographic anchor nodes should not skew callout health totals.");

        var callouts = TopologyGeographicCallouts.Build(chart, options, TopologyTheme.Light());
        Assert(callouts.Count == 1 && callouts[0].NodeCount == 1 && callouts[0].CriticalCount == 0, "Geographic callout models should count rendered group members only.");
    }
}
