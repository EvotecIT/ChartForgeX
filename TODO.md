# ChartForgeX roadmap

This is the active product roadmap. The [README](README.md) introduces the library, the [architecture](docs/v2/architecture.md) defines ownership, the [chart capability ledger](docs/v2/chart-capabilities.csv) records family options, and the [Mermaid matrix](docs/mermaid-support-matrix.md) records language and translation limits. Keep those references current rather than accumulating completed tasks here.

## Current position

Assessment refreshed on 14 October 2026. The shared native renderer and package separation are implemented. The nine coordinated 2.0.0 package IDs were verified on NuGet during the earlier assessment; that does not establish that later source changes are published or that every downstream host has migrated.

| Area | Implemented foundation | Remaining work |
| --- | --- | --- |
| Rendering and ownership | Typed models prepare a detached scene for SVG/native PNG; common frame, theme, typography, labels, semantic regions, and artifact handoff; runtime-dependency-free core across four target frameworks | Qualify the unified public contracts across packages and hosts; whole-chart visual fidelity needs stronger independent proof |
| Chart catalog | Broad Cartesian, radial, financial, hierarchy, relationship, map, and gauge coverage; 34 comparison entries in the option ledger | Existing family options, supported mixtures/orientations/axes, and data lifecycle need depth; a family name is not a complete capability contract |
| API usability | One immutable paired theme, retained series handles with construction-time IDs, typed specialized observations including maps/calendars, atomic configuration callbacks and data replacement, row selectors, common exports, a supported combination matrix, and standalone compiled recipes | Complete combined qualification of the breaking migration and consumer workflows; extend supported combination proof where real use exposes gaps |
| Visual appearance | Graphite light/dark defaults, measured typography, compact/wide examples, shared mark geometry and color owners; readable ordinary axes, numeric keys, tile codes and inherited Sunburst captions; neutral outlines for color-scaled Scatter/Bubble; explicit independent panel themes | Qualify difficult labels, sparse/dense data, mixed hosts, and complete SVG/PNG output beyond numerical health |
| Browser experience | Optional tooltips, selection, legend controls, crosshairs, brush ranges, linked charts, topology exploration, and an ordinary-chart mount/update/resize/dispose controller | Standalone zoom transforms the whole SVG. Qualify the integrated lifecycle across hosts and browsers; data-domain zoom, actual assistive-technology proof, UI localization, and RTL layout remain open |
| Mermaid | Thirty native family paths, typed source-preserving documents, located diagnostics, configuration mapping, direct/fence conversion, versioned semantic reference fixtures, and sequence block-boundary validation | Full grammar/notation fidelity varies; some mappings approximate or retain meaning without visible effects; the corpus needs broader feature and malformed-source coverage |
| Quality and delivery | Cross-platform build workflow, required Windows Chromium lane, explicit browser/platform prerequisite skips, AOT smoke, artifact/package checks, 401 numerical visual-baseline entries, and an existing packed-consumer matrix | Numerical health is not visual equivalence; actual assistive-technology and broader platform/browser proof remain open; broad packed consumers need an explicit qualification gate |

These are separate completion dimensions. Source coverage, usability, appearance, interaction behavior, translation fidelity, package publication, and installed-consumer proof cannot be reduced to one parity percentage.

The unified-authoring baseline passed the local Windows quality loop: 6,880 native tests, all four target frameworks, Native AOT, 401 healthy SVG/PNG/HTML comparison pairs, nine packed packages, four library-consumer lanes, and both CLI installation routes. The ordinary loop reported 353 explicit prerequisite skips. Eight verbatim downloadable recipes also compiled and ran against the isolated Core package, and the gallery's copy/download controls executed at compact and wide widths. Later Waterfall, relationship, map/calendar, panel-theme and marker-outline changes have focused native, browser and rendered-output evidence. They still require the final combined quality loop, regenerated gallery, current packed recipes and full required Chromium lane. Earlier baseline and browser results do not qualify the final combined API. Hosted cross-platform checks, a new public release and installed-consumer behavior remain separate gates.

## Delivery order

