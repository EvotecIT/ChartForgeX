using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

internal static partial class TopologyVisualExamples {
    private static TopologyChart BuildReplicationMeshExplorer() {
        var chart = TopologyChart.Create()
            .WithId("visual-replication-mesh-explorer")
            .WithTitle("Replication Mesh Explorer")
            .WithSubtitle("Replication paths, lag, queues, route ports, lanes, and icon nodes.")
            .WithViewport(1280, 700, 28)
            .WithLegend(TopologyLegend.Default().AddNodeKind("Domain Controller", TopologyNodeKind.Server, symbol: "DC").AddEdgeKind("Replication", TopologyEdgeKind.Replication))
            .AddGroup("HQ-NYC", "HQ-NYC", 80, 132, 230, 142, TopologyHealthStatus.Healthy, "2 DCs")
            .AddGroup("LON-LON", "LON-LON", 386, 132, 230, 142, TopologyHealthStatus.Healthy, "2 DCs")
            .AddGroup("FRA-FRA", "FRA-FRA", 700, 132, 230, 142, TopologyHealthStatus.Warning, "2 DCs")
            .AddGroup("SFO-SFO", "SFO-SFO", 210, 430, 246, 142, TopologyHealthStatus.Healthy, "2 DCs")
            .AddGroup("SIN-SIN", "SIN-SIN", 710, 430, 246, 142, TopologyHealthStatus.Critical, "2 DCs")
            .AddNode("nyc-dc1", "NYC-DC1", 112, 194, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "HQ-NYC", "12 ms", symbol: "DC")
            .AddNode("nyc-dc2", "NYC-DC2", 222, 194, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "HQ-NYC", "12 ms", symbol: "DC")
            .AddNode("lon-dc1", "LON-DC1", 418, 194, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "LON-LON", "14 ms", symbol: "DC")
            .AddNode("lon-dc2", "LON-DC2", 528, 194, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "LON-LON", "14 ms", symbol: "DC")
            .AddNode("fra-dc1", "FRA-DC1", 732, 194, TopologyNodeKind.Server, TopologyHealthStatus.Warning, "FRA-FRA", "56 ms", symbol: "DC")
            .AddNode("fra-dc2", "FRA-DC2", 842, 194, TopologyNodeKind.Server, TopologyHealthStatus.Warning, "FRA-FRA", "56 ms", symbol: "DC")
            .AddNode("sfo-dc1", "SFO-DC1", 250, 492, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "SFO-SFO", "18 ms", symbol: "DC")
            .AddNode("sfo-dc2", "SFO-DC2", 360, 492, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "SFO-SFO", "18 ms", symbol: "DC")
            .AddNode("sin-dc1", "SIN-DC1", 750, 492, TopologyNodeKind.Server, TopologyHealthStatus.Critical, "SIN-SIN", "124 ms", symbol: "DC")
            .AddNode("sin-dc2", "SIN-DC2", 860, 492, TopologyNodeKind.Server, TopologyHealthStatus.Critical, "SIN-SIN", "124 ms", symbol: "DC")
            .AddEdge("nyc-local", "nyc-dc1", "nyc-dc2", "12 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("lon-local", "lon-dc1", "lon-dc2", "14 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("fra-local", "fra-dc1", "fra-dc2", "56 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("sfo-local", "sfo-dc1", "sfo-dc2", "18 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("sin-local", "sin-dc1", "sin-dc2", "124 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, VisualLinkDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("nyc-lon", "nyc-dc1", "lon-dc1", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight, "Q:312", tertiaryLabel: "15m / 2m")
            .AddEdge("lon-nyc", "lon-dc1", "nyc-dc1", "107 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight, "Q:233", tertiaryLabel: "7m ago")
            .AddEdge("lon-fra", "lon-dc2", "fra-dc1", "156 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight, "Q:842", tertiaryLabel: "3m ago")
            .AddEdge("fra-lon", "fra-dc1", "lon-dc2", "142 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight, "Q:615", tertiaryLabel: "5m ago")
            .AddEdge("fra-sin", "fra-dc2", "sin-dc1", "238 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal, "Q:1124", tertiaryLabel: "2m ago")
            .AddEdge("sin-fra", "sin-dc1", "fra-dc2", "198 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal, "Q:876", tertiaryLabel: "4m ago")
            .AddEdge("sfo-sin", "sfo-dc2", "sin-dc1", "214 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight, "Q:1552", tertiaryLabel: "1m ago")
            .AddEdge("sin-sfo", "sin-dc1", "sfo-dc2", "124 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight, "Q:910", tertiaryLabel: "3m ago")
            .AddEdge("nyc-sfo", "nyc-dc1", "sfo-dc1", "107 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal, "Q:233", tertiaryLabel: "7m ago")
            .AddEdge("sfo-nyc", "sfo-dc1", "nyc-dc1", "112 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal, "Q:276", tertiaryLabel: "9m ago")
            .AddEdge("lon-sfo", "lon-dc1", "sfo-dc1", "118 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal, "Q:301", tertiaryLabel: "10m ago")
            .AddEdge("sfo-lon", "sfo-dc1", "lon-dc1", "121 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal, "Q:289", tertiaryLabel: "9m ago")
            .WithEdgePorts("nyc-lon", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("lon-nyc", TopologyEdgePort.Left, TopologyEdgePort.Right)
            .WithEdgePorts("lon-fra", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("fra-lon", TopologyEdgePort.Left, TopologyEdgePort.Right)
            // Side approaches enter below site headings and preserve separate reciprocal lanes.
            .WithEdgePorts("fra-sin", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("sin-fra", TopologyEdgePort.Left, TopologyEdgePort.Right)
            .WithEdgePorts("nyc-sfo", TopologyEdgePort.Left, TopologyEdgePort.Left)
            .WithEdgePorts("sfo-nyc", TopologyEdgePort.Left, TopologyEdgePort.Left)
            .WithEdgePorts("lon-sfo", TopologyEdgePort.Left, TopologyEdgePort.Left)
            .WithEdgePorts("sfo-lon", TopologyEdgePort.Left, TopologyEdgePort.Left)
            .WithEdgeWaypoints("nyc-sfo", new ChartPoint(60, 216), new ChartPoint(60, 514))
            .WithEdgeWaypoints("sfo-nyc", new ChartPoint(72, 514), new ChartPoint(72, 216))
            .WithEdgeWaypoints("lon-sfo", new ChartPoint(350, 216), new ChartPoint(350, 300), new ChartPoint(184, 300), new ChartPoint(184, 514))
            .WithEdgeWaypoints("sfo-lon", new ChartPoint(196, 514), new ChartPoint(196, 316), new ChartPoint(334, 316), new ChartPoint(334, 216))
            .WithEdgeWaypoints("fra-sin", new ChartPoint(974, 216), new ChartPoint(974, 352), new ChartPoint(676, 352), new ChartPoint(676, 514))
            .WithEdgeWaypoints("sin-fra", new ChartPoint(688, 514), new ChartPoint(688, 370), new ChartPoint(988, 370), new ChartPoint(988, 216))
            .WithEdgePorts("sfo-sin", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("sin-sfo", TopologyEdgePort.Left, TopologyEdgePort.Right)
            .WithEdgeLabelOffset("lon-nyc", 0, 30)
            .WithEdgeLabelOffset("fra-lon", 0, 30)
            .WithEdgeLabelOffset("sin-fra", 26, -8)
            .WithEdgeLabelOffset("sin-sfo", 0, 30)
            .WithEdgeLabelOffset("sfo-nyc", -30, -180)
            .WithEdgeLabelOffset("sfo-lon", 34, -18)
            .WithEdgeLabelOffset("nyc-local", 0, 34)
            .WithEdgeLabelOffset("lon-local", 0, 34)
            .WithEdgeLabelOffset("fra-local", 0, 34)
            .WithEdgeLabelOffset("sfo-local", 0, 34)
            .WithEdgeLabelOffset("sin-local", 0, 34)
            .WithEdgeLineStyle("nyc-local", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("lon-local", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("fra-local", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("sfo-local", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("sin-local", TopologyEdgeLineStyle.Dashed)
            .WithGroupColor("HQ-NYC", EmeaColor)
            .WithGroupColor("LON-LON", ApacColor)
            .WithGroupColor("FRA-FRA", "#F97316")
            .WithGroupColor("SFO-SFO", "#0891B2")
            .WithGroupColor("SIN-SIN", ApacColor)
            .WithGroupSymbol("HQ-NYC", "globe")
            .WithGroupSymbol("LON-LON", "globe")
            .WithGroupSymbol("FRA-FRA", "globe")
            .WithGroupSymbol("SFO-SFO", "globe")
            .WithGroupSymbol("SIN-SIN", "globe");

        foreach (var node in chart.Nodes) chart.WithNodeDisplay(node.Id, TopologyNodeDisplayMode.Icon);
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "HQ-NYC", StringComparison.Ordinal))) chart.WithNodeColor(node.Id, EmeaColor);
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "LON-LON", StringComparison.Ordinal))) chart.WithNodeColor(node.Id, ApacColor);
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "FRA-FRA", StringComparison.Ordinal))) chart.WithNodeColor(node.Id, "#F97316");
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "SFO-SFO", StringComparison.Ordinal))) chart.WithNodeColor(node.Id, "#0891B2");
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "SIN-SIN", StringComparison.Ordinal))) chart.WithNodeColor(node.Id, ApacColor);
        foreach (var edge in chart.Edges) {
            edge.Metrics["lag"] = edge.Label ?? string.Empty;
            edge.Metrics["queue"] = edge.SecondaryLabel ?? string.Empty;
        }

        chart
            .AddScenario("client-request-europe", "Europe request", scenario => scenario
                .WithColor("#2563EB")
                .WithDescription("Nominal client request from Frankfurt through London to New York.")
                .WithMetadata("request.type", "client")
                .WithMetadata("origin.region", "EMEA")
                .AddNodeStep("fra-dc1", "Origin")
                .AddEdgeStep("fra-lon", "Route", configure: step => step.WithMetadata("transport", "WAN"))
                .AddNodeStep("lon-dc2", "Policy")
                .AddEdgeStep("lon-nyc", "Directory", configure: step => step.WithMetadata("transport", "Directory replication"))
                .AddNodeStep("nyc-dc1", "Lookup"))
            .AddScenario("apac-failover", "APAC failover", scenario => scenario
                .WithColor("#EF4444")
                .WithDescription("Critical APAC failover path through Singapore and San Francisco.")
                .WithMetadata("request.type", "failover")
                .WithMetadata("origin.region", "APAC")
                .AddNodeStep("fra-dc2", "Origin")
                .AddEdgeStep("fra-sin", "Degraded", configure: step => step.WithMetadata("risk", "critical"))
                .AddNodeStep("sin-dc1", "Fallback")
                .AddEdgeStep("sin-sfo", "Recovery", configure: step => step.WithMetadata("risk", "recovery"))
                .AddNodeStep("sfo-dc2", "Target"));

        return chart;
    }

    private static TopologyChart BuildReplicationHealthHub() {
        var chart = TopologyChart.Create()
            .WithId("visual-replication-health-hub")
            .WithTitle("Replication Health")
            .WithSubtitle("Compact hub view with dense site markers and critical replication paths.")
            .WithViewport(1240, 610, 28)
            .WithLegend(TopologyLegend.Create("Connection Health")
                .AddEdgeKind("Healthy", TopologyEdgeKind.Replication, "#16A34A")
                .AddEdgeKind("Warning", TopologyEdgeKind.Replication, "#F97316")
                .AddEdgeKind("Critical", TopologyEdgeKind.Replication, "#EF4444")
                .AddEdgeKind("Unknown", TopologyEdgeKind.Replication, "#64748B"))
            .AddNode("cloud", "Inter-Forest", 594, 286, TopologyNodeKind.Cloud, TopologyHealthStatus.Healthy, null, symbol: "CL", width: 56, height: 56)
            .WithNodeDisplay("cloud", TopologyNodeDisplayMode.Icon)
            .WithNodeColor("cloud", "#60A5FA")
            .AddGroup("HQ", "HQ-NYC (32 DCs)", 430, 92, 356, 128, TopologyHealthStatus.Healthy, symbol: "globe")
            .AddGroup("LON", "LON-London (12 DCs)", 72, 260, 314, 128, TopologyHealthStatus.Healthy, symbol: "globe")
            .AddGroup("SFO", "SFO-SanFrancisco (11 DCs)", 838, 260, 330, 128, TopologyHealthStatus.Healthy, symbol: "globe")
            .AddGroup("AMS", "AMS-Amsterdam (9 DCs)", 128, 436, 314, 128, TopologyHealthStatus.Warning, symbol: "globe")
            .AddGroup("FRA", "FRA-Frankfurt (10 DCs)", 462, 456, 314, 128, TopologyHealthStatus.Healthy, symbol: "globe")
            .AddGroup("SIN", "SIN-Singapore (8 DCs)", 798, 436, 314, 128, TopologyHealthStatus.Critical, symbol: "globe");

        AddSiteDots(chart, "HQ", 470, 156, 10, TopologyHealthStatus.Healthy);
        AddSiteDots(chart, "LON", 112, 326, 8, TopologyHealthStatus.Healthy);
        AddSiteDots(chart, "SFO", 878, 326, 8, TopologyHealthStatus.Healthy);
        AddSiteDots(chart, "AMS", 168, 502, 7, TopologyHealthStatus.Warning);
        AddSiteDots(chart, "FRA", 502, 522, 8, TopologyHealthStatus.Healthy);
        AddSiteDots(chart, "SIN", 838, 502, 7, TopologyHealthStatus.Critical);
        AddSiteAnchors(chart, "HQ-bottom", 536, 220, 42, 0, 4);
        AddSiteAnchors(chart, "LON-right", 386, 310, 0, 24, 3);
        AddSiteAnchors(chart, "SFO-left", 838, 310, 0, 24, 3);
        AddSiteAnchor(chart, "AMS-top", 304, 436);
        AddSiteAnchor(chart, "FRA-top", 620, 456);
        AddSiteAnchor(chart, "SIN-left", 798, 490);
        AddReplicationFan(chart, "cloud", "HQ-bottom", 1, 4, TopologyHealthStatus.Healthy, TopologyEdgePort.Top, TopologyEdgePort.Bottom);
        AddReplicationFan(chart, "cloud", "LON-right", 1, 3, TopologyHealthStatus.Healthy, TopologyEdgePort.Left, TopologyEdgePort.Right);
        AddReplicationFan(chart, "cloud", "SFO-left", 1, 3, TopologyHealthStatus.Healthy, TopologyEdgePort.Right, TopologyEdgePort.Left);
        chart
            .AddEdge("cloud-ams", "cloud", "AMS-top", "warning", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Curved)
            .AddEdge("cloud-fra", "cloud", "FRA-top", "healthy", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Curved)
            .AddEdge("cloud-sin", "cloud", "SIN-left", "critical", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight)
            .WithEdgeLineStyle("cloud-fra", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("cloud-sin", TopologyEdgeLineStyle.Dashed)
            .WithEdgePorts("cloud-ams", TopologyEdgePort.Left, TopologyEdgePort.Top)
            .WithEdgePorts("cloud-fra", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgePorts("cloud-sin", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgeWaypoints("cloud-ams", new ChartPoint(430, 390), new ChartPoint(304.5, 408))
            .WithEdgeWaypoints("cloud-fra", new ChartPoint(702, 418), new ChartPoint(702, 428), new ChartPoint(620.5, 428))
            .WithEdgeWaypoints("cloud-sin", new ChartPoint(744, 408), new ChartPoint(784, 456), new ChartPoint(784, 490.5));
        return chart;
    }
}
