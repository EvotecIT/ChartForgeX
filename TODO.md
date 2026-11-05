# ChartForgeX roadmap

This is the active product roadmap, refreshed on 1 November 2026. The [README](README.md) introduces the library, the [architecture](docs/v2/architecture.md) defines ownership, the [chart capability ledger](docs/v2/chart-capabilities.csv) records what each family supports today, and the [Mermaid matrix](docs/mermaid-support-matrix.md) records language limits. Completed work leaves this file; durable facts move to those references.

## Where we stand

The chart catalogue is at parity with AG Charts or ahead of it: 54 native series kinds, plus Gantt, Timeline, Lollipop, Dumbbell, Slope, WordCloud, TileMap, CalendarHeatmap and topology, which AG Charts doesn't have. Every Mermaid 11/12 family parses and renders natively. Rendering is dependency-free and produces the same scene in SVG and native PNG.

What holds us back is depth, not breadth: charts are less configurable than AG Charts, standalone HTML interactivity is thin, diagrams still look like engineering output, the three Evotec libraries have drifted apart visually, and the website undersells all of it. The five workstreams below close those gaps. Each item states the outcome and when it counts as done.

## Delivery order

1. Chart configurability and standalone interactivity: these decide whether ChartForgeX competes with AG Charts.
2. Diagram appearance and one shared token contract: these decide whether the output looks professional next to HtmlForgeX and CanopyX.
3. Website showcase: it should follow the first two, so it shows the improved product rather than today's.
4. Mermaid fidelity and later extensions, driven by real consumers.

Workstreams can run in parallel where they don't share owners. One coherent PR per outcome. The ledger's `PriorityLane` column still uses the earlier five lanes: 1 quality gates, 2 public contracts, 3 existing-family depth, 4 interaction and accessibility, 5 specialized extensions.

## 1. Chart configurability

AG Charts lets a caller style any datum, format any label and theme every series type. I want the same reach through our typed API.

- [ ] **Per-datum style callback.** A series accepts a callback that receives the datum, its index and series context and returns fill, stroke, marker and label overrides, replacing index-based `WithPoint*` for conditional styling. Done when it works on every Cartesian and radial family, SVG and PNG agree, and an example colours bars by threshold.
- [ ] **Per-segment line dash.** Series dash (`WithStrokeStyle`) has landed; a forecast still needs a dash per segment after the last actual. Done when legend keys show the dash and a forecast example ships.
- [ ] **Axis control.** Numeric axes accept a step/interval and explicit tick values, minor ticks, per-axis grid style and alternating bands; more than one secondary axis per side where the layout allows. Done when examples show each option and existing tick selection is unchanged when none is set.
- [ ] **Theme defaults per series type.** `VisualDesignTokens` can carry defaults per series kind (line width, marker, bar radius, label style), like AG `theme.overrides`. Done when a theme file restyles every line chart without per-chart code.
- [ ] **Legend control.** Custom legend items, marker shape/size config and an item formatter, alongside the existing eight positions. Done when a chart can show a legend entry for a reference line and format entries with values.

## 2. Standalone browser interactivity

Static HTML from reports and email has to feel interactive without a .NET host. Feature bundles (context menu, data export, hierarchy drill, isolate, motion), re-projected standalone zoom with fresh ticks, brush zoom, legend rescale, entry and update motion, tooltip templates and renderers, CSV/JSON/clipboard export, custom context-menu items, pinch zoom and double-tap isolate have landed.

- [ ] **Standalone zoom for the remaining Cartesian families.** Saved pages re-project automatic UTC calendar time X axes with native calendar ticks. Stacked, normalized and horizontal bars, aligned stacked areas and waterfalls now re-project, and legend toggles restack from the captured observations. Saved pages still decline zoom for zoned, culture-formatted, session and spaced time axes, symmetric-logarithmic axes, secondary axes on horizontal charts, stacked areas whose members miss each other's X coordinates, and lollipop, dumbbell, range-bar and timeline families. Done when each re-projects with native-consistent ticks or the decline is deliberate.
- [ ] **Large graph navigation.** Large topology/graph scenes get zoom, filtering or hierarchy navigation rather than shrinking to fit. Done when a dense example stays readable.
- [ ] **Use case playback.** Response steps reverse the active edge arrow in interactive HTML, and the request/message/response/failed-call labels are translated in the topology UI catalogs.