| Order | Outcome | Dependencies and completion evidence |
| --- | --- | --- |
| 1 | Trustworthy correctness and executed quality gates | Use the required browser lane and located semantic errors as the baseline. Expand measured visual evidence and qualify packed consumers before treating later API or rendering work as delivered. |
| 2 | One public configuration vocabulary and useful examples | Use current consumer workflows to settle theme, series, observation, and option ownership. Compile and run normal examples; publish intentional migration notes with the change. |
| 3 | Professional defaults and depth in existing families | Use the common configuration contracts and family ledger. Qualify complete compact/wide light/dark SVG and PNG, supported mixtures, no-data states, and difficult labels. |
| 4 | A complete interactive chart lifecycle | Extend core-owned projections/ticks/layers and keep input, controls, lifecycle, and browser layout in adapters. Qualify domains, updates, keyboard/touch, exports, and real accessibility. |
| 5 | Deeper language and specialized product lanes | Complete the high-use Mermaid families and visible approximations. Choose advanced map, financial, org, and new-language work from real consumer requirements. |

Mermaid correctness, API design, and quality infrastructure can proceed independently where their owners do not overlap. A pending review or release holds only the dependent step. Keep one coherent implementation PR per requested outcome; the rows above are planning stages, not automatic PR boundaries.

## 1. Correctness and quality evidence

Sequence conversion rejects unfinished blocks, unmatched `end`, and branches outside their immediate owners with located errors and no artifact through direct and Markdown-fence APIs. Gauge needle summaries use the shared font ascent envelope. Pointer focus keeps a partly visible word-cloud term stationary through mouse and native touch clicks while preserving keyboard reveal. Boundary-selection fixtures use observable painted extents and isolate rounded clips from font-dependent axis reserves.

Ordinary axes retain requested type while established placement thins crowded captions; per-tick allocations no longer force the complete axis to eight-pixel type. Numeric key fitting shares the 10-pixel ordinary floor. Tile-code layout recovers gap space before fitting complete codes down to that floor; omitted codes retain their descriptive and source facts. The catalog overview reserves more map space, and inherited Sunburst secondary captions have a readable floor. Explicitly authored small fonts remain intentional author choices.

`Build/Test-BrowserQuality.ps1` generates and serves fresh examples, runs required browser contracts through HtmlTinkerX, rejects missing setup or skipped proof, verifies decisive contracts executed, retains captures and console evidence, and releases its server. CI runs it in a separate Windows Chromium job. Disabled browser-only contracts and selected missing-platform prerequisites report explicit skips in the ordinary loop. Mixed tests retain their native proof and select their browser branches in the required lane. Browser color comparisons use rendered sRGB channels and alpha. Viewport contracts require native scale/resize and touch-pan behavior; physical pinch recognition needs separate input proof.

- [ ] Strengthen visual proof beyond numerical health. Current baselines constrain dimensions, node/pixel/color counts, and edge pressure; SVG text clipping uses anchor positions. Add measured glyph bounds and targeted geometry/semantic assertions for real regressions, plus curated observed complete-chart comparisons. Internal SVG rasterization comparisons need independent browser evidence for shared scene/font defects.
- [ ] Run `-QualifyPackages` at a defined release gate for net472, netstandard2.0, net8.0, and net10.0. The matrix includes four library-consumer lanes and Markup CLI installation through both tool-path and manifest routes; netstandard2.0 has compile proof rather than a standalone runtime. Default packing inspects all framework assets but its simpler consumer executes only net8. Qualify downloaded recipes against the actual packages. Keep local qualification distinct from public-feed and installed-host proof.
- [ ] Keep timing comparisons in the established controlled PowerForge benchmark/history lane. Review wall-clock budgets in the ordinary smoke suite; retain correctness/resource invariants there and record latency, allocation, capacity, output size, and browser response separately.

Completion: required checks demonstrably execute, known failure states have decisive regressions, optional omissions are visible, and green health checks are not described as complete appearance or interaction proof.

## 2. API consistency and ease of use

The shared authoring contracts are implemented: one immutable `VisualTheme` and explicit mode; retained series handles with immutable IDs; typed specialized observations, schedule lanes, nullable categorical Heatmap cells and Waterfall items; and callbacks that clone, configure, clone and install after success. Single-item updates preserve identity, full replacement clears observation metadata, and callbacks preserve explicit default-valued overrides and roll back on failure. Typed Waterfall supports empty data and refill while retaining authored item IDs across calculated checkpoints. Colors, typography and geometry share one theme owner; producer-specific chrome stays in local style options.

Map and Calendar observations keep coordinates, dates, values, labels and colors in complete typed items. Region/tile geometry belongs to the retained series; calendar layout and localized names use `ConfigureCalendar`. Factories and full replacements normalize duplicate dates and region aliases before assigning observation identities. Relationship series replace their complete validated graph through `ReplaceFlowData`, `ReplaceTreeData` or `ReplaceHierarchyItems`, retaining the configured handle. Empty typed data can be refilled, prepared exports remain detached, and invalid replacement leaves installed facts and metadata intact.

