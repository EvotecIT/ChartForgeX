using ChartForgeX.Core;
using ChartForgeX.Topology;

internal static partial class TopologyVisualExamples {
    private static TopologyChart BuildGeographicTopologyMap() {
        var chart = TopologyChart.Create()
            .WithId("visual-geographic-topology-map")
            .WithTitle("Geographic Topology View")
            .WithSubtitle("Topology-native region and site projection with route arcs, health, and host-ready metadata.")
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .WithViewport(1280, 720, 28)
            .WithTheme(TopologyTheme.Light())
            .WithLegend(TopologyLegend.Default()
                .AddNodeKind("Region hub", TopologyNodeKind.Hub, symbol: "H")
                .AddNodeKind("Site", TopologyNodeKind.Branch, symbol: "S")
                .AddNodeKind("Bridgehead DC", TopologyNodeKind.Server, symbol: "DC")
                .AddEdgeKind("WAN link", TopologyEdgeKind.Connectivity))
            .AddGroup("AMER", "AMER", 0, 0, 0, 0, TopologyHealthStatus.Healthy, "47 sites", "/geo/regions/amer", symbol: "region", color: AmerColor)
            .AddGroup("EMEA", "EMEA", 0, 0, 0, 0, TopologyHealthStatus.Healthy, "56 sites", "/geo/regions/emea", symbol: "region", color: EmeaColor)
            .AddGroup("APAC", "APAC", 0, 0, 0, 0, TopologyHealthStatus.Critical, "39 sites", "/geo/regions/apac", symbol: "region", color: ApacColor)
            .AddNode("amer-hub", "AMER", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "AMER", "47 sites", "/geo/regions/amer", width: 104, height: 52, symbol: "H", color: AmerColor)
            .AddNode("emea-hub", "EMEA", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "EMEA", "56 sites", "/geo/regions/emea", width: 104, height: 52, symbol: "H", color: EmeaColor)
            .AddNode("apac-hub", "APAC", 0, 0, TopologyNodeKind.Hub, TopologyHealthStatus.Critical, "APAC", "39 sites", "/geo/regions/apac", width: 104, height: 52, symbol: "H", color: ApacColor)
            .AddNode("nyc", "NYC DC", 0, 0, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "AMER", "Healthy", "/geo/sites/nyc", width: 58, height: 38, symbol: "DC", color: AmerColor)
            .AddNode("chi", "CHI DC", 0, 0, TopologyNodeKind.Server, TopologyHealthStatus.Warning, "AMER", "Packet loss", "/geo/sites/chi", width: 58, height: 38, symbol: "DC", color: AmerColor)
            .AddNode("sfo", "SFO Branch", 0, 0, TopologyNodeKind.Branch, TopologyHealthStatus.Critical, "AMER", "Down", "/geo/sites/sfo", width: 58, height: 38, symbol: "S", color: AmerColor)
            .AddNode("lon", "UK DC", 0, 0, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "EMEA", "Healthy", "/geo/sites/lon", width: 58, height: 38, symbol: "DC", color: EmeaColor)
            .AddNode("fra", "DE Branch", 0, 0, TopologyNodeKind.Branch, TopologyHealthStatus.Warning, "EMEA", "172 ms", "/geo/sites/fra", width: 58, height: 38, symbol: "S", color: EmeaColor)
            .AddNode("ams", "AMS DC", 0, 0, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "EMEA", "Healthy", "/geo/sites/ams", width: 58, height: 38, symbol: "DC", color: EmeaColor)
            .AddNode("sin", "APAC Hub", 0, 0, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "APAC", "Healthy", "/geo/sites/sin", width: 58, height: 38, symbol: "DC", color: ApacColor)
            .AddNode("syd", "ANZ", 0, 0, TopologyNodeKind.Branch, TopologyHealthStatus.Critical, "APAC", "Critical", "/geo/sites/syd", width: 58, height: 38, symbol: "S", color: ApacColor)
            .AddNode("bom", "IN Branch", 0, 0, TopologyNodeKind.Branch, TopologyHealthStatus.Warning, "APAC", "185 ms", "/geo/sites/bom", width: 58, height: 38, symbol: "S", color: ApacColor)
            .AddEdge("amer-emea", "amer-hub", "emea-hub", "68 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Curved, "WAN", "/geo/links/amer-emea")
            .AddEdge("emea-apac", "emea-hub", "apac-hub", "92 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Critical, TopologyDirection.Bidirectional, TopologyEdgeRouting.Curved, "WAN", "/geo/links/emea-apac")
            .AddEdge("amer-apac", "amer-hub", "apac-hub", "142 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Curved, "backup", "/geo/links/amer-apac")
            .AddEdge("amer-nyc", "amer-hub", "nyc", "local", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, routing: TopologyEdgeRouting.Curved)
            .AddEdge("amer-chi", "amer-hub", "chi", "loss", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, routing: TopologyEdgeRouting.Curved)
            .AddEdge("amer-sfo", "amer-hub", "sfo", "down", TopologyEdgeKind.Dependency, TopologyHealthStatus.Critical, routing: TopologyEdgeRouting.Curved)
            .AddEdge("emea-lon", "emea-hub", "lon", "local", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, routing: TopologyEdgeRouting.Curved)
            .AddEdge("emea-fra", "emea-hub", "fra", "172 ms", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, routing: TopologyEdgeRouting.Curved)
            .AddEdge("emea-ams", "emea-hub", "ams", "local", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, routing: TopologyEdgeRouting.Curved)
            .AddEdge("apac-sin", "apac-hub", "sin", "local", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, routing: TopologyEdgeRouting.Curved)
            .AddEdge("apac-syd", "apac-hub", "syd", "critical", TopologyEdgeKind.Dependency, TopologyHealthStatus.Critical, routing: TopologyEdgeRouting.Curved)
            .AddEdge("apac-bom", "apac-hub", "bom", "185 ms", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, routing: TopologyEdgeRouting.Curved)
            .WithGroupCoordinates("AMER", -98.5795, 39.8283)
            .WithGroupCoordinates("EMEA", 12.4964, 41.9028)
            .WithGroupCoordinates("APAC", 103.8198, 1.3521)
            .WithNodeCoordinates("amer-hub", -98.5795, 39.8283)
            .WithNodeCoordinates("emea-hub", 12.4964, 41.9028)
            .WithNodeCoordinates("apac-hub", 103.8198, 1.3521)
            .WithNodeCoordinates("nyc", -74.006, 40.7128)
            .WithNodeCoordinates("chi", -87.6298, 41.8781)
            .WithNodeCoordinates("sfo", -122.4194, 37.7749)
            .WithNodeCoordinates("lon", -0.1276, 51.5072)
            .WithNodeCoordinates("fra", 8.6821, 50.1109)
            .WithNodeCoordinates("ams", 4.9041, 52.3676)
            .WithNodeCoordinates("sin", 103.8198, 1.3521)
            .WithNodeCoordinates("syd", 151.2093, -33.8688)
            .WithNodeCoordinates("bom", 72.8777, 19.0760)
            .WithEdgeLineStyle("amer-apac", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("amer-chi", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("apac-bom", TopologyEdgeLineStyle.Dashed);

        foreach (var node in chart.Nodes) {
            if (node.Kind == TopologyNodeKind.Hub) {
                node.DisplayMode = TopologyNodeDisplayMode.Pill;
                continue;
            }

            node.DisplayMode = TopologyNodeDisplayMode.Dot;
            node.Badge = node.Symbol;
        }

        chart.Groups[0].Metadata["calloutSubtitle"] = "47 sites / 1 down";
        chart.Groups[1].Metadata["calloutSubtitle"] = "56 sites / 1 warning";
        chart.Groups[2].Metadata["calloutSubtitle"] = "39 sites / critical";
        return chart;
    }
}
