---
title: "Install ChartForgeX"
description: "Install the core ChartForgeX package and add optional adapters only when needed."
layout: docs
---

Install the core renderer from NuGet:

```powershell
dotnet add package ChartForgeX
```

Add an optional package only for the capability the host needs:

```powershell
dotnet add package ChartForgeX.Interactivity.Html
dotnet add package ChartForgeX.Markup
dotnet add package ChartForgeX.Mermaid
```

The core package targets .NET Framework 4.7.2, .NET Standard 2.0, .NET 8, and .NET 10. It has no runtime package dependencies. The browser adapter generates self-contained HTML and does not turn the static renderer into a JavaScript-first dependency.

See the [NuGet package](https://www.nuget.org/packages/ChartForgeX) for the current public version and supported assets.
