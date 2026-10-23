---
title: Mermaid diagrams in .NET
description: Render supported Mermaid diagrams natively in .NET. Explore the source, native output, documented grammar and layout limits, and C# integration.
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
- The `default` and `dark` themes select native static palettes. Supported color and font variables configure native presentation roles; other theme settings retain diagnostics.
- Accessible titles and descriptions from `accTitle` and `accDescr` become the SVG's accessible name and description.
- Remote fonts are never fetched; native font resolution uses the registered fonts and fallbacks.

The [support matrix](/docs/mermaid-support-matrix/) lists each native model, supported grammar and remaining limitation. Journey retains task scores and actors; Timeline retains categorical periods without inventing dates. C4 preserves real hierarchy, and quantitative Venn fits report their residual limits. Railroad frontends share one native grammar scene, while Agentflow and ZenUML source remains inert diagram data.
