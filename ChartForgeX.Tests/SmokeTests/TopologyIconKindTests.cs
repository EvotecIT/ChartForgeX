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
        Assert(chart.ToPng(new TopologyRenderOptions { IconCatalog = catalog, IncludeLegend = false }).Length > 64, "Bulk node-kind icons should render as PNG.");
    }
}