## 3. Diagram appearance

Charts already look professional; topology-laid-out diagrams don't yet. The defects below were seen on the published atlas renders.

- [ ] **One diagram theme.** Every diagram family uses the Graphite theme, one font stack and one background, instead of Mermaid's lavender flowchart, pastel sequence participants and mixed Calibri/Lato/Open Sans. Done when all atlas renders share fonts, background and palette.
- [ ] **Edge routing that meets its nodes.** Edges end on the node border with visible arrowheads, enter from the expected side (decision branches enter the top), don't run through their own labels and don't stop short. Done for Flowchart, Mindmap, GitGraph, Gantt dependencies and topology.
- [ ] **Proper notation.** Class boxes get compartment dividers and readable member text; ER entities show attribute tables and crow's-feet on the border; GitGraph lanes end at merges with visible connectors; Gantt gets section bands and a legend without repeated rows; C4/architecture fills the canvas, hides internal ids and keeps descriptions whole; database cylinders carry labels.
- [ ] **Trimmed, readable canvases.** Diagrams trim surrounding whitespace and scale titles to the canvas. Venn captions keep clear of other circles' outlines.
- Done when the atlas renders for every family pass a side-by-side review against Mermaid and the visual baselines are refreshed deliberately.

## 4. One look across HtmlForgeX, ChartForgeX and CanopyX

HtmlForgeX's `evotec.tokens.json` is the source of truth, but our embedded copy is older (1.1.0 vs 1.1.2), fonts differ in every product, and tokens carry colours only.

Token contract v2 has landed: ChartForgeX reads its font stack, typography roles, radius, elevation and interactive chrome from the shared export, and interactive CSS resolves `--hfx-*` roles before its literal fallbacks.

- [ ] **Drift check.** CI fails when ChartForgeX's embedded token file differs from HtmlForgeX's generated one. Done when the check runs and the current copy matches.
- [ ] **Host CSS check in HtmlForgeX.** Interactive CSS now reads `--hfx-*` first and no longer carries Tailwind slate fallbacks. Done when a ChartForgeX chart in a real HtmlForgeX page is observed matching the page's surface, font and tooltip style.
- [ ] Coordinate with HtmlForgeX to theme its legacy ApexCharts/Chart.js output from the same tokens, and with CanopyX to point its role tokens at the merged HtmlForgeX roles.

## 5. Website showcase

- [ ] **Interactive gallery.** Gallery cards open a live interactive chart, not a picture. Done when tooltips, zoom and legend work from every card.
- [ ] **Realistic data.** Examples use multi-series, realistically sized data; no near-empty charts. Still thin: Bubble, Error Bar, Candlestick, OHLC, Trend Line, Histogram, Timeline and Bullet.
- [ ] **Multi-ring callouts use the free side.** A compact multi-ring donut shortens its callouts to "Cons… 33.3%" while the space left of the rings stays empty. Done when callouts take the available side before shortening names.
- [ ] **Align explicitly placed value legends.** A pie/donut legend placed above or below the plot by the author keeps natural entry widths, so its value and percentage columns do not line up across wrapped rows. Give those rows shared columns in the native drawing and the responsive layout together.
- [ ] **Per-family pages.** Each family gets a page with variants, options and code, including Quadrant, Org chart, Cone funnel and Linear gauge, and the navigator, context menu and annotation editor appear in at least one example.
- [ ] **Chart-first snippets.** Snippets lead with the chart code; setup plumbing moves out of the way.
- [ ] **Public wording.** Published docs (README development section, visual effects, API conventions, migration, Mermaid matrix) describe what works in plain language, without internal qualification jargon, and the site stops linking to this file as product documentation.

## Mermaid

Grammar coverage is broad; the remaining work is fidelity and honest results. The reference is Mermaid.js 12.2.0, with 10.9.8 and 11.17.2 kept as compatibility lanes.

