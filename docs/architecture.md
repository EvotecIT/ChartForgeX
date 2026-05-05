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

## Renderer Layout

- Keep each output format in its own folder, for example `Svg`, `Raster`, and `Html`.
- Use partial classes for renderer internals when one renderer naturally has multiple responsibilities.
- Split renderer partials by behavior, such as entry point, axes/layout, series drawing, labels, and helpers.
- Keep the public renderer surface in the main file.

## Interactivity Layout

- Keep static rendering in `ChartForgeX`; it must remain deterministic and script-free.
- Keep host-neutral interaction contracts in `ChartForgeX.Interactivity`.
- Keep host-specific adapters in sibling packages such as `ChartForgeX.Interactivity.Html`.
- Add browser or desktop behavior only through an adapter package, never by making the core HTML renderer require JavaScript.
- Pack adapters separately and validate them from a clean consumer app so package dependency drift is caught before release.
- Keep at least one generated example page for each adapter so interaction work remains visible in the local gallery.
- Let adapter dashboards reuse the same per-chart section and script runtime as single-chart pages so interaction behavior stays consistent.

## Public API Layout

- Keep user-facing chart configuration APIs close to `Core`.
- Use focused option/enumeration files for concepts that are likely to grow.
- Add XML documentation for public members as they are introduced.
- Validate public setters and constructors so invalid chart states fail near the caller rather than inside renderers.

## Growth Rules

- Add a test with every new chart behavior.
- Add an example when a feature affects visual output.
- Keep warnings as errors enabled and avoid suppressions unless there is a documented reason.
- Prefer SVG fidelity first; PNG is a fallback and should not drive design decisions.
