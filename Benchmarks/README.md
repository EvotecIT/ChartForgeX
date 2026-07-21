# Rendering benchmarks

The rendering suite measures representative ChartForgeX SVG, direct RGBA, and encoded PNG work through PowerForge's reusable benchmark runner. It covers a three-series, 72-point, 1,200 x 675 report chart and a deterministic 128-node, 230-edge force topology. The RGBA cases isolate rendering from image encoding for composition-heavy hosts such as wallpaper and report generators.

```powershell
.\Benchmarks\Invoke-RenderingBenchmark.ps1
```

Use `-Plan` to inspect the matrix without running it. Results are machine-specific and are written under `Ignore/Benchmarks/Rendering` by default, including raw samples, normalized summaries, run metadata, and Markdown output. The recorded assembly hash ties every result to the measured binary.

This suite is a regression baseline for ChartForgeX rendering. It does not claim a direct speed ranking against ApexCharts, Chart.js, or vis-network: those libraries primarily measure browser startup, layout, paint, and interaction, while this suite measures deterministic .NET artifact generation. Browser performance is reviewed separately through the graph scale fixtures and render-work telemetry described in `docs/graph-explorer.md`.
