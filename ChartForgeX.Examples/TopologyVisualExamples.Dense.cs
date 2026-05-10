using System.Globalization;
using ChartForgeX.Topology;

internal static partial class TopologyVisualExamples {
    private static TopologyChart BuildNestedUserHierarchy(TopologyLayoutDirection direction = TopologyLayoutDirection.TopToBottom) {
        var items = new List<TopologyHierarchyItem> {
            new("forest", "evotec.xyz") { Kind = TopologyNodeKind.Namespace, IconId = "forest", Symbol = "FOR" },
            new("domain", "ad.evotec.xyz", "forest") { Kind = TopologyNodeKind.Namespace, IconId = "domain", Symbol = "DOM" },
            new("corp", "Corporate OU", "domain") { Kind = TopologyNodeKind.Team, IconId = "team", Symbol = "OU" },
            new("ops", "Operations OU", "domain") { Kind = TopologyNodeKind.Team, IconId = "team", Symbol = "OU" },
            new("apps", "Applications OU", "domain") { Kind = TopologyNodeKind.Team, IconId = "team", Symbol = "OU" },
            new("corp-admins", "Corp Admins", "corp") { Kind = TopologyNodeKind.Team, IconId = "team", Symbol = "G" },
            new("corp-finance", "Finance", "corp") { Kind = TopologyNodeKind.Team, IconId = "team", Symbol = "G" },
            new("ops-helpdesk", "Helpdesk", "ops") { Kind = TopologyNodeKind.Team, IconId = "team", Symbol = "G" },
            new("ops-platform", "Platform", "ops") { Kind = TopologyNodeKind.Team, IconId = "team", Symbol = "G" },
            new("apps-web", "Web Apps", "apps") { Kind = TopologyNodeKind.Team, IconId = "team", Symbol = "G" },
            new("apps-data", "Data Apps", "apps") { Kind = TopologyNodeKind.Team, IconId = "team", Symbol = "G" }
        };

        AddHierarchyUsers(items, "corp-admins", "Admin", 18);
        AddHierarchyUsers(items, "corp-finance", "Finance", 22);
        AddHierarchyUsers(items, "ops-helpdesk", "Helpdesk", 24);
        AddHierarchyUsers(items, "ops-platform", "Platform", 26);
        AddHierarchyUsers(items, "apps-web", "Web", 28);
        AddHierarchyUsers(items, "apps-data", "Data", 30);

        var suffix = DirectionSuffix(direction);
        var horizontal = direction is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        return TopologyChart.Create()
            .WithId("visual-nested-user-hierarchy" + suffix)
            .WithTitle("Nested User Hierarchy" + DirectionTitleSuffix(direction))
            .WithSubtitle("Parent-child directory hierarchy with wrapped user rows and tiered bus routes.")
            .WithViewport(horizontal ? 1540 : 1320, horizontal ? 760 : 920, 28)
            .WithLegend(TopologyLegend.Default()
                .AddNodeKind("Namespace", TopologyNodeKind.Namespace, symbol: "DOM")
                .AddNodeKind("Group", TopologyNodeKind.Team, symbol: "G")
                .AddNodeKind("User", TopologyNodeKind.Person, symbol: "U"))
            .AddHierarchy(items, new TopologyHierarchyOptions {
                NodeDisplayMode = TopologyNodeDisplayMode.Tile,
                LayoutDirection = direction,
                NodeWidth = 78,
                NodeHeight = 48,
                EdgeKind = TopologyEdgeKind.Membership
            });
    }

    private static string DirectionSuffix(TopologyLayoutDirection direction) {
        return direction switch {
            TopologyLayoutDirection.LeftToRight => "-left-right",
            TopologyLayoutDirection.BottomToTop => "-bottom-top",
            TopologyLayoutDirection.RightToLeft => "-right-left",
            _ => string.Empty
        };
    }

    private static string DirectionTitleSuffix(TopologyLayoutDirection direction) {
        return direction switch {
            TopologyLayoutDirection.LeftToRight => " Left-to-Right",
            TopologyLayoutDirection.BottomToTop => " Bottom-to-Top",
            TopologyLayoutDirection.RightToLeft => " Right-to-Left",
            _ => string.Empty
        };
    }

    private static void AddHierarchyUsers(ICollection<TopologyHierarchyItem> items, string parentId, string prefix, int count) {
        var visible = Math.Min(count, 6);
        for (var i = 0; i < visible; i++) {
            items.Add(new TopologyHierarchyItem(parentId + "-user-" + i.ToString("00", CultureInfo.InvariantCulture), prefix + " User " + i.ToString("00", CultureInfo.InvariantCulture), parentId) {
                Kind = TopologyNodeKind.Person,
                IconId = "person",
                Symbol = "U"
            });
        }

        if (count <= visible) return;
        items.Add(new TopologyHierarchyItem(parentId + "-more", "+" + (count - visible).ToString(CultureInfo.InvariantCulture) + " more", parentId) {
            Kind = TopologyNodeKind.Team,
            IconId = "team",
            Symbol = "+",
            Subtitle = count.ToString(CultureInfo.InvariantCulture) + " total users",
            Width = 86,
            Height = 48
        });
    }

}
