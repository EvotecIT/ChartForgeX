# ChartForgeX TODO

This is the central place for active follow-up work. Keep feature ideas here until they are implemented, removed, or promoted into focused reference documentation. Avoid adding separate roadmap or "next plan" documents unless the topic needs a durable technical specification.

## Rendering Pipeline

- Continue reducing raw/string SVG render paths where shared writer or element-tree helpers make the renderer safer and easier to test.
- Keep path geometry helpers independent of SVG serialization so PNG parity remains intact.
- Preserve existing SVG contracts while migrating internals: ids, `data-cfx-role`, data attributes, selected/highlight classes, href behavior, title tooltips, accessibility metadata, and deterministic output.
- Line parity gaps the PNG line migration left alone, because they are drawing or colour choices rather than line quality: PNG-only halos on box plot, candlestick, OHLC, error bar, and range band marks, and bar highlights on range, bullet, and waterfall bars; error-bar range opacity (SVG 0.72) and dumbbell connector alpha; hatch overlay opacity; the bullet target length; pie slice separators drawn as radial lines where the SVG strokes each slice outline; the topology route halo width condition and opacity, the chevron arrow size, and the Wardley evolve arrow head and accelerator markers; the fork-and-knife icon. Topology glyph geometry is shared, but PNG still labels database nodes with text where the SVG draws a drum, and draws queue bars for badge-shaped queue nodes where the SVG shows "Q".
- Dense polylines stroke about 1.3-1.7x slower than the old per-segment quads (thousands of points, with every scanline crossing many outline edges), while gridlines, axes, and topology edges are faster. If dense charts need it, skip join pieces whose outer wedge is under a sub-pixel and emit fill edges from the stroker without intermediate piece lists.
- Raster typography gaps: variable fonts render only their default instance (`fvar`/`gvar` and CFF2 blends are not applied). Small-text hinting is vertical only (baseline, x-height, cap height); stem darkening and horizontal stem snapping are not done.
- Complex-script syllable shaping: add Indic feature stages (`nukt`/`akhn`/`rphf`/`blwf`/`half`/`pstf`/`vatu`/`pres`/`abvs`/`blws`/`psts`) and script-specific reordering over the shared GSUB/GPOS glyph engine, so Devanagari, Bengali, Tamil, Thai, and Khmer match the selected font's browser layout. The generic engine already applies composition, localization, contextual/ligature substitutions, Arabic forms, kerning and mark/cursive attachment, including font-provided emoji ZWJ ligatures. Add explicit language selection and size-dependent GPOS corrections where the public text contract needs them.
- Colour emoji: PNG draws the emoji font's monochrome outlines in the text color. Colour needs COLR v0 (per-glyph layer lists of glyph ids and CPAL palette indices, painted in order with the palette's colors, the foreground index drawing in the text color) as a first step; COLR v1 adds a paint graph with linear, radial, and sweep gradients, transforms, and composite modes. Bitmap emoji need CBDT/CBLC (Noto Color Emoji) or sbix (Apple Color Emoji): pick the strike nearest the pixel size, decode its PNG with `PngReader`, and scale it into the glyph box.
- A fallback face lighter than a bold run (Segoe UI Bold drawing Arabic in a Segoe UI Black title) is drawn at its own weight; embolden such glyphs per cluster, with the advance kept, to match a browser's synthetic bold.
- Use the PowerForge rendering benchmark history to establish tighter cross-platform CI thresholds only after enough runner evidence exists to avoid machine-specific gates.
- Static SVG and PNG charts are drawn in one time zone (UTC or `ChartAxis.TimeZone`) and cannot follow the reader's zone; hosts with a local/UTC switch label them as fixed. Following the reader needs a browser-side redraw of time-axis ticks, labels, and tooltips (for example from the `data-cfx-start`/`data-cfx-end` instants) in `ChartForgeX.Interactivity.Html`.
- Colour roles for SVG variables cover lines, bars, histograms, calendars, matrix and hexbin heatmaps with their cell text, state marks and Gantt lane labels, and topology tints, contrast white, and arrow and endpoint markers (keyed by status, muted, or explicit colour order). Still written as literal derived colours: the gradients of funnel, Sankey, tree, treemap, timeline, and Gantt task marks, and the card and plot surface gradients (whose blend depends on the surface luminance, so a drawn card differs between light and dark). The interactive graph explorer (`ChartForgeX.Interactivity.Html`) still names its arrow markers after the edge colour.

## Interactivity

