# Shared rendering architecture

ChartForgeX prepares typed chart and diagram models into a detached scene. The scene contains final geometry, positioned text, source identities, descriptive regions and diagnostics. SVG serialization and native raster painting consume those decisions; neither backend performs family layout or parses the other backend's output.

The entry point is `IVisualRenderable.Prepare(VisualRenderContext)`. Charts, grids and diagram exporters use the shared preparation route. Invalid family combinations and unsupported configurations fail explicitly. Phase 1 established this ownership boundary; Phases 2–3 implement the family producers and their shared presentation contracts. Package extraction and consumer qualification remain separate phases.

## Package ownership

| Owner | Responsibility | Dependency direction |
| --- | --- | --- |
| ChartForgeX | Charts; topology, flow, sequence and other genuine diagrams; colors, geometry, fonts, shared frame/theme contracts, static scene painting and semantic interchange | No runtime package dependencies |
| ChartForgeX.Visuals | VisualCanvas, ImageComposition, watermark decoration, factual metric tiles, tables and lists | Core only |
| ChartForgeX.Stories | VisualStory, TerminalStory, motion timelines, animated topology and all GIF/APNG encoding | Core; static renderable inputs |
| Existing interactivity/HTML adapters | Browser events, navigation, controls, responsive recompilation and host wiring | Core and their applicable adapter contracts |
| Existing Mermaid/markup adapters | Parsing and typed source-model production | Core semantic contracts; optional capability bridges only when real callers require them |

Visuals and Stories are peers. A still wallpaper must not load an animation package. Stories accepts common static inputs without making the core dispatch over optional concrete types. Core retains static GIF input/first-frame decoding as an explicit input exception; GIF output belongs to Stories even when it has one frame.

Phase 4 separates the eight runtime assemblies. Visuals and Stories each reference core only. Markup references Visuals because its existing table parser produces a static factual-table presentation; Mermaid remains core-only. Interactivity is host-neutral, and the HTML adapter keeps its existing core/interactivity direction. No compatibility forwarding framework or global renderer registry is used.

`IStaticVisualSource` is the producer-owned export boundary for optional static presentations. It provides scoped SVG and native RGBA output without making core name canvas, factual block or story implementations. `VisualArtifact.RenderSource` can carry that presentation independently of its semantic `Model`. A watermark decorator captures the previous presentation and supplies an ordered Visuals-owned source. Prepared native scenes continue through `IVisualRenderable` and `PreparedVisual`; immediate image composition and legacy block layouts do not claim a native prepared-scene conversion merely because their assemblies move.

Neutral `VisualStatus`, table semantics, common block contracts and the six genuine block diagrams stay in core. Canvas layout, factual block models and their concrete rendering move to Visuals. Stable static grid target IDs remain usable by a Stories motion presentation without a Visuals assembly reference. Topology motion samples a detached core geometry observation rather than re-running layout or reading geometry from SVG.

Keep product data collection, dashboard shells, filters, inspectors, wallpaper templates and document placement in consumers. PowerBGInfo is a required consumer of both core and Visuals. Its designed wallpaper effects remain supported capabilities. Progress handles and completed-state markers are data marks; they are not removed with decorative menus, selection checkboxes, navigation arrows and action buttons baked into still images.

## Contracts

| Contract | Behavior |
| --- | --- |
| `VisualSize` | Finite positive logical width/height; raster density is separate |
| `VisualLayoutOptions` | Resolved fixed viewport and validated outer padding |
| `VisualFrame` | Common title, subtitle, measured legend title/entries and density budgets; independent card/content surfaces and transparent canvas |
| `VisualTheme` | Immutable light/dark color snapshots, typography scale, independent card/mark radii and bounded card-shadow settings |
| `VisualRenderContext` | Explicit size, frame, theme mode and cloned font request |
| `PreparedVisual` | Detached scene, immutable diagnostics/regions, cloned accessibility metadata; reusable SVG, RGBA and PNG export |
| `VisualRenderOptions` | Bounded integer output scale, supersampling and working-pixel budget |
| `VisualArtifact` | Host identity and metadata, static handoff and a separate versioned semantic payload |

Mutable source builders are not safe for concurrent editing during preparation. Once preparation completes, source mutations, font registration changes and mutations of returned font/accessibility copies cannot change the prepared geometry or export. Scene commands and shaping resources remain internal. A future same-repository composition package may receive narrow friend access; a public editable scene DSL requires a concrete external consumer first.

