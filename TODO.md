# ChartForgeX TODO

## Story playback and sharing

- [x] Audit scene, terminal, source, export and consumer boundaries against the current theme baseline.
- [x] Qualify prepared story snapshots, one playback clock, timestamp frames and bounded GIF/APNG producers.
- [x] Qualify source typing, selection, replacement, fixed text size and landscape/square/portrait examples.
- [x] Qualify optional browser playback controls, chapters, keyboard use and accessible transcripts.
- [x] Integrate the prepared frame contract through thin ImagePlayground authoring and replay surfaces.
- MP4 integration is deferred. The prepared frame source provides the future encoder boundary.
- [ ] Settle the Stories owner and dependent consumer PR checks and reviews. Local source and package qualification do not establish public package readiness.

## Consumer migration: Phase 5

All implemented chart kinds, ChartGrid, topology, flow and sequence use the shared native prepared scene. Visuals owns static composition and factual layouts; Stories owns animation and transcripts. The [architecture reassessment](docs/v2/architecture.md#phase-4-reassessment) records owner qualification and its limits. The remaining migration work is downstream execution and release qualification.

- [x] Inspect current owner and consumer branches, package boundaries and the public package state.
- [x] Qualify the PowerBGInfo candidate with installed PowerShell 5.1/7 module types and observed wallpapers.
- [x] Qualify the OfficeIMO candidate with saved-document, accessibility, placement and editable-diagram fixtures.
- [x] Qualify the private HTML and reporting candidates with isolated package consumers and generated report layouts.
- [x] Qualify ImagePlayground's managed-imaging candidate with installed PowerShell 5.1/7 static, composition, topology, story and terminal workflows against the integrated local owner packages.
- [x] Prepare unsigned eight-package release archives and matching source/archive API bundles.
- [x] Complete consumer visual acceptance with coherent wallpaper and image themes, readable compact HTML, truthful sparse report states, and observed saved/native Office delivery. Native document projection retains its documented reflow and font-substitution limits.
- [ ] Settle owner and consumer PR review/CI and merge qualified candidates in dependency order. Public-package-only consumer checks retain their feed-availability gate.
- [ ] With separate release authority, publish and verify owner packages, then repin and release consumers against verified public three-part versions. Owner source and local packs do not establish downstream execution.

## Rendering Pipeline

- Keep Graphite light/dark SVG and PNG pairs in the visual baseline; review new family geometry against [the approved look](docs/design/chart-look-spec.html) before changing it.
- Financial marks expose independent body/wick and OHLC colors, alpha, widths, dash patterns and phase, plus hollow candles through `ChartSeries.Financial`. Trading-session spacing remains separate work; session calendars must preserve source prices and date bounds across candle/OHLC views.
- Preserve explicit named effect styles and test colour roles, small-label contrast and SVG theme switching when palettes or filled marks change.
- Cartesian plot regions provide passive, bounded background guidance and arbitrary quadrants with primary/secondary axes, interior captions and shared SVG/PNG geometry. Mermaid quadrant conversion remains separate work.
- Keep annotation captions clear of data marks in compact plots, including bar caps and nearby value labels.

SVG and PNG charts share measured label placement, including data labels, targets, annotations, maps, topology and axis thinning. The light and dark `label-placement-*` gallery fixtures protect collisions, accessible dropped values and dashboard density. Keep these fixtures and the rendering benchmark budgets current as renderer behavior grows.

- Continue reducing raw/string SVG render paths where shared writer or element-tree helpers make the renderer safer and easier to test.
- Reduce repeated paint attributes and scoped-ID payload in dense prepared SVG while preserving complete source alternatives and embedding isolation; compare raw and compressed output sizes.
- Keep path geometry helpers independent of SVG serialization so PNG parity remains intact.
- Preserve existing SVG contracts while migrating internals: ids, `data-cfx-role`, data attributes, selected/highlight classes, href behavior, title tooltips, accessibility metadata, and deterministic output.
- Dense polylines stroke about 1.3-1.7x slower than the old per-segment quads (thousands of points, with every scanline crossing many outline edges), while gridlines, axes, and topology edges are faster. If dense charts need it, skip join pieces whose outer wedge is under a sub-pixel and emit fill edges from the stroker without intermediate piece lists.
- Raster typography gaps: full-mode stem fitting handles narrow straight stems; font instruction programs, contour-point anchors and script-specific curved/diagonal stem fitting remain unsupported. Extend axis mapping beyond supported `avar` version 1 with a reference-backed `avar` version 2 case.
- Text layout extensions: extend script-specific syllable rules beyond the supported Indic, Thai/Lao, Khmer, Sinhala and modern Myanmar font models with reference-font proof before claiming support. Explicit OpenType language tags select font-localized forms; automatic culture-to-language mapping is not provided. Font-authored GPOS device corrections use the rounded logical font size, and variation positioning follows the selected font instance; raster export resolution preserves both layouts.
- Colour-font extensions: apply non-default COLR v1 paint variation when applications need it. Extend embedded bitmap decoding beyond PNG, JPEG, uncompressed RGB TIFF and horizontal raw EBDT images only with a concrete reference-font case; component EBDT images and scaled EBSC strikes retain outline fallback.
- Use the PowerForge rendering benchmark history to establish tighter cross-platform CI thresholds only after enough runner evidence exists to avoid machine-specific gates.
- Static SVG and PNG charts are drawn in one time zone (UTC or `ChartAxis.TimeZone`) and cannot follow the reader's zone; hosts with a local/UTC switch label them as fixed. Following the reader needs a browser-side redraw of time-axis ticks, labels, and tooltips (for example from the `data-cfx-start`/`data-cfx-end` instants) in `ChartForgeX.Interactivity.Html`.
- Graphite colour roles cover flat marks, surfaces, guides, ramps and readable ink across light/dark SVG switching. Continue extending variable mapping for the derived gradients in named effect themes; their surface blends can still require regeneration when switching themes. The interactive graph explorer (`ChartForgeX.Interactivity.Html`) still names its arrow markers after the edge colour.

## Interactivity

- Keep super-topology parity aligned with `docs/super-topology-parity.md`: preserve the `TopologyChart` static/export owner, route large interactive exploration through `GraphScene`, and validate parity claims across API, generated artifacts, smoke tests, and performance evidence.
- Broaden rich tooltip payloads with chart-family-specific diagnostics where renderers already expose useful `data-cfx-*` attributes, while keeping the static SVG output script-free.
- Broaden pinned tooltip, nearest-point crosshair, brush-to-lasso selection, one-series focus, selected-target compare, and keyboard traversal coverage across more chart families as the shared rendered-target contract grows.
- Continue graph explorer hardening beyond SVG/Canvas/WebGL rendering, worker physics, live release/reheat dragging, compact large-scene documents, graph-native hierarchy navigation, deterministic static stage exports, adaptive clustering, atomic patches, persisted interaction state, cross-renderer box selection, group transforms, bounded undo/redo, and the 1k/2k/5k/10k fixtures with OffscreenCanvas rendering hardening and automated cross-platform CI performance budgets.
- Extend the typed vis-style compatibility layer with more option families and import/export helpers so C# users can migrate common examples without learning a second model first.
- Broaden graph node and edge styling beyond the current parity tranche with selected/hover styles, middle arrows, self-reference polish, edge scaling, and relationship-specific styling.
- Extend graph clustering beyond explicit, group-derived, and deterministic structural communities with hub/outlier clustering, zoom-driven clustering, host predicates, and saved cluster-state import.
- Extend synchronized dashboards beyond viewport, selection, hover, keyboard traversal, brush, crosshair, lasso, series focus, compare markers, scenario playback, and opt-in state bookmarks into named multi-chart review presets across mixed chart types.
- Add playful but report-safe interaction presets beyond the opt-in focus trail, scenario-step trail integration, reveal labels, and route progress; keep future route-tour controls opt-in in `ChartForgeX.Interactivity.Html`.
- Design a production table-interaction adapter only when there is a real host requirement for search, sort, filter, selection, copy, export, paging, or virtualization. Keep demos and generated proof output in `ChartForgeX.Examples` or docs, not in library packages.
- Keep generated interactive examples in the gallery for single charts, mixed dashboards, and topology routes so browser-visible behavior is reviewed before release.

## Chart Catalog

- Keep marketing/poster chart matrices honest by checking each advertised family against public API, SVG renderer, PNG renderer, smoke tests, generated examples, and website gallery tags.
- When adding a future chart family, update the README catalog, public model/API, SVG and PNG renderers, smoke tests, generated examples, gallery metadata, and promotional imagery together.

The [family capability ledger](docs/v2/chart-capabilities.csv) is the option roadmap. Named stacks, signed normalization, interpolation/step placement, explicit funnel forms/orientation, pyramid partitions, typed Waterfall openings/checkpoints and orientation, shared Sunburst/Treemap hierarchy items, explicit Sunburst parent totals, Sankey alignment/order/node geometry/paint, shared Sankey/Chord flow styles and states, and numeric color scales for Scatter/Bubble observations, maps, hierarchies, rectangular/hexagonal matrices and calendars share native owners. Further work should retain that API and source-value boundary:

- Difference areas retain two named authored values with separate positive/negative fills at linear crossings, explicit gaps and independent primary/secondary value axes. Smooth/Step crossing semantics, nonlinear scales and nullable paired ingestion remain separate work; ordered range-area intervals keep their existing contract.
- Extend Sunburst with further label fitting and sector padding controls. Opt-in HTML root-branch emphasis, rounded sectors, typed secondary captions, shared hierarchy facts, independent numeric colors, leaf aggregation and inclusive authored parent totals are implemented. Automatic caption shrinking remains a separate fitting policy.
- Measured left/right/center labels and inside/outside edge policies are implemented. Any optional tiny-flow minimum-width treatment must preserve truthful weights and conservation; current alignment, ordering, node geometry and fill controls keep one proportional scale.
- Full-turn radial columns combine with radar Line/Area through the existing series calls, shared categories and independent value axes. `XAxis.WithReversal()` reverses standalone and mixed radar categories, continuous Polar angles and PolarArea category slots while retaining source identities. Broader radial mixtures, partial mixed sweeps, PolarArea radius reversal and schedule reversal retain separate projection work.
- Range bars and box plots group multiple series with vertical or horizontal value direction. Range bars also support authored Start/End captions. Each horizontal family uses the primary value axis independently; horizontal mixtures and secondary value axes retain separate slot and axis work. Box plots retain interpolated quartiles and min/max whiskers; optional IQR/outlier policies and separate per-statistic data labels remain open.
- Extend the shared numeric color owner with arbitrary interior captions. Absolute positioned stops, Scatter/Bubble color values, automatic, fixed and partly inferred domains, missing-value paint, measured keys, typed dimension names/formats, independent key visibility, continuous gradient or sampled/exact-band keys and raw readouts use the existing shared scale; Bubble size remains independent.
- Extend weighted Chord with deliberate ordering strategies and an independent quantitative color dimension. Its native scene retains directed reciprocal, parallel, self, and raw zero flows in source order; circular span, node gaps/thickness, flow paint/state, target cues, and measured labels are configurable.
- [ ] Add data-domain zoom policies to the HTML adapter; current zoom transforms the complete SVG. Typed crosshair line paint and label visibility are implemented without enabling features. Static legend display and browser muting/isolation have separate controls. Any future hide-versus-mute policy must be explicit and independent of palette choice.
- [ ] Qualify each increment with compact/wide light/dark SVG and native PNG, then maintain the ledger's remaining gaps and evidence limits.

## Topology

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

The [support matrix](docs/mermaid-support-matrix.md) records current parsing, rendering limits, upstream references and open owner dependencies. Thirty families have native render paths; that breadth does not establish complete grammar or visual fidelity. ZenUML, Agentflow and Railroad are diagnostic-only. The current conformance reference is Mermaid.js 12.1.0.

The compatibility goal is to adopt newer documented Mermaid syntax while preserving supported older syntax through regression fixtures. Start with explicit Mermaid 10, 11 and 12 reference versions; qualify the promised window before publishing it. Keep one semantic model where old and new spellings mean the same thing. Record deliberate grammar changes and migration guidance rather than maintaining separate renderer forks. Older majors and beta syntax need evidence for their individual contracts.

The stages set the default work order. Independent family fixes can proceed once their own fixtures and owner APIs are available; chart/calendar correctness does not depend on completing unrelated graph notation.

### Active owner PRs

The following implementation PRs are open as of 12 October 2026. Each owns a distinct part of the remaining roadmap. Their local qualification does not establish merged support or public package availability; review and CI remain gates for the exact published candidate.

| Active owner | Reusable contribution | Remaining qualification or scope |
| --- | --- | --- |
| [#295](https://github.com/EvotecIT/ChartForgeX/pull/295) | Native sequence presentation through the prepared sequence owner | Complete the sequence grammar and visible notation gaps listed above without losing unsupported source. |
| [#296](https://github.com/EvotecIT/ChartForgeX/pull/296) | Shared HTML sizing that preserves readable sequence text | Qualify compact host scrolling, explicit fit sizing and export behavior. |
| [#297](https://github.com/EvotecIT/ChartForgeX/pull/297), [#311](https://github.com/EvotecIT/ChartForgeX/pull/311) | Native source font families and sizes across chart, topology, sequence and block output | Qualify compact/dense content and explicit host precedence; broader color and theme-variable mapping remains open. |
| [#300](https://github.com/EvotecIT/ChartForgeX/pull/300), [#301](https://github.com/EvotecIT/ChartForgeX/pull/301), [#304](https://github.com/EvotecIT/ChartForgeX/pull/304), [#305](https://github.com/EvotecIT/ChartForgeX/pull/305), [#306](https://github.com/EvotecIT/ChartForgeX/pull/306), [#307](https://github.com/EvotecIT/ChartForgeX/pull/307), [#308](https://github.com/EvotecIT/ChartForgeX/pull/308), [#310](https://github.com/EvotecIT/ChartForgeX/pull/310) | Gantt dependencies, date/axis/tick formats, markers, guide strokes and explicit calendar anchors | Settle the dependency-ordered chain with resolved-date, native export, host and resource-bound proof. |
| [#313](https://github.com/EvotecIT/ChartForgeX/pull/313) | Sankey source geometry and value-label settings over the native flow owner | Qualify CSV identities, source precedence, Unicode affixes and visible labels. |
| [#318](https://github.com/EvotecIT/ChartForgeX/pull/318), [#321](https://github.com/EvotecIT/ChartForgeX/pull/321) | Packet row/bit-number settings and shared packet geometry | Qualify dimensions, fields crossing rows and source/host precedence. |
| [#337](https://github.com/EvotecIT/ChartForgeX/pull/337) | Horizontal XY line geometry through the shared Cartesian orientation owner | Qualify continuous numeric/time categories, reversal, labels, zones and both orientations. |
| [#339](https://github.com/EvotecIT/ChartForgeX/pull/339) | Treemap value-caption visibility through the merged hierarchy owner | Complete remaining source classes/styles and value formatting; keep hierarchy construction in the shared owner. |
| [#341](https://github.com/EvotecIT/ChartForgeX/pull/341), [#342](https://github.com/EvotecIT/ChartForgeX/pull/342) | Closed radar curves, graticules, dimensions, margins and native polar-axis layout | Settle curve/layout proof and complete source theme mapping without duplicating polar geometry. |
| [#346](https://github.com/EvotecIT/ChartForgeX/pull/346) | Flowchart circle/cross endpoint operators and bidirectional markers through shared topology geometry | Current-main native SVG/PNG, Markdown, serialization and versioned edge-type fixtures pass locally. Complete the remaining edge IDs/properties, length and compound-layout contracts separately. |
| [#347](https://github.com/EvotecIT/ChartForgeX/pull/347) | Additional flowchart metadata shape aliases over existing native shapes | Versioned parser and native-shape fixtures cover the mapped aliases. The wider shape catalog, icon/image metadata and shape-specific notation remain open. |

### 1. Compatibility evidence and honest results

- [ ] Expand the conformance corpus from representative examples into feature coverage for every supported family, using upstream documentation and parser regressions with recorded version and provenance.
- [x] Qualify isolated test-time lanes for Mermaid 10.9.8, 11.17.2 and 12.1.0. `compatibility.json` records each fixture's accepted/rejected versions. The Gantt endpoint-calendar expectation records Mermaid 10's different scheduling result explicitly; ChartForgeX follows the current 11/12 result.
- [ ] Compare typed semantic facts in both implementations: nodes, edges, nesting, labels, styles, time calculations, attributes and values. The upstream harness checks syntax for every fixture and selected flowchart/Gantt facts plus class/state/ER identities and directions; extend both upstream and .NET assertions deliberately.
- [ ] Distinguish invalid source, recognized-but-unimplemented features, retained metadata, approximate rendering and exact semantic mapping in parse/render results. Give diagnostics stable identities and original source spans, including through front matter, compact statements and Markdown fences.
- [x] Guard class notes/interactions, state note bodies and ER presentation/grouping statements before node/relationship parsing. Preserve raw source with `CFXM007`; invalid directions and unmatched state/ER boundaries report `CFXM008`. Qualify global directions against observed SVG geometry and native PNG output.
- [ ] Extend statement classification across the remaining family grammar, including compact source forms and typed styling/interaction metadata. Retention warnings do not establish complete notation support.
- [x] Inventory the registered upstream families and header aliases, including Agentflow, the four Railroad dialects and `flowchart-elk`. Known families without native implementations preserve source and report `CFXM002`; unknown headers report `CFXM001`, and the static ELK-header flowchart fallback reports `CFXM003`.

Completion: every advertised feature has versioned positive and meaningful negative fixtures, preserved semantic facts, and an explicit rendering or diagnostic outcome. Parser acceptance alone cannot mark a family complete.

### 2. Shared presentation, layout and host policy

- [x] Parse bounded source configuration into inspectable settings. Space-indented YAML scalar mappings and single-line/multiline legacy `init`/`initialize` declarations share limits and preserve declaration spans; legacy JSON reuses the core reader.
- [x] Qualify frontmatter-before-directive precedence, last declarations and family-scoped theme overrides. Apply native `dark`/`default` palettes and root font stacks where supported; retain unsupported family font keys and other layout, appearance and configuration settings with stable diagnostics.
- [ ] Define a coherent precedence and policy for host presentation overrides, source settings and family defaults through the existing native export owners.
- [x] Apply sequence source theme/font defaults through `SequenceArtifact` and its prepared scene. Explicit host contexts override authored defaults; portable presentation records the actual colors and font. Qualify direct rendering, typed conversion, Markdown fences and native/browser output.
- [x] Map `themeVariables.fontFamily` into existing native typography defaults, retaining the original declarations and qualifying root/variable precedence and unsupported family-key diagnostics through direct and Markdown conversion.
- [ ] Map remaining supported themes, `themeVariables`, palettes, typography, spacing and family options into the shared prepared scene. Source font stacks use the native owners; color variables, font sizes and further family options remain open.
- [ ] Handle Mermaid 12 `theme`/`look`/`layout` changes deliberately. Preserve older `defaultRenderer` declarations with a documented diagnostic or mapping. Keep deterministic CFX layout and appearance explicit; reproducing ELK or browser paint is a separate fidelity goal.
- [ ] Extend the existing safe label/text pipeline for escaped text, entities, multiline labels and Mermaid Markdown strings. Specify literal fallback and diagnostics for HTML labels and math until a native implementation has artifact proof.
- [ ] Apply supported `classDef`, default classes, `class`, `style`, `linkStyle` and edge-ID styling consistently across families. Preserve inheritance and source order; report properties retained without visible effect.
- [ ] Extend shared topology layout for nested compound groups, group endpoints, local direction, ports, self-loops, parallel edges and obstacle-aware label routing where fixtures expose gaps. Preserve identical scene geometry in SVG and PNG.
- [ ] Add a coherent host policy for accepted diagram families, source styling, safe links/assets and bounded source/model/render size. Reuse existing SVG, URI and artwork safety owners; prove script-free output and deterministic rejection without fetching remote assets or invoking source callbacks.
- [x] Keep sequence labels at their logical font size through shared HTML host sizing. Source and prepared artifacts use a contained keyboard-accessible scroll region at compact widths; hosts can explicitly select `FitToWidth`. SVG/PNG scene geometry remains unchanged, and print output fits the page.

Completion: source presentation produces consistent SVG/PNG scenes or specific diagnostics, host policy has a single reusable owner, and compact/dense fixtures remain readable and bounded.

### 3. Core diagram semantics

- [ ] Settle the versioned circle/cross/bidirectional endpoint owner in [#346](https://github.com/EvotecIT/ChartForgeX/pull/346) and metadata shape-alias owner in [#347](https://github.com/EvotecIT/ChartForgeX/pull/347). Complete the remaining shape catalog, icon/image metadata, edge IDs/properties, minimum edge lengths, nested subgraphs and local directions. Keep edge animation and callbacks in optional host adapters.
- [ ] Complete sequence participant creation/destruction, participant boxes, supported message variants, nested parallel/critical branches, branch labels, notes, activations and autonumber behavior. Keep actor menus and links as safe host-facing data.
- [ ] Complete class generics, visibility/static/abstract members, lollipop and two-way relationships, labeled/nested namespaces, notes, styling and safe link metadata.
- [ ] Complete state choice/fork/join notation, concurrent regions, nested composites, multiline notes and classes; keep transition and start/end meaning visible.
- [ ] Complete ER aliases, optional attribute types, keys/comments, direction, subgraphs and styling while preserving identifying relationships and cardinalities.
- [ ] Complete requirement kinds, fields, one-line blocks, relation directions, Markdown labels and classes with typed validation.

Completion: the corresponding matrix rows have grammar, semantic and visible-notation proof through direct rendering and Markdown fences; generic topology previews no longer hide supported diagram meaning.

### 4. Chart and calendar fidelity

- [x] Correct Gantt duration-unit case semantics: `M` means calendar months and `m` means minutes. Shared upstream timestamp fixtures cover month-end/leap-year clamping, fractional units, exclusion calendars and dependent tasks.
- [ ] Qualify hierarchical treemap behavior against the shared owner merged in PR #273; PR #271 is closed after consolidation. Close Mermaid class/style and value-formatting gaps without recreating hierarchy locally.
- [x] Preserve comma-separated treemap magnitudes and native group/leaf captions through typed hierarchy conversion and static direct/fence output.
- [x] Honor treemap value-caption visibility through the native hierarchy owner, source configuration and nullable host overrides while retaining semantic values and labels.
- [ ] Reuse the stable Sankey identity work in PR #261 and the shared XY orientation work in #251, also integrated in #273. Qualify Mermaid label/configuration handling and all series orientations against the settled owner APIs.
- [x] Render closed radar curves through shared path geometry, apply source curve tension and host overrides, and fit cubic overshoot without changing observation values.
- [x] Map radar graticules, dimensions, margins and axis factors through the native chart and polar-grid owners, including curved-series fitting and source/host precedence.
- [ ] Map radar theme variables through the existing native theme and series-style owners.
- [x] Resolve Gantt `until` end references, merge repeated calendar declarations, apply the final diagram calendar/date format and preserve milestone ranges for dependencies. Shared upstream fixtures cover source order, earliest referenced starts and included/excluded dates.
- [x] Resolve forward `after` references without reordering source rows, and render every predecessor through the shared native Gantt dependency owner. Qualify duplicate links, graph cycles, direct conversion and Markdown fences against pinned reference dates.
- [x] Keep Gantt dependency arrowheads visible over successor bars and beside milestone boundaries, including automatic window endpoints; qualify the shared schedule scene in SVG and PNG.
- [ ] Complete Gantt date/axis/tick formats, vertical markers and explicit today-marker behavior. Preserve month/year and excludes/includes/weekend calculations and compare resolved dates against upstream.
- [x] Apply English D3 Gantt axis directives, padding and literal text through the native axis formatter; qualify shared weekday, clock and ISO/week boundary labels independently of host culture.
- [ ] Render journey sections, actors and score paths as a reusable journey scene, quadrant regions and their labels as chart geometry, and timeline sections/events in both directions. Keep the current score-bar/scatter/ordered-time mappings explicit until replaced.
- [ ] Broaden pie/show-data, XY numeric/category axes and legends, Sankey CSV/configuration/colors, and packet row/label/configuration cases, including Mermaid 12 bit-numbering direction.

Completion: values, dates, hierarchy, direction and requested encodings survive conversion and are visibly represented. Existing owner PRs supply shared capability; they do not by themselves complete Mermaid configuration or compatibility proof.

### 5. Existing specialized diagram depth

- [ ] Complete C4 boundary/deployment notation, relationship variants and update style/layout statements; block composites, spans, shapes and styles; and architecture nested boundaries, endpoint-side routing, row/column alignment and safe icon mapping.
- [ ] Replace TreeView's topology approximation with reusable tree-row geometry that supports box-drawing input, descriptions, classes and file/directory icons. Complete mindmap Markdown labels, shapes, classes and icon handling.
- [ ] Complete Event Modeling frame/reset order, relation syntax, typed data tables and swimlane geometry; render Kanban column/task metadata with deliberate board layout.
- [ ] Qualify Venn area/overlap semantics and higher-arity cases before extending the current one-to-three-set preview. Keep research and unsupported-layout diagnostics visible; size metadata is not a proportional rendering guarantee.
- [ ] Complete Ishikawa dense nested-cause layout and Wardley annotation boxes, pipeline styling, inertia and sourcing overlays through `FishboneDiagramBlock` and `WardleyMapBlock`.
- [ ] Complete swimlane nested responsibility groups, use-case notes/business actors/JSON tables/association styles, and Cynefin domain boundaries/cliff notation without introducing host dashboards or product data collection.

Completion: each row's retained-only or approximate features either have tested native scene mappings or remain explicitly documented limits. Inspect actual compact/wide and light/dark SVG/PNG pairs before changing visual baselines.

### 6. Missing language families

- [x] Recognize `agentflow-beta` with retained source and an explicit unsupported-rendering diagnostic through direct and Markdown parsing.
- [ ] Add typed Agentflow flows/global scope, collapsed containers, metadata, connectors and sequence/reference/failure edges. Reuse topology scene primitives; model/instruction/connector metadata is data, never executable work.
- [x] Recognize `railroad-beta`, `railroad-ebnf-beta`, `railroad-abnf-beta` and `railroad-peg-beta` with retained source and an explicit unsupported-rendering diagnostic.
- [ ] Implement one reusable grammar-diagram model with distinct front ends for each railroad notation. Qualify rule references, alternatives, repetition, recursion and bounded layout before enabling rendering.
- [ ] Evaluate ZenUML grammar, licensing and semantic differences against `SequenceArtifact`. Implement a native mapping only after that evaluation; keep the existing diagnostic-only result until then. A JavaScript plugin is not a runtime fallback.

Completion: each new family passes the same source/model/diagnostic/scene/export/fence/version gates as existing families. No new production dependency is implied by this roadmap.

### 7. Consumer and release qualification

- [ ] Propagate each completed contract through `MermaidRenderer`, direct typed APIs, `VisualArtifact` serialization, `ChartForgeX.Markup.Mermaid`, examples and supported CLI/editor/OfficeIMO/HTML host paths. Keep grammar and conversion in `ChartForgeX.Mermaid` and hosts thin.
- [ ] Add observed upstream/CFX render comparisons to the existing gallery workflow through HtmlTinkerX test-time browser tooling. Compare meaning, labels, clipping and routing; record intentional appearance differences instead of treating pixel equality as the compatibility contract.
- [ ] Run focused semantic/artifact tests first, then the full `Build.ps1 -Configuration Release` loop for stable implementation candidates. Qualify all supported target frameworks, Windows/Linux/macOS, Native AOT, package consumers, deterministic output and consequential resource limits.
- [ ] Update the support matrix, generated examples and user-facing compatibility/migration notes with each completed tranche. Keep package/feed evidence distinct from local source and open PRs.
- [ ] Complete PR CI/review settlement, then publish and verify owner packages and dependent hosts only with release authority. Remove superseded validation output and close completed roadmap items after the durable contract is documented.

Completion: the documented support window is backed by source, artifacts, host workflows and package evidence. Implementation, merge, publication and installed-consumer qualification remain separate gates.

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
