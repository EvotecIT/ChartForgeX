using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyNodesCanApplyIconsByKind() {
        var catalog = TopologyIconCatalog.Default();
        var chart = TopologyChart.Create()
            .WithId("kind-icons")
            .WithViewport(520, 300, 24)
            .WithLegend(null)
            .AddNode("cert-a", "Certificate A", 72, 120, TopologyNodeKind.Certificate, TopologyHealthStatus.Healthy)
            .AddNode("cert-b", "Certificate B", 250, 120, TopologyNodeKind.Certificate, TopologyHealthStatus.Warning, symbol: "CRT", color: "#1D4ED8")
            .AddNode("owner", "Owner", 250, 220, TopologyNodeKind.Person, TopologyHealthStatus.Healthy)
            .AddEdge("cert-owner", "cert-a", "owner", "owned by", TopologyEdgeKind.Ownership, TopologyHealthStatus.Healthy, TopologyDirection.Forward)
            .WithNodesOfKindIcon(TopologyNodeKind.Certificate, "common:certificate", catalog);

        Assert(chart.Nodes[0].Symbol == "TLS", "Bulk node-kind icon styling should fill missing node symbols from the icon.");
        Assert(chart.Nodes[1].Symbol == "CRT", "Bulk node-kind icon styling should preserve explicit node symbols.");
        var svg = chart.ToSvg(new TopologyRenderOptions { IconCatalog = catalog, IncludeLegend = false });
        Assert(svg.Contains("data-node-icon-id=\"common:certificate\"", StringComparison.Ordinal), "Bulk node-kind icon styling should apply reusable icon ids.");
        Assert(svg.Contains("data-node-icon-pack=\"common\"", StringComparison.Ordinal), "Bulk node-kind icon styling should expose icon pack metadata.");
        Assert(svg.Contains("data-node-icon-label=\"Certificate\"", StringComparison.Ordinal), "Bulk node-kind icon styling should expose icon label metadata.");
        Assert(svg.Contains("data-node-color=\"#1D4ED8\"", StringComparison.Ordinal), "Bulk node-kind icon styling should preserve explicit node colors.");
        var legendSvg = chart.ToSvg(new TopologyRenderOptions { IconCatalog = catalog, LegendMode = TopologyLegendMode.Auto });
        var legendStart = legendSvg.IndexOf("data-cfx-role=\"topology-legend\"", StringComparison.Ordinal);
        Assert(legendStart >= 0, "Topology auto legends should render for bulk icon-styled nodes.");
        var legend = legendSvg.Substring(legendStart);
        Assert(legend.Contains("data-legend-icon-id=\"common:certificate\"", StringComparison.Ordinal), "Topology auto legends should infer shared node-kind icon ids.");
        Assert(legend.Contains("data-legend-icon-shape=\"Certificate\"", StringComparison.Ordinal), "Topology auto legends should render the inferred icon shape.");
        Assert(legend.Contains(">TLS Certificate<", StringComparison.Ordinal), "Topology auto legends should keep the inferred icon symbol in the node-kind label.");
        Assert(chart.ToPng(new TopologyRenderOptions { IconCatalog = catalog, IncludeLegend = false }).Length > 64, "Bulk node-kind icons should render as PNG.");

        var generic = TopologyChart.Create()
            .WithId("kind-icon-preserve-kind")
            .WithViewport(260, 180, 24)
            .WithLegend(null)
            .AddNode("generic-owner", "Owner", 70, 80, TopologyNodeKind.Generic, TopologyHealthStatus.Healthy)
            .WithNodesOfKindIcon(TopologyNodeKind.Generic, "people:owner", catalog);
        Assert(generic.Nodes[0].Kind == TopologyNodeKind.Generic, "Bulk node-kind icon styling should preserve the matched node kind instead of reclassifying by icon catalog metadata.");
        Assert(generic.Nodes[0].IconId == "people:owner", "Bulk node-kind icon styling should still apply the requested icon id.");
    }

    private static void TopologyNodesCanApplyCombinedKindStyle() {
        var catalog = TopologyIconCatalog.Default();
        var chart = TopologyChart.Create()
            .WithId("combined-kind-style")
            .WithViewport(520, 300, 24)
            .AddNode("owner-a", "Owner A", 72, 120, TopologyNodeKind.Generic, TopologyHealthStatus.Healthy)
            .AddNode("owner-b", "Owner B", 250, 120, TopologyNodeKind.Generic, TopologyHealthStatus.Warning)
            .WithNodesOfKindStyle(TopologyNodeKind.Generic, color: "#7C3AED", backgroundColor: "#F5F3FF", iconId: "people:owner", catalog: catalog);

        Assert(chart.Nodes[0].Kind == TopologyNodeKind.Person, "Combined node-kind styling should apply the icon's generic node kind after matching the original kind.");
        Assert(chart.Nodes[0].Color == "#7C3AED", "Combined node-kind styling should keep caller accent colors ahead of icon fallback colors.");
        Assert(chart.Nodes[0].BackgroundColor == "#F5F3FF", "Combined node-kind styling should apply caller node backgrounds.");
        Assert(chart.Nodes[0].IconId == "people:owner", "Combined node-kind styling should apply reusable icon ids.");
        Assert(chart.Nodes[0].Symbol == "OWN", "Combined node-kind styling should fill missing symbols from the icon.");
        var svg = chart.ToSvg(new TopologyRenderOptions { IconCatalog = catalog, LegendMode = TopologyLegendMode.Auto });
        Assert(svg.Contains("data-node-kind=\"Person\"", StringComparison.Ordinal), "Combined node-kind styling should expose the icon node kind in SVG metadata.");
        Assert(svg.Contains("data-node-color=\"#7C3AED\"", StringComparison.Ordinal), "Combined node-kind styling should expose caller accent colors in SVG metadata.");
        Assert(svg.Contains("data-node-background-color=\"#F5F3FF\"", StringComparison.Ordinal), "Combined node-kind styling should expose caller backgrounds in SVG metadata.");
        var legend = svg.Substring(svg.IndexOf("data-cfx-role=\"topology-legend\"", StringComparison.Ordinal));
        Assert(legend.Contains("data-legend-icon-id=\"people:owner\"", StringComparison.Ordinal), "Combined node-kind styling should flow into auto legend icon markers.");
        Assert(legend.Contains("fill=\"#F5F3FF\"", StringComparison.Ordinal), "Combined node-kind styling should flow into auto legend backgrounds.");
        Assert(chart.ToPng(new TopologyRenderOptions { IconCatalog = catalog, LegendMode = TopologyLegendMode.Auto }).Length > 64, "Combined node-kind styling should render as PNG.");
    }
}
