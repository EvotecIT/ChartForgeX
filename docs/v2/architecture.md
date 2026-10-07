# Shared rendering architecture

ChartForgeX prepares typed chart and diagram models into a detached scene. The scene contains final geometry, positioned text, source identities, descriptive regions and diagnostics. SVG serialization and native raster painting consume those decisions; neither backend performs family layout or parses the other backend's output.

The Phase 1 entry point is `IVisualRenderable.Prepare(VisualRenderContext)`. Existing exporters remain available while families migrate. This is an intentional staged replacement: the prepared route rejects unsupported options rather than silently returning a different chart. Phase 1 proves the ownership and rendering boundary, not completion of the v2 catalog.

## Package ownership

| Owner | Responsibility | Dependency direction |
| --- | --- | --- |
| ChartForgeX | Charts; topology, flow, sequence and other genuine diagrams; colors, geometry, fonts, shared frame/theme contracts, static scene painting and semantic interchange | No runtime package dependencies |
| ChartForgeX.Visuals | VisualCanvas, ImageComposition, watermark decoration, factual metric tiles, tables and lists | Core only |
| ChartForgeX.Stories | VisualStory, TerminalStory, motion timelines, animated topology and all GIF/APNG encoding | Core; static renderable inputs |
| Existing interactivity/HTML adapters | Browser events, navigation, controls, responsive recompilation and host wiring | Core and their applicable adapter contracts |
| Existing Mermaid/markup adapters | Parsing and typed source-model production | Core semantic contracts; optional capability bridges only when real callers require them |

Visuals and Stories are peers. A still wallpaper must not load an animation package. Stories accepts common static inputs without making the core dispatch over optional concrete types. Core retains static GIF input/first-frame decoding as an explicit input exception; GIF output belongs to Stories even when it has one frame.

Package extraction is Phase 4. Phase 1 retains the existing six package projects and their framework assets. It adds a generic prepared handoff beside the legacy dispatch, then later phases remove replaced switches and optional concrete types from core. No compatibility forwarding framework or global renderer registry is planned.

Keep product data collection, dashboard shells, filters, inspectors, wallpaper templates and document placement in consumers. PowerBGInfo is a required consumer of both core and Visuals. Its designed wallpaper effects remain supported capabilities. Progress handles and completed-state markers are data marks; they are not removed with decorative menus, selection checkboxes, navigation arrows and action buttons baked into still images.

## Contracts

| Contract | Phase 1 behavior |
| --- | --- |
| `VisualSize` | Finite positive logical width/height; raster density is separate |
| `VisualLayoutOptions` | Resolved fixed viewport and validated outer padding |
| `VisualFrame` | Common title, subtitle, legend position/visibility, optional content surface and transparent canvas |
| `VisualTheme` | Immutable light/dark color snapshots, typography scale and mark/layout settings |
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

The initial command set covers groups, rectangular clips, rectangles, ellipses, paths, numeric pie/ring slices and positioned shaped text. Arbitrary transforms, gradients, image nodes and outlined text are later scene work. Their absence does not remove the corresponding existing composition capabilities; those migrate with their owner and acceptance fixtures.

## Size, frame and overflow

Logical bounds are exact. Raster dimensions round each positive logical dimension upward, then multiply by output scale. Supersampling affects working coverage and allocation, not text positions or the logical scene. The pixel budget includes supersampled working pixels and is checked before allocation; the existing raster allocation guard supplies its independent byte ceiling.

Phase 1 accepts a resolved viewport. A host can measure its container and prepare again at a compact size; scaling an SVG `viewBox` preserves proportions but does not reflow its layout. Natural-content sizing, constrained aspect selection and a complete Fixed/Fit/Content policy remain later work. Do not advertise those modes as implemented by a placeholder enum.

The common frame measures headings and legend before handing a content rectangle to the family. Headings have a bounded two-line budget. Legend entries wrap into rows or a side strip; insufficient space produces diagnostics. Truncated display text retains its full source label in semantic metadata. Factual content is not turned into a chart series to obtain a frame.

Cartesian axes reserve measured strips within that content rectangle. Side legends and axis text therefore cannot claim the same space. Root titles are distinct from axis titles, donut center values and future VisualCanvas hero text. All use shared typography roles while keeping their own meaning.

## Theme ownership and schemas

HtmlForgeX owns Evotec color values. The embedded chart export is copied from revision `c9154b0d9c798061ecb178add498cac3c035ddec`, repository-root path `Generated/Tokens/ChartForgeX/evotec.chartforgex.tokens.json`, Git blob `efe1fcf67d73cf0a8c6b21ffeb1023062698c24a`. Its canonical source is `HtmlForgeX/Resources/Tokens/evotec.tokens.json`, version 1.1.0, blob `af906aceb366249828fc91322ece98199e3145ae`. Core does not reference an HtmlForgeX assembly.

The generated palette has `light` and `dark` objects and no numeric schema-version field. `VisualTheme.FromJson` imports this existing palette shape through the bounded shared parser. It supplies six series colors, surfaces, foregrounds, fill/ink status pairs and sequential/diverging ramps. It is not a full typography/layout schema.

The full ChartForgeX theme contract uses schema version 1, keeping the palette fields and adding `typography` and `geometry`. `FromThemeJson` and `ToThemeJson` are distinct from palette intake. Unknown full-theme versions fail explicitly. Schema versions describe portable contracts, independently of the NuGet package version.

