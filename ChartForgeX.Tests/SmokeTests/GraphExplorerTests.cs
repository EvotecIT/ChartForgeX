using System;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GraphSceneContractsModelExplorerPhysicsAndLod() {
        var scene = SampleGraphScene();
        scene.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.Export | GraphSceneFeatures.PerformanceTelemetry);
        scene.Options.Physics.Solver = GraphPhysicsSolver.BarnesHut;
        scene.Options.Physics.StabilizationIterations = 700;
        scene.Options.LevelOfDetail.ClusterNodeThreshold = 50;
        scene.Options.LevelOfDetail.HideEdgeLabelsThreshold = 80;
        scene.Options.LevelOfDetail.CompactNodeThreshold = 120;
        scene.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 400;
        scene.Options.LevelOfDetail.CollapseClustersOnLoad = true;
        scene.Options.Performance.FrameBudgetMilliseconds = 12;
        scene.Options.Performance.MaxInteractiveSvgNodes = 900;
        scene.Options.Performance.MaxInteractiveSvgEdges = 1800;
        scene.Options.Performance.MaxInteractiveCanvasNodes = 4000;
        scene.Options.Performance.MaxInteractiveCanvasEdges = 9000;
        scene.Options.Performance.TelemetrySampleInterval = 10;
        scene.Metadata["source"] = "smoke";

        scene.Validate();

        Assert(scene.Nodes.Count == 3 && scene.Edges.Count == 2 && scene.Clusters.Count == 1, "Graph scenes should preserve product-neutral nodes, edges, and clusters.");
        Assert(scene.Options.HasFeature(GraphSceneFeatures.MultiSelection), "Graph scenes should include multi-selection in the reusable Explorer feature group.");
        Assert(scene.Options.HasFeature(GraphSceneFeatures.RuntimePhysics) && scene.Options.HasFeature(GraphSceneFeatures.Stabilization), "Graph scenes should model runtime physics and stabilization as reusable feature flags.");
        Assert(scene.Options.Physics.Solver == GraphPhysicsSolver.BarnesHut && scene.Options.Physics.StabilizationIterations == 700, "Graph scenes should carry solver profiles without binding the core contract to a browser engine.");
        Assert(scene.Options.LevelOfDetail.CollapseClustersOnLoad && scene.Options.LevelOfDetail.CanvasPreferredNodeThreshold == 400, "Graph scenes should carry clustering and LOD thresholds for large object counts.");
        Assert(scene.Options.Performance.FrameBudgetMilliseconds == 12 && scene.Options.Performance.MaxInteractiveSvgNodes == 900 && scene.Options.Performance.MaxInteractiveCanvasNodes == 4000 && scene.Options.Performance.TelemetrySampleInterval == 10, "Graph scenes should carry reusable performance budgets for SVG and Canvas graph runtimes.");
        Assert(scene.Metadata["source"] == "smoke" && scene.Nodes[0].Metadata["owner"] == "identity", "Graph scenes should carry host metadata for search, tooltips, and inspectors.");
        Assert(scene.Nodes[0].Shape == GraphNodeShape.Image && scene.Nodes[0].ImageUrl != null && scene.Nodes[0].IconText == "A", "Graph scenes should model image and icon nodes without binding to an adapter.");
        Assert(scene.Edges[0].Directed && scene.Edges[0].Shape == GraphEdgeShape.Curve && scene.Edges[0].Dashed, "Graph scenes should model directional rich edges for adapters that support arrows, curves, and dashed strokes.");

        var duplicateNode = GraphScene.Create("duplicate-node", "Duplicate node")
            .AddNode("api", "API")
            .AddNode("api", "API copy");
        AssertThrows<InvalidOperationException>(() => duplicateNode.Validate(), "Graph scenes should reject duplicate node ids before adapters render misleading state.");

        var missingEdgeTarget = GraphScene.Create("missing-target", "Missing target")
            .AddNode("api", "API")
            .AddEdge("api-db", "api", "db");
        AssertThrows<InvalidOperationException>(() => missingEdgeTarget.Validate(), "Graph scenes should reject edges that reference missing nodes.");

        var missingClusterNode = GraphScene.Create("missing-cluster-node", "Missing cluster node")
            .AddNode("api", "API")
            .AddCluster("core", "Core", new[] { "api", "db" });
        AssertThrows<InvalidOperationException>(() => missingClusterNode.Validate(), "Graph scenes should reject clusters that reference missing nodes.");
    }

    private static void GraphExplorerHtmlAdapterRendersSelfContainedScene() {
        var scene = SampleGraphScene();
        scene.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.DragNodes | GraphSceneFeatures.Export | GraphSceneFeatures.PerformanceTelemetry);
        scene.Options.Physics.Solver = GraphPhysicsSolver.ForceAtlas2;
        scene.Options.Physics.StabilizationIterations = 900;
        scene.Options.LevelOfDetail.ClusterNodeThreshold = 2;
        scene.Options.LevelOfDetail.CompactNodeThreshold = 3;
        scene.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 5;
        scene.Options.LevelOfDetail.CollapseClustersOnLoad = true;
        scene.Options.Performance.FrameBudgetMilliseconds = 10;
        scene.Options.Performance.MaxInteractiveSvgNodes = 1000;
        scene.Options.Performance.MaxInteractiveSvgEdges = 2000;
        scene.Options.Performance.MaxInteractiveCanvasNodes = 6000;
        scene.Options.Performance.MaxInteractiveCanvasEdges = 14000;
        scene.Options.Performance.TelemetrySampleInterval = 7;

        var html = scene.ToGraphExplorerHtmlPage(options => {
            options.PageTitle = "Graph Explorer";
            options.ScriptNonce = "nonce-graph";
        });

        Assert(html.Contains("<title>Graph Explorer</title>", StringComparison.Ordinal), "Graph explorer pages should honor configured page titles.");
        Assert(html.Contains("<script nonce=\"nonce-graph\">", StringComparison.Ordinal), "Graph explorer pages should support CSP nonces.");
        Assert(html.Contains("data-cfx-graph-id=\"service-map\"", StringComparison.Ordinal), "Graph explorer pages should expose a stable scene id.");
        Assert(html.Contains("data-cfx-graph-renderer=\"svg\"", StringComparison.Ordinal), "Graph explorer pages should default to a dependency-free SVG renderer.");
        Assert(html.Contains("data-cfx-graph-canvas-fallback=\"true\"", StringComparison.Ordinal), "Graph explorer pages should allow threshold-based Canvas fallback by default.");
        Assert(html.Contains("data-cfx-graph-physics=\"ForceAtlas2\"", StringComparison.Ordinal), "Graph explorer pages should expose the requested physics profile.");
        Assert(html.Contains("data-cfx-graph-stabilization-iterations=\"900\"", StringComparison.Ordinal), "Graph explorer pages should expose stabilization budgets.");
        Assert(html.Contains("data-cfx-graph-repulsion=\"4500\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-damping=\"0.09\"", StringComparison.Ordinal), "Graph explorer pages should expose force simulation constants.");
        Assert(html.Contains("data-cfx-lod-cluster-threshold=\"2\"", StringComparison.Ordinal) && html.Contains("data-cfx-lod-compact-node-threshold=\"3\"", StringComparison.Ordinal) && html.Contains("data-cfx-lod-canvas-threshold=\"5\"", StringComparison.Ordinal), "Graph explorer pages should expose LOD thresholds.");
        Assert(html.Contains("data-cfx-lod-collapse-clusters=\"true\"", StringComparison.Ordinal), "Graph explorer pages should expose default cluster collapse state.");
        Assert(html.Contains("data-cfx-performance-frame-budget=\"10\"", StringComparison.Ordinal) && html.Contains("data-cfx-performance-max-svg-nodes=\"1000\"", StringComparison.Ordinal) && html.Contains("data-cfx-performance-max-canvas-nodes=\"6000\"", StringComparison.Ordinal) && html.Contains("data-cfx-performance-telemetry-interval=\"7\"", StringComparison.Ordinal), "Graph explorer pages should expose SVG and Canvas performance budgets.");
        Assert(html.Contains("data-cfx-role=\"graph-canvas\"", StringComparison.Ordinal) && html.Contains("class=\"cfx-graph-canvas\"", StringComparison.Ordinal), "Graph explorer pages should include a real Canvas rendering target for large-object fallback.");
        Assert(html.Contains("data-cfx-graph-search=\"true\"", StringComparison.Ordinal), "Graph explorer pages should include search controls.");
        Assert(html.Contains("data-cfx-graph-filter=\"status\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-filter=\"kind\"", StringComparison.Ordinal), "Graph explorer pages should include reusable status and kind filters.");
        Assert(html.Contains("data-cfx-graph-action=\"clusters\"", StringComparison.Ordinal), "Graph explorer pages should include cluster controls when clusters are present.");
        Assert(html.Contains("data-cfx-graph-action=\"focus\"", StringComparison.Ordinal), "Graph explorer pages should include selected-neighborhood focus controls when neighborhood exploration is enabled.");
        Assert(html.Contains("data-cfx-graph-action=\"clear-selection\"", StringComparison.Ordinal), "Graph explorer pages should include an explicit clear-selection control when multi-selection is enabled.");
        Assert(html.Contains("data-cfx-graph-action=\"physics\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-action=\"stabilize\"", StringComparison.Ordinal), "Graph explorer pages should include runtime physics controls when enabled.");
        Assert(html.Contains("data-cfx-graph-action=\"export-svg\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-action=\"export-png\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-action=\"export-json\"", StringComparison.Ordinal), "Graph explorer pages should include SVG, PNG, and JSON export controls when export is enabled.");
        Assert(html.Contains("data-cfx-role=\"graph-node\"", StringComparison.Ordinal) && html.Contains("data-node-id=\"api\"", StringComparison.Ordinal), "Graph explorer SVG should expose graph node metadata.");
        Assert(html.Contains("data-node-size=\"11\"", StringComparison.Ordinal) && html.Contains("data-node-fixed=\"true\"", StringComparison.Ordinal), "Graph explorer SVG should expose node size and fixed-position hints for physics adapters.");
        Assert(html.Contains("data-node-shape=\"image\"", StringComparison.Ordinal) && html.Contains("data-node-image-url=\"data:image/svg+xml", StringComparison.Ordinal) && html.Contains("data-node-icon=\"A\"", StringComparison.Ordinal), "Graph explorer SVG should expose image and icon node metadata.");
        Assert(html.Contains("<image href=\"data:image/svg+xml", StringComparison.Ordinal), "Graph explorer SVG should render image-backed nodes without external dependencies.");
        Assert(html.Contains("data-cfx-role=\"graph-edge\"", StringComparison.Ordinal) && html.Contains("data-source-node-id=\"api\"", StringComparison.Ordinal) && html.Contains("data-target-node-id=\"db\"", StringComparison.Ordinal), "Graph explorer SVG should expose graph edge metadata.");
        Assert(html.Contains("data-edge-weight=\"2\"", StringComparison.Ordinal) && html.Contains("data-edge-length=\"140\"", StringComparison.Ordinal), "Graph explorer SVG should expose edge physics hints.");
        Assert(html.Contains("data-edge-directed=\"true\"", StringComparison.Ordinal) && html.Contains("data-edge-shape=\"curve\"", StringComparison.Ordinal) && html.Contains("marker-end=\"url(#service-map-arrow)\"", StringComparison.Ordinal), "Graph explorer SVG should render directional curved edge contracts.");
        Assert(html.Contains("data-cfx-role=\"graph-edge-label\"", StringComparison.Ordinal), "Graph explorer SVG should render relationship labels as addressable graph output.");
        Assert(html.Contains("data-cfx-role=\"graph-cluster\"", StringComparison.Ordinal) && html.Contains("data-cluster-node-ids=\"api,db\"", StringComparison.Ordinal), "Graph explorer SVG should expose reusable cluster summaries.");
        Assert(html.Contains("requestAnimationFrame(step)", StringComparison.Ordinal) && html.Contains("physicsTick", StringComparison.Ordinal), "Graph explorer runtime should run dependency-free browser physics instead of only exposing buttons.");
        Assert(html.Contains("barnesHutTree", StringComparison.Ordinal) && html.Contains("data-cfx-graph-physics=\"ForceAtlas2\"", StringComparison.Ordinal) && html.Contains("cfxGraphPhysicsAcceleration", StringComparison.Ordinal), "Graph explorer runtime should include scalable many-body acceleration and expose the active physics path for large graph diagnostics.");
        Assert(html.Contains("workerPhysicsSource", StringComparison.Ordinal) && html.Contains("cfxGraphPhysicsThread", StringComparison.Ordinal) && html.Contains("thread: 'worker'", StringComparison.Ordinal), "Graph explorer runtime should move large Barnes-Hut stabilization to a dependency-free Web Worker when the browser allows it.");
        Assert(html.Contains("drawCanvas", StringComparison.Ordinal) && html.Contains("bindCanvasHitTesting", StringComparison.Ordinal), "Graph explorer runtime should draw and hit-test the dependency-free Canvas backend.");
        Assert(html.Contains("if (raw === '') return fallback", StringComparison.Ordinal), "Graph explorer runtime should preserve numeric defaults for absent viewport and performance attributes.");
        Assert(html.Contains("sampleTicks", StringComparison.Ordinal) && html.Contains("cfxGraphPerformanceSampleBudgetMs", StringComparison.Ordinal), "Graph explorer runtime should compare performance samples against the effective sampled-tick budget.");
        Assert(html.Contains("indexHitTesting", StringComparison.Ordinal) && html.Contains("cfxGraphHitTest", StringComparison.Ordinal) && html.Contains("hitTestNodes", StringComparison.Ordinal), "Graph explorer runtime should expose spatial hit testing for large-scene Canvas selection and dragging.");
        Assert(html.Contains("return { nodes, edges, clusters, byId }", StringComparison.Ordinal) && html.Contains("state.byId || new Map", StringComparison.Ordinal), "Graph explorer runtime should reuse node indexes during layout and Canvas drawing.");
        Assert(html.Contains("const labels = new Map(items(root, '[data-cfx-role=\"graph-edge-label\"]')", StringComparison.Ordinal), "Graph explorer runtime should update edge labels from a per-layout label index.");
        Assert(html.Contains("return best || domHitNodeAt(root, point)", StringComparison.Ordinal) && html.Contains("items(root, '[data-cfx-role=\"graph-node\"]')", StringComparison.Ordinal) && html.Contains("data-node-x", StringComparison.Ordinal) && html.Contains("data-node-y", StringComparison.Ordinal), "Graph explorer Canvas hit testing should use indexed state first and current DOM coordinates as recovery.");
        Assert(html.Contains("bindPointerInteractions", StringComparison.Ordinal) && html.Contains("cfxgraphdragstart", StringComparison.Ordinal) && html.Contains("cfxgraphviewport", StringComparison.Ordinal), "Graph explorer runtime should expose drag/drop node movement and viewport events.");
        Assert(html.Contains("toggleNeighborhoodFocus", StringComparison.Ordinal) && html.Contains("cfxgraphfocus", StringComparison.Ordinal) && html.Contains("cfxGraphFocusNode", StringComparison.Ordinal), "Graph explorer runtime should expose selected-node neighborhood focus and host events.");
        Assert(html.Contains("exportGraph", StringComparison.Ordinal) && html.Contains("cfxgraphexport", StringComparison.Ordinal) && html.Contains("exportGraphJson", StringComparison.Ordinal), "Graph explorer runtime should expose cancelable SVG, PNG, and JSON export events.");
        Assert(html.Contains("image.naturalWidth > 0", StringComparison.Ordinal) && html.Contains("malformed host-supplied images", StringComparison.Ordinal), "Graph explorer Canvas runtime should tolerate broken image-node URLs without breaking interaction.");
        Assert(html.Contains("data-cfx-graph-action=\"zoom-in\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-action=\"zoom-out\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-action=\"fit\"", StringComparison.Ordinal), "Graph explorer toolbar should expose dependency-free zoom and fit controls.");
        Assert(html.Contains("cfxgraphready", StringComparison.Ordinal) && html.Contains("cfxgraphselect", StringComparison.Ordinal) && html.Contains("cfxgraphselection", StringComparison.Ordinal) && html.Contains("cfxgraphfilter", StringComparison.Ordinal), "Graph explorer runtime should publish reusable host events.");
        Assert(html.Contains("cfxgraphstabilized", StringComparison.Ordinal) && html.Contains("cfxgraphperformance", StringComparison.Ordinal) && html.Contains("cfxgraphlod", StringComparison.Ordinal) && html.Contains("cfxgraphcluster", StringComparison.Ordinal), "Graph explorer runtime should publish physics, performance, LOD, and clustering events.");
        Assert(html.Contains("publishPerformance", StringComparison.Ordinal) && html.Contains("cfxGraphPerformanceSamples", StringComparison.Ordinal) && html.Contains("cfxGraphPerformanceBudgetMisses", StringComparison.Ordinal), "Graph explorer runtime should keep durable performance summary diagnostics on the graph root.");
        Assert(html.Contains("cfxGraphSelectionCount", StringComparison.Ordinal) && html.Contains("cfxGraphSelectionIds", StringComparison.Ordinal) && html.Contains("cfxGraphSelectionPrimary", StringComparison.Ordinal), "Graph explorer runtime should keep durable multi-selection diagnostics on the graph root.");
        Assert(html.Contains("cfx-graph-lod-compact", StringComparison.Ordinal) && html.Contains("cfx-graph-performance-gated", StringComparison.Ordinal) && html.Contains("cfx-graph-neighborhood-active", StringComparison.Ordinal), "Graph explorer runtime and CSS should expose large-graph LOD, performance, and neighborhood focus states.");
        Assert(html.Contains(".cfx-graph-edge.cfx-graph-selected", StringComparison.Ordinal), "Graph explorer CSS should visibly preserve selected edge state.");
        Assert(!html.Contains("<link", StringComparison.OrdinalIgnoreCase), "Graph explorer pages should not reference external stylesheets.");
        Assert(!html.Contains("@import", StringComparison.OrdinalIgnoreCase), "Graph explorer pages should not import external stylesheets.");
        Assert(!html.Contains("http://", StringComparison.OrdinalIgnoreCase) && !html.Contains("https://", StringComparison.OrdinalIgnoreCase), "Graph explorer pages should remain self-contained.");

        var fragment = scene.ToGraphExplorerHtmlFragment(options => options.ScriptNonce = "fragment-nonce");
        Assert(fragment.Contains("data-cfx-graph-assets=\"true\"", StringComparison.Ordinal), "Graph explorer fragments should include embeddable CSS assets.");
        Assert(fragment.Contains("<script nonce=\"fragment-nonce\">", StringComparison.Ordinal), "Graph explorer fragments should support CSP nonces.");
        Assert(HtmlGraphExplorerRenderer.BuildFragmentStyle().Contains(".cfx-graph-explorer", StringComparison.Ordinal), "Host-registered graph explorer CSS should stay scoped to graph explorer surfaces.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("requestAnimationFrame(step)", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("cfxgraphperformance", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("cfxGraphRendererActive", StringComparison.Ordinal), "Host-registered graph explorer runtime should include physics, performance, and backend-selection behavior.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("performance: {", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("maxSampleMs", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("performanceBudget:", StringComparison.Ordinal), "Host-registered graph explorer runtime should export repeatable performance summary evidence.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("acceleration === 'barnes-hut'", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("thread: 'main'", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("thread: 'worker'", StringComparison.Ordinal), "Host-registered graph explorer runtime should publish Barnes-Hut versus pairwise physics telemetry, including main-thread versus worker execution.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("cfxGraphHitTest", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("state.nodes.length >= 160 ? 'grid' : 'linear'", StringComparison.Ordinal), "Host-registered graph explorer runtime should publish the active hit-test strategy.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("return best || domHitNodeAt(root, point)", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("distance <= size + 10", StringComparison.Ordinal), "Host-registered graph explorer runtime should include indexed Canvas hit testing with DOM-backed recovery.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("applyNeighborhoodFocus", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("focus: { active:", StringComparison.Ordinal), "Host-registered graph explorer runtime should focus and export selected-neighborhood state.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("downloadExport", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("cancelable: !!options?.cancelable", StringComparison.Ordinal), "Host-registered graph explorer runtime should support host-interceptable export downloads.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("selection: {", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("selectionCount", StringComparison.Ordinal), "Host-registered graph explorer runtime should export multi-selection state.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("featureGroups", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("Explorer: ['Selection', 'MultiSelection', 'Search', 'Filtering', 'Viewport'", StringComparison.Ordinal), "Graph explorer runtime should expand grouped feature flags so Explorer enables multi-selection, viewport, filtering, and clustering behavior in the browser.");
        Assert(!HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("<script>", StringComparison.Ordinal), "Host-registered graph explorer runtime should return raw JavaScript.");

        var canvasHtml = scene.ToGraphExplorerHtmlFragment(options => options.RenderBackend = HtmlGraphRenderBackend.Canvas);
        Assert(canvasHtml.Contains("data-cfx-graph-renderer=\"canvas\"", StringComparison.Ordinal), "Graph explorer fragments should allow hosts to request Canvas as the initial renderer.");
    }

    private static GraphScene SampleGraphScene() {
        return GraphScene.Create("service-map", "Service map")
            .AddNode("api", "API", node => {
                node.Kind = "service";
                node.GroupId = "platform";
                node.ClusterId = "core";
                node.Status = "healthy";
                node.Size = 11;
                node.Shape = GraphNodeShape.Image;
                node.ImageUrl = "data:image/svg+xml,%3Csvg viewBox='0 0 64 64'%3E%3Crect width='64' height='64' rx='16' fill='%232563eb'/%3E%3C/svg%3E";
                node.ImageAlt = "API service icon";
                node.IconText = "A";
                node.Metadata["owner"] = "identity";
            })
            .AddNode("db", "Database", node => {
                node.Kind = "database";
                node.GroupId = "platform";
                node.ClusterId = "core";
                node.Status = "warning";
                node.Size = 13;
            })
            .AddNode("worker", "Worker", node => {
                node.Kind = "service";
                node.GroupId = "jobs";
                node.Status = "healthy";
                node.Fixed = true;
                node.X = 760;
                node.Y = 340;
            })
            .AddEdge("api-db", "api", "db", "queries", edge => {
                edge.Kind = "dependency";
                edge.Status = "warning";
                edge.Weight = 2;
                edge.Length = 140;
                edge.Directed = true;
                edge.Shape = GraphEdgeShape.Curve;
                edge.Curvature = 32;
                edge.Dashed = true;
            })
            .AddEdge("api-worker", "api", "worker", "enqueues", edge => {
                edge.Kind = "queue";
                edge.Status = "healthy";
            })
            .AddCluster("core", "Core services", new[] { "api", "db" }, cluster => {
                cluster.Kind = "community";
                cluster.Collapsed = true;
            });
    }
}