- Keep super-topology parity aligned with `docs/super-topology-parity.md`: preserve the `TopologyChart` static/export owner, route large interactive exploration through `GraphScene`, and validate parity claims across API, generated artifacts, smoke tests, and performance evidence.
- Broaden rich tooltip payloads with chart-family-specific diagnostics where renderers already expose useful `data-cfx-*` attributes, while keeping the static SVG output script-free.
- Broaden pinned tooltip, nearest-point crosshair, brush-to-lasso selection, one-series focus, selected-target compare, and keyboard traversal coverage across more chart families as the shared rendered-target contract grows.
- Continue graph explorer hardening beyond SVG/Canvas/WebGL rendering, worker physics, live release/reheat dragging, compact large-scene documents, graph-native hierarchy navigation, deterministic static stage exports, adaptive clustering, atomic patches, persisted interaction state, cross-renderer box selection, group transforms, bounded undo/redo, and the 1k/2k/5k/10k fixtures with OffscreenCanvas rendering hardening and automated cross-platform CI performance budgets.
- Extend the typed vis-style compatibility layer with more option families and import/export helpers so C# users can migrate common examples without learning a second model first.
- Broaden graph node and edge styling beyond the current parity tranche with selected/hover styles, middle arrows, self-reference polish, edge scaling, and relationship-specific styling.
- Add WebGL label and detail rendering and per-edge width parity with SVG and Canvas, with generated examples and browser proof across all three backends.
- Extend graph clustering beyond explicit, group-derived, and deterministic structural communities with hub/outlier clustering, zoom-driven clustering, host predicates, and saved cluster-state import.
- Extend synchronized dashboards beyond viewport, selection, hover, keyboard traversal, brush, crosshair, lasso, series focus, compare markers, scenario playback, and opt-in state bookmarks into named multi-chart review presets across mixed chart types.
- Add playful but report-safe interaction presets beyond the opt-in focus trail, scenario-step trail integration, reveal labels, and route progress; keep future route-tour controls opt-in in `ChartForgeX.Interactivity.Html`.
- Design a production table-interaction adapter only when there is a real host requirement for search, sort, filter, selection, copy, export, paging, or virtualization. Keep demos and generated proof output in `ChartForgeX.Examples` or docs, not in library packages.
- Keep generated interactive examples in the gallery for single charts, mixed dashboards, and topology routes so browser-visible behavior is reviewed before release.

## Chart Catalog

- Keep marketing/poster chart matrices honest by checking each advertised family against public API, SVG renderer, PNG renderer, smoke tests, generated examples, and website gallery tags.
- When adding a future chart family, update the README catalog, public model/API, SVG and PNG renderers, smoke tests, generated examples, gallery metadata, and promotional imagery together.

## Topology

- Reduce crossings between planned dense routes: the planner charges for a crossing during the search and orders lanes by where routes turn, but it does not yet re-route earlier edges or swap lanes after all routes are known.
- Let planned dense routes share a trunk on request (edges of one kind into one node), for hosts that prefer a bus to separate lanes.
- Widen column gutters, not only row gaps, when many routed edges end in a group; this needs a width budget so wrapped panels still fit the viewport.
- Outside `ReadableDenseLayout`, obstacle-avoiding routes still avoid a left-aligned estimate of the group header instead of the centered block the renderers draw (`TopologyGroupHeader`); switching changes curated examples, so do it together with a gallery review.
- The dense route planner does not see fixed (`Orthogonal`, `Straight`, `Curved`) or waypoint edges; count them as occupied corridors when a chart mixes them with obstacle-avoiding edges.
- `VisualArtifact.NaturalSize` of a topology is the requested viewport, while the rendered canvas is the prepared one (it grows with the content and, with `ReadableDenseLayout` on `DenseGrouped`, also shrinks to it); report the prepared size without feeding it back into `PreserveNaturalSize` renders.
- Tighten the replication fixture time budget (45 s per tier) once CI runner history exists.
- Continue growing the dependency-free inline SVG raster layer for topology PNG artwork: extend loss diagnostics to additional unsupported paint and text behavior, and add richer text shaping through typed parser/renderer stages rather than ad hoc string handling.
- Keep vendor icon-pack provenance, license notes, source revision, category counts, skipped-file diagnostics, and unsafe-SVG findings in generated import reports.
- Improve geographic label placement, route arc trimming, clustering, and callout placement through generic fixtures.
- Keep every generated topology SVG/PNG pair in the shared numeric visual baseline; add dense routing and geographic fixtures to that gate whenever those renderers grow.
- Keep dashboard shells outside ChartForgeX; host projects such as HtmlForgeX and TestimoX should own sidebars, filters, inspectors, cards, and collected product data.

