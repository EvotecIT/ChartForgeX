namespace ChartForgeX.Topology;

/// <summary>
/// Provides sample topology charts for documentation, demos, and tests.
/// </summary>
public static class TopologyDemoCharts {
    /// <summary>
    /// Builds a sample site topology chart.
    /// </summary>
    /// <returns>A topology chart with regions, sites, bridgeheads, links, labels, and statuses.</returns>
    public static TopologyChart SiteTopologyDemo() {
        var chart = Create("site-topology", "Site Topology", "Sample regional site topology with hubs, branches, bridgeheads, and site-link health.");
        AddGroup(chart, "AMER", "AMER", "47 sites", TopologyHealthStatus.Healthy, 60, 110, 310, 330, "/topology/regions/amer");
        AddGroup(chart, "EMEA", "EMEA", "56 sites", TopologyHealthStatus.Warning, 435, 110, 330, 330, "/topology/regions/emea");
        AddGroup(chart, "APAC", "APAC", "39 sites", TopologyHealthStatus.Critical, 835, 110, 310, 330, "/topology/regions/apac");

        AddNode(chart, "amer-hub", "AMER Hub", "Hub site", TopologyNodeKind.HubSite, TopologyHealthStatus.Healthy, "AMER", 160, 170, "/topology/sites/amer-hub");
        AddNode(chart, "nva-east", "NVA East", "Branch", TopologyNodeKind.BranchSite, TopologyHealthStatus.Healthy, "AMER", 95, 275, "/topology/sites/nva-east");
        AddNode(chart, "chi-dc", "CHI DC", "Bridgehead", TopologyNodeKind.BridgeheadServer, TopologyHealthStatus.Warning, "AMER", 230, 275, "/topology/sites/chi-dc");
        AddNode(chart, "lon-hub", "EMEA Hub", "Hub site", TopologyNodeKind.HubSite, TopologyHealthStatus.Healthy, "EMEA", 540, 170, "/topology/sites/lon-hub");
        AddNode(chart, "eu-west", "EU West", "Branch", TopologyNodeKind.BranchSite, TopologyHealthStatus.Healthy, "EMEA", 465, 275, "/topology/sites/eu-west");
        AddNode(chart, "tr-branch", "TR Branch", "High latency", TopologyNodeKind.BranchSite, TopologyHealthStatus.Warning, "EMEA", 615, 275, "/topology/sites/tr-branch");
        AddNode(chart, "apac-hub", "APAC Hub", "Hub site", TopologyNodeKind.HubSite, TopologyHealthStatus.Healthy, "APAC", 930, 170, "/topology/sites/apac-hub");
        AddNode(chart, "anz", "ANZ", "Critical", TopologyNodeKind.BranchSite, TopologyHealthStatus.Critical, "APAC", 875, 275, "/topology/sites/anz");
        AddNode(chart, "in-india", "IN India", "Unknown", TopologyNodeKind.BranchSite, TopologyHealthStatus.Unknown, "APAC", 1010, 275, "/topology/sites/in-india");
        AddNode(chart, "bh-1", "Bridgehead DC 1", "Healthy", TopologyNodeKind.BridgeheadServer, TopologyHealthStatus.Healthy, null, 415, 510, "/topology/bridgeheads/bh-1");
        AddNode(chart, "bh-2", "Bridgehead DC 2", "Degraded", TopologyNodeKind.BridgeheadServer, TopologyHealthStatus.Critical, null, 650, 510, "/topology/bridgeheads/bh-2");

        AddEdge(chart, "amer-emea", "amer-hub", "lon-hub", "24 ms", null, TopologyEdgeKind.SiteLink, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "/topology/links/amer-emea");
        AddEdge(chart, "emea-apac", "lon-hub", "apac-hub", "82 ms", null, TopologyEdgeKind.SiteLink, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "/topology/links/emea-apac");
        AddEdge(chart, "apac-anz", "apac-hub", "anz", "142 ms", null, TopologyEdgeKind.SiteLink, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "/topology/links/apac-anz");
        AddEdge(chart, "amer-bh", "nva-east", "bh-1", "32 ms", "bridgehead", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "/topology/links/amer-bh");
        AddEdge(chart, "bh-path", "bh-1", "bh-2", "68 ms", "queue 44", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "/topology/links/bh-path");
        return chart;
    }

