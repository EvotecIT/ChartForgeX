using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyMindMapBuilderCreatesBalancedBranches() {
        var chart = BuildMindMapFixture();
        var options = new TopologyRenderOptions()
            .WithMindMapStyle()
            .WithRequiredIcons()
            .WithMultilineNodeLabels();

        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var root = Node(prepared, "entra");
        var users = Node(prepared, "users");
        var devices = Node(prepared, "devices");
        var mfa = Node(prepared, "mfa-registration");
        var hybrid = Node(prepared, "hybrid-cloud-sync");

        Assert(prepared.LayoutMode == TopologyLayoutMode.MindMap, "Mind-map builder should select the mind-map layout mode.");
        Assert(root.Metadata["mindmap.side"] == "center", "Mind-map root should be marked as the center node.");
        Assert(users.X > root.X + root.Width, "Mind-map layout should place right-side branches to the right of the root.");
        Assert(devices.X < root.X, "Mind-map layout should place left-side branches to the left of the root.");
        Assert(mfa.X > users.X, "Mind-map descendants should continue outward on their branch side.");
        Assert(hybrid.X < devices.X, "Left-side mind-map descendants should continue outward on the left branch.");
        Assert(users.Width >= 220, "Mind-map branch cards should preserve readable builder widths.");
        Assert(mfa.Width >= 220, "Mind-map leaf pills should preserve readable builder widths.");
        Assert(prepared.Edges.All(edge => edge.SourcePort != TopologyEdgePort.Auto && edge.TargetPort != TopologyEdgePort.Auto), "Mind-map branch routes should infer explicit source and target ports.");

        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-layout-mode=\"MindMap\"", StringComparison.Ordinal), "Mind-map SVG should expose the layout mode.");
        Assert(svg.Contains("data-header-style=\"CenterBanner\"", StringComparison.Ordinal), "Mind-map style should render a centered title banner.");
        Assert(svg.Contains("data-node-icon-id=\"cloud:cloud\"", StringComparison.Ordinal), "Mind-map SVG should preserve resolved root icon ids.");
        Assert(svg.Contains("data-node-icon-id=\"people:team\"", StringComparison.Ordinal), "Mind-map SVG should preserve resolved branch icon ids.");
        Assert(svg.Contains("data-route-strategy=\"Orthogonal\"", StringComparison.Ordinal), "Mind-map branch routes should use deterministic orthogonal routing.");
        Assert(!svg.Contains("data-cfx-role=\"topology-node-status\"", StringComparison.Ordinal), "Mind-map style should omit operational status badges.");
        Assert(chart.ToPng(options).Length > 64, "Mind-map layout should render as PNG with the same topology model.");
    }

    private static void TopologyMindMapRequiresOneRoot() {
        AssertThrows<ArgumentException>(() => TopologyChart.Create().AddMindMap(new[] {
            new TopologyHierarchyItem("one", "One"),
            new TopologyHierarchyItem("two", "Two")
        }), "Mind-map builder should reject multiple root nodes because centered mind maps need one root.");
    }

    private static void TopologyRequiredIconsRejectMissingMindMapIcons() {
        var chart = TopologyChart.Create()
            .WithId("strict-mindmap-icons")
            .WithViewport(720, 420, 24)
            .AddMindMap(new[] {
                new TopologyHierarchyItem("root", "Root") { IconId = "cloud:cloud" },
                new TopologyHierarchyItem("missing", "Missing Icon", "root") { IconId = "microsoft-entra:not-real" }
            });

        try {
            chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }.WithRequiredIcons());
        } catch (TopologyValidationException ex) {
            Assert(ex.Result.Errors.Any(error => error.Code == "missing-node-icon" && error.ItemId == "missing"), "Required icon validation should identify the unresolved mind-map node icon.");
            return;
        }

        throw new InvalidOperationException("Required icon validation should reject unresolved mind-map icon ids.");
    }

    private static TopologyChart BuildMindMapFixture() {
        return TopologyChart.Create()
            .WithId("entra-mind-map")
            .WithTitle("Microsoft Entra Mind Map")
            .WithViewport(980, 560, 28)
            .AddMindMap(new[] {
                new TopologyHierarchyItem("entra", "Microsoft Entra") { IconId = "cloud:cloud", Kind = TopologyNodeKind.Cloud, Status = TopologyHealthStatus.Healthy, Color = "#34489A" },
                new TopologyHierarchyItem("users", "Users", "entra") { IconId = "people:person", Kind = TopologyNodeKind.Person, Status = TopologyHealthStatus.Healthy }.WithMetadata("mindmap.side", "right").WithMetadata("mindmap.order", "10"),
                new TopologyHierarchyItem("groups", "Groups", "entra") { IconId = "people:team", Kind = TopologyNodeKind.Team, Status = TopologyHealthStatus.Healthy }.WithMetadata("mindmap.side", "right").WithMetadata("mindmap.order", "20"),
                new TopologyHierarchyItem("protection", "ID Protection", "users") { IconId = "common:certificate", Kind = TopologyNodeKind.Certificate, Status = TopologyHealthStatus.Warning },
                new TopologyHierarchyItem("mfa-registration", "MFA Registration Policy", "protection") { IconId = "common:service", Kind = TopologyNodeKind.Service, Status = TopologyHealthStatus.Healthy },
                new TopologyHierarchyItem("devices", "Devices", "entra") { IconId = "common:desktop", Kind = TopologyNodeKind.Endpoint, Status = TopologyHealthStatus.Healthy }.WithMetadata("mindmap.side", "left").WithMetadata("mindmap.order", "10"),
                new TopologyHierarchyItem("applications", "Applications", "entra") { IconId = "common:application", Kind = TopologyNodeKind.Application, Status = TopologyHealthStatus.Healthy }.WithMetadata("mindmap.side", "left").WithMetadata("mindmap.order", "20"),
                new TopologyHierarchyItem("hybrid", "Hybrid management", "devices") { IconId = "network:wan-link", Kind = TopologyNodeKind.Network, Status = TopologyHealthStatus.Healthy },
                new TopologyHierarchyItem("hybrid-cloud-sync", "Cloud Sync", "hybrid") { IconId = "cloud:cloud", Kind = TopologyNodeKind.Cloud, Status = TopologyHealthStatus.Healthy },
                new TopologyHierarchyItem("app-roles", "App roles", "applications") { IconId = "common:service", Kind = TopologyNodeKind.Service, Status = TopologyHealthStatus.Healthy }
            });
    }

    private static TopologyNode Node(TopologyChart chart, string id) =>
        chart.Nodes.First(node => string.Equals(node.Id, id, StringComparison.Ordinal));
}
