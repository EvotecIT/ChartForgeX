# Performance runners and guards

ChartForgeX keeps two kinds of performance evidence. The runners under
`Benchmarks/` measure timings on demand through PowerForge
(`PSPublishModule`) and never gate CI. The test suite holds a few deterministic
guards that fail when a size budget is exceeded or cost stops growing linearly.
The [benchmark guide](../../Benchmarks/README.md) describes each runner's
fixtures, validation and paired-comparison rules.

## Running the runners

Build the solution in Release first. The rendering wrappers read
`ChartForgeX/bin/Release/net8.0`; the browser runners read the test output,
which carries HtmlTinkerX and Playwright. Point `-OutputRoot` at a scratch
directory outside the repository and delete it when you have kept what you need.

```powershell
dotnet build .\ChartForgeX.sln -c Release
$out = 'D:\Scratch\cfx-bench'          # any task-owned scratch directory
$bin = '.\ChartForgeX.Tests\bin\Release\net8.0'

./Benchmarks/Invoke-RenderingBenchmark.ps1 -Suite Rendering  -SkipBuild -OutputRoot "$out\rendering"
./Benchmarks/Invoke-RenderingBenchmark.ps1 -Suite Decimation -SkipBuild -OutputRoot "$out\decimation"
./Benchmarks/Invoke-RenderingBenchmark.ps1 -Suite Charts     -SkipBuild -OutputRoot "$out\charts"
./Benchmarks/Invoke-RenderingBenchmark.ps1 -Suite Scenes -SceneGroup All -SkipBuild -OutputRoot "$out\scenes"
./Benchmarks/Invoke-RenderingBenchmark.ps1 -Suite Topology   -SkipBuild -OutputRoot "$out\topology"
./Benchmarks/Invoke-RenderingBenchmark.ps1 -Suite XZoom      -SkipBuild -OutputRoot "$out\xzoom"
./Benchmarks/Invoke-InteractivityBenchmark.ps1      -ProductBinaryRoot $bin -OutputRoot "$out\interactivity"
./Benchmarks/Invoke-SectionPreparationBenchmark.ps1 -ProductBinaryRoot $bin -OutputRoot "$out\section" -Matrix Cartesian
./Benchmarks/Invoke-SustainedUpdateBenchmark.ps1    -ProductBinaryRoot $bin -OutputRoot "$out\sustained" -Matrix Current `
    -BaselineUnavailableReason 'Current-only run'
./Benchmarks/Invoke-RenderingHistory.ps1 -ModulePath (Get-Module -ListAvailable PSPublishModule | Select-Object -First 1).Path `
    -RunnerIdentity my-machine -Mode FunctionalProof -OutputRoot "$out\history"
```

Add `-WarmupCount 0 -IterationCount 1` for a smoke run that only proves a runner
works, or `-Plan` to list its matrix. PowerForge rejects a run when the source
changes while it measures, so do not edit the worktree during a run. The
Topology, Charts and Scenes suites pair a `Baseline` and a `Candidate` lane;
without `-BaselineAssemblyPath` both use the current binary as a repeatability
check.

## Reference measurements

Measured on 9 November 2026 on an AMD Ryzen 9 9950X3D2 with Windows 11, .NET 8
and headless Chromium. Medians from one warmup and three to five measured runs.
They are machine-specific and show orders of magnitude, not guarantees.

| Runner | Result |
| --- | --- |
| Rendering | 72-point report chart: SVG 29 ms, RGBA 75 ms, PNG 87 ms. 128-node force topology: SVG 183 ms, PNG 335 ms |
| Decimation | 100,000 to 1,200 points: LTTB 11.9 ms, min/max 11.8 ms |
| Charts | 60-lane status timeline: SVG 45 ms, PNG 228 ms. Small report charts: SVG 3–6 ms, PNG 15–34 ms |
| Topology | Small groups: prepare and SVG about 7.5 ms. 40–60 node dense meshes, overviews and replication maps: prepare 2.4–2.7 s; a reused prepared snapshot exports SVG in 6–8 ms |
| XZoom | Zoom-bank preparation 10.9 ms (median of case medians); prepared window 0.27 ms; prepared SVG 0.62 ms |
| Interactivity | 16 pointer hovers: 170 ms at 1,000 Scatter points, 840 ms at 5,000 |
| Section preparation | 3-point fixtures 9.8–16.3 ms; 1,000-point scatter 29–70 ms |
| Sustained update | 32 points: about 67 ms per update; 1,000 per series with a secondary value: about 5.1 s per update |

