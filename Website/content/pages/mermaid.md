---
title: Mermaid diagrams in .NET
description: Thirty Mermaid diagram families render natively in ChartForgeX. See what is supported, how output differs from Mermaid.js, and how to use it from C#.
slug: mermaid
layout: mermaid
---

## Render Mermaid from C#

```csharp
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

MermaidRenderResult result = MermaidRenderer.Render(source);
if (!result.HasErrors && result.Artifact != null) {
    result.Artifact.SaveSvg("diagram.svg");
    result.Artifact.SavePng("diagram.png");
}
```

Check `result.Diagnostics` even when rendering succeeds. Warnings identify syntax or configuration that was kept without an exact visual interpretation.

## Render Mermaid fences in Markdown

Add `ChartForgeX.Markup.Mermaid` when Markdown documents contain `mermaid` fences. The bridge keeps the dependency boundary clean: `ChartForgeX.Mermaid` owns parsing and conversion, and the markup bridge only adapts fence metadata and diagnostics.

```csharp
using ChartForgeX.Markup.Mermaid;

var result = new MermaidVisualMarkupParser().Parse(markdown);
```

## How rendering differs from Mermaid.js

- Diagrams are laid out and drawn by ChartForgeX, so the output uses your ChartForgeX theme and typography rather than Mermaid's CSS.
- The `default` and `dark` source themes select static palettes. Other themes and settings produce approximation warnings.
- Accessible titles and descriptions from `accTitle` and `accDescr` become the SVG's accessible name and description.
- Remote fonts are never fetched; native font resolution uses the registered fonts and fallbacks.

The [support matrix](https://github.com/EvotecIT/ChartForgeX/blob/main/docs/mermaid-support-matrix.md) lists the status of each family and option.
