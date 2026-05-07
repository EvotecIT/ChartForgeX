using System.Globalization;
using System.Text;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

internal static partial class TopologyVisualExamples {
    private const string AmerColor = "#16A34A";
    private const string EmeaColor = "#2563EB";
    private const string ApacColor = "#8B5CF6";

    public static void Write(string target) {
        var artifacts = new List<VisualArtifact>();

        var tileOptions = new TopologyRenderOptions { NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeDirectionMarkers = false, IncludeEdgeLabelBackplates = false, LegendMode = TopologyLegendMode.Merge };
        var tileSubtitleOptions = new TopologyRenderOptions { NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeTileSubtitles = true, IncludeDirectionMarkers = false, IncludeEdgeLabelBackplates = false, LegendMode = TopologyLegendMode.Merge };
        var routeOptions = new TopologyRenderOptions { IncludeEdgeLabelBackplates = false, LegendMode = TopologyLegendMode.Merge };
        var topologyExplorerOptions = new TopologyRenderOptions { NodeDisplayMode = TopologyNodeDisplayMode.Tile, CardSubtitleMode = TopologyCardSubtitleMode.Chip, IncludeDirectionMarkers = false, IncludeEdgeLabelBackplates = false, LegendMode = TopologyLegendMode.Merge }
            .WithSelectedGroup("APAC")
            .WithSelectedNode("anz")
            .WithSelectedEdge("apac-anz")
            .WithSelectedEdge("bh-apac");

        SaveTopology(target, artifacts, "visual-topology-explorer", BuildTopologyExplorer(), "Topology Explorer", "Regional grouped topology with hubs, branches, bridgeheads, route ports, labels, links, tooltips, metadata hooks, SVG, HTML, and PNG.", topologyExplorerOptions);
        SaveTopology(target, artifacts, "visual-replication-mesh-explorer", BuildReplicationMeshExplorer(), "Replication Mesh Explorer", "Site-to-site replication mesh with icon nodes, bidirectional paths, explicit edge ports, route lanes, metric labels, and offender highlighting support.", routeOptions);
        SaveTopology(target, artifacts, "visual-subnets-site-links-map", BuildSubnetsSiteLinksMap(), "Subnets and Site Links Map", "Subnet-to-site mapping topology with overlapping/orphan subnet states, bridgehead mapping, site links, and route labels.", tileSubtitleOptions);
        SaveTopology(target, artifacts, "visual-dc-connectivity-map", BuildDcConnectivityMap(), "Domain Controller Connectivity", "Selected-object connectivity topology for domain controllers, connection objects, service checks, and partner health.");
        SaveTopology(target, artifacts, "visual-ad-sites-hierarchy", BuildAdSitesHierarchy(), "AD Sites Hierarchy", "Hierarchy-style site map with hubs, branches, bridgeheads, primary and backup links.", tileSubtitleOptions);
        SaveTopology(target, artifacts, "visual-replication-health-hub", BuildReplicationHealthHub(), "Replication Health Hub", "Compact replication health view with central hub, grouped sites, dense dot nodes, and critical path emphasis.", routeOptions);
        SaveTopology(target, artifacts, "visual-directory-health-replication", BuildDirectoryHealthReplication(), "Directory Health Replication", "Small directory-health topology with site cards, domain controller nodes, and cross-site replication status.", routeOptions);
        SaveTopology(target, artifacts, "visual-service-dependency-map", BuildServiceDependencyMap(), "Service Dependency Map", "Generic service dependency topology showing upstream/downstream service health without TestimoX-specific types.");
        SaveTopology(target, artifacts, "visual-geographic-topology-map", BuildGeographicTopologyMap(), "Geographic Topology Map", "Topology-native geographic layout with typed coordinates, projected site markers, curved WAN route arcs, labels, regional callouts, and SVG/PNG metadata hooks.", new TopologyRenderOptions { IncludeGroups = false, IncludeGeographicCallouts = true, IncludeEdgeLabelBackplates = false, LegendMode = TopologyLegendMode.Merge }.WithSelectedGroup("APAC").WithSelectedNode("apac-hub").WithSelectedEdge("emea-apac"));

        SaveMap(target, artifacts, "visual-geographic-region-map", BuildGeographicRegionMap(), "Geographic Region Map", "Dotted-map chart with AMER, EMEA, and APAC hubs, weighted markers, and cross-region route overlays.");
        SaveMap(target, artifacts, "visual-site-distribution-map", BuildSiteDistributionMap(), "Site Distribution Map", "Dotted-map chart for site distribution, site health, and regional route overlays.");
        SaveMap(target, artifacts, "visual-wan-latency-map", BuildWanLatencyMap(), "WAN Latency Map", "Map-backed route chart for WAN latency paths that can sit behind an HtmlForgeX region panel.");

        WriteManifest(target, artifacts);
        WriteCoverageIndex(target, artifacts);
    }

