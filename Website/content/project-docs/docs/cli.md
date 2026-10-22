---
title: "Use the command-line tool"
order: 3
description: "Validate Markdown visual blocks and export native SVG, PNG or HTML with the ChartForgeX Markup CLI."
layout: docs
---

The ChartForgeX Markup CLI reads Markdown containing native `chartforgex <kind> v1` blocks or supported Mermaid fences. Use it for file-based validation, previews and exports without writing a .NET host.

## Install

Install the .NET 8 SDK, which includes the runtime used by the tool, then install the global tool:

```powershell
dotnet tool install --global ChartForgeX.Markup.Cli
chartforgex-markup --help
```

## Create a visual

Save this as `trend.cfx.md`:

````markdown
```chartforgex chart v1 {#trend title="Trend" type="line"}
labels Jan Feb Mar
values 12 18 16
```
````

Validate the file, create a preview, and export the chart:

```powershell
chartforgex-markup validate trend.cfx.md
chartforgex-markup preview trend.cfx.md --output trend.preview.html
chartforgex-markup export trend.cfx.md --output trend.svg
chartforgex-markup export trend.cfx.md --output trend.png
chartforgex-markup export trend.cfx.md --output trend.html
```

The output extension selects the export format. Open the HTML file in a browser or use the SVG and PNG in a report or document. SVG and PNG exports require exactly one visual artifact in the input; HTML can contain several.

## Mermaid and diagnostics

Use a fenced `mermaid` block in the Markdown file for a supported diagram. The [Mermaid catalog](https://chartforgex.com/mermaid/) shows the implemented families and limits. For yearless Gantt input, pass `--gantt-date-anchor YYYY-MM-DD` to validation, preview and export so each command uses the same calendar context.

Validation reports source locations. Exit code `0` means the input is valid, `2` means invalid input or no supported visual artifacts, and `1` means an invocation or file-access error.

## Generate topology code

For a native topology document, `emit` creates C# builder code:

```powershell
chartforgex-markup emit topology.cfx.md --target csharp --output Topology.cs
```

Code generation supports topology documents. It is not a general converter for every chart or Mermaid family.

Continue with [markup authoring](../markup/), the [v1 grammar reference](../markup-v1-reference/), or [VS Code](../editor/) for the same authoring workflow inside an editor.
