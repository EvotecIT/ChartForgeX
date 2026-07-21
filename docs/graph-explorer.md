# Graph Explorer

ChartForgeX separates graph data from browser behavior:

- `ChartForgeX.Interactivity` owns the dependency-free, host-neutral `GraphScene` document: nodes, edges, clusters, hierarchy, level of detail, performance budgets, and atomic patches.
- `ChartForgeX.Interactivity.Html` turns that document into a self-contained explorer with SVG, Canvas, and WebGL rendering. The generated page has no CDN, JavaScript framework, or runtime package dependency.
- `TopologyChart` remains the deterministic topology owner for static SVG, PNG, HTML, reports, maps, and scenarios. `ToGraphScene(...)` is the optional bridge when a topology needs large interactive exploration.

This keeps report and email output deterministic while letting browser, desktop, or native adapters reuse the same graph contract.

## Build a hierarchy

```csharp
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

var graph = GraphScene.Create("service-estate", "Service estate")
    .AddNode("estate", "Global estate", node => {
        node.Kind = "platform";
        node.BadgeText = "42";
        node.SecondaryLabel = "9 sites";
        node.Shape = GraphNodeShape.Image;
        node.ImageUrl = "data:image/svg+xml,...";
    })
    .AddNode("europe", "Europe", node => {
        node.ParentId = "estate";
        node.Kind = "region";
        node.Status = "warning";
        node.BadgeText = "18";
        node.SecondaryLabel = "4 sites · 18 workloads";
    })
    .AddNode("identity", "Identity plane", node => {
        node.ParentId = "europe";
        node.Kind = "identity";
        node.Status = "healthy";
    })
    .AddEdge("estate-europe", "estate", "europe", configure: edge => {
        edge.Kind = "hierarchy";
        edge.Directed = true;
        edge.LayoutDirected = true;
    })
    .AddEdge("europe-identity", "europe", "identity", configure: edge => {
        edge.Kind = "hierarchy";
        edge.Directed = true;
        edge.LayoutDirected = true;
    });

graph.Options.UseSuperTopologyDefaults();
graph.Options.Layout.Mode = GraphLayoutMode.Hierarchical;
graph.Options.Layout.Direction = GraphLayoutDirection.LeftToRight;
graph.Options.Hierarchy.InitialRootNodeId = "estate";
graph.Options.Hierarchy.InitialDepth = 1;

var html = graph.ToGraphExplorerHtmlPage(options => {
    options.PageTitle = "Service Estate";
    options.RenderBackend = HtmlGraphRenderBackend.Svg;
});
File.WriteAllText("service-estate.html", html);
```

Double-clicking a node with children enters that node. Double-clicking empty graph space returns one level; `Escape`, `Backspace`, and Left Arrow do the same from the graph. Every breadcrumb segment is clickable, while `Overview` and `Up` remain explicit accessible controls. Search Enter selects and centers the first match. Semantic zoom reduces labels in overview mode and reveals secondary labels and badges in detail mode.

`ParentId` is a real validated contract: missing parents, self-parenting, and cycles fail before rendering. `ParentClusterId` provides the same structural relationship for nested cluster descriptions.

## Premium themes and accessibility

The HTML adapter owns one responsive explorer shell rather than leaving each generated page to invent its own fields, graph controls, physics panel, breadcrumbs, focus treatment, and minimap. Search, filters, and appearance live in the discovery header. Hierarchy and graph commands float inside the stage, where icon-only actions have accessible names and custom tooltips; export formats open as a labeled popover instead of three unexplained glyphs. The shell and all three rendering backends consume the same theme tokens.

`System` is the default. It follows `prefers-color-scheme` until the reader chooses Light or Dark, and the choice is reused on the same origin by default:

```csharp
var html = graph.ToGraphExplorerHtmlPage(options => {
    options.Theme = HtmlGraphExplorerTheme.System;
    options.IncludeThemeToggle = true;
    options.PersistThemePreference = true;
});
```

