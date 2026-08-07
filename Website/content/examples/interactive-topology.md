---
title: "Interactive topology explorer"
description: "Turn a GraphScene into a self-contained searchable and navigable HTML explorer."
layout: docs
---

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
    .AddEdge("global-europe", "global", "europe",
        configure: edge => edge.Directed = true);

graph.Options.UseSuperTopologyDefaults();
graph.Options.Hierarchy.InitialRootNodeId = "global";
graph.Options.Hierarchy.InitialDepth = 1;

var html = graph.ToGraphExplorerHtmlPage(options => {
    options.RenderBackend = HtmlGraphRenderBackend.Svg;
    options.Theme = HtmlGraphExplorerTheme.System;
    options.IncludeThemeToggle = true;
});

File.WriteAllText("estate.html", html);
```

The generated page is self-contained. Switch to Canvas or WebGL for larger scenes while keeping the same model and navigation semantics.