Interactive line charts bind in time proportional to their points. A raw
100,000-point line exports to PNG in about 2.5 s. `AddAdaptiveLine` keeps
binding and export near 200–400 ms by retaining about two points per pixel.

### Dense interactive lines

The line from `InteractiveBindingScalingBrowserTests` (1200 × 600, no legend)
saved with `ToInteractiveHtmlPage`, opened from disk in headless Chromium at
1400 × 900. *Zoom* pages enable Tooltips, Zoom and Pan; *review* pages add the
default report features (selection, keyboard, crosshair, compare). Ready is
navigation to the bound marker plus the next frame. Hover is the time from a
pointer move to the second painted frame, measured in the page over 40 moves
that alternate between the line and the empty plot, so each one crosses the
line. Zoom is one toolbar zoom-in from the click until the eased window settles.
Before and after pages ran interleaved in one browser; best of three loads.

| Line | Ready before | Ready after | Hover p95 before | Hover p95 after | Zoom before | Zoom after |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1,000, zoom | 132 ms | 132 ms | 42 ms | 33 ms | 344 ms | 333 ms |
| 1,000, review | 199 ms | 198 ms | 46 ms | 33 ms | 359 ms | 333 ms |
| 10,000, zoom | 733 ms | 698 ms | 256 ms | 51 ms | 543 ms | 424 ms |
| 10,000, review | 1,433 ms | 1,254 ms | 280 ms | 51 ms | 637 ms | 513 ms |
| 50,000, zoom | 3,449 ms | 3,232 ms | 37 ms | 49 ms | 1,375 ms | 926 ms |
| 50,000, review | 6,169 ms | 6,200 ms | 59 ms | 69 ms | 1,965 ms | 1,434 ms |

Measured on 11 November 2026 on the machine above; single loads vary by up to a
third, so the pairs compare and the ready times are within noise. Ready
includes parsing the saved page, 10 MB at 10,000 points and 45 MB at 50,000.
Median hover at 10,000 points fell from 158 ms to 37 ms (zoom) and from 193 ms
to 41 ms (review). At 50,000 points the pointer never leaves the series band,
so the series hover and the mode switch never ran there; hover stays within
run-to-run variation, and repeats of the review page measured its
p95 at 54–69 ms before and 61–82 ms after.

Crossing a dense line no longer restyles the whole series. Entering a line,
area or bar series used to hover the series group, whose inherited stroke width
and descendant marker rules restyled every mark, and the stage readout then
moved the hover to the nearest observation in its next frame, restyling them
all again. Unless only exact hits read out, the stage readout now takes that
pointer directly. Switching between series and shared hover used to toggle
cfx-hover-series on every observation; a series hover unit now marks only the
series, its legend items and other summaries, and an observation carries
cfx-hover-series only when the hover unit is the point. The legend's hover tint
now follows its hit area's own `:hover`, so the pointer entering the chart or a
series no longer restyles every observation's hit area.

Zooming used to rewrite the geometry of the hit area and hover marker that the
runtime adds to each observation. An observation drawn by nothing else now
moves by one translation, and both marks keep their drawn size, as markers
already did. Its region stays in its own coordinates, which the translation
carries to the screen. What remains at 50,000 points is restyling the moved
observations (about 0.2 s), laying out and painting the drawing, and the
keyboard and axis refresh.
## Guards in the test suite

| Guard | Test | Fails when |
| --- | --- | --- |
| Runtime size | `InteractiveBundleTests` | A chart, feature, diagram, topology or graph Explorer runtime exceeds its raw or gzip budget (see [interactivity](../interactivity.md#selecting-an-offline-runtime)) |
| Static export scaling | `StaticExportScalingTests` | Four times the points of a raw line chart costs more than eight times the SVG or PNG time, or more than six times the allocation |
| Interactive binding scaling | `InteractiveBindingScalingBrowserTests` | Four times the points of an interactive line chart takes more than eight times as long to bind in Chromium |
| Dense line locality | `DenseLineInteractionBrowserTests` | Crossing a 2,000-point line hovers the whole series or repaints more than a few observations, or a zoom rewrites the observations' hit areas and hover markers |
| Release budgets | The release-budget smoke test, `GraphiteRenderingBudgetTests`, `TopologyReplicationBudgetTests` | Representative renders exceed generous time or allocation ceilings |

The scaling guards compare two sizes on the same machine and take the best of
three runs, so machine speed cancels out. Linear work gives a ratio near four
and quadratic work near sixteen. They run in the non-parallel
`RenderingBudgetCollection`; the browser guard runs only where browser tests are
enabled (`HFX_TEST_ENABLE_PLAYWRIGHT=1`).
