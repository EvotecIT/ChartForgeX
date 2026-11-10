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
pointer move to the second painted frame, measured in the page over 40 moves.
Zoom is one toolbar zoom-in from the click until the eased window settles.
Before and after pages ran interleaved in one browser; best of three loads.

| Line | Ready before | Ready after | Hover p95 before | Hover p95 after | Zoom before | Zoom after |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1,000, zoom | 158 ms | 126 ms | 65 ms | 51 ms | 349 ms | 349 ms |
| 1,000, review | 291 ms | 174 ms | 83 ms | 81 ms | 381 ms | 364 ms |
| 10,000, zoom | 1,331 ms | 764 ms | 362 ms | 349 ms | 615 ms | 566 ms |
| 10,000, review | 2,108 ms | 1,430 ms | 495 ms | 483 ms | 880 ms | 730 ms |
| 50,000, zoom | 6,042 ms | 4,136 ms | 71 ms | 88 ms | 1,912 ms | 1,525 ms |
| 50,000, review | 9,610 ms | 8,221 ms | 117 ms | 117 ms | 3,675 ms | 1,947 ms |

Measured on 10 November 2026 on the machine above while other builds kept it
75–100% busy, so the pairs compare and the absolute values run high. Ready
includes parsing the saved page, 10 MB at 10,000 points and 45 MB at 50,000.

Binding cost one style pass per readout write: the card and toolbar tokens were
inherited custom properties, so each write restyled every mark. They no longer
inherit, which leaves one style pass for the first frame. The zoom repaint now
reuses the keyboard availability of observations wholly inside the plot and
pauses the change watches while it moves every mark. What remains at 50,000
points is proportional to the marks the runtime adds per observation, a hit area
and a hover marker: rewriting their geometry and restyling them take about
0.7 s each, and refreshing tens of thousands of keyboard stops about 0.5 s.
Hover on 10,000 points takes about 0.3 s per move, mostly restyling marks.

## Guards in the test suite

| Guard | Test | Fails when |
| --- | --- | --- |
| Runtime size | `InteractiveBundleTests` | A chart, feature, diagram, topology or graph Explorer runtime exceeds its raw or gzip budget (see [interactivity](../interactivity.md#selecting-an-offline-runtime)) |
| Static export scaling | `StaticExportScalingTests` | Four times the points of a raw line chart costs more than eight times the SVG or PNG time, or more than six times the allocation |
| Interactive binding scaling | `InteractiveBindingScalingBrowserTests` | Four times the points of an interactive line chart takes more than eight times as long to bind in Chromium |
| Release budgets | The release-budget smoke test, `GraphiteRenderingBudgetTests`, `TopologyReplicationBudgetTests` | Representative renders exceed generous time or allocation ceilings |

The scaling guards compare two sizes on the same machine and take the best of
three runs, so machine speed cancels out. Linear work gives a ratio near four
and quadratic work near sixteen. They run in the non-parallel
`RenderingBudgetCollection`; the browser guard runs only where browser tests are
enabled (`HFX_TEST_ENABLE_PLAYWRIGHT=1`).
