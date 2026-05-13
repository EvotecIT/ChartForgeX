using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyNodesCanBeStyledByKindAndReflectedInLegends() {
        var chart = TopologyChart.Create()
            .WithId("styled-node-kind-map")
            .WithViewport(560, 340, 24)
            .AddNode("cert-a", "Certificate A", 90, 120, TopologyNodeKind.Certificate, TopologyHealthStatus.Healthy, symbol: "TLS")
            .AddNode("cert-b", "Certificate B", 300, 120, TopologyNodeKind.Certificate, TopologyHealthStatus.Warning, symbol: "TLS")
            .AddNode("risk", "Risk", 300, 220, TopologyNodeKind.Process, TopologyHealthStatus.Critical, symbol: "!")
            .AddEdge("cert-risk", "cert-a", "risk", "flags", TopologyEdgeKind.Mapping, TopologyHealthStatus.Critical, TopologyDirection.Forward)
            .WithNodesOfKind(TopologyNodeKind.Certificate, color: "#2563EB", backgroundColor: "#EFF6FF");

        var svg = chart.ToSvg(new TopologyRenderOptions { LegendMode = TopologyLegendMode.Auto });
        Assert(svg.Contains("data-node-color=\"#2563EB\"", StringComparison.Ordinal), "Bulk node-kind styling should apply reusable node accent colors.");
        Assert(svg.Contains("data-node-background-color=\"#EFF6FF\"", StringComparison.Ordinal), "Bulk node-kind styling should apply reusable node backgrounds.");
        var legendStart = svg.IndexOf("data-cfx-role=\"topology-legend\"", StringComparison.Ordinal);
        Assert(legendStart >= 0, "Topology auto legend should render for styled node kinds.");
        var legend = svg.Substring(legendStart);
        Assert(TopologyRenderPrimitives.LegendColumnCount(TopologyLegend.Infer(chart), 512) == 2, "Topology legends should reduce column count when the available width would crowd markers and labels.");
        Assert(TopologyRenderPrimitives.LegendColumnWidth(TopologyRenderPrimitives.LegendMaxWidth, TopologyRenderPrimitives.LegendColumns) >= 200, "Wide topology legends should keep enough room between icon markers and neighboring labels.");
        Assert(legend.Contains("data-legend-column-width=\"", StringComparison.Ordinal), "Topology SVG legends should expose chosen column spacing for host diagnostics.");
        Assert(legend.Contains("width=\"22\" height=\"22\"", StringComparison.Ordinal), "Topology legend node markers should use the shared card-icon footprint.");
        Assert(legend.Contains(">TLS Certificate<", StringComparison.Ordinal), "Topology auto legend should include the styled node symbol and kind.");
        Assert(legend.Contains("stroke=\"#2563EB\"", StringComparison.Ordinal), "Topology auto legend should reuse a shared node-kind accent color.");
        Assert(legend.Contains("fill=\"#EFF6FF\"", StringComparison.Ordinal), "Topology auto legend should reuse a shared node-kind background color.");
        Assert(chart.ToPng(new TopologyRenderOptions { LegendMode = TopologyLegendMode.Auto }).Length > 64, "Styled node-kind legends should render as PNG.");
    }

    private static void TopologyInferredLegendsUseSharedEdgeKindStyling() {
        var chart = TopologyChart.Create()
            .WithId("styled-relationship-map")
            .WithTitle("Styled Relationship Map")
            .WithViewport(560, 340, 24)
            .AddNode("app", "Application", 80, 110, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, symbol: "APP")
            .AddNode("db", "Database", 300, 90, TopologyNodeKind.Database, TopologyHealthStatus.Healthy, symbol: "SQL")
            .AddNode("queue", "Queue", 300, 180, TopologyNodeKind.Service, TopologyHealthStatus.Warning, symbol: "Q")
            .AddEdge("app-db", "app", "db", "reads", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Forward)
            .AddEdge("app-queue", "app", "queue", "publishes", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward)
            .WithEdgesOfKind(TopologyEdgeKind.Dependency, lineStyle: TopologyEdgeLineStyle.Dotted, color: "#64748B");

        var svg = chart.ToSvg(new TopologyRenderOptions { LegendMode = TopologyLegendMode.Auto });
        var legendStart = svg.IndexOf("data-cfx-role=\"topology-legend\"", StringComparison.Ordinal);
        Assert(legendStart >= 0, "Topology auto legend should render for styled relationship maps.");
        var legend = svg.Substring(legendStart);
        Assert(legend.Contains(">Dependency<", StringComparison.Ordinal), "Topology auto legend should include the styled edge kind.");
        Assert(legend.Contains("stroke=\"#64748B\"", StringComparison.Ordinal), "Topology auto legend should reuse a shared edge-kind color.");
        Assert(legend.Contains("stroke-dasharray=\"2 5\"", StringComparison.Ordinal), "Topology auto legend should reuse a shared edge-kind line style.");
        Assert(chart.ToPng(new TopologyRenderOptions { LegendMode = TopologyLegendMode.Auto }).Length > 64, "Styled inferred topology legends should render as PNG.");
    }

    private static void TopologyMergedLegendsEnrichExplicitItems() {
        var catalog = TopologyIconCatalog.Default();
        var chart = TopologyChart.Create()
            .WithId("merged-legend-style")
            .WithViewport(560, 340, 24)
            .WithLegend(TopologyLegend.Create("Focused Legend")
                .AddNodeKind("Certificates", TopologyNodeKind.Certificate)
                .AddEdgeKind("Ownership path", TopologyEdgeKind.Ownership)
                .AddEdgeKind("Observed path", TopologyEdgeKind.Mapping))
            .AddNode("cert-a", "Certificate A", 90, 120, TopologyNodeKind.Certificate, TopologyHealthStatus.Healthy)
            .AddNode("cert-b", "Certificate B", 300, 120, TopologyNodeKind.Certificate, TopologyHealthStatus.Warning)
            .AddNode("owner", "Owner", 300, 220, TopologyNodeKind.Person, TopologyHealthStatus.Healthy)
            .AddEdge("cert-owner", "cert-a", "owner", "owned by", TopologyEdgeKind.Ownership, TopologyHealthStatus.Healthy, TopologyDirection.Forward)
            .WithNodesOfKind(TopologyNodeKind.Certificate, color: "#2563EB", backgroundColor: "#EFF6FF")
            .WithNodesOfKindIcon(TopologyNodeKind.Certificate, "common:certificate", catalog)
            .WithEdgesOfKind(TopologyEdgeKind.Ownership, lineStyle: TopologyEdgeLineStyle.Dashed, color: "#7C3AED");

        var svg = chart.ToSvg(new TopologyRenderOptions { IconCatalog = catalog, LegendMode = TopologyLegendMode.Merge });
        var legendStart = svg.IndexOf("data-cfx-role=\"topology-legend\"", StringComparison.Ordinal);
        Assert(legendStart >= 0, "Topology merged legends should render.");
        var legend = svg.Substring(legendStart);
        Assert(legend.Contains(">Focused Legend<", StringComparison.Ordinal), "Topology merged legends should preserve explicit titles.");
        Assert(legend.Contains(">Certificates<", StringComparison.Ordinal), "Topology merged legends should preserve explicit node labels.");
        Assert(legend.Contains("data-legend-icon-id=\"common:certificate\"", StringComparison.Ordinal), "Topology merged legends should enrich explicit node items with inferred icons.");
        Assert(legend.Contains("fill=\"#EFF6FF\"", StringComparison.Ordinal), "Topology merged legends should enrich explicit node items with inferred backgrounds.");
        Assert(legend.Contains(">Ownership path<", StringComparison.Ordinal), "Topology merged legends should preserve explicit edge labels.");
        Assert(legend.Contains("stroke=\"#7C3AED\"", StringComparison.Ordinal), "Topology merged legends should enrich explicit edge items with inferred colors.");
        Assert(legend.Contains("stroke-dasharray=\"8 5\"", StringComparison.Ordinal), "Topology merged legends should enrich explicit edge items with inferred line styles.");
        Assert(CountOccurrences(legend, ">Certificates<") == 1, "Topology merged legends should not duplicate enriched explicit node items.");
        var focusedSvg = chart.ToSvg(new TopologyRenderOptions { IconCatalog = catalog, LegendMode = TopologyLegendMode.Enrich });
        var focusedLegend = focusedSvg.Substring(focusedSvg.IndexOf("data-cfx-role=\"topology-legend\"", StringComparison.Ordinal));
        Assert(focusedLegend.Contains("data-legend-icon-id=\"common:certificate\"", StringComparison.Ordinal), "Topology enriched legends should fill explicit marker details.");
        Assert(focusedLegend.Contains("stroke=\"#7C3AED\"", StringComparison.Ordinal), "Topology enriched legends should fill explicit edge details.");
        Assert(focusedLegend.Contains(">Observed path<", StringComparison.Ordinal) && focusedLegend.Contains("stroke-dasharray=\"8 5\"", StringComparison.Ordinal), "Topology edge-kind legend entries with default Auto line style should render as dashed observed links.");
        Assert(!focusedLegend.Contains(">Person<", StringComparison.Ordinal), "Topology enriched legends should not add unrelated inferred items to focused legends.");
        Assert(chart.ToPng(new TopologyRenderOptions { IconCatalog = catalog, LegendMode = TopologyLegendMode.Merge }).Length > 64, "Enriched merged topology legends should render as PNG.");
    }
}
