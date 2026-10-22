---
title: "Small-multiple report grid"
order: 2
description: "Facet typed records into a deterministic multi-panel chart report with shared axes and pages."
layout: example
---

Typed records become one chart per site, laid out as a grid. The output above is this program's SVG; the selected grid theme applies to every panel. Run the complete program with `--dark` for the dark theme.

## Load typed records

`ChartDataset<T>` keeps your own record type, so the chart reads properties instead of parallel arrays.

{{< cfx-example-code id="report-grid" part="records" >}}

## Facet into a grid

`ChartGrid.FromFacets` groups the records by a key and builds one chart per group with the factory you pass. Groups keep their input order.

{{< cfx-example-code id="report-grid" part="grid" >}}

## Save the report

The grid renders as one image, so a report, an email or a wiki page gets every panel in a single file.

{{< cfx-example-code id="report-grid" part="save" >}}

## Declare the record

Any record or class works; the selectors above read its properties.

{{< cfx-example-code id="report-grid" part="record" >}}

Use `ChartGrid` for chart-only small multiples. Use visual blocks or a visual canvas when the report also needs exact facts, lists, or positioned artwork.
