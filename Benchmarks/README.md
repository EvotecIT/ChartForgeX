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

The topology suite measures dense layout, completed routes and exports on small linked groups, a wrapped mesh, an overview, a 60-site replication capture from a large directory-monitoring report and mixed authored/planned routes:

```powershell
.\Benchmarks\Invoke-RenderingBenchmark.ps1 -Suite Topology -WarmupCount 2 -IterationCount 9
```

`Prepare` measures detached layout and can defer route planning. `CompletePrepare` includes `Analyze()` to finish and inspect the routes inside the timed operation. `Svg` creates a fresh snapshot and exports it; `PreparedSvg` reuses a fully planned snapshot. The dense planner shares a plan between charts whose planning inputs are equal (a host's light and dark drawings of one chart), so `Prepare`, `CompletePrepare` and `Svg` clear that cache before they run; `RepeatSvg` is `Svg` without clearing it, as a host drawing the same chart again. Each lane checks attached ends, dimensions, node/edge counts, complete diagnostic digests and SVG outside timing.

The charts suite measures the report charts of that large monitoring report: a 60-lane status timeline with about 1,300 periods, an 8-lane overview timeline, a two-series latency line and a calendar heatmap, each with host colour variables, rendered to SVG and PNG:

```powershell
.\Benchmarks\Invoke-RenderingBenchmark.ps1 -Suite Charts -WarmupCount 2 -IterationCount 9
```

Every lane validates byte-identical SVG and PNG outside timing, and both suites record `ThreadAllocatedBytes`, the managed allocation of the rendering thread inside the measured operation.
Pass `-BaselineAssemblyPath` with a saved `net8.0/ChartForgeX.dll` to compare a change against that binary. Both binaries run in the same process with rotated ordering and identical fixtures; a geometry or SVG difference fails the comparison. Without a saved baseline, both lanes use the current binary as a repeatability check. Keep raw samples, binary hashes, source commits and processor/power settings with qualified comparisons. These machine-specific measurements run separately from ordinary correctness CI.

## Direct-scene proof

The `Scenes` suite compares the frozen legacy public export path with the prepared-scene path for six models: a seven-category Cartesian combination and donut, three-series grouped and stacked bars over 24 categories with positive, negative and zero values, 500 numeric scatter points, and a 1,000-point line with four explicit breaks. Both lanes use the canonical token JSON, the licensed Carlito regular/bold fixtures, an 800 × 440 logical viewport, explicit identical marker density and raster scale 1. The fixture project references a built product DLL and stays outside the solution and shipped packages.

```powershell
./Benchmarks/Invoke-RenderingBenchmark.ps1 -Suite Scenes -SkipBuild -BaselineAssemblyPath /path/to/frozen/ChartForgeX.dll -BaselineApiProfile Legacy -WarmupCount 2 -IterationCount 9 -OutputRoot /path/to/task-evidence/scenes
```

`Svg`, `Rgba` and `Png` include layout and export in both lanes. Candidate-only `Compile` measures preparation; `PreparedSvg`, `PreparedRgba` and `PreparedPng` reuse a scene prepared outside timing. Compare complete operations with their legacy counterpart. Prepared export timings describe scene reuse and do not represent complete rendering.

Validation checks exact source-data digests across both lanes, every SVG source point and value (including radial aggregation coverage), nonempty drawing commands, SVG title, exact SVG/PNG/RGBA dimensions, decoded PNG/RGBA ink and deterministic SVG/PNG bytes within each unchanged lane. Each lane validates a complete SVG before samples, so raster workloads have the same source-coverage proof. Approved layout changes can alter old/new output bytes; the existing `Charts` suite retains its separate byte-identical preservation gate. `ThreadAllocatedBytes` measures managed allocations on the rendering thread; `OutputBytesOrRegions` reports bytes for exported outputs or semantic-region count for `Compile`.

`-SceneGroup Phase2` is the default and retains those six fixtures and their operation names. `-SceneGroup Phase3` selects eight additional workloads; `-SceneGroup All` includes both groups. The additional groups have separate suite names, so their results cannot silently replace the six-fixture history. All exported formats also check deterministic RGBA pixels; compilation checks deterministic SVG and nonempty semantic regions outside timing.

| Phase 3 fixture | Authored input | Complete SVG source proof |
| --- | --- | --- |
| `matrix` | Eight heatmap rows × twelve columns, including zero values | Every row/column and numeric value in its accessible name; numeric metadata where emitted |
| `calendar` | A fixed 120-day interval, 102 authored days, missing and zero days | Every authored date and value; empty cells remain distinct from authored zero values |
| `progress` | Six labeled rows from zero to a maximum of 100 | Every row index and value, including zero |
| `polar` | Two series of 24 angle/radius pairs | Every series/point index, angle and radius |
| `map` | Six labeled, weighted world locations | Every index, label, longitude, latitude and weight |
| `treemap` | Twelve labeled positive weights | Every tile index, label and weight |
| `sankey` | Eight weighted directed links between six nodes | Every source/target label pair and weight |
| `topology` | Twelve nodes and eleven orthogonal links in a layered tree | Every node ID, label and status; every edge ID, endpoints and label |

```powershell
./Benchmarks/Invoke-RenderingBenchmark.ps1 -Suite Scenes -SceneGroup Phase3 -SkipBuild -BaselineAssemblyPath /path/to/frozen/ChartForgeX.dll -BaselineApiProfile Legacy -WarmupCount 2 -IterationCount 9 -OutputRoot /path/to/task-evidence/scenes-phase3
```

Use `-BaselineApiProfile Legacy` for the frozen integrated baseline `fdb8fe16df11037c1228168c6984ed31316ae04f`. Each lane's fixture is compiled against its measured product DLL. The legacy profile uses the historical Sankey link API and validates numeric endpoint indices, source/target labels and weights. It retains the original authoring labels outside the measured chart and requires rendered endpoint labels to match them. The default `Current` profile uses authored node/link IDs and validates those identities, endpoints, labels and weights; it also supports the current-binary repeatability check when no saved baseline is supplied. The source digests compare common authoring labels and actual directed endpoints and weights, without requiring identities the historical API did not have. Profiles are explicit compile-time choices; malformed or mixed relationship metadata fails validation.

The Phase 3 chart factories set padding 24, raster scale 1 and supersampling 2 explicitly. Both lanes register the same regular/bold Carlito files and apply the same canonical light palette and title/subtitle/axis/legend/data-label sizes of 22/13/11/12/11. Public baseline mode preserves the historical convenience-export comparison; its topology surface and typography mapping can differ from the candidate's explicit frame. Flow and sequence are excluded from this paired group because their frozen public exporters cannot accept the same token/font request. The dedicated topology suite remains the separate dense-routing proof. These small workloads do not establish performance for every option or for large datasets.

For a baseline that already supports the current relationship APIs and prepared scenes, use `-BaselineRendering Direct` with the default `Current` API profile. The wrapper compiles the same direct adapter source separately against each product binary, so constructor changes cannot bind the baseline to the candidate's API. Both lanes receive the same explicit frame, canonical colors, typography, font files and raster settings. Their visual behavior may differ because the product changed; retained source counts, dimensions and within-lane determinism are still validated. The run records both adapter hashes, the API profile and the selected baseline boundary. Direct mode requires a compatible prepared-scene baseline and rejects the legacy profile.

Paired chart, topology and scene comparisons use shared fixture scenarios, format-specific operation names, and `Baseline`/`Candidate` engines. Case variables are identical between paired engines, so PowerForge's rotated ordering alternates which engine runs first in each group. Do not put a lane name in the scenario or case variables: that creates separate groups and defeats paired rotation. Compare summary rows by scenario, operation, host and OS, using the engine field to identify each lane. The topology suite retains its 22 supported pairs and explicitly skips the six mixed-fixture SVG work items.

Direct scene adapters explicitly pin padding to 24 and legend placement to `TopLeft`, with legend visibility enabled for chart fixtures and disabled for topology. Optional constructor defaults are compiled into each adapter; leaving those settings implicit can select different requests when the product API changes. Typography roles and visual geometry inside the product can still differ intentionally.

Qualify a stable candidate by first inspecting `-Plan`, then running a zero-warmup, one-iteration correctness smoke, and finally the two-warmup, nine-iteration rotated comparison with no other heavy work running. Even `-SkipBuild -Plan` builds the fixture adapter and loads both product assemblies; it is not a source-only command. Freeze and hash the product DLLs, adapter DLL, suite/helper files, token JSON and both fonts before the measured run. Keep the baseline source revision and original binary hash with the report. A failed source, drawing, dimensions, decoded-ink or determinism check invalidates the affected case; resolve it before interpreting timings. Compare only paired complete operations against the requested 10% threshold, retaining all raw samples and the environment record.

PowerForge rejects measurements when source provenance changes during a run. Freeze the participating worktree and binaries before qualification. An immutable copy of the same suite/helper and hashed product, fixture, token and font inputs can exercise validation while other development continues, but a zero-warmup, one-sample smoke run establishes no performance result. Record source revisions, input hashes, SDK/runtime, CPU/OS, power policy and quality settings with paired results. Keep raw samples; investigate any requested complete-operation regression above 10% rather than treating the separate history noise allowance as that proof.

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
