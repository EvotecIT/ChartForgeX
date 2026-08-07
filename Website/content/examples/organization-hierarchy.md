---
title: "Organization hierarchy with branch layout policies"
description: "Mix standard, compact, and vertical branches in one deterministic hierarchy."
layout: docs
---

Use `TopologyChart` for a static organization view and apply hierarchy policy where a branch needs a different density. Policies inherit, so most of a tree can remain automatic while one team becomes compact or vertical.

The maintained [team hierarchy example](https://github.com/EvotecIT/ChartForgeX/blob/main/ChartForgeX.Examples/TopologyVisualExamples.cs) shows the complete typed model, branch-level policies, SVG/PNG output, and routing metadata.

Choose:

- `Auto` to let the renderer select a deterministic layout from branch density
- `Standard` for a conventional row of direct reports
- `Compact` for a dense wrapped branch
- `Vertical` for a narrow single-column branch

Render the same model to SVG and PNG so documentation and raster consumers preserve the same hierarchy geometry.
