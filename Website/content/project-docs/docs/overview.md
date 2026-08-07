---
title: "ChartForgeX overview"
description: "Understand where ChartForgeX fits and what its deterministic rendering model provides."
layout: docs
---

Use ChartForgeX when a .NET application, report generator, build process, or automation host needs polished visuals without sending data to a browser chart service. The same typed model can produce SVG, static HTML, PNG, GIF, JPEG, BMP, PPM, or TIFF output.

## Good fit

- charts and small-multiple report grids
- KPI cards, tables, lists, timelines, and structured visual blocks
- topology, organization hierarchies, maps, and relationship diagrams
- fixed-size wallpapers, report covers, and social-preview canvases
- self-contained interactive chart or graph pages through an optional adapter
- trimmed and Native AOT applications on supported modern .NET targets

## Rendering boundary

The core `ChartForgeX` package stays deterministic, script-free, and free of runtime package dependencies. Host-neutral interaction contracts live in `ChartForgeX.Interactivity`; browser behavior lives in `ChartForgeX.Interactivity.Html`. Product-specific data collection, filters, dashboards, and policy remain in the consuming application.

## Related project pages

- [Installation](../install/)
- [Choose a rendering surface](../choosing-a-surface/)
- [Topology and hierarchy](../topology-and-hierarchy/)
- [Interactive HTML](../interactivity/)
- [.NET API](/projects/chartforgex/api/)
- [Examples](/projects/chartforgex/examples/)
