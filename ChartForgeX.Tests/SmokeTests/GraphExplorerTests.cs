using System;
using System.Globalization;
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

        var originScene = GraphScene.Create("origin", "Origin")
            .AddNode("origin-node", "Origin node", node => {
                node.X = 0;
                node.Y = 0;
            });
        var originHtml = originScene.ToGraphExplorerHtmlFragment();
        Assert(originScene.Nodes[0].HasExplicitPosition && originHtml.Contains("data-node-x=\"0\"", StringComparison.Ordinal) && originHtml.Contains("data-node-y=\"0\"", StringComparison.Ordinal), "Graph scenes should preserve caller-supplied origin positions instead of treating zero coordinates as unset.");

        var fixedOnlyHtml = GraphScene.Create("fixed-only", "Fixed only")
            .AddNode("fixed", "Fixed", node => node.Fixed = true)
            .ToGraphExplorerHtmlFragment();
        Assert(fixedOnlyHtml.Contains("data-node-fixed=\"true\"", StringComparison.Ordinal) && !fixedOnlyHtml.Contains("data-node-x=\"0\" data-node-y=\"0\"", StringComparison.Ordinal), "Graph explorer prepared layout should not treat Fixed as a caller-supplied origin position.");

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

        var conflictingClusterNode = GraphScene.Create("conflicting-cluster", "Conflicting cluster")
            .AddNode("api", "API", node => node.ClusterId = "right")
            .AddCluster("left", "Left", new[] { "api" });
        AssertThrows<InvalidOperationException>(() => conflictingClusterNode.Validate(), "Graph scenes should reject nodes whose explicit ClusterId conflicts with declared cluster membership.");

        var missingExplicitClusterNode = GraphScene.Create("missing-explicit-cluster", "Missing explicit cluster")
            .AddNode("api", "API", node => node.ClusterId = "missing")
            .AddCluster("core", "Core", Array.Empty<string>());
        AssertThrows<InvalidOperationException>(() => missingExplicitClusterNode.Validate(), "Graph scenes should reject nodes whose explicit ClusterId does not match a declared cluster.");

        var unknownFeatures = GraphScene.Create("unknown-features", "Unknown features").AddNode("node", "Node");
        unknownFeatures.Options.Features |= (GraphSceneFeatures)(1 << 20);
        AssertThrows<InvalidOperationException>(() => unknownFeatures.Validate(), "Graph scenes should reject unknown feature bits before adapters serialize browser feature flags.");

        var unknownPhysicsSolver = GraphScene.Create("unknown-physics", "Unknown physics").AddNode("node", "Node");
        unknownPhysicsSolver.Options.Physics.Solver = (GraphPhysicsSolver)999;
        AssertThrows<InvalidOperationException>(() => unknownPhysicsSolver.Validate(), "Graph scenes should reject unknown physics solver values before adapters serialize runtime physics settings.");

        var nonPositiveSize = GraphScene.Create("bad-size-range", "Bad size range")
            .AddNode("node", "Node", node => node.Size = 0);
        AssertThrows<InvalidOperationException>(() => nonPositiveSize.Validate(), "Graph scenes should reject non-positive node sizes instead of exporting clamped marks.");

        var unknownNodeShape = GraphScene.Create("bad-node-shape", "Bad node shape").AddNode("node", "Node", node => node.Shape = (GraphNodeShape)999);
        AssertThrows<InvalidOperationException>(() => unknownNodeShape.Validate(), "Graph scenes should reject unknown node shape values before adapters silently fall back to circles.");

        var unknownEdgeShape = GraphScene.Create("bad-edge-shape", "Bad edge shape").AddNode("a", "A").AddNode("b", "B").AddEdge("edge", "a", "b", configure: edge => edge.Shape = (GraphEdgeShape)999);
        AssertThrows<InvalidOperationException>(() => unknownEdgeShape.Validate(), "Graph scenes should reject unknown edge shape values before adapters silently fall back to lines.");

        var blankPublicNode = GraphScene.Create("blank-node", "Blank node");
        blankPublicNode.Nodes.Add(new GraphSceneNode());
        AssertThrows<InvalidOperationException>(() => blankPublicNode.Validate(), "Graph scenes should reject blank public node ids before adapters render empty keys.");

        var blankPublicNodeLabel = GraphScene.Create("blank-node-label", "Blank node label");
        blankPublicNodeLabel.Nodes.Add(new GraphSceneNode { Id = "node" });
        AssertThrows<InvalidOperationException>(() => blankPublicNodeLabel.Validate(), "Graph scenes should reject blank public node labels before adapters render invisible nodes.");

        var blankPublicClusterLabel = GraphScene.Create("blank-cluster-label", "Blank cluster label")
            .AddNode("node", "Node");
        blankPublicClusterLabel.Clusters.Add(new GraphSceneCluster { Id = "cluster" });
        blankPublicClusterLabel.Clusters[0].NodeIds.Add("node");
        AssertThrows<InvalidOperationException>(() => blankPublicClusterLabel.Validate(), "Graph scenes should reject blank public cluster labels before adapters render invisible cluster summaries.");

        var nonFinitePosition = GraphScene.Create("bad-position", "Bad position")
            .AddNode("bad", "Bad", node => node.X = double.NaN);
        AssertThrows<InvalidOperationException>(() => nonFinitePosition.Validate(), "Graph scenes should reject non-finite explicit node coordinates before adapters serialize invalid geometry.");

        var nonFiniteSize = GraphScene.Create("bad-size", "Bad size")
            .AddNode("bad", "Bad", node => node.Size = double.PositiveInfinity);
        AssertThrows<InvalidOperationException>(() => nonFiniteSize.Validate(), "Graph scenes should reject non-finite node sizes before adapters serialize invalid marks.");

        var invalidPhysics = GraphScene.Create("bad-physics", "Bad physics").AddNode("node", "Node");
        invalidPhysics.Options.Physics.Damping = double.NaN;
        AssertThrows<InvalidOperationException>(() => invalidPhysics.Validate(), "Graph scenes should reject non-finite or out-of-range physics options before adapters serialize invalid runtime settings.");
        var zeroRepulsion = GraphScene.Create("zero-repulsion", "Zero repulsion").AddNode("node", "Node"); zeroRepulsion.Options.Physics.Repulsion = 0;
        AssertThrows<InvalidOperationException>(() => zeroRepulsion.Validate(), "Graph scenes should reject zero physics repulsion before the runtime silently clamps it to a different force.");

        var invalidPerformance = GraphScene.Create("bad-performance", "Bad performance").AddNode("node", "Node");
        invalidPerformance.Options.Performance.FrameBudgetMilliseconds = 0;
        AssertThrows<InvalidOperationException>(() => invalidPerformance.Validate(), "Graph scenes should reject non-positive frame budgets before adapters report misleading performance telemetry.");

        var negativePerformanceLimit = GraphScene.Create("bad-performance-limit", "Bad performance limit").AddNode("node", "Node");
        negativePerformanceLimit.Options.Performance.MaxInteractiveCanvasEdges = -1;
        AssertThrows<InvalidOperationException>(() => negativePerformanceLimit.Validate(), "Graph scenes should reject negative SVG or Canvas performance limits before runtime gating.");

        var negativeLodThreshold = GraphScene.Create("bad-lod", "Bad LOD").AddNode("node", "Node");
        negativeLodThreshold.Options.LevelOfDetail.HideEdgeLabelsThreshold = -1;
        AssertThrows<InvalidOperationException>(() => negativeLodThreshold.Validate(), "Graph scenes should reject negative LOD thresholds before every graph falls into compact or Canvas modes.");

        var nonFiniteCurvature = GraphScene.Create("bad-curvature", "Bad curvature")
            .AddNode("source", "Source")
            .AddNode("target", "Target")
            .AddEdge("source-target", "source", "target", configure: edge => edge.Curvature = double.NaN);
        AssertThrows<InvalidOperationException>(() => nonFiniteCurvature.Validate(), "Graph scenes should reject non-finite edge curvature before adapters serialize invalid paths.");

        var nonFiniteWeight = GraphScene.Create("bad-weight", "Bad weight")
            .AddNode("source", "Source")
            .AddNode("target", "Target")
            .AddEdge("source-target", "source", "target", configure: edge => edge.Weight = double.PositiveInfinity);
        AssertThrows<InvalidOperationException>(() => nonFiniteWeight.Validate(), "Graph scenes should reject non-finite edge weights before adapters serialize inconsistent physics hints.");

        var nonFiniteLength = GraphScene.Create("bad-length", "Bad length")
            .AddNode("source", "Source")
            .AddNode("target", "Target")
            .AddEdge("source-target", "source", "target", configure: edge => edge.Length = double.NaN);
        AssertThrows<InvalidOperationException>(() => nonFiniteLength.Validate(), "Graph scenes should reject non-finite edge lengths before adapters serialize inconsistent physics hints.");

        var negativeWeight = GraphScene.Create("bad-weight-range", "Bad weight range")
            .AddNode("source", "Source")
            .AddNode("target", "Target")
            .AddEdge("source-target", "source", "target", configure: edge => edge.Weight = 0);
        AssertThrows<InvalidOperationException>(() => negativeWeight.Validate(), "Graph scenes should reject non-positive edge weights instead of exporting physics hints that the runtime clamps differently.");

        var negativeLength = GraphScene.Create("bad-length-range", "Bad length range")
            .AddNode("source", "Source")
            .AddNode("target", "Target")
            .AddEdge("source-target", "source", "target", configure: edge => edge.Length = -1);
        AssertThrows<InvalidOperationException>(() => negativeLength.Validate(), "Graph scenes should reject negative edge lengths instead of exporting physics hints that the runtime ignores.");

    }

    private static void GraphExplorerHtmlAdapterRendersSelfContainedScene() {
        var scene = SampleGraphScene();
        scene.Metadata["source"] = "smoke-suite";
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
        Assert(html.Contains("data-cfx-graph-layout=\"structured-prepared\"", StringComparison.Ordinal), "Graph explorer pages should expose the graph-aware prepared layout path used before runtime physics starts.");
        Assert(html.Contains("data-cfx-graph-canvas-fallback=\"true\"", StringComparison.Ordinal), "Graph explorer pages should allow threshold-based Canvas fallback by default.");
        Assert(html.Contains("data-cfx-graph-physics=\"ForceAtlas2\"", StringComparison.Ordinal), "Graph explorer pages should expose the requested physics profile.");
        Assert(html.Contains("data-cfx-graph-stabilization-iterations=\"900\"", StringComparison.Ordinal), "Graph explorer pages should expose stabilization budgets.");
        Assert(html.Contains("data-cfx-graph-repulsion=\"4500\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-damping=\"0.09\"", StringComparison.Ordinal), "Graph explorer pages should expose force simulation constants.");
        Assert(html.Contains("data-cfx-lod-cluster-threshold=\"2\"", StringComparison.Ordinal) && html.Contains("data-cfx-lod-compact-node-threshold=\"3\"", StringComparison.Ordinal) && html.Contains("data-cfx-lod-canvas-threshold=\"5\"", StringComparison.Ordinal), "Graph explorer pages should expose LOD thresholds.");
        Assert(html.Contains("data-cfx-lod-collapse-clusters=\"true\"", StringComparison.Ordinal), "Graph explorer pages should expose default cluster collapse state.");
        Assert(html.Contains("data-cfx-performance-frame-budget=\"10\"", StringComparison.Ordinal) && html.Contains("data-cfx-performance-max-svg-nodes=\"1000\"", StringComparison.Ordinal) && html.Contains("data-cfx-performance-max-canvas-nodes=\"6000\"", StringComparison.Ordinal) && html.Contains("data-cfx-performance-telemetry-interval=\"7\"", StringComparison.Ordinal), "Graph explorer pages should expose SVG and Canvas performance budgets.");
        Assert(html.Contains("data-cfx-role=\"graph-canvas\"", StringComparison.Ordinal) && html.Contains("class=\"cfx-graph-canvas\" data-cfx-role=\"graph-canvas\" width=\"960\" height=\"560\" role=\"img\" aria-label=\"Service map\"", StringComparison.Ordinal), "Graph explorer pages should include an accessible Canvas rendering target for large-object fallback.");
        Assert(html.Contains("data-cfx-role=\"graph-overview\"", StringComparison.Ordinal) && html.Contains("class=\"cfx-graph-overview\"", StringComparison.Ordinal), "Graph explorer pages should include a dependency-free overview canvas for dense graph navigation.");
        Assert(html.Contains("class=\"cfx-graph-svg\" data-cfx-role=\"graph-scene\" width=\"960\" height=\"560\" viewBox=\"0 0 960 560\"", StringComparison.Ordinal), "Graph explorer SVG output should preserve intrinsic dimensions for standalone exports and host-registered CSS.");
        Assert(html.Contains("<rect class=\"cfx-graph-bg\" width=\"960\" height=\"560\"></rect>", StringComparison.Ordinal), "Graph explorer SVG output should include an explicit background rectangle so inline and exported SVG match the Canvas page background.");
        Assert(html.Contains("data-cfx-graph-search=\"true\"", StringComparison.Ordinal), "Graph explorer pages should include search controls.");
        Assert(html.Contains("data-cfx-graph-filter=\"status\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-filter=\"kind\"", StringComparison.Ordinal), "Graph explorer pages should include reusable status and kind filters.");
        Assert(html.Contains("data-cfx-graph-action=\"clusters\"", StringComparison.Ordinal), "Graph explorer pages should include cluster controls when clusters are present.");
        Assert(html.Contains("data-cfx-graph-action=\"focus\"", StringComparison.Ordinal), "Graph explorer pages should include selected-neighborhood focus controls when neighborhood exploration is enabled.");
        Assert(html.Contains("data-cfx-graph-action=\"clear-selection\"", StringComparison.Ordinal), "Graph explorer pages should include an explicit clear-selection control when multi-selection is enabled.");
        var singleSelectionScene = SampleGraphScene();
        singleSelectionScene.Options.Disable(GraphSceneFeatures.MultiSelection);
        var singleSelectionHtml = singleSelectionScene.ToGraphExplorerHtmlFragment();
        Assert(singleSelectionHtml.Contains("data-cfx-graph-action=\"clear-selection\"", StringComparison.Ordinal), "Graph explorer pages should include an explicit clear-selection control when single selection is enabled.");
        Assert(html.Contains("data-cfx-graph-action=\"physics\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-action=\"stabilize\"", StringComparison.Ordinal), "Graph explorer pages should include runtime physics controls when enabled.");
        Assert(html.Contains("data-cfx-graph-action=\"export-svg\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-action=\"export-png\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-action=\"export-json\"", StringComparison.Ordinal), "Graph explorer pages should include SVG, PNG, and JSON export controls when export is enabled.");
        Assert(html.Contains("data-cfx-role=\"graph-node\"", StringComparison.Ordinal) && html.Contains("data-node-id=\"api\"", StringComparison.Ordinal), "Graph explorer SVG should expose graph node metadata.");
        Assert(html.Contains("data-node-size=\"11\"", StringComparison.Ordinal) && html.Contains("data-node-fixed=\"true\"", StringComparison.Ordinal), "Graph explorer SVG should expose node size and fixed-position hints for physics adapters.");
        Assert(html.Contains("data-node-shape=\"image\"", StringComparison.Ordinal) && html.Contains("data-node-image-url=\"data:image/svg+xml", StringComparison.Ordinal) && html.Contains("data-node-icon=\"A\"", StringComparison.Ordinal), "Graph explorer SVG should expose image and icon node metadata.");
        Assert(html.Contains("data-cfx-search=\"owner identity\"", StringComparison.Ordinal) && html.Contains("evidence privileged-path", StringComparison.Ordinal) && html.Contains("tier core\"", StringComparison.Ordinal), "Graph explorer SVG should serialize node, edge, and cluster metadata into searchable attributes.");
        Assert(html.Contains("data-cfx-metadata=\"{&quot;source&quot;:&quot;smoke-suite&quot;}\"", StringComparison.Ordinal) && html.Contains("data-cfx-metadata=\"{&quot;owner&quot;:&quot;identity&quot;}\"", StringComparison.Ordinal) && html.Contains("data-cfx-metadata=\"{&quot;evidence&quot;:&quot;privileged-path&quot;}\"", StringComparison.Ordinal) && html.Contains("data-cfx-metadata=\"{&quot;tier&quot;:&quot;core&quot;}\"", StringComparison.Ordinal), "Graph explorer SVG should serialize scene, node, edge, and cluster metadata for host inspectors and selection events.");
        Assert(html.Contains("<image href=\"data:image/svg+xml", StringComparison.Ordinal), "Graph explorer SVG should render image-backed nodes without external dependencies.");
        Assert(html.Contains("data-cfx-role=\"graph-edge\"", StringComparison.Ordinal) && html.Contains("data-source-node-id=\"api\"", StringComparison.Ordinal) && html.Contains("data-target-node-id=\"db\"", StringComparison.Ordinal), "Graph explorer SVG should expose graph edge metadata.");
        Assert(html.Contains("data-edge-weight=\"2\"", StringComparison.Ordinal) && html.Contains("data-edge-length=\"140\"", StringComparison.Ordinal), "Graph explorer SVG should expose edge physics hints.");
        Assert(html.Contains("data-edge-directed=\"true\"", StringComparison.Ordinal) && html.Contains("data-edge-shape=\"curve\"", StringComparison.Ordinal) && html.Contains("marker-end=\"url(#service-map-", StringComparison.Ordinal), "Graph explorer SVG should render directional curved edge contracts with scoped markers.");
        Assert(html.Contains("aria-label=\"queries\"", StringComparison.Ordinal) && html.Contains("data-source-cluster-id=\"core\"", StringComparison.Ordinal) && html.Contains("data-target-cluster-id=\"core\"", StringComparison.Ordinal), "Graph explorer edge paths should be accessible and carry endpoint cluster ids for aggregate collapsed-edge rendering.");
        Assert(html.Contains("aria-label=\"API\"", StringComparison.Ordinal) && html.Contains("aria-label=\"Core services\"", StringComparison.Ordinal), "Graph explorer focusable SVG nodes and collapsed cluster summaries should expose accessible names.");
        var rendererSource = System.IO.File.ReadAllText(System.IO.Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity.Html", "HtmlGraphExplorerRenderer.cs"));
        var layoutSource = System.IO.File.ReadAllText(System.IO.Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity.Html", "HtmlGraphExplorerRenderer.Layout.cs"));
        Assert(rendererSource.Contains("DirectedTargetPoint", StringComparison.Ordinal) && rendererSource.Contains("TargetBoundaryInset", StringComparison.Ordinal) && rendererSource.Contains("GraphNodeShape.Box", StringComparison.Ordinal), "Graph explorer SVG should trim directed edges before marker placement with shape-aware boundaries so arrowheads remain visible.");
        Assert(rendererSource.Contains("BuildCollapsedNodeRenderRadii", StringComparison.Ordinal) && rendererSource.Contains("collapsedNodeRadii.TryGetValue(edge.TargetNodeId", StringComparison.Ordinal) && rendererSource.Contains("collapsedNodeRadii.TryGetValue(edge.SourceNodeId", StringComparison.Ordinal), "Graph explorer static collapsed-cluster arrows should trim against each cluster summary radius instead of a fixed inset.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("const hasLayoutBox = rect.width > 0 && rect.height > 0", StringComparison.Ordinal), "Hidden SVG-mode canvases should size PNG exports from the scene instead of compounding reflected high-DPI canvas attributes.");
        Assert(html.Contains("data-cfx-role=\"graph-edge-label\"", StringComparison.Ordinal), "Graph explorer SVG should render relationship labels as addressable graph output.");
        Assert(html.Contains("data-cfx-role=\"graph-cluster\"", StringComparison.Ordinal) && html.Contains("data-cluster-node-ids=\"api,db\"", StringComparison.Ordinal), "Graph explorer SVG should expose reusable cluster summaries.");
        Assert(html.Contains("data-cfx-role=\"graph-cluster\" tabindex=\"0\" aria-hidden=\"false\"", StringComparison.Ordinal), "Graph explorer SVG should keep collapsed cluster summaries keyboard reachable.");
        Assert(html.Contains("class=\"cfx-graph-node cfx-graph-cluster-collapsed-member\" tabindex=\"0\" data-cfx-role=\"graph-node\" data-node-id=\"api\"", StringComparison.Ordinal) && html.Contains("class=\"cfx-graph-edge cfx-graph-cluster-collapsed-member\" tabindex=\"0\" data-cfx-role=\"graph-edge\" data-edge-id=\"api-db\"", StringComparison.Ordinal) && html.Contains("class=\"cfx-graph-edge-label cfx-graph-cluster-collapsed-member\" data-cfx-role=\"graph-edge-label\" data-edge-label-for=\"api-db\"", StringComparison.Ordinal), "Graph explorer SVG should render collapsed cluster members hidden before browser bindings run.");
        Assert(html.Contains("requestAnimationFrame(step)", StringComparison.Ordinal) && html.Contains("physicsTick", StringComparison.Ordinal), "Graph explorer runtime should run dependency-free browser physics instead of only exposing buttons.");
        Assert(html.Contains("barnesHutTree", StringComparison.Ordinal) && html.Contains("data-cfx-graph-physics=\"ForceAtlas2\"", StringComparison.Ordinal) && html.Contains("cfxGraphPhysicsAcceleration", StringComparison.Ordinal), "Graph explorer runtime should include scalable many-body acceleration and expose the active physics path for large graph diagnostics.");
        Assert(html.Contains("workerPhysicsSource", StringComparison.Ordinal) && html.Contains("cfxGraphPhysicsThread", StringComparison.Ordinal) && html.Contains("thread: 'worker'", StringComparison.Ordinal), "Graph explorer runtime should move large Barnes-Hut stabilization to a dependency-free Web Worker when the browser allows it.");
        Assert(html.Contains("stopMainPhysics", StringComparison.Ordinal) && html.Contains("__cfxGraphMainPhysics", StringComparison.Ordinal) && html.Contains("cancelAnimationFrame", StringComparison.Ordinal), "Graph explorer runtime should cancel existing main-thread physics loops before starting another stabilization pass.");
        Assert(html.Contains("return false", StringComparison.Ordinal) && html.Contains("if (!running) startPhysics(root)", StringComparison.Ordinal), "Graph explorer runtime should not mark gated physics as running when no simulation starts.");
        Assert(html.Contains("if (action === 'stabilize' && hasFeature(root, 'Stabilization')", StringComparison.Ordinal), "Graph explorer runtime should honor the Stabilization feature flag for manual stabilization.");
        Assert(html.Contains("if (!hasFeature(root, 'PerformanceTelemetry')) return", StringComparison.Ordinal), "Graph explorer runtime should not publish performance telemetry when the feature is disabled.");
        Assert(html.Contains("drawCanvas", StringComparison.Ordinal) && html.Contains("bindCanvasHitTesting", StringComparison.Ordinal), "Graph explorer runtime should draw and hit-test the dependency-free Canvas backend.");
        Assert(html.Contains("renderer: root.dataset.cfxGraphRendererActive", StringComparison.Ordinal), "Graph explorer ready events should report the active renderer after LOD fallback.");
        Assert(html.Contains("context.clip()", StringComparison.Ordinal) && html.Contains("drawImage(image", StringComparison.Ordinal), "Graph explorer Canvas image nodes should be clipped like SVG image nodes.");
        Assert(html.Contains("preloadCanvasImages", StringComparison.Ordinal) && html.Contains("await preloadCanvasImages(root, state)", StringComparison.Ordinal), "Graph explorer PNG export should wait for image-backed Canvas nodes before snapshotting.");
        Assert(html.Contains("if (raw === '') return fallback", StringComparison.Ordinal), "Graph explorer runtime should preserve numeric defaults for absent viewport and performance attributes.");
        Assert(html.Contains("if (action === 'stabilize' && hasFeature(root, 'Stabilization') && startPhysics(root)) syncPhysicsControls(root);", StringComparison.Ordinal), "Graph explorer Stabilize should sync the maintained Physics toggle after starting a physics run.");
        Assert(html.Contains("body class=\"cfx-graph-shell\"", StringComparison.Ordinal) && html.Contains("class=\"cfx-graph-search\"", StringComparison.Ordinal) && html.Contains("class=\"cfx-graph-tool\"", StringComparison.Ordinal), "Graph explorer pages should emit the styled page shell and toolbar classes.");
        Assert(html.Contains("contentBounds", StringComparison.Ordinal) && html.Contains("fitViewport(root)", StringComparison.Ordinal), "Graph explorer runtime should fit visible content into the opening viewport.");
        Assert(html.Contains("nodeHalfWidth(node)", StringComparison.Ordinal) && html.Contains("edgeLabelPoint(rendered, control)", StringComparison.Ordinal) && html.Contains("routeRenderPoints(rendered).forEach(point => pushPoint(point, pad))", StringComparison.Ordinal) && html.Contains("selfLoopGeometry(rendered.source)", StringComparison.Ordinal), "Graph explorer fit bounds should include shape-aware node extents plus route vertices, curved edge, self-loop, and aggregate cluster-edge geometry.");
        Assert(html.Contains("sceneSize(root)", StringComparison.Ordinal) && html.Contains("cfxGraphFitScale", StringComparison.Ordinal), "Graph explorer fit should use the live graph viewport and expose repeatable fit diagnostics instead of relying on fixed pixel guesses.");
        Assert(html.Contains("clusterMetrics", StringComparison.Ordinal) && html.Contains("Math.min(54", StringComparison.Ordinal) && html.Contains("const members = expanded ? allMembers.filter(node => visible(node.el)) : allMembers", StringComparison.Ordinal), "Graph explorer Canvas clusters should use bounded readable hulls and visible-member metrics when expanded clusters are filtered.");
        Assert(html.Contains("sampleTicks", StringComparison.Ordinal) && html.Contains("cfxGraphPerformanceSampleBudgetMs", StringComparison.Ordinal), "Graph explorer runtime should compare performance samples against the effective sampled-tick budget.");
        Assert(html.Contains("communityPackingEvents", StringComparison.Ordinal) && html.Contains("cfxGraphPerformanceCommunityPackingEvents", StringComparison.Ordinal), "Graph explorer runtime should export community-packing telemetry for dense layout QA.");
        Assert(html.Contains("indexHitTesting", StringComparison.Ordinal) && html.Contains("cfxGraphHitTest", StringComparison.Ordinal) && html.Contains("hitTestNodes", StringComparison.Ordinal), "Graph explorer runtime should expose spatial hit testing for large-scene Canvas selection and dragging.");
        Assert(html.Contains("return { nodes, edges, clusters, byId, clusterById }", StringComparison.Ordinal) && html.Contains("state.byId || new Map", StringComparison.Ordinal), "Graph explorer runtime should reuse node and cluster indexes during layout and Canvas drawing.");
        Assert(html.Contains("const labels = new Map(items(root, '[data-cfx-role=\"graph-edge-label\"]')", StringComparison.Ordinal), "Graph explorer runtime should update edge labels from a per-layout label index.");
        Assert(html.Contains("return best || domHitNodeAt(root, point)", StringComparison.Ordinal) && html.Contains("items(root, '[data-cfx-role=\"graph-node\"]')", StringComparison.Ordinal) && html.Contains("data-node-x", StringComparison.Ordinal) && html.Contains("data-node-y", StringComparison.Ordinal), "Graph explorer Canvas hit testing should use indexed state first and current DOM coordinates as recovery.");
        Assert(html.Contains("hitGraphItemAt", StringComparison.Ordinal) && html.Contains("hitClusterAt", StringComparison.Ordinal) && html.Contains("hitEdgeAt", StringComparison.Ordinal), "Graph explorer Canvas hit testing should preserve node, edge, and cluster selection.");
        Assert(html.Contains("const metrics = clusterMetrics(cluster, state.byId)", StringComparison.Ordinal), "Graph explorer Canvas cluster hit testing should reuse drawn cluster bounds.");
        Assert(html.Contains("!visible(cluster.el) || cluster.el.classList.contains('cfx-graph-cluster-expanded')", StringComparison.Ordinal), "Graph explorer Canvas cluster hit testing should skip transparent expanded cluster hulls.");
        Assert(html.Contains("bindPointerInteractions", StringComparison.Ordinal) && html.Contains("cfxgraphdragstart", StringComparison.Ordinal) && html.Contains("cfxgraphviewport", StringComparison.Ordinal), "Graph explorer runtime should expose drag/drop node movement and viewport events.");
        Assert(html.Contains("toggleNeighborhoodFocus", StringComparison.Ordinal) && html.Contains("cfxgraphfocus", StringComparison.Ordinal) && html.Contains("cfxGraphFocusNode", StringComparison.Ordinal), "Graph explorer runtime should expose selected-node neighborhood focus and host events.");
        Assert(html.Contains("if (focusNode && items(root, '[data-cfx-role=\"graph-node\"]').some(node => attr(node, 'data-node-id') === focusNode && visible(node))) applyNeighborhoodFocus(root, focusNode);", StringComparison.Ordinal), "Graph explorer filters should recompute active neighborhood focus after visible edges change.");
        Assert(html.Contains("exportGraph", StringComparison.Ordinal) && html.Contains("cfxgraphexport", StringComparison.Ordinal) && html.Contains("exportGraphJson", StringComparison.Ordinal), "Graph explorer runtime should expose cancelable SVG, PNG, and JSON export events.");
        Assert(html.Contains("exportSvgContent", StringComparison.Ordinal) && html.Contains("data-cfx-export-style", StringComparison.Ordinal), "Graph explorer SVG export should inline graph styles into the standalone SVG payload.");
        Assert(html.Contains("clone.classList.add(name)", StringComparison.Ordinal) && html.Contains("cfx-graph-neighborhood-active", StringComparison.Ordinal), "Graph explorer SVG export should preserve root state classes needed by LOD and focus selectors.");
        Assert(html.Contains("find(style => (style.textContent || '').includes('.cfx-graph-explorer'))", StringComparison.Ordinal), "Graph explorer SVG export should find graph CSS even when host-registered styles do not carry the generated data marker.");
        Assert(html.Contains("parent.appendChild(link)", StringComparison.Ordinal) && html.Contains("link.remove()", StringComparison.Ordinal), "Graph explorer export downloads should click an attached anchor for browsers that ignore detached download links.");
        Assert(html.Contains("drawCanvas(root, state, { force: true })", StringComparison.Ordinal), "Graph explorer PNG export should paint the Canvas surface even when the active renderer is SVG.");
        Assert(html.Contains("cfxgraphexporterror", StringComparison.Ordinal) && html.Contains("cfxGraphLastExportError", StringComparison.Ordinal), "Graph explorer PNG export should report tainted-canvas failures instead of throwing.");
        Assert(html.Contains("edgeRenderEndpoints", StringComparison.Ordinal) && html.Contains("const endpoints = edgeRenderEndpoints(edge, control)", StringComparison.Ordinal) && html.Contains("edge.sourceCollapsed", StringComparison.Ordinal) && html.Contains("nodeBoundaryInset(edge.target", StringComparison.Ordinal), "Graph explorer runtime layout should keep directed and collapsed-proxy SVG edge endpoints and labels trimmed after physics, dragging, or cluster toggles.");
        Assert(html.Contains("edgeStatusOk", StringComparison.Ordinal) && html.Contains("edgeKindOk", StringComparison.Ordinal) && html.Contains("visibleNodes.add(attr(edge, 'data-source-node-id'))", StringComparison.Ordinal), "Graph explorer filters should evaluate edge facets and keep matching edge endpoints visible.");
        Assert(html.Contains("data-cluster-node-ids", StringComparison.Ordinal) && html.Contains("memberVisible", StringComparison.Ordinal), "Graph explorer filters should evaluate cluster facets and member visibility.");
        Assert(html.Contains("hiddenMemberHits", StringComparison.Ordinal) && html.Contains("edgeMatchesFacet", StringComparison.Ordinal), "Graph explorer filters should keep collapsed clusters visible when hidden member nodes or edges match search and facet filters.");
        Assert(html.Contains("expanded && queryOk && statusOk && kindOk", StringComparison.Ordinal) && html.Contains("idList(attr(cluster, 'data-cluster-node-ids')).forEach(id => visibleNodes.add(id))", StringComparison.Ordinal), "Graph explorer filters should surface expanded cluster matches through their member nodes.");
        Assert(html.Contains("data-edge-label-for", StringComparison.Ordinal) && html.Contains("cfx-graph-cluster-collapsed-member", StringComparison.Ordinal) && html.Contains("const sameCollapsedCluster = sourceHidden && targetHidden && edge.sourceCluster && edge.sourceCluster === edge.targetCluster", StringComparison.Ordinal), "Graph explorer collapsed clusters should hide internal edge labels while preserving aggregate external relationships.");
        Assert(html.Contains("applyFilters(root); updateEdges(root, state.edges);", StringComparison.Ordinal), "Graph explorer SVG edges should recompute rendered aggregate paths after cluster collapse or expansion toggles.");
        Assert(html.Contains("__cfxGraphPointerSelectionId", StringComparison.Ordinal) && html.Contains("__cfxGraphPointerSelectionTick", StringComparison.Ordinal), "Graph explorer SVG selection should suppress the follow-up click after pointerdown selection to preserve additive multi-selection.");
        Assert(html.Contains("__cfxGraphSuppressClickId", StringComparison.Ordinal) && html.Contains("root.__cfxGraphSuppressClickId === bestId", StringComparison.Ordinal), "Graph explorer selection should suppress the post-drag click once even when the drag lasts longer than the pointerdown selection window.");
        Assert(!html.Contains("canvas.addEventListener('mousedown'", StringComparison.Ordinal), "Graph explorer Canvas selection should not double-toggle through both pointerdown and mousedown.");
        Assert(html.Contains("if (!hasFeature(root, 'Selection')) return", StringComparison.Ordinal), "Graph explorer selection handlers should honor scenes with selection disabled.");
        Assert(html.Contains("hasFeature(root, 'LevelOfDetail')", StringComparison.Ordinal) && html.Contains("hasFeature(root, 'Clustering')", StringComparison.Ordinal), "Graph explorer binding should honor disabled LOD and clustering feature flags.");
        Assert(html.Contains("canvas.setAttribute('tabindex'", StringComparison.Ordinal) && html.Contains("canvas.addEventListener('keydown'", StringComparison.Ordinal), "Graph explorer Canvas mode should keep keyboard selection available when SVG graph item tab stops are disabled.");
        Assert(html.Contains("cfx-graph-edge-label.cfx-graph-label-selected", StringComparison.Ordinal) && html.Contains("cfx-graph-edge-label.cfx-graph-neighborhood-related{display:block}", StringComparison.Ordinal), "Graph explorer LOD should reveal selected and focused edge labels even when dense graphs hide ordinary labels.");
        Assert(html.Contains("edge.label && edge.showLabel && (!root.classList.contains('cfx-graph-lod-hide-edge-labels') || selected || related)", StringComparison.Ordinal), "Graph explorer Canvas LOD should draw selected and focused edge labels even when ordinary dense edge labels are hidden.");
        Assert(html.Contains("syncSelectedEdgeLabels", StringComparison.Ordinal) && html.Contains("state.clusters.find(cluster => visible(cluster.el))", StringComparison.Ordinal), "Graph explorer Canvas keyboard selection should include visible collapsed clusters, not only nodes and edges.");
        Assert(html.Contains("!visible(edge.el) || !edgeHasVisibleEndpoints(edge, state.byId)", StringComparison.Ordinal), "Graph explorer neighborhood focus should ignore hidden edges and hidden aggregate endpoints.");
        Assert(html.Contains("label: attr(node.el, 'data-node-label')", StringComparison.Ordinal) && html.Contains("groupId: attr(node.el, 'data-node-group')", StringComparison.Ordinal) && html.Contains("imageUrl: attr(node.el, 'data-node-image-url')", StringComparison.Ordinal), "Graph explorer JSON export should include node labels, grouping, and image/icon details needed to reconstruct reviewed graphs.");
        Assert(html.Contains("metadata: metadataDetail(root)", StringComparison.Ordinal), "Graph explorer JSON export should include scene-level metadata from the graph root.");
        Assert(html.Contains("kind: attr(edge, 'data-edge-kind')", StringComparison.Ordinal) && html.Contains("showLabel: attr(edge, 'data-edge-show-label') !== 'false'", StringComparison.Ordinal) && html.Contains("hidden: edge.classList.contains('cfx-graph-hidden')", StringComparison.Ordinal), "Graph explorer JSON export should include edge facets and current visibility needed to reconstruct reviewed graphs.");
        Assert(html.Contains("clusters: items(root, '[data-cfx-role=\"graph-cluster\"]')", StringComparison.Ordinal), "Graph explorer JSON export should include cluster membership and collapsed state.");
        Assert(html.Contains("metadata: metadataDetail(node.el)", StringComparison.Ordinal) && html.Contains("metadata: metadataDetail(edge)", StringComparison.Ordinal) && html.Contains("metadata: metadataDetail(cluster)", StringComparison.Ordinal), "Graph explorer JSON export should include structured metadata for nodes, edges, and clusters.");
        Assert(html.Contains("cfxGraphClusterLod", StringComparison.Ordinal) && html.Contains("data-cfx-lod-collapse-clusters", StringComparison.Ordinal), "Graph explorer binding should report cluster LOD while honoring the explicit collapse-on-load option.");
        Assert(html.Contains("data-cfx-graph-cluster-collapse-on-load", StringComparison.Ordinal) && html.Contains("attr(root, 'data-cfx-graph-cluster-collapse-on-load') === 'true' || (hasFeature(root, 'LevelOfDetail')", StringComparison.Ordinal), "Graph explorer binding and JSON export should preserve explicit cluster collapse defaults independently from LOD collapse.");
        Assert(html.Contains("routePoints: routePoints(attr(el, 'data-edge-route-points'))", StringComparison.Ordinal) && html.Contains("routeRenderPoints(edge).map", StringComparison.Ordinal) && html.Contains("distanceToRoute(point, routeRenderPoints(rendered))", StringComparison.Ordinal), "Graph explorer SVG, Canvas/PNG, export, and hit-testing paths should consume prepared edge route points while re-anchoring endpoints to live node positions.");
        Assert(html.Contains("dragThreshold", StringComparison.Ordinal) && html.Contains("if (!active.moved) {", StringComparison.Ordinal) && html.Contains("data-node-fixed', active.fixed", StringComparison.Ordinal), "Graph explorer node selection should only pin nodes and pause physics after an actual drag.");
        Assert(html.Contains("const hitItem = node || graphItem || hitGraphItemAt(root, point)", StringComparison.Ordinal) && html.Contains("hasFeature(root, 'Viewport') && !hitBlocksPan", StringComparison.Ordinal), "Graph explorer viewport panning should start from graph background or inert graph items, not from selectable nodes, clusters, or edges.");
        Assert(html.Contains("if (hasFeature(root, 'Viewport')) {", StringComparison.Ordinal) && html.Contains("fitViewport(root);", StringComparison.Ordinal), "Graph explorer binding should not fit or emit viewport changes when hosts disable viewport behavior.");
        Assert(html.Contains("adaptivePhysicsLayout", StringComparison.Ordinal) && html.Contains("balanceLayoutAspect", StringComparison.Ordinal) && html.Contains("root.__cfxGraphAutoFitOnStabilize", StringComparison.Ordinal), "Graph explorer runtime physics should settle in an adaptive layout space, balance dense graph aspect ratio, and refit after stabilization when the user has not manually moved the viewport.");
        Assert(html.Contains("Math.min(0.98, base.damping * 1.15)", StringComparison.Ordinal), "Graph explorer Barnes-Hut profile should clamp scaled damping to the supported damping range.");
        Assert(html.Contains("homeX", StringComparison.Ordinal) && html.Contains("homeY", StringComparison.Ordinal), "Graph explorer runtime should preserve prepared node homes for post-physics layout quality passes.");
        Assert(html.Contains("applyStructuralForces", StringComparison.Ordinal) && html.Contains("homeGravity", StringComparison.Ordinal) && html.Contains("communityGravity", StringComparison.Ordinal), "Graph explorer runtime physics should continuously pull nodes toward prepared homes and communities instead of letting center gravity flatten the graph.");
        Assert(html.Contains("applyCommunityPacking", StringComparison.Ordinal) && html.Contains("communitySeparation", StringComparison.Ordinal), "Graph explorer runtime physics should keep clustered graph areas separated as aggregates during stabilization.");
        Assert(html.Contains("applyOverlapPressure", StringComparison.Ordinal) && html.Contains("overlapPressureEvents", StringComparison.Ordinal), "Graph explorer runtime physics should apply and export lightweight overlap pressure while the solver is running.");
        Assert(html.Contains("physicsCommunityAnchors", StringComparison.Ordinal) && html.Contains("physicsCommunityKey", StringComparison.Ordinal) && html.Contains("hubSpread", StringComparison.Ordinal), "Graph explorer runtime physics should keep communities and hubs structurally separated during stabilization.");
        Assert(html.Contains("compactStabilizedLayout", StringComparison.Ordinal) && html.Contains("cfxGraphLayoutCompaction", StringComparison.Ordinal), "Graph explorer runtime physics should compact oversized stabilized layouts back into a readable viewport envelope.");
        Assert(html.Contains("expandDenseLayout", StringComparison.Ordinal) && html.Contains("cfxGraphLayoutDensityExpansion", StringComparison.Ordinal), "Graph explorer runtime physics should expand dense stabilized layouts when overlap pressure shows the graph is too tightly packed.");
        Assert(html.Contains("runLayoutQualityPass", StringComparison.Ordinal) && html.Contains("spreadHubNeighborhoods", StringComparison.Ordinal) && html.Contains("separateOverlaps", StringComparison.Ordinal), "Graph explorer runtime physics should run a structured quality pass after stabilization instead of only fitting whatever the force solver produced.");
        Assert(html.Contains("restoreClusterAnchors", StringComparison.Ordinal) && html.Contains("cfxGraphLayoutClusterGravity", StringComparison.Ordinal) && html.Contains("cfxGraphLayoutCommunityGravity", StringComparison.Ordinal), "Graph explorer runtime physics should pull stabilized communities back toward their prepared regions instead of blending all clusters into one mass.");
        Assert(html.Contains("spreadCommunityAreas", StringComparison.Ordinal) && html.Contains("cfxGraphLayoutCommunitySpread", StringComparison.Ordinal), "Graph explorer runtime layout quality should separate whole community areas before final overlap cleanup.");
        Assert(html.Contains("cluster:${node.cluster}", StringComparison.Ordinal) && html.Contains("group:${node.groupId}", StringComparison.Ordinal) && html.Contains("kind:${node.kind}", StringComparison.Ordinal), "Graph explorer runtime layout quality should keep cluster, group, and kind community namespaces separate.");
        Assert(html.Contains("hasFixedAnchors = state.nodes.some(node => node.fixed)", StringComparison.Ordinal) && html.Contains("fixed-anchors-skipped", StringComparison.Ordinal), "Graph explorer runtime layout quality should skip movable-only recentering when fixed anchors are present.");
        Assert(html.Contains("cfxGraphLayoutQualityScore", StringComparison.Ordinal) && html.Contains("cfxGraphLayoutOverlapCount", StringComparison.Ordinal) && html.Contains("cfxGraphLayoutHubSpread", StringComparison.Ordinal), "Graph explorer runtime should publish layout quality diagnostics for dense graph QA.");
        Assert(html.Contains("root.__cfxGraphAutoFitOnStabilize && root.__cfxGraphViewportTouched !== true", StringComparison.Ordinal), "Graph explorer runtime physics should re-check user viewport touches before auto-fitting after stabilization.");
        Assert(html.Contains("if (dx === 0 && dy === 0)", StringComparison.Ordinal), "Graph explorer pairwise physics should force a non-zero deterministic jitter vector for perfectly overlapping node pairs.");
        Assert(!html.Contains("node.x = Math.max(24, Math.min(936, point.x))", StringComparison.Ordinal), "Graph explorer dragging should not force nodes back into the old fixed viewport box.");
        Assert(html.Contains("ResizeObserver", StringComparison.Ordinal) && html.Contains("__cfxGraphViewportTouched", StringComparison.Ordinal), "Graph explorer viewport should refit on stage resize until the user explicitly pans or zooms.");
        Assert(html.Contains("root.__cfxGraphState ||", StringComparison.Ordinal) && html.Contains("renderer === 'canvas' && currentState", StringComparison.Ordinal), "Graph explorer viewport updates should reuse cached graph state and avoid Canvas redraw work while SVG is the active renderer.");
        Assert(html.Contains("bindOverview", StringComparison.Ordinal) && html.Contains("updateOverview", StringComparison.Ordinal) && html.Contains("cfxgraphoverview", StringComparison.Ordinal) && html.Contains("const drawableEdges = currentState.edges.filter", StringComparison.Ordinal), "Graph explorer runtime should keep an overview/minimap synchronized with viewport, selection, focus, filtering, and layout changes.");
        Assert(html.Contains("clearHiddenSelections", StringComparison.Ordinal) && html.Contains("syncGraphItemTabStops", StringComparison.Ordinal) && html.Contains("cfxGraphRendererActive !== 'canvas'", StringComparison.Ordinal), "Graph explorer runtime should remove hidden selections and remove invisible SVG tab stops when Canvas is the active renderer.");
        Assert(html.Contains("cfx-graph-cluster-expanded", StringComparison.Ordinal) && html.Contains("clearHiddenSelections(root);", StringComparison.Ordinal), "Graph explorer runtime should remove selected clusters when cluster expansion makes them inaccessible.");
        Assert(html.Contains("focusedSelectionHidden", StringComparison.Ordinal) && html.Contains("clearNeighborhoodFocus(root)", StringComparison.Ordinal), "Graph explorer runtime should clear neighborhood focus when filters or cluster collapse hide the focused selection.");
        Assert(html.Contains("image.naturalWidth > 0", StringComparison.Ordinal) && html.Contains("malformed host-supplied images", StringComparison.Ordinal) && html.Contains("imageLoadCallbacks", StringComparison.Ordinal), "Graph explorer Canvas runtime should tolerate broken image-node URLs and redraw every waiting root when shared images finish loading.");
        Assert(html.Contains("data-cfx-graph-action=\"zoom-in\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-action=\"zoom-out\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-action=\"fit\"", StringComparison.Ordinal), "Graph explorer toolbar should expose dependency-free zoom and fit controls.");
        Assert(html.Contains("edgePathData(rendered, control)", StringComparison.Ordinal) && html.Contains("bezierCurveTo(loop.c1.x", StringComparison.Ordinal), "Graph explorer runtime should preserve visible self-loop paths after physics, dragging, aggregate edge retargeting, and Canvas redraws.");
        Assert(html.Contains("nodeHitDistance(node, point, 10)", StringComparison.Ordinal) && html.Contains("nodeUsesRectangularGeometry(node)", StringComparison.Ordinal) && html.Contains("node.shape === 'database'", StringComparison.Ordinal), "Graph explorer Canvas hit testing should use shape extents instead of circular hit radii for rich nodes.");
        Assert(html.Contains("node.shape === 'text'", StringComparison.Ordinal) && html.Contains("return;", StringComparison.Ordinal), "Graph explorer Canvas should not draw an artificial mark for text-only nodes.");
        Assert(html.Contains("loop?.c1 || control || edge.target", StringComparison.Ordinal) && html.Contains("loop?.start || edge.source", StringComparison.Ordinal), "Graph explorer Canvas should draw source arrows at self-loop starts instead of over target markers.");
        Assert(html.Contains("distanceToSelfLoop", StringComparison.Ordinal) && html.Contains("selfLoopGeometry(node)", StringComparison.Ordinal), "Graph explorer Canvas hit testing should follow visible self-loop curves instead of their zero-length source-target segment.");
        Assert(html.Contains("distanceToQuadratic(point, endpoints.source, control, endpoints.target)", StringComparison.Ordinal), "Graph explorer Canvas hit testing should follow the rendered quadratic curve for curved and aggregate edges.");
        Assert(html.Contains("nodeHalfWidth(node)", StringComparison.Ordinal) && html.Contains("nodeHalfHeight(node)", StringComparison.Ordinal) && html.Contains("seen.add(node.id)", StringComparison.Ordinal), "Graph explorer Canvas hit testing should index large nodes across every grid cell they cover without duplicate candidates.");
        Assert(html.Contains("if (exhausted) break", StringComparison.Ordinal) && html.Contains("pairs > maxPairs", StringComparison.Ordinal), "Graph explorer structural physics should stop scanning overlap pairs once the tick budget is exhausted.");
        Assert(html.Contains("fixed-anchors-skipped", StringComparison.Ordinal), "Graph explorer layout-quality recentering should not move only movable nodes when fixed anchors are present.");
        Assert(html.Contains("const hitBlocksPan = (node && hasFeature(root, 'DragNodes')) || hitCanSelect", StringComparison.Ordinal), "Graph explorer panning should still work over inert graph items when selection and dragging are disabled.");
        Assert(html.Contains("root.__cfxGraphPointerSelectionId = node.id;\n        root.__cfxGraphSuppressClickId = node.id;", StringComparison.Ordinal), "Graph explorer click suppression should consume the follow-up click for pointer-selected nodes even after slow modifier clicks.");
        Assert(!html.Contains("else button.setAttribute('aria-pressed'", StringComparison.Ordinal) && !html.Contains("data-cfx-graph-action=\"fit\" aria-pressed", StringComparison.Ordinal), "Graph explorer toolbar should not leave one-shot actions announced as pressed toggles.");
        Assert(html.Contains("syncClusterControls", StringComparison.Ordinal) && html.Contains("syncFocusControls", StringComparison.Ordinal) && html.Contains("syncPhysicsControls", StringComparison.Ordinal), "Graph explorer toolbar should keep aria-pressed only on maintained toggle controls.");
        Assert(html.Contains("edges > num(root, 'data-cfx-performance-max-svg-edges', 3000)", StringComparison.Ordinal) && html.Contains("edgeHasVisibleEndpoints", StringComparison.Ordinal) && html.Contains("visualEdge(edge", StringComparison.Ordinal), "Graph explorer runtime should prefer Canvas for dense edge graphs and render aggregate collapsed-cluster edges.");
        Assert(html.Contains("clusterInfo.radius * metrics.scale", StringComparison.Ordinal), "Graph explorer overview should draw visible collapsed cluster summaries instead of an empty minimap.");
        Assert(html.Contains("cfxgraphready", StringComparison.Ordinal) && html.Contains("cfxgraphselect", StringComparison.Ordinal) && html.Contains("cfxgraphselection", StringComparison.Ordinal) && html.Contains("cfxgraphfilter", StringComparison.Ordinal), "Graph explorer runtime should publish reusable host events.");
        Assert(html.Contains("cfxgraphstabilized", StringComparison.Ordinal) && html.Contains("cfxgraphperformance", StringComparison.Ordinal) && html.Contains("cfxgraphlod", StringComparison.Ordinal) && html.Contains("cfxgraphcluster", StringComparison.Ordinal), "Graph explorer runtime should publish physics, performance, LOD, and clustering events.");
        Assert(html.Contains("publishPerformance", StringComparison.Ordinal) && html.Contains("cfxGraphPerformanceSamples", StringComparison.Ordinal) && html.Contains("cfxGraphPerformanceBudgetMisses", StringComparison.Ordinal), "Graph explorer runtime should keep durable performance summary diagnostics on the graph root.");
        Assert(html.Contains("cfxGraphSelectionCount", StringComparison.Ordinal) && html.Contains("cfxGraphSelectionIds", StringComparison.Ordinal) && html.Contains("cfxGraphSelectionPrimary", StringComparison.Ordinal), "Graph explorer runtime should keep durable multi-selection diagnostics on the graph root.");
        Assert(html.Contains("cfx-graph-lod-compact", StringComparison.Ordinal) && html.Contains("cfx-graph-performance-gated", StringComparison.Ordinal) && html.Contains("cfx-graph-neighborhood-active", StringComparison.Ordinal), "Graph explorer runtime and CSS should expose large-graph LOD, performance, and neighborhood focus states.");
        Assert(html.Contains(".cfx-graph-has-overview .cfx-graph-overview{display:block}", StringComparison.Ordinal), "Graph explorer CSS should show the overview only when viewport navigation is enabled.");
        Assert(html.Contains(".cfx-graph-edge.cfx-graph-selected", StringComparison.Ordinal) && html.Contains("stroke:var(--cfx-edge-stroke,#94a3b8)", StringComparison.Ordinal), "Graph explorer CSS should visibly preserve selected edge state even when edges carry custom color variables.");
        Assert(html.Contains(".cfx-graph-node.cfx-graph-selected", StringComparison.Ordinal) && html.Contains("stroke:var(--cfx-node-stroke,#eff6ff)", StringComparison.Ordinal), "Graph explorer CSS should visibly preserve selected node state even when nodes carry custom style variables.");
        Assert(html.Contains(".cfx-graph-edge-label.cfx-graph-hidden", StringComparison.Ordinal) && html.Contains(".cfx-graph-edge-label.cfx-graph-cluster-collapsed-member", StringComparison.Ordinal), "Graph explorer CSS should hide filtered and collapsed edge labels.");
        Assert(html.Contains(".cfx-graph-stage{position:relative;overflow:hidden;aspect-ratio:12/7", StringComparison.Ordinal) && html.Contains(".cfx-graph-svg{display:block;width:100%;height:auto;aspect-ratio:12/7}", StringComparison.Ordinal) && !html.Contains("min-height:420px", StringComparison.Ordinal), "Graph explorer CSS should preserve the SVG scene aspect ratio so pointer mapping does not drift under narrow embeds.");
        Assert(!html.Contains("<link", StringComparison.OrdinalIgnoreCase), "Graph explorer pages should not reference external stylesheets.");
        Assert(!html.Contains("@import", StringComparison.OrdinalIgnoreCase), "Graph explorer pages should not import external stylesheets.");
        Assert(!html.Contains("http://", StringComparison.OrdinalIgnoreCase) && !html.Contains("https://", StringComparison.OrdinalIgnoreCase), "Graph explorer pages should remain self-contained.");

        var fragment = scene.ToGraphExplorerHtmlFragment(options => options.ScriptNonce = "fragment-nonce");
        Assert(fragment.Contains("data-cfx-graph-assets=\"true\"", StringComparison.Ordinal), "Graph explorer fragments should include embeddable CSS assets.");
        Assert(fragment.Contains("<script nonce=\"fragment-nonce\">", StringComparison.Ordinal), "Graph explorer fragments should support CSP nonces.");
        var scopedFragment = scene.ToGraphExplorerHtmlFragment(options => options.IdScope = "host-scope");
        Assert(scopedFragment.Contains("id=\"host-scope-title\"", StringComparison.Ordinal) && scopedFragment.Contains("marker-end=\"url(#host-scope-arrow)\"", StringComparison.Ordinal), "Graph explorer fragments should let hosts provide a deterministic SVG id scope for repeated embeds.");
        var repeatedScene = GraphScene.Create("graph", "Repeated graph").AddNode("a", "A");
        var firstRepeatedFragment = repeatedScene.ToGraphExplorerHtmlFragment();
        var secondRepeatedFragment = repeatedScene.ToGraphExplorerHtmlFragment();
        Assert(firstRepeatedFragment == secondRepeatedFragment && !firstRepeatedFragment.Contains("id=\"graph-title\"", StringComparison.Ordinal), "Graph explorer fragments should derive deterministic default SVG ids without process-global counters.");
        Assert(HtmlGraphExplorerRenderer.BuildFragmentStyle().Contains(".cfx-graph-explorer", StringComparison.Ordinal), "Host-registered graph explorer CSS should stay scoped to graph explorer surfaces.");
        Assert(HtmlGraphExplorerRenderer.BuildFragmentStyle().Contains(".cfx-graph-explorer *,.cfx-graph-explorer *:before,.cfx-graph-explorer *:after{box-sizing:inherit}", StringComparison.Ordinal), "Host-registered graph explorer CSS should scope base box sizing to fragments without requiring a page-level shell.");
        Assert(HtmlGraphExplorerRenderer.BuildFragmentStyle().Contains(".cfx-graph-explorer{position:relative", StringComparison.Ordinal), "Host-registered graph explorer CSS should anchor graph tooltips to the explorer component.");
        Assert(HtmlGraphExplorerRenderer.BuildFragmentStyle().Contains("touch-action:auto", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildFragmentStyle().Contains(".cfx-graph-interactive-touch .cfx-graph-stage{touch-action:none", StringComparison.Ordinal), "Graph explorer CSS should preserve native page scrolling for static embeds and disable touch scrolling only when the graph consumes touch movement.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("requestAnimationFrame(step)", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("cfxgraphperformance", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("cfxGraphRendererActive", StringComparison.Ordinal), "Host-registered graph explorer runtime should include physics, performance, and backend-selection behavior.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("performance: {", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("maxSampleMs", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("performanceBudget:", StringComparison.Ordinal), "Host-registered graph explorer runtime should export repeatable performance summary evidence.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("acceleration === 'barnes-hut'", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("thread: 'main'", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("thread: 'worker'", StringComparison.Ordinal), "Host-registered graph explorer runtime should publish Barnes-Hut versus pairwise physics telemetry, including main-thread versus worker execution.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("cfxGraphHitTest", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("state.nodes.length >= 160 ? 'grid' : 'linear'", StringComparison.Ordinal), "Host-registered graph explorer runtime should publish the active hit-test strategy.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("cluster.el.classList.contains('cfx-graph-cluster-expanded')", StringComparison.Ordinal), "Host-registered graph explorer runtime should ignore expanded cluster hulls for hit testing.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("return best || domHitNodeAt(root, point)", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("nodeHitDistance(candidate, point, 10)", StringComparison.Ordinal), "Host-registered graph explorer runtime should include indexed Canvas hit testing with DOM-backed recovery.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("applyNeighborhoodFocus", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("focus: { active:", StringComparison.Ordinal), "Host-registered graph explorer runtime should focus and export selected-neighborhood state.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("const primary = details.find(item => item.role === 'graph-node')", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("else clearNeighborhoodFocus(root)", StringComparison.Ordinal), "Graph explorer runtime should clear stale neighborhood focus when selection no longer contains a node.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("syncPhysicsControls", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("cfxGraphPhysicsState === 'running'"), "Graph explorer runtime should sync the Physics button pressed state from the actual runtime physics state.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("root.dataset.cfxGraphPhysicsState = 'paused'; syncPhysicsControls(root);", StringComparison.Ordinal), "Graph explorer runtime should sync the Physics button when dragging pauses runtime physics.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("downloadExport", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("cancelable: !!options?.cancelable", StringComparison.Ordinal), "Host-registered graph explorer runtime should support host-interceptable export downloads.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("selection: {", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("selectionCount", StringComparison.Ordinal), "Host-registered graph explorer runtime should export multi-selection state.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("metadata: metadataDetail(node.el)", StringComparison.Ordinal), "Host-registered graph explorer runtime should export structured node metadata in JSON snapshots.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("featureGroups", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("Explorer: ['Selection', 'MultiSelection', 'Search', 'Filtering', 'Viewport'", StringComparison.Ordinal), "Graph explorer runtime should expand grouped feature flags so Explorer enables multi-selection, viewport, filtering, and clustering behavior in the browser.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("data-cfx-search", StringComparison.Ordinal), "Graph explorer runtime search should include serialized metadata attributes.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("metadataDetail", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("metadata: metadataDetail(node)", StringComparison.Ordinal), "Graph explorer runtime selection events should include structured metadata for host inspectors.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("syncSelectionTooltip(root, details, detail)", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("syncSelectionTooltip(root, details)", StringComparison.Ordinal), "Graph explorer runtime should refresh selection tooltip text after additive deselection and hidden-selection cleanup.");
        Assert(!HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("<script>", StringComparison.Ordinal), "Host-registered graph explorer runtime should return raw JavaScript.");

        var canvasHtml = scene.ToGraphExplorerHtmlFragment(options => options.RenderBackend = HtmlGraphRenderBackend.Canvas);
        Assert(canvasHtml.Contains("data-cfx-graph-renderer=\"canvas\"", StringComparison.Ordinal), "Graph explorer fragments should allow hosts to request Canvas as the initial renderer.");
        var webGlHtml = scene.ToGraphExplorerHtmlFragment(options => options.RenderBackend = HtmlGraphRenderBackend.WebGl);
        Assert(webGlHtml.Contains("data-cfx-graph-renderer=\"canvas\"", StringComparison.Ordinal) && !webGlHtml.Contains("data-cfx-graph-renderer=\"webgl\"", StringComparison.Ordinal), "Graph explorer should route explicit WebGL requests to the supported Canvas backend until a real WebGL renderer exists.");
        AssertThrows<InvalidOperationException>(() => scene.ToGraphExplorerHtmlFragment(options => options.RenderBackend = (HtmlGraphRenderBackend)999), "Graph explorer should reject unknown render backend enum values instead of silently falling back to SVG.");
        Assert(HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("configured === 'webgl' ? 'canvas' : configured", StringComparison.Ordinal), "Graph explorer runtime should also route host-authored WebGL backend attributes to Canvas instead of silently falling back to SVG.");

        var expandedClusterHtml = GraphScene.Create("expanded-cluster", "Expanded cluster")
            .AddNode("a", "A")
            .AddNode("b", "B")
            .AddCluster("expanded", "Expanded", new[] { "a", "b" })
            .ToGraphExplorerHtmlFragment();
        Assert(expandedClusterHtml.Contains("class=\"cfx-graph-cluster cfx-graph-cluster-expanded\" data-cfx-role=\"graph-cluster\" tabindex=\"-1\" aria-hidden=\"true\"", StringComparison.Ordinal), "Graph explorer SVG should remove expanded transparent cluster summaries from the keyboard tab order before browser bindings run.");

        var clusteringDisabledScene = GraphScene.Create("cluster-disabled", "Cluster disabled")
            .AddNode("a", "A")
            .AddNode("b", "B")
            .AddEdge("a-b", "a", "b")
            .AddCluster("disabled", "Disabled", new[] { "a", "b" }, cluster => {
                cluster.Collapsed = true;
                cluster.Kind = "cluster-only";
            });
        clusteringDisabledScene.Options.Disable(GraphSceneFeatures.Clustering);
        clusteringDisabledScene.Options.LevelOfDetail.CollapseClustersOnLoad = true;
        var clusteringDisabledHtml = clusteringDisabledScene.ToGraphExplorerHtmlFragment();
        Assert(!clusteringDisabledHtml.Contains("<g class=\"cfx-graph-cluster", StringComparison.Ordinal) && !clusteringDisabledHtml.Contains("class=\"cfx-graph-node cfx-graph-cluster-collapsed-member\"", StringComparison.Ordinal) && !clusteringDisabledHtml.Contains("class=\"cfx-graph-edge cfx-graph-cluster-collapsed-member\"", StringComparison.Ordinal), "Graph explorer static markup should not hide cluster members when the host disables clustering.");
        Assert(!clusteringDisabledHtml.Contains("<option value=\"cluster-only\">", StringComparison.Ordinal), "Graph explorer filters should not expose cluster-only kind facets when the host disables clustering.");

        var readOnlyScene = GraphScene.Create("read-only", "Read only")
            .AddNode("a", "A")
            .AddNode("b", "B")
            .AddEdge("a-b", "a", "b")
            .AddCluster("readonly-cluster", "Read only cluster", new[] { "a", "b" }, cluster => cluster.Collapsed = true);
        readOnlyScene.Options.Disable(GraphSceneFeatures.Selection);
        var readOnlyHtml = readOnlyScene.ToGraphExplorerHtmlFragment();
        Assert(readOnlyHtml.Contains("tabindex=\"-1\" data-cfx-role=\"graph-node\" data-node-id=\"a\"", StringComparison.Ordinal) && readOnlyHtml.Contains("tabindex=\"-1\" data-cfx-role=\"graph-edge\" data-edge-id=\"a-b\"", StringComparison.Ordinal) && readOnlyHtml.Contains("data-cfx-role=\"graph-cluster\" tabindex=\"-1\" aria-hidden=\"false\"", StringComparison.Ordinal), "Graph explorer static markup should not leave inert graph items in the tab order when selection is disabled.");
        Assert(!readOnlyHtml.Contains("data-cfx-graph-action=\"focus\"", StringComparison.Ordinal) && !readOnlyHtml.Contains("data-cfx-graph-action=\"clear-selection\"", StringComparison.Ordinal), "Graph explorer toolbar should hide selection-dependent actions when selection is disabled.");

        var noLodCanvasHtml = readOnlyScene.ToGraphExplorerHtmlFragment(options => options.RenderBackend = HtmlGraphRenderBackend.Canvas);
        Assert(noLodCanvasHtml.Contains("syncGraphItemTabStops(root);", StringComparison.Ordinal), "Graph explorer binding should sync invisible SVG tab stops when Canvas is requested while LOD is disabled.");

        var declaredClusterHtml = GraphScene.Create("declared-cluster", "Declared cluster")
            .AddNode("a", "A")
            .AddNode("b", "B")
            .AddCluster("declared", "Declared", new[] { "a", "b" })
            .ToGraphExplorerHtmlFragment();
        Assert(declaredClusterHtml.Contains("data-node-id=\"a\" data-node-label=\"A\" data-node-cluster=\"declared\"", StringComparison.Ordinal) && declaredClusterHtml.Contains("data-node-id=\"b\" data-node-label=\"B\" data-node-cluster=\"declared\"", StringComparison.Ordinal), "Graph explorer renderer should derive node cluster membership from declared GraphSceneCluster.NodeIds when node ClusterId is not duplicated.");
        Assert(!declaredClusterHtml.Contains("data-cfx-role=\"graph-cluster\" tabindex=\"0\" aria-hidden=\"false\" data-cluster-id=\"declared\" data-cluster-label=\"Declared\" data-cluster-kind=\"\" data-cluster-node-ids=\"a,b\" data-cluster-collapsed=\"false\" data-cfx-status=", StringComparison.Ordinal), "Graph explorer renderer should not serialize cluster kind as status.");
        var nodeSideClusterHtml = GraphScene.Create("node-side-cluster", "Node side cluster")
            .AddNode("a", "A", node => node.ClusterId = "declared")
            .AddNode("b", "B")
            .AddEdge("a-b", "a", "b")
            .AddCluster("declared", "Declared", Array.Empty<string>(), cluster => cluster.Collapsed = true)
            .ToGraphExplorerHtmlFragment();
        Assert(nodeSideClusterHtml.Contains("data-cluster-node-ids=\"a\"", StringComparison.Ordinal) && nodeSideClusterHtml.Contains("class=\"cfx-graph-node cfx-graph-cluster-collapsed-member\" tabindex=\"0\" data-cfx-role=\"graph-node\" data-node-id=\"a\"", StringComparison.Ordinal), "Graph explorer collapsed clusters should honor node-side ClusterId membership even when GraphSceneCluster.NodeIds is not duplicated.");
        Assert(!nodeSideClusterHtml.Contains("class=\"cfx-graph-edge cfx-graph-cluster-collapsed-member\" tabindex=\"0\" data-cfx-role=\"graph-edge\" data-edge-id=\"a-b\"", StringComparison.Ordinal), "Graph explorer collapsed clusters should keep cross-boundary edges visible for aggregate rendering.");
        Assert(layoutSource.Contains("BuildClusterMembership", StringComparison.Ordinal) && rendererSource.Contains("NodeClusterId(node, clusterMembership)", StringComparison.Ordinal) && layoutSource.Contains("CommunityCenters", StringComparison.Ordinal), "Graph explorer prepared layout should use declared cluster membership as a community key and assign deterministic community areas.");
        Assert(layoutSource.Contains("SeparatePreparedOverlaps", StringComparison.Ordinal) && layoutSource.Contains("GridKey", StringComparison.Ordinal), "Graph explorer prepared layout should resolve opening overlaps with a scalable spatial grid before runtime physics starts.");
        Assert(layoutSource.Contains("if (pairs > maxPairs) return moved;", StringComparison.Ordinal), "Graph explorer prepared layout should stop scanning overlap pairs once the static layout budget is exhausted.");

        var loadCollapsedHtml = GraphScene.Create("load-collapsed", "Load collapsed")
            .AddNode("a", "A")
            .AddCluster("collapsed", "Collapsed", new[] { "a" }, cluster => cluster.Collapsed = false);
        loadCollapsedHtml.Options.LevelOfDetail.CollapseClustersOnLoad = true;
        Assert(loadCollapsedHtml.ToGraphExplorerHtmlFragment().Contains("data-cluster-collapsed=\"true\"", StringComparison.Ordinal), "Graph explorer static markup should serialize the effective load-collapsed cluster state before bindings run.");

        var explicitClusterCollapse = GraphScene.Create("explicit-cluster-collapse", "Explicit cluster collapse")
            .AddNode("a", "A")
            .AddCluster("collapsed", "Collapsed", new[] { "a" }, cluster => cluster.Collapsed = false);
        explicitClusterCollapse.Options.Cluster.CollapseOnLoad = true;
        var explicitClusterCollapseHtml = explicitClusterCollapse.ToGraphExplorerHtmlFragment();
        Assert(explicitClusterCollapseHtml.Contains("data-cfx-graph-cluster-collapse-on-load=\"true\"", StringComparison.Ordinal) && explicitClusterCollapseHtml.Contains("collapseOnLoad: attr(root, 'data-cfx-lod-collapse-clusters') === 'true' || attr(root, 'data-cfx-graph-cluster-collapse-on-load') === 'true'", StringComparison.Ordinal), "Graph explorer JSON export should report explicit cluster collapse defaults, not only LOD collapse defaults.");

        var dashPatternOnly = GraphScene.Create("dash-pattern-only", "Dash pattern only")
            .AddNode("a", "A")
            .AddNode("b", "B")
            .AddEdge("a-b", "a", "b", configure: edge => edge.Style.DashPattern = "4 2");
        var dashPatternOnlyHtml = dashPatternOnly.ToGraphExplorerHtmlFragment();
        Assert(dashPatternOnlyHtml.Contains("data-edge-dash-pattern=\"4 2\"", StringComparison.Ordinal) && !dashPatternOnlyHtml.Contains("stroke-dasharray:4 2", StringComparison.Ordinal), "Graph explorer SVG should only apply dash patterns when the graph edge is marked dashed, matching Canvas and PNG behavior.");

        var anchoredHierarchy = GraphScene.Create("anchored-hierarchy", "Anchored hierarchy")
            .AddNode("root", "Root", node => {
                node.Level = 0;
                node.X = 480;
                node.Y = 280;
                node.Fixed = true;
            })
            .AddNode("child", "Child")
            .AddEdge("root-child", "root", "child", configure: edge => edge.Directed = true);
        anchoredHierarchy.Options.Layout.Mode = GraphLayoutMode.Hierarchical;
        var anchoredHierarchyHtml = anchoredHierarchy.ToGraphExplorerHtmlFragment();
        Assert(anchoredHierarchyHtml.Contains("data-node-id=\"root\" data-node-label=\"Root\"", StringComparison.Ordinal) && anchoredHierarchyHtml.Contains("data-node-id=\"child\" data-node-label=\"Child\"", StringComparison.Ordinal) && anchoredHierarchyHtml.Contains("data-node-y=\"400\"", StringComparison.Ordinal), "Hierarchical graph layout should place generated nodes relative to explicit fixed anchors instead of collapsing them onto the viewport center.");

        var largeCollapsedCluster = GraphScene.Create("large-collapsed-cluster", "Large collapsed cluster");
        for (var i = 0; i < 16; i++) largeCollapsedCluster.AddNode("n" + i, "Node " + i);
        largeCollapsedCluster.AddCluster("large", "Large", Enumerable.Range(0, 16).Select(i => "n" + i).ToArray(), cluster => cluster.Collapsed = true);
        Assert(largeCollapsedCluster.ToGraphExplorerHtmlFragment().Contains("data-cluster-id=\"large\"", StringComparison.Ordinal) && largeCollapsedCluster.ToGraphExplorerHtmlFragment().Contains("<circle r=\"48\"></circle>", StringComparison.Ordinal), "Graph explorer SVG collapsed cluster summaries should serialize the same member-count radius used by runtime Canvas metrics.");

        var loadCollapsedCrossEdgeHtml = GraphScene.Create("load-collapsed-cross-edge", "Load collapsed cross edge")
            .AddNode("a", "A", node => { node.X = 100; node.Y = 100; })
            .AddNode("b", "B", node => { node.X = 300; node.Y = 100; })
            .AddNode("c", "C", node => { node.X = 100; node.Y = 300; })
            .AddNode("d", "D", node => { node.X = 300; node.Y = 300; })
            .AddEdge("a-b", "a", "b")
            .AddCluster("left", "Left", new[] { "a", "c" }, cluster => cluster.Collapsed = false)
            .AddCluster("right", "Right", new[] { "b", "d" }, cluster => cluster.Collapsed = false);
        loadCollapsedCrossEdgeHtml.Options.LevelOfDetail.CollapseClustersOnLoad = true;
        var loadCollapsedCrossEdgeMarkup = loadCollapsedCrossEdgeHtml.ToGraphExplorerHtmlFragment();
        Assert(!loadCollapsedCrossEdgeMarkup.Contains("class=\"cfx-graph-edge cfx-graph-cluster-collapsed-member\" tabindex=\"0\" data-cfx-role=\"graph-edge\" data-edge-id=\"a-b\"", StringComparison.Ordinal), "Graph explorer static load-collapse should preserve cross-cluster edges for aggregate rendering.");
        Assert(loadCollapsedCrossEdgeMarkup.Contains("data-edge-id=\"a-b\"", StringComparison.Ordinal) && loadCollapsedCrossEdgeMarkup.Contains("d=\"M 134 200 L 266 200\"", StringComparison.Ordinal), "Graph explorer static load-collapse should retarget visible cross-cluster edges to cluster summaries and trim them away from collapsed summaries.");

        var lodDisabledLoadCollapseScene = GraphScene.Create("lod-disabled-load-collapsed", "LOD disabled load collapsed")
            .AddNode("a", "A")
            .AddCluster("collapsed", "Collapsed", new[] { "a" }, cluster => cluster.Collapsed = false);
        lodDisabledLoadCollapseScene.Options.LevelOfDetail.CollapseClustersOnLoad = true;
        lodDisabledLoadCollapseScene.Options.Disable(GraphSceneFeatures.LevelOfDetail);
        var lodDisabledLoadCollapseHtml = lodDisabledLoadCollapseScene.ToGraphExplorerHtmlFragment();
        Assert(lodDisabledLoadCollapseHtml.Contains("data-cluster-collapsed=\"false\"", StringComparison.Ordinal) && lodDisabledLoadCollapseHtml.Contains("cfx-graph-cluster-expanded", StringComparison.Ordinal), "Graph explorer static load-collapse should not collapse clusters when the host disables LevelOfDetail.");

        var staticEmbed = GraphScene.Create("static-embed", "Static embed").AddNode("a", "A");
        staticEmbed.Options.Disable(GraphSceneFeatures.Viewport | GraphSceneFeatures.DragNodes);
        Assert(!staticEmbed.ToGraphExplorerHtmlFragment().Contains("class=\"cfx-graph-explorer cfx-graph-interactive-touch\"", StringComparison.Ordinal), "Graph explorer static embeds should not block native touch scrolling when panning and dragging are disabled.");

        var nonRuntimePhysics = GraphScene.Create("non-runtime-physics", "Non runtime physics").AddNode("a", "A");
        nonRuntimePhysics.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization);
        var nonRuntimePhysicsHtml = nonRuntimePhysics.ToGraphExplorerHtmlFragment();
        Assert(!nonRuntimePhysicsHtml.Contains("<button class=\"cfx-graph-tool\" type=\"button\" data-cfx-graph-action=\"physics\"", StringComparison.Ordinal) && !nonRuntimePhysicsHtml.Contains("<button class=\"cfx-graph-tool\" type=\"button\" data-cfx-graph-action=\"stabilize\"", StringComparison.Ordinal), "Graph explorer toolbar should hide physics controls when the selected solver cannot run runtime physics.");

        var layoutScene = GraphScene.Create("layout-quality", "Layout quality");
        layoutScene.AddNode("hub", "Hub", node => {
            node.Kind = "service";
            node.Size = 14;
        });
        for (var index = 0; index < 4; index++) {
            var id = "left-" + index.ToString(CultureInfo.InvariantCulture);
            layoutScene.AddNode(id, "Left " + index.ToString(CultureInfo.InvariantCulture), node => {
                node.Kind = "identity";
                node.ClusterId = "left";
            });
            layoutScene.AddEdge("hub-" + id, "hub", id);
        }
        for (var index = 0; index < 4; index++) {
            var id = "right-" + index.ToString(CultureInfo.InvariantCulture);
            layoutScene.AddNode(id, "Right " + index.ToString(CultureInfo.InvariantCulture), node => {
                node.Kind = "database";
                node.ClusterId = "right";
            });
            layoutScene.AddEdge("hub-" + id, "hub", id);
        }

        layoutScene.AddCluster("left", "Left", Enumerable.Range(0, 4).Select(index => "left-" + index.ToString(CultureInfo.InvariantCulture)).ToArray());
        layoutScene.AddCluster("right", "Right", Enumerable.Range(0, 4).Select(index => "right-" + index.ToString(CultureInfo.InvariantCulture)).ToArray());

        var layoutHtml = layoutScene.ToGraphExplorerHtmlFragment();
        var hub = ExtractGraphNodePoint(layoutHtml, "hub");
        Assert(Math.Abs(hub.X - 480) < 1 && Math.Abs(hub.Y - 280) < 1, "Graph explorer prepared layout should seed the strongest hub in the middle before runtime physics starts.");
        var leftAverage = AveragePoint(layoutHtml, "left-", 4);
        var rightAverage = AveragePoint(layoutHtml, "right-", 4);
        var communityDistance = Distance(leftAverage, rightAverage);
        Assert(communityDistance > 120, "Graph explorer prepared layout should open connected communities into visibly different areas instead of stacking them together.");
        for (var index = 0; index < 4; index++) {
            var left = ExtractGraphNodePoint(layoutHtml, "left-" + index.ToString(CultureInfo.InvariantCulture));
            var right = ExtractGraphNodePoint(layoutHtml, "right-" + index.ToString(CultureInfo.InvariantCulture));
            Assert(left.X >= 75 && left.X <= 885 && left.Y >= 65 && left.Y <= 495, "Graph explorer prepared layout should fit generated left-community nodes inside the opening scene.");
            Assert(right.X >= 75 && right.X <= 885 && right.Y >= 65 && right.Y <= 495, "Graph explorer prepared layout should fit generated right-community nodes inside the opening scene.");
            Assert(Distance(hub, left) > 35 && Distance(hub, right) > 35, "Graph explorer prepared layout should push non-hub nodes outward from the centered hub.");
        }

        var explicitNeighborHtml = GraphScene.Create("explicit-neighbor", "Explicit neighbor")
            .AddNode("fixed", "Fixed", node => {
                node.X = 320;
                node.Y = 240;
                node.Size = 20;
            })
            .AddNode("generated", "Generated", node => node.Size = 18)
            .AddEdge("fixed-generated", "fixed", "generated")
            .ToGraphExplorerHtmlFragment();
        Assert(Distance(ExtractGraphNodePoint(explicitNeighborHtml, "fixed"), ExtractGraphNodePoint(explicitNeighborHtml, "generated")) > 70, "Graph explorer prepared layout should keep a generated neighbor visibly away from an explicit-position node.");

        var centeredExplicitNeighborHtml = GraphScene.Create("center-explicit-neighbor", "Center explicit neighbor")
            .AddNode("fixed", "Fixed", node => {
                node.X = 480;
                node.Y = 280;
                node.Size = 20;
            })
            .AddNode("generated", "Generated", node => node.Size = 18)
            .AddEdge("fixed-generated", "fixed", "generated")
            .ToGraphExplorerHtmlFragment();
        Assert(Distance(ExtractGraphNodePoint(centeredExplicitNeighborHtml, "fixed"), ExtractGraphNodePoint(centeredExplicitNeighborHtml, "generated")) > 70, "Graph explorer prepared layout should not normalize a single generated neighbor back over an explicit center anchor.");

        var mixedAnchorHtml = GraphScene.Create("mixed-anchor", "Mixed anchor")
            .AddNode("fixed", "Fixed", node => {
                node.X = 100;
                node.Y = 100;
                node.Size = 20;
            })
            .AddNode("generated-0", "Generated 0")
            .AddNode("generated-1", "Generated 1")
            .AddEdge("fixed-generated-0", "fixed", "generated-0")
            .AddEdge("fixed-generated-1", "fixed", "generated-1")
            .ToGraphExplorerHtmlFragment();
        var mixedAverage = AveragePoint(mixedAnchorHtml, "generated-", 2);
        Assert(mixedAverage.X < 300 && mixedAverage.Y < 280 && Distance(ExtractGraphNodePoint(mixedAnchorHtml, "fixed"), mixedAverage) < 220, "Graph explorer prepared layout should keep generated neighbors anchored near explicit positions instead of normalizing them to the scene center.");
        var mixedFixed = ExtractGraphNodePoint(mixedAnchorHtml, "fixed");
        Assert(Distance(mixedFixed, ExtractGraphNodePoint(mixedAnchorHtml, "generated-0")) > 70 && Distance(mixedFixed, ExtractGraphNodePoint(mixedAnchorHtml, "generated-1")) > 70, "Graph explorer prepared layout should reserve explicit-anchor spacing for each generated neighbor in a mixed component.");

        var generatedHubHtml = GraphScene.Create("generated-hub-anchor", "Generated hub anchor")
            .AddNode("fixed", "Fixed", node => {
                node.X = 120;
                node.Y = 120;
                node.Size = 20;
            })
            .AddNode("generated-hub", "Generated hub")
            .AddNode("generated-leaf", "Generated leaf")
            .AddEdge("fixed-generated-hub", "fixed", "generated-hub")
            .AddEdge("generated-hub-generated-leaf", "generated-hub", "generated-leaf")
            .ToGraphExplorerHtmlFragment();
        Assert(Distance(ExtractGraphNodePoint(generatedHubHtml, "fixed"), ExtractGraphNodePoint(generatedHubHtml, "generated-hub")) > 70, "Graph explorer prepared layout should keep generated hubs from landing on explicit anchors in mixed components.");

        var generatedHubsHtml = GraphScene.Create("generated-hubs-anchor", "Generated hubs anchor")
            .AddNode("fixed", "Fixed", node => {
                node.X = 140;
                node.Y = 140;
                node.Size = 20;
            })
            .AddNode("generated-hub-a", "Generated hub A")
            .AddNode("generated-hub-b", "Generated hub B")
            .AddNode("generated-leaf-a", "Generated leaf A").AddNode("generated-leaf-b", "Generated leaf B")
            .AddEdge("fixed-generated-hub-a", "fixed", "generated-hub-a")
            .AddEdge("fixed-generated-hub-b", "fixed", "generated-hub-b")
            .AddEdge("generated-hub-a-leaf-a", "generated-hub-a", "generated-leaf-a").AddEdge("generated-hub-b-leaf-b", "generated-hub-b", "generated-leaf-b")
            .ToGraphExplorerHtmlFragment();
        var generatedHubsFixed = ExtractGraphNodePoint(generatedHubsHtml, "fixed");
        Assert(Distance(generatedHubsFixed, ExtractGraphNodePoint(generatedHubsHtml, "generated-hub-a")) > 70 && Distance(generatedHubsFixed, ExtractGraphNodePoint(generatedHubsHtml, "generated-hub-b")) > 70, "Graph explorer prepared layout should apply explicit-anchor spacing to every generated hub in multi-hub mixed components.");

        var selfLoopHtml = GraphScene.Create("self-loop", "Self loop")
            .AddNode("node", "Node", node => {
                node.X = 480;
                node.Y = 280;
            })
            .AddEdge("loop", "node", "node", label: "Loop", configure: edge => edge.Directed = true)
            .ToGraphExplorerHtmlFragment();
        Assert(ExtractGraphEdgePath(selfLoopHtml, "loop").Contains(" C ", StringComparison.Ordinal), "Graph explorer SVG should render accepted self-loop edges as visible paths instead of a zero-length segment hidden by the node.");
        Assert(ExtractGraphEdgeLabelPoint(selfLoopHtml, "loop").Y < ExtractGraphNodePoint(selfLoopHtml, "node").Y - 40, "Graph explorer SVG should place self-loop labels on the loop instead of at the node center.");

        var boxTargetHtml = GraphScene.Create("box-target", "Box target")
            .AddNode("source", "Source", node => {
                node.X = 100;
                node.Y = 280;
            })
            .AddNode("target", "Target", node => {
                node.X = 480;
                node.Y = 280;
                node.Size = 30;
                node.Shape = GraphNodeShape.Box;
            })
            .AddEdge("source-target", "source", "target", configure: edge => edge.Directed = true)
            .ToGraphExplorerHtmlFragment();
        Assert(ExtractGraphEdgePath(boxTargetHtml, "source-target").Contains("L 429.5 280", StringComparison.Ordinal), "Graph explorer SVG should trim directed arrows to the visible edge of box nodes instead of under the rectangle fill.");

        var collapsedRouteLabelHtml = GraphScene.Create("collapsed-route-label", "Collapsed route label")
            .AddNode("source", "Source", node => { node.X = 100; node.Y = 260; node.ClusterId = "cluster"; })
            .AddNode("target", "Target", node => { node.X = 520; node.Y = 260; })
            .AddCluster("cluster", "Cluster", new[] { "source" }, cluster => cluster.Collapsed = true)
            .AddEdge("route", "source", "target", "Route", edge => {
                edge.Shape = GraphEdgeShape.Polyline;
                edge.RoutePoints.Add(new GraphScenePoint(100, 260));
                edge.RoutePoints.Add(new GraphScenePoint(100, 120));
                edge.RoutePoints.Add(new GraphScenePoint(520, 120));
                edge.RoutePoints.Add(new GraphScenePoint(520, 260));
                edge.Directed = true;
            })
            .ToGraphExplorerHtmlFragment();
        Assert(Math.Abs(ExtractGraphEdgeLabelPoint(collapsedRouteLabelHtml, "route").Y - 253) < 0.001, "Graph explorer SVG should place collapsed routed-edge labels on the rendered collapsed geometry instead of the hidden member route.");

        var imageFallbackHtml = GraphScene.Create("image-fallback-target", "Image fallback target").AddNode("source", "Source", node => { node.X = 100; node.Y = 280; }).AddNode("target", "Target", node => { node.X = 480; node.Y = 280; node.Size = 30; node.Shape = GraphNodeShape.Image; }).AddEdge("source-target", "source", "target", configure: edge => edge.Directed = true).ToGraphExplorerHtmlFragment();
        Assert(imageFallbackHtml.Contains("data-node-shape=\"circle\"", StringComparison.Ordinal) && ExtractGraphEdgePath(imageFallbackHtml, "source-target").Contains("L 443 280", StringComparison.Ordinal), "Graph explorer SVG should use the effective rendered circle shape for image nodes without image URLs.");

        var dottedIdHtml = GraphScene.Create("graph.with.dot", "Graph with dot").AddNode("a", "A").ToGraphExplorerHtmlFragment();
        var dashedIdHtml = GraphScene.Create("graph-with-dot", "Graph with dash").AddNode("a", "A").ToGraphExplorerHtmlFragment();
        Assert(dottedIdHtml.Contains("id=\"graph.with.dot-", StringComparison.Ordinal) && dottedIdHtml.Contains("-arrow\"", StringComparison.Ordinal), "Graph explorer SVG ids should preserve valid dots in scoped scene ids.");
        Assert(dashedIdHtml.Contains("id=\"graph-with-dot-", StringComparison.Ordinal) && dashedIdHtml.Contains("-arrow\"", StringComparison.Ordinal), "Graph explorer SVG ids should keep dashed scoped scene ids distinct from dotted scene ids.");

        var scaleScene = BuildScaleConfidenceScene(1000, 1800);
        scaleScene.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 500;
        scaleScene.Options.Performance.MaxInteractiveCanvasNodes = 10000;
        scaleScene.Options.Performance.MaxInteractiveCanvasEdges = 24000;
        var scaleHtml = scaleScene.ToGraphExplorerHtmlFragment(options => options.RenderBackend = HtmlGraphRenderBackend.Canvas);
        Assert(scaleHtml.Contains("data-cfx-graph-node-count=\"1000\"", StringComparison.Ordinal) && scaleHtml.Contains("data-cfx-graph-edge-count=\"1800\"", StringComparison.Ordinal), "Graph explorer should render a 1k-node scale-confidence artifact instead of only toy and 360-node scenes.");
        Assert(scaleHtml.Contains("data-cfx-graph-renderer=\"canvas\"", StringComparison.Ordinal) && scaleHtml.Contains("data-cfx-lod-canvas-threshold=\"500\"", StringComparison.Ordinal), "Graph explorer should expose Canvas LOD for larger graph counts.");
        Assert(scaleHtml.Contains("data-cfx-performance-max-canvas-nodes=\"10000\"", StringComparison.Ordinal) && scaleHtml.Contains("data-cfx-performance-max-canvas-edges=\"24000\"", StringComparison.Ordinal), "Graph explorer should carry explicit 10k-object Canvas performance budgets for large-scene QA.");
        var scaleBounds = GraphNodeBounds(scaleHtml, "n", 1000);
        Assert(scaleBounds.Width > 460 && scaleBounds.Height > 260, "Graph explorer prepared layout should open 1k-node graphs across the scene instead of stacking them into a corner or line.");
        Assert(Math.Abs(scaleBounds.CenterX - 480) < 80 && Math.Abs(scaleBounds.CenterY - 280) < 70, "Graph explorer prepared layout should keep 1k-node graphs centered before viewport fitting.");
    }

    private static (double MinX, double MinY, double MaxX, double MaxY, double Width, double Height, double CenterX, double CenterY) GraphNodeBounds(string html, string prefix, int count) {
        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        for (var index = 0; index < count; index++) {
            var point = ExtractGraphNodePoint(html, prefix + index.ToString(CultureInfo.InvariantCulture));
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
        }

        return (minX, minY, maxX, maxY, maxX - minX, maxY - minY, (minX + maxX) / 2, (minY + maxY) / 2);
    }

    private static (double X, double Y) AveragePoint(string html, string prefix, int count) {
        double x = 0;
        double y = 0;
        for (var index = 0; index < count; index++) {
            var point = ExtractGraphNodePoint(html, prefix + index.ToString(CultureInfo.InvariantCulture));
            x += point.X;
            y += point.Y;
        }

        return (x / count, y / count);
    }

    private static (double X, double Y) ExtractGraphNodePoint(string html, string id) {
        var nodeIndex = html.IndexOf("data-node-id=\"" + id + "\"", StringComparison.Ordinal);
        if (nodeIndex < 0) throw new InvalidOperationException("Missing graph node: " + id);
        var nodeEnd = html.IndexOf('>', nodeIndex);
        if (nodeEnd < nodeIndex) throw new InvalidOperationException("Malformed graph node: " + id);
        var nodeMarkup = html.Substring(nodeIndex, nodeEnd - nodeIndex);
        return (ExtractDoubleAttribute(nodeMarkup, "data-node-x"), ExtractDoubleAttribute(nodeMarkup, "data-node-y"));
    }

    private static string ExtractGraphEdgePath(string html, string id) {
        var edgeIndex = html.IndexOf("data-edge-id=\"" + id + "\"", StringComparison.Ordinal);
        if (edgeIndex < 0) throw new InvalidOperationException("Missing graph edge: " + id);
        var edgeEnd = html.IndexOf('>', edgeIndex);
        if (edgeEnd < edgeIndex) throw new InvalidOperationException("Malformed graph edge: " + id);
        return ExtractStringAttribute(html.Substring(edgeIndex, edgeEnd - edgeIndex), "d");
    }

    private static (double X, double Y) ExtractGraphEdgeLabelPoint(string html, string id) {
        var labelIndex = html.IndexOf("data-edge-label-for=\"" + id + "\"", StringComparison.Ordinal);
        if (labelIndex < 0) throw new InvalidOperationException("Missing graph edge label: " + id);
        var labelEnd = html.IndexOf('>', labelIndex);
        if (labelEnd < labelIndex) throw new InvalidOperationException("Malformed graph edge label: " + id);
        var labelMarkup = html.Substring(labelIndex, labelEnd - labelIndex);
        return (ExtractDoubleAttribute(labelMarkup, "x"), ExtractDoubleAttribute(labelMarkup, "y"));
    }

    private static double ExtractDoubleAttribute(string markup, string name) {
        return double.Parse(ExtractStringAttribute(markup, name), CultureInfo.InvariantCulture);
    }

    private static string ExtractStringAttribute(string markup, string name) {
        var marker = " " + name + "=\"";
        var start = markup.IndexOf(marker, StringComparison.Ordinal);
        var offset = marker.Length;
        if (start < 0 && markup.StartsWith(name + "=\"", StringComparison.Ordinal)) {
            start = 0;
            offset = name.Length + 2;
        }

        if (start < 0) throw new InvalidOperationException("Missing graph node attribute: " + name);
        start += offset;
        var end = markup.IndexOf('"', start);
        if (end < start) throw new InvalidOperationException("Malformed graph node attribute: " + name);
        return markup.Substring(start, end - start);
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b) {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static GraphScene BuildScaleConfidenceScene(int nodeCount, int edgeCount) {
        var scene = GraphScene.Create("scale-confidence", "Scale confidence");
        scene.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.DragNodes | GraphSceneFeatures.Export | GraphSceneFeatures.PerformanceTelemetry);
        scene.Options.Physics.Solver = GraphPhysicsSolver.BarnesHut;
        for (var index = 0; index < nodeCount; index++) {
            var current = index;
            scene.AddNode("n" + current.ToString(CultureInfo.InvariantCulture), "Node " + current.ToString(CultureInfo.InvariantCulture), node => {
                node.Kind = current % 5 == 0 ? "service" : current % 5 == 1 ? "identity" : current % 5 == 2 ? "endpoint" : current % 5 == 3 ? "data" : "network";
                node.ClusterId = "c" + (current % 12).ToString(CultureInfo.InvariantCulture);
                node.Size = current % 17 == 0 ? 12 : 8;
            });
        }

        for (var index = 0; index < edgeCount; index++) {
            var source = index % nodeCount;
            var target = (index * 37 + 11) % nodeCount;
            if (target == source) target = (target + 1) % nodeCount;
            scene.AddEdge("e" + index.ToString(CultureInfo.InvariantCulture), "n" + source.ToString(CultureInfo.InvariantCulture), "n" + target.ToString(CultureInfo.InvariantCulture), configure: edge => {
                edge.Directed = true;
                edge.Weight = index % 9 == 0 ? 1.8 : 1;
                edge.Length = 80 + index % 5 * 14;
            });
        }

        for (var index = 0; index < 12; index++) {
            var clusterId = "c" + index.ToString(CultureInfo.InvariantCulture);
            scene.AddCluster(clusterId, "Cluster " + index.ToString(CultureInfo.InvariantCulture), scene.Nodes.Where(node => string.Equals(node.ClusterId, clusterId, StringComparison.Ordinal)).Select(node => node.Id).ToArray());
        }

        return scene;
    }
}