```text
typed model + resolved viewport + immutable theme + font request
    → validation, common frame measurement, family geometry/text layout
    → detached scene + source regions + diagnostics + text alternative
    → SVG serializer | native raster painter → raster encoder
```

The shared command set covers groups, rectangular and path clips, rectangles, ellipses, paths, numeric pie/ring slices, image nodes, linear gradients and positioned shaped text. Rigid transforms and glyph outlines preserve diagram and chart requirements in both SVG and native raster output. Scene commands remain internal; the public boundary is the typed model and immutable prepared output. Composition capabilities migrate with their owner and acceptance fixtures.

## Size, frame and overflow

Logical bounds are exact. Raster dimensions round each positive logical dimension multiplied by output scale upward. Supersampling affects working coverage and allocation, not text positions or the logical scene. The pixel budget includes supersampled working pixels and is checked before allocation; the existing raster allocation guard supplies its independent byte ceiling.

Phase 1 accepts a resolved viewport. A host can measure its container and prepare again at a compact size; scaling an SVG `viewBox` preserves proportions but does not reflow its layout. Natural-content sizing, constrained aspect selection and a complete Fixed/Fit/Content policy remain later work. Do not advertise those modes as implemented by a placeholder enum.

The common frame measures headings and legend before handing a content rectangle to the family. Headings have a bounded two-line budget. Legend entries wrap into rows or a side strip; insufficient space produces diagnostics. Truncated display text retains its full source label in semantic metadata. Factual content is not turned into a chart series to obtain a frame.

`VisualFrame.LegendTitle` is an optional measured heading above categorical legend entries. It shares the legend's height budget, and its complete text remains in a descriptive region if it cannot fit. A null value permits a producer-supplied title; an empty string suppresses it. Chart and grid frame copies preserve the title, styles and density budgets.

`ShowCard` controls the complete elevated frame independently of `ShowSurface`, which controls the content surface. `VisualTheme.CardRadius` shapes the outer card; `BarRadius` and family plot/mark settings remain independent. `CardShadowOpacity` and `CardShadowColor` produce shared rounded native layers for both SVG and PNG. Shadow spread stays within available outer padding and the authored viewport, without moving content. Zero opacity keeps a flat card. If padding leaves no room, the shadow is omitted with `frame.card-shadow-no-room`; the fixed viewport never grows.

Cartesian axes reserve measured strips within that content rectangle. Side legends and axis text therefore cannot claim the same space. Root titles are distinct from axis titles, donut center values and future VisualCanvas hero text. All use shared typography roles while keeping their own meaning.

## Theme ownership and schemas

HtmlForgeX owns Evotec color values. The embedded chart export is copied from revision `c9154b0d9c798061ecb178add498cac3c035ddec`, repository-root path `Generated/Tokens/ChartForgeX/evotec.chartforgex.tokens.json`, Git blob `efe1fcf67d73cf0a8c6b21ffeb1023062698c24a`. Its canonical source is `HtmlForgeX/Resources/Tokens/evotec.tokens.json`, version 1.1.0, blob `af906aceb366249828fc91322ece98199e3145ae`. Core does not reference an HtmlForgeX assembly.

The generated palette has `light` and `dark` objects and no numeric schema-version field. `VisualTheme.FromJson` imports this existing palette shape through the bounded shared parser. It supplies six series colors, surfaces, foregrounds, fill/ink status pairs and sequential/diverging ramps. It is not a full typography/layout schema.

The full ChartForgeX theme contract uses schema version 1, keeping the palette fields and adding `typography`, `geometry` and `effects`. `geometry.cardRadius` is independent of `geometry.barRadius`; `effects.cardShadowOpacity` and `effects.cardShadowColor` retain the chosen shadow, including color alpha. Older version-1 documents without these optional fields use radius 3 and zero shadow opacity. `FromThemeJson` and `ToThemeJson` are distinct from palette intake. Unknown full-theme versions fail explicitly. Schema versions describe portable contracts, independently of the NuGet package version.

The Graphite preset uses canonical HtmlForgeX colors and the chart look specification's layout/type scale. Renderers read presentation values from the theme or explicit model overrides. They do not select a different geometry implementation because of a palette. Existing `UseGraphiteLayout` branches are removed as their families migrate; Phase 1 does not claim those legacy branches have all gone.

