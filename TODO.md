# ChartForgeX roadmap

This is the active product roadmap. The [README](README.md) introduces the library, the [architecture](docs/v2/architecture.md) defines ownership, the [chart capability ledger](docs/v2/chart-capabilities.csv) records family options, and the [Mermaid matrix](docs/mermaid-support-matrix.md) records language and translation limits. Keep those references current rather than accumulating completed tasks here.

## Current position

Assessment refreshed on 20 October 2026. The shared native renderer and package separation are implemented. The unified authoring contracts use the coordinated 3.0.0 package family. Source acceptance, public package availability and installed-host qualification remain separate gates.

Calendar and Gantt correctness work preserves measured caption gaps, authored tick ceilings, regular calendar grids and culture-neutral compact dates. Cartesian stack totals and outside bar captions use separate lanes from axis captions, including lower value edges under reversal. The remaining narrow-axis cases are tracked below.

| Area | Implemented foundation | Remaining work |
| --- | --- | --- |
| Rendering and ownership | Typed models prepare a detached scene for SVG/native PNG; common frame, theme, typography, labels, semantic regions, and artifact handoff; runtime-dependency-free core across four target frameworks | Complete downstream migration of the unified contracts; whole-chart visual fidelity needs stronger independent proof |
| Chart catalog | Broad Cartesian, radial, financial, hierarchy, relationship, map, and gauge coverage; all 54 native series kinds explicitly mapped in the option ledger, alongside compound workflows | Existing family options, supported mixtures/orientations/axes, and data lifecycle need depth; a family name is not a complete capability contract |
| API usability | One immutable paired theme, retained series handles with construction-time IDs, typed specialized observations including maps/calendars, atomic configuration callbacks and data replacement, row selectors, common exports, a supported combination matrix, and standalone compiled recipes | Complete downstream migrations and coordinated package delivery; extend supported combination proof where real use exposes gaps |
| Visual appearance | Graphite light/dark defaults, measured typography, compact/wide examples, shared mark geometry and color owners; readable ordinary axes, numeric keys, tile codes and inherited Sunburst captions; neutral outlines for color-scaled Scatter/Bubble; explicit independent panel themes | Qualify difficult labels, sparse/dense data, mixed hosts, and complete SVG/PNG output beyond numerical health |
| Browser experience | Optional tooltips, selection, legend controls, crosshairs, brush ranges, linked charts, topology exploration, an ordinary-chart mount/update/resize/dispose controller, connected native X windows across the supported numeric/time Cartesian and financial families, explicit primary-Y/XY windows, included-time sessions and overview/range controls, plus native source drawings, opt-in context menus, translated adapter controls and deliberate RTL chrome | Broaden native axis/family policies, host and browser qualification, actual assistive-technology proof and translation coverage. Standalone zoom transforms the whole SVG |
| Relationships and diagrams | Native topology, flow and sequence models; graph exploration; rich node content, routed links, groups, ports and scenarios; shared graph, topology and report control localization | Complete documented notation and layout limits; qualify dense/compound routing, broader locales/fonts, lifecycle and large-scene behavior independently of ordinary charts |
| Factual composition and stories | Separate Visuals and Stories owners for exact tables/lists/metrics, canvases, image composition, ordered motion, transcripts and GIF/APNG | Qualify consumer workflows, composition/effect parity and animation bounds through their actual exporters; these capabilities are outside the native series-kind count |
| Mermaid | Thirty native family paths, typed source-preserving documents, stable located diagnostics for invalid, retained and approximate features, configuration mapping, direct/fence conversion, versioned semantic reference fixtures, and sequence block-boundary validation | Full grammar/notation fidelity varies; some mappings approximate or retain meaning without visible effects; the corpus needs broader feature and malformed-source coverage |
| Quality and delivery | Cross-platform build workflow, required Windows Chromium lane, bounded macOS Firefox/WebKit compatibility, explicit browser/platform prerequisite skips, AOT smoke, artifact/package checks, versioned numerical visual baselines, and selected packed-consumer fixtures | Numerical health is not visual equivalence; keep packed qualification at the release gate and extend actual assistive-technology and platform/browser proof |

The ledger maps all 54 native series kinds and records implemented options, limits, acceptance fixtures and six data-state dimensions: `EmptyRefill`, `Singleton`, `SparseMissing`, `ZeroConstant`, `LongLabel` and `Dense`. A mapped kind or passing example does not establish complete options, appearance or interaction policies. Representative native SVG/PNG observations apply only to their tested data, dimensions, theme and font. Radar captions use the observed-category union rather than a separate category domain.

