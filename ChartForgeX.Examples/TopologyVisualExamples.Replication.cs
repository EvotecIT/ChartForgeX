using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

internal static partial class TopologyVisualExamples {
    private static TopologyChart BuildReplicationMeshExplorer() {
        var chart = TopologyChart.Create()
            .Id("visual-replication-mesh-explorer")
            .Title("Replication Mesh Explorer")
            .Subtitle("Replication paths, lag, queues, route ports, lanes, and icon nodes.")
            .Viewport(new TopologyViewport(1280, 700, 28))
            .Legend(TopologyLegend.Default().AddNodeKind("Domain Controller", TopologyNodeKind.Server, symbol: "DC").AddEdgeKind("Replication", TopologyEdgeKind.Replication))
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
            .UpdateEdge("nyc-lon", e => e.SourcePort(TopologyEdgePort.Right).TargetPort(TopologyEdgePort.Left))
            .UpdateEdge("lon-nyc", e => e.SourcePort(TopologyEdgePort.Left).TargetPort(TopologyEdgePort.Right))
            .UpdateEdge("lon-fra", e => e.SourcePort(TopologyEdgePort.Right).TargetPort(TopologyEdgePort.Left))
            .UpdateEdge("fra-lon", e => e.SourcePort(TopologyEdgePort.Left).TargetPort(TopologyEdgePort.Right))
            // Side approaches enter below site headings and preserve separate reciprocal lanes.
            .UpdateEdge("fra-sin", e => e.SourcePort(TopologyEdgePort.Right).TargetPort(TopologyEdgePort.Left))
            .UpdateEdge("sin-fra", e => e.SourcePort(TopologyEdgePort.Left).TargetPort(TopologyEdgePort.Right))
            .UpdateEdge("nyc-sfo", e => e.SourcePort(TopologyEdgePort.Left).TargetPort(TopologyEdgePort.Left))
            .UpdateEdge("sfo-nyc", e => e.SourcePort(TopologyEdgePort.Left).TargetPort(TopologyEdgePort.Left))
            .UpdateEdge("lon-sfo", e => e.SourcePort(TopologyEdgePort.Left).TargetPort(TopologyEdgePort.Left))
            .UpdateEdge("sfo-lon", e => e.SourcePort(TopologyEdgePort.Left).TargetPort(TopologyEdgePort.Left))
            .EdgeWaypoints("nyc-sfo", new ChartPoint(60, 216), new ChartPoint(60, 514))
            .EdgeWaypoints("sfo-nyc", new ChartPoint(72, 514), new ChartPoint(72, 216))
            .EdgeWaypoints("lon-sfo", new ChartPoint(376, 216), new ChartPoint(376, 340), new ChartPoint(184, 340), new ChartPoint(184, 514))
            .EdgeWaypoints("sfo-lon", new ChartPoint(172, 514), new ChartPoint(172, 316), new ChartPoint(366, 316), new ChartPoint(366, 216))
            .EdgeWaypoints("fra-sin", new ChartPoint(974, 216), new ChartPoint(974, 352), new ChartPoint(676, 352), new ChartPoint(676, 514))
            .EdgeWaypoints("sin-fra", new ChartPoint(688, 514), new ChartPoint(688, 370), new ChartPoint(988, 370), new ChartPoint(988, 216))
            .UpdateEdge("sfo-sin", e => e.SourcePort(TopologyEdgePort.Right).TargetPort(TopologyEdgePort.Left))
            .UpdateEdge("sin-sfo", e => e.SourcePort(TopologyEdgePort.Left).TargetPort(TopologyEdgePort.Right))
            .UpdateEdge("sin-fra", e => e.LabelOffsetX(26).LabelOffsetY(-8))
            .UpdateEdge("sin-sfo", e => e.LabelOffsetX(0).LabelOffsetY(30))
            .UpdateEdge("sfo-nyc", e => e.LabelOffsetX(-30).LabelOffsetY(-180))
            .UpdateEdge("sfo-lon", e => e.LabelOffsetX(130).LabelOffsetY(72))
            .UpdateEdge("nyc-local", e => e.LabelOffsetX(0).LabelOffsetY(34))
            .UpdateEdge("lon-local", e => e.LabelOffsetX(0).LabelOffsetY(34))
            .UpdateEdge("fra-local", e => e.LabelOffsetX(0).LabelOffsetY(34))
            .UpdateEdge("sfo-local", e => e.LabelOffsetX(0).LabelOffsetY(34))
            .UpdateEdge("sin-local", e => e.LabelOffsetX(0).LabelOffsetY(34))
            .UpdateEdge("nyc-local", e => e.LineStyle(TopologyEdgeLineStyle.Dashed))
            .UpdateEdge("lon-local", e => e.LineStyle(TopologyEdgeLineStyle.Dashed))
            .UpdateEdge("fra-local", e => e.LineStyle(TopologyEdgeLineStyle.Dashed))
            .UpdateEdge("sfo-local", e => e.LineStyle(TopologyEdgeLineStyle.Dashed))
            .UpdateEdge("sin-local", e => e.LineStyle(TopologyEdgeLineStyle.Dashed))
            .UpdateGroup("HQ-NYC", g => g.Color(EmeaColor))
            .UpdateGroup("LON-LON", g => g.Color(ApacColor))
            .UpdateGroup("FRA-FRA", g => g.Color("#F97316"))
            .UpdateGroup("SFO-SFO", g => g.Color("#0891B2"))
            .UpdateGroup("SIN-SIN", g => g.Color(ApacColor))
            .UpdateGroup("HQ-NYC", g => g.Symbol("globe"))
            .UpdateGroup("LON-LON", g => g.Symbol("globe"))
            .UpdateGroup("FRA-FRA", g => g.Symbol("globe"))
            .UpdateGroup("SFO-SFO", g => g.Symbol("globe"))
            .UpdateGroup("SIN-SIN", g => g.Symbol("globe"));

        foreach (var node in chart.Nodes) chart.UpdateNode(node.Id, n => n.DisplayMode(TopologyNodeDisplayMode.Icon).Badge(null));
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "HQ-NYC", StringComparison.Ordinal))) chart.UpdateNode(node.Id, n => n.Color(EmeaColor));
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "LON-LON", StringComparison.Ordinal))) chart.UpdateNode(node.Id, n => n.Color(ApacColor));
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "FRA-FRA", StringComparison.Ordinal))) chart.UpdateNode(node.Id, n => n.Color("#F97316"));
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "SFO-SFO", StringComparison.Ordinal))) chart.UpdateNode(node.Id, n => n.Color("#0891B2"));
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "SIN-SIN", StringComparison.Ordinal))) chart.UpdateNode(node.Id, n => n.Color(ApacColor));
        foreach (var edge in chart.Edges) {
            edge.Metrics["lag"] = edge.Label ?? string.Empty;
            edge.Metrics["queue"] = edge.SecondaryLabel ?? string.Empty;
        }

        chart
            .AddScenario("client-request-europe", "Europe request", scenario => scenario
                .Color("#2563EB")
                .Description("Nominal client request from Frankfurt through London to New York.")
                .Metadata("request.type", "client")
                .Metadata("origin.region", "EMEA")
                .AddNodeStep("fra-dc1", "Origin")
                .AddEdgeStep("fra-lon", "Route", configure: step => step.Metadata("transport", "WAN"))
                .AddNodeStep("lon-dc2", "Policy")
                .AddEdgeStep("lon-local", "Site handoff", configure: step => step.Metadata("transport", "LAN"))
                .AddNodeStep("lon-dc1", "Directory source")
                .AddEdgeStep("lon-nyc", "Directory", configure: step => step.Metadata("transport", "Directory replication"))
                .AddNodeStep("nyc-dc1", "Lookup"))
            .AddScenario("apac-failover", "APAC failover", scenario => scenario
                .Color("#EF4444")
                .Description("Critical APAC failover path through Singapore and San Francisco.")
                .Metadata("request.type", "failover")
                .Metadata("origin.region", "APAC")
                .AddNodeStep("fra-dc2", "Origin")
                .AddEdgeStep("fra-sin", "Degraded", configure: step => step.Metadata("risk", "critical"))
                .AddNodeStep("sin-dc1", "Fallback")
                .AddEdgeStep("sin-sfo", "Recovery", configure: step => step.Metadata("risk", "recovery"))
                .AddNodeStep("sfo-dc2", "Target"));

        return chart;
    }

    private static TopologyChart BuildReplicationHealthHub() {
        var chart = TopologyChart.Create()
            .Id("visual-replication-health-hub")
            .Title("Replication Health")
            .Subtitle("Compact hub view with dense site markers and critical replication paths.")
            .Viewport(new TopologyViewport(1240, 610, 28))
            .Legend(TopologyLegend.Create("Connection Health")
                .AddEdgeKind("Healthy", TopologyEdgeKind.Replication, "#16A34A")
                .AddEdgeKind("Warning", TopologyEdgeKind.Replication, "#F97316")
                .AddEdgeKind("Critical", TopologyEdgeKind.Replication, "#EF4444")
                .AddEdgeKind("Unknown", TopologyEdgeKind.Replication, "#64748B"))
            .AddNode("cloud", "Inter-Forest", 594, 286, TopologyNodeKind.Cloud, TopologyHealthStatus.Healthy, null, symbol: "CL", width: 56, height: 56)
            .UpdateNode("cloud", n => n.DisplayMode(TopologyNodeDisplayMode.Icon).Badge(null))
            .UpdateNode("cloud", n => n.Color("#60A5FA"))
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
            .UpdateEdge("cloud-fra", e => e.LineStyle(TopologyEdgeLineStyle.Dashed))
            .UpdateEdge("cloud-sin", e => e.LineStyle(TopologyEdgeLineStyle.Dashed))
            .UpdateEdge("cloud-ams", e => e.SourcePort(TopologyEdgePort.Left).TargetPort(TopologyEdgePort.Top))
            .UpdateEdge("cloud-fra", e => e.SourcePort(TopologyEdgePort.Bottom).TargetPort(TopologyEdgePort.Top))
            .UpdateEdge("cloud-sin", e => e.SourcePort(TopologyEdgePort.Right).TargetPort(TopologyEdgePort.Left))
            .EdgeWaypoints("cloud-ams", new ChartPoint(430, 390), new ChartPoint(304.5, 408))
            .EdgeWaypoints("cloud-fra", new ChartPoint(702, 418), new ChartPoint(702, 428), new ChartPoint(620.5, 428))
            .EdgeWaypoints("cloud-sin", new ChartPoint(744, 408), new ChartPoint(784, 456), new ChartPoint(784, 490.5));
        return chart;
    }
}
