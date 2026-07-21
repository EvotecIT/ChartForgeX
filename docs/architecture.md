# ChartForgeX Architecture Notes

ChartForgeX should stay easy to extend without letting renderer files become oversized. The library is intentionally dependency-free at runtime, so structure matters more than outsourcing complexity.

## File Size

- Keep production source files under roughly 800 lines.
- Split earlier when a file mixes unrelated responsibilities, even if it is below the limit.
- Prefer small folders by concern over a flat project root.
- The smoke test runner enforces this budget for project source files outside `bin` and `obj`.

## Build Standards

- Treat warnings as errors in every project.
- Generate XML documentation for the core library.
- Do not add `NoWarn` suppressions in project, props, or targets files.
- Keep the core package free of runtime package dependencies; private build-time reference assemblies are allowed only where required for targeting.
- Run repository smoke tests through `dotnet test` so local, CI, and IDE test flows share the same entry point.
- Generate NuGet symbol packages with deterministic library builds.
- Run GitHub Actions on private self-hosted runners only.
- The smoke test runner enforces these project settings so quality does not depend on memory.

## Release Performance Budgets

The release smoke suite measures three synchronous static-rendering workloads after compilation: a 960x540 multi-series chart, a 2x-density 1920x1080 desktop wallpaper, and a dense 128-node/230-edge topology. Each workload must finish within 15 seconds. Managed allocations are capped at 64 MiB for the chart, 384 MiB for the wallpaper, and 256 MiB for the topology. These are regression ceilings rather than benchmark claims; update them only after profiling a deliberate renderer change on the supported CI platforms.

For repeatable local measurements rather than broad smoke-test ceilings, run `./Benchmarks/Invoke-RenderingBenchmark.ps1`. The PowerForge-owned suite records warmups, iterations, output validation, machine metadata, and the measured assembly hash. Keep browser-library comparisons separate: browser-first libraries include startup, layout, paint, and interaction costs that are not equivalent to deterministic .NET SVG/PNG generation.

## Renderer Layout

- Keep each output format in its own folder, for example `Svg`, `Raster`, and `Html`.
- Use partial classes for renderer internals when one renderer naturally has multiple responsibilities.
- Split renderer partials by behavior, such as entry point, axes/layout, series drawing, labels, and helpers.
- Keep the public renderer surface in the main file.

## Interactivity Layout

- Keep static rendering in `ChartForgeX`; it must remain deterministic and script-free.
- Keep host-neutral interaction contracts in `ChartForgeX.Interactivity`.
- Keep host-specific adapters in sibling packages such as `ChartForgeX.Interactivity.Html`.
- Keep reusable scenario, step playback, and deep-link state concepts in `ChartForgeX.Interactivity`; chart families such as topology should map their own domain ids into those contracts instead of moving domain models into adapters.
- Add browser or desktop behavior only through an adapter package, never by making the core HTML renderer require JavaScript.
- Pack adapters separately and validate them from a clean consumer app so package dependency drift is caught before release.
- Keep at least one generated example page for each adapter so interaction work remains visible in the local gallery.
- Let adapter dashboards reuse the same per-chart section and script runtime as single-chart pages so interaction behavior stays consistent.

## Visual Artifact Layout

- Keep product-neutral artifact models such as `VisualArtifact` and `TableArtifact` in the core package when they can render deterministic static previews without runtime dependencies.
- Keep rich interaction, keyboard behavior, clipboard, export workflows, virtualization, and native control binding in host or adapter packages.
- Keep `ChartForgeX.Markup` generic. It owns Markdown fence scanning, built-in ChartForgeX visual parsers, diagnostics, and extension points.
- Keep `ChartForgeX.Mermaid` separate from markup. It owns Mermaid source parsing, AST models, diagnostics, and Mermaid-to-ChartForgeX conversion.
- Keep `ChartForgeX.Markup.Mermaid` thin. It should adapt Mermaid fences and attributes into markup results, not duplicate Mermaid parsing or rendering logic.
- When a visual language cannot map naturally to an existing chart or topology model, introduce a reusable product-neutral artifact model instead of adding product-specific layout rules.

## Public API Layout

- Keep user-facing chart configuration APIs close to `Core`.
- Use focused option/enumeration files for concepts that are likely to grow.
- Add XML documentation for public members as they are introduced.
- Validate public setters and constructors so invalid chart states fail near the caller rather than inside renderers.
- Audit exported enums across every runtime package; identical member sets require one shared public owner rather than adapter-specific aliases and conversion switches.

## Growth Rules

- Add a test with every new chart behavior.
- Add an example when a feature affects visual output.
- Keep warnings as errors enabled and avoid suppressions unless there is a documented reason.
- Prefer SVG fidelity first; PNG is a fallback and should not drive design decisions.