Chart grids share one theme, mode and font by default. `WithPanelThemes()` explicitly preserves independently authored panel palettes, modes, typography and native font requests through preparation, pagination, SVG color scopes, HTML and native PNG. The outer context still owns the grid frame, layout and visibility permissions. The [API conventions](docs/v2/api-conventions.md) and [migration guide](docs/v2/migration.md) document these ownership and update rules.

The [supported combination matrix](docs/v2/chart-combinations.md) records orientation, category alignment, primary/secondary axes and rejection boundaries. Radar categories use the same coordinate equivalence as their preparation map and reject duplicate categories within a series. Selected gallery downloads are complete console programs with concise compiled family recipes; normal programs use system fonts and a separate gallery-font download reproduces the gallery typography.

- [ ] Qualify this breaking API batch through focused and full native contracts, all four framework assets, packed consumers, required Chromium interaction tests, current gallery/README recipes and an independent review. Keep migration instructions aligned with the final API.
- [ ] Expand combination proof only for supported combinations that need it. Keep sparse/missing observations, category equivalence, horizontal axes and radial forms in the shared preparation owner; do not infer support from two individually supported family names.

Completion: a caller can construct, configure, update, theme, and export representative charts through one vocabulary without knowing renderer internals, collection encodings, or series positions. Intentional breaking changes include consumer inventory, focused proof, and a migration path rather than compatibility probes.

## 3. Chart depth and visual standard

The [family capability ledger](docs/v2/chart-capabilities.csv) is the detailed option inventory. Named stacks, signed normalization, interpolation/step placement, DifferenceArea polarity, authored histograms, explicit funnel/pyramid forms, Waterfall checkpoints, hierarchy value policies, flow styles/states, and shared numeric color dimensions are implemented. Scatter/Bubble support partly inferred continuous color domains and neutral theme-foreground outlines for pale midpoint and missing-value fills. The shared marker owner preserves mapped fill, alpha, raw measurements and identity; explicit outline colors and widths, including zero, remain authoritative.

- [ ] Establish a visual acceptance packet for every claimed family: compact/wide × light/dark, representative SVG/native PNG, readable title/ticks/legend/data labels, complete important marks, and understandable no-data/sparse/dense states. Measure collision and truncation where consequential; retain full facts in accessibility and semantic regions.
- [ ] Complete useful existing-family depth before adding headline names: BoxPlot IQR/outlier/statistic policies, Bubble size keys, gauge orientation/sweep/ticks/targets, hierarchy label fitting/padding, mixed radial projections, and horizontal/secondary-axis combinations.
- [ ] Extend the existing color-scale owner with arbitrary interior captions and independent Chord quantitative color where required. Preserve raw color facts, missing-value behavior, identity, and independent Bubble size.
- [ ] Add deliberate Sankey tiny-flow and hierarchy fitting policies only when actual compact data requires them. Preserve proportional weights, conservation, authored parent totals, source identity, and readable captions.
- [ ] Keep chart look choices in shared geometry/paint/label/theme owners. Palette changes must not select different renderer implementations. Preserve explicit effect styles and qualify small-label contrast, theme switching, and mixed-grid export density.
- [ ] Review complete report examples with chart grids and factual blocks, including host/embed frames and print. Keep page shells, collection, filters, and inspectors in consuming products; keep reusable block/composition behavior in Visuals and motion in Stories.

Completion: every advertised capability has a usable API, valid native output, a representative example, and visual acceptance; all remaining family limits stay explicit in the ledger.

## 4. Interactive charts

The ordinary-chart HTML controller mounts dynamic fragments, replaces the browser view from a freshly prepared adapter section, refreshes browser geometry and releases its resources on disposal or removal. Explicit series and observation identities reconcile surviving state; removed and anonymous targets clear. C# remains the data and layout owner. Native reflow uses a new render context and replacement section, while graph patching retains its separate API. Focused browser contracts cover repeated lifecycle cleanup, typed and relationship updates, hidden/slotted hosts and updated exports; final combined qualification remains part of this batch.

