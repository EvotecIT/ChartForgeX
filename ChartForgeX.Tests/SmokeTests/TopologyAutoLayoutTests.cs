using System;
using System.Linq;
using System.Xml.Linq;
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
        Assert(!svg.Contains("data-cfx-role=\"legend-entry\"", StringComparison.Ordinal), "Hidden legends should not reserve layout space or render.");
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
            .AddEdge("a-c", "a", "c", "188 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "MPLS");

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle()
            .WithFitContentToViewport();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("width=\"320\"", StringComparison.Ordinal), "Fit-to-viewport topology should preserve the requested SVG width.");
        Assert(svg.Contains("height=\"220\"", StringComparison.Ordinal), "Fit-to-viewport topology should preserve the requested SVG height.");
        Assert(svg.Contains("viewBox=\"0 0 320 220\"", StringComparison.Ordinal), "Fitted SVG and PNG should share the exact authored logical viewport.");
        var envelope = chart.Prepare(options).ToInterchangeEnvelope();
        Assert(envelope.Nodes.All(node => node.X >= 20 && node.Y >= 20 && node.X + node.Width <= 300 && node.Y + node.Height <= 200), "Every fitted node should remain inside the common content padding.");
        var scale = envelope.Nodes[0].Width!.Value / chart.Nodes[0].Width;
        Assert(scale > 0 && scale < 1 && envelope.Nodes.All(node => Math.Abs(node.Width!.Value / chart.Nodes.Single(source => source.Id == node.Id).Width - scale) < .0001), "Fitting should uniformly scale every resolved node.");
        var label = XDocument.Parse(svg).Descendants().First(element => element.Name.LocalName == "text" && element.Ancestors().Any(parent => (string?)parent.Attribute("data-cfx-role") == "topology-node-label"));
        Assert((double?)label.Attribute("font-size") < 11, "Native fitted text should scale together with geometry.");
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
            .AddEdge("amer-emea", "amer-hub", "emea-hub", "24 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, VisualLinkDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .AddEdge("emea-apac", "emea-hub", "apac-hub", "82 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, VisualLinkDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);

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

}