Use `Light` or `Dark` for a fixed initial mode. Set `IncludeThemeToggle = false` when a host owns appearance, and set `PersistThemePreference = false` for isolated report pages that should not reuse a browser choice. Hosts can change or inspect the current mode without rebuilding the page:

```javascript
ChartForgeXGraphExplorer.theme("service-estate", "dark");
const appearance = ChartForgeXGraphExplorer.theme("service-estate");
// { mode: "dark", active: "dark" }
```

Every change raises `cfxgraphthemechange` with `mode` and resolved `active` colors. Scoped CSS custom properties such as `--cfx-color-accent`, `--cfx-color-surface-raised`, `--cfx-color-paper`, and `--cfx-color-focus` are the supported host override seam. Canvas, WebGL, the minimap, SVG, and exported SVG all resolve through the same palette. Explicit model label colors remain when they provide at least 4.5:1 contrast against the graph paper; otherwise the adapter uses the active theme text color.

Accessibility behavior is part of the adapter contract:

- Controls have explicit names, logical groups, visible text where it helps orientation, and a high-visibility focus ring.
- SVG uses one roving tab stop. Arrow keys move spatially between visible graph items; Home and End jump to the bounds; Enter or Space select. This keeps large graphs out of the normal page tab sequence.
- Canvas and WebGL expose one labeled keyboard surface. Arrow keys move the current node selection and announce it.
- Search results, selection, hierarchy, physics, and appearance state use polite live regions without replacing visible status.
- `prefers-reduced-motion: reduce` suppresses intermediate stabilization frames, connected-node bounce during dragging, and release momentum while retaining deliberate drag and final layout results.
- `forced-colors: active` and `prefers-contrast: more` replace decorative surfaces with system colors and strengthen structural borders. Status and selection remain distinguishable by stroke, text, or state—not color alone.
- At narrow widths, search and named filters remain in the header while the graph command rail becomes a horizontally scrollable bottom dock. The breadcrumb stays inside the stage and the theme action keeps its accessible name.