- [ ] **Theme mapping.** Source `theme`, `themeVariables`, palettes and typography map into the shared diagram theme; Mermaid 12 `look`/`layout` and older `defaultRenderer` get a documented mapping or diagnostic. Done when an authored Mermaid theme visibly changes the native render.
- [ ] **Feature coverage per family.** The conformance corpus grows from representative examples to per-feature fixtures, with malformed-source cases for every family. Done when each matrix row lists its positive and negative fixtures.
- [ ] **Remaining approximations.** Retained-only or approximate features (styles and edge IDs beyond Flowchart/Class/ER/Requirement, safe rich text in the other families, sequence spacing and participant presentation, dense State/ER routing) either render natively or stay listed as limits in the matrix.
- [ ] **Reference comparisons.** Extend the browser comparison against upstream Mermaid from Flowchart to every family, recording intentional differences rather than chasing pixel equality. Each completed contract reaches `MermaidRenderer`, the typed APIs, `ChartForgeX.Markup.Mermaid`, examples and host paths.

## Visual effects

The [visual effects reference](docs/visual-effects.md) describes the implemented finite effects and the proposed wider design.

- [ ] Measure blur cost at 2560×1440 on net472 and net10, and SVG parity for blur, shadow and blend in browsers, Office, Safari and resvg; settle the design's open decisions and per-render budgets.
- [ ] Ship reusable effect presets through `VisualThemeEffects`, then the remaining backdrop effects (frosted glass, tapered strokes, grain, vignette and the rest of the proposal).
- [ ] Adopt effects in charts, diagrams, canvas and stories, then move PowerBGInfo and ImagePlayground onto them. Done when the HUD-wallpaper example renders in SVG and PNG within budget and existing defaults are unchanged.

## Later, driven by real consumers

Financial indicators (moving averages, Bollinger, MACD, RSI); map label placement, clustering and callouts; org chart collapse/expand; topology ports, self/parallel edges and obstacle-aware labels; font fallback and colour fonts with reference fonts; animated WebP/MP4 behind optional adapters. I'll pick these up when a consumer needs them.

## Quality gates

- **Browser lanes.** The required Windows Chromium lane and the macOS Firefox/WebKit lane run for every candidate; required runs reject skipped prerequisites. Settle readouts and transitions before screenshots, and treat intermittent capture differences as their own bug.
- **Packed consumers.** The 16 packed-candidate programs run at the release gate; extend them when a new recipe or package changes. Public feeds and installed hosts are checked separately after publication.
- **Visual acceptance packets.** A visible change ships only after compact and wide, light and dark SVG/PNG output has been looked at. Numerical baselines catch regressions; they don't prove appearance.
- **Recently landed source contracts** that have not yet passed one integrated native/browser/packed run: StackedArea Gap/Zero alignment, DifferenceArea independent sources and nonlinear axes, Waterfall interchange, multi-ring Pie/Donut callouts, Funnel/Pyramid captions, polar forms and partial sweeps, quadrant axis crossings, Git graph directions, Wardley overlays. Run them through `./Build.ps1 -Configuration Release` and inspect the gallery before calling them done.
- **Local glyph-ink browser runs.** On this Windows workstation, RadialLabelTarget, RadialLabelEligibility, RoundedRadial, WaterfallCheckpoint, StaticExportNativeFont owned delivery, WordCloud loaded glyphs and RangeBar endpoint captions fail on `main` itself, while the hosted Windows Chromium lane passes them. The tests wait for SVG `text` that the locally installed fonts turn into hidden owned-font text under glyph ink. Make them read the caption helper (accessible text plus painted ink bounds) so the result no longer depends on the machine.
- **Accessibility and localization.** Test with a real screen reader on supported platforms, and extend locale/RTL coverage beyond the current Arabic fixtures. Accessible names and keyboard contracts are the foundation, not the proof.
- **Performance.** Keep timing out of correctness tests; measure preparation, update latency, allocation, output size and browser response for small, ordinary and dense charts through the PowerForge benchmark lane.

## Consumer and release qualification

The 3.0.0 authoring migration still has to reach its consumers. Candidate-package results don't prove public delivery.

- [ ] PowerBGInfo: chart, topology and factual-canvas workflows on public packages and in the released module.
- [ ] ImagePlayground: moved authoring types, typed series, grids and story exports through the packaged module.
- [ ] OfficeIMO: paired themes and Mermaid on public packages, including Visio and PowerPoint export.
- [ ] Private HTML bridge: host-versus-authored theme ownership and localization on public dependencies.
- [ ] Publish the owner release and affected host releases only with release authority, and read the public packages back before announcing them.