Precedence is explicit model/element setting, then context/host override, then selected preset defaults. A context-supplied theme controls the prepared route; a legacy `ChartTheme` is not silently combined with it. Explicit series/point colors and configured text styles remain meaningful. Geometry choices such as aggregation and stacked layout are explicit model policy and apply independently of light/dark mode.

Severity, result outcome and operational state keep separate vocabularies. A universal status enum would lose meaning. Preserve arbitrary stable keys, localized labels, drawing order and unknown/not-observable/not-evaluated distinctions. Color pairs alone do not establish hatch/outline treatment equivalence: the generated palette omits those policies, and their owner mappings need dedicated fixtures during full migration. The initial chart proof requires explicit colors for status marks and rejects unsupported pattern treatments.

## Typography and portability

Preparation uses the existing font resolver, shaping engine and fallback faces. Text metrics and runs are retained in the scene. Export does not rediscover fonts. The public context returns a defensive font copy.

Native SVG text remains selectable, but its final glyph painting depends on the reader's fonts. Shared scene geometry does not promise browser/native pixel identity. Outlined SVG text and embedded-font product policy remain later decisions; neither is claimed by the current prepared exporter.

The review gallery and benchmarks use explicitly registered Carlito regular/bold fixtures under the SIL Open Font License 1.1, pinned to Google Fonts revision `3dd78844021e948ceb633d1dcee3f7885561b5d9`. Regular SHA-256 is `F6418F708BAEDE9789DAEF5D458C0F53D2A888AF9820E8062934E504FEDC6595`; bold is `BB5D20F79B82599EC72983597437373A80F2D2085FA91FC144FD74E876A594DB`. These are validation/example assets, outside runtime packages. Gallery CSS supplies those same faces for review. No font download is required by the rendering engine.

## Semantic artifacts and consumer handoff

Prepared pixels are separate from native editable diagram data. `PreparedVisual.ToArtifact` captures an optional existing semantic interchange envelope through its versioned bounded writer/reader. IDs, kind and supplied dimensions must match. The factory preserves logical dimensions, alternative text and descriptive regions, and keeps the source semantics beside the static scene.

Preparation first takes a defensive typed copy of producer semantics, including nested collections and presentation values. JSON serialization is lazy over that private copy; later edits to the model or its original envelope cannot alter the retained payload. Portable JSON budgets apply when interchange is requested, including conversion to an artifact, rather than limiting static preparation or SVG/PNG export. Reading retained interchange returns an independent envelope.

A host can edit artifact titles, metadata and accessibility without mutating prepared pixels. Changing its natural size does not resize the scene; prepare again. Scene nodes are not serialized as Office interchange, and formatter delegates never become portable payloads.

Existing topology/flow/sequence interchange readers keep their explicit schema-version policy. Family/source IDs and entity relationships are retained for native Office consumers. Generic static artifacts may use SVG/PNG fallback; that fallback cannot be described as editable native diagram fidelity.