- [ ] Add genuine data-domain zoom using core-prepared projection/tick/layer plans and adapter-owned controls. The existing `ChartXViewport`/`ChartXProjection` contract covers vertical numeric Line/StepLine. Extend only deliberately supported families/axes; keep labels readable, regenerate ticks and readouts, retain full source facts, and qualify reset/export behavior.
- [ ] Qualify feature policies across ordinary chart families: legend hide/mute, pinned tooltips, nearest-point lookup, crosshair, brush/lasso, touch, keyboard traversal, and linked views. Keep display/palette choices independent from interaction enablement.
- [ ] Exercise actual assistive technologies and complete control/legend/series navigation on representative supported platforms. Accessible SVG names and keyboard contracts are foundations; they do not establish screen-reader behavior alone.
- [ ] Separate UI localization from data number/date formatting and font shaping. Add translated controls, no-data/error text, and deliberate RTL layout where required; test long labels, mirrored controls, focus order, and compact hosts.
- [ ] Measure small, ordinary, and dense Cartesian workloads separately from large topology scenes. Record preparation/update latency, allocation, output size, hit lookup, viewport response, and browser frame behavior before making scale claims.

Completion: interaction changes data views predictably, controls and targets remain usable after updates, lifecycle is bounded, and accessibility/localization claims have observed evidence.

## Mermaid

The [support matrix](docs/mermaid-support-matrix.md) records current parsing, rendering limits, upstream references and open owner dependencies. Thirty families have native render paths; that breadth does not establish complete grammar or visual fidelity. ZenUML, Agentflow and Railroad are diagnostic-only. The current conformance reference is Mermaid.js 12.2.0.

The reference corpus has 100 sources (95 native-family fixtures and five recognition fixtures), with 55 shared semantic expectation files. The upstream harness checks syntax and selected model facts; it does not render reference images. Fixture depth is uneven across families.

The compatibility goal is to adopt newer documented Mermaid syntax while preserving supported older syntax through regression fixtures. Start with explicit Mermaid 10, 11 and 12 reference versions; qualify the promised window before publishing it. Keep one semantic model where old and new spellings mean the same thing. Record deliberate grammar changes and migration guidance rather than maintaining separate renderer forks. Older majors and beta syntax need evidence for their individual contracts.

The stages set the default work order. Independent family fixes can proceed once their own fixtures and owner APIs are available; chart/calendar correctness does not depend on completing unrelated graph notation.

### Implementation status

Gantt support includes month/minute duration semantics, dependency/calendar resolution, input date fields and timestamps, English D3 axis directives, calendar tick intervals, vertical markers, explicit today-marker suppression and dependency arrows. Native sequence presentation and readable sequence HTML sizing preserve nested SVG constraints and support root-only host sizing.

Source font families and logical-pixel sizes flow through the native chart, topology, sequence and visual-block owners. Oversized captions fit their measured bounds, dense sequence layouts respect raster allocation limits, and ordinary title/subtitle roles retain their theme sizes. Packet source configuration controls row width, bit visibility/direction, preferred dimensions and gaps through shared SVG/PNG geometry with independent host overrides.

Further implemented support includes treemap value-caption visibility; Sankey source geometry and value-caption controls; closed radar curves, dimensions, margins and axis layout; native quadrant regions with accessible host-palette captions; horizontal XY lines through the shared Cartesian orientation; and flowchart circle/cross/bidirectional endpoints, literal captions and documented aliases over existing native shapes. Broader grammar and presentation work remains below. Source implementation and public package availability remain separate states.

### 1. Compatibility evidence and honest results

- [ ] Expand the conformance corpus from representative examples into feature coverage for every supported family, using upstream documentation and parser regressions with recorded version and provenance.
- [ ] Add release-specific coverage beyond the qualified 12.2.0 positive corpus: native mindmap branch/depth coloring, circle/caption bounds, disconnected swimlane order, handoffs and label/routing behavior. Current `mindmap.colorBy` probes retain settings with `CFXM005` instead of applying the palette. Preserve the 10.9.8 and 11.17.2 compatibility lanes and recorded semantic differences.
- [ ] Add malformed-source fixtures that the current reference rejects. The existing corpus requires acceptance by reference lane 12, so documented older-version rejections do not qualify invalid current grammar.
- [ ] Compare typed semantic facts in both implementations: nodes, edges, nesting, labels, styles, time calculations, attributes and values. Preserve the implemented flowchart, class/state/ER, sequence, packet, Sankey, treemap, radar, quadrant, configuration, and Gantt assertions; extend uncovered features through both upstream and .NET contracts.
- [ ] Distinguish invalid source, recognized-but-unimplemented features, retained metadata, approximate rendering and exact semantic mapping in parse/render results. Give diagnostics stable identities and original source spans, including through front matter, compact statements and Markdown fences.
- [ ] Extend statement classification across the remaining family grammar, including compact source forms and typed styling/interaction metadata. Retention warnings do not establish complete notation support.