These defaults target [WCAG 2.2 contrast](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html), [focus visibility](https://www.w3.org/WAI/WCAG22/Understanding/focus-appearance.html), and [target size](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html). Accessibility still depends on callers supplying useful node labels, image alternative text, and status wording.

## Large topology pipeline

Small scenes remain SVG-first because SVG provides rich shapes, images, labels, keyboard targets, and inspectable metadata. As scenes cross their configured thresholds, the same document moves through Canvas and WebGL without changing the C# model.

The large-scene path has four layers:

1. C# computes deterministic initial positions and serializes a compact graph document.
2. The browser creates lightweight runtime items rather than thousands of hidden SVG marks.
3. WebGL2 draws edges and nodes in batched buffers; Canvas remains the acceleration fallback when WebGL2 is unavailable.
4. SVG export materializes complete node and edge artwork on demand, so fast startup does not remove vector export.

The compact document is activated only after a level-of-detail threshold is crossed. Normal SVG scenes retain their complete markup. Large pages therefore avoid the hidden-SVG DOM cost while keeping search, filters, selection, neighborhood focus, cluster state, hit testing, JSON export, and incremental updates.

`UseSuperTopologyDefaults()` enables a practical starting policy. Every threshold remains configurable:

```csharp
graph.Options.UseSuperTopologyDefaults();
graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 500;
graph.Options.LevelOfDetail.WebGlPreferredNodeThreshold = 2500;
graph.Options.LevelOfDetail.OverviewScaleThreshold = 0.62;
graph.Options.LevelOfDetail.DetailScaleThreshold = 1.12;
graph.Options.Performance.MaxInteractiveWebGlNodes = 30000;
graph.Options.Performance.MaxInteractiveWebGlEdges = 80000;
```

WebGL currently renders topology as status-aware lines and points. Use SVG for small image-rich views and WebGL for dense overview exploration; hierarchy navigation lets one application move naturally between those levels.

## Runtime physics and dragging

Runtime physics is adapter behavior over the same host-neutral graph document. The HTML adapter implements four distinct solver families:

- `BarnesHut` uses quadtree-accelerated inverse-square repulsion for large general graphs;
- `ForceAtlas2` uses degree-weighted, inverse-linear repulsion for relationship and community graphs;
- `Repulsion` uses direct distance-limited repulsion for smaller scenes;
- `HierarchicalRepulsion` adds level-aware separation for directed layered graphs.

Each solver owns its meaningful settings instead of sharing ambiguous flat knobs. Common velocity, timestep, and stabilization settings remain on `GraphPhysicsOptions`:

```csharp
graph.Options.Physics.Solver = GraphPhysicsSolver.ForceAtlas2;
graph.Options.Physics.MinVelocity = 0.1;
graph.Options.Physics.MaxVelocity = 50;
graph.Options.Physics.Timestep = 0.5;
graph.Options.Physics.AdaptiveTimestep = true;
graph.Options.Physics.Stabilization.Enabled = true;
graph.Options.Physics.Stabilization.Iterations = 700;

graph.Options.Physics.ForceAtlas2.Theta = 0.5;
graph.Options.Physics.ForceAtlas2.GravitationalConstant = -72;
graph.Options.Physics.ForceAtlas2.CentralGravity = 0.01;
graph.Options.Physics.ForceAtlas2.SpringLength = 92;
graph.Options.Physics.ForceAtlas2.SpringConstant = 0.08;
graph.Options.Physics.ForceAtlas2.Damping = 0.4;
graph.Options.Physics.ForceAtlas2.AvoidOverlap = 0.82;
```

Scenes with at least 160 active nodes move physics to a browser worker automatically when the platform supports it. SVG, Canvas, and WebGL consume the same live coordinates, so changing rendering backends does not change drag semantics. Hierarchy navigation, cluster changes, and runtime graph patches rebuild and reheat the active simulation by default.

`ReleaseAndReheat` is the default drag policy. The held node is fixed only while the pointer is down, connected movable nodes keep responding, recent pointer momentum is transferred on release, and the node's original fixed state is restored. Use `PinOnDrop` when the dropped position should become a new fixed anchor:

```csharp
graph.Options.Interaction.NodeDragBehavior = GraphNodeDragBehavior.ReleaseAndReheat;
graph.Options.Interaction.SimulateConnectedNodesWhileDragging = true;
graph.Options.Interaction.DragMomentum = 0.18;
graph.Options.Interaction.ReheatAfterDrag = true;
graph.Options.Interaction.ReheatAfterClusterChange = true;
graph.Options.Interaction.ReheatAfterHierarchyChange = true;
graph.Options.Interaction.ReheatAfterGraphChange = true;
```

The premium configurator is intentionally opt-in. It is useful while tuning an example or host preset, but normal generated reports do not need the extra controls:

```csharp
var html = graph.ToGraphExplorerHtmlPage(options => {
    options.IncludePhysicsConfigurator = true;
});
```

It switches solvers, enables only the applicable fields, updates the document attributes, and reheats the scene. Hosts can perform the same operation without the UI:

```javascript
ChartForgeXGraphExplorer.physics("service-estate", {
  solver: "BarnesHut",
  springLength: 86,
  gravitationalConstant: -2400,
  avoidOverlap: 0.75
});
```

Static `ToGraphSvg`, `ToGraphPng`, and stage-image exports never execute browser physics. They remain deterministic, script-free renders of the prepared document or requested hierarchy stage.

### Migrating flat physics settings

The solver-specific model intentionally replaces the former flat API; no compatibility layer keeps ambiguous settings alive:

| Former setting | Current setting |
| --- | --- |
| `StabilizationIterations` | `Stabilization.Iterations` |
| `LinkDistance` | The active solver's `SpringLength` |
| `Damping` | The active solver's `Damping` |
| `CenterGravity` | The active solver's `CentralGravity` |
| `Repulsion` | `BarnesHut.GravitationalConstant`, `ForceAtlas2.GravitationalConstant`, or `Repulsion.Strength`, depending on the solver |
| `GraphPhysicsSolver.ForceDirected` | `GraphPhysicsSolver.Repulsion` or one of the accelerated named solvers |

`VisNetworkPhysicsOptions` remains the typed migration envelope for vis-network-style input. Its flat values are mapped into the selected ChartForgeX solver profile during conversion.

## Save overview-to-detail images

Interactive HTML is optional. The same hierarchy can be planned once and saved as deterministic, script-free SVG and PNG stages from C#:

```csharp
var files = graph.SaveGraphStageImages("report-assets", "estate", options => {
    // Save1 = overview, then levels 1, 2, 3, and 5/full.
    options.Stages.Depths.AddRange(new[] { 0, 1, 2, 3, 5 });
    options.Formats = GraphSceneStaticImageFormat.Both;
    options.Render.Width = 1600;
    options.Render.Height = 900;
    options.Render.MaximumNodeLabels = 160;
});
```

Files use stable names such as `estate-01-overview.png`, `estate-02-depth-1.png`, and `estate-05-full.png`. A frontier node receives a `+N` badge and `N hidden` secondary label, so a static overview still communicates what was collapsed. The deepest requested level is clamped to the scene depth and the complete view is included by default.

For one image, use `graph.ToGraphSvg(stage)` or `graph.ToGraphPng(stage)`. These paths do not open a browser and do not add scripts. PNG export preserves self-contained PNG, JPEG, or SVG data-URI node images. Static output limits labels deterministically at large scale—frontier summaries, roots, and high-degree nodes win—because rendering 2,000 labels into one non-interactive frame would not be readable. Set `MaximumNodeLabels` explicitly for the target medium.

## Clustering

`GraphClusterMode` supports:

- `Explicit` for caller-authored clusters;
- `ByGroup` for deterministic group-derived summaries;
- `Adaptive` for deterministic, bounded structural communities;
- `Hybrid` for declared clusters first and structural summaries when needed.

Adaptive cluster ids and membership are stable for the same graph document. Clusters can collapse globally from the toolbar or individually by double-clicking a summary. Their current state is included in JSON export and host events.

## Incremental updates

The C# document supports validated atomic patches:

```csharp
var patch = new GraphScenePatch();
patch.RemoveNodeIds.Add("old-api");
patch.UpsertNodes.Add(new GraphSceneNode {
    Id = "queue",
    Label = "Queue",
    ParentId = "platform",
    Status = "warning"
});
patch.UpsertEdges.Add(new GraphSceneEdge {
    Id = "platform-queue",
    SourceNodeId = "platform",
    TargetNodeId = "queue",
    Directed = true
});

var result = graph.ApplyPatch(patch);
```

Invalid references roll the complete patch back. Node removal cleans incident edges and cluster membership by default.

Generated pages expose the same operation through a small host API when `IncrementalUpdates` is enabled:

```javascript
const graph = window.ChartForgeXGraphExplorer;

graph.update("service-estate", {
  upsertNodes: [
    { id: "queue", label: "Queue", parentId: "platform", status: "warning", x: 420, y: 180 }
  ],
  upsertEdges: [
    { id: "platform-queue", sourceNodeId: "platform", targetNodeId: "queue", directed: true }
  ],
  fit: true
});

graph.navigate("service-estate", "platform", 2);
graph.focus("service-estate", "queue");
graph.physics("service-estate", { solver: "ForceAtlas2", damping: 0.42 });
graph.theme("service-estate", "dark");
graph.fit("service-estate");
const snapshot = graph.get("service-estate");
```

The API also exposes `export(target, "svg" | "png" | "json")`. Export remains interceptable through the cancelable `cfxgraphexport` event.

## Interaction and host events

The explorer includes search, status/kind filters, selection, Ctrl/Meta/Shift multi-selection, keyboard selection, neighborhood focus, live force-aware node dragging, pan, wheel and command-rail zoom, fit, cluster controls, direct hierarchy navigation, semantic zoom, minimap navigation, runtime physics, and labeled SVG/PNG/JSON export choices.

Stable events include `cfxgraphready`, `cfxgraphselect`, `cfxgraphselection`, `cfxgraphfilter`, `cfxgraphfocus`, `cfxgraphnavigate`, `cfxgraphcluster`, `cfxgraphpatch`, `cfxgraphdragstart`, `cfxgraphdrag`, `cfxgraphdragend`, `cfxgraphphysicschange`, `cfxgraphthemechange`, `cfxgraphviewport`, `cfxgraphexport`, `cfxgraphstabilized`, `cfxgraphlod`, and `cfxgraphperformance`.

Performance telemetry deliberately separates renderer work from browser cadence. `performance.budgetMisses`, `budgetMissRate`, and `maxRenderMs` measure ChartForgeX render work against the configured frame budget. `cadenceBudgetMisses`, `cadenceBudgetMissRate`, and `maxFrameMs` report delayed animation-frame delivery, which can also include browser scheduling, background throttling, capture tooling, or unrelated page work. Use the first group as the ChartForgeX release gate; use cadence as a diagnostic signal instead of attributing every late browser callback to the renderer.

The same values are available through `ChartForgeXGraph.get(id).performance` and root attributes such as `data-cfx-graph-performance-budget-misses` and `data-cfx-graph-performance-cadence-budget-misses`.

Exactly one rendering surface is exposed to assistive technology. SVG uses a single roving graph-item tab stop; the labeled Canvas or WebGL surface becomes the keyboard target in accelerated modes.

## Scale review fixtures

Generate the normal premium examples:

```powershell
dotnet run --project .\ChartForgeX.Examples\ChartForgeX.Examples.csproj -c Release -- --graph-explorer-only
```

Generate the explicit browser scale fixtures without burdening every normal gallery build:

```powershell
dotnet run --project .\ChartForgeX.Examples\ChartForgeX.Examples.csproj -c Release -- --graph-scale-only
```

The latter writes `graph-scale-1000.html`, `graph-scale-5000.html`, and `graph-scale-10000.html`, each with two edges per node and fixed positions for repeatable startup and interaction review.

The July 13, 2026 local Chrome release-gate run on the maintainer workstation produced the following observations. These are evidence for that environment, not universal performance guarantees:

| Fixture | Live DOM elements | Full load | Fit call | Active renderer |
| --- | ---: | ---: | ---: | --- |
| 1,000 nodes / 2,000 edges | 82 | 0.17 s | — | WebGL2 |
| 5,000 nodes / 10,000 edges | 202 | 0.64 s | 75 ms | WebGL2 |
| 10,000 nodes / 20,000 edges | 352 | 1.26 s | 143 ms | WebGL2 |

The 10k browser run also applied a node-and-edge patch, searched the result to one match, and selected the new item without console errors. The 1k compact page synthesized a complete 1,000-node/2,000-edge SVG export in about 49 ms.

## Generated review pages

- `global-estate-premium-topology.html` demonstrates image nodes, badges, secondary labels, status overlays, nested clusters, and three-level drill navigation.
- `graph-2000-interactive.html` and `graph-2000-stage-01-overview.*` through `graph-2000-stage-05-full.*` demonstrate one 2,001-object document as a WebGL/Barnes-Hut explorer and five script-free report stages.
- `identity-risk-graph-explorer.html` demonstrates a product-shaped relationship graph with images, filters, selection, focus, clusters, live ForceAtlas2 dragging, and the opt-in physics configurator.
- `enterprise-access-graph-benchmark.html` demonstrates accelerated compact-document rendering with 360 nodes and 720 directed edges.
- `vis-network-parity-hierarchy.html` demonstrates the typed vis-style compatibility surface.
