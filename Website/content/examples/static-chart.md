---
title: "Your first static chart"
description: "Render the same typed ChartForgeX chart as SVG, HTML, and PNG."
layout: docs
---

```csharp
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

var chart = Chart.Create()
    .WithTitle("Deployment health")
    .WithXAxis("Run")
    .WithYAxis("Checks")
    .WithSize(1180, 640)
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri")
    .AddSmoothArea("Passed", Points(820, 940, 980, 1040, 1120))
    .AddSmoothLine("Failed", Points(22, 30, 28, 21, 18), ChartColor.FromRgb(248, 113, 113));

chart.SaveSvg("deployment-health.svg");
chart.SaveHtml("deployment-health.html");
chart.SavePng("deployment-health.png");

static IEnumerable<ChartPoint> Points(params double[] values) {
    for (var index = 0; index < values.Length; index++) {
        yield return new ChartPoint(index + 1, values[index]);
    }
}
```

`To*` methods return content, `Save*` methods write files, and `Write*` methods stream bytes. SVG is the highest-fidelity static target; HTML wraps inline SVG; PNG is useful for email and document pipelines.
