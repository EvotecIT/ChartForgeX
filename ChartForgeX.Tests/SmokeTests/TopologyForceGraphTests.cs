using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyForceGraphStyleReducesStaticNoise() {
        var chart = CreateForceGraphFixture("force-style", 5, 7)
            .WithTitle("Force graph style");

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithForceGraphStyle()
            .WithFitContentToViewport();
        var svg = chart.ToSvg(options);

        Assert(options.UseForceGraphPresentation, "Force graph style should mark render options for lower-ink graph presentation.");
        Assert(options.ForceLayoutProfile == TopologyForceLayoutProfile.RelationshipGraph, "Force graph style should select the relationship graph solver profile.");
        Assert(!options.IncludeEdgeLabels, "Force graph style should hide static edge labels by default.");
        Assert(!options.IncludeDirectionMarkers, "Force graph style should hide static direction markers by default.");
        Assert(!options.IncludeGroups, "Force graph style should hide static group shells by default.");
        Assert(!options.IncludeLegend, "Force graph style should avoid oversized static legends by default.");
        Assert(svg.Contains("data-layout-mode=\"ForceDirected\"", StringComparison.Ordinal), "Force graph style should still render through the force-directed topology mode.");
        Assert(svg.Contains("data-cfx-meta-layout-force-profile=\"RelationshipGraph\"", StringComparison.Ordinal), "Force graph style should expose the relationship graph solver profile in SVG metadata.");
        Assert(!svg.Contains("data-cfx-role=\"topology-edge-label\"", StringComparison.Ordinal), "Force graph static SVG should not paint every relationship label.");
        Assert(svg.Contains("opacity=\"0.26\"", StringComparison.Ordinal), "Force graph static SVG should lower normal edge ink instead of drawing every edge at full emphasis.");
        Assert(chart.ToPng(options).Length > 64, "Force graph style should preserve PNG output.");
    }

    private static void TopologyForceGraphRelationshipProfileSpreadsBusyGraphs() {
        var chart = CreateBusyForceGraphFixture("force-busy-style", 8, 15)
            .WithTitle("Busy force graph");

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithForceGraphStyle()
            .WithFitContentToViewport();
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var dotNodes = prepared.Nodes.Where(node => node.Kind != TopologyNodeKind.Hub).ToList();
        var hubNodes = prepared.Nodes.Where(node => node.Kind == TopologyNodeKind.Hub).ToList();
        var svg = chart.ToSvg(options);

        Assert(prepared.Nodes.Count >= 120, "Busy force graph fixture should exercise a graph larger than the polite demo case.");
        Assert(prepared.Edges.Count >= 230, "Busy force graph fixture should exercise dense relationship edges.");
        Assert(dotNodes.Select(node => Math.Round(node.X, 1)).Distinct().Count() > 40, "Relationship profile should spread busy graph nodes horizontally.");
        Assert(dotNodes.Select(node => Math.Round(node.Y, 1)).Distinct().Count() > 30, "Relationship profile should spread busy graph nodes vertically.");
        Assert(OverlapCount(dotNodes) <= 2, "Relationship profile should keep almost all busy dot nodes separated.");
        Assert(hubNodes.All(node => node.Metadata.TryGetValue("layout.force.mass", out var mass) && double.Parse(mass, CultureInfo.InvariantCulture) > 8), "Relationship profile should expose degree-weighted hub mass diagnostics.");
        Assert(prepared.Nodes.All(node => node.Metadata.TryGetValue("layout.force.profile", out var profile) && profile == "RelationshipGraph"), "Every busy force node should carry relationship profile diagnostics.");
        Assert(prepared.Edges.All(edge => edge.Metadata.TryGetValue("layout.force.profile", out var profile) && profile == "RelationshipGraph"), "Every busy force edge should carry relationship profile diagnostics.");
        Assert(!svg.Contains("data-cfx-role=\"topology-edge-label\"", StringComparison.Ordinal), "Busy static force SVG should keep relationship labels off the canvas by default.");
        Assert(svg.Contains("data-cfx-meta-layout-force-mass=", StringComparison.Ordinal), "Busy static force SVG should expose node mass diagnostics for host adapters.");
        Assert(chart.ToPng(options).Length > 64, "Busy force graph fixture should preserve PNG output.");
    }

    private static void TopologyForceGraphHtmlPagesExposeGraphControls() {
        var chart = CreateForceGraphFixture("force-html", 4, 6)
            .WithTitle("Interactive force graph");

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithForceGraphStyle()
            .WithHtmlForceGraphControls();
        var html = chart.ToHtmlPage(options);

        Assert(html.Contains("data-cfx-force-graph-controls=\"true\"", StringComparison.Ordinal), "Force graph HTML should opt into graph-specific controls.");
        Assert(html.Contains("data-cfx-force-search=\"true\"", StringComparison.Ordinal), "Force graph HTML should expose a search field.");
        Assert(html.Contains("data-cfx-force-status=\"true\"", StringComparison.Ordinal), "Force graph HTML should expose status filtering.");
        Assert(html.Contains("data-cfx-force-group=\"true\"", StringComparison.Ordinal), "Force graph HTML should expose group filtering.");
        Assert(html.Contains("data-cfx-force-toggle=\"edge-labels\"", StringComparison.Ordinal), "Force graph HTML should let users reveal edge labels on demand.");
        Assert(html.Contains("data-cfx-force-toggle=\"focus\"", StringComparison.Ordinal), "Force graph HTML should let users focus on one node's neighborhood.");
        Assert(html.Contains("data-cfx-role=\"topology-edge-label\"", StringComparison.Ordinal), "Force graph HTML should carry edge labels so focused neighborhoods can reveal what talks to what.");
        Assert(html.Contains("data-cfx-role=\"topology-group\"", StringComparison.Ordinal), "Force graph HTML should carry group shells so the Groups toggle can reveal current clusters.");
        Assert(html.Contains("cfx-topology-html-force-focus-label", StringComparison.Ordinal), "Force graph HTML should reveal labels only for the focused relationship neighborhood.");
        Assert(html.Contains("data-cfx-force-toggle=\"hide-moving-edges\"", StringComparison.Ordinal), "Force graph HTML should support hiding noisy edges while panning and zooming.");
        Assert(html.Contains("cfx-topology-force-filter", StringComparison.Ordinal), "Force graph HTML should dispatch filter events for host integrations.");
        Assert(html.Contains("cfx-topology-html-force-hidden", StringComparison.Ordinal), "Force graph HTML should include reusable filter classes.");
        Assert(html.Contains("data-node-label=\"Node 0-0\"", StringComparison.Ordinal), "Force graph HTML search should include human-facing node labels even when dot labels are hidden.");
        Assert(html.Contains("data-edge-label=\"bridge\"", StringComparison.Ordinal), "Force graph HTML search should include edge labels.");
        Assert(html.Contains("data-group-label=\"Group 1\"", StringComparison.Ordinal), "Force graph HTML search should include group labels.");
        Assert(html.Contains("queryNodes.add(attr(edge, 'data-source-node-id'))", StringComparison.Ordinal), "Force graph search should surface endpoints for matching edge terms.");
        Assert(html.Contains("if (!state.focus || !state.edges) {", StringComparison.Ordinal), "Force graph focus toggles should clear stale focus labels when disabled.");
        Assert(html.Contains("statusNodes.add(attr(edge, 'data-source-node-id'))", StringComparison.Ordinal), "Force graph status filters should surface endpoints for matching edge statuses.");
        Assert(html.Contains("const edgeQueryOk = !query || forceSearchText(edge).includes(query)", StringComparison.Ordinal), "Force graph search filters should not reveal unrelated parallel edges between matching endpoints.");
        Assert(html.Contains("const edgeStatusOk = !state.status || attr(edge, 'data-cfx-status') === state.status", StringComparison.Ordinal), "Force graph status filters should not reveal unrelated parallel edges between matching endpoints.");
        Assert(html.Contains("const hasVisibleNodes = !!wrapper.querySelector", StringComparison.Ordinal), "Force graph group shells should disappear when filtering leaves them empty.");
        Assert(html.Contains("style.setAttribute('data-cfx-export-style', 'force-filters')", StringComparison.Ordinal), "Force graph exports should serialize filter hiding styles into the SVG.");
    }

    private static void TopologyForceGraphHtmlGroupFiltersUsePreparedView() {
        var chart = CreateForceGraphFixture("force-html-view", 3, 4)
            .WithTitle("Interactive force graph view");

        var options = new TopologyRenderOptions {
                IncludeLegend = false,
                View = new TopologyView { GroupIds = { "group-01" } }
            }
            .WithForceGraphStyle()
            .WithHtmlForceGraphControls();
        var html = chart.ToHtmlPage(options);

        Assert(html.Contains("<option value=\"group-01\">Group 2</option>", StringComparison.Ordinal), "Force graph group filters should include groups that remain in the prepared view.");
        Assert(!html.Contains("<option value=\"group-00\">Group 1</option>", StringComparison.Ordinal), "Force graph group filters should omit groups removed by the prepared view.");
        Assert(!html.Contains("<option value=\"group-02\">Group 3</option>", StringComparison.Ordinal), "Force graph group filters should not offer off-view groups that would blank the graph.");
    }

    private static void TopologyRelationshipRadialHtmlPagesExposeFocusControls() {
        var chart = CreateForceGraphFixture("radial-html", 5, 8)
            .WithTitle("Interactive radial graph")
            .WithLayout(TopologyLayoutMode.RelationshipRadial);

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithForceGraphStyle()
            .WithHtmlForceGraphControls()
            .WithRelationshipRadialFocus("group-00-hub", maxDepth: 2, maxFanout: 12);
        var html = chart.ToHtmlPage(options);

        Assert(html.Contains("data-layout-mode=\"RelationshipRadial\"", StringComparison.Ordinal), "Relationship-radial HTML should preserve the graph-exploration layout mode.");
        Assert(html.Contains("data-cfx-force-graph-controls=\"true\"", StringComparison.Ordinal), "Relationship-radial HTML should opt into graph-specific focus controls.");
        Assert(html.Contains("data-cfx-force-toggle=\"focus\"", StringComparison.Ordinal), "Relationship-radial HTML should expose the neighborhood focus toggle.");
        Assert(html.Contains("data-cfx-role=\"topology-edge-label\"", StringComparison.Ordinal), "Relationship-radial HTML should carry edge labels for focused neighborhoods.");
    }

    private static void TopologyRelationshipRadialLayoutExpandsFromRootByHop() {
        var chart = TopologyChart.Create()
            .WithId("radial-talks")
            .WithViewport(920, 640, 28)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.RelationshipRadial)
            .AddAutoNode("root", "Root", TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 42, height: 42, symbol: "R");

        for (var i = 0; i < 5; i++) {
            var first = "first-" + i.ToString("00", CultureInfo.InvariantCulture);
            chart.AddAutoNode(first, "First " + i.ToString(CultureInfo.InvariantCulture), TopologyNodeKind.Endpoint, TopologyHealthStatus.Healthy, width: 30, height: 30, symbol: "1")
                .AddEdge("root-" + first, "root", first, "talks", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional);
            for (var j = 0; j < 2; j++) {
                var second = first + "-second-" + j.ToString("00", CultureInfo.InvariantCulture);
                chart.AddAutoNode(second, "Second " + i.ToString(CultureInfo.InvariantCulture) + "-" + j.ToString(CultureInfo.InvariantCulture), TopologyNodeKind.Database, TopologyHealthStatus.Healthy, width: 24, height: 24, symbol: "2")
                    .AddEdge(first + "-" + second, first, second, "next", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy, TopologyDirection.Forward);
            }
        }

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot }
            .WithRelationshipRadialFocus("root", maxDepth: 2, maxFanout: 5)
            .WithFitContentToViewport();
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var root = prepared.Nodes.Single(node => node.Id == "root");
        var firstHop = prepared.Nodes.Where(node => node.Id.StartsWith("first-", StringComparison.Ordinal) && !node.Id.Contains("-second-", StringComparison.Ordinal)).ToList();
        var secondHop = prepared.Nodes.Where(node => node.Id.Contains("-second-", StringComparison.Ordinal)).ToList();
        var centerX = prepared.Viewport.Width / 2d;
        var centerY = prepared.Viewport.Height / 2d;
        var averageFirstDistance = firstHop.Average(node => Distance(node, root.X + root.Width / 2, root.Y + root.Height / 2));
        var averageSecondDistance = secondHop.Average(node => Distance(node, root.X + root.Width / 2, root.Y + root.Height / 2));

        Assert(prepared.LayoutMode == TopologyLayoutMode.RelationshipRadial, "Relationship-radial layout should preserve the requested layout mode.");
        Assert(Distance(root, centerX, centerY) < 24, "Relationship-radial layout should put the selected root near the center.");
        Assert(firstHop.Select(node => Math.Round(Math.Atan2(node.Y - root.Y, node.X - root.X), 1)).Distinct().Count() >= 5, "First-hop conversations should spread into distinct directions around the root.");
        Assert(averageSecondDistance > averageFirstDistance + 70, "Second-hop conversations should be pushed farther away than direct conversations.");
        Assert(prepared.Nodes.All(node => node.Metadata.TryGetValue("layout.radial.root", out var value) && value == "root"), "Relationship-radial nodes should expose the radial root.");
        Assert(prepared.Edges.All(edge => edge.Metadata.TryGetValue("layout.radial", out var value) && value == "true"), "Relationship-radial edges should expose radial layout diagnostics.");
    }

    private static void TopologyRelationshipRadialFanoutIgnoresVisitedParent() {
        var chart = TopologyChart.Create()
            .WithId("radial-parent-fanout")
            .WithViewport(720, 480, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.RelationshipRadial)
            .AddAutoNode("root", "Root", TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 42, height: 42)
            .AddAutoNode("branch", "Branch", TopologyNodeKind.Endpoint, TopologyHealthStatus.Healthy, width: 30, height: 30)
            .AddEdge("root-branch", "root", "branch", "root edge", TopologyEdgeKind.Dependency, TopologyHealthStatus.Critical, TopologyDirection.Bidirectional);

        for (var i = 0; i < 3; i++) {
            var leaf = "leaf-" + i.ToString("00", CultureInfo.InvariantCulture);
            chart.AddAutoNode(leaf, "Leaf " + i.ToString(CultureInfo.InvariantCulture), TopologyNodeKind.Person, TopologyHealthStatus.Healthy, width: 24, height: 24)
                .AddEdge("branch-" + leaf, "branch", leaf, "leaf", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Forward);
        }

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot }.WithRelationshipRadialFocus("root", maxDepth: 2, maxFanout: 3));
        var leaves = prepared.Nodes.Where(node => node.Id.StartsWith("leaf-", StringComparison.Ordinal)).ToList();

        Assert(leaves.Count == 3, "The fanout fixture should include three second-hop leaves.");
        Assert(leaves.All(node => node.Metadata.TryGetValue("layout.radial.depth", out var depth) && depth == "2"), "Relationship-radial fanout should not let the visited parent consume a child expansion slot.");
        Assert(leaves.All(node => !node.Metadata.ContainsKey("layout.radial.overflow")), "Relationship-radial fanout should keep in-scope leaves out of the overflow ring.");
    }

    private static void TopologyRelationshipRadialLayoutHandlesLargeEgoGraphs() {
        var chart = TopologyChart.Create()
            .WithId("large-radial")
            .WithViewport(1680, 1060, 32)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.RelationshipRadial)
            .AddAutoNode("root", "Root", TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 42, height: 42, symbol: "R");

        for (var branch = 0; branch < 36; branch++) {
            var branchId = "branch-" + branch.ToString("00", CultureInfo.InvariantCulture);
            chart.AddAutoNode(branchId, "Branch " + branch.ToString(CultureInfo.InvariantCulture), TopologyNodeKind.Endpoint, TopologyHealthStatus.Healthy, width: 28, height: 28, symbol: "B")
                .AddEdge("root-" + branchId, "root", branchId, "talks", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional);
            for (var leaf = 0; leaf < 13; leaf++) {
                var leafId = branchId + "-leaf-" + leaf.ToString("00", CultureInfo.InvariantCulture);
                chart.AddAutoNode(leafId, "Leaf " + branch.ToString(CultureInfo.InvariantCulture) + "." + leaf.ToString(CultureInfo.InvariantCulture), TopologyNodeKind.Person, TopologyHealthStatus.Healthy, width: 20, height: 20, symbol: "L")
                    .AddEdge(branchId + "-" + leaf.ToString("00", CultureInfo.InvariantCulture), branchId, leafId, "leaf", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Forward);
            }
        }

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot }
            .WithRelationshipRadialFocus("root", maxDepth: 2, maxFanout: 40)
            .WithFitContentToViewport();
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var root = prepared.Nodes.Single(node => node.Id == "root");
        var firstHop = prepared.Nodes.Where(node => node.Id.StartsWith("branch-", StringComparison.Ordinal) && !node.Id.Contains("-leaf-", StringComparison.Ordinal)).ToList();
        var secondHop = prepared.Nodes.Where(node => node.Id.Contains("-leaf-", StringComparison.Ordinal)).ToList();
        var rootCenterX = root.X + root.Width / 2;
        var rootCenterY = root.Y + root.Height / 2;

        Assert(prepared.Nodes.Count == 505, "Large relationship-radial fixture should keep all 505 nodes present.");
        Assert(prepared.Edges.Count == 504, "Large relationship-radial fixture should keep all relationship edges present.");
        Assert(firstHop.Count == 36, "Large relationship-radial fixture should expand every first-hop conversation.");
        Assert(secondHop.Count == 468, "Large relationship-radial fixture should expand second-hop conversations.");
        Assert(firstHop.Select(node => Math.Round(double.Parse(node.Metadata["layout.radial.angle"], CultureInfo.InvariantCulture), 1)).Distinct().Count() > 24, "Large first-hop conversations should occupy many different radial directions.");
        Assert(secondHop.Average(node => Distance(node, rootCenterX, rootCenterY)) > firstHop.Average(node => Distance(node, rootCenterX, rootCenterY)) + 120, "Large second-hop conversations should remain farther out than direct conversations.");
        Assert(prepared.Nodes.All(node => node.Metadata.TryGetValue("layout.radial.root", out var value) && value == "root"), "Large relationship-radial nodes should expose the root for host focus logic.");
    }

    private static TopologyChart CreateForceGraphFixture(string id, int groupCount, int nodesPerGroup) {
        var chart = TopologyChart.Create()
            .WithId(id)
            .WithViewport(1180, 760, 28)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.ForceDirected);

        for (var groupIndex = 0; groupIndex < groupCount; groupIndex++) {
            var groupId = "group-" + groupIndex.ToString("00", CultureInfo.InvariantCulture);
            chart.AddAutoGroup(groupId, "Group " + (groupIndex + 1).ToString(CultureInfo.InvariantCulture), groupIndex % 3 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy, nodesPerGroup.ToString(CultureInfo.InvariantCulture) + " nodes", symbol: "G");
            chart.AddAutoNode(groupId + "-hub", "Hub " + (groupIndex + 1).ToString(CultureInfo.InvariantCulture), TopologyNodeKind.Service, TopologyHealthStatus.Healthy, groupId, "hub", width: 70, height: 46, symbol: "H");
            for (var nodeIndex = 0; nodeIndex < nodesPerGroup; nodeIndex++) {
                var nodeId = groupId + "-n" + nodeIndex.ToString("00", CultureInfo.InvariantCulture);
                chart.AddAutoNode(nodeId, "Node " + groupIndex.ToString(CultureInfo.InvariantCulture) + "-" + nodeIndex.ToString(CultureInfo.InvariantCulture), TopologyNodeKind.Team, nodeIndex % 5 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy, groupId, "cohort", width: 54, height: 42, symbol: string.Empty);
                chart.AddEdge(groupId + "-member-" + nodeIndex.ToString("00", CultureInfo.InvariantCulture), groupId + "-hub", nodeId, "member", TopologyEdgeKind.Membership, TopologyHealthStatus.Healthy, TopologyDirection.Forward);
                if (nodeIndex > 0) chart.AddEdge(groupId + "-peer-" + nodeIndex.ToString("00", CultureInfo.InvariantCulture), groupId + "-n" + (nodeIndex - 1).ToString("00", CultureInfo.InvariantCulture), nodeId, "peer", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional);
            }
        }

        for (var groupIndex = 0; groupIndex < groupCount; groupIndex++) {
            var nextGroup = (groupIndex + 1) % groupCount;
            chart.AddEdge(
                "bridge-" + groupIndex.ToString("00", CultureInfo.InvariantCulture),
                "group-" + groupIndex.ToString("00", CultureInfo.InvariantCulture) + "-n00",
                "group-" + nextGroup.ToString("00", CultureInfo.InvariantCulture) + "-n" + Math.Max(0, nodesPerGroup - 1).ToString("00", CultureInfo.InvariantCulture),
                "bridge",
                TopologyEdgeKind.Dependency,
                TopologyHealthStatus.Warning,
                TopologyDirection.Bidirectional);
        }

        return chart;
    }

    private static TopologyChart CreateBusyForceGraphFixture(string id, int groupCount, int nodesPerGroup) {
        var chart = CreateForceGraphFixture(id, groupCount, nodesPerGroup)
            .WithViewport(1500, 920, 32);

        for (var groupIndex = 0; groupIndex < groupCount; groupIndex++) {
            var groupId = "group-" + groupIndex.ToString("00", CultureInfo.InvariantCulture);
            for (var nodeIndex = 0; nodeIndex < nodesPerGroup; nodeIndex++) {
                var source = groupId + "-n" + nodeIndex.ToString("00", CultureInfo.InvariantCulture);
                var targetGroup = "group-" + ((groupIndex + 2 + nodeIndex % 3) % groupCount).ToString("00", CultureInfo.InvariantCulture);
                var target = targetGroup + "-n" + ((nodeIndex * 5 + groupIndex) % nodesPerGroup).ToString("00", CultureInfo.InvariantCulture);
                chart.AddEdge(
                    "busy-cross-" + groupIndex.ToString("00", CultureInfo.InvariantCulture) + "-" + nodeIndex.ToString("00", CultureInfo.InvariantCulture),
                    source,
                    target,
                    "cross",
                    nodeIndex % 4 == 0 ? TopologyEdgeKind.AuthenticationPath : TopologyEdgeKind.Dependency,
                    nodeIndex % 6 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy,
                    TopologyDirection.Forward);
            }
        }

        for (var groupIndex = 0; groupIndex < groupCount; groupIndex++) {
            var groupId = "group-" + groupIndex.ToString("00", CultureInfo.InvariantCulture);
            chart.AddEdge(
                "busy-hub-ring-" + groupIndex.ToString("00", CultureInfo.InvariantCulture),
                groupId + "-hub",
                "group-" + ((groupIndex + 1) % groupCount).ToString("00", CultureInfo.InvariantCulture) + "-hub",
                "hub",
                TopologyEdgeKind.Link,
                groupIndex % 3 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy,
                TopologyDirection.Bidirectional);
        }

        return chart;
    }

    private static int OverlapCount(IReadOnlyList<TopologyNode> nodes) {
        var count = 0;
        for (var i = 0; i < nodes.Count; i++) {
            for (var j = i + 1; j < nodes.Count; j++) {
                if (nodes[i].X < nodes[j].X + nodes[j].Width && nodes[i].X + nodes[i].Width > nodes[j].X && nodes[i].Y < nodes[j].Y + nodes[j].Height && nodes[i].Y + nodes[i].Height > nodes[j].Y) count++;
            }
        }

        return count;
    }
}
