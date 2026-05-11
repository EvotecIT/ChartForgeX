# Agent Guidance

ChartForgeX is a dependency-free rendering engine. Keep changes generic, testable, and reusable across SVG, HTML, PNG, reports, dashboards, email, documentation, and static site output.

## Boundaries

- Keep `ChartForgeX` free of runtime package dependencies.
- Keep static rendering deterministic and script-free by default.
- Put host-specific browser behavior in adapter packages such as `ChartForgeX.Interactivity.Html`.
- Do not move dashboard shells, filters, inspectors, data collection, Active Directory calculations, or product-specific page layout into ChartForgeX.
- Keep topology product-neutral. Host projects map their own data into `TopologyChart`.
- Keep non-chart exact facts in `ChartForgeX.VisualBlocks`; do not force tables, lists, or KPI cards into chart series.

## Change Expectations

- Add or update smoke tests for new renderer behavior.
- Add or update generated examples when a change affects visible output.
- Preserve SVG and PNG parity for visual polish.
- Preserve `net472`, `netstandard2.0`, `net8.0`, and `net10.0` support.
- Breaking changes are allowed when they produce a cleaner architecture, better usability, or a simpler public API. Prefer intentional migration notes over compatibility layers that preserve awkward designs.
- Do not add `NoWarn` suppressions.
- Keep production source files under the repository line-budget expectation.
- Prefer shared helpers for color math, mark surfaces, line layers, text fitting, map surfaces, and validation instead of local renderer copies.

## Documentation

- Keep `README.md` readable as the product and developer entry point.
- Keep durable reference material in focused files under `docs/`.
- Keep active future work in `TODO.md`.
- Do not create extra roadmap, handoff, or "next work" documents for ordinary follow-up items.
- Remove stale "next" language once the described work has landed.

## Validation

Use the repository quality loop before trusting visual or packaging changes:

```powershell
./Build.ps1 -Configuration Release
```

Use the faster loop for code-only changes:

```powershell
dotnet test .\ChartForgeX.sln -c Release
```

When examples or visual baselines intentionally change, inspect the generated gallery before refreshing:

```powershell
./Build.ps1 -Configuration Release -UpdateVisualBaseline
```
