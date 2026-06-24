# Graph Explorer

`ChartForgeX.Interactivity` contains the host-neutral graph scene contract. It models nodes, image/icon node hints, rich edges, clusters, physics profiles, and level-of-detail thresholds without requiring a browser, a JavaScript framework, or a runtime package dependency.

`ChartForgeX.Interactivity.Html` is the first adapter for that contract. It renders a self-contained HTML explorer with inline CSS, inline JavaScript, an SVG scene, a Canvas runtime target for large-object fallback, search, filters, cluster controls, multi-selection, selected-node neighborhood focus, runtime physics with Barnes-Hut acceleration and worker-thread stabilization for large scenes, spatial hit testing for large-scene selection and dragging, draggable nodes, pan/zoom/fit viewport controls, SVG/PNG/JSON export actions, image/icon nodes, directional curved/dashed edges with labels, level-of-detail modes, performance telemetry, stable `data-cfx-*` metadata, and host events such as `cfxgraphready`, `cfxgraphselect`, `cfxgraphselection`, `cfxgraphfilter`, `cfxgraphfocus`, `cfxgraphdragstart`, `cfxgraphdrag`, `cfxgraphdragend`, `cfxgraphviewport`, `cfxgraphexport`, `cfxgraphstabilized`, and `cfxgraphperformance`.

```csharp
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

var graph = GraphScene.Create("service-map", "Service map")
    .AddNode("api", "API", node => {
        node.Kind = "service";
        node.Status = "healthy";
        node.ClusterId = "core";
        node.Shape = GraphNodeShape.Image;
        node.ImageUrl = "data:image/svg+xml,%3Csvg viewBox='0 0 64 64'%3E%3Crect width='64' height='64' rx='16' fill='%232563eb'/%3E%3C/svg%3E";
        node.IconText = "A";
    })
    .AddNode("db", "Database", node => {
        node.Kind = "database";
        node.Status = "warning";
        node.ClusterId = "core";
    })
    .AddEdge("api-db", "api", "db", "queries", edge => {
        edge.Kind = "dependency";
        edge.Weight = 2;
        edge.Directed = true;
        edge.Shape = GraphEdgeShape.Curve;
        edge.Curvature = 32;
    })
    .AddCluster("core", "Core services", new[] { "api", "db" });

graph.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.DragNodes);
graph.Options.Physics.Solver = GraphPhysicsSolver.ForceDirected;
graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 1200;
graph.Options.Performance.MaxInteractiveCanvasNodes = 5000;

var html = graph.ToGraphExplorerHtmlPage();
```

The generated gallery includes two review fixtures for this adapter:

- `identity-risk-graph-explorer.html` exercises the compact product-real explorer surface with image nodes, icon nodes, multi-selection, selected-neighborhood focus, draggable nodes, pan/zoom/fit controls, directed curved/dashed edges, filters, clusters, SVG/PNG/JSON export, and host events.
- `enterprise-access-graph-benchmark.html` exercises the large-object path with 360 nodes, 720 directed edges, Canvas fallback at 160 nodes, cluster collapse, LOD, selected-neighborhood focus, worker-backed Barnes-Hut runtime physics when the browser allows Blob workers, spatial hit testing, drag, pan, zoom, and performance telemetry.

## Vis-network-style coverage

The HTML adapter is intended to cover the common vis-network-style exploration surface without adding a runtime dependency.

| Capability | Current support |
| --- | --- |
| Nodes | Circle, box, image-backed, icon text, metadata, status, kind, group, cluster, fixed-position hints. |
| Edges | Directed, curved, dashed, weighted, length hints, labels, status, kind, SVG paths, Canvas drawing. |
| Layout and physics | Static prepared positions, browser force simulation, Barnes-Hut acceleration, worker-backed stabilization for large Barnes-Hut scenes, main-thread fallback. |
| Interaction | Selection, multi-selection with Ctrl/Meta/Shift, keyboard selection, explicit clear selection, drag nodes, pan, wheel zoom, toolbar zoom, fit viewport, selected-node neighborhood focus. |
| Scale controls | Canvas fallback, compact LOD, edge-label hiding, cluster collapse, grid hit testing, performance gates, durable performance summaries, telemetry. |
| Export | Current SVG, Canvas PNG, and JSON with node positions, edge metadata, viewport, selection state, focus state, and performance summary. |
| Host integration | Stable `data-cfx-*` attributes and cancelable/observable browser events for ready, select, selection, filter, focus, drag, viewport, export, cluster, LOD, physics, and performance. |

Known gaps for the next hardening layer are WebGL rendering, OffscreenCanvas solver hardening, adaptive clustering, lasso/box selection, group transforms, undo/redo, richer keyboard shortcuts, persisted interaction state import, and CI/browser performance budgets. These should extend the same `GraphScene` contract and adapter events rather than introducing a second graph model.

The JavaScript runtime source is split into ordered `graph-explorer.*.js` fragments for core rendering, layout/hit testing, physics, and bindings. `HtmlGraphExplorerAssets` concatenates those fragments into one inline script at render time, so generated HTML remains self-contained while the source stays small enough for focused review.

The current adapter intentionally keeps the runtime dependency-free and SVG-first for small scenes. When the requested backend is Canvas, or when an SVG scene crosses `CanvasPreferredNodeThreshold` and fallback is enabled, the runtime draws and hit-tests the same graph model on an HTML canvas while keeping the SVG metadata as the accessible source of truth. It runs browser force simulation for runtime physics profiles, uses Barnes-Hut many-body acceleration for `GraphPhysicsSolver.BarnesHut` and very large scenes, moves large Barnes-Hut stabilization to a Blob Web Worker when supported, falls back to main-thread `requestAnimationFrame` physics when workers are unavailable or blocked by host policy, switches Canvas node hit testing from linear scans to a grid index at large-scene thresholds, lets users select multiple nodes or edges with Ctrl/Meta/Shift, keeps durable `data-cfx-graph-selection-*` diagnostics, emits `cfxgraphselection`, clears selection from the toolbar, focuses the selected node's immediate neighborhood across SVG and Canvas, lets users drag nodes, supports pan/zoom/fit viewport movement, collapses configured clusters, switches compact and Canvas LOD states from thresholds, and emits performance telemetry including the active `pairwise` or `barnes-hut` physics path plus the `main` or `worker` execution thread while respecting separate SVG and Canvas `GraphPerformanceOptions` budgets. Each physics telemetry event also updates durable `data-cfx-graph-performance-*` diagnostics on the explorer root, including sample count, sampled tick count, effective sample budget, last tick, last and maximum sample time, budget miss count, active thread, active acceleration path, and the current `within-budget` or `over-budget` budget state. Export actions emit cancelable `cfxgraphexport` events before downloading current SVG markup, Canvas PNG data, or JSON node positions, selection state, focus state, performance summary, and edge metadata, so hosts can intercept or replace the built-in browser download behavior. Future WebGL rendering, OffscreenCanvas simulation hardening, adaptive clustering, lasso/box selection, group transforms, undo/redo, and CI benchmark gates should consume the same `GraphScene` model instead of adding host-specific graph DTOs.