The Graphite preset uses canonical HtmlForgeX colors and the chart look specification's layout/type scale. Renderers read presentation values from the theme or explicit model overrides. They do not select a different geometry implementation because of a palette. Existing `UseGraphiteLayout` branches are removed as their families migrate; Phase 1 does not claim those legacy branches have all gone.

Precedence is explicit model/element setting, then context/host override, then selected preset defaults. A context-supplied theme controls the prepared route; a legacy `ChartTheme` is not silently combined with it. Explicit series/point colors and configured text styles remain meaningful. Geometry choices such as aggregation and stacked layout are explicit model policy and apply independently of light/dark mode.

Severity, result outcome and operational state keep separate vocabularies. A universal status enum would lose meaning. Preserve arbitrary stable keys, localized labels, drawing order and unknown/not-observable/not-evaluated distinctions. Color pairs alone do not establish hatch/outline treatment equivalence: the generated palette omits those policies, and their owner mappings need dedicated fixtures during full migration. The initial chart proof requires explicit colors for status marks and rejects unsupported pattern treatments.

## Typography and portability

Preparation uses the existing font resolver, shaping engine and fallback faces. Text metrics and runs are retained in the scene. Export does not rediscover fonts. The public context returns a defensive font copy.

Native SVG text remains selectable, but its final glyph painting depends on the reader's fonts. Shared scene geometry does not promise browser/native pixel identity. Outlined SVG text and embedded-font product policy remain later decisions; neither is claimed by the current prepared exporter.

The review gallery and benchmarks use explicitly registered Carlito regular/bold fixtures under the SIL Open Font License 1.1, pinned to Google Fonts revision `3dd78844021e948ceb633d1dcee3f7885561b5d9`. Regular SHA-256 is `F6418F708BAEDE9789DAEF5D458C0F53D2A888AF9820E8062934E504FEDC6595`; bold is `BB5D20F79B82599EC72983597437373A80F2D2085FA91FC144FD74E876A594DB`. These are validation/example assets, outside runtime packages. Gallery CSS supplies those same faces for review. No font download is required by the rendering engine.

## Semantic artifacts and consumer handoff

Prepared pixels are separate from native editable diagram data. `PreparedVisual.ToArtifact` captures an optional existing semantic interchange envelope through its versioned bounded writer/reader. IDs, kind and supplied dimensions must match. The factory preserves logical dimensions, alternative text and descriptive regions, and keeps the source semantics beside the static scene.

A host can edit artifact titles, metadata and accessibility without mutating prepared pixels. Changing its natural size does not resize the scene; prepare again. Scene nodes are not serialized as Office interchange, and formatter delegates never become portable payloads.

Existing topology/flow/sequence interchange readers keep their explicit schema-version policy. Family/source IDs and entity relationships are retained for native Office consumers. Generic static artifacts may use SVG/PNG fallback; that fallback cannot be described as editable native diagram fidelity.

The [consumer capability ledger](consumer-capabilities.csv) covers PowerBGInfo, ImagePlayground, OfficeIMO.ChartForgeX, HtmlForgeX.ChartForgeX and TestimoX. Its owner fixtures and later consumer/package qualification are separate gates. Read-only inspection does not count as executing a consumer. No downstream source or dependency pins change in Phases 0–1.

## API and catalog closure

The [API conventions](api-conventions.md) define meaningful fluent verbs and purposeful exceptions. The [public member inventory](api-ledger.md) records the integrated source, exact overloads/defaults and planned ownership. It includes all six current runtime assemblies. Its fate column is a migration plan, not evidence that extraction is complete.

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
| 5 | Consumer migrations and release qualification | Published-package-only consumer builds and real document/module/wallpaper execution |

The active checklist lives in [TODO.md](../../TODO.md). Phase 1 ends with a reassessment; it does not authorize publishing packages or migrating consumers.

## Validation and performance

The integrated baseline is commit `fdb8fe16df11037c1228168c6984ed31316ae04f`: main `20e3443da9521bcd3371f68b232406596564191b` plus label-placement `5d2c4ada0f7b4aa1fb8704e9ba150ff3b9f0d5bb`. Its test run passed 2,384 tests on net8.0. The frozen net8 assembly SHA-256 is `43AE57D902E428A3A84674B16EA9DC6FB1C6D1194B3C60CB9E2F48D30FFE010E`. Build/test evidence is separate from the open prerequisite PR and from published package state.

Use `dotnet test ChartForgeX.sln -c Release` for code checks and `./Build.ps1 -Configuration Release` for the stable quality/package loop. Keep net472, netstandard2.0, net8.0 and net10.0 compile/assets checks; netstandard is not an executable test runtime. Native AOT and browser execution are separate evidence.

Keep unchanged-output benchmark cases byte-identical. Redesign cases must validate equivalent data, labels, bounds and visual quality before timing. Measure end-to-end SVG/RGBA/PNG, preparation and reused-scene export separately. Candidate-only prepared operations must not masquerade as a comparison with legacy end-to-end work.

Use the existing PowerForge benchmark orchestration, paired/rotated warm samples, allocations and output sizes. Investigate every unexplained regression above 10% per representative workload; do not average away a family regression or remove labels/quality to pass. Record source/assembly/font hashes, runtime/SDK, quality settings and distributions. The threshold is a gate to investigate, not a guarantee from one median.

Generated SVG must be inspected in a browser and native PNG viewed directly. Numeric health checks and XML assertions complement that inspection. Keep the legacy gallery intact while the isolated `generated-v2` catalog is reviewed. Baseline refresh follows observed approval, not the other way around.
