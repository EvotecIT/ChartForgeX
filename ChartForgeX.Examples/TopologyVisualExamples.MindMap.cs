using ChartForgeX.Topology;

internal static partial class TopologyVisualExamples {
    private static TopologyChart BuildIdentityMindMap() {
        const string blue = "#3153C9";
        const string blueSoft = "#DDEBFF";
        const string navy = "#32408F";
        const string navySoft = "#E6ECFF";
        const string yellow = "#D8A900";
        const string yellowSoft = "#FFF4B8";
        const string orange = "#E9781B";
        const string orangeSoft = "#FFE0C2";
        const string teal = "#03A47C";
        const string tealSoft = "#CFF7EA";
        const string purple = "#7A35C7";
        const string purpleSoft = "#EAD8FF";

        return TopologyChart.Create()
            .WithId("visual-identity-mind-map")
            .WithTitle("Identity Mind Map")
            .WithViewport(1600, 900, 38)
            .WithTheme(TopologyTheme.Light())
            .AddMindMap(new[] {
                Item("identity", "Cloud Identity", null, TopologyNodeKind.Cloud, "cloud:cloud", navy, navySoft, "central service"),
                Item("users", "Users", "identity", TopologyNodeKind.Person, "people:person", yellow, yellowSoft, "accounts").WithMetadata("mindmap.side", "right").WithMetadata("mindmap.order", "10"),
                Item("groups", "Groups", "identity", TopologyNodeKind.Team, "people:team", blue, blueSoft, "membership").WithMetadata("mindmap.side", "right").WithMetadata("mindmap.order", "20"),
                Item("applications", "Applications", "identity", TopologyNodeKind.Application, "common:application", yellow, yellowSoft, "app access").WithMetadata("mindmap.side", "right").WithMetadata("mindmap.order", "30"),
                Item("licenses", "Licenses", "users", TopologyNodeKind.Service, "common:service", yellow, yellowSoft),
                Item("assigned-roles", "Assigned roles", "users", TopologyNodeKind.Team, "people:team", yellow, yellowSoft),
                Item("authentication", "Authentication methods", "users", TopologyNodeKind.Certificate, "common:certificate", yellow, yellowSoft),
                Item("self-service", "Self-service password reset", "users", TopologyNodeKind.Service, "common:service", yellow, yellowSoft),
                Item("app-roles", "App roles", "applications", TopologyNodeKind.Team, "people:team", yellow, yellowSoft),
                Item("certificates", "App certificates and secrets", "applications", TopologyNodeKind.Certificate, "common:certificate", yellow, yellowSoft),
                Item("app-permissions", "Scopes and permissions", "applications", TopologyNodeKind.Service, "common:service", yellow, yellowSoft),
                Item("devices", "Devices", "identity", TopologyNodeKind.Endpoint, "common:desktop", orange, orangeSoft, "endpoints").WithMetadata("mindmap.side", "left").WithMetadata("mindmap.order", "10"),
                Item("security", "Protect and secure", "identity", TopologyNodeKind.Certificate, "common:certificate", blue, blueSoft, "controls").WithMetadata("mindmap.side", "left").WithMetadata("mindmap.order", "20"),
                Item("monitoring", "Monitoring and health", "identity", TopologyNodeKind.Service, "common:service", teal, tealSoft, "signals").WithMetadata("mindmap.side", "left").WithMetadata("mindmap.order", "30"),
                Item("governance", "Identity Governance", "identity", TopologyNodeKind.Team, "people:team", purple, purpleSoft, "lifecycle").WithMetadata("mindmap.side", "left").WithMetadata("mindmap.order", "40"),
                Item("hybrid", "Hybrid management", "devices", TopologyNodeKind.Network, "network:wan-link", orange, orangeSoft),
                Item("joined-devices", "Joined devices", "devices", TopologyNodeKind.Endpoint, "common:desktop", orange, orangeSoft),
                Item("linux-login", "Linux login", "devices", TopologyNodeKind.Endpoint, "common:laptop", orange, orangeSoft),
                Item("conditional-access", "Conditional access", "security", TopologyNodeKind.Service, "common:service", blue, blueSoft),
                Item("identity-protection", "Identity protection", "security", TopologyNodeKind.Certificate, "common:certificate", blue, blueSoft),
                Item("audit-logs", "Audit logs", "monitoring", TopologyNodeKind.Database, "common:database", teal, tealSoft),
                Item("reports", "Reports", "monitoring", TopologyNodeKind.Application, "common:application", teal, tealSoft),
                Item("access-reviews", "Access reviews", "governance", TopologyNodeKind.Certificate, "common:certificate", purple, purpleSoft),
                Item("lifecycle", "Lifecycle workflows", "governance", TopologyNodeKind.Service, "common:service", purple, purpleSoft)
            }, new TopologyMindMapOptions {
                RootWidth = 280,
                RootHeight = 106,
                BranchWidth = 230,
                BranchHeight = 78,
                LeafWidth = 250,
                LeafHeight = 46
            });
    }

    private static TopologyHierarchyItem Item(string id, string label, string? parentId, TopologyNodeKind kind, string iconId, string color, string backgroundColor, string? subtitle = null) =>
        new(id, label, parentId) {
            Kind = kind,
            IconId = iconId,
            Color = color,
            BackgroundColor = backgroundColor,
            Subtitle = subtitle
        };
}
