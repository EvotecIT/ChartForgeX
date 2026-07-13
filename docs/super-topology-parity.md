# Super Topology Parity Reference

ChartForgeX should cover the useful vis-network-style graph exploration surface while staying better suited to C# report, documentation, email, Office, static-site, and server-side use cases.

This document records durable architecture and capability-state reference material. Active follow-up work belongs in `TODO.md`.

## Architecture

Use two models deliberately:

- `TopologyChart` remains the deterministic static/export model for SVG, PNG, HTML, reports, topology maps, scenarios, route animation, and metadata-rich snapshots.
- `GraphScene` remains the host-neutral large-graph exploration contract for SVG, Canvas, WebGL, desktop, or native adapters.

The bridge from `TopologyChart` to `GraphScene` lives in the optional interactivity adapter layer so the core `ChartForgeX` package stays script-free and dependency-free by default.

## Parity Ledger

Status key: `Landed` means implemented and validated in ChartForgeX; `Partial` means useful support exists but important vis-network-equivalent behavior is still missing; `Missing` means the public capability is not present yet.

| Area | Status | Current ChartForgeX shape | Parity target |
| --- | --- | --- | --- |
| Data model | Partial | `GraphScene` nodes, edges, clusters, parent hierarchy, metadata, physics, LOD, performance budgets, atomic `GraphScenePatch`, and typed `VisNetworkGraph` conversion | Broaden vis-style option import/export and add persisted interaction-state import. |
| Topology bridge | Landed | `TopologyChart.ToGraphScene(...)` and graph explorer HTML helpers in `ChartForgeX.Interactivity.Html` | Keep topology as source of truth and avoid a second topology DTO. |
| Nodes | Partial | Common vis-style shapes, image and resolved topology-icon artwork, icon text, secondary labels, badges, status overlays, groups, clusters, hierarchy, colors, label backgrounds, shadows, and fixed positions | Broaden chosen/hover customization and relationship-aware styling without making static topology depend on browser behavior. |
| Edges | Partial | Directed, curved, dashed, weighted, labels, metadata, color, width, hidden and physics flags, and common smoothing metadata | Add richer bidirectional arrow placement, middle arrows, self-reference polish, edge scaling, selection styling, and relationship-specific styling. |
| Groups and clustering | Partial | Explicit, group-derived, deterministic adaptive structural, and hybrid clusters with individual or global open/close behavior | Add hub/outlier, health, viewport-scale, and host-predicate clustering plus persisted cluster-state import. |
| Layout and physics | Partial | Prepared deterministic layouts plus distinct Barnes-Hut, ForceAtlas2, direct repulsion, and hierarchical-repulsion solvers; adaptive timesteps; worker stabilization; live drag/release/reheat; opt-in configurator; SVG/Canvas/WebGL parity; compact documents; and semantic LOD | Add explicit random seeds, persisted physics/configurator state, OffscreenCanvas rendering hardening, and automated CI performance budgets. |
| Interaction | Partial | Selection, multi-selection, focus, search, filters, live connected-node drag response, pin-on-drop or release-and-reheat policies, viewport, direct hierarchy drill/breadcrumbs, semantic zoom, minimap, export, telemetry, and runtime patches | Add lasso/box selection, richer keyboard graph traversal, persisted interaction-state import, group transforms, and undo/redo. |
| Manipulation | Missing | Host-neutral opt-in manipulation capabilities | Add adapter UI for add/edit/delete nodes and edges, group dragging, position persistence, and host validation callbacks. |
| Events and host API | Partial | Selection, focus, viewport, export, LOD, cluster, hierarchy navigation, patch, drag, physics-change, and performance events plus `get`, `update`, `navigate`, `focus`, `physics`, `fit`, and `export` browser methods | Broaden hover/blur and render-lifecycle events while keeping the API small and document-oriented. |
| Scale proof | Landed | Generated 1k/5k/10k WebGL fixtures, compact runtime documents, real-Chrome startup/interaction evidence, and on-demand complete SVG export | Turn the documented release-review measurements into stable cross-platform CI budgets without treating one workstation as a universal guarantee. |
| C# advantage | Partial | Static SVG/PNG/HTML, AOT, deterministic report output, topology scenarios, animated route exports | Keep every interactive state exportable as a deterministic C# artifact for reports, docs, email, and Office-style hosts. |