Completion: every advertised feature has versioned positive and meaningful negative fixtures, preserved semantic facts, and an explicit rendering or diagnostic outcome. Parser acceptance alone cannot mark a family complete.

### 2. Shared presentation, layout and host policy

- [ ] Define a coherent precedence and policy for host presentation overrides, source settings and family defaults through the existing native export owners.
- [ ] Map remaining supported themes, `themeVariables`, palettes, typography, spacing and family options into the shared prepared scene. Source font stacks and supported logical-pixel sizes use the native owners; additional color variables, family-specific typography, CSS sizing modes, and further family options remain open.
- [ ] Handle Mermaid 12 `theme`/`look`/`layout` changes deliberately. Preserve older `defaultRenderer` declarations with a documented diagnostic or mapping. Keep deterministic CFX layout and appearance explicit; reproducing ELK or browser paint is a separate fidelity goal.
- [ ] Extend the existing safe label/text pipeline for escaped text, entities, multiline labels and Mermaid Markdown strings. Specify literal fallback and diagnostics for HTML labels and math until a native implementation has artifact proof.
- [ ] Apply supported `classDef`, default classes, `class`, `style`, `linkStyle` and edge-ID styling consistently across families. Preserve inheritance and source order; report properties retained without visible effect.
- [ ] Extend shared topology layout for nested compound groups, group endpoints, local direction, ports, self-loops, parallel edges and obstacle-aware label routing where fixtures expose gaps. An orthogonal shortcut across a three-node chain can intersect the middle node; reuse the native obstacle-avoiding route owner and prove SVG/PNG geometry together.
- [ ] Add a coherent host policy for accepted diagram families, source styling, safe links/assets and bounded source/model/render size. Reuse existing SVG, URI and artwork safety owners; prove script-free output and deterministic rejection without fetching remote assets or invoking source callbacks.

Completion: source presentation produces consistent SVG/PNG scenes or specific diagnostics, host policy has a single reusable owner, and compact/dense fixtures remain readable and bounded.

### 3. Core diagram semantics

- [ ] Qualify shape-only flowchart surfaces and centered captions. Inferred topology kinds currently add infrastructure glyphs to decision and other semantic shapes; source shape syntax should determine the diagram notation.
- [ ] Complete the remaining shape catalog, icon/image metadata, edge IDs/properties, minimum edge lengths, nested subgraphs and local directions. Keep edge animation and callbacks in optional host adapters.
- [ ] Complete sequence participant creation/destruction, participant boxes, supported message variants, nested parallel/critical branches, branch labels, notes, activations and autonumber behavior. Keep actor menus and links as safe host-facing data.
- [ ] Complete class generics, visibility/static/abstract members, lollipop and two-way relationships, labeled/nested namespaces, notes, styling and safe link metadata.
- [ ] Complete state choice/fork/join notation, concurrent regions, nested composites, multiline notes and classes; keep transition and start/end meaning visible.
- [ ] Complete ER aliases, optional attribute types, keys/comments, direction, subgraphs and styling while preserving identifying relationships and cardinalities.
- [ ] Complete requirement kinds, fields, one-line blocks, relation directions, Markdown labels and classes with typed validation.

Completion: the corresponding matrix rows have grammar, semantic and visible-notation proof through direct rendering and Markdown fences; generic topology previews no longer hide supported diagram meaning.

### 4. Chart and calendar fidelity

- [ ] Complete Mermaid treemap classes, styles and value formatting through the shared hierarchy options and retained `HierarchyItems` owner.
- [ ] Qualify Mermaid Sankey labels, CSV identities and configuration through the retained nodes, links and `ReplaceFlowData` owner.
- [ ] Map radar theme variables through the existing native theme and series-style owners.
- [ ] Complete quadrant point/class styling, theme variables, source dimensions and separate axis endpoint labels. Native region captions use the shared bounded placement policy.
- [ ] Render journey sections, actors and score paths as a reusable journey scene and timeline sections/events in both directions. Keep the current score-bar/ordered-time mappings explicit until replaced.
- [ ] Broaden pie/show-data, XY named-series legend rules, per-point line labels, numeric/category axes and source configuration, Sankey CSV/configuration/colors, and remaining packet row/label cases.

