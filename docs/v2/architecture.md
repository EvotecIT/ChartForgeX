# Shared rendering architecture

ChartForgeX prepares typed chart and diagram models into a detached scene. The scene contains final geometry, positioned text, source identities, descriptive regions and diagnostics. SVG serialization and native raster painting consume those decisions; neither backend performs family layout or parses the other backend's output.

The entry point is `IVisualRenderable.Prepare(VisualRenderContext)`. Charts, grids and diagram exporters use the shared preparation route. Invalid family combinations and unsupported configurations fail explicitly. Chart families share the native scene, frame, theme and typography contracts. Static composition and animation have separate Visuals and Stories owners; downstream execution and package qualification remain distinct validation gates.

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

`SequenceArtifact.Theme` and `ThemeMode` supply authored presentation defaults for convenience exports. An explicit `Prepare(VisualRenderContext)` call uses the host's complete theme and font request. Prepared interchange captures those resolved colors and font independently from subsequent model changes; authored interchange preserves model defaults. Source adapters map supported presentation into this owner.

## Size, frame and overflow

Logical bounds are exact. Raster dimensions round each positive logical dimension multiplied by output scale upward. Supersampling affects working coverage and allocation, not text positions or the logical scene. The pixel budget includes supersampled working pixels and is checked before allocation; the existing raster allocation guard supplies its independent byte ceiling.

Phase 1 accepts a resolved viewport. A host can measure its container and prepare again at a compact size; scaling an SVG `viewBox` preserves proportions but does not reflow its layout. Natural-content sizing, constrained aspect selection and a complete Fixed/Fit/Content policy remain later work. Do not advertise those modes as implemented by a placeholder enum.

`VisualArtifactRenderOptions.HtmlSizing` controls only the standalone HTML host around completed SVG geometry. Automatic sizing preserves sequence label size in a keyboard-accessible horizontal scroll region, including prepared and decorated sequence artifacts. Other families retain their existing sizing. Hosts can choose `FitToWidth` or `PreserveSize` explicitly; SVG/PNG preparation and portable semantics stay independent of that page choice. Print output fits the completed scene to page width.

The common frame measures headings and legend before handing a content rectangle to the family. Headings have a bounded two-line budget. Legend entries wrap into rows or a side strip; insufficient space produces diagnostics. Truncated display text retains its full source label in semantic metadata. Factual content is not turned into a chart series to obtain a frame.

`VisualFrame.LegendTitle` is an optional measured heading above categorical legend entries. It shares the legend's height budget, and its complete text remains in a descriptive region if it cannot fit. A null value permits a producer-supplied title; an empty string suppresses it. Chart and grid frame copies preserve the title, styles and density budgets.

`ShowCard` controls the complete elevated frame independently of `ShowSurface`, which controls the content surface. `VisualTheme.CardRadius` shapes the outer card; `BarRadius` and family plot/mark settings remain independent. `CardShadowOpacity` and `CardShadowColor` produce shared rounded native layers for both SVG and PNG. Shadow spread stays within available outer padding and the authored viewport, without moving content. Zero opacity keeps a flat card. If padding leaves no room, the shadow is omitted with `frame.card-shadow-no-room`; the fixed viewport never grows.

Cartesian axes reserve measured strips within that content rectangle. Side legends and axis text therefore cannot claim the same space. Root titles are distinct from axis titles, donut center values and future VisualCanvas hero text. All use shared typography roles while keeping their own meaning.

## Theme ownership and schemas

HtmlForgeX owns Evotec color values. The embedded chart export is copied from revision `c9154b0d9c798061ecb178add498cac3c035ddec`, repository-root path `Generated/Tokens/ChartForgeX/evotec.chartforgex.tokens.json`, Git blob `efe1fcf67d73cf0a8c6b21ffeb1023062698c24a`. Its canonical source is `HtmlForgeX/Resources/Tokens/evotec.tokens.json`, version 1.1.0, blob `af906aceb366249828fc91322ece98199e3145ae`. Core does not reference an HtmlForgeX assembly.

