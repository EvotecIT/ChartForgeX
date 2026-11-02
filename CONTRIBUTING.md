# Contributing

Thanks for helping make ChartForgeX better.

## Local Quality Loop

Run the full repository quality loop before opening a pull request:

```powershell
./Build.ps1 -Configuration Release
```

That command restores packages, builds all projects, runs the smoke suite through `dotnet test`, checks the pinned Mermaid reference corpus, runs the AOT smoke, regenerates example outputs, checks the visual-health baselines, packs the library packages, verifies their dependency boundaries, and checks the symbol packages.

For a faster test-only loop:

```powershell
dotnet test .\ChartForgeX.sln -c Release
```

Use a supported Node.js runtime for the Mermaid reference checks. CI selects Node 24, starting at 24.15.0; the npm restore enforces dependency engine requirements.

For slow native test runs, add `-TestDiagnosticsDirectory <output-directory>`. This records named test timings, warnings identifying tests still running after 30 seconds, a final TRX report, and incremental VSTest logs that remain available if the process times out. It preserves the existing 900-second command limit and test scheduling. CI retains these files in the `chartforgex-native-results-<OS>` artifacts, including after a failed run.

## Package Consumers

`-QualifyPackages` additionally builds and runs isolated consumers of the packed packages. The Windows CI quality job enables this gate and retains its reports. Core, Visuals and Stories execute net472 consumers on the runner's installed .NET Framework. All five consumer lanes execute net8.0 and net10.0, while netstandard2.0 has compile proof.

The downstream lane loads public OfficeIMO.ChartForgeX and HtmlForgeX.ChartForgeX adapter packages with the candidate ChartForgeX packages and saves real Office files and report HTML. Running [package qualification](Build/Test-PackageQualification.ps1) with `HFX_TEST_ENABLE_PLAYWRIGHT=1` also exercises packed chart selection, graph editor add/undo/redo and downstream HTML at compact and wide sizes through HtmlTinkerX. Native Office appearance, other installed hosts and private consumers need their own acceptance.

## Change Expectations

- Add or update smoke tests for every new renderer behavior.
- Add an example when a change affects visible chart output.
- Keep the core package free of runtime package dependencies.
- Keep project package references private unless they are part of the core package contract.
- Keep SVG/HTML output static and self-contained.
- Preserve `net472`, `netstandard2.0`, `net8.0`, and `net10.0` support.
- Breaking changes are allowed when they bring a cleaner architecture, better usability, or a simpler public API. Document the migration path instead of keeping compatibility shims that lock in poor design.
- Do not suppress warnings with `NoWarn`.

## Browser Quality

The quality workflow runs the build/test/example/package loop on Windows, Linux, and macOS. A separate Windows Chromium job requires rendered browser and interaction checks to execute.

Run that browser lane locally with a task-owned absolute output directory:

```powershell
./Build/Test-BrowserQuality.ps1 -OutputRoot (Join-Path ([IO.Path]::GetTempPath()) 'chartforgex-browser-quality')
```

The runner builds the tests, generates and serves a fresh v2 gallery on loopback, and enables the `Category=Browser` contracts. HtmlTinkerX owns Chromium installation and session lifetime. The runner stops its gallery server and restores its environment when it finishes, including after a test failure. Its output directory retains the TRX, screenshots, measured defects, and console evidence for inspection; remove superseded task output when it is no longer needed.

The ordinary test loop reports disabled browser contracts and missing platform prerequisites as explicit skips. Required mode fails on incomplete setup, an empty selection, or skipped tests. A passing build without the required browser lane does not qualify interaction behavior or complete appearance.

## Examples and Website

The website presents the examples; the executable projects own their source and qualification. Keep chart definitions in compiled recipes, then generate displayed code and artifacts from those recipes. Do not maintain a second chart definition in a page or snippet string.

- `ChartForgeX.Examples` generates the gallery manifest, SVG/PNG/HTML artifacts, downloadable programs and visual-health results. It also supplies fixtures to the browser quality lane.
- `ChartForgeX.Examples.Snippets` links the core-only recipes and executes their advertised selections without referencing the examples executable.
- `Website/build/Sync-GeneratedExamples.ps1` imports the gallery manifest and its artifacts. The website does not maintain a separate family catalog.
- `Website/build/Sync-GeneratedExamples.ps1` also writes one page per chart family under `Website/content/pages/gallery/`. Those pages are front matter only; never edit them by hand.
- `Website/tools/SiteVisuals` generates the site's curated examples and presentation artwork. Shared documentation comes from the source-backed collections configured in `Website/site.json`.

Gallery recipes should sell their chart: realistic names, several series where the family supports them and enough points to show a shape. Keep edge-case data (empty, zero, sparse or single observations) in separate variants; the family pages leave those to the catalog. Downloadable programs keep imports and output plumbing in a `#region Setup` block so the site shows the chart model first.

