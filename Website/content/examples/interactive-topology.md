---
title: "Interactive topology explorer"
order: 4
description: "Turn a GraphScene into a self-contained searchable and navigable HTML explorer."
layout: example
---

A `GraphScene` becomes one self-contained HTML file with search, drill-down and a theme toggle. The explorer above is this program's output, running in the page.

## Model the estate

Nodes carry labels, badges, secondary text and a status. `ParentId` builds the hierarchy the explorer drills into.

{{< cfx-example-code id="interactive-topology" part="estate" >}}

## Add the sites

Sites hang under their region. Stable ids keep selection and links valid when the data changes.

{{< cfx-example-code id="interactive-topology" part="sites" >}}

## Lay it out as a hierarchy

The hierarchical layout sets regions under the global node and sites under their region. The explorer opens on the global node with one level of children; readers drill into a region from there.

{{< cfx-example-code id="interactive-topology" part="layout" >}}

## Write one HTML file

The page carries its own script and styles. Switch the backend to Canvas or WebGL for larger scenes while keeping the same model and navigation.

{{< cfx-example-code id="interactive-topology" part="save" >}}

`graph.ToGraphSvg()` writes a script-free SVG of the same scene for documents that cannot run script; set `Theme` in its options for a dark export. The card image on the examples page is that export, in both themes.
