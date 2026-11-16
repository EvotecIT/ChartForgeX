---
title: What's new in ChartForgeX
description: Changes in the next ChartForgeX release, with before and after renders of the same input.
slug: whats-new
layout: page
---

<!-- RELEASE-TODO(version): on release day, change "the next release" in this intro and the heading below to the version, for example "ChartForgeX 4.1.0", and link the GitHub release. -->

These changes are merged and ship together in the next release of all nine ChartForgeX packages. Each pair below renders the same input with ChartForgeX 4.0.2 and with the new release. Earlier versions are listed on [GitHub Releases](https://github.com/EvotecIT/ChartForgeX/releases).

## In the next release

### Topology edges are solid unless something is wrong

An edge's default status is `Unknown`, which used to draw a dashed line. Most diagrams came out dashed, including hosts that map healthy, configured links to `Unknown` to keep them neutral. `Unknown` and `Healthy` edges are now solid. `Warning` and `Critical` edges keep their `8 5` dash and `Disabled` edges their `3 6` dash, and an explicit `LineStyle` or dash pattern still wins. SVG and PNG agree.

### Diagonal neighbours connect with one bend

The obstacle-avoiding router now also offers single-bend routes between nodes that sit diagonally from each other, scored with the same obstacle, caption and label rules as the two-bend routes. In the diagram below, Requests leaves the bottom of Client service and turns once, instead of sharing the arrow point of Lookup. Authored and named ports are unchanged, and readable dense layouts still use their own planner.

### Title-only cards centre their heading

A card without details or a badge now centres its heading and subtitle beside its glyph instead of leaving the lower half empty. Processing service below shows it; organisation charts get the same treatment.

{{< cfx-compare name="topology-edges" w="1057" h="457" alt="a service topology with two regions, four nodes and three labelled edges" caption="One service topology rendered with 4.0.2 and with the new release. Dependency edges are solid, only the authored Lookup edge stays dashed, Requests turns once, and Processing service centres its title." >}}

<!-- RELEASE-TODO(topology-grouping): add a "### Topology grouping" section here with a cfx-compare pair (static/whats-new/topology-grouping-{before,after}-{light,dark}.png) once the grouping PR merges. -->

<!-- RELEASE-TODO(striped-links): add a "### Striped links" section here, with a pair if the change is visual, once the striped links PR merges. -->

<!-- RELEASE-TODO(link-evidence): add a "### Link evidence and chips" section here once the link evidence and chips PR merges. -->

### Heatmap keys

Key captions now sit a few pixels below the colour strip instead of touching it, and middle captions keep a full spacing from the end captions. Cell values can be printed inside the cells with `WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Always)` or data labels; each label picks a contrasting ink for its cell.

<!-- RELEASE-TODO(heatmap-ramps): once HtmlForgeX#384 merges and its token files are copied into ChartForgeX/Themes/Tokens/, add: "Sequential heatmap ramps step evenly in lightness. The middle stops of every theme family were retuned in OKLab, so a 44% cell no longer reads duller than a 60% one." Add a pair from the heatmap gallery family (static/whats-new/heatmap-ramp-{before,after}-{light,dark}.png). -->

### Mermaid pie colours

Pie charts apply `themeVariables.pie1` to `pie12` to their slices and `pieTitleTextColor` to the title. The [Mermaid support matrix](/docs/mermaid-support-matrix/#theme-mapping) now has a theme-mapping section that lists which variables map where and which don't yet.

{{< cfx-compare name="mermaid-pie" dark="false" w="820" h="460" alt="a Mermaid pie chart of pets with three slices" caption="A Mermaid pie that sets pie1 and pie2 in its init block. Dogs and Cats now take those colours; Fish keeps the theme colour." >}}