    /// <summary>
    /// Builds a sample replication mesh chart.
    /// </summary>
    /// <returns>A topology chart with directional replication paths.</returns>
    public static TopologyChart ReplicationMeshDemo() {
        var chart = Create("replication-mesh", "Replication Mesh", "Sample deterministic replication paths with lag, queue, and last-success labels.");
        AddGroup(chart, "HQ-NYC", "HQ-NYC", "2 DCs", TopologyHealthStatus.Healthy, 55, 120, 205, 135, "/replication/sites/hq-nyc");
        AddGroup(chart, "LON-LON", "LON-LON", "2 DCs", TopologyHealthStatus.Healthy, 350, 120, 205, 135, "/replication/sites/lon-lon");
        AddGroup(chart, "FRA-FRA", "FRA-FRA", "2 DCs", TopologyHealthStatus.Warning, 645, 120, 205, 135, "/replication/sites/fra-fra");
        AddGroup(chart, "SFO-SFO", "SFO-SFO", "2 DCs", TopologyHealthStatus.Healthy, 180, 410, 225, 135, "/replication/sites/sfo-sfo");
        AddGroup(chart, "SIN-SIN", "SIN-SIN", "2 DCs", TopologyHealthStatus.Critical, 650, 410, 225, 135, "/replication/sites/sin-sin");
        AddDcPair(chart, "nyc", "HQ-NYC", 83, 178);
        AddDcPair(chart, "lon", "LON-LON", 378, 178);
        AddDcPair(chart, "fra", "FRA-FRA", 673, 178);
        AddDcPair(chart, "sfo", "SFO-SFO", 215, 468);
        AddDcPair(chart, "sin", "SIN-SIN", 685, 468);
        AddEdge(chart, "nyc-lon", "nyc-dc1", "lon-dc1", "105 ms", "Q:312; 12m ago", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "/replication/paths/nyc-lon");
        AddEdge(chart, "lon-fra", "lon-dc2", "fra-dc1", "156 ms", "Q:842; 3m ago", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "/replication/paths/lon-fra");
        AddEdge(chart, "fra-sin", "fra-dc2", "sin-dc1", "238 ms", "Q:1124; 2m ago", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "/replication/paths/fra-sin");
        AddEdge(chart, "sfo-sin", "sfo-dc2", "sin-dc1", "214 ms", "Q:1552; 1m ago", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Straight, "/replication/paths/sfo-sin");
        AddEdge(chart, "lon-sfo", "lon-dc1", "sfo-dc1", "118 ms", "Q:301; 10m ago", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Orthogonal, "/replication/paths/lon-sfo");
        return chart;
    }