Keep offline fixtures that cover distinct rendering or interaction contracts, even when they are not public gallery cards. Keep branding and page composition in the website generator. After changing a recipe, compile its displayed program, inspect the affected SVG and PNG, and regenerate the website through its existing owners. [The website guide](Website/AGENTS.md) describes those sources and commands.

## Build Timeouts

`Build.ps1` time-limits every `dotnet` validation step so a stuck restore, build, test, example, pack, or package-consumer run fails with a named timeout instead of hanging indefinitely.

- `-DotNetCommandTimeoutSeconds` controls the general restore/build/test/example/pack timeout. The default is 900 seconds.
- `-PackageConsumerTimeoutSeconds` controls the final temporary-console-app package smoke run. The default is 180 seconds.

If a runner is under heavy load, rerun with a larger timeout:

```powershell
./Build.ps1 -Configuration Release -DotNetCommandTimeoutSeconds 1800 -PackageConsumerTimeoutSeconds 300
```

## Package Invariants

- `ChartForgeX` and `ChartForgeX.Interactivity` must not contain runtime NuGet dependencies.
- `ChartForgeX.Interactivity.Html` must depend on the matching local `ChartForgeX` and `ChartForgeX.Interactivity` packages.
- `README.md` must be included in the package root.
- XML documentation should be present for every target framework.
- Each library package generates a `.snupkg` alongside its `.nupkg`. The markup CLI is packaged separately as a .NET tool.
- A clean temporary `net8.0` console app must be able to install the freshly packed HTML interactivity package and render SVG, HTML, PNG, and interactive HTML output.

## Coordinated Package Release

`Build/Build-Project.ps1` delegates releases to PSPublishModule and PowerForge using
`Build/project.build.json`. All nine ChartForgeX packages, including the markup CLI tool,
share the `3.0.X` release lane. PowerForge resolves the next version from the highest published
version in that group and updates `ChartForgeXProductVersion` in `Directory.Build.props`.
The projects keep their shared version references, so package and assembly metadata stay aligned.

Install PSPublishModule 3.0.168 or later before running this release entrypoint:

```powershell
Install-Module PSPublishModule -MinimumVersion 3.0.168 -Scope CurrentUser
```

Inspect the release plan before signing or publishing:

```powershell
./Build/Build-Project.ps1 -Plan:$true -PlanPath ./artifacts/project.build.plan.json -PublishNuget:$false -PublishGitHub:$false
```

The plan selects the nine release projects without changing their source versions or publishing.
The release configuration packages each project and creates one GitHub release for the group.
Signing uses the configured certificate when the operator runs the release; ordinary validation
uses the quality loop above. Publishing is enabled only through explicit release arguments.

## Visual Baseline

Release builds validate `ChartForgeX.Examples/visual-baseline.json` against the generated SVG/PNG comparison manifest. The baseline tracks SVG dimensions, high-DPI PNG output scale, minimum SVG visual-node counts, minimum PNG visible-pixel/color counts, clipped SVG text, near-edge SVG text, and PNG edge pressure.

Before refreshing the baseline, inspect:

- `ChartForgeX.Examples/bin/Release/net8.0/output/quality-dashboard.html`
- `ChartForgeX.Examples/bin/Release/net8.0/output/svg-png-comparison.html`
- `ChartForgeX.Examples/bin/Release/net8.0/output/svg-png-comparison.json`

If a visual change is intentional, refresh the baseline with:

```powershell
./Build.ps1 -Configuration Release -UpdateVisualBaseline
```

Commit the refreshed baseline only with the renderer or example change that justifies it. Do not refresh the baseline to hide warnings, clipped SVG text, edge pressure, or accidental PNG/SVG parity regressions.

## Topology Visual Coverage

Topology and geographic topology SVG/PNG pairs participate in the shared `ChartForgeX.Examples/visual-baseline.json`. The quality loop also checks `topology-demo/visual-capability-manifest.json` in the selected examples output directory, required SVG metadata, generated SVG/HTML/PNG artifacts, and PNG size. Every topology pair must appear in the shared comparison and baseline.

Treat numerical health as one check. Inspect complete SVG and native PNG output for readable labels, routing and fitting before accepting or refreshing a baseline. Geometry and browser regressions should protect concrete failures such as labels occupying the same lane; matching dimensions and visible pixels cannot establish that a diagram is readable.

## Release Qualification

Before an authorized release, run the quality loop with `-QualifyPackages`, the required browser lane, and the applicable cross-platform CI checks. Inspect the generated examples and retain the release candidate's package-consumer results. Use the coordinated PowerForge entrypoint described above to version and publish the nine packages together. Verify the public artifacts and affected installed consumers separately; successful local packing establishes neither publication nor host readiness.