## Visual Blocks

- Broaden table, list, and metric-card style presets from real PowerBGInfo, ImagePlayground, email, Word, and wallpaper examples.
- Add small icon/status symbol options only when they stay renderer-owned and dependency-free.
- Add grouped capsule-bar polish only if repeated dashboard examples need it outside ordinary grouped `Bar` output.
- Promote shared chips, badges, delta pills, and avatar stacks into reusable primitives only where multiple blocks need the same bounded geometry.
- Add reusable status palettes and compact infographic snippets that reuse shared primitive layout/styling instead of arbitrary markup.
- Add examples when they protect a real host scenario, and place every stable SVG/PNG pair in the shared numeric visual baseline.

## Mermaid

- Keep `docs/mermaid-support-matrix.md` current as the family-by-family completion contract.
- Continue expanding Mermaid support through typed AST models plus Mermaid.js-backed conformance fixtures before advertising a family as implemented.
- Broaden class/state/ER/mindmap/kanban syntax coverage from Mermaid documentation examples, preserving raw statements where static CFX rendering cannot yet match Mermaid exactly.
- Harden C4 with richer boundary/deployment examples and deliberate handling for Mermaid update style/layout statements now that a typed C4-to-topology model exists.
- Harden Venn with area-proportional/Euler layout research and broader style fidelity while keeping the current one-to-three-set `VennDiagramBlock` deterministic and dependency-free.
- Harden Ishikawa/fishbone rendering with Mermaid layout/style parity and richer nested cause examples while keeping `FishboneDiagramBlock` product-neutral.
- Harden Wardley map rendering with Mermaid browser visual parity, annotation-box rendering, pipeline styling, sourcing-strategy overlays, and broader grammar examples while keeping `WardleyMapBlock` product-neutral.
- Harden TreeView rendering with Mermaid browser visual parity, row indentation fidelity, directory/file styling, and configuration/theme mapping while keeping the implementation on reusable topology contracts.
- Harden Event Modeling rendering with Mermaid browser visual parity, richer relation syntax, data block display, and Event Modeling-specific swimlane styling while keeping the implementation on reusable topology contracts.
- Keep recognized diagnostic-only handling current with Mermaid families before adding renderers.
- Harden git graph rendering with Mermaid config/theme support, alternate orientation rendering, and more docs examples now that the reusable git graph model exists.
- Harden block rendering with nested/composite blocks, full shape fidelity, and style/class application now that the reusable block layout model exists.
- Harden architecture rendering with richer nested-group boundaries, endpoint-side routing, and icon-specific visual styling now that the typed model exists.
- Keep runtime packages JavaScript-free; Mermaid.js belongs only in test-time compatibility fixtures.

## Formats

- Keep SVG as the highest-fidelity static output.
- Keep dependency-free PNG fidelity aligned with SVG through contract fixtures for alpha-correct compositing, downsampling, antialiasing, gradients, and TrueType text measurement.
- Keep animated raster export format-neutral internally so future chart families and formats can reuse sampled RGBA frames instead of topology-specific code.
- Evaluate animated WebP only if a dependency-free encoder can share the same frame pipeline and meet the GIF/APNG validation bar.
- Treat MP4 as a likely adapter concern unless a dependency-free encoder is practical; core ChartForgeX can expose deterministic frames while host packages own platform codecs or external tooling.
- Keep PDF, Office document, and Visio generation in OfficeIMO adapters over `VisualArtifact` and its semantic interchange envelope; expand the contract only for a proven host requirement.

## Release Readiness

- Keep the first-release public surface stable where it represents real charting concepts; make pre-release breaking changes only for clearer naming, stronger typing, dependency boundaries, or product-neutral API design.
- Keep extension-inferred export behavior documented in README; new output formats should update `Save`, raster metadata helpers, and smoke tests together.
- Keep map catalog discovery split between embedded entries and known external entries so hosts can tell package-shipped geometry from user-supplied GeoJSON assets.
- Use GitHub Releases as the release-note source of truth; keep package release notes short enough for NuGet and do not maintain a second long-form repository changelog.
- Keep package license metadata aligned across `ChartForgeX`, `ChartForgeX.Interactivity`, and `ChartForgeX.Interactivity.Html`.
- Update package versions and release metadata in all package projects when preparing a release.
- Run the full quality loop and inspect generated examples before publishing packages.
- Publish `.nupkg` and `.snupkg` files from `artifacts/packages/Release`.
