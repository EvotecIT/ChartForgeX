# Rendering benchmarks

The rendering suite measures representative ChartForgeX SVG, direct RGBA, and encoded PNG work through PowerForge's reusable benchmark runner. It covers a three-series, 72-point, 1,200 x 675 report chart and a deterministic 128-node, 230-edge force topology. The RGBA cases isolate rendering from image encoding for composition-heavy hosts such as wallpaper and report generators.

```powershell
.\Benchmarks\Invoke-RenderingBenchmark.ps1
```

Use `-Plan` to inspect the matrix without running it. Results are machine-specific and are written under `Ignore/Benchmarks/Rendering` by default, including raw samples, normalized summaries, run metadata, and Markdown output. The recorded assembly hash ties every result to the measured binary.

This suite is a regression baseline for ChartForgeX rendering. It does not claim a direct speed ranking against ApexCharts, Chart.js, or vis-network: those libraries primarily measure browser startup, layout, paint, and interaction, while this suite measures deterministic .NET artifact generation. Browser performance is reviewed separately through the graph scale fixtures and render-work telemetry described in `docs/graph-explorer.md`.

Run the explicit dense-series reduction suite through the same wrapper:

```powershell
.\Benchmarks\Invoke-RenderingBenchmark.ps1 -Suite Decimation
```

It reduces a deterministic 100,000-point signal to at most 1,200 retained points with both LTTB and min/max algorithms. Validation checks source count, point budget, endpoint preservation, and source-index consumption. The suite measures point reduction only; the rendering suite remains the separate artifact-generation baseline.

The topology suite measures dense layout, completed routes and exports on small linked groups, a wrapped mesh, an overview and mixed authored/planned routes:

```powershell
.\Benchmarks\Invoke-RenderingBenchmark.ps1 -Suite Topology -WarmupCount 2 -IterationCount 9
```

`Prepare` measures detached layout and can defer route planning. `CompletePrepare` includes `Analyze()` to finish and inspect the routes inside the timed operation. `Svg` creates a fresh snapshot and exports it; `PreparedSvg` reuses a fully planned snapshot. Each lane checks attached ends, dimensions, node/edge counts, complete diagnostic digests and SVG outside timing.

Pass `-BaselineAssemblyPath` with a saved `net8.0/ChartForgeX.dll` to compare a change against that binary. Both binaries run in the same process with rotated ordering and identical fixtures; a geometry or SVG difference fails the comparison. Without a saved baseline, both lanes use the current binary as a repeatability check. Keep raw samples, binary hashes, source commits and processor/power settings with qualified comparisons. These machine-specific measurements run separately from ordinary correctness CI.
