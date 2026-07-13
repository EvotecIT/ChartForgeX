using System;
using System.IO;
using System.Linq;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void InteractiveJavaScriptAssetsStayGeneratedFromSourceFragments() {
        var root = FindRepositoryRoot();
        var syncScript = Path.Combine(root, "Build", "Sync-InteractiveAssets.ps1");
        Assert(File.Exists(syncScript), "Interactive JS assets should include a dependency-free source sync script.");

        AssertGeneratedAssetMatchesSource(
            root,
            Path.Combine("ChartForgeX.Interactivity.Html", "Assets", "topology-interaction.source"),
            Path.Combine("ChartForgeX.Interactivity.Html", "Assets", "topology-interaction.js"));

        AssertGeneratedAssetMatchesSource(
            root,
            Path.Combine("ChartForgeX.Interactivity.Html", "Assets", "interactive.source"),
            Path.Combine("ChartForgeX.Interactivity.Html", "Assets", "interactive.js"));
    }

    private static void GraphExplorerCanvasConsumersPreferLivePhysicsState() {
        var assetRoot = Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity.Html", "Assets");
        var bindings = File.ReadAllText(Path.Combine(assetRoot, "graph-explorer.30-bindings.js"));
        var pointers = File.ReadAllText(Path.Combine(assetRoot, "graph-explorer.27-pointer-interactions.js"));
        const string liveState = "root.__cfxGraphState || graphState(root)";
        var liveStateUses = (bindings + pointers).Split(new[] { liveState }, StringSplitOptions.None).Length - 1;
        Assert(liveStateUses >= 7, "Canvas dragging and SVG, PNG, and JSON exports should consume live physics coordinates before falling back to hidden SVG attributes.");
        Assert(bindings.Contains("indexHitTesting(root, graphState(root))", StringComparison.Ordinal), "Initial graph binding should still build a fresh state before the live cache exists.");
        var script = HtmlGraphExplorerRenderer.BuildInteractionScript();
        var bootstrap = script.IndexOf("const start = () => roots().forEach(bind);", StringComparison.Ordinal);
        Assert(bootstrap > script.IndexOf("const bindPhysicsConfigurator = (root) =>", StringComparison.Ordinal) && bootstrap > script.IndexOf("window.ChartForgeXGraphExplorer = Object.freeze(graphExplorerApi);", StringComparison.Ordinal), "Graph explorer bootstrap should run after every runtime definition so fragments injected into loaded documents bind without temporal-dead-zone errors.");
    }

    private static void GraphExplorerRuntimePatchesPreserveVisualAndReferenceContracts() {
        var assetRoot = Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity.Html", "Assets");
        var api = File.ReadAllText(Path.Combine(assetRoot, "graph-explorer.40-api.js"));
        var hierarchy = File.ReadAllText(Path.Combine(assetRoot, "graph-explorer.13-hierarchy.js"));
        Assert(api.Contains("edges.forEach((edge, edgeId)", StringComparison.Ordinal) && api.Contains("references a missing endpoint", StringComparison.Ordinal), "Browser graph patches should validate every surviving edge against the final node set before mutating the document.");
        Assert(api.Contains("nodePolygonPoints", StringComparison.Ordinal) && api.Contains("nodeDatabasePath", StringComparison.Ordinal) && api.Contains("shape === 'text'", StringComparison.Ordinal), "Browser graph patches should rebuild rich SVG node shapes through the shared graph geometry helpers instead of reducing them to circles.");
        Assert(api.Contains("setGraphAttribute(element, 'marker-start'", StringComparison.Ordinal) && api.Contains("setGraphAttribute(element, 'marker-end'", StringComparison.Ordinal), "Browser graph patches should restore SVG direction markers for upserted edges.");
        Assert(api.Contains("syncGraphPatchClusterMembership", StringComparison.Ordinal) && api.Contains("data-source-cluster-id", StringComparison.Ordinal) && api.Contains("data-target-cluster-id", StringComparison.Ordinal), "Browser graph patches should synchronize node moves across cluster memberships and edge cluster references.");
        Assert(api.Contains("graphPatchEdgeStyle", StringComparison.Ordinal) && api.Contains("setGraphAttribute(element, 'style', graphPatchEdgeStyle(edge, style))", StringComparison.Ordinal), "Browser graph patches should apply and clear edge stroke, width, dash, and visibility styles on SVG paths.");
        Assert(api.Contains("graphPatchRoutePoints", StringComparison.Ordinal) && api.Contains("'data-edge-route-points', graphPatchRoutePoints(edge.routePoints)", StringComparison.Ordinal), "Browser graph patches should serialize prepared polyline route points into runtime graph state.");
        Assert(api.Contains("cfx-graph-node-label-bg", StringComparison.Ordinal) && api.Contains("labelBackgroundColor", StringComparison.Ordinal), "Browser graph patches should rebuild styled node label backgrounds instead of dropping them until a full render.");
        Assert(api.Contains("--cfx-node-fill", StringComparison.Ordinal) && api.Contains("--cfx-node-stroke", StringComparison.Ordinal) && api.Contains("filter:drop-shadow", StringComparison.Ordinal), "Browser graph patches should apply node colors and shadows through the same reusable style contract as the initial SVG renderer.");
        Assert(api.Contains("graphPatchArrowMarker", StringComparison.Ordinal) && api.Contains("path.setAttribute('style', `fill:${color};stroke:${color}`)", StringComparison.Ordinal), "Browser graph patches should keep custom-colored arrow markers aligned with their edge stroke.");
        Assert(api.Contains("detachGraphPatchRemovedClusters", StringComparison.Ordinal) && api.Contains("setGraphAttribute(node, 'data-node-cluster', null)", StringComparison.Ordinal), "Browser graph patches should mirror core atomic removal semantics by clearing node-side references to removed clusters.");
        Assert(api.Contains("if (graphChanged) { stopWorkerPhysics(root, true); stopMainPhysics(root, true); }", StringComparison.Ordinal), "Fit-only and no-op browser patches should not terminate a running physics simulation while leaving its state reported as running.");
        Assert(hierarchy.Contains("applyLayout(root, state);", StringComparison.Ordinal), "Hierarchy updates should run the shared layout synchronization path so newly patched SVG edges receive visible path geometry.");
        Assert(hierarchy.Contains("if (event.defaultPrevented ||", StringComparison.Ordinal), "Hierarchy shortcuts should not reinterpret arrow keys already handled by graph-item or accelerated-surface keyboard navigation.");
        var pointers = File.ReadAllText(Path.Combine(assetRoot, "graph-explorer.27-pointer-interactions.js"));
        Assert(pointers.Contains("overlayRole === 'graph-node' ? state.nodes.find(item => item.id === overlayId)", StringComparison.Ordinal) && pointers.Contains("runtimeOverlay && hitCanSelect && hitItem.el", StringComparison.Ordinal), "Accelerated SVG overlays should resolve stable virtual graph ids before deciding selection, dragging, or viewport panning.");
        var bindings = File.ReadAllText(Path.Combine(assetRoot, "graph-explorer.30-bindings.js"));
        var stateSync = File.ReadAllText(Path.Combine(assetRoot, "graph-explorer.11-state-sync.js"));
        var svgExport = File.ReadAllText(Path.Combine(assetRoot, "graph-explorer.28-svg-export.js"));
        Assert(bindings.Contains("imageAlt: attr(node.el, 'data-node-image-alt') ||", StringComparison.Ordinal), "Accelerated JSON exports should preserve compact-document image alternative text without requiring a physical child image element.");
        Assert(bindings.Contains("bindAcceleratedSvgKeyboard(root)", StringComparison.Ordinal) && stateSync.Contains("scene.setAttribute('tabindex', acceleratedSvg ? '0' : '-1')", StringComparison.Ordinal) && svgExport.Contains("'aria-hidden': 'true'", StringComparison.Ordinal), "Accelerated SVG scenes should expose one stable keyboard surface while keeping transient overlay nodes out of the tab order and accessibility tree.");
        var webGl = File.ReadAllText(Path.Combine(assetRoot, "graph-explorer.04-webgl.js"));
        Assert(webGl.Contains("edgeHasRoute(rendered) ? routeRenderPoints(rendered) : [rendered.source, rendered.target]", StringComparison.Ordinal) && webGl.Contains("for (let index = 1; index < points.length; index++)", StringComparison.Ordinal), "WebGL graph rendering should emit every prepared polyline route segment instead of collapsing large-scene routes to one chord.");
        Assert(webGl.Contains("edgeArrowGeometry(rendered, control, side)", StringComparison.Ordinal) && webGl.Contains("rendered.targetArrow || rendered.directed", StringComparison.Ordinal), "WebGL graph rendering should preserve source and target direction with arrowhead geometry on large directed scenes.");
    }

    private static void AssertGeneratedAssetMatchesSource(string root, string sourceRelativePath, string targetRelativePath) {
        var sourceDirectory = Path.Combine(root, sourceRelativePath);
        var targetPath = Path.Combine(root, targetRelativePath);
        Assert(Directory.Exists(sourceDirectory), "Interactive JS source fragments should exist: " + sourceRelativePath);
        Assert(File.Exists(targetPath), "Interactive JS generated output should exist: " + targetRelativePath);

        var parts = Directory.EnumerateFiles(sourceDirectory, "*.js", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();
        Assert(parts.Length >= 3, "Interactive JS assets should be split into maintainable source fragments: " + sourceRelativePath);

        var generated = string.Join("\n", parts.Select(part => NormalizeAsset(File.ReadAllText(part)).TrimEnd('\n'))) + "\n";
        var current = NormalizeAsset(File.ReadAllText(targetPath));
        Assert(current == generated, "Generated interactive JS asset is out of date: " + targetRelativePath + ". Run Build/Sync-InteractiveAssets.ps1.");
    }

    private static string NormalizeAsset(string value) => value.Replace("\r\n", "\n").Replace("\r", "\n");
}