Completion: values, dates, hierarchy, direction and requested encodings survive conversion and are visibly represented. Shared native APIs are implemented; each language mapping still needs its own configuration, semantic and compatibility proof.

### 5. Existing specialized diagram depth

- [ ] Complete C4 boundary/deployment notation, relationship variants and update style/layout statements; block composites, spans, shapes and styles; and architecture nested boundaries, endpoint-side routing, row/column alignment and safe icon mapping.
- [ ] Replace TreeView's topology approximation with reusable tree-row geometry that supports box-drawing input, descriptions, classes and file/directory icons. Complete mindmap Markdown labels, shapes, classes and icon handling.
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
- [ ] Add observed upstream/CFX render comparisons to the existing gallery workflow through HtmlTinkerX test-time browser tooling. Compare meaning, labels, clipping and routing; record intentional appearance differences instead of treating pixel equality as the compatibility contract.
- [ ] Run focused semantic/artifact tests first, then the full `Build.ps1 -Configuration Release` loop for stable implementation candidates. Qualify all supported target frameworks, Windows/Linux/macOS, Native AOT, package consumers, deterministic output and consequential resource limits.
- [ ] Update the support matrix, generated examples and user-facing compatibility/migration notes with each completed tranche. Keep package/feed evidence distinct from local source and open PRs.
- [ ] Complete PR CI/review settlement, then publish and verify owner packages and dependent hosts only with release authority. Remove superseded validation output and close completed roadmap items after the durable contract is documented.

Completion: the documented support window is backed by source, artifacts, host workflows and package evidence. Implementation, merge, publication and installed-consumer qualification remain separate gates.

## Focused extensions

These are later lanes selected by real consumers; they do not block making the current report/chart product coherent.

- [ ] Extend map composition with independent layer scales, geographic label placement, route trimming, clustering, and callouts through the existing map owner.
- [ ] Define trading-session time compression for financial charts without changing source prices or authored date bounds. Keep candlestick/OHLC styling in their shared financial owner.
- [ ] Qualify org/tree portraits, collapse/expand, saved state, and layout only as explicit host/model contracts. Keep large interactive graphs routed through `GraphScene` and static topology through `TopologyChart`.
- [ ] Extend topology compound groups, group endpoints, ports, self/parallel edges, local direction, and obstacle-aware labels where real fixtures expose gaps. Preserve icon-pack provenance and artwork-loss diagnostics.
- [ ] Extend factual block presets, shared chips/status symbols, and composition primitives only where several report/image consumers need the same geometry. Do not create chart series for exact tables, lists, or KPI facts.
- [ ] Extend advanced font/Unicode/variation/color-font behavior only with concrete reference fonts and visible regressions. Preserve logical typography across raster densities and report font substitution limits.
- [ ] Consider animated WebP and MP4 only against a real delivery requirement. Stories already supplies bounded RGBA frames and GIF/APNG; platform codecs belong behind approved optional adapters. PDF, Office, and Visio delivery stays in OfficeIMO.

## Consumer and release qualification

The core prepared scene, Visuals/Stories extraction, and public 2.0.0 owner packages are complete. Earlier local consumer qualification is useful historical evidence, not proof that today's public packages or installed hosts execute every current contract. The current read-only consumer inventory identifies legacy theme and authoring calls that still need migration; the owner API changes do not update those repositories.

- [ ] Recheck and complete each outstanding consumer migration against its current repository, dependencies, CI/review, and installed delivery path. Preserve the inspected wallpaper, report, image-composition, story and document workflows. Validate through ordinary public packages or explicitly isolated candidate packages, with source/feed/install state recorded separately. Do not infer downstream readiness from owner availability.
- [ ] For stable implementation candidates, run focused contracts, the full quality loop, required browser evidence, and the packed-consumer matrix. Qualify Windows/Linux/macOS and Native AOT at the applicable gate.
- [ ] Keep package identities/dependencies/framework assets/XML docs/symbols/licenses coordinated. Use the existing shared release entrypoint and three-part public versions; GitHub Releases is the release-note source of truth.
- [ ] Publish and verify a new owner release and affected host releases only with release authority. Read public artifacts back before claiming availability, and retain installed-runtime proof separately.
- [ ] Remove superseded task output and merged/abandoned task worktrees after fresh ownership, activity, containment, and Git checks. Retain only bounded evidence still needed for review or delivery.

Completion: each delivered outcome has current source and artifact proof, required CI/review settlement, verified public packages when released, and representative installed-consumer evidence. A local build or merged source does not substitute for those delivery boundaries.