The generated palette has `light` and `dark` objects and no numeric schema-version field. `VisualTheme.FromPaletteJson` imports this existing palette shape through the bounded shared parser. It supplies six series colors, surfaces, foregrounds, fill/ink status pairs and sequential/diverging ramps. It is not a full typography/layout schema.

The full ChartForgeX theme contract uses schema version 1, keeping the palette fields and adding `typography`, `geometry` and `effects`. `geometry.cardRadius`, `geometry.plotRadius` and `geometry.barRadius` are independent; `effects.cardShadowOpacity` and `effects.cardShadowColor` retain the chosen shadow, including color alpha. Older version-1 documents use documented defaults for optional fields. `VisualTheme.FromJson` and `theme.ToJson()` read and write this complete format. Unknown full-theme versions fail explicitly. Schema versions describe portable contracts, independently of the NuGet package version.

The Graphite preset uses canonical HtmlForgeX colors and the chart look specification's layout/type scale. Every visual model uses immutable `VisualTheme` plus explicit `ThemeMode`. A detached per-mode paint view can format colors for a backend, but owns no defaults or configuration. Palette changes do not select different geometry, text measurement or mark implementations. Local canvas backdrop, terminal chrome and syntax controls remain styles owned by their optional packages.

An explicit context supplies the prepared theme, mode and font; convenience exports resolve those values from the model. Explicit series/point colors and configured text styles remain meaningful. Geometry choices such as aggregation and stacked layout are model policy and apply independently of light/dark mode. Option callbacks clone, configure and install detached state atomically, with explicit setter intent preserved even for default values.

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

