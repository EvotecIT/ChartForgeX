using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyMindMapBuilderCreatesBalancedBranches() {
        var chart = BuildMindMapFixture();
        var explicitPortEdge = chart.Edges.First(edge => edge.SourceNodeId == "entra" && edge.TargetNodeId == "users");
        explicitPortEdge.SourcePort = TopologyEdgePort.Bottom;
        explicitPortEdge.TargetPort = TopologyEdgePort.Top;
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
        var preparedExplicitPortEdge = prepared.Edges.First(edge => edge.SourceNodeId == "entra" && edge.TargetNodeId == "users");
        Assert(preparedExplicitPortEdge.SourcePort == TopologyEdgePort.Bottom && preparedExplicitPortEdge.TargetPort == TopologyEdgePort.Top, "Mind-map layout should preserve caller-specified edge ports.");

        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-layout-mode=\"MindMap\"", StringComparison.Ordinal), "Mind-map SVG should expose the layout mode.");
        Assert(svg.Contains("data-header-style=\"CenterBanner\"", StringComparison.Ordinal), "Mind-map style should render a centered title banner.");
        Assert(svg.Contains("data-node-icon-id=\"cloud:cloud\"", StringComparison.Ordinal), "Mind-map SVG should preserve resolved root icon ids.");
        Assert(svg.Contains("data-node-icon-id=\"people:team\"", StringComparison.Ordinal), "Mind-map SVG should preserve resolved branch icon ids.");
        Assert(svg.Contains("data-route-strategy=\"Orthogonal\"", StringComparison.Ordinal), "Mind-map branch routes should use deterministic orthogonal routing.");
        Assert(!svg.Contains("data-cfx-role=\"topology-node-status\"", StringComparison.Ordinal), "Mind-map style should omit operational status badges.");
        Assert(chart.ToPng(options).Length > 64, "Mind-map layout should render as PNG with the same topology model.");
    }

    private static void TopologyMindMapDirectLayoutBreaksReachableCycles() {
        var chart = TopologyChart.Create()
            .WithId("direct-mindmap-cycle")
            .WithViewport(640, 360, 24)
            .AddNode("root", "Root", 0, 0, TopologyNodeKind.Hub, width: 220, height: 88, iconId: "cloud:cloud")
            .AddNode("child", "Child", 0, 0, TopologyNodeKind.Application, width: 180, height: 64, iconId: "common:application")
            .AddEdge("root-child", "root", "child")
            .AddEdge("child-root", "child", "root")
            .WithLayout(TopologyLayoutMode.MindMap);

        var svg = chart.ToSvg(new TopologyRenderOptions().WithMindMapStyle().WithRequiredIcons());
        Assert(svg.Contains("data-layout-mode=\"MindMap\"", StringComparison.Ordinal), "Direct mind-map layout should render cyclic input without unbounded recursion.");
    }

    private static void TopologyMindMapRejectsExplicitLevelParentCycles() {
        AssertThrows<ArgumentException>(() => TopologyChart.Create().AddMindMap(new[] {
            new TopologyHierarchyItem("root", "Root"),
            new TopologyHierarchyItem("cycle-a", "Cycle A", "cycle-b") { Level = 1 },
            new TopologyHierarchyItem("cycle-b", "Cycle B", "cycle-a") { Level = 2 }
        }), "Mind-map builder should reject parent cycles even when callers provide explicit levels.");
    }

    private static void TopologyMindMapDirectLayoutUsesMetadataParentRoot() {
        var chart = TopologyChart.Create()
            .WithId("metadata-parent-root")
            .WithViewport(640, 360, 24)
            .AddNode("a-child", "Child", 0, 0, TopologyNodeKind.Application, width: 160, height: 56)
            .AddNode("z-root", "Root", 0, 0, TopologyNodeKind.Hub, width: 220, height: 88)
            .WithLayout(TopologyLayoutMode.MindMap);

        Node(chart, "a-child").Metadata["mindmap.parentId"] = "z-root";

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions().WithMindMapStyle());
        var root = Node(prepared, "z-root");
        var child = Node(prepared, "a-child");
        Assert(root.Metadata["mindmap.side"] == "center", "Direct mind-map layout should use metadata parent links when selecting the centered root.");
        Assert(child.X > root.X + root.Width, "Metadata-only mind-map children should be placed outward from the metadata parent root.");
    }

    private static void TopologyMindMapPreservesCurvedRouting() {
        var chart = TopologyChart.Create()
            .WithId("curved-mindmap")
            .WithViewport(640, 360, 24)
            .AddMindMap(new[] {
                new TopologyHierarchyItem("root", "Root"),
                new TopologyHierarchyItem("child", "Child", "root")
            }, new TopologyMindMapOptions { EdgeRouting = TopologyEdgeRouting.Curved });

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions().WithMindMapStyle());
        Assert(prepared.Edges.Count == 1 && prepared.Edges[0].Routing == TopologyEdgeRouting.Curved, "Mind-map layout should preserve caller-selected curved routing.");
    }

    private static void TopologyMindMapMarksOnlyParentlessRoot() {
        var chart = TopologyChart.Create()
            .WithId("explicit-level-root")
            .WithViewport(640, 360, 24)
            .AddMindMap(new[] {
                new TopologyHierarchyItem("z-root", "Root"),
                new TopologyHierarchyItem("a-child", "Child", "z-root") { Level = 0 }
            });

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions().WithMindMapStyle());
        var root = Node(prepared, "z-root");
        var child = Node(prepared, "a-child");
        Assert(root.Metadata["mindmap.side"] == "center", "Mind-map builder should mark only the validated parentless item as the root.");
        Assert(!child.Metadata.ContainsKey("mindmap.root"), "Explicit level zero should not make a child a second mind-map root.");
    }

    private static void TopologyMindMapDirectLayoutPlacesDetachedTrees() {
        var chart = TopologyChart.Create()
            .WithId("direct-mindmap-forest")
            .WithViewport(760, 420, 24)
            .AddNode("a-root", "Primary", 0, 0, TopologyNodeKind.Hub, width: 180, height: 72)
            .AddNode("a-child", "Primary Child", 0, 0, TopologyNodeKind.Application, width: 150, height: 52)
            .AddNode("z-root", "Detached", 0, 0, TopologyNodeKind.Service, width: 170, height: 58)
            .AddNode("z-child", "Detached Child", 0, 0, TopologyNodeKind.Queue, width: 150, height: 52)
            .AddEdge("a-root-child", "a-root", "a-child")
            .AddEdge("z-root-child", "z-root", "z-child")
            .WithLayout(TopologyLayoutMode.MindMap);

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions().WithMindMapStyle());
        var root = Node(prepared, "a-root");
        var detached = Node(prepared, "z-root");
        var detachedChild = Node(prepared, "z-child");
        Assert(root.Metadata["mindmap.side"] == "center", "Direct mind-map forest layout should still center the selected root.");
        Assert(detached.Metadata.ContainsKey("mindmap.side"), "Direct mind-map forest layout should place detached source-free trees instead of leaving them at default coordinates.");
        Assert(detachedChild.Metadata.ContainsKey("mindmap.side"), "Direct mind-map forest layout should continue placing children under detached source-free roots.");
    }

    private static void TopologyMindMapDirectLayoutPlacesDetachedCycles() {
        var chart = TopologyChart.Create()
            .WithId("direct-mindmap-detached-cycle")
            .WithViewport(760, 420, 24)
            .AddNode("root", "Root", 0, 0, TopologyNodeKind.Hub, width: 180, height: 72)
            .AddNode("child", "Child", 0, 0, TopologyNodeKind.Application, width: 150, height: 52)
            .AddNode("cycle-a", "Cycle A", 0, 0, TopologyNodeKind.Service, width: 150, height: 52)
            .AddNode("cycle-b", "Cycle B", 0, 0, TopologyNodeKind.Queue, width: 150, height: 52)
            .AddEdge("root-child", "root", "child")
            .AddEdge("cycle-a-b", "cycle-a", "cycle-b")
            .AddEdge("cycle-b-a", "cycle-b", "cycle-a")
            .WithLayout(TopologyLayoutMode.MindMap);

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions().WithMindMapStyle());
        var cycleA = Node(prepared, "cycle-a");
        var cycleB = Node(prepared, "cycle-b");
        Assert(cycleA.Metadata.ContainsKey("mindmap.side"), "Direct mind-map layout should place detached cyclic components that have no source-free node.");
        Assert(cycleB.Metadata.ContainsKey("mindmap.side"), "Direct mind-map layout should prune detached cycles after grafting the component under the selected root.");
    }

    private static void TopologyMindMapDirectLayoutFallsBackFromStaleParentMetadata() {
        var chart = TopologyChart.Create()
            .WithId("stale-parent-metadata")
            .WithViewport(640, 360, 24)
            .AddNode("root", "Root", 0, 0, TopologyNodeKind.Hub, width: 220, height: 88)
            .AddNode("child", "Child", 0, 0, TopologyNodeKind.Application, width: 160, height: 56)
            .AddEdge("root-child", "root", "child")
            .WithLayout(TopologyLayoutMode.MindMap);

        Node(chart, "child").Metadata["mindmap.parentId"] = "missing-parent";

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions().WithMindMapStyle());
        var root = Node(prepared, "root");
        var child = Node(prepared, "child");
        Assert(child.Metadata.ContainsKey("mindmap.side"), "Direct mind-map layout should fall back to valid edges when parent metadata points at a missing node.");
        Assert(child.X > root.X + root.Width, "Edge-derived children with stale parent metadata should be placed outward from the root.");
    }

    private static void TopologyMindMapDirectLayoutKeepsMetadataRootWithCrossLinks() {
        var chart = TopologyChart.Create()
            .WithId("metadata-root-cross-link")
            .WithViewport(640, 360, 24)
            .AddNode("a-child", "Child", 0, 0, TopologyNodeKind.Application, width: 160, height: 56)
            .AddNode("z-root", "Root", 0, 0, TopologyNodeKind.Hub, width: 220, height: 88)
            .AddEdge("child-root-cross-link", "a-child", "z-root")
            .WithLayout(TopologyLayoutMode.MindMap);

        Node(chart, "a-child").Metadata["mindmap.parentId"] = "z-root";

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions().WithMindMapStyle());
        var root = Node(prepared, "z-root");
        var child = Node(prepared, "a-child");
        Assert(root.Metadata["mindmap.side"] == "center", "Direct mind-map root discovery should prefer resolved parent metadata over cross-link edge targets.");
        Assert(child.X > root.X + root.Width, "Metadata children should still be placed outward when cross-links point back at the root.");
    }

    private static void TopologyMindMapDirectLayoutPrefersMetadataRootBeforeDetachedNodes() {
        var chart = TopologyChart.Create()
            .WithId("metadata-root-before-detached")
            .WithViewport(760, 420, 24)
            .AddNode("a-detached", "Detached", 0, 0, TopologyNodeKind.Service, width: 150, height: 52)
            .AddNode("b-child", "Child", 0, 0, TopologyNodeKind.Application, width: 150, height: 52)
            .AddNode("z-root", "Root", 0, 0, TopologyNodeKind.Cloud, width: 220, height: 88)
            .WithLayout(TopologyLayoutMode.MindMap);

        Node(chart, "b-child").Metadata["mindmap.parentId"] = "z-root";

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions().WithMindMapStyle());
        var root = Node(prepared, "z-root");
        var detached = Node(prepared, "a-detached");
        Assert(root.Metadata["mindmap.side"] == "center", "Direct mind-map root discovery should prefer the parentless metadata hierarchy root before sorted detached components.");
        Assert(detached.Metadata.ContainsKey("mindmap.side"), "Detached source-free nodes should still be placed after the metadata root is selected.");
    }

    private static void TopologyMindMapBuilderPreservesValidatedHierarchyMetadata() {
        var chart = TopologyChart.Create()
            .WithId("validated-mindmap-metadata")
            .WithViewport(720, 420, 24)
            .AddMindMap(new[] {
                new TopologyHierarchyItem("z-root", "Root"),
                new TopologyHierarchyItem("b-parent", "Other Parent", "z-root"),
                new TopologyHierarchyItem("a-child", "Child", "z-root") { Level = 2 }
                    .WithMetadata("mindmap.root", "true")
                    .WithMetadata("mindmap.parentId", "b-parent")
                    .WithMetadata("mindmap.level", "0")
                    .WithMetadata("layer", "0")
            });

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions().WithMindMapStyle());
        var root = Node(prepared, "z-root");
        var child = Node(prepared, "a-child");
        Assert(root.Metadata["mindmap.side"] == "center", "Mind-map builder should preserve the validated parentless root even when item metadata contains reserved root keys.");
        Assert(!child.Metadata.ContainsKey("mindmap.root"), "Mind-map builder should not allow item metadata to promote children to roots.");
        Assert(child.Metadata["mindmap.parentId"] == "z-root", "Mind-map builder should preserve validated parent ids over reserved item metadata.");
        Assert(child.Metadata["mindmap.level"] != "0" && child.Metadata["layer"] != "0", "Mind-map builder should preserve resolved level metadata over reserved item metadata.");
    }

    private static void TopologyMindMapBuilderStylesParentlessRootAsRoot() {
        var chart = TopologyChart.Create()
            .WithId("positive-level-root")
            .WithViewport(720, 420, 24)
            .AddMindMap(new[] {
                new TopologyHierarchyItem("root", "Root") { Level = 2 },
                new TopologyHierarchyItem("child", "Child", "root") { Level = 3 }
            }, new TopologyMindMapOptions {
                RootWidth = 280,
                RootHeight = 120,
                RootDisplayMode = TopologyNodeDisplayMode.Card,
                BranchWidth = 180,
                BranchHeight = 64,
                BranchDisplayMode = TopologyNodeDisplayMode.Pill
            });

        var root = Node(chart, "root");
        Assert(root.Width == 280 && root.Height == 120, "Parentless mind-map roots should use root size defaults even when explicit levels start above zero.");
        Assert(root.DisplayMode == TopologyNodeDisplayMode.Card, "Parentless mind-map roots should use the root display default even when explicit levels start above zero.");
        Assert(root.Metadata["mindmap.level"] == "2", "Explicit source levels should still be preserved as metadata.");
    }

    private static void TopologyMindMapCenterBannerClampsNarrowViewportWidth() {
        var chart = TopologyChart.Create()
            .WithId("narrow-banner")
            .WithTitle("Narrow")
            .WithViewport(120, 180, 80)
            .AddNode("root", "Root", 0, 0, TopologyNodeKind.Hub, width: 64, height: 42);

        var options = new TopologyRenderOptions { HeaderStyle = TopologyHeaderStyle.CenterBanner, IncludeLegend = false };
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-header-style=\"CenterBanner\"", StringComparison.Ordinal), "Center banner header should still render for narrow padded viewports.");
        Assert(!svg.Contains("width=\"-", StringComparison.Ordinal), "Center banner header should not emit negative SVG dimensions when padding consumes the viewport width.");
        Assert(chart.ToPng(options).Length > 64, "Center banner PNG export should tolerate narrow padded viewports.");
    }

    private static void TopologyMindMapInfersReverseSameBranchPortsFromPositions() {
        var chart = TopologyChart.Create()
            .WithId("reverse-branch-link")
            .WithViewport(760, 420, 24)
            .AddMindMap(new[] {
                new TopologyHierarchyItem("root", "Root"),
                new TopologyHierarchyItem("parent", "Parent", "root").WithMetadata("mindmap.side", "left"),
                new TopologyHierarchyItem("child", "Child", "parent")
            });

        chart.AddEdge("child-parent-back", "child", "parent");

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions().WithMindMapStyle());
        var parent = Node(prepared, "parent");
        var child = Node(prepared, "child");
        var edge = prepared.Edges.First(item => item.Id == "child-parent-back");
        Assert(child.X < parent.X, "Mind-map fixture should place the child farther left than its same-branch parent.");
        Assert(edge.SourcePort == TopologyEdgePort.Right && edge.TargetPort == TopologyEdgePort.Left, "Reverse same-branch mind-map links should infer ports from source and target positions.");
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
