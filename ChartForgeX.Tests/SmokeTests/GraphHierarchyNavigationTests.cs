using System;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GraphHierarchyContractValidatesAndRendersNavigation() {
        var scene = GraphScene.Create("hierarchy", "Service hierarchy")
            .AddNode("platform", "Platform", node => node.Level = 0)
            .AddNode("identity", "Identity", node => { node.ParentId = "platform"; node.Level = 1; node.SecondaryLabel = "Core capability"; })
            .AddNode("api", "API", node => { node.ParentId = "identity"; node.Level = 2; node.BadgeText = "12"; node.Status = "healthy"; })
            .AddEdge("platform-identity", "platform", "identity")
            .AddEdge("identity-api", "identity", "api");
        scene.Options.Enable(GraphSceneFeatures.HierarchyNavigation | GraphSceneFeatures.IncrementalUpdates);
        scene.Options.Hierarchy.InitialRootNodeId = "identity";
        scene.Options.Hierarchy.InitialDepth = 1;
        scene.Validate();

        var html = scene.ToGraphExplorerHtmlFragment(options => options.RenderBackend = HtmlGraphRenderBackend.WebGl);
        Assert(html.Contains("data-cfx-graph-renderer=\"webgl\"", StringComparison.Ordinal) && html.Contains("data-cfx-role=\"graph-webgl\"", StringComparison.Ordinal), "WebGL graph scenes should advertise and render a dedicated WebGL surface instead of silently routing to Canvas.");
        Assert(html.Contains("data-node-parent=\"identity\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-hierarchy-root=\"identity\"", StringComparison.Ordinal) && html.Contains("data-cfx-role=\"graph-breadcrumbs\"", StringComparison.Ordinal), "Hierarchy graph scenes should serialize parent links, initial view state, and breadcrumb navigation.");
        Assert(html.Contains("data-node-secondary-label=\"Core capability\"", StringComparison.Ordinal) && html.Contains("data-node-badge=\"12\"", StringComparison.Ordinal) && html.Contains("cfx-graph-node-status", StringComparison.Ordinal), "Detailed graph nodes should carry secondary labels, badges, and semantic status marks.");
        Assert(html.Contains("applyHierarchyView", StringComparison.Ordinal) && html.Contains("drillHierarchyNode", StringComparison.Ordinal) && html.Contains("cfxgraphnavigate", StringComparison.Ordinal), "The HTML adapter should ship real hierarchy navigation behavior and host events.");
        Assert(html.Contains("data-cfx-hierarchy-node", StringComparison.Ordinal) && html.Contains("stage.addEventListener('dblclick'", StringComparison.Ordinal) && html.Contains("navigateHierarchyUp(root)", StringComparison.Ordinal), "Hierarchy navigation should be available directly on graph nodes, clickable breadcrumb segments, and the graph background instead of requiring the Up control.");
        Assert(html.Contains("visibleMemberCount < 2", StringComparison.Ordinal) && html.Contains("circle.setAttribute('r', metrics.radius.toFixed(3))", StringComparison.Ordinal), "Expanded hierarchy clusters should suppress orphan hulls and resize surviving hulls to the active drill view.");
        Assert(html.Contains("window.ChartForgeXGraphExplorer", StringComparison.Ordinal) && html.Contains("applyGraphRuntimePatch", StringComparison.Ordinal), "IncrementalUpdates should expose a callable browser document API.");

        var missingParent = GraphScene.Create("missing-parent", "Missing parent").AddNode("child", "Child", node => node.ParentId = "missing");
        AssertThrows<InvalidOperationException>(() => missingParent.Validate(), "Graph hierarchy validation should reject missing parents before adapters render inconsistent views.");

        var cycle = GraphScene.Create("cycle", "Cycle")
            .AddNode("a", "A", node => node.ParentId = "b")
            .AddNode("b", "B", node => node.ParentId = "a");
        AssertThrows<InvalidOperationException>(() => cycle.Validate(), "Graph hierarchy validation should reject parent cycles.");
    }
}
