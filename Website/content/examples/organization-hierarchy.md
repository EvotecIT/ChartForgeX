---
title: "Organization hierarchy with branch layout policies"
meta.seo_title: "Organization hierarchy layouts | ChartForgeX"
order: 3
description: "Mix standard, compact, and vertical branches in one deterministic hierarchy."
layout: example
---

`TopologyChart` draws a static organization view, and hierarchy policy changes the density of one branch without touching the rest. The output above is this program's SVG; choose `--dark` when running the complete program to use the dark theme.

## Describe the people

Each member names its manager; the small `Reports` helper keeps the leaf entries short. A layout policy on a member applies to the branch below it and is inherited, so most of a tree can stay automatic while one team becomes compact or vertical.

{{< cfx-example-code id="organization-hierarchy" parts="people,helpers" >}}

## Pick a policy per branch

- `Auto` lets the renderer select a deterministic layout from branch density.
- `Standard` sets a conventional row of direct reports.
- `Compact` wraps a dense branch.
- `Vertical` stacks a narrow branch in a single column.

## Build the chart

`AddTeam` turns the parent links into nodes and ownership edges. `MaxLevel` bounds how deep the chart goes.

{{< cfx-example-code id="organization-hierarchy" part="chart" >}}

## Save it

SVG and PNG share the same hierarchy geometry, so documentation and raster consumers see the same chart.

{{< cfx-example-code id="organization-hierarchy" part="save" >}}

The maintained [team hierarchy example](https://github.com/EvotecIT/ChartForgeX/blob/main/ChartForgeX.Examples/TopologyVisualExamples.cs) adds a legend, routing metadata and a larger team.
