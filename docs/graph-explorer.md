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

The current adapter intentionally keeps the runtime dependency-free and SVG-first for small scenes. When the requested backend is Canvas, or when an SVG scene crosses `CanvasPreferredNodeThreshold` and fallback is enabled, the runtime draws and hit-tests the same graph model on an HTML canvas. Exactly one rendering surface is exposed to assistive technology: SVG in SVG mode and the labeled Canvas in Canvas mode; the hidden SVG still carries the structured metadata used by the runtime.

Browser force simulation uses Barnes-Hut many-body acceleration for `GraphPhysicsSolver.BarnesHut` and very large scenes, moves large Barnes-Hut stabilization to a Blob Web Worker when supported, and falls back to main-thread `requestAnimationFrame` physics when workers are unavailable or blocked by host policy. `GraphPerformanceOptions.WorkerProgressInterval` controls browser-visible worker updates independently from `TelemetrySampleInterval`. While Canvas physics is moving, the adapter skips transient labels, icons, arrows, shadows, and hidden-SVG DOM rewrites; it restores the full surface and synchronizes SVG geometry after stabilization and before SVG export.

Performance telemetry reports browser frame cadence and render time separately from multi-tick physics throughput. `WarmupFrameCount` reports initial layout/render startup cost as warmup evidence without charging it to steady-state performance. Every steady frame contributes to the budget miss rate, while host events remain sampled; `over-budget` requires at least two misses and a miss rate above 20%, so one scheduler spike remains visible without permanently failing an otherwise healthy session. Warmup maxima, steady-state maxima, render time, miss count/rate, physics samples, and physics misses remain available through `data-cfx-graph-performance-*`, `cfxgraphperformance`, and JSON export. Export actions emit cancelable `cfxgraphexport` events before downloading current SVG markup, Canvas PNG data, or JSON node positions, selection state, focus state, performance summary, and edge metadata, so hosts can intercept or replace the built-in browser download behavior.
