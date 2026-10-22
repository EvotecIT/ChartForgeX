---
title: "Author visuals in VS Code"
order: 4
description: "Install ChartForgeX Markup for VS Code, preview Markdown visuals and export SVG, PNG or HTML."
layout: docs
---

The ChartForgeX Markup extension adds syntax highlighting, source-line diagnostics, preview and export commands to Markdown visual blocks. Packaged installs include the rendering CLI, so a separate global tool installation is optional.

## Install

Install [ChartForgeX Markup from the Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=EvotecServices.chartforgex-markup), or run:

```powershell
code --install-extension EvotecServices.chartforgex-markup
```

## Preview a document

Create `trend.cfx.md` and add a native block:

````markdown
```chartforgex chart v1 {#trend title="Trend" type="line"}
labels Jan Feb Mar
values 12 18 16
```
````

Open the command palette and run **ChartForgeX Markup: Open Preview**. The preview can refresh as you edit. **ChartForgeX Markup: Validate** reports diagnostics against the original source lines.

The extension recognizes `.cfx.md`, `.chartforgex.md` and Markdown files containing native visual fences. Supported Mermaid fences use the Mermaid adapter included with the packaged CLI; consult the [Mermaid catalog](https://chartforgex.com/mermaid/) for the supported syntax rather than assuming every Mermaid construct is accepted.

## Export or move into application code

Run **ChartForgeX Markup: Export SVG**, **Export PNG** or **Export HTML** from the command palette. **Open Output Folder** opens the destination. SVG and PNG require one visual artifact; use HTML when a file contains several.

**Generate C#** and **Generate C# File** convert native topology documents into builder code. Other chart and Mermaid families are not code-generation targets.

## Configure the authoring workflow

Use VS Code settings to choose automatic preview refresh, validation delay, export format and output folder. Set `chartforgexMarkup.ganttDateAnchor` when a Mermaid Gantt document uses yearless dates. `chartforgexMarkup.cliPath` selects a custom CLI when the bundled tool is not the desired host.

Continue with the [markup guide](../markup/), [v1 reference](../markup-v1-reference/) or [CLI commands](../cli/).