    private static TopologyChart BuildTopologyExplorer() {
        return TopologyChart.Create()
            .WithId("visual-topology-explorer")
            .WithTitle("Topology Explorer")
            .WithSubtitle("Logical regional topology view: groups, hubs, branches, bridgeheads, site links, statuses, and host-ready data hooks.")
            .WithViewport(1280, 720, 28)
            .WithTheme(TopologyTheme.Light())
            .WithLegend(TopologyLegend.Default()
                .AddNodeKind("Hub Site", TopologyNodeKind.Hub, symbol: "H")
                .AddNodeKind("Site", TopologyNodeKind.Branch, symbol: "S")
                .AddNodeKind("Bridgehead DC", TopologyNodeKind.Server, symbol: "BH")
                .AddEdgeKind("Site Link", TopologyEdgeKind.Link)
                .AddEdgeKind("Replication", TopologyEdgeKind.Replication))
            .AddGroup("AMER", "AMER", 70, 116, 330, 360, TopologyHealthStatus.Healthy, "47 sites", "/regions/amer", cssClass: "region-amer")
            .AddGroup("EMEA", "EMEA", 470, 116, 340, 360, TopologyHealthStatus.Healthy, "56 sites", "/regions/emea", cssClass: "region-emea")
            .AddGroup("APAC", "APAC", 880, 116, 330, 360, TopologyHealthStatus.Critical, "39 sites", "/regions/apac", cssClass: "region-apac")
            .AddNode("amer-hub", "AMER Hub", 170, 176, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "AMER", "47 sites", "/sites/amer-hub", "AMER Hub", symbol: "H")
            .AddNode("nam-west", "NAM West", 112, 292, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "AMER", "12 sites", "/sites/nam-west", symbol: "S")
            .AddNode("nam-east", "NAM East", 240, 292, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "AMER", "18 sites", "/sites/nam-east", symbol: "S")
            .AddNode("la-branch", "LA Branch", 112, 402, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "AMER", "2 DCs", "/sites/la", symbol: "B")
            .AddNode("ny-branch", "NY Branch", 240, 402, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "AMER", "3 DCs", "/sites/ny", symbol: "B")
            .AddNode("emea-hub", "EMEA Hub", 574, 176, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "EMEA", "56 sites", "/sites/emea-hub", symbol: "H")
            .AddNode("eu-west", "EU West", 512, 292, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "EMEA", "16 sites", "/sites/eu-west", symbol: "S")
            .AddNode("eu-central", "EU Central", 642, 292, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "EMEA", "22 sites", "/sites/eu-central", symbol: "S")
            .AddNode("tr-branch", "TR Branch", 704, 402, TopologyNodeKind.Branch, TopologyHealthStatus.Warning, "EMEA", "185 ms", "/sites/tr", symbol: "B")
            .AddNode("apac-hub", "APAC Hub", 980, 176, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "APAC", "39 sites", "/sites/apac-hub", symbol: "H")
            .AddNode("anz", "ANZ", 934, 292, TopologyNodeKind.Branch, TopologyHealthStatus.Critical, "APAC", "Critical", "/sites/anz", symbol: "S")
            .AddNode("in-india", "IN India", 1062, 292, TopologyNodeKind.Branch, TopologyHealthStatus.Unknown, "APAC", "Unknown", "/sites/in", symbol: "S")
            .AddNode("syd-branch", "SYD Branch", 934, 402, TopologyNodeKind.Branch, TopologyHealthStatus.Critical, "APAC", "Down", "/sites/syd", symbol: "B")
            .AddNode("bh-1", "Bridgehead DC 1", 412, 558, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, null, "Healthy", "/bridgeheads/1", width: 168, height: 58, symbol: "BH")
            .AddNode("bh-2", "Bridgehead DC 2", 686, 558, TopologyNodeKind.Server, TopologyHealthStatus.Critical, null, "Degraded", "/bridgeheads/2", width: 168, height: 58, symbol: "BH")
            .AddEdge("amer-hub-west", "amer-hub", "nam-west", null, TopologyEdgeKind.Link, TopologyHealthStatus.Unknown, TopologyDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("amer-hub-east", "amer-hub", "nam-east", null, TopologyEdgeKind.Link, TopologyHealthStatus.Unknown, TopologyDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("nam-west-la", "nam-west", "la-branch", null, TopologyEdgeKind.Link, TopologyHealthStatus.Unknown, TopologyDirection.None, TopologyEdgeRouting.Orthogonal)
            .AddEdge("nam-east-ny", "nam-east", "ny-branch", null, TopologyEdgeKind.Link, TopologyHealthStatus.Unknown, TopologyDirection.None, TopologyEdgeRouting.Orthogonal)
            .AddEdge("emea-hub-west", "emea-hub", "eu-west", null, TopologyEdgeKind.Link, TopologyHealthStatus.Unknown, TopologyDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("emea-hub-central", "emea-hub", "eu-central", null, TopologyEdgeKind.Link, TopologyHealthStatus.Unknown, TopologyDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("emea-hub-tr", "emea-hub", "tr-branch", null, TopologyEdgeKind.Link, TopologyHealthStatus.Unknown, TopologyDirection.None, TopologyEdgeRouting.Orthogonal)
            .AddEdge("apac-hub-anz", "apac-hub", "anz", null, TopologyEdgeKind.Link, TopologyHealthStatus.Unknown, TopologyDirection.None, TopologyEdgeRouting.Orthogonal)
            .AddEdge("apac-hub-in", "apac-hub", "in-india", null, TopologyEdgeKind.Link, TopologyHealthStatus.Unknown, TopologyDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("anz-syd", "anz", "syd-branch", null, TopologyEdgeKind.Link, TopologyHealthStatus.Unknown, TopologyDirection.None, TopologyEdgeRouting.Orthogonal)
            .AddEdge("amer-emea", "amer-hub", "emea-hub", "24 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "MPLS", "/links/amer-emea")
            .AddEdge("emea-apac", "emea-hub", "apac-hub", "82 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "MPLS", "/links/emea-apac")
            .AddEdge("apac-anz", "apac-hub", "anz", "142 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "critical", "/links/apac-anz")
            .AddEdge("amer-bh", "ny-branch", "bh-1", "32 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "bridgehead")
            .AddEdge("bh-link", "bh-1", "bh-2", "68 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "queue 44")
            .AddEdge("bh-apac", "bh-2", "syd-branch", "142 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "failed")
            .WithEdgePorts("amer-emea", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("emea-apac", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("apac-anz", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgeRouteLane("apac-anz", 20)
            .WithEdgePorts("amer-bh", TopologyEdgePort.Bottom, TopologyEdgePort.Left)
            .WithEdgeRouteLane("amer-bh", 20)
            .WithEdgePorts("bh-apac", TopologyEdgePort.Right, TopologyEdgePort.Bottom)
            .WithEdgeRouteLane("bh-apac", -22)
            .WithEdgeLineStyle("amer-bh", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("amer-emea", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("emea-apac", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("apac-anz", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("bh-link", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("bh-apac", TopologyEdgeLineStyle.Dashed)
            .WithGroupSymbol("AMER", "region")
            .WithGroupSymbol("EMEA", "region")
            .WithGroupSymbol("APAC", "region")
            .WithGroupColor("AMER", AmerColor)
            .WithGroupColor("EMEA", EmeaColor)
            .WithGroupColor("APAC", ApacColor)
            .WithNodeColor("amer-hub", AmerColor)
            .WithNodeColor("emea-hub", EmeaColor)
            .WithNodeColor("apac-hub", ApacColor)
            .WithNodeColor("anz", ApacColor)
            .WithNodesDisplay(TopologyNodeKind.Server, TopologyNodeDisplayMode.Card)
            .WithEdgeMuted("amer-hub-west")
            .WithEdgeMuted("amer-hub-east")
            .WithEdgeMuted("nam-west-la")
            .WithEdgeMuted("nam-east-ny")
            .WithEdgeMuted("emea-hub-west")
            .WithEdgeMuted("emea-hub-central")
            .WithEdgeMuted("emea-hub-tr")
            .WithEdgeMuted("apac-hub-anz")
            .WithEdgeMuted("apac-hub-in")
            .WithEdgeMuted("anz-syd");
    }

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
            .AddEdge("nyc-local", "nyc-dc1", "nyc-dc2", "12 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("lon-local", "lon-dc1", "lon-dc2", "14 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("fra-local", "fra-dc1", "fra-dc2", "56 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("sfo-local", "sfo-dc1", "sfo-dc2", "18 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("sin-local", "sin-dc1", "sin-dc2", "124 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("nyc-lon", "nyc-dc1", "lon-dc1", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "Q:312")
            .AddEdge("lon-fra", "lon-dc2", "fra-dc1", "156 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "Q:842")
            .AddEdge("fra-sin", "fra-dc2", "sin-dc1", "238 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "Q:1124")
            .AddEdge("sfo-sin", "sfo-dc2", "sin-dc1", "214 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "Q:1552")
            .AddEdge("nyc-sfo", "nyc-dc1", "sfo-dc1", "107 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Orthogonal, "Q:233")
            .AddEdge("lon-sfo", "lon-dc1", "sfo-dc1", "118 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Orthogonal, "Q:301")
            .WithEdgePorts("nyc-lon", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("lon-fra", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("fra-sin", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgeRouteLane("fra-sin", 26)
            .WithEdgePorts("nyc-sfo", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgeRouteLane("nyc-sfo", -24)
            .WithEdgePorts("lon-sfo", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgeRouteLane("lon-sfo", 22)
            .WithEdgePorts("sfo-sin", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgeLineStyle("nyc-local", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("lon-local", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("fra-local", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("sfo-local", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("sin-local", TopologyEdgeLineStyle.Dashed)
            .WithGroupColor("HQ-NYC", EmeaColor)
            .WithGroupColor("LON-LON", ApacColor)
            .WithGroupColor("FRA-FRA", "#F97316")
            .WithGroupColor("SFO-SFO", "#0891B2")
            .WithGroupColor("SIN-SIN", ApacColor);

        foreach (var node in chart.Nodes) chart.WithNodeDisplay(node.Id, TopologyNodeDisplayMode.Icon, node.Id.EndsWith("dc1", StringComparison.Ordinal) ? "1" : "2");
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "HQ-NYC", StringComparison.Ordinal))) chart.WithNodeColor(node.Id, EmeaColor);
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "LON-LON", StringComparison.Ordinal))) chart.WithNodeColor(node.Id, ApacColor);
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "FRA-FRA", StringComparison.Ordinal))) chart.WithNodeColor(node.Id, "#F97316");
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "SFO-SFO", StringComparison.Ordinal))) chart.WithNodeColor(node.Id, "#0891B2");
        foreach (var node in chart.Nodes.Where(node => string.Equals(node.GroupId, "SIN-SIN", StringComparison.Ordinal))) chart.WithNodeColor(node.Id, ApacColor);
        foreach (var edge in chart.Edges) {
            edge.Metrics["lag"] = edge.Label ?? string.Empty;
            edge.Metrics["queue"] = edge.SecondaryLabel ?? string.Empty;
        }

        return chart;
    }

    private static TopologyChart BuildSubnetsSiteLinksMap() {
        return TopologyChart.Create()
            .WithId("visual-subnets-site-links-map")
            .WithTitle("Subnets and Site Links Map")
            .WithSubtitle("Subnet-to-site mapping, site links, bridgeheads, orphan and overlap states.")
            .WithViewport(1280, 700, 28)
            .WithLegend(TopologyLegend.Default().AddNodeKind("Hub Site", TopologyNodeKind.Hub, symbol: "H").AddNodeKind("Subnet", TopologyNodeKind.NetworkSegment, symbol: "NET").AddNodeKind("Bridgehead", TopologyNodeKind.Server, symbol: "BH").AddEdgeKind("Site Link", TopologyEdgeKind.Link))
            .AddGroup("AMER", "AMER", 70, 126, 330, 350, TopologyHealthStatus.Healthy, "47 sites")
            .AddGroup("EMEA", "EMEA", 476, 126, 330, 350, TopologyHealthStatus.Healthy, "56 sites")
            .AddGroup("APAC", "APAC", 882, 126, 330, 350, TopologyHealthStatus.Critical, "39 sites")
            .AddNode("amer-hub", "AMER Hub", 170, 184, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "AMER", "10.0.0.0/16", symbol: "H")
            .AddNode("na-west", "NA-West", 112, 300, TopologyNodeKind.NetworkSegment, TopologyHealthStatus.Healthy, "AMER", "10.1.0.0/24", symbol: "NET")
            .AddNode("na-east", "NA-East", 242, 300, TopologyNodeKind.NetworkSegment, TopologyHealthStatus.Healthy, "AMER", "10.2.0.0/24", symbol: "NET")
            .AddNode("emea-hub", "EMEA Hub", 578, 184, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "EMEA", "10.10.0.0/16", symbol: "H")
            .AddNode("eu-west", "EU West", 520, 300, TopologyNodeKind.NetworkSegment, TopologyHealthStatus.Healthy, "EMEA", "10.10.1.0/24", symbol: "NET")
            .AddNode("eu-east", "EU East", 650, 300, TopologyNodeKind.NetworkSegment, TopologyHealthStatus.Warning, "EMEA", "10.10.3.0/24", symbol: "NET")
            .AddNode("apac-hub", "APAC Hub", 986, 184, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "APAC", "10.20.0.0/16", symbol: "H")
            .AddNode("anz", "10.20.2.0/24", 930, 300, TopologyNodeKind.NetworkSegment, TopologyHealthStatus.Critical, "APAC", "Overlapping", symbol: "NET")
            .AddNode("india", "10.20.3.0/24", 1060, 300, TopologyNodeKind.NetworkSegment, TopologyHealthStatus.Healthy, "APAC", "Healthy", symbol: "NET")
            .AddNode("bh-1", "Bridgehead DC 1", 380, 560, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, null, "172.16.0.0/16", symbol: "BH")
            .AddNode("bh-2", "Bridgehead DC 2", 640, 560, TopologyNodeKind.Server, TopologyHealthStatus.Critical, null, "172.16.1.0/16", symbol: "BH")
            .AddNode("bh-3", "Bridgehead DC 3", 900, 560, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, null, "172.16.2.0/16", symbol: "BH")
            .AddEdge("amer-emea", "amer-hub", "emea-hub", "MPLS", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "$1.20 / Mbps", tertiaryLabel: "24 ms")
            .AddEdge("emea-apac", "emea-hub", "apac-hub", "MPLS", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "$1.35 / Mbps", tertiaryLabel: "82 ms")
            .AddEdge("apac-anz", "apac-hub", "anz", "MPLS", TopologyEdgeKind.Mapping, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "$1.10 / Mbps", tertiaryLabel: "62 ms")
            .AddEdge("amer-bh", "na-east", "bh-1", "mapped", TopologyEdgeKind.Mapping, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("emea-bh", "eu-east", "bh-2", "degraded", TopologyEdgeKind.Mapping, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("apac-bh", "india", "bh-3", "mapped", TopologyEdgeKind.Mapping, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgePorts("amer-emea", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("emea-apac", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("apac-anz", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgeRouteLane("apac-anz", 18)
            .WithEdgePorts("amer-bh", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgePorts("emea-bh", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgePorts("apac-bh", TopologyEdgePort.Bottom, TopologyEdgePort.Top)
            .WithEdgeLineStyle("amer-bh", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("emea-bh", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("apac-bh", TopologyEdgeLineStyle.Dashed)
            .WithGroupSymbol("AMER", "region")
            .WithGroupSymbol("EMEA", "region")
            .WithGroupSymbol("APAC", "region")
            .WithGroupColor("AMER", AmerColor)
            .WithGroupColor("EMEA", EmeaColor)
            .WithGroupColor("APAC", ApacColor)
            .WithNodeColor("amer-hub", AmerColor)
            .WithNodeColor("emea-hub", EmeaColor)
            .WithNodeColor("apac-hub", ApacColor)
            .WithNodeColor("anz", ApacColor);
    }

    private static TopologyChart BuildDcConnectivityMap() {
        return TopologyChart.Create()
            .WithId("visual-dc-connectivity-map")
            .WithTitle("Domain Controller Connectivity")
            .WithSubtitle("Selected domain controller and connected partners with service health paths.")
            .WithViewport(1180, 560, 28)
            .WithLegend(TopologyLegend.Default().AddNodeKind("Domain Controller", TopologyNodeKind.Server, symbol: "DC").AddNodeKind("Global Catalog", TopologyNodeKind.Server, symbol: "GC").AddEdgeKind("Connection", TopologyEdgeKind.Connectivity))
            .AddNode("na-dc01", "NA-DC01", 510, 240, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, null, "PDC, RID", "/dcs/na-dc01", "Selected DC", symbol: "GC")
            .AddNode("na-dc02", "NA-DC02", 160, 132, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, null, "Inbound", symbol: "DC")
            .AddNode("na-dc03", "NA-DC03", 160, 218, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, null, "Inbound", symbol: "DC")
            .AddNode("na-rodc01", "NA-RODC01", 160, 304, TopologyNodeKind.Server, TopologyHealthStatus.Unknown, null, "Branch", symbol: "RO")
            .AddNode("eu-dc01", "EU-DC01", 850, 132, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, null, "Schema Master", symbol: "GC")
            .AddNode("ap-dc01", "AP-DC01", 850, 218, TopologyNodeKind.Server, TopologyHealthStatus.Warning, null, "High lag", symbol: "DC")
            .AddNode("na-dc04", "NA-DC04", 850, 304, TopologyNodeKind.Server, TopologyHealthStatus.Critical, null, "LDAP down", symbol: "DC")
            .AddEdge("na-dc02-na-dc01", "na-dc02", "na-dc01", "LDAP OK", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "Kerberos OK")
            .AddEdge("na-dc03-na-dc01", "na-dc03", "na-dc01", "DNS OK", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "RPC")
            .AddEdge("rodc-na-dc01", "na-rodc01", "na-dc01", "RODC", TopologyEdgeKind.AuthenticationPath, TopologyHealthStatus.Unknown, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "No recent bind")
            .AddEdge("na-dc01-eu-dc01", "na-dc01", "eu-dc01", "72 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "RPC")
            .AddEdge("na-dc01-ap-dc01", "na-dc01", "ap-dc01", "168 ms", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "Queue 18")
            .AddEdge("na-dc01-na-dc04", "na-dc01", "na-dc04", "LDAP fail", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "DNS OK")
            .WithEdgePorts("na-dc02-na-dc01", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("na-dc03-na-dc01", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("rodc-na-dc01", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("na-dc01-eu-dc01", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("na-dc01-ap-dc01", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("na-dc01-na-dc04", TopologyEdgePort.Right, TopologyEdgePort.Left);
    }

    private static TopologyChart BuildAdSitesHierarchy() {
        return TopologyChart.Create()
            .WithId("visual-ad-sites-hierarchy")
            .WithTitle("AD Sites Topology")
            .WithSubtitle("Hub-and-branch hierarchy with primary and backup site links.")
            .WithViewport(1180, 560, 28)
            .WithLegend(TopologyLegend.Default().AddNodeKind("Site", TopologyNodeKind.Branch, symbol: "S").AddNodeKind("Hub Site", TopologyNodeKind.Hub, symbol: "H").AddEdgeKind("Primary Link", TopologyEdgeKind.Link))
            .AddNode("amer-hub", "AMER-HUB", 160, 120, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, null, "New York", symbol: "H")
            .AddNode("emea-hub", "EMEA-HUB", 500, 120, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, null, "London", symbol: "H")
            .AddNode("apac-hub", "APAC-HUB", 840, 120, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, null, "Singapore", symbol: "H")
            .AddNode("dallas", "Dallas", 80, 270, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, null, "AMER", symbol: "S")
            .AddNode("chicago", "Chicago", 190, 270, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, null, "AMER", symbol: "S")
            .AddNode("miami", "Miami", 300, 270, TopologyNodeKind.Branch, TopologyHealthStatus.Warning, null, "AMER", symbol: "S")
            .AddNode("frankfurt", "Frankfurt", 430, 270, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, null, "EMEA", symbol: "S")
            .AddNode("paris", "Paris", 540, 270, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, null, "EMEA", symbol: "S")
            .AddNode("sydney", "Sydney", 770, 270, TopologyNodeKind.Branch, TopologyHealthStatus.Critical, null, "APAC", symbol: "S")
            .AddNode("tokyo", "Tokyo", 880, 270, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, null, "APAC", symbol: "S")
            .AddEdge("amer-emea", "amer-hub", "emea-hub", "72 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight)
            .AddEdge("emea-apac", "emea-hub", "apac-hub", "168 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight)
            .AddEdge("amer-dallas", "amer-hub", "dallas", "IP", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy)
            .AddEdge("amer-chicago", "amer-hub", "chicago", "IP", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy)
            .AddEdge("amer-miami", "amer-hub", "miami", "backup", TopologyEdgeKind.Link, TopologyHealthStatus.Warning)
            .AddEdge("emea-frankfurt", "emea-hub", "frankfurt", "IP", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy)
            .AddEdge("emea-paris", "emea-hub", "paris", "IP", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy)
            .AddEdge("apac-sydney", "apac-hub", "sydney", "down", TopologyEdgeKind.Link, TopologyHealthStatus.Critical)
            .AddEdge("apac-tokyo", "apac-hub", "tokyo", "IP", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy)
            .WithEdgePorts("amer-emea", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("emea-apac", TopologyEdgePort.Right, TopologyEdgePort.Left);
    }

    private static TopologyChart BuildReplicationHealthHub() {
        var chart = TopologyChart.Create()
            .WithId("visual-replication-health-hub")
            .WithTitle("Replication Health")
            .WithSubtitle("Compact hub view with dense site markers and critical replication paths.")
            .WithViewport(1120, 560, 28)
            .WithLegend(TopologyLegend.Default().AddNodeKind("Site", TopologyNodeKind.Location, symbol: "S").AddNodeKind("Domain Controller", TopologyNodeKind.Server, symbol: "DC").AddEdgeKind("Replication", TopologyEdgeKind.Replication))
            .AddNode("cloud", "Inter-Forest", 520, 250, TopologyNodeKind.Cloud, TopologyHealthStatus.Healthy, null, "Hub", symbol: "CL")
            .AddGroup("HQ", "HQ-NYC (32 DCs)", 430, 80, 250, 110, TopologyHealthStatus.Healthy)
            .AddGroup("LON", "LON-London (12 DCs)", 90, 250, 250, 110, TopologyHealthStatus.Healthy)
            .AddGroup("SFO", "SFO-SanFrancisco (11 DCs)", 780, 250, 250, 110, TopologyHealthStatus.Healthy)
            .AddGroup("AMS", "AMS-Amsterdam (9 DCs)", 170, 410, 250, 110, TopologyHealthStatus.Warning)
            .AddGroup("SIN", "SIN-Singapore (8 DCs)", 700, 410, 250, 110, TopologyHealthStatus.Critical);

        AddSiteDots(chart, "HQ", 470, 132, 8, TopologyHealthStatus.Healthy);
        AddSiteDots(chart, "LON", 130, 302, 6, TopologyHealthStatus.Healthy);
        AddSiteDots(chart, "SFO", 820, 302, 6, TopologyHealthStatus.Healthy);
        AddSiteDots(chart, "AMS", 210, 462, 5, TopologyHealthStatus.Warning);
        AddSiteDots(chart, "SIN", 740, 462, 5, TopologyHealthStatus.Critical);
        chart
            .AddEdge("cloud-hq", "cloud", "HQ-1", "healthy", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Orthogonal)
            .AddEdge("cloud-lon", "cloud", "LON-1", "healthy", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("cloud-sfo", "cloud", "SFO-1", "healthy", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("cloud-ams", "cloud", "AMS-1", "warning", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("cloud-sin", "cloud", "SIN-1", "critical", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Straight);
        return chart;
    }

    private static TopologyChart BuildDirectoryHealthReplication() {
        return TopologyChart.Create()
            .WithId("visual-directory-health-replication")
            .WithTitle("AD Site / Replication Topology")
            .WithSubtitle("Directory health topology with compact DC lists and cross-site replication paths.")
            .WithViewport(1080, 520, 28)
            .WithLegend(TopologyLegend.Default().AddNodeKind("Site", TopologyNodeKind.Location, symbol: "S").AddNodeKind("Domain Controller", TopologyNodeKind.Server, symbol: "DC").AddEdgeKind("Replication", TopologyEdgeKind.Replication))
            .AddGroup("KATOWICE-1", "KATOWICE-1", 250, 130, 240, 260, TopologyHealthStatus.Healthy, "5 DCs")
            .AddGroup("KATOWICE-2", "KATOWICE-2", 660, 130, 240, 260, TopologyHealthStatus.Warning, "3 DCs")
            .AddNode("dc-ktw1-01", "DC-KTW1-01", 300, 190, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "KATOWICE-1", "192.168.241.4", symbol: "DC")
            .AddNode("dc-ktw1-02", "DC-KTW1-02", 300, 260, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "KATOWICE-1", "192.168.241.5", symbol: "DC")
            .AddNode("dc-ktw1-04", "DC-KTW1-04", 300, 330, TopologyNodeKind.Server, TopologyHealthStatus.Warning, "KATOWICE-1", "Kerberos warn", symbol: "DC")
            .AddNode("dc-ktw2-01", "DC-KTW2-01", 710, 190, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "KATOWICE-2", "192.168.242.4", symbol: "DC")
            .AddNode("dc-ktw2-02", "DC-KTW2-02", 710, 260, TopologyNodeKind.Server, TopologyHealthStatus.Warning, "KATOWICE-2", "Replication lag", symbol: "DC")
            .AddNode("dc-ktw2-03", "DC-KTW2-03", 710, 330, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "KATOWICE-2", "192.168.242.6", symbol: "DC")
            .AddEdge("rep-1", "dc-ktw1-01", "dc-ktw2-01", "healthy", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "1m ago")
            .AddEdge("rep-2", "dc-ktw1-02", "dc-ktw2-02", "warning", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "27m lag")
            .AddEdge("rep-3", "dc-ktw1-04", "dc-ktw2-03", "healthy", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "2m ago")
            .WithEdgePorts("rep-1", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("rep-2", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("rep-3", TopologyEdgePort.Right, TopologyEdgePort.Left);
    }

    private static TopologyChart BuildServiceDependencyMap() {
        return TopologyChart.Create()
            .WithId("visual-service-dependency-map")
            .WithTitle("Service Dependency Map")
            .WithSubtitle("Generic service topology: upstream, service tier, stores, queues, ownership, and failed dependencies.")
            .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
            .WithViewport(1180, 600, 28)
            .WithLegend(null)
            .AddNode("dns", "DNS", 0, 0, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, symbol: "DNS")
            .AddNode("ldap", "LDAP", 0, 0, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, symbol: "LDAP")
            .AddNode("kerberos", "Kerberos", 0, 0, TopologyNodeKind.Service, TopologyHealthStatus.Warning, symbol: "KRB")
            .AddNode("api", "Directory API", 0, 0, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, symbol: "API")
            .AddNode("queue", "Probe Queue", 0, 0, TopologyNodeKind.Queue, TopologyHealthStatus.Warning, symbol: "Q")
            .AddNode("sql", "State Store", 0, 0, TopologyNodeKind.Database, TopologyHealthStatus.Critical, symbol: "SQL")
            .AddNode("team", "Ops Team", 0, 0, TopologyNodeKind.Team, TopologyHealthStatus.Healthy, symbol: "TM")
            .AddEdge("dns-api", "dns", "api", "100%", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Forward)
            .AddEdge("ldap-api", "ldap", "api", "98.4%", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Forward)
            .AddEdge("krb-api", "kerberos", "api", "96.2%", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward)
            .AddEdge("api-queue", "api", "queue", "1,287", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Warning, TopologyDirection.Forward)
            .AddEdge("api-sql", "api", "sql", "P95 412 ms", TopologyEdgeKind.Dependency, TopologyHealthStatus.Critical, TopologyDirection.Forward)
            .AddEdge("team-api", "team", "api", "owns", TopologyEdgeKind.Ownership, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Curved);
    }

    private static Chart BuildGeographicRegionMap() {
        return Chart.Create()
            .WithTitle("Geographic Topology View")
            .WithSubtitle("Map-backed region markers and WAN route overlays for HtmlForgeX-hosted region panels")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(1180, 620)
            .WithLegend(false)
            .WithMapViewport(ChartMapViewport.World())
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + " sites")
            .AddDottedMap("Regions", new[] {
                new ChartMapPoint("AMER", -98.5795, 39.8283, 47, ChartColor.FromRgb(22, 163, 74)),
                new ChartMapPoint("EMEA", 10.0, 50.0, 56, ChartColor.FromRgb(37, 99, 235)),
                new ChartMapPoint("APAC", 103.8198, 1.3521, 39, ChartColor.FromRgb(124, 58, 237))
            })
            .AddMapRouteBetweenPoints("AMER to EMEA 68 ms", "AMER", "EMEA", ChartColor.FromRgb(22, 163, 74))
            .AddMapRouteBetweenPoints("EMEA to APAC 92 ms", "EMEA", "APAC", ChartColor.FromRgb(239, 68, 68))
            .AddMapRouteBetweenPoints("AMER to APAC 142 ms", "AMER", "APAC", ChartColor.FromRgb(245, 158, 11));
    }

    private static Chart BuildSiteDistributionMap() {
        return Chart.Create()
            .WithTitle("Sites by Geography")
            .WithSubtitle("Weighted dotted map with healthy/degraded/down site markers")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(1180, 620)
            .WithLegend(false)
            .WithMapViewport(ChartMapViewport.World())
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + " sites")
            .AddDottedMap("Sites", new[] {
                new ChartMapPoint("New York", -74.006, 40.7128, 18, ChartColor.FromRgb(22, 163, 74)),
                new ChartMapPoint("Chicago", -87.6298, 41.8781, 8, ChartColor.FromRgb(245, 158, 11)),
                new ChartMapPoint("London", -0.1276, 51.5072, 22, ChartColor.FromRgb(37, 99, 235)),
                new ChartMapPoint("Frankfurt", 8.6821, 50.1109, 14, ChartColor.FromRgb(245, 158, 11)),
                new ChartMapPoint("Singapore", 103.8198, 1.3521, 15, ChartColor.FromRgb(124, 58, 237)),
                new ChartMapPoint("Sydney", 151.2093, -33.8688, 8, ChartColor.FromRgb(239, 68, 68))
            })
            .AddMapRouteBetweenPoints("New York to London", "New York", "London", ChartColor.FromRgb(22, 163, 74))
            .AddMapRouteBetweenPoints("London to Singapore", "London", "Singapore", ChartColor.FromRgb(245, 158, 11))
            .AddMapRouteBetweenPoints("Singapore to Sydney", "Singapore", "Sydney", ChartColor.FromRgb(239, 68, 68));
    }

    private static Chart BuildWanLatencyMap() {
        return Chart.Create()
            .WithTitle("Top Cross-Region Links")
            .WithSubtitle("WAN latency routes rendered as a reusable map chart")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(1180, 620)
            .WithLegend(false)
            .WithMapViewport(ChartMapViewport.World())
            .WithDataLabels()
            .AddDottedMap("Hubs", new[] {
                new ChartMapPoint("Dallas", -96.797, 32.7767, ChartColor.FromRgb(22, 163, 74)),
                new ChartMapPoint("Frankfurt", 8.6821, 50.1109, ChartColor.FromRgb(37, 99, 235)),
                new ChartMapPoint("Singapore", 103.8198, 1.3521, ChartColor.FromRgb(124, 58, 237)),
                new ChartMapPoint("Sydney", 151.2093, -33.8688, ChartColor.FromRgb(239, 68, 68))
            })
            .AddMapRouteBetweenPoints("Dallas to Frankfurt 68 ms", "Dallas", "Frankfurt", ChartColor.FromRgb(22, 163, 74))
            .AddMapRouteBetweenPoints("Frankfurt to Singapore 92 ms", "Frankfurt", "Singapore", ChartColor.FromRgb(239, 68, 68))
            .AddMapRouteBetweenPoints("Singapore to Sydney 142 ms", "Singapore", "Sydney", ChartColor.FromRgb(245, 158, 11));
    }

    private static void AddSiteDots(TopologyChart chart, string prefix, double x, double y, int count, TopologyHealthStatus status) {
        for (var i = 0; i < count; i++) {
            var col = i % 4;
            var row = i / 4;
            chart.AddNode(prefix + "-" + (i + 1).ToString(CultureInfo.InvariantCulture), "DC " + (i + 1).ToString(CultureInfo.InvariantCulture), x + col * 42, y + row * 34, TopologyNodeKind.Server, status, prefix, symbol: "DC")
                .WithNodeDisplay(prefix + "-" + (i + 1).ToString(CultureInfo.InvariantCulture), TopologyNodeDisplayMode.Dot, (i + 1).ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void SaveTopology(string target, List<VisualArtifact> artifacts, string name, TopologyChart chart, string title, string notes, TopologyRenderOptions? options = null) {
        chart.SaveSvg(Path.Combine(target, name + ".svg"), options);
        chart.SaveHtml(Path.Combine(target, name + ".html"), options);
        chart.SavePng(Path.Combine(target, name + ".png"), options);
        artifacts.Add(new VisualArtifact(name, title, "topology", notes));
    }

    private static void SaveMap(string target, List<VisualArtifact> artifacts, string name, Chart chart, string title, string notes) {
        chart.WithPngOutputScale(ChartPngOutputScale.Retina);
        chart.SaveSvg(Path.Combine(target, name + ".svg"));
        chart.SaveHtml(Path.Combine(target, name + ".html"));
        chart.SavePng(Path.Combine(target, name + ".png"));
        artifacts.Add(new VisualArtifact(name, title, "map", notes));
    }

    private static void WriteManifest(string target, IReadOnlyList<VisualArtifact> artifacts) {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"name\": \"ChartForgeX topology visual coverage\",");
        sb.AppendLine("  \"host\": \"HtmlForgeX-ready SVG/HTML/PNG artifacts\",");
        sb.AppendLine("  \"interactiveContract\": \"Inline SVG elements expose ids, CSS classes, hrefs, title tooltips, and data-cfx/data-node/data-edge hooks. Complete topology HTML pages add click selection and cfx-topology-select/cfx-topology-clear events; hosts can disable that script and own the behavior.\",");
        sb.AppendLine("  \"artifacts\": [");
        for (var i = 0; i < artifacts.Count; i++) {
            var artifact = artifacts[i];
            sb.AppendLine("    {");
            sb.AppendLine("      \"name\": \"" + EscapeJson(artifact.Name) + "\",");
            sb.AppendLine("      \"title\": \"" + EscapeJson(artifact.Title) + "\",");
            sb.AppendLine("      \"kind\": \"" + EscapeJson(artifact.Kind) + "\",");
            sb.AppendLine("      \"svg\": \"" + EscapeJson(artifact.Name + ".svg") + "\",");
            sb.AppendLine("      \"html\": \"" + EscapeJson(artifact.Name + ".html") + "\",");
            sb.AppendLine("      \"png\": \"" + EscapeJson(artifact.Name + ".png") + "\",");
            sb.AppendLine("      \"notes\": \"" + EscapeJson(artifact.Notes) + "\"");
            sb.Append("    }");
            sb.AppendLine(i == artifacts.Count - 1 ? string.Empty : ",");
        }

        sb.AppendLine("  ]");
        sb.AppendLine("}");
        File.WriteAllText(Path.Combine(target, "visual-capability-manifest.json"), sb.ToString(), Encoding.UTF8);
    }

    private static void WriteCoverageIndex(string target, IReadOnlyList<VisualArtifact> artifacts) {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("<title>ChartForgeX Topology Visual Coverage</title>");
        sb.AppendLine("<style>body{margin:0;background:#f8fafc;color:#0f172a;font-family:Inter,Segoe UI,system-ui,sans-serif;padding:24px}main{max-width:1280px;margin:0 auto}h1{font-size:24px;margin:0 0 8px}.lead{color:#475569;margin:0 0 20px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:16px}.card{background:white;border:1px solid #dbe3ef;border-radius:10px;padding:14px;box-shadow:0 12px 28px rgba(15,23,42,.06)}.card h2{font-size:15px;margin:0 0 8px}.kind{font-size:11px;font-weight:700;text-transform:uppercase;color:#2563eb}.notes{font-size:12px;color:#475569;min-height:48px}.links{display:flex;gap:10px;flex-wrap:wrap}.links a{font-size:12px;color:#2563eb;text-decoration:none;font-weight:700}.preview{display:block;width:100%;height:auto;border:1px solid #e2e8f0;border-radius:8px;margin:10px 0;background:white}</style>");
        sb.AppendLine("</head><body><main>");
        sb.AppendLine("<h1>ChartForgeX Topology Visual Coverage</h1>");
        sb.AppendLine("<p class=\"lead\">Generated SVG, HTML, and PNG artifacts for topology and map-based visuals. Topology HTML pages include lightweight selection events; SVG and map outputs stay host-ready through stable metadata hooks for HtmlForgeX.</p>");
        sb.AppendLine("<div class=\"grid\">");
        foreach (var artifact in artifacts) {
            sb.AppendLine("<article class=\"card\">");
            sb.AppendLine("<div class=\"kind\">" + EscapeHtml(artifact.Kind) + "</div>");
            sb.AppendLine("<h2>" + EscapeHtml(artifact.Title) + "</h2>");
            sb.AppendLine("<p class=\"notes\">" + EscapeHtml(artifact.Notes) + "</p>");
            sb.AppendLine("<img class=\"preview\" loading=\"lazy\" src=\"" + EscapeHtml(artifact.Name) + ".svg\" alt=\"" + EscapeHtml(artifact.Title) + "\">");
            sb.AppendLine("<div class=\"links\"><a href=\"" + EscapeHtml(artifact.Name) + ".svg\">SVG</a><a href=\"" + EscapeHtml(artifact.Name) + ".html\">HTML</a><a href=\"" + EscapeHtml(artifact.Name) + ".png\">PNG</a></div>");
            sb.AppendLine("</article>");
        }

        sb.AppendLine("</div>");
        sb.AppendLine("</main></body></html>");
        File.WriteAllText(Path.Combine(target, "visual-coverage.html"), sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeHtml(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string EscapeJson(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");

    private readonly struct VisualArtifact {
        public VisualArtifact(string name, string title, string kind, string notes) {
            Name = name;
            Title = title;
            Kind = kind;
            Notes = notes;
        }

        public readonly string Name;
        public readonly string Title;
        public readonly string Kind;
        public readonly string Notes;
    }
}
