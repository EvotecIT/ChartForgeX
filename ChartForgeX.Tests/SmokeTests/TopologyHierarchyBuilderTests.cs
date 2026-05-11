using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyHierarchyBuilderCreatesLevelFilteredDiagrams() {
        var chart = TopologyChart.Create()
            .WithId("hierarchy-builder")
            .WithViewport(640, 420, 24)
            .WithLegend(null)
            .AddHierarchy(new[] {
                new TopologyHierarchyItem("forest", "evotec.xyz") { Kind = TopologyNodeKind.Namespace, IconId = "forest", Symbol = "FOR" },
                new TopologyHierarchyItem("domain", "ad.evotec.xyz", "forest") { Kind = TopologyNodeKind.Namespace, IconId = "domain", Symbol = "DOM" },
                new TopologyHierarchyItem("admins", "Domain Admins", "domain") { Kind = TopologyNodeKind.Team, IconId = "team", Symbol = "DA" },
                new TopologyHierarchyItem("user", "Przemyslaw Klys", "admins") { Kind = TopologyNodeKind.Person, IconId = "person", Symbol = "U" }
            }, new TopologyHierarchyOptions { MaxLevel = 2, NodeDisplayMode = TopologyNodeDisplayMode.Tile, EdgeKind = TopologyEdgeKind.Membership });

        Assert(chart.LayoutMode == TopologyLayoutMode.Layered, "Hierarchy builders should default to layered topology layout.");
        Assert(chart.Nodes.Count == 3, "MaxLevel should keep levels 0, 1, and 2 while excluding deeper descendants.");
        Assert(!chart.Nodes.Any(node => node.Id == "user"), "Level filtering should remove deeper descendants before rendering.");
        Assert(chart.Edges.Count == 2, "Hierarchy builders should only create edges between visible parent-child nodes.");
        Assert(chart.Edges.All(edge => edge.SourcePort == TopologyEdgePort.Bottom && edge.TargetPort == TopologyEdgePort.Top), "Top-to-bottom hierarchy edges should attach from parent bottom to child top.");
        Assert(chart.Nodes.Single(node => node.Id == "admins").Metadata["hierarchy.level"] == "2", "Hierarchy builders should expose resolved levels as reusable metadata.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle()).Length > 64, "Level-filtered hierarchy diagrams should render as PNG.");
    }

    private static void TopologyLayeredLayoutWrapsCrowdedLevels() {
        var items = new List<TopologyHierarchyItem> { new("root", "Root") { Kind = TopologyNodeKind.Team, IconId = "team" } };
        for (var i = 0; i < 18; i++) items.Add(new TopologyHierarchyItem("member-" + i.ToString("00", CultureInfo.InvariantCulture), "Member " + i.ToString("00", CultureInfo.InvariantCulture), "root") { Kind = TopologyNodeKind.Person, IconId = "person" });
        var chart = TopologyChart.Create()
            .WithId("crowded-level")
            .WithViewport(520, 360, 24)
            .WithLegend(null)
            .AddHierarchy(items, new TopologyHierarchyOptions { NodeDisplayMode = TopologyNodeDisplayMode.Tile, NodeWidth = 72, NodeHeight = 54 });

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false });
        var children = prepared.Nodes.Where(node => node.Metadata.TryGetValue("hierarchy.parentId", out var parent) && parent == "root").ToList();
        Assert(children.Select(node => node.Metadata["layout.row"]).Distinct(StringComparer.Ordinal).Count() > 1, "Crowded hierarchy levels should wrap into multiple rows.");
        Assert(!AnyOverlap(children), "Wrapped hierarchy levels should not place sibling nodes on top of each other.");
        Assert(chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle()).Contains("data-layout-mode=\"Layered\"", StringComparison.Ordinal), "Wrapped hierarchy diagrams should render with layered metadata.");
    }

    private static void TopologyTeamBuilderCreatesOrgDiagrams() {
        var chart = TopologyChart.Create()
            .WithId("team-builder")
            .WithViewport(540, 380, 24)
            .WithLegend(null)
            .AddTeam("team", "Team", new[] {
                new TopologyTeamMember("lead", "Marcel", "Founder"),
                new TopologyTeamMember("pm", "Deniz", "Project Manager") { ParentId = "lead" },
                new TopologyTeamMember("designer", "Ersad", "Product Designer") { ParentId = "lead" }
            }, new TopologyTeamOptions { MaxLevel = 2 });

        Assert(chart.Nodes.Single(node => node.Id == "team").Kind == TopologyNodeKind.Team, "Team builders should create a reusable team root.");
        Assert(chart.Nodes.Where(node => node.Kind == TopologyNodeKind.Person).Count() == 3, "Team builders should create person nodes for members.");
        Assert(chart.Nodes.All(node => node.DisplayMode == TopologyNodeDisplayMode.Card), "Team builders should default to self-contained cards so hierarchy connectors do not cross external tile labels.");
        Assert(chart.Edges.Any(edge => edge.SourceNodeId == "lead" && edge.TargetNodeId == "pm"), "Team builders should preserve member reporting lines.");
        Assert(chart.Nodes.Single(node => node.Id == "pm").Metadata["hierarchy.level"] == "2", "Nested team members should inherit hierarchy levels.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle()).Length > 64, "Team diagrams should render as PNG.");
    }

    private static void TopologyHierarchyBuilderSupportsLevelWindowsWithAncestors() {
        var chart = TopologyChart.Create()
            .WithId("hierarchy-window")
            .WithViewport(720, 440, 24)
            .WithLegend(null)
            .AddHierarchy(SampleDirectoryHierarchy(), new TopologyHierarchyOptions { MinLevel = 2, MaxLevel = 3, IncludeAncestorContext = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile });

        Assert(chart.Nodes.Any(node => node.Id == "forest" && node.Metadata["hierarchy.context"] == "ancestor"), "Level windows should keep root ancestors as context when requested.");
        Assert(chart.Nodes.Any(node => node.Id == "domain" && node.Metadata["hierarchy.context"] == "ancestor"), "Level windows should keep intermediate ancestors as context when requested.");
        Assert(chart.Nodes.Any(node => node.Id == "admins" && node.Metadata["hierarchy.context"] == "primary"), "Level windows should mark included range nodes as primary.");
        Assert(chart.Nodes.Any(node => node.Id == "owner" && node.Metadata["hierarchy.context"] == "primary"), "Level windows should include descendants inside the selected range.");
        Assert(!chart.Nodes.Any(node => node.Id == "device"), "Level windows should exclude nodes deeper than MaxLevel.");
        Assert(chart.Edges.Any(edge => edge.SourceNodeId == "forest" && edge.TargetNodeId == "domain"), "Ancestor context should preserve breadcrumb edges.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle()).Length > 64, "Level-windowed hierarchy diagrams should render as PNG.");
    }

    private static void TopologyHierarchyBuilderCanRenderExactLevelRanges() {
        var chart = TopologyChart.Create()
            .WithId("hierarchy-exact-window")
            .WithViewport(600, 320, 24)
            .WithLegend(null)
            .AddHierarchy(SampleDirectoryHierarchy(), new TopologyHierarchyOptions { MinLevel = 2, MaxLevel = 2, IncludeAncestorContext = false, NodeDisplayMode = TopologyNodeDisplayMode.Pill });

        Assert(chart.Nodes.Select(node => node.Id).OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(new[] { "admins", "operators" }, StringComparer.Ordinal), "Exact level windows should render only the requested levels when ancestor context is disabled.");
        Assert(chart.Edges.Count == 0, "Exact level windows without ancestors should not create edges to hidden parents.");
        Assert(chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }).Contains("data-node-id=\"admins\"", StringComparison.Ordinal), "Exact level windows should still render selected hierarchy nodes.");
    }

    private static void TopologyLayeredLayoutKeepsChildrenNearParents() {
        var chart = TopologyChart.Create()
            .WithId("parent-child-order")
            .WithViewport(560, 360, 24)
            .WithLegend(null)
            .AddHierarchy(new[] {
                new TopologyHierarchyItem("root", "Root") { Kind = TopologyNodeKind.Team },
                new TopologyHierarchyItem("parent-a", "Parent A", "root") { Kind = TopologyNodeKind.Team },
                new TopologyHierarchyItem("parent-b", "Parent B", "root") { Kind = TopologyNodeKind.Team },
                new TopologyHierarchyItem("z-child", "Z Child", "parent-a") { Kind = TopologyNodeKind.Person },
                new TopologyHierarchyItem("a-child", "A Child", "parent-b") { Kind = TopologyNodeKind.Person }
            }, new TopologyHierarchyOptions { NodeDisplayMode = TopologyNodeDisplayMode.Tile });

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false });
        var parentA = prepared.Nodes.Single(node => node.Id == "parent-a");
        var parentB = prepared.Nodes.Single(node => node.Id == "parent-b");
        var childA = prepared.Nodes.Single(node => node.Id == "z-child");
        var childB = prepared.Nodes.Single(node => node.Id == "a-child");
        Assert(parentA.X < parentB.X, "The parent layer should establish a deterministic order.");
        Assert(childA.X < childB.X, "Child ordering should follow parent order before child ids.");
        Assert(Math.Abs(CenterX(parentA) - CenterX(childA)) < Math.Abs(CenterX(parentB) - CenterX(childA)), "A child should stay closer to its own parent than to the next parent.");
        Assert(Math.Abs(CenterX(parentB) - CenterX(childB)) < Math.Abs(CenterX(parentA) - CenterX(childB)), "Sibling subtrees should not swap sides because of child ids.");
    }

    private static void TopologyLayeredLayoutCentersHierarchyBuckets() {
        var chart = TopologyChart.Create()
            .WithId("parent-child-buckets")
            .WithViewport(760, 420, 24)
            .WithLegend(null)
            .AddHierarchy(new[] {
                new TopologyHierarchyItem("root", "Root") { Kind = TopologyNodeKind.Team },
                new TopologyHierarchyItem("parent-a", "Parent A", "root") { Kind = TopologyNodeKind.Team },
                new TopologyHierarchyItem("parent-b", "Parent B", "root") { Kind = TopologyNodeKind.Team },
                new TopologyHierarchyItem("child-a1", "Child A1", "parent-a") { Kind = TopologyNodeKind.Person },
                new TopologyHierarchyItem("child-a2", "Child A2", "parent-a") { Kind = TopologyNodeKind.Person },
                new TopologyHierarchyItem("child-a3", "Child A3", "parent-a") { Kind = TopologyNodeKind.Person },
                new TopologyHierarchyItem("child-b1", "Child B1", "parent-b") { Kind = TopologyNodeKind.Person },
                new TopologyHierarchyItem("child-b2", "Child B2", "parent-b") { Kind = TopologyNodeKind.Person }
            }, new TopologyHierarchyOptions { NodeDisplayMode = TopologyNodeDisplayMode.CompactCard, NodeWidth = 100, NodeHeight = 52 });

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false });
        var parentA = prepared.Nodes.Single(node => node.Id == "parent-a");
        var parentB = prepared.Nodes.Single(node => node.Id == "parent-b");
        var childrenA = prepared.Nodes.Where(node => node.Metadata.TryGetValue("hierarchy.parentId", out var parent) && parent == "parent-a").ToList();
        var childrenB = prepared.Nodes.Where(node => node.Metadata.TryGetValue("hierarchy.parentId", out var parent) && parent == "parent-b").ToList();
        var centerA = childrenA.Average(CenterX);
        var centerB = childrenB.Average(CenterX);
        Assert(centerA < centerB, "Hierarchy child buckets should preserve the visual order of their parent buckets.");
        Assert(Math.Abs(CenterX(parentA) - centerA) < Math.Abs(CenterX(parentB) - centerA), "The first child bucket should be centered near its parent.");
        Assert(Math.Abs(CenterX(parentB) - centerB) < Math.Abs(CenterX(parentA) - centerB), "The second child bucket should be centered near its parent.");
    }

    private static void TopologyLayeredHierarchyRoutesUseSharedBuses() {
        var chart = TopologyChart.Create()
            .WithId("shared-hierarchy-bus")
            .WithViewport(720, 420, 24)
            .WithLegend(null)
            .AddTeam("team", "Team", new[] {
                new TopologyTeamMember("lead", "Lead", "Manager"),
                new TopologyTeamMember("a", "A", "Engineer") { ParentId = "lead" },
                new TopologyTeamMember("b", "B", "Engineer") { ParentId = "lead" },
                new TopologyTeamMember("c", "C", "Engineer") { ParentId = "lead" }
            });

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false });
        var leadEdges = prepared.Edges.Where(edge => edge.SourceNodeId == "lead").ToList();
        Assert(leadEdges.Count == 3, "The test hierarchy should have multiple child routes from the same manager.");
        Assert(leadEdges.All(edge => edge.Metadata.TryGetValue("hierarchy.route", out var route) && route == "shared-bus"), "Hierarchy fan-out routes should be promoted to a shared bus.");
        Assert(leadEdges.All(edge => edge.SourcePort == TopologyEdgePort.Auto && edge.TargetPort == TopologyEdgePort.Top), "Shared hierarchy buses should avoid source-port spreading while keeping child top anchors.");
        Assert(leadEdges.All(edge => edge.Waypoints.Count >= 2), "Shared hierarchy buses should use deterministic bend points per child route.");
        Assert(leadEdges.Select(edge => edge.Waypoints[0].X.ToString("0.###", CultureInfo.InvariantCulture) + ":" + edge.Waypoints[0].Y.ToString("0.###", CultureInfo.InvariantCulture)).Distinct(StringComparer.Ordinal).Count() == 1, "All child routes from one parent should share one parent trunk waypoint.");
    }

    private static void TopologyHierarchyBuilderRejectsInvalidOptions() {
        AssertThrows<ArgumentOutOfRangeException>(() => TopologyChart.Create().AddHierarchy(SampleDirectoryHierarchy(), new TopologyHierarchyOptions { MinLevel = 3, MaxLevel = 2 }), "Hierarchy builders should reject inverted level windows.");
        AssertThrows<ArgumentOutOfRangeException>(() => TopologyChart.Create().AddHierarchy(SampleDirectoryHierarchy(), new TopologyHierarchyOptions { NodeWidth = 0 }), "Hierarchy builders should reject invalid default node widths.");
        AssertThrows<ArgumentException>(() => TopologyChart.Create().AddHierarchy(SampleDirectoryHierarchy(), new TopologyHierarchyOptions { EdgeIdPrefix = " " }), "Hierarchy builders should reject empty edge id prefixes.");
        AssertThrows<ArgumentException>(() => TopologyChart.Create().AddHierarchy(new[] { new TopologyHierarchyItem("child", "Child", "missing") }), "Hierarchy builders should reject missing parents close to the caller.");
        AssertThrows<ArgumentException>(() => TopologyChart.Create().AddHierarchy(new[] { new TopologyHierarchyItem("a", "A", "b"), new TopologyHierarchyItem("b", "B", "a") }), "Hierarchy builders should reject parent cycles close to the caller.");
    }

    private static bool AnyOverlap(IReadOnlyList<TopologyNode> nodes) {
        for (var i = 0; i < nodes.Count; i++) {
            for (var j = i + 1; j < nodes.Count; j++) {
                if (nodes[i].X < nodes[j].X + nodes[j].Width && nodes[i].X + nodes[i].Width > nodes[j].X && nodes[i].Y < nodes[j].Y + nodes[j].Height && nodes[i].Y + nodes[i].Height > nodes[j].Y) return true;
            }
        }

        return false;
    }

    private static double CenterX(TopologyNode node) => node.X + node.Width / 2;

    private static TopologyHierarchyItem[] SampleDirectoryHierarchy() => new[] {
        new TopologyHierarchyItem("forest", "evotec.xyz") { Kind = TopologyNodeKind.Namespace, IconId = "forest", Symbol = "FOR" },
        new TopologyHierarchyItem("domain", "ad.evotec.xyz", "forest") { Kind = TopologyNodeKind.Namespace, IconId = "domain", Symbol = "DOM" },
        new TopologyHierarchyItem("admins", "Domain Admins", "domain") { Kind = TopologyNodeKind.Team, IconId = "team", Symbol = "DA" },
        new TopologyHierarchyItem("operators", "Operators", "domain") { Kind = TopologyNodeKind.Team, IconId = "team", Symbol = "OPS" },
        new TopologyHierarchyItem("owner", "Przemyslaw Klys", "admins") { Kind = TopologyNodeKind.Person, IconId = "person", Symbol = "U" },
        new TopologyHierarchyItem("device", "Admin Workstation", "owner") { Kind = TopologyNodeKind.Endpoint, IconId = "desktop", Symbol = "PC" }
    };
}
