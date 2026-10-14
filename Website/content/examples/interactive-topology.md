---
title: "Interactive topology explorer"
order: 4
description: "Turn a GraphScene into a self-contained searchable and navigable HTML explorer."
layout: example
---

A `GraphScene` becomes one self-contained HTML file with search, drill-down and a theme toggle. The explorer above is this program's output, running in the page.

## Model the estate

Nodes carry labels, badges, secondary text and a status. `ParentId` builds the hierarchy the explorer drills into.

```csharp
using System.IO;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

var graph = GraphScene.Create("estate", "Global estate")
    .AddNode("global", "Global", node => node.BadgeText = "42")
    .AddNode("europe", "Europe", node => {
        node.ParentId = "global";
        node.SecondaryLabel = "4 sites · 18 workloads";
        node.Status = "warning";
    })
    .AddNode("americas", "Americas", node => {
        node.ParentId = "global";
        node.SecondaryLabel = "3 sites · 14 workloads";
        node.Status = "healthy";
    })
    .AddNode("apac", "Asia Pacific", node => {
        node.ParentId = "global";
        node.SecondaryLabel = "2 sites · 10 workloads";
        node.Status = "healthy";
    })
    .AddEdge("global-europe", "global", "europe", configure: edge => edge.Directed = true)
    .AddEdge("global-americas", "global", "americas", configure: edge => edge.Directed = true)
    .AddEdge("global-apac", "global", "apac", configure: edge => edge.Directed = true)
    .AddEdge("europe-americas", "europe", "americas", "replication", edge => edge.Status = "warning");
```

## Add the sites

Sites hang under their region. Stable ids keep selection and links valid when the data changes.

```csharp
foreach (var (id, label, region) in new[] {
    ("warsaw", "Warsaw", "europe"), ("london", "London", "europe"), ("frankfurt", "Frankfurt", "europe"),
    ("new-york", "New York", "americas"), ("toronto", "Toronto", "americas"),
    ("sydney", "Sydney", "apac"), ("singapore", "Singapore", "apac")
}) {
    graph.AddNode(id, label, node => node.ParentId = region)
        .AddEdge(region + "-" + id, region, id);
}
```

## Start at the top level

The explorer opens on the global node with one level of children; readers drill into a region from there.

```csharp
graph.Options.UseSuperTopologyDefaults();
graph.Options.Hierarchy.InitialRootNodeId = "global";
graph.Options.Hierarchy.InitialDepth = 1;
```

## Write one HTML file

The page carries its own script and styles. Switch the backend to Canvas or WebGL for larger scenes while keeping the same model and navigation.

```csharp
var html = graph.ToGraphExplorerHtmlPage(options => {
    options.RenderBackend = HtmlGraphRenderBackend.Svg;
    options.Theme = HtmlGraphExplorerTheme.System;
    options.IncludeThemeToggle = true;
});

File.WriteAllText("estate.html", html);
```

`graph.ToGraphSvg()` writes a script-free SVG of the same scene for documents that cannot run script.