    /// <summary>
    /// Builds a sample subnet and site-link map.
    /// </summary>
    /// <returns>A topology chart with site links and subnet mappings.</returns>
    public static TopologyChart SubnetsSiteLinksDemo() {
        var chart = Create("subnets-site-links", "Subnets and Site Links", "Sample mapping of sites, subnet groups, bridgeheads, costs, transport, and mapping issues.");
        AddGroup(chart, "AMER", "AMER", "47 sites", TopologyHealthStatus.Healthy, 55, 120, 315, 335, "/subnets/regions/amer");
        AddGroup(chart, "EMEA", "EMEA", "56 sites", TopologyHealthStatus.Warning, 435, 120, 315, 335, "/subnets/regions/emea");
        AddGroup(chart, "APAC", "APAC", "39 sites", TopologyHealthStatus.Critical, 815, 120, 315, 335, "/subnets/regions/apac");
        AddNode(chart, "amer-hub", "AMER Hub", "10.0.0.0/16", TopologyNodeKind.HubSite, TopologyHealthStatus.Healthy, "AMER", 145, 175, "/subnets/sites/amer-hub");
        AddNode(chart, "na-west", "NA-West", "10.1.0.0/24", TopologyNodeKind.SubnetGroup, TopologyHealthStatus.Healthy, "AMER", 90, 290, "/subnets/10.1.0.0-24");
        AddNode(chart, "na-east", "NA-East", "10.2.0.0/24", TopologyNodeKind.SubnetGroup, TopologyHealthStatus.Healthy, "AMER", 225, 290, "/subnets/10.2.0.0-24");
        AddNode(chart, "emea-hub", "EMEA Hub", "10.10.0.0/16", TopologyNodeKind.HubSite, TopologyHealthStatus.Healthy, "EMEA", 525, 175, "/subnets/sites/emea-hub");
        AddNode(chart, "eu-east", "EU East", "10.10.3.0/24", TopologyNodeKind.SubnetGroup, TopologyHealthStatus.Warning, "EMEA", 605, 290, "/subnets/10.10.3.0-24");
        AddNode(chart, "apac-hub", "APAC Hub", "10.20.0.0/16", TopologyNodeKind.HubSite, TopologyHealthStatus.Healthy, "APAC", 905, 175, "/subnets/sites/apac-hub");
        AddNode(chart, "anz-subnet", "10.20.2.0/24", "Overlapping", TopologyNodeKind.Subnet, TopologyHealthStatus.Critical, "APAC", 855, 290, "/subnets/10.20.2.0-24");
        AddNode(chart, "orphan", "172.31.50.0/24", "Orphaned", TopologyNodeKind.Subnet, TopologyHealthStatus.Warning, null, 515, 520, "/subnets/172.31.50.0-24");
        AddNode(chart, "bridgehead", "Bridgehead DC", "172.16.0.0/16", TopologyNodeKind.BridgeheadServer, TopologyHealthStatus.Healthy, null, 745, 520, "/subnets/bridgeheads/1");
        AddEdge(chart, "amer-emea", "amer-hub", "emea-hub", "MPLS $1.20", "24 ms", TopologyEdgeKind.SiteLink, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "/subnets/links/amer-emea");
        AddEdge(chart, "emea-apac", "emea-hub", "apac-hub", "MPLS $1.35", "82 ms", TopologyEdgeKind.SiteLink, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Straight, "/subnets/links/emea-apac");
        AddEdge(chart, "apac-anz", "apac-hub", "anz-subnet", "MPLS $1.10", "62 ms", TopologyEdgeKind.SubnetMapping, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "/subnets/links/apac-anz");
        AddEdge(chart, "orphan-bh", "orphan", "bridgehead", "unmapped", "needs owner", TopologyEdgeKind.SubnetMapping, TopologyHealthStatus.Unknown, TopologyDirection.None, TopologyEdgeRouting.Straight, "/subnets/issues/orphan-bh");
        return chart;
    }