Source coverage, API usability, whole-chart appearance, interaction, language fidelity, hosted checks, public delivery and installed-consumer behavior remain separate gates. The assessment does not exhaust every public member or option combination, benchmark competing engines, or qualify arbitrary fonts, actual assistive technologies and physical devices. Visuals, Stories, language adapters and downstream hosts retain their own acceptance boundaries. See the [canvas](docs/visual-canvas.md), [visual-block](docs/visual-blocks.md) and [Stories](docs/stories.md) references for their implemented contracts.

## Delivery order

| Order | Outcome | Dependencies and completion evidence |
| --- | --- | --- |
| 1 | Trustworthy correctness and executed quality gates | Use the required browser lane and located semantic errors as the baseline. Expand measured visual evidence and qualify packed consumers before treating later API or rendering work as delivered. |
| 2 | Qualify and migrate the unified public contracts | Theme, series, observation and option ownership share one vocabulary. Qualify normal examples and consumer workflows, and maintain intentional migration notes. |
| 3 | Professional defaults and depth in existing families | Use the common configuration contracts and family ledger. Qualify complete compact/wide light/dark SVG and PNG, supported mixtures, no-data states, and difficult labels. |
| 4 | Data-domain interaction and accessibility | Build on the implemented mount/update/resize/dispose lifecycle. Extend core-owned projections/ticks/layers and adapter-owned controls; qualify domains, keyboard/touch, exports and actual accessibility. |
| 5 | Deeper language and specialized product lanes | Complete the high-use Mermaid families and visible approximations. Choose advanced map, financial, org, and new-language work from real consumer requirements. |

Mermaid correctness, API design, and quality infrastructure can proceed independently where their owners do not overlap. A pending review or release holds only the dependent step. Keep one coherent implementation PR per requested outcome; the rows above are planning stages, not automatic PR boundaries.

## 1. Correctness and quality evidence