The [consumer capability ledger](consumer-capabilities.csv) covers [PowerBGInfo](https://github.com/EvotecIT/PowerBGInfo), [ImagePlayground](https://github.com/EvotecIT/ImagePlayground), [OfficeIMO.ChartForgeX](https://github.com/EvotecIT/OfficeIMO), a private HTML adapter and a private reporting consumer. Public documentation records the private consumers' technical requirements; their identities and exact source evidence stay in the private planning audit. Owner fixtures and later consumer/package qualification are separate gates. Read-only inspection does not count as executing a consumer.

## API and catalog closure

The [API conventions](api-conventions.md) define current authoring and update contracts. The [public member inventory](api-ledger.md) records the earlier migration signatures and assembly moves. It is historical migration evidence rather than an exhaustive qualification of current public options. Packed dependency, resource and execution qualification remains independent evidence.

The [chart capability roadmap](chart-capabilities.csv) explicitly maps all native series kinds through `NativeKinds`, alongside compound workflows. It records existing capabilities, missing options, priority lanes, dependencies and representative acceptance fixtures. `Verification` and `QualificationStatus` bound the current evidence; `FutureAcceptance` describes work that is not established by those fixtures. Priority is driven by real use and architectural reuse. Browser interaction, maps and advanced financial tooling have separate scope from static chart quality. Chart names alone do not establish completeness.

`PriorityLane` refers to the numbered delivery outcomes in [TODO.md](../../TODO.md#delivery-order). It describes the next kind of work for that row, not a license tier or a completion score.

Every migrated family needs typed data/geometry, shared frame/theme/text, SVG/native PNG proof, meaningful edge cases, catalog examples, semantics and performance evidence. Do not implement every catalog gap before the common pipeline has qualified.

## Delivery and acceptance

The shared native pipeline and peer-package extraction are implemented. The active implementation and acceptance checklist lives in [TODO.md](../../TODO.md); consumer requirements live in the [consumer capability ledger](consumer-capabilities.csv) and [migration guide](migration.md). Keep implementation, visual acceptance, package qualification, publication and installed-consumer execution separate. A pending external gate holds only work that depends on it.

## Composition and animation coverage

These capabilities extend beyond the native chart-kind catalog. Each row has its own producer, export and qualification boundary.

| Capability | Owner and current contract | Qualification boundary |
| --- | --- | --- |
| Factual blocks | Visuals owns the twelve factual table, list, metric, timeline, strip and header families. Shared paint and label owners produce static SVG and native pixels. Segmented proportions retain finite source values and distinguish an unrepresentable aggregate from zero. | Individual primitive and numeric regressions complement complete report acceptance. Dense layouts, arbitrary option combinations, fonts and actual assistive technology remain separate. |
| Mixed grids | Visuals owns chart/factual children, spans, fit modes, panel frames and stable composition target IDs. | Detached child/artifact and layout fixtures do not establish every span, density or native document placement. Mutable builders require serialized use. |
| Layered canvases | Visuals owns backdrops, text runs, tiles, images and completed-state marks. TechHorizon shares its exact cubic outline across SVG and density-aware native painting; authored halo colors include transparency. | SVG blur and native bounded halos retain different kernels. Effect parity, every local style combination and arbitrary font substitution are not implied. |
| Image composition | Visuals reuses core image, text, shape and transform services for ordered immediate composition and static raster containers. | Alpha, fit and placement need representative artifact proof; a public primitive inventory does not qualify every combination. |
| Visual stories | Stories owns ordered scenes, media, source/terminal/replay panels, outcomes, detached clocks and complete transcripts. | Actual exported frames and timings are checked independently of model construction. Arbitrary tokenizers, script shaping, assistive technology and physical print remain separate. |
| Terminal stories and replay | Stories owns commands, output, waits, viewport state, terminal tables and recorded playback. | The supported replay model does not promise arbitrary terminal control-language or installed-host compatibility. |
| Animation and motion | Stories owns repeatable frame sources, GIF palette encoding, lossless RGBA APNG, streamed output and ordered motion. Topology uses resolved core routes. | Container decoding, frame timing, caller stream ownership and native/browser motion have distinct proof. GIF quantization remains lossy; optical quality and platform playback need their own assessment. |
| Static artifacts and watermarks | Producer-owned static sources pass through the common artifact boundary; Visuals owns ordered text/image watermark decoration. | Existing-SVG decoration is a trusted-input operation, not sanitization. Native Office/print and all transform, font or image combinations remain separate qualification. |

The current review accounts for the public types in both optional owners and their export routes. Source inspection, targeted contracts, actual artifact execution and independent decoding are different evidence levels; none establishes universal option-combination coverage.

## Validation and performance

Use `dotnet test ChartForgeX.sln -c Release` for code checks and `./Build.ps1 -Configuration Release` for the stable quality/package loop. Keep net472, netstandard2.0, net8.0 and net10.0 compile/assets checks; netstandard is not an executable test runtime. Native AOT and browser execution are separate evidence.

`Build/Test-BrowserQuality.ps1` runs the full required Chromium contracts, including the native family matrix and trusted touch input. `Build/Test-BrowserCompatibility.ps1 -Browser firefox -OutputRoot <absolute-task-output>` runs a bounded compatibility set; select `webkit` for the second engine. HtmlTinkerX owns engine installation and session disposal. The compatibility set covers pointer/keyboard acquisition, update and remount, native-window export/reset, translated controls and RTL chrome. It does not imply that every Chromium-specific test or physical-device interaction ran in those engines. CI retains each engine's results and captures.

The optional interactivity benchmark isolates nearest-observation acquisition on 1,000- and 5,000-mark Scatter charts. A paired Apple M4/Chromium run used 30 warmups and nine retained samples per lane, rotated ordering and 16 queries per operation. Median batches fell from 153.47 to 26.81 ms and from 779.85 to 121.83 ms after ranking live geometry before probing paint. All 36 retained samples matched the independent source-index oracle. These local measurements include Playwright transport and exclude complete tooltip/crosshair painting; they establish neither full interaction latency nor cross-platform throughput. The [benchmark guide](../../Benchmarks/README.md) describes frozen inputs, validation and limits.

Keep unchanged-output benchmark cases byte-identical. Redesign cases must validate equivalent data, labels, bounds and visual quality before timing. Measure end-to-end SVG/RGBA/PNG, preparation and reused-scene export separately. Candidate-only prepared operations must not masquerade as a comparison with legacy end-to-end work.

Use the existing PowerForge benchmark orchestration, paired/rotated warm samples, allocations and output sizes. Investigate every unexplained regression above 10% per representative workload; do not average away a family regression or remove labels/quality to pass. Record source/assembly/font hashes, runtime/SDK, quality settings and distributions. The threshold is a gate to investigate, not a guarantee from one median.

Generated SVG must be inspected in a browser and native PNG viewed directly. Numeric health checks and XML assertions complement that inspection. The single published catalog at `Website/static/examples/generated/catalog.html` provides uniform family examples. The maintained curated tour also includes source-linked compositions and interactive scenarios; regenerate its selected assets from the same source. Dense review fixtures and numeric diagnostic pages remain build output. Baseline refresh follows observed approval.

## Native and optional producer boundaries

All supported chart kinds, chart grids, topology, flow and sequence have native prepared-scene producers. Convenience exports use those producers, and replaced static chart/diagram renderers are retired. This establishes common rendering ownership; per-chart options and their acceptance fixtures remain in the [capability roadmap](chart-capabilities.csv). Integrated quality, observed output and representative performance qualify the combined implementation separately, with current gates in [TODO.md](../../TODO.md).

Fixed viewport fitting preserves graph geometry and source alternatives, but can make a dense diagram's text small. Compact topology therefore needs an appropriate source layout or a larger host viewport. Scaling does not provide automatic content reflow. Hosted image artwork remains an SVG resource with a diagnosed canonical-glyph raster fallback; embedded artwork is the self-contained paired-output path.

Visuals and Stories are peers depending on core. Their ownership boundaries are:

1. Core owns generic prepared artifacts, neutral semantics, text, geometry and pixels. Producer extensions live with their optional models. `IStaticVisualSource` carries optional output through artifact dispatch without a global registry or concrete producer references.
2. Visuals owns VisualCanvas, ImageComposition, factual tiles/tables/lists, static composite grids and ordered watermark decoration. It reuses core text/image services and preserves wallpaper layer order, alpha, text fitting, offsets and chosen raised/glass effects. Core render options contain no watermark-specific types. Legacy factual and composition producers expose their completed output through the common boundary; the extraction does not convert them into native chart scenes.
3. Stories owns visual/terminal stories, transcripts/tokenizer contracts, motion timelines, topology animation and all GIF/APNG output, including single-frame GIF encoding. Core retains image input decoding. Stories reuses core image-fit geometry and resolved topology routes without depending on Visuals.
4. Qualify core-only, core+Visuals and core+Stories from isolated packed assets across all four target frameworks. Verify resources, namespaces and assembly-load boundaries; compositions and animations need their own execution fixtures.
5. Migrate consumers against the qualified owners in dependency order. PowerBGInfo needs wallpaper execution, ImagePlayground needs installed-module proof in both PowerShell runtimes, and OfficeIMO needs saved-document and editable-diagram proof. The HTML adapter and private reporting consumer retain separate package/dependency gates. Publication and consumer releases require their own authority.

Retain semantic completion markers and deliberate progress handles when removing decorative static controls. Keep genuine browser interaction in its adapter and product shells/data collection in consumers. The extraction introduces no additional runtime dependency or public scene framework.

## Visual acceptance

Charts and diagrams share the paired canonical colors, measured frame, typography roles and label-placement services. The default heading is 17px bold, outer padding is 24px, and scalar values have a separate 34px role. Frame legend settings resolve consistently across families; authored sizes, colors, axis bounds and family-specific scale switches remain effective. Automatic Cartesian and schedule ranges reserve room for endpoint marks. Annotation placement accounts for data marks, including the appointment chart's peak-window caption.

The curated catalog covers supported chart kinds with native wide and compact light/dark SVG and PNG examples. Primary tiles use one 400x280 preview canvas, meaningful titles and chart-only artwork. Full examples retain their semantic content and expose SVG, PNG and C# source. The canonical website imports declared artifact dependencies and preserves theme, family and return navigation. A compact example may use a smaller overview dataset so its labels remain readable; its source makes that choice visible.

The repository quality loop checks all four target-framework builds, native renderer tests, Mermaid conformance, Native AOT execution, visual baselines and isolated local packages. Browser acceptance covers compact/wide light/dark output, theme ownership, responsive family navigation and return links. Run these checks against the candidate being assessed; historical results do not establish its current status.

Controlled performance comparisons cover 42 complete-export pairs on each of two processor cache domains, with 30 warmups, twelve retained samples per lane and explicit matching model, size, frame, font, theme and raster requests. All allocation comparisons meet the 10% budget; elapsed medians on the shared host flag different operations between domains. A bounded 24-sample confirmation and separate preparation, prepared-raster and complete-PNG checks establish representative steady-state work without a confirmed runtime hotspot above that budget. Several individual elapsed-time comparisons still exceed 10%; every original sample and flag is retained. This evidence does not establish an upper bound on single-export latency, a quiet-host result or cross-platform performance. Rendering-thread cycle counters are attribution diagnostics, separate from the elapsed-time gate.

Measured placement retains full semantic text when an optional label cannot fit, and records the omission. Fixed canvases cannot promise that arbitrary dense data or long text will remain fully painted. Explicit undersized fonts and bounds remain caller choices. Browser hover, selection, zoom and host controls belong to the interaction adapters; additional chart options and families remain in the [capability roadmap](chart-capabilities.csv). Visual acceptance covers the supported static layouts and observed fixtures, while consumer execution, public package availability and website deployment retain separate gates.

## Chart option qualification

The generated [API inventory](api-ledger.md) records exported source symbols across the eight assemblies at its pinned revision. Source and local-package qualification remain separate from public package availability and downstream release builds.

The current gallery covers supported chart kinds and their declared variants in compact/wide light/dark output. Enabled browser checks exercise the native export matrix and interaction cases, including authored and derived target identities, contributor readout, roving keyboard navigation and retained zero/collapsed facts. Native previews respect their logical width. Representative SVG/browser and PNG outputs are visually inspected; browser font painting remains distinct from native rasterization. Further family options and adapter policies remain explicit in the capability roadmap.

## Consumer handoff qualification

Prepared artifacts capture their own identity, accessibility text and semantic envelope. Reader or caller mutation cannot alter another export. Visuals applies ordered watermark layers without replacing those semantics; Stories owns motion and static-source selection. Interactive topology prepares the complete SVG needed by its presentation adapter. Explicit text alternatives take precedence over presentation subtitles, including localized host formatters.

Dense SVG scenes share identical rectangular clip definitions while preserving every node's clip reference, drawing order and embedding scope. Artifact copies use the canonical `Clone()` method; interactive presentation callbacks receive one prepared topology rather than preparing a second layout from options.

The SVG performance comparison retains both cache-domain runs, their individual timing flags, a same-binary calibration and bounded compile/prepared-SVG/complete-SVG attribution. A focused topology confirmation stays within the 10% elapsed and allocation budgets on both domains. Calibration also exposes measurement variation; no samples or earlier flags are removed. These local results establish neither a universal single-export latency bound nor cross-platform performance.

Local package qualification checks all eight package archives and compares source and archive API bundles, including DLL/XML payloads and manifests. Publication, feed read-back and normal downstream release builds remain separate gates; the [migration guide](migration.md) records the dependency order.
