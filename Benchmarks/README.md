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

## Accepted rendering history

`Invoke-RenderingHistory.ps1` uses PowerForge's `Test-BenchmarkHistory` to verify three fixed 500 × 180 rendering cases: twelve composition labels with default and full hinting, and twelve imported SVG labels. The original portable font fixture avoids installed-font discovery. Every case checks dimensions and visible ink in all twelve rows outside timing. The benchmark fixture is excluded from solution and package builds.

Supply the path to a built PSPublishModule manifest containing `Test-BenchmarkHistory`:

```powershell
./Benchmarks/Invoke-RenderingHistory.ps1 -ModulePath /path/to/PSPublishModule.psd1 -RunnerIdentity my-rendering-runner -Mode CalibrationProof
./Benchmarks/Invoke-RenderingHistory.ps1 -ModulePath /path/to/PSPublishModule.psd1 -RunnerIdentity my-rendering-runner -Mode AcceptReference
./Benchmarks/Invoke-RenderingHistory.ps1 -ModulePath /path/to/PSPublishModule.psd1 -RunnerIdentity my-rendering-runner -Mode Verify
```

`CalibrationProof` requires a separate empty history path. It explicitly accepts five benchmark executions, verifies a sixth, then proves that adding a deliberate 50 ms delay to each operation fails without changing history. This contemporaneous functional cohort is not a longitudinal fleet baseline. `AcceptReference` accepts one run only by explicit request; inspect raw measurements before accepting it. `Verify` is read-only and fails when history is missing, incomparable, incomplete or regressed.

Each execution retains two warmups and nine rotated samples per lane without removing outliers. A gate requires five accepted executions in the same workload, CPU, OS, SDK/runtime, measurement boundary and process-policy environment. Its allowance is the historical median plus the largest of 10%, 3 ms or three median absolute deviations. These are visible operational noise floors, not a confidence interval or a portable performance promise. PowerForge measures the guarded operation-body invocation after captured-context and native-exit guard setup, and records `GuardedScriptBodyV1` in run metadata. References collected with the earlier wrapper-inclusive timing boundary require fresh qualification; they cannot establish the new boundary's baseline. A changed workload requires a new workload ID; product commit and binary hashes are retained as provenance rather than resetting the history for every code change.

The separate `Rendering history` workflow runs Linux, Windows and macOS. Benchmark-path PRs opt into disposable calibration proof; ordinary product PRs do not run this timing lane. Manual execution can accept or verify references, and the weekly scheduled run verifies the latest successful explicit `AcceptReference` run on `main`. Five separate accepted runs are needed to bootstrap a runner environment. Updated runner hardware, images or SDKs require new calibration instead of reusing an incompatible threshold. No accepted run means verification fails visibly.

The workflow builds pinned [PowerForge source](https://github.com/EvotecIT/PSPublishModule/commit/f6a704092041a81841ec5154eb4fd028912f375e) at its declared module version, using its exact SDK policy, without installation, signing or publication. PowerForge captures the SDK selected from the benchmark source directory; compiled fixture hashes remain separate provenance. Process priority is controlled only on Windows, where that option is supported. The workflow restores only the small accepted-history artifact from a successful explicit main-branch acceptance; current raw samples, environment and gate reports are retained separately for 90 days. It never automatically accepts a regression or downloads executable benchmark code from a reference artifact.
