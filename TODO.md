# ChartForgeX TODO

This is the central place for active follow-up work. Keep feature ideas here until they are implemented, removed, or promoted into focused reference documentation. Avoid adding separate roadmap or "next plan" documents unless the topic needs a durable technical specification.

## Rendering Pipeline

- Continue reducing raw/string SVG render paths where shared writer or element-tree helpers make the renderer safer and easier to test.
- Keep path geometry helpers independent of SVG serialization so PNG parity remains intact.
- Preserve existing SVG contracts while migrating internals: ids, `data-cfx-role`, data attributes, selected/highlight classes, href behavior, title tooltips, accessibility metadata, and deterministic output.
- Add a performance harness only when it becomes part of CI or a documented release review gate.

## Chart Catalog

- Keep marketing/poster chart matrices honest by checking each advertised family against public API, SVG renderer, PNG renderer, smoke tests, generated examples, and website gallery tags.
- When adding a future chart family, update the README catalog, public model/API, SVG and PNG renderers, smoke tests, generated examples, gallery metadata, and promotional imagery together.

## Topology

- Add denser replication and site-link fixtures that prove routes do not cross node cards in common real-world layouts.
- Tune label-clearance and route-overlap weights with real dense examples.
- Continue growing the dependency-free inline SVG raster layer for topology PNG artwork: reusable diagnostics and richer text shaping should be added through typed parser/renderer stages rather than ad hoc string handling.
- Turn the stencil browser into a small builder demo only if the reusable contract is a `TopologyChart` export, not host UI state.
- Keep vendor icon-pack provenance, license notes, source revision, category counts, skipped-file diagnostics, and unsafe-SVG findings in generated import reports.
- Improve geographic label placement, route arc trimming, clustering, and callout placement through generic fixtures.
- Promote topology artifacts into `visual-baseline.json` only after dense routing and geographic layout polish are stable enough for numeric baselines.
- Keep dashboard shells outside ChartForgeX; host projects such as HtmlForgeX and TestimoX should own sidebars, filters, inspectors, cards, and collected product data.
- Unify topology HTML interaction code with `ChartForgeX.Interactivity.Html` only where it reduces duplicated event/control code without changing the static default.

## Visual Blocks

- Broaden table, list, and metric-card style presets from real PowerBGInfo, ImagePlayground, email, Word, and wallpaper examples.
- Add small icon/status symbol options only when they stay renderer-owned and dependency-free.
- Add grouped capsule-bar polish only if repeated dashboard examples need it outside ordinary grouped `Bar` output.
- Promote shared chips, badges, delta pills, and avatar stacks into reusable primitives only where multiple blocks need the same bounded geometry.
- Add reusable status palettes and compact infographic snippets that reuse shared primitive layout/styling instead of arbitrary markup.
- Add more examples and visual-baseline candidates once layouts stabilize.

## Formats

- Keep SVG as the highest-fidelity static output.
- Keep improving PNG through the dependency-free rasterizer: alpha-correct compositing, downsampling, antialiasing, gradient parity, and text measurement.
- Evaluate future PDF or Office-friendly emitters only when they can share the same chart model, layout rules, and quality standards.

## Release Readiness

- Keep the first-release public surface stable where it represents real charting concepts; make pre-release breaking changes only for clearer naming, stronger typing, dependency boundaries, or product-neutral API design.
- Keep extension-inferred export behavior documented in README; new output formats should update `Save`, raster metadata helpers, and smoke tests together.
- Keep map catalog discovery split between embedded entries and known external entries so hosts can tell package-shipped geometry from user-supplied GeoJSON assets.
- Use GitHub Releases as the release-note source of truth; keep package release notes short enough for NuGet and do not maintain a second long-form repository changelog.
- Keep package license metadata aligned across `ChartForgeX`, `ChartForgeX.Interactivity`, and `ChartForgeX.Interactivity.Html`.
- Update package versions and release metadata in all package projects when preparing a release.
- Run the full quality loop and inspect generated examples before publishing packages.
- Publish `.nupkg` and `.snupkg` files from `artifacts/packages/Release`.