The [consumer capability ledger](consumer-capabilities.csv) covers [PowerBGInfo](https://github.com/EvotecIT/PowerBGInfo), [ImagePlayground](https://github.com/EvotecIT/ImagePlayground), [OfficeIMO.ChartForgeX](https://github.com/EvotecIT/OfficeIMO), a private HTML adapter and a private reporting consumer. Public documentation records the private consumers' technical requirements; their identities and exact source evidence stay in the private planning audit. Owner fixtures and later consumer/package qualification are separate gates. Read-only inspection does not count as executing a consumer. No downstream source or dependency pins change in Phases 0–1.

## API and catalog closure

The [API conventions](api-conventions.md) define meaningful fluent verbs and purposeful exceptions. The [public member inventory](api-ledger.md) records the integrated source, exact overloads/defaults and planned ownership. The original inventory describes six baseline runtime assemblies. Phase 4 adds a separate eight-assembly source inventory and an assembly/signature diff; packed dependency, resource and execution qualification remains independent evidence.

The [chart capability roadmap](chart-capabilities.csv) records per-family existing capabilities, missing options, priority lanes, dependencies and acceptance fixtures. Priority is driven by real use and architectural reuse. Browser interaction, maps and advanced financial tooling have separate scope from static chart quality. Chart names alone do not establish completeness.

Every migrated family needs typed data/geometry, shared frame/theme/text, SVG/native PNG proof, meaningful edge cases, catalog examples, semantics and performance evidence. Do not implement every catalog gap before the common pipeline has qualified.

## Phases and stop point

| Phase | Work | Gate |
| --- | --- | --- |
| 0 | Architecture/conventions, full API inventory, consumer mapping, integrated baseline | Exact source identities; explained API differences; reproducible baseline |
| 1 | Shared pipeline; Cartesian and donut proof; tiny topology/sequence; artifact handoff; isolated light/dark gallery | Build/tests, observed SVG+PNG, framework/package/AOT checks and performance comparison; reassess before expansion |
| 2 | Complete Cartesian/radial migration and unified formatting/sparkline policy | Supported option closure; remove replaced legacy family paths |
| 3 | Diagram and remaining family migration | Shared contracts plus family-specific semantic/visual closure |
| 4 | Extract Visuals and Stories; remove decorative static dashboard controls | Full package dependency/asset closure and preserved composition/animation capabilities |
| Visual acceptance | Finish the shared visual system and curated family gallery before consumer migration | Observed SVG/PNG light/dark compact/wide output; coherent typography, spacing, labels and legends; usable theme/filter/example/export navigation; qualified performance and package boundaries |
| 5 | Consumer migrations and release qualification | Published-package-only consumer builds and real document/module/wallpaper execution |

The active checklist lives in [TODO.md](../../TODO.md). Visual acceptance precedes Phase 5. Package extraction qualifies ownership and retained capabilities; the gallery and rendered chart layouts require their own observed acceptance. Package publication and consumer migration require separate authority.

## Validation and performance

The integrated baseline is commit `fdb8fe16df11037c1228168c6984ed31316ae04f`: main `20e3443da9521bcd3371f68b232406596564191b` plus label-placement `5d2c4ada0f7b4aa1fb8704e9ba150ff3b9f0d5bb`. Its test run passed 2,384 tests on net8.0. The frozen net8 assembly SHA-256 is `43AE57D902E428A3A84674B16EA9DC6FB1C6D1194B3C60CB9E2F48D30FFE010E`. Build/test evidence is separate from the open prerequisite PR and from published package state.

Use `dotnet test ChartForgeX.sln -c Release` for code checks and `./Build.ps1 -Configuration Release` for the stable quality/package loop. Keep net472, netstandard2.0, net8.0 and net10.0 compile/assets checks; netstandard is not an executable test runtime. Native AOT and browser execution are separate evidence.

Keep unchanged-output benchmark cases byte-identical. Redesign cases must validate equivalent data, labels, bounds and visual quality before timing. Measure end-to-end SVG/RGBA/PNG, preparation and reused-scene export separately. Candidate-only prepared operations must not masquerade as a comparison with legacy end-to-end work.

Use the existing PowerForge benchmark orchestration, paired/rotated warm samples, allocations and output sizes. Investigate every unexplained regression above 10% per representative workload; do not average away a family regression or remove labels/quality to pass. Record source/assembly/font hashes, runtime/SDK, quality settings and distributions. The threshold is a gate to investigate, not a guarantee from one median.

Generated SVG must be inspected in a browser and native PNG viewed directly. Numeric health checks and XML assertions complement that inspection. The curated `generated-v2` catalog provides uniform family examples; the complementary scenario gallery retains compositions, interactions and dense fixtures. Regenerate both from the same source when defaults change. Baseline refresh follows observed approval.

The stable Phase 1 runtime is commit `913245cd0d6e34d3e9fb5a7a02c6fe6ae00aa67c`, net8.0 assembly SHA-256 `F9E6C9DE9137BA37698FC73539CF7554D9DF60A82DAF5F47EFA542D64D5A9DC4`. Its complete local quality loop passed 2,491 tests, 42 Mermaid conformance fixtures, Native AOT execution, 317 healthy visual pairs, all four target-framework assets and isolated consumption of the six packed libraries. The 56-case gallery and 16 checked-in selections remain byte-identical after the bounded opaque-background raster optimization; the earlier 14 executed browser cases and direct PNG/SVG inspection therefore cover those same outputs.

Final performance qualification uses 30 warmups and nine retained samples per case, paired/rotated PowerForge ordering and recorded processor affinity/priority. Across 84 cases and 756 samples there are no correctness failures. All 18 complete prepared-scene comparisons improve elapsed time by 7.9–71.2% and reduce allocations. The 12 unchanged-output comparisons preserve exact SVG/PNG bytes and range from 5.1% faster to 8.6% slower, within the 10% gate. Earlier timing flags and every confirmation remain in the private evidence; no pooled averages or removed outliers override them. Concurrent unrelated builds/tests were observed, so these are qualified local comparisons, not a quiet-host or cross-platform performance guarantee. Output-size changes remain separate costs below.

## Phase 1 reassessment

Retain the prepared-scene boundary and the core/Visuals/Stories ownership plan. The implemented fixtures establish detached geometry, shared frame and theme resolution, reusable native raster export, descriptive source alternatives and scoped SVG embedding. The fixed-size proof is sufficient to guide family migration; it does not establish every existing option or composition primitive.

The Phase 1 expansion plan included formatting and time semantics, missing observations versus zero, disconnected paths, normalized and independent stack groups, aggregation provenance, label and legend density, explicit style overrides and unsupported-option closure. The migration implements shared formatting/sparkline policies, missing-data ingestion and axis-isolated stacks. Authored stack-group keys and 100% normalization remain unclosed ambitions in the [chart roadmap](chart-capabilities.csv), alongside selectable interpolation and step placement. Severity, outcome and operational state retain separate meanings and presentation policies.

Prepared output adds semantic metadata and embedding isolation. In the measured 500-point scatter fixture, raw SVG grows from 139,591 to 285,201 bytes; repeated metadata, scoped identities and redundant paint attributes account for most of the increase. Redesigned scene fixtures also produce larger PNGs, ranging from about 4% to 50% above their legacy counterparts despite equal dimensions and quality settings. Lower rendering time does not erase those transfer/storage costs. Reduce redundant serialization in later work while retaining full source alternatives, stable identities and geometry, and compare compressed as well as raw sizes.

Composition migration must adopt the shared image, transform and gradient owners while preserving the measured effects used by wallpapers. Stories must retain both visual-story workflows, terminal playback/transcripts, ordered watermark decoration and animated topology. Owner fixtures do not qualify downstream execution: PowerBGInfo needs actual wallpaper proof, ImagePlayground needs installed-module proof on PowerShell 5.1 and 7, and OfficeIMO needs picture/DPI/accessibility and editable-diagram qualification. Native source coordinates and prepared display regions remain distinct until exact document placement is proven.

## Phases 2–3 reassessment

Retain the shared native scene as the owner for every migrated chart and diagram. Integrated local qualification passes 3,016 tests, 42 Mermaid fixtures, all four framework assets without warnings, 317 healthy SVG/PNG/HTML outputs and isolated consumption of all six existing packages. Fresh Native AOT publication and execution cover the corrected production implementation. The same portable test assembly also passes all 3,016 tests under Linux with Linux fonts; hosted platform builds remain a separate gate. Resolved-font measurements protect rotated heatmap labels, topology symbols and badges, stack totals and endpoint callouts. Compact dots retain their readable logical font size and report fitted prefixes without losing source symbols. The full Linux gallery passes both baseline evaluators: reviewed natural-height exports record resolved outer-frame font provenance; fixed canvases, widths, scale and actual raster allocation remain strict. The checked-in selected catalog contains 122 artifacts, covers all 49 chart kinds in light and dark, and passes compact/wide browser checks with registered regular/bold fonts. This qualifies owner behavior; it does not establish downstream execution, package publication or website deployment.

The migration measurements exercise complete exports and retain preparation and reused-scene export as separate diagnostics. Historical public exporters can map frame and typography requests differently, and the former scenario naming did not guarantee first/second alternation within each engine pair. Those results are historical evidence rather than the current controlled timing budget. The maintained suites use explicit shared render requests and pair Baseline/Candidate engines within one fixture and operation, with balanced execution positions.

These are representative local measurements, not an all-family, dense-layout or cross-platform guarantee. Topology retains legacy typography versus the candidate common frame. Flow and sequence are excluded from this paired performance set because the frozen public exporters cannot accept equivalent token/font inputs. Redesigned payload size remains a separate tradeoff: measured Phase 3 SVG changes range from 47.72% smaller to 111.38% larger, while PNG changes range from 49.41% smaller to 38.52% larger. Preserve source semantics and embedding isolation when reducing serialization overhead.

The package boundary separates static compositions and animation into the peer owners described below. Native producer coverage does not close the family option roadmap: authored stack groups, normalized stacks, interpolation and step placement remain open. Keep consumer repositories read-only until their migration phase, and retain the package-only, installed-module, saved-document and wallpaper execution gates described below.

## Family migration and extraction boundary

All 49 chart kinds, chart grids, topology, flow and sequence have native prepared-scene producers. Convenience exports use those producers, and replaced static chart/diagram renderers are retired. This establishes common rendering ownership; per-chart options and their acceptance fixtures remain in the [capability roadmap](chart-capabilities.csv). Integrated quality, observed output and representative performance qualify the combined implementation separately, with current gates in [TODO.md](../../TODO.md).

Fixed viewport fitting preserves graph geometry and source alternatives, but can make a dense diagram's text small. Compact topology therefore needs an appropriate source layout or a larger host viewport. Scaling does not provide automatic content reflow. Hosted image artwork remains an SVG resource with a diagnosed canonical-glyph raster fallback; embedded artwork is the self-contained paired-output path.

Visuals and Stories are peers depending on core. Their ownership boundaries are:

1. Core owns generic prepared artifacts, neutral semantics, text, geometry and pixels. Producer extensions live with their optional models. `IStaticVisualSource` carries optional output through artifact dispatch without a global registry or concrete producer references.
2. Visuals owns VisualCanvas, ImageComposition, factual tiles/tables/lists, static composite grids and ordered watermark decoration. It reuses core text/image services and preserves wallpaper layer order, alpha, text fitting, offsets and chosen raised/glass effects. Core render options contain no watermark-specific types. Legacy factual and composition producers expose their completed output through the common boundary; the extraction does not convert them into native chart scenes.
3. Stories owns visual/terminal stories, transcripts/tokenizer contracts, motion timelines, topology animation and all GIF/APNG output, including single-frame GIF encoding. Core retains image input decoding. Stories reuses core image-fit geometry and resolved topology routes without depending on Visuals.
4. Qualify core-only, core+Visuals and core+Stories from isolated packed assets across all four target frameworks. Verify resources, namespaces and assembly-load boundaries; compositions and animations need their own execution fixtures.
5. Migrate consumers against the qualified owners in dependency order. PowerBGInfo needs wallpaper execution, ImagePlayground needs installed-module proof in both PowerShell runtimes, and OfficeIMO needs saved-document and editable-diagram proof. The HTML adapter and private reporting consumer retain separate package/dependency gates. Publication and consumer releases require their own authority.

Retain semantic completion markers and deliberate progress handles when removing decorative static controls. Keep genuine browser interaction in its adapter and product shells/data collection in consumers. The extraction introduces no additional runtime dependency or public scene framework.

## Phase 4 reassessment

Retain the peer package split. Core-only charts and static diagrams load independently of Visuals and Stories. Wallpapers and factual layouts use Visuals; animation, transcripts and GIF/APNG output use Stories. Markup explicitly takes Visuals for its table preview. Shared text, pixels, image fitting and resolved topology geometry stay in core, while optional producers pass completed output through `IStaticVisualSource` without concrete core dispatch.

Assembly moves require compiled callers to rebuild against package version `2.0.0`, even where domain namespaces stay stable. The separate [Phase 4 API inventory](api-ledger.md) records those moves and replacements. Isolated packed probes verify all four target assets, resources and dependency boundaries; execution covers net472, net8.0 and net10.0 for the three core/presentation lanes. The adapter lane executes on net8.0 and net10.0. Owner wallpapers, factual layouts, terminal output and animated stories have observed visual fixtures. This qualification preserves the retained producers' export contracts; it does not convert their legacy layouts into native chart scenes.

Topology measurements retain 22 supported comparisons with identical full diagnostics and SVG; mixed-scene SVG cases are explicitly excluded where the baseline cannot produce them. The maintained suite pairs engines within every fixture/operation and records processor placement. Historical extraction runs and their timing flags remain available, but do not establish quiet-host or cross-platform performance.

Visual acceptance closes the shared presentation and gallery work in [TODO.md](../../TODO.md) before Phase 5. Phase 5 closes the downstream contracts in the [consumer migration guide](migration.md). PowerBGInfo needs actual wallpaper execution, ImagePlayground needs installed-module static and animated workflows in both PowerShell runtimes, and OfficeIMO needs saved-document and editable-diagram proof. Owner source, local packages and owner fixtures do not establish those results or package publication. The chart option roadmap remains separate future work.

## Visual acceptance reassessment

Charts and diagrams share the paired canonical colors, measured frame, typography roles and label-placement services. The default heading is 17px bold, outer padding is 24px, and scalar values have a separate 34px role. Frame legend settings resolve consistently across families; authored sizes, colors, axis bounds and family-specific scale switches remain effective. Automatic Cartesian and schedule ranges reserve room for endpoint marks. Annotation placement accounts for data marks, including the appointment chart's peak-window caption.

The curated catalog covers all 49 chart kinds with native wide and compact light/dark SVG and PNG examples. Its 54 primary tiles use one 400x280 preview canvas, meaningful titles and chart-only artwork. Full examples retain their semantic content and expose SVG, PNG and C# source. The canonical website imports declared artifact dependencies and preserves theme, family and return navigation. A compact example may use a smaller overview dataset so its labels remain readable; its source makes that choice visible.

The owner quality loop passes 3,172 tests, 42 Mermaid fixtures, all four target frameworks without warnings, fresh Windows Native AOT execution, 317 healthy visual pairs and eight isolated local packages. All 196 native family exports pass observed compact/wide light/dark acceptance. Fourteen website browser states cover theme ownership, responsive family navigation and return links with no overflow or console errors.

Controlled performance comparisons cover 42 complete-export pairs on each of two processor cache domains, with 30 warmups, twelve retained samples per lane and explicit matching model, size, frame, font, theme and raster requests. All allocation comparisons meet the 10% budget; elapsed medians on the shared host flag different operations between domains. A bounded 24-sample confirmation and separate preparation, prepared-raster and complete-PNG checks establish representative steady-state work without a confirmed runtime hotspot above that budget. Several individual elapsed-time comparisons still exceed 10%; every original sample and flag is retained. This evidence does not establish an upper bound on single-export latency, a quiet-host result or cross-platform performance. Rendering-thread cycle counters are attribution diagnostics, separate from the elapsed-time gate.

Measured placement retains full semantic text when an optional label cannot fit, and records the omission. Fixed canvases cannot promise that arbitrary dense data or long text will remain fully painted. Explicit undersized fonts and bounds remain caller choices. Browser hover, selection, zoom and host controls belong to the interaction adapters; additional chart options and families remain in the [capability roadmap](chart-capabilities.csv). Visual acceptance covers the supported static layouts and observed fixtures, while consumer execution, public package availability and website deployment retain separate gates.

## Consumer handoff qualification

Prepared artifacts capture their own identity, accessibility text and semantic envelope. Reader or caller mutation cannot alter another export. Visuals applies ordered watermark layers without replacing those semantics; Stories owns motion and static-source selection. Interactive topology prepares the complete SVG needed by its presentation adapter. Explicit text alternatives take precedence over presentation subtitles, including localized host formatters.

Dense SVG scenes share identical rectangular clip definitions while preserving every node's clip reference, drawing order and embedding scope. The report fixture's largest matrix remains below its existing 1.5 MB SVG budget. All 371 compared PNGs retain identical bytes. Integrated owner qualification passes 3,269 tests, all four target frameworks, fresh Windows Native AOT execution, eight local packages and the 317-output gallery. The generated eight-assembly API ledger records 7,902 entries, including the integrated managed-image contracts. Artifact copies use the canonical `Clone()` method; interactive presentation callbacks receive one prepared topology rather than preparing a second layout from options.

The SVG performance comparison retains both cache-domain runs, their individual timing flags, a same-binary calibration and bounded compile/prepared-SVG/complete-SVG attribution. A focused topology confirmation stays within the 10% elapsed and allocation budgets on both domains. Calibration also exposes measurement variation; no samples or earlier flags are removed. These local results establish neither a universal single-export latency bound nor cross-platform performance.

The unsigned release candidate includes all eight package archives. Source and archive API bundles contain byte-identical DLL/XML payloads and validated manifests. Publication, feed read-back and normal downstream release builds remain separate gates; the [migration guide](migration.md) records the dependency order.