The [quality loop](README.md#development-and-quality-checks) and `Build/Test-BrowserQuality.ps1` cover native and browser contracts, paired output, frameworks, AOT and packed consumers. The required Windows package gate includes net472 execution and CLI installation. Browser/platform prerequisites remain explicit skips in the ordinary loop; required browser proof rejects skips and missing setup. Native touch and browser-emulated accessibility settings do not qualify physical pinch or actual assistive technology.

Use the established PowerForge benchmark/history lane for balanced baseline/candidate comparisons, preserving samples, losses, host activity and variation. Keep correctness/resource invariants in smoke tests and performance measurements separate.

- [ ] Strengthen visual proof beyond numerical health. Current baselines constrain dimensions, node/pixel/color counts, and edge pressure; the browser family matrix measures complete glyph bounds, while the numerical baseline uses anchor positions. Extend targeted geometry/semantic assertions for real regressions and curated observed complete-chart comparisons. Settle readout and transition state before screenshot comparisons, and investigate intermittent capture/acquisition differences separately from static layout. Internal SVG rasterization comparisons need independent browser evidence for shared scene/font defects.
- [ ] Extend actual package execution beyond the selected 16 downloaded programs, and verify public-feed and installed-host behavior separately. The selected eight default-font and eight gallery-font programs execute against the packed candidate on net8.0; the gallery-font programs retain exact PNG bytes and SVG scenes after document-ID normalization. This does not qualify every recipe, public delivery or arbitrary fonts.
- [ ] Review wall-clock budgets in the ordinary smoke suite and measure preparation/update latency, allocation, capacity, output size and browser response separately from correctness/resource invariants.
- [ ] Close the remaining data-state qualification gaps recorded for all 54 native kinds in the ledger. Continue family-specific singleton, sparse/missing, zero/constant, long-label and dense proof where a gap remains. Shared canvas/font/paint health does not establish mark-collision or complete appearance proof.
- [ ] Extend browser qualification beyond the selected ordinary-chart, graph and topology fixtures for forced colors, reduced motion, zoom, print and disposal. Cover remaining host/platform/control combinations, transient layers and active resources; retain representative theme evidence and explicit omissions.

Completion: required checks demonstrably execute, known failure states have decisive regressions, optional omissions are visible, and green health checks are not described as complete appearance or interaction proof.

## 2. API consistency and ease of use

The [API conventions](docs/v2/api-conventions.md), [migration guide](docs/v2/migration.md) and [supported combination matrix](docs/v2/chart-combinations.md) own the implemented theme, identity, observation, configuration, update and export contracts. Native preparation uses one optional Cartesian viewport input, numeric and typed transactions share `ChartObservationChange<T>`, and annotations expose validated membership edits through a read-only collection. Map point `Weight` is separate from signed `ColorValue`. The [keyed batch example](ChartForgeX.Examples/Recipes/KeyedPointChangesRecipe.cs) demonstrates atomic numeric transactions and rolling windows; hosts still own serialized collection, pending queues and refresh cadence. Trend fitting uses centered coordinates and preserves separately retained source observations.

- [ ] Expand combination proof only for supported combinations that need it. Keep sparse/missing observations, category equivalence, horizontal axes and radial forms in the shared preparation owner; do not infer support from two individually supported family names.
- [ ] Qualify real host backpressure, collection cadence and long-running numeric and typed observation updates beyond the bounded synchronous transaction. Extend calculated datasets only with deliberate identity and presentation rules; GraphScene patches retain their own contract.

Completion: a caller can construct, configure, update, theme, and export representative charts through one vocabulary without knowing renderer internals, collection encodings, or series positions. Intentional breaking changes include consumer inventory, focused proof, and a migration path rather than compatibility probes.

## 3. Chart depth and visual standard

The [family capability ledger](docs/v2/chart-capabilities.csv) owns implemented family options, native observations and remaining data-state gaps. Keep chart look choices in shared geometry, paint, label and theme owners; palettes must not select different renderer implementations.

Current limits still matter: crowded labels can shorten, omit or lose distinguishing suffixes; closely spaced Bubble marks can overlap. Polar filled/smooth forms and disconnected runs remain outside current support; broader browser qualification remains open. Selected TileMap temperature views omit all 51 compact codes and show 48 in wide output. Fallback-font, shaping and platform equivalence remain unqualified.

Segmented factual metrics and proportion charts retain source contributors when totals exceed numeric range and report a lower bound. Canvas TechHorizon uses shared boundary geometry and preserves authored halo transparency in native/SVG output; true blur and shadow equivalence remains in the effects work below.

- [ ] Establish a visual acceptance packet for every claimed family: compact/wide × light/dark, representative SVG/native PNG, readable title/ticks/legend/data labels, complete important marks, and understandable no-data/sparse/dense states. Measure collision and truncation where consequential; retain full facts in accessibility and semantic regions. Qualify configured fonts and substitution boundaries explicitly, including browser ink against native header clips; a requested font stack does not identify the actual resolved face, and same-font comparisons do not establish arbitrary fallback-font equivalence.
- [ ] Improve identification where dense authored point captions still shorten after complete single-line and two-line placement. End ellipsis can remove the distinguishing suffix from similar names; retain full source text, explicit placement and numeric meaning while qualifying further fitting policies.
- [ ] Continue useful existing-family depth before adding headline names: hierarchy label fitting/padding, mixed radial projections, and horizontal/secondary-axis combinations. Qualify the implemented BoxPlot, Bubble and gauge options in complete consumer charts and maintain explicit family limits.
- [ ] Extend grouped-category qualification beyond the selected vertical Bar/Line, horizontal Bar, RangeBar, BoxPlot and Waterfall consumers. Linear Cartesian parent paths, contiguous source spans, measured tiers and passive semantics are implemented; selected compact/wide light/dark native views and three-engine context/replacement/window contracts are observed. Broader family mixtures, fonts, densities and platforms remain open. Decide unit/ordinal time, configurable logarithmic bases and negative-only logarithmic behavior separately from the existing Linear, Time, explicit included-session Time, positive base-10 Logarithmic and SymmetricLogarithmic policies; qualify missing values, reversal, ticks, annotations and supported mixtures for each accepted policy.
- [ ] Extend numeric-color qualification across combined categorical/size/color keys, additional short viewports, fonts and hosts. Arbitrary continuous captions and independent Chord flow color are implemented; preserve raw facts, missing values, source identity and independent weight or Bubble size while qualifying further layouts. Sankey quantitative color needs its own deliberate policy.
- [ ] Add deliberate Sankey tiny-flow and hierarchy fitting policies only when actual compact data requires them. Preserve proportional weights, conservation, authored parent totals, source identity, and readable captions.
- [ ] Explore curved arc placement for difficult Sunburst inner-ring names and qualify cases still omitted after measured horizontal/tangential fitting and balanced two-line wrapping.
- [ ] Refine automatic steps for narrow date windows that still select 6-, 8- or 10-day strides. Add measured final data-day caption retention where it fits without shifting regular ticks; preserve authored fixed intervals and source cadence.
- [ ] Improve tight caption gaps below 400 pixels wide, including compact Gantt layouts, while keeping complete captions anchored to their ticks in SVG and native PNG.
- [ ] Qualify TileMap codes at the ordinary 10-pixel floor in compact and dense layouts, preserving explicit font choices and complete source facts when codes are omitted.
- [ ] Qualify small-label contrast, theme switching and mixed-grid export density, preserving explicit effect styles.
- [ ] Review complete report examples with chart grids and factual blocks, including host/embed frames and print. Keep page shells, collection, filters, and inspectors in consuming products; keep reusable block/composition behavior in Visuals and motion in Stories.
- [ ] Qualify remaining graph card/icon typography, image backplates and neighborhood emphasis across SVG, Canvas, WebGL and static exports. Caption placement searches clear points along the painted route before normal offsets; a completely crowded route can still need an explicit association treatment. Targeted paint, image-fit and caption-clearance regressions do not establish complete appearance equivalence.
- [ ] Extend graph readability beyond the qualified compact small-scene fit, painted caption/badge bounds, floating-control clearance and current-frame export cases. Large graphs still need zooming, filtering or hierarchy navigation. Any responsive reflow must preserve authored positions and touched camera state; output dimensions alone do not request a new layout.

Completion: every advertised capability has a usable API, valid native output, a representative example, and visual acceptance; all remaining family limits stay explicit in the ledger.

## 4. Interactive charts

The [interactivity guide](docs/interactivity.md) owns controller lifecycle, identified state, linked views, legend/comparison paging, localization, adaptive hosts, print/export and connected native X-window contracts. Standalone zoom still transforms the SVG. Selected Chromium, Firefox and WebKit observations cover controls, host geometry and lifecycle, including actual WebGL; they do not establish complete appearance, locale/font coverage, actual assistive-technology behavior or physical input.

- [ ] Broaden native-window host and family qualification beyond the supported vertical point, path, financial and paired-boundary families. Explicit primary Y/XY windows, native X overview/presets/scrollbar, declared time-session compression and adaptive full-domain alignment are implemented. Preserve fresh ticks, exact source facts, stable identity, reset/export and shared request cancellation. Secondary-axis Y navigation, visible-X Y autoscaling, horizontal/categorical windows and market-calendar inference need separate deliberate policies.
- [ ] Qualify feature policies across ordinary chart families: legend hide/mute, pinned tooltips, nearest-point lookup, crosshair, brush/lasso, touch, keyboard traversal, and linked views. Keep display/palette choices independent from interaction enablement.
- [ ] Qualify further source-drawing layouts and editor workflows. Independent notes, finite line segments and source-Y-offset channels share native identity, measured captions, clipping and SVG/PNG output with existing axis-spanning guides. Connected source commands add/replace/remove through the same cancellation and native scene-commit owner as viewports. Pointer tools, editing handles, undo/redo and persistence remain consuming-editor work; nonlinear channels do not promise parallel screen boundaries.
- [ ] Exercise actual assistive technologies and complete control/legend/series navigation on representative supported platforms. Accessible SVG names and keyboard contracts are foundations; they do not establish screen-reader behavior alone.
- [ ] Extend localization qualification beyond the implemented ordinary-chart, graph, topology and report message bundles and selected Arabic/RTL fixtures. Audit remaining native, no-data and error paths; qualify required locales, long translations, bidirectional labels, font shaping, focus order, compact hosts, explorer inspectors, state announcements and omission summaries while keeping number/date formatting separate.
- [ ] Decide optional ordinary-chart load/update/legend animation and flash-on-update against real consumer workflows. Keep static output deterministic and script-free and honor reduced motion. The opt-in context menu reuses existing translated reset/export commands, keyboard navigation and chart-owned cleanup; it does not imply animated updates. Stories exports and topology route motion remain separate capabilities.
- [ ] Extend the measured Cartesian acquisition baseline to preparation/update latency, allocation, output size, complete viewport/hover response and browser frame behavior across small, ordinary and dense workloads. Keep large topology measurements separate.
- [ ] Profile and reduce remaining dense-chart bulk-selection and end-to-end interaction costs using representative workloads. The graph adapter releases cached images when their last active scene or pending export consumer leaves; churn, shared URLs, disposal and overlapping exports have focused browser regressions. Extend long-running patch/remount measurements beyond these cache and listener contracts before claiming bounded total memory. Preserve equivalent-work PowerForge comparisons and correctness/lifecycle CI without timing thresholds.
- [ ] Cache invariant display-day offsets when choosing fixed daily Gantt captions; measure long-project preparation while preserving calendar phase and label placement.

Completion: interaction changes data views predictably, controls and targets remain usable after updates, lifecycle is bounded, and accessibility/localization claims have observed evidence.

## Mermaid

The [support matrix](docs/mermaid-support-matrix.md) records current parsing, rendering limits, upstream references and open owner dependencies. Thirty families have native render paths; that breadth does not establish complete grammar or visual fidelity. ZenUML, Agentflow and Railroad are diagnostic-only. The current conformance reference is Mermaid.js 12.2.0.

The versioned corpus and [matrix](docs/mermaid-support-matrix.md#cross-family-gaps) record syntax and selected semantic expectations across pinned Mermaid 10, 11 and 12 references. A separate observed reference-rendering lane checks selected Flowchart captions, styles and compound routes against all three engines. Feature depth remains uneven; those observations do not qualify every family or layout.

The compatibility goal is to adopt newer documented Mermaid syntax while preserving supported older syntax through regression fixtures. Preserve the pinned Mermaid 10, 11 and 12 references and extend feature-level evidence before broadening the promised window. Keep one semantic model where old and new spellings mean the same thing. Record deliberate grammar changes and migration guidance rather than maintaining separate renderer forks. Older majors and beta syntax need evidence for their individual contracts.

The stages set the default work order. Independent family fixes can proceed once their own fixtures and owner APIs are available; chart/calendar correctness does not depend on completing unrelated graph notation.

### 1. Compatibility evidence and honest results

- [ ] Expand the conformance corpus from representative examples into feature coverage for every supported family, using upstream documentation and parser regressions with recorded version and provenance.
- [ ] Add release-specific coverage beyond the qualified 12.2.0 positive corpus: circle/caption bounds, disconnected swimlane order, handoffs and label/routing behavior. Extend observed mindmap layout proof for deep and compact hosts; coloring does not resolve their label-size limits. Preserve the 10.9.8 and 11.17.2 compatibility lanes and recorded semantic differences.
- [ ] Extend malformed-source fixtures beyond the qualified Flowchart, sequence, GitGraph, class, state, ER and packet cases. Direct and Markdown-fence APIs preserve located errors without artifacts. Keep runtime failures distinct from qualified syntax or semantic rejection, and broaden meaningful negative coverage across the remaining families.
- [ ] Compare typed semantic facts in both implementations: nodes, edges, nesting, labels, styles, time calculations, attributes and values. Preserve the implemented flowchart, class/state/ER, sequence, packet, Sankey, treemap, radar, quadrant, configuration, and Gantt assertions; extend uncovered features through both upstream and .NET contracts.
- [ ] Extend diagnostic outcome and source-span coverage across the remaining grammar and compact statements. Stable codes distinguish invalid source, diagnostic-only families, retained statements/settings and deliberate approximate mappings; Journey, Timeline, Venn and C4 report their native mapping limits. Preserve original locations through front matter and Markdown fences as further notation is implemented.
- [ ] Extend statement classification across the remaining family grammar, including compact source forms and typed styling/interaction metadata. Retention warnings do not establish complete notation support.

Completion: every advertised feature has versioned positive and meaningful negative fixtures, preserved semantic facts, and an explicit rendering or diagnostic outcome. Parser acceptance alone cannot mark a family complete.

### 2. Shared presentation, layout and host policy

The playground preserves authored Mermaid themes in both direct source and Markdown fences; the page theme supplies a default for unthemed source. This host rule uses the Mermaid producer's existing configuration metadata.

- [ ] Extend source setting coverage over the shared host presentation and explicit family overrides, preserving native/source/shared-host/family precedence.
- [ ] Map remaining supported themes, `themeVariables`, palettes, typography, spacing and family options into the shared prepared scene. Source font stacks and logical-pixel sizes from 1 through 256 use the native owners; additional color variables, family-specific typography, CSS sizing modes, and further family options remain open.
- [ ] Handle Mermaid 12 `theme`/`look`/`layout` changes deliberately. Preserve older `defaultRenderer` declarations with a documented diagnostic or mapping. Keep deterministic CFX layout and appearance explicit; reproducing ELK or browser paint is a separate fidelity goal.
- [ ] Extend the implemented Flowchart safe-text, multiline/Markdown and inert HTML/math fallback contracts to remaining families. Richer native HTML labels and math need their own artifact proof.
- [ ] Extend the implemented flowchart class/style policy across other supported families and edge-ID styling. Flowcharts resolve default classes, class assignments and direct node colors, plus supported link colors and native dash treatment; unsupported properties retain diagnostics. Preserve family-specific inheritance and source order rather than assuming every notation shares the flowchart rules.
- [ ] Extend qualified nested compound groups, group endpoints and local directions to further ports, self-loops, parallel edges and dense obstacle-aware label routing where fixtures expose gaps. Extend the qualified orthogonal rank-skipping lanes and separated target ports to further dense, reversed and compound layouts; preserve obstacle avoidance and readable edge labels in SVG and PNG.
- [ ] Extend captured source/model/depth/output/pixel policy to family/style/link/asset allowlists and further adversarial grammar coverage. Cancellation remains cooperative, with no hard interrupt deadline. Reuse existing SVG, URI and artwork safety owners; prove script-free output and deterministic rejection without fetching remote assets or invoking source callbacks.

Completion: source presentation produces consistent SVG/PNG scenes or specific diagnostics, host policy has a single reusable owner, and compact/dense fixtures remain readable and bounded.

### 3. Core diagram semantics

- [ ] Complete the remaining shape catalog, icon/image metadata, edge IDs/properties, nested subgraphs and local directions. Qualify authored minimum rank spans in dense, reversed and compound layouts. Keep edge animation and callbacks in optional host adapters.
- [ ] Extend sequence title/lifetime/box proof to additional color forms, message variants, nested parallel/critical branches, branch labels, notes, activations and numbering boundaries. Keep actor menus and links as safe host-facing data.
- [ ] Complete further class generic/member semantics, lollipop relationships, namespace labels, styling and safe link metadata. Preserve qualified base identities, two-way relationships, nested groups and visible notes.
- [ ] Complete state concurrent regions, further dense nested-composite geometry and classes. Preserve qualified fork/join bars, captionless choices and visible multiline notes. Preserve the implemented choice diamond, plain rounded states, filled start dot, ringed end dot and solid transitions.
- [ ] Complete ER optional attribute types, subgraphs and styling; broaden alias/attribute/key/comment/direction fixtures while preserving identifying relationships and cardinalities.
- [ ] Complete requirement kinds, fields, one-line blocks, relation directions, Markdown labels and classes with typed validation.

Completion: the corresponding matrix rows have grammar, semantic and visible-notation proof through direct rendering and Markdown fences; generic topology previews no longer hide supported diagram meaning.

### 4. Chart and calendar fidelity

- [ ] Complete Mermaid treemap classes, styles and value formatting through the shared hierarchy options and retained `HierarchyItems` owner.
- [ ] Qualify Mermaid Sankey labels, CSV identities and configuration through the retained nodes, links and `ReplaceFlowData` owner.
- [ ] Complete quadrant point/class styling, theme variables, source dimensions and separate axis endpoint labels. Native region captions use the shared bounded placement policy.
- [ ] Render journey sections, actors and score paths as a reusable journey scene and timeline sections/events in both directions. Keep the current score-bar/ordered-time mappings explicit until replaced.
- [ ] Broaden pie/show-data, XY named-series legend rules, per-point line labels, numeric/category axes and source configuration, Sankey CSV/configuration/colors, and remaining packet row/label cases.

Completion: values, dates, hierarchy, direction and requested encodings survive conversion and are visibly represented. Shared native APIs are implemented; each language mapping still needs its own configuration, semantic and compatibility proof.

### 5. Existing specialized diagram depth

- [ ] Complete C4 boundary/deployment notation, relationship variants and update style/layout statements; block composites, spans, shapes and styles; and architecture nested boundaries, endpoint-side routing, row/column alignment and safe icon mapping.
- [ ] Replace TreeView's topology approximation with reusable tree-row geometry that supports box-drawing input, descriptions, classes and file/directory icons. Complete mindmap Markdown labels, source shapes, classes and explicit icon handling. Current mindmaps use generic topology boxes and inferred infrastructure glyphs; qualify deliberate mindmap notation and compact/deep layout alongside the implemented hierarchy and color translation.
- [ ] Complete Event Modeling frame/reset order, relation syntax, typed data tables and swimlane geometry; render Kanban column/task metadata with deliberate board layout.
- [ ] Qualify Venn area/overlap semantics and higher-arity cases before extending the current one-to-three-set preview. Keep research and unsupported-layout diagnostics visible; size metadata is not a proportional rendering guarantee.
- [ ] Complete Ishikawa dense nested-cause layout and Wardley annotation boxes, pipeline styling, inertia and sourcing overlays through `FishboneDiagramBlock` and `WardleyMapBlock`.
- [ ] Complete swimlane nested responsibility groups, use-case notes/business actors/JSON tables/association styles, and Cynefin domain boundaries/cliff notation without introducing host dashboards or product data collection.

Completion: each row's retained-only or approximate features either have tested native scene mappings or remain explicitly documented limits. Inspect actual compact/wide and light/dark SVG/PNG pairs before changing visual baselines.

### 6. Missing language families

- [ ] Add typed Agentflow flows/global scope, collapsed containers, metadata, connectors and sequence/reference/failure edges. Reuse topology scene primitives; model/instruction/connector metadata is data, never executable work.
- [ ] Implement one reusable grammar-diagram model with distinct front ends for each railroad notation. Qualify rule references, alternatives, repetition, recursion and bounded layout before enabling rendering.
- [ ] Evaluate ZenUML grammar, licensing and semantic differences against `SequenceArtifact`. Implement a native mapping only after that evaluation; keep the existing diagnostic-only result until then. A JavaScript plugin is not a runtime fallback.

Completion: each new family passes the same source/model/diagnostic/scene/export/fence/version gates as existing families. No new production dependency is implied by this roadmap.

### 7. Consumer and release qualification

- [ ] Propagate each completed contract through `MermaidRenderer`, direct typed APIs, `VisualArtifact` serialization, `ChartForgeX.Markup.Mermaid`, examples and supported CLI/editor/OfficeIMO/HTML host paths. Keep grammar and conversion in `ChartForgeX.Mermaid` and hosts thin.
- [ ] Extend the selected Flowchart upstream/CFX render comparisons to remaining family contracts through HtmlTinkerX test-time browser tooling. Compare meaning, labels, clipping and routing; record intentional appearance differences instead of treating pixel equality as the compatibility contract.
- [ ] Complete PR CI/review settlement, then publish and verify owner packages and dependent hosts only with release authority. Remove superseded validation output and close completed roadmap items after the durable contract is documented.

Completion: the documented support window is backed by source, artifacts, host workflows and package evidence, with the support matrix, examples and compatibility/migration notes kept current. Implementation, merge, publication and installed-consumer qualification remain separate gates.

## Visual effects

The [visual effects design](docs/visual-effects.md) proposes shared effect definitions, theme tokens, SVG profiles, host format policies, kernels, parity rules and phases. The proposed system is not implemented. PowerBGInfo and ImagePlayground hand-build glows, glass and blur; internal canvas glow and terminal shadow also have SVG/PNG differences.

- [ ] Phase 0: benchmark an integer triple-box blur at 2560×1440 on net472 and net10, measure browser parity for blur, shadow and two blend modes, check Office, Safari and resvg with one blur each, and close the design's open decisions, stroke error limits and per-render work budgets.
- [ ] Phases 1–2: add bounded raster effect layers, shared kernels and SVG lowering, complete support regions, explicit mask conversion and atomic mark/text capture. Preserve each legacy backend's RGBA8 compositing, masks and default bytes; precision changes and dither belong to opt-in units. Qualify total work as well as allocation.
- [ ] Phase 3: ship the public `ChartForgeX.Effects` presets, `VisualThemeEffects` with unambiguous JSON precedence, SVG lowering profiles and export diagnostics. Move the existing card shadow to the shared compatibility recipe without changing default output; add true blurred elevation as opt-in. Qualify canvas and terminal migrations through their actual SVG and PNG producers.
- [ ] Phases 4–5: add frosted glass with shared contrast decisions and dependency-aware caching, bloom, bounded arc-length gradient/tapered strokes, light streaks, grain, turbulence, particles, dot matrix, perspective grid, vignette and `VisualBackdrop`.
- [ ] Phases 6–7: adopt effects in charts, Mermaid, topology, canvas, stories and `ImageComposition`, then move PowerBGInfo and ImagePlayground onto them.

Completion: every effect has one owner, deterministic native PNG output and declared SVG behaviour per output profile; existing defaults are unchanged; and the integrated HUD-wallpaper acceptance example renders in both outputs within the stated performance budget.

## Focused extensions

These are later lanes selected by real consumers; they do not block making the current report/chart product coherent.

- [ ] Extend geographic label placement, route trimming, clustering and callouts through the existing map owner. Independent RegionMap/DottedMap datasets, declared common projections, per-dataset color and size keys, separate point weight/color facts and GeoJSON point/line ingestion are implemented; arbitrary-coordinate mixtures and independent TileMap overlays remain unsupported.
- [ ] Qualify financial session schedules against further real consumers and display zones. Explicit included-time intervals and materialized Candlestick/OHLC datum styles preserve source prices, dates and authored bounds; no exchange calendar or calendar-month preset is inferred.
- [ ] Qualify org/tree portraits, collapse/expand, saved state, and layout only as explicit host/model contracts. Keep large interactive graphs routed through `GraphScene` and static topology through `TopologyChart`.
- [ ] Extend topology compound groups, group endpoints, ports, self/parallel edges, local direction, and obstacle-aware labels where real fixtures expose gaps. Preserve icon-pack provenance and artwork-loss diagnostics.
- [ ] Extend factual block presets, shared chips/status symbols, and composition primitives only where several report/image consumers need the same geometry. Do not create chart series for exact tables, lists, or KPI facts.
- [ ] Extend advanced font/Unicode/variation/color-font behavior only with concrete reference fonts and visible regressions. Preserve logical typography across raster densities and report font substitution limits.
- [ ] Consider animated WebP and MP4 only against a real delivery requirement. Stories already supplies bounded RGBA frames and GIF/APNG; platform codecs belong behind approved optional adapters. PDF, Office, and Visio delivery stays in OfficeIMO.

## Consumer and release qualification

The coordinated 3.0.0 authoring migration still needs consumer delivery and installed-host qualification. Candidate-package results do not establish public-feed or installed-consumer readiness.

- [ ] Recheck and complete each outstanding consumer migration against its current repository, dependencies, CI/review, and installed delivery path. Preserve the inspected wallpaper, report, image-composition, story and document workflows. Validate through ordinary public packages or explicitly isolated candidate packages, with source/feed/install state recorded separately. Do not infer downstream readiness from owner availability.
- [ ] Deliver PowerBGInfo's migrated chart, topology and factual-canvas workflows. Isolated candidate packages pass its PowerShell 5.1/7 commands and ultrawide/4K file exports, including independent text colors and percent-ring semantics; public-package CI and module delivery remain.
- [ ] Qualify ImagePlayground's moved authoring types, typed series, grid artifacts and story/terminal exports through the packaged module, preserving image and code-generation commands.
- [ ] Deliver OfficeIMO's paired-theme and Mermaid migration. Candidate-package proof covers saved Word, Excel, PowerPoint, PDF and editable Visio output, plus native PowerPoint export; public-package CI and adapter delivery remain.
- [ ] Deliver the migrated private HTML bridge with explicit host-versus-authored theme ownership, stable identities and localization. Candidate packages pass compact/wide light/dark browser and native export checks; public dependency delivery and domain-consumer migrations remain.
- [ ] Publish and verify a new owner release and affected host releases only with release authority. Read public artifacts back before claiming availability, and retain installed-runtime proof separately.

Completion: each delivered outcome has focused contracts, the full quality loop, required browser and packed-consumer proof, applicable Windows/Linux/macOS and Native AOT acceptance, settled CI/review, verified public packages when released, and representative installed-consumer evidence. Package identities, dependencies, framework assets, XML docs, symbols and licenses stay coordinated through the shared release entrypoint and three-part public versions; GitHub Releases remains the release-note source. Remove superseded task output and merged or abandoned worktrees only after fresh ownership, activity, containment and Git checks; retain evidence still needed for review or delivery. A local build or merged source does not substitute for delivery proof.