    /// <summary>
    /// Builds an abstract geographic-style topology chart.
    /// </summary>
    /// <returns>A topology chart with region positions and curved cross-region links.</returns>
    public static TopologyChart GeographicTopologyDemo() {
        var chart = Create("geographic-topology", "Geographic Topology", "Abstract region placement with clustered child sites and WAN latency labels.");
        AddNode(chart, "amer", "AMER", "47 sites", TopologyNodeKind.Region, TopologyHealthStatus.Healthy, null, 180, 270, "/geo/regions/amer");
        AddNode(chart, "emea", "EMEA", "56 sites", TopologyNodeKind.Region, TopologyHealthStatus.Healthy, null, 520, 210, "/geo/regions/emea");
        AddNode(chart, "apac", "APAC", "39 sites", TopologyNodeKind.Region, TopologyHealthStatus.Critical, null, 860, 330, "/geo/regions/apac");
        AddNode(chart, "nva", "NVA East", "Healthy", TopologyNodeKind.BranchSite, TopologyHealthStatus.Healthy, null, 120, 410, "/geo/sites/nva");
        AddNode(chart, "chi", "CHI DC", "Warning", TopologyNodeKind.DomainController, TopologyHealthStatus.Warning, null, 250, 415, "/geo/sites/chi");
        AddNode(chart, "uk", "UK DC", "Healthy", TopologyNodeKind.DomainController, TopologyHealthStatus.Healthy, null, 490, 360, "/geo/sites/uk");
        AddNode(chart, "de", "DE Branch", "Warning", TopologyNodeKind.BranchSite, TopologyHealthStatus.Warning, null, 625, 355, "/geo/sites/de");
        AddNode(chart, "sg", "SG DC", "Healthy", TopologyNodeKind.DomainController, TopologyHealthStatus.Healthy, null, 815, 485, "/geo/sites/sg");
        AddNode(chart, "anz", "ANZ", "Down", TopologyNodeKind.BranchSite, TopologyHealthStatus.Critical, null, 960, 485, "/geo/sites/anz");
        AddEdge(chart, "amer-emea", "amer", "emea", "68 ms", "WAN", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Curved, "/geo/links/amer-emea");
        AddEdge(chart, "emea-apac", "emea", "apac", "92 ms", "WAN", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Critical, TopologyDirection.Bidirectional, TopologyEdgeRouting.Curved, "/geo/links/emea-apac");
        AddEdge(chart, "amer-apac", "amer", "apac", "142 ms", "backup", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Curved, "/geo/links/amer-apac");
        AddEdge(chart, "amer-nva", "amer", "nva", "local", null, TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.None, TopologyEdgeRouting.Curved, "/geo/links/amer-nva");
        AddEdge(chart, "apac-anz", "apac", "anz", "down", null, TopologyEdgeKind.Dependency, TopologyHealthStatus.Critical, TopologyDirection.None, TopologyEdgeRouting.Curved, "/geo/links/apac-anz");
        return chart;
    }

    private static TopologyChart Create(string id, string title, string subtitle) {
        return new TopologyChart {
            Id = id,
            Title = title,
            Subtitle = subtitle,
            Viewport = new TopologyViewport { Width = 1200, Height = 680, Padding = 28 },
            Legend = TopologyLegend.Default(),
            Theme = TopologyTheme.Light()
        };
    }

    private static void AddGroup(TopologyChart chart, string id, string label, string subtitle, TopologyHealthStatus status, double x, double y, double width, double height, string href) {
        chart.Groups.Add(new TopologyGroup { Id = id, Label = label, Subtitle = subtitle, Status = status, X = x, Y = y, Width = width, Height = height, Href = href, Tooltip = label + " topology group" });
    }

    private static void AddNode(TopologyChart chart, string id, string label, string subtitle, TopologyNodeKind kind, TopologyHealthStatus status, string? groupId, double x, double y, string href) {
        chart.Nodes.Add(new TopologyNode { Id = id, Label = label, Subtitle = subtitle, Kind = kind, Status = status, GroupId = groupId, X = x, Y = y, Width = 128, Height = 62, Href = href, Tooltip = label + " (" + status + ")" });
    }

    private static void AddEdge(TopologyChart chart, string id, string source, string target, string label, string? secondaryLabel, TopologyEdgeKind kind, TopologyHealthStatus status, TopologyDirection direction, TopologyEdgeRouting routing, string href) {
        chart.Edges.Add(new TopologyEdge { Id = id, SourceNodeId = source, TargetNodeId = target, Label = label, SecondaryLabel = secondaryLabel, Kind = kind, Status = status, Direction = direction, Routing = routing, Href = href, Tooltip = source + " to " + target + ": " + label });
    }

    private static void AddDcPair(TopologyChart chart, string prefix, string groupId, double x, double y) {
        AddNode(chart, prefix + "-dc1", prefix.ToUpperInvariant() + "-DC1", "DC", TopologyNodeKind.DomainController, TopologyHealthStatus.Healthy, groupId, x, y, "/replication/nodes/" + prefix + "-dc1");
        AddNode(chart, prefix + "-dc2", prefix.ToUpperInvariant() + "-DC2", "DC", TopologyNodeKind.DomainController, TopologyHealthStatus.Healthy, groupId, x + 88, y, "/replication/nodes/" + prefix + "-dc2");
    }
}
