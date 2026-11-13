# ChartForgeX

Dependency-free .NET chart and diagram rendering engine. Renders charts,
topology, Mermaid, stories and visual blocks to SVG, HTML and PNG for
reports, dashboards, email, documentation and static sites, with optional
browser interactivity.

## Layout

- `ChartForgeX/`: core charts and renderers (SVG, PNG, HTML).
- `ChartForgeX.Visuals/`: `VisualBlocks` (tables, lists, KPI cards),
  `VisualCanvas`, `VisualArtifacts` and composition.
- `ChartForgeX.Stories/`, `ChartForgeX.Mermaid/`, `ChartForgeX.Markup/`,
  `ChartForgeX.Markup.Mermaid/`, `ChartForgeX.Markup.Cli/`: stories, Mermaid
  rendering and the ChartForgeX markup language.
- `ChartForgeX.Interactivity/` and `ChartForgeX.Interactivity.Html/`: browser
  behaviour. JavaScript and CSS live in `ChartForgeX.Interactivity.Html/Assets/`.
- `ChartForgeX.Markup.VSCode/`: VS Code extension for the markup language.
- `ChartForgeX.Tests/`: xUnit tests. `tests/`: Node tests for the browser runtime.
- `ChartForgeX.Examples/` and `ChartForgeX.Examples.Snippets/`: generated gallery
  and snippets. `ChartForgeX.AotSmoke/`: Native AOT check. `Benchmarks/`.
- `ChartForgeX.Tools.IconImport/`, `tools/`, `assets/topology-icons/`: icon
  packs and import tooling.
- `docs/`: focused reference. `TODO.md`: active future work.
- `Website/`: chartforgex.com. It has its own `Website/AGENTS.md`.

## Build and test

```powershell
./Build.ps1 -Configuration Release                        # full quality loop: tests, examples, visual baseline, AOT, packs
dotnet test ./ChartForgeX.sln -c Release                  # faster loop for code-only changes
./Build.ps1 -Configuration Release -UpdateVisualBaseline  # only after inspecting an intended visual change
./Build/Test-BrowserQuality.ps1 -OutputRoot <absolute-dir>   # browser tests (Category=Browser)
node Build/Test-GraphRuntime.js                           # Node tests under tests/
```

`Build.ps1` switches: `-SkipTests`, `-SkipExamples`, `-SkipAot`, `-SkipPack`,
`-QualifyPackages`. Its ordinary test run excludes `Category=Gallery` and
`Category=RasterStress` and runs them as separate contracts.

Targets: library projects build `net472`, `netstandard2.0`, `net8.0` and
`net10.0`; keep all four. Tests, examples and tools target `net8.0`.

Release-only: `Build/Build-Project.ps1` (packages via `Build/project.build.json`).

## Generated files

- `interactive*.js`, `topology-interaction.js` and `diagram-interaction.js` in
  `ChartForgeX.Interactivity.Html/Assets/` are built from the `*.source`
  fragments by `./Build/Sync-InteractiveAssets.ps1 -PowerForgeCliPath
  <PowerForge.Web.Cli.dll>` (`-Check` compares only). Edit the fragments.
- `ChartForgeX.Examples/visual-baseline.json` is the SVG/PNG baseline; refresh
  it only through `-UpdateVisualBaseline`.
- Website gallery and visuals are generated; see `Website/AGENTS.md`.
- Design tokens mirror the HtmlForgeX generated contract.
  `Build/Test-DesignTokenDrift.ps1` checks them (`CHARTFORGEX_TOKEN_SOURCE`
  or a sibling HtmlForgeX checkout); without a source it skips.

## Conventions

- `ChartForgeX` has no runtime package dependencies.
- Static rendering is deterministic and script-free by default. Browser
  behaviour goes in adapter packages such as `ChartForgeX.Interactivity.Html`.
- Don't move dashboard shells, filters, inspectors, data collection, Active
  Directory calculations or product page layout into ChartForgeX. Topology
  stays product-neutral; hosts map their data into `TopologyChart`.
- Exact non-chart facts (tables, lists, KPI cards) belong in `VisualBlocks`,
  not in chart series.
- Reuse the shared helpers for colour math, mark surfaces, line layers, text
  fitting, map surfaces and validation instead of renderer-local copies.
- Keep SVG and PNG output in parity for visual changes.
- Add smoke tests for new renderer behaviour and update generated examples
  when visible output changes.
- Source files stay under 800 lines (`RepositoryQualityTests`); graph explorer
  runtime fragments (`graph-explorer*.js`) under 350 (`Build.ps1`).
- No `NoWarn` suppressions.
- Breaking changes are fine when they give a cleaner architecture or simpler
  API. Write a migration note instead of a compatibility layer.
- Keep `README.md` as the entry point, reference in `docs/` and future work in
  `TODO.md`. Don't add roadmap or handoff documents; remove "next" wording
  once the work lands.
- Commit titles are plain imperative sentences.

## Gotchas

- Clone with `core.longpaths=true` on Windows; some
  `assets/topology-icons/` paths exceed 260 characters.
- `Build/Test-BrowserQuality.ps1` requires an absolute, task-owned
  `-OutputRoot`.
- `CHARTFORGEX_REAL_IMAGE_SOURCES` enables optional real-image input tests.
