---
title: "Small-multiple report grid"
description: "Facet typed records into a deterministic multi-panel chart report."
layout: docs
---

```csharp
using ChartForgeX.Core;
using ChartForgeX.Data;

var samples = ChartDataset<CpuSample>.From(new[] {
    new CpuSample("Warsaw", 1, 35),
    new CpuSample("Warsaw", 2, 42),
    new CpuSample("London", 1, 48),
    new CpuSample("London", 2, 61)
});

var report = ChartGrid.FromFacets(
    samples,
    sample => sample.Site,
    (site, rows) => Chart.Create()
        .WithTitle(site)
        .WithYAxis("CPU (%)")
        .AddLine("CPU", rows, sample => sample.Minute, sample => sample.Cpu),
    columns: 2);

report.SavePng("cpu-by-site.png");
report.SaveSvg("cpu-by-site.svg");

record CpuSample(string Site, double Minute, double Cpu);
```

Use `ChartGrid` for chart-only small multiples. Use visual blocks or a visual canvas when the report also needs exact facts, lists, or positioned artwork.
