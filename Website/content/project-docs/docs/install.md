---
title: "Install ChartForgeX"
order: 2
description: "Install the core ChartForgeX package and add optional adapters only when needed."
layout: docs
---

Use the .NET library when a service, desktop app, report generator or other host owns the data and rendering calls. For Markdown authoring, use the [command-line tool](../cli/) or [VS Code extension](../editor/).

Install the core renderer from NuGet:

```powershell
dotnet add package ChartForgeX
```

{{< cfx-figure id="install" caption="The packages and what each adds, drawn as a ChartForgeX table. Visual blocks set exact facts like these beside charts." >}}

Add an optional package only for the capability the host needs:

```powershell
dotnet add package ChartForgeX.Interactivity.Html
dotnet add package ChartForgeX.Markup
dotnet add package ChartForgeX.Mermaid
dotnet add package ChartForgeX.Markup.Mermaid
```

`ChartForgeX.Mermaid` renders supported Mermaid source directly. `ChartForgeX.Markup.Mermaid` adds Mermaid fences to the Markdown artifact pipeline; the [markup guide](../markup/) explains the host integration. Use `ChartForgeX.Visuals` for [visual blocks](../visual-blocks/) and [canvases](../visual-canvas/).

The core package targets .NET Framework 4.7.2, .NET Standard 2.0, .NET 8, and .NET 10. It has no runtime package dependencies. The browser adapter generates self-contained HTML and does not turn the static renderer into a JavaScript-first dependency.

See the [NuGet package](https://www.nuget.org/packages/ChartForgeX) for the current public version and supported assets.
