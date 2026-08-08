---
title: "Topology and hierarchy"
description: "Render product-neutral service maps, ownership trees, routes, and dense hierarchies."
layout: docs
---

`TopologyChart` owns validated nodes, edges, groups, layout, legends, routes, static HTML, SVG, and raster output. Map application or directory records into stable ids and labels; keep collection and domain rules in the host.

Available layout modes include layered, hub-and-spoke, matrix, dense grouped, geographic, group grid, and manual placement. Hierarchy branches can inherit or override `Auto`, `Standard`, `Compact`, or `Vertical` layout policy so a dense subtree can compact without changing the rest of the organization chart.

Use `TopologyChart.FromData<TNode, TEdge>(...)` when the host already owns its records. The mapper preserves input order and rejects duplicate ids or dangling endpoints before rendering.

For thousands of relationships, hierarchy drill-down, editing, or runtime patches, bridge the model to `GraphScene` and the HTML graph explorer rather than moving browser state into the core renderer.
