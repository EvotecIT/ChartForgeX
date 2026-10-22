---
title: "Topology and hierarchy"
order: 4
description: "Render product-neutral service maps, ownership trees, routes, and dense hierarchies."
layout: docs
---

`TopologyChart` owns validated nodes, edges, groups, layout, legends, routes, static HTML, SVG, and raster output. Map application or directory records into stable ids and labels; keep collection and domain rules in the host.

{{< cfx-figure id="topology" caption="A layered service map with groups, status and labelled links, rendered from ChartForgeX markup." >}}

Available layout modes include layered, hub-and-spoke, matrix, dense grouped, geographic, group grid, and manual placement. Hierarchy branches can inherit or override `Auto`, `Standard`, `Compact`, or `Vertical` layout policy so a dense subtree can compact without changing the rest of the organization chart.

{{< cfx-figure id="hierarchy" caption="One team tree with standard, compact and vertical branches. The code is in the organization hierarchy example." >}}

Use `TopologyChart.FromData<TNode, TEdge>(...)` when the host already owns its records. The mapper preserves input order and rejects duplicate ids or dangling endpoints before rendering.

For large relationship graphs, editing, or runtime graph patches, bridge the model to `GraphScene` and the HTML graph explorer rather than moving browser state into the core renderer.

## Treemap and Sunburst drill-down

Area hierarchies use their native layout. Call `chart.PrepareHierarchy(context, new ChartHierarchyView("team-id"))` to make an existing item the visible root, or pass null to restore the complete hierarchy. The source items, IDs, authored parents, size/color values and paint overrides remain unchanged. Treemap uses its configured group padding and gap; Sunburst rebases ring depth while preserving authored parent remainders.

For a connected HTML host, use `HtmlInteractiveChartRenderer.PrepareHierarchySection` and enable `ChartInteractionFeatures.HierarchyNavigation`. Group activation, **Up one level** and **Full hierarchy** request native re-preparation through `connectNative(callback, { basis, axes: null, hierarchy: true })`. The callback owns transport and returns the generated section plus a new opaque basis. Bookmarks and SVG/PNG exports follow the accepted view; rejected or stale replies leave it unchanged. The [interactivity reference](../interactivity/) describes the request and lifetime contract.

Use the [topology icon-pack guide](../topology-icon-packs/) for reusable stencils and the [graph explorer reference](../graph-explorer/) for large interactive scenes.
