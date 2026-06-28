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
