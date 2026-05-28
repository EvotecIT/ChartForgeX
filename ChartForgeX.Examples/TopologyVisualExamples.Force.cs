using System.Globalization;
using ChartForgeX.Topology;

internal static partial class TopologyVisualExamples {
    public static void WriteForceGraph(string target) {
        Directory.CreateDirectory(target);
        var artifacts = new List<VisualArtifact>();
        var options = new TopologyRenderOptions { IncludeLegend = true }
            .WithForceGraphStyle()
            .WithHtmlForceGraphControls()
            .WithFitContentToViewport();
        WriteForceGraphArtifact(target, artifacts, "visual-force-relationship-graph", "Force Relationship Graph", "Moderately dense force-directed relationship graph with low-ink SVG/PNG defaults, HTML search, status/group filtering, zoom, pan, and on-demand edge labels.", BuildForceRelationshipGraph(), options);
        WriteForceGraphArtifact(target, artifacts, "visual-force-busy-relationship-graph", "Busy Force Relationship Graph", "Busy force-directed relationship graph using the relationship solver profile for degree-weighted hub mass, linear repulsion, overlap avoidance, HTML filtering, zoom, pan, and on-demand edge labels.", BuildBusyForceRelationshipGraph(), options);
        var radialOptions = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabels = true }
            .WithForceGraphStyle()
            .WithHtmlForceGraphControls()
            .WithRelationshipRadialFocus("application-03", maxDepth: 2, maxFanout: 9)
            .WithFitContentToViewport();
        radialOptions.IncludeEdgeLabels = true;
        WriteForceGraphArtifact(target, artifacts, "visual-relationship-radial-ego-graph", "Relationship Radial Ego Graph", "Selected application relationship graph with the root in the center, direct conversations around it, second-hop conversations farther out, and capped expansion for large networks.", BuildRelationshipRadialEgoGraph(), radialOptions);
        var largeRadialOptions = new TopologyRenderOptions { IncludeLegend = false }
            .WithForceGraphStyle()
            .WithHtmlForceGraphControls()
            .WithRelationshipRadialFocus("app-root", maxDepth: 2, maxFanout: 40)
            .WithFitContentToViewport();
        WriteForceGraphArtifact(target, artifacts, "visual-relationship-radial-500-ego-graph", "Relationship Radial 500 Ego Graph", "Large 500-node relationship ego graph: all nodes remain present as dots, first-hop conversations spread around the root, second-hop conversations fan out farther, and HTML focus reveals labels on demand.", BuildLargeRelationshipRadialEgoGraph(), largeRadialOptions);
        WriteManifest(target, artifacts);
        WriteCoverageIndex(target, artifacts);
    }

    private static void WriteForceGraphArtifact(string target, List<VisualArtifact> artifacts, string name, string title, string description, TopologyChart chart, TopologyRenderOptions options) {
        Console.WriteLine("Writing " + name + ".svg");
        chart.SaveSvg(Path.Combine(target, name + ".svg"), options);
        Console.WriteLine("Writing " + name + ".html");
        chart.SaveHtml(Path.Combine(target, name + ".html"), options);
        Console.WriteLine("Writing " + name + ".png");
        chart.SavePng(Path.Combine(target, name + ".png"), options);
        artifacts.Add(new VisualArtifact(name, title, "topology", description));
    }

    private static TopologyChart BuildForceRelationshipGraph() {
        var chart = TopologyChart.Create()
            .WithId("visual-force-relationship-graph")
            .WithTitle("Force Relationship Graph")
            .WithSubtitle("Force-directed identity, endpoint, application, data, and owner relationships with graph-first static defaults and interactive HTML filtering.")
            .WithViewport(1280, 760, 28)
            .WithLayout(TopologyLayoutMode.ForceDirected)
            .WithLegend(TopologyLegend.Default()
                .AddNodeKind("Identity", TopologyNodeKind.Person, symbol: "ID")
                .AddNodeKind("Endpoint", TopologyNodeKind.Endpoint, symbol: "PC")
                .AddNodeKind("Application", TopologyNodeKind.Service, symbol: "APP")
                .AddNodeKind("Data", TopologyNodeKind.Database, symbol: "DB")
                .AddNodeKind("Owner", TopologyNodeKind.Team, symbol: "TM")
                .AddEdgeKind("Membership", TopologyEdgeKind.Membership)
                .AddEdgeKind("Dependency", TopologyEdgeKind.Dependency)
                .AddEdgeKind("Data flow", TopologyEdgeKind.DataFlow)
                .AddEdgeKind("Ownership", TopologyEdgeKind.Ownership));

        AddForceCluster(chart, "identity", "Identity", TopologyNodeKind.Person, 10, "#2563EB");
        AddForceCluster(chart, "endpoint", "Endpoints", TopologyNodeKind.Endpoint, 9, "#0891B2");
        AddForceCluster(chart, "application", "Applications", TopologyNodeKind.Service, 11, "#7C3AED");
        AddForceCluster(chart, "data", "Data", TopologyNodeKind.Database, 7, "#059669");
        AddForceCluster(chart, "owner", "Owners", TopologyNodeKind.Team, 6, "#EA580C");

        AddForceRing(chart, "identity", "endpoint", 9, TopologyEdgeKind.AuthenticationPath, "sign-in");
        AddForceRing(chart, "endpoint", "application", 11, TopologyEdgeKind.Dependency, "calls");
        AddForceRing(chart, "application", "data", 10, TopologyEdgeKind.DataFlow, "reads");
        AddForceRing(chart, "owner", "application", 8, TopologyEdgeKind.Ownership, "owns");
        AddForceRing(chart, "owner", "identity", 6, TopologyEdgeKind.Membership, "member");

        chart
            .AddEdge("force-critical-app-data", "application-03", "data-04", "critical dependency", TopologyEdgeKind.Dependency, TopologyHealthStatus.Critical, TopologyDirection.Bidirectional)
            .AddEdge("force-warning-id-app", "identity-07", "application-08", "privileged path", TopologyEdgeKind.AuthenticationPath, TopologyHealthStatus.Warning, TopologyDirection.Forward)
            .AddEdge("force-warning-endpoint-data", "endpoint-06", "data-01", "direct data path", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Warning, TopologyDirection.Forward)
            .AddEdge("force-owner-data", "owner-02", "data-06", "steward", TopologyEdgeKind.Ownership, TopologyHealthStatus.Healthy, TopologyDirection.Forward)
            .WithEdgeEmphasis("force-critical-app-data", TopologyEdgeEmphasis.Strong)
            .WithEdgeEmphasis("force-warning-id-app", TopologyEdgeEmphasis.Strong)
            .WithEdgeEmphasis("force-warning-endpoint-data", TopologyEdgeEmphasis.Strong);

        return chart;
    }

    private static TopologyChart BuildBusyForceRelationshipGraph() {
        var chart = TopologyChart.Create()
            .WithId("visual-force-busy-relationship-graph")
            .WithTitle("Busy Force Relationship Graph")
            .WithSubtitle("Large identity, endpoint, application, data, network, control, and ownership relationship graph using degree-weighted force layout diagnostics.")
            .WithViewport(1500, 920, 32)
            .WithLayout(TopologyLayoutMode.ForceDirected)
            .WithLegend(TopologyLegend.Default()
                .AddNodeKind("Identity", TopologyNodeKind.Person, symbol: "ID")
                .AddNodeKind("Endpoint", TopologyNodeKind.Endpoint, symbol: "PC")
                .AddNodeKind("Application", TopologyNodeKind.Service, symbol: "APP")
                .AddNodeKind("Data", TopologyNodeKind.Database, symbol: "DB")
                .AddNodeKind("Network", TopologyNodeKind.Network, symbol: "NET")
                .AddNodeKind("Control", TopologyNodeKind.Process, symbol: "CTL")
                .AddNodeKind("Owner", TopologyNodeKind.Team, symbol: "TM")
                .AddEdgeKind("Membership", TopologyEdgeKind.Membership)
                .AddEdgeKind("Dependency", TopologyEdgeKind.Dependency)
                .AddEdgeKind("Data flow", TopologyEdgeKind.DataFlow)
                .AddEdgeKind("Authentication", TopologyEdgeKind.AuthenticationPath)
                .AddEdgeKind("Ownership", TopologyEdgeKind.Ownership));

        AddForceCluster(chart, "identity", "Identity", TopologyNodeKind.Person, 24, "#2563EB");
        AddForceCluster(chart, "endpoint", "Endpoints", TopologyNodeKind.Endpoint, 22, "#0891B2");
        AddForceCluster(chart, "application", "Applications", TopologyNodeKind.Service, 28, "#7C3AED");
        AddForceCluster(chart, "data", "Data", TopologyNodeKind.Database, 20, "#059669");
        AddForceCluster(chart, "network", "Network", TopologyNodeKind.Network, 18, "#0F766E");
        AddForceCluster(chart, "control", "Controls", TopologyNodeKind.Process, 14, "#DC2626");
        AddForceCluster(chart, "owner", "Owners", TopologyNodeKind.Team, 16, "#EA580C");

        AddForceRing(chart, "identity", 24, "endpoint", 22, 42, TopologyEdgeKind.AuthenticationPath, "sign-in");
        AddForceRing(chart, "endpoint", 22, "application", 28, 54, TopologyEdgeKind.Dependency, "calls");
        AddForceRing(chart, "application", 28, "data", 20, 48, TopologyEdgeKind.DataFlow, "reads");
        AddForceRing(chart, "network", 18, "endpoint", 22, 28, TopologyEdgeKind.Link, "segment");
        AddForceRing(chart, "control", 14, "application", 28, 30, TopologyEdgeKind.Dependency, "policy");
        AddForceRing(chart, "owner", 16, "application", 28, 34, TopologyEdgeKind.Ownership, "owns");
        AddForceRing(chart, "owner", 16, "identity", 24, 24, TopologyEdgeKind.Membership, "member");
        AddForceRing(chart, "control", 14, "data", 20, 22, TopologyEdgeKind.DataFlow, "governs");

        chart
            .AddEdge("busy-critical-app-data", "application-03", "data-17", "critical dependency", TopologyEdgeKind.Dependency, TopologyHealthStatus.Critical, TopologyDirection.Bidirectional)
            .AddEdge("busy-warning-id-app", "identity-19", "application-08", "privileged path", TopologyEdgeKind.AuthenticationPath, TopologyHealthStatus.Warning, TopologyDirection.Forward)
            .AddEdge("busy-warning-endpoint-data", "endpoint-16", "data-01", "direct data path", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Warning, TopologyDirection.Forward)
            .AddEdge("busy-critical-control-network", "control-05", "network-12", "policy gap", TopologyEdgeKind.Dependency, TopologyHealthStatus.Critical, TopologyDirection.Forward)
            .AddEdge("busy-owner-data", "owner-11", "data-06", "steward", TopologyEdgeKind.Ownership, TopologyHealthStatus.Healthy, TopologyDirection.Forward)
            .WithEdgeEmphasis("busy-critical-app-data", TopologyEdgeEmphasis.Strong)
            .WithEdgeEmphasis("busy-warning-id-app", TopologyEdgeEmphasis.Strong)
            .WithEdgeEmphasis("busy-warning-endpoint-data", TopologyEdgeEmphasis.Strong)
            .WithEdgeEmphasis("busy-critical-control-network", TopologyEdgeEmphasis.Strong);

        return chart;
    }

    private static TopologyChart BuildRelationshipRadialEgoGraph() {
        var chart = TopologyChart.Create()
            .WithId("visual-relationship-radial-ego-graph")
            .WithTitle("Relationship Radial Ego Graph")
            .WithSubtitle("Start from one application, show direct conversations in different directions, then push second-hop conversations farther away with capped expansion.")
            .WithViewport(1320, 820, 32)
            .WithLayout(TopologyLayoutMode.RelationshipRadial)
            .AddAutoGroup("application", "Applications", TopologyHealthStatus.Healthy, "root", color: "#7C3AED")
            .AddAutoGroup("identity", "Identity", TopologyHealthStatus.Healthy, "users", color: "#2563EB")
            .AddAutoGroup("endpoint", "Endpoints", TopologyHealthStatus.Healthy, "devices", color: "#0891B2")
            .AddAutoGroup("data", "Data", TopologyHealthStatus.Warning, "stores", color: "#059669")
            .AddAutoGroup("control", "Controls", TopologyHealthStatus.Warning, "policies", color: "#DC2626")
            .AddAutoGroup("owner", "Owners", TopologyHealthStatus.Healthy, "teams", color: "#EA580C")
            .AddAutoNode("application-03", "Payroll API", TopologyNodeKind.Service, TopologyHealthStatus.Warning, "application", "root", width: 44, height: 44, symbol: "API", color: "#7C3AED")
            .AddAutoNode("identity-admins", "Privileged users", TopologyNodeKind.Person, TopologyHealthStatus.Warning, "identity", "sign-in", width: 34, height: 34, symbol: "ID", color: "#2563EB")
            .AddAutoNode("endpoint-admin", "Admin workstation", TopologyNodeKind.Endpoint, TopologyHealthStatus.Healthy, "endpoint", "calls", width: 34, height: 34, symbol: "PC", color: "#0891B2")
            .AddAutoNode("data-payroll", "Payroll DB", TopologyNodeKind.Database, TopologyHealthStatus.Critical, "data", "reads", width: 34, height: 34, symbol: "DB", color: "#059669")
            .AddAutoNode("control-mfa", "MFA policy", TopologyNodeKind.Process, TopologyHealthStatus.Warning, "control", "governs", width: 34, height: 34, symbol: "CTL", color: "#DC2626")
            .AddAutoNode("owner-hr", "HR owners", TopologyNodeKind.Team, TopologyHealthStatus.Healthy, "owner", "owns", width: 34, height: 34, symbol: "TM", color: "#EA580C")
            .AddEdge("radial-admins-api", "application-03", "identity-admins", "sign-in", TopologyEdgeKind.AuthenticationPath, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional)
            .AddEdge("radial-endpoint-api", "application-03", "endpoint-admin", "calls", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional)
            .AddEdge("radial-api-db", "application-03", "data-payroll", "reads", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Critical, TopologyDirection.Forward)
            .AddEdge("radial-policy-api", "application-03", "control-mfa", "governs", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward)
            .AddEdge("radial-owner-api", "application-03", "owner-hr", "owns", TopologyEdgeKind.Ownership, TopologyHealthStatus.Healthy, TopologyDirection.Forward)
            .WithEdgeEmphasis("radial-api-db", TopologyEdgeEmphasis.Strong)
            .WithEdgeEmphasis("radial-admins-api", TopologyEdgeEmphasis.Strong);

        AddRadialSecondHop(chart, "identity-admins", "identity-breakglass", "Breakglass", TopologyNodeKind.Person, "identity", "member", "#2563EB");
        AddRadialSecondHop(chart, "identity-admins", "identity-service", "Service account", TopologyNodeKind.Person, "identity", "delegates", "#2563EB");
        AddRadialSecondHop(chart, "endpoint-admin", "endpoint-vpn", "VPN client", TopologyNodeKind.Endpoint, "endpoint", "uses", "#0891B2");
        AddRadialSecondHop(chart, "endpoint-admin", "network-admin", "Admin VLAN", TopologyNodeKind.Network, "endpoint", "segment", "#0891B2");
        AddRadialSecondHop(chart, "data-payroll", "data-export", "Export share", TopologyNodeKind.Storage, "data", "exports", "#059669");
        AddRadialSecondHop(chart, "data-payroll", "data-warehouse", "Warehouse", TopologyNodeKind.Database, "data", "syncs", "#059669");
        AddRadialSecondHop(chart, "control-mfa", "control-exception", "MFA exception", TopologyNodeKind.Process, "control", "bypass", "#DC2626");
        AddRadialSecondHop(chart, "owner-hr", "owner-security", "Security review", TopologyNodeKind.Team, "owner", "approves", "#EA580C");
        return chart;
    }

    private static void AddRadialSecondHop(TopologyChart chart, string parentId, string nodeId, string label, TopologyNodeKind kind, string groupId, string edgeLabel, string color) {
        chart.AddAutoNode(nodeId, label, kind, TopologyHealthStatus.Healthy, groupId, edgeLabel, width: 28, height: 28, symbol: label.Substring(0, 1).ToUpperInvariant(), color: color)
            .AddEdge("radial-" + parentId + "-" + nodeId, parentId, nodeId, edgeLabel, TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Forward);
    }

    private static TopologyChart BuildLargeRelationshipRadialEgoGraph() {
        var colors = new[] { "#2563EB", "#0891B2", "#059669", "#7C3AED", "#EA580C", "#DC2626", "#0F766E", "#9333EA", "#0284C7" };
        var chart = TopologyChart.Create()
            .WithId("visual-relationship-radial-500-ego-graph")
            .WithTitle("Relationship Radial 500 Ego Graph")
            .WithSubtitle("500-node ego graph: first-hop conversations radiate from the root, second-hop conversations fan outward, labels stay hidden until HTML focus.")
            .WithViewport(1680, 1060, 32)
            .WithLayout(TopologyLayoutMode.RelationshipRadial)
            .AddAutoGroup("root", "Root", TopologyHealthStatus.Warning, "1 app", color: "#7C3AED")
            .AddAutoNode("app-root", "Application Root", TopologyNodeKind.Service, TopologyHealthStatus.Warning, "root", "selected", width: 42, height: 42, symbol: "APP", color: "#7C3AED");

        for (var branch = 0; branch < 36; branch++) {
            var groupId = "branch-" + branch.ToString("00", CultureInfo.InvariantCulture);
            var color = colors[branch % colors.Length];
            var branchId = groupId + "-hub";
            chart.AddAutoGroup(groupId, "Conversation " + (branch + 1).ToString(CultureInfo.InvariantCulture), branch % 8 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy, "14 nodes", color: color);
            chart.AddAutoNode(branchId, "Hop " + (branch + 1).ToString(CultureInfo.InvariantCulture), BranchKind(branch), branch % 7 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy, groupId, "direct", width: 28, height: 28, symbol: BranchSymbol(branch), color: color);
            chart.AddEdge("large-root-" + branchId, "app-root", branchId, BranchEdgeLabel(branch), BranchEdgeKind(branch), branch % 7 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional);

            for (var leaf = 0; leaf < 13; leaf++) {
                var leafId = groupId + "-leaf-" + leaf.ToString("00", CultureInfo.InvariantCulture);
                chart.AddAutoNode(leafId, "Leaf " + (branch + 1).ToString(CultureInfo.InvariantCulture) + "." + (leaf + 1).ToString(CultureInfo.InvariantCulture), LeafKind(leaf), leaf % 11 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy, groupId, LeafEdgeLabel(leaf), width: 20, height: 20, symbol: LeafSymbol(leaf), color: color);
                chart.AddEdge("large-" + branchId + "-" + leaf.ToString("00", CultureInfo.InvariantCulture), branchId, leafId, LeafEdgeLabel(leaf), LeafEdgeKind(leaf), leaf % 11 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy, TopologyDirection.Forward);
            }
        }

        return chart;
    }

    private static TopologyNodeKind BranchKind(int index) => (index % 5) switch {
        0 => TopologyNodeKind.Person,
        1 => TopologyNodeKind.Endpoint,
        2 => TopologyNodeKind.Database,
        3 => TopologyNodeKind.Team,
        _ => TopologyNodeKind.Network
    };

    private static string BranchSymbol(int index) => (index % 5) switch {
        0 => "ID",
        1 => "PC",
        2 => "DB",
        3 => "TM",
        _ => "NET"
    };

    private static TopologyEdgeKind BranchEdgeKind(int index) => (index % 4) switch {
        0 => TopologyEdgeKind.AuthenticationPath,
        1 => TopologyEdgeKind.Dependency,
        2 => TopologyEdgeKind.DataFlow,
        _ => TopologyEdgeKind.Ownership
    };

    private static string BranchEdgeLabel(int index) => (index % 4) switch {
        0 => "sign-in",
        1 => "calls",
        2 => "reads",
        _ => "owns"
    };

    private static TopologyNodeKind LeafKind(int index) => (index % 6) switch {
        0 => TopologyNodeKind.Person,
        1 => TopologyNodeKind.Endpoint,
        2 => TopologyNodeKind.Database,
        3 => TopologyNodeKind.Storage,
        4 => TopologyNodeKind.Process,
        _ => TopologyNodeKind.Network
    };

    private static TopologyEdgeKind LeafEdgeKind(int index) => (index % 4) switch {
        0 => TopologyEdgeKind.Membership,
        1 => TopologyEdgeKind.Dependency,
        2 => TopologyEdgeKind.DataFlow,
        _ => TopologyEdgeKind.Link
    };

    private static string LeafEdgeLabel(int index) => (index % 6) switch {
        0 => "member",
        1 => "uses",
        2 => "syncs",
        3 => "exports",
        4 => "policy",
        _ => "segment"
    };

    private static string LeafSymbol(int index) => (index % 6) switch {
        0 => "U",
        1 => "E",
        2 => "D",
        3 => "S",
        4 => "P",
        _ => "N"
    };

    private static void AddForceCluster(TopologyChart chart, string groupId, string label, TopologyNodeKind kind, int count, string color) {
        chart.AddAutoGroup(groupId, label, TopologyHealthStatus.Healthy, count.ToString(CultureInfo.InvariantCulture) + " nodes", symbol: label.Substring(0, 1).ToUpperInvariant(), color: color);
        chart.AddAutoNode(groupId + "-hub", label + " Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, groupId, "hub", width: 42, height: 42, symbol: "H", color: color);

        for (var i = 0; i < count; i++) {
            var id = groupId + "-" + i.ToString("00", CultureInfo.InvariantCulture);
            var status = i % 9 == 4 ? TopologyHealthStatus.Critical : i % 5 == 2 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy;
            chart.AddAutoNode(id, label + " " + (i + 1).ToString(CultureInfo.InvariantCulture), kind, status, groupId, status.ToString(), width: 32, height: 32, symbol: label.Substring(0, 1).ToUpperInvariant(), color: color);
            chart.AddEdge(groupId + "-hub-" + i.ToString("00", CultureInfo.InvariantCulture), groupId + "-hub", id, "cluster", TopologyEdgeKind.Membership, TopologyHealthStatus.Healthy, TopologyDirection.None)
                .WithEdgeEmphasis(groupId + "-hub-" + i.ToString("00", CultureInfo.InvariantCulture), TopologyEdgeEmphasis.Subtle);
            if (i > 0 && i % 3 != 0) {
                chart.AddEdge(groupId + "-peer-" + i.ToString("00", CultureInfo.InvariantCulture), groupId + "-" + (i - 1).ToString("00", CultureInfo.InvariantCulture), id, "peer", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.None)
                    .WithEdgeEmphasis(groupId + "-peer-" + i.ToString("00", CultureInfo.InvariantCulture), TopologyEdgeEmphasis.Subtle);
            }
        }
    }

    private static void AddForceRing(TopologyChart chart, string sourcePrefix, string targetPrefix, int count, TopologyEdgeKind kind, string label) {
        AddForceRing(chart, sourcePrefix, ForceClusterSize(sourcePrefix), targetPrefix, ForceClusterSize(targetPrefix), count, kind, label);
    }

    private static void AddForceRing(TopologyChart chart, string sourcePrefix, int sourceCount, string targetPrefix, int targetCount, int count, TopologyEdgeKind kind, string label) {
        for (var i = 0; i < count; i++) {
            var source = sourcePrefix + "-" + (i % sourceCount).ToString("00", CultureInfo.InvariantCulture);
            var target = targetPrefix + "-" + ((i * 2 + 1) % targetCount).ToString("00", CultureInfo.InvariantCulture);
            var status = i % 7 == 3 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy;
            chart.AddEdge(
                "force-" + sourcePrefix + "-" + targetPrefix + "-" + i.ToString("00", CultureInfo.InvariantCulture),
                source,
                target,
                label,
                kind,
                status,
                TopologyDirection.Forward);
        }
    }

    private static int ForceClusterSize(string prefix) => prefix switch {
        "identity" => 10,
        "endpoint" => 9,
        "application" => 11,
        "data" => 7,
        "owner" => 6,
        _ => 1
    };
}
