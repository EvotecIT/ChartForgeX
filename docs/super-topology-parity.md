# Super Topology Parity Roadmap

ChartForgeX should cover the useful vis-network-style graph exploration surface while staying better suited to C# report, documentation, email, Office, static-site, and server-side use cases.

This document is a roadmap, not a completion claim. The current implementation establishes the shared `GraphScene` contract, topology bridge, effective clusters, vis-network-style compatibility conversion, deterministic hierarchical layout, export metadata, and smoke/visual proof. It is not yet full vis-network parity.

## Architecture

Use two models deliberately:

- `TopologyChart` remains the deterministic static/export model for SVG, PNG, HTML, reports, topology maps, scenarios, route animation, and metadata-rich snapshots.
- `GraphScene` remains the host-neutral large-graph exploration contract for SVG, Canvas, WebGL, desktop, or native adapters.

The bridge from `TopologyChart` to `GraphScene` lives in the optional interactivity adapter layer so the core `ChartForgeX` package stays script-free and dependency-free by default.

## Parity Ledger

Status key: `Landed` means implemented and validated in ChartForgeX; `Partial` means useful support exists but important vis-network-equivalent behavior is still missing; `Missing` means the public capability is not present yet.

| Area | Status | Current ChartForgeX shape | Parity target |
| --- | --- | --- | --- |
| Data model | Partial | `GraphScene` nodes, edges, clusters, metadata, physics, LOD, performance budgets, and typed `VisNetworkGraph` conversion | Preserve topology ids and metadata while supporting broader vis-style option import/export, graph-native snapshots, and persisted positions. |
| Topology bridge | Landed | `TopologyChart.ToGraphScene(...)` and graph explorer HTML helpers in `ChartForgeX.Interactivity.Html` | Keep topology as source of truth and avoid a second topology DTO. |
| Nodes | Partial | Circle, box, image hints, icon text, kind, status, group, cluster, hierarchy level, fixed positions, common vis-style shapes, colors, label backgrounds, and shadows | Broaden icon artwork, badges, selected/hover styling, and status overlays without making static topology depend on browser behavior. |
| Edges | Partial | Directed, curved, dashed, weighted, labels, metadata, color, width, hidden and physics flags, and common smoothing metadata | Add richer bidirectional arrow placement, middle arrows, self-reference polish, edge scaling, selection styling, and relationship-specific styling. |
| Groups and clustering | Partial | Explicit clusters, group-derived effective clusters, and hybrid/adaptive cluster policy | Cluster by topology group, kind, hub, health, viewport scale, graph community, or host predicate; open clusters; persist clustered state. |
| Layout and physics | Partial | Prepared layout, deterministic hierarchical layout, runtime physics, Barnes-Hut presets, stabilization | Add random seeds, improved initial layout, broader solver parity, configure UI/export, OffscreenCanvas hardening, WebGL rendering, adaptive LOD, and CI performance budgets. |
| Interaction | Partial | Selection, multi-selection, focus, search, filters, drag, viewport, export, telemetry | Add hover/chosen states, lasso/box selection, richer keyboard navigation, navigation buttons, persisted interaction-state import, group transforms, and undo/redo. |
| Manipulation | Missing | Host-neutral opt-in manipulation capabilities | Add adapter UI for add/edit/delete nodes and edges, group dragging, position persistence, and host validation callbacks. |
| Events and host API | Partial | Graph events for selection, focus, viewport, export, LOD, cluster, drag, performance | Match the useful vis-network event surface for click, double-click, hover, blur, drag, zoom, stabilization, render lifecycle, and export interception. |
| Scale proof | Partial | 360-node benchmark plus 1k-node smoke confidence | Add generated 1k, 5k, and 10k object fixtures with browser-visible performance budgets and screenshot/pixel proof. |
| C# advantage | Partial | Static SVG/PNG/HTML, AOT, deterministic report output, topology scenarios, animated route exports | Keep every interactive state exportable as a deterministic C# artifact for reports, docs, email, and Office-style hosts. |

## Parity Gaps To Close First

1. Extend the typed vis-style compatibility layer with more option families and import/export helpers so C# users can migrate common examples without learning a second model first.
2. Implement manipulation UI in the HTML adapter behind the existing opt-in `GraphManipulationOptions` contract, with host callbacks and JSON patch export instead of silent mutation.
3. Add richer node and edge styling: selected/hover styles, arrows from/to/middle, self-reference polish, edge scaling, and relationship-specific styling.
4. Extend clustering beyond current group-derived summaries with hub/outlier clustering, zoom-driven clustering, open-cluster behavior, and saved cluster state import.
5. Add browser-visible scale baselines for 1k, 5k, and 10k graphs across SVG, Canvas, and future WebGL, including screenshot/pixel checks and performance telemetry thresholds.

## Implementation Route

1. Keep `TopologyChart -> GraphScene` projection thin and metadata-preserving.
2. Grow clustering as a reusable `GraphScene` policy before adding adapter-specific behavior; adapters should render effective clusters from `GetEffectiveClusters()` instead of reading only explicit declarations.
3. Keep manipulation opt-in and adapter-owned; static topology output stays immutable unless the host persists edited state.
4. Add WebGL and OffscreenCanvas as backends over the same `GraphScene`, not as new models.
5. Make examples product-real: identity risk, service dependencies, replication mesh, geography plus WAN, evidence graphs, and imported icon/stencil topologies.
6. Validate every parity claim through public API, generated HTML/SVG/PNG artifacts, smoke tests, and performance evidence.
