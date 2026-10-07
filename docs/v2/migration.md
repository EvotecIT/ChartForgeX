# ChartForgeX v2 consumer migration

This guide records the breaking-release target and the observed consumer contracts. Charts, grids, topology, flow and sequence diagrams use the shared native scene. Optional package extraction and actual consumer upgrades have their own qualification gates. The [capability ledger](consumer-capabilities.csv) maps inspected calls to their destination, migration recipe and acceptance fixture. Its `owner_implementation` and `owner_evidence` columns distinguish available source and owner fixtures from downstream qualification. A row with `status=planned` still requires consumer migration or qualification; an implemented owner path does not close that gate. The ledger covers observed consumer capabilities, not every exported member of ChartForgeX.

Consumer repositories remain unchanged. Revalidate their intended branch before migrating: a primary checkout, cached remote source, local project reference and installed NuGet package are different evidence boundaries.

| Consumer | Inspected source | Declared dependency / boundary |
| --- | --- | --- |
| [PowerBGInfo](https://github.com/EvotecIT/PowerBGInfo) | `v2-speedygonzales`, `b267584067aa58370ce8954423ec6582268950e7`, clean; three commits behind cached upstream | `Sources/PowerBGInfo/PowerBGInfo.csproj`: core `1.7.0`, optional source override. Core + Visuals required. |
| [ImagePlayground](https://github.com/EvotecIT/ImagePlayground) | `master`, `46bd2fd5e06b076c8c038e134239edf2639ea724`, clean; nine commits behind cached upstream `c1df15a` | Primary core/HTML pins `1.6.0`; cached upstream pins `1.8.1`. PowerShell surface needs core + Visuals + Stories and selected interaction adapters. |
| [OfficeIMO.ChartForgeX](https://github.com/EvotecIT/OfficeIMO) | Primary `a7207099322cd4496eca9c54990c835291df2a4d`, clean; relevant adapter also inspected at cached `origin/master fc7f5718c4e152257fedcc0651344a7c44a60c14` | `[1.6.0,2.0.0)` explicitly excludes v2. Review the major range and adapter contract together. |
| Private HTML adapter | Read-only source inspection | Keep the base adapter core-focused; qualify optional composition/story integration independently. |
| Private reporting consumer | Read-only source inspection | Restore a reproducible dependency closure before claiming consumer build proof. |

Paths in this guide and the CSV are repository-relative. Resolve the repository root through `EVOTEC_GITHUB_ROOT`, with the platform default described in `AGENTS.md` when unset. Source findings are not consumer builds, installed-module tests or package publication proof.

## Static HTML embedding

`chart.ToHtmlFragment()` and `grid.ToHtmlFragment()` embed a responsive prepared SVG. The inline SVG shrinks to the host's width and preserves its aspect ratio without a page stylesheet. Its logical `width`, `height` and `viewBox` remain the prepared viewport; proportional scaling does not perform a new compact layout.

Prepared chart-grid children use the shared `data-cfx-role="panel"` group and a scoped `data-cfx-source-id="panel-N"` identity. Update selectors that used the legacy `grid-panel` role. Semantic panel regions retain the same source index and contain the translated child regions.

Hosts embedding a detached artifact can request the same policy explicitly: `prepared.ToSvg(new VisualSvgOptions(idPrefix: "capacity-left", colorVariables: hostVariables, responsive: true))`. Supply the host namespace and colour mapping when replacing an export policy. Standalone `prepared.ToSvg()` retains exact viewport sizing.

Topology convenience SVG exports retain `TopologyRenderOptions.UseResponsiveSvg`, which defaults to true. Turning it off preserves the fixed logical viewport. Neither mode changes the prepared diagram layout.

## Package selection

| Workflow | Target dependency |
| --- | --- |
| Charts, diagrams, shared color/text/pixels, static export and semantic interchange | `ChartForgeX` |
| Wallpapers, social canvases, low-level image composition, factual tiles/tables/lists and watermark decoration | `ChartForgeX.Visuals` → core |
| VisualStory, TerminalStory, motion timelines, topology route/scenario animation and GIF/APNG output | `ChartForgeX.Stories` → core |
| Real browser interaction | Existing `ChartForgeX.Interactivity` / `.Html` and the host |

Visuals and Stories are peers. Stories consumes common renderable/static inputs; Visuals must not depend on Stories to render a still canvas. Core must not switch over optional concrete types, reference watermark options, or load animation assemblies for a chart. Static GIF input/first-frame decoding remains a deliberately retained core input capability; GIF encoding, including single-frame output, belongs to Stories.

Do not move genuine diagrams solely because their current names end in `Block`. Wardley, Venn, packet, git graph, fishbone and block-layout diagrams remain core. Plain factual table layout belongs to Visuals; the neutral tabular semantic/interchange contract remains available to core adapters and markup parsers.

## Chart export and presentation changes

`chart.ToSvg()`, `chart.ToPng()` and `chart.ToRgbaImage()` resolve the model's size, padding, theme values, visible frame, font request and raster density into the common rendering contracts. HTML hosts and image composition use the same prepared chart output. `SvgChartRenderer`, `PngChartRenderer`, `SvgChartGridRenderer` and `PngChartGridRenderer` are removed; use the model convenience methods or `Prepare(context)`. There is no separate Graphite family renderer.

Use `Prepare(context)` when a host supplies an exact viewport, paired theme, frame and font. The context owns those presentation values; model data, axis configuration and explicit text overrides remain inputs to preparation. `VisualLayoutOptions` accepts uniform padding or `ChartPadding` with independent edges. `VisualFrame` carries measured legend row and height budgets, and omitted legend entries retain their full semantic descriptions.

Preserve the distinction between the outer card and the plot when migrating theme or host settings:

| Existing model theme/input | Shared prepared contract |
| --- | --- |
| `ChartTheme.CornerRadius` | `VisualTheme.CardRadius` for the outer frame card |
| `ChartTheme.PlotCornerRadius` | `VisualTheme.BarRadius` for the prepared plot/mark default; it does not reshape the card |
| `ChartTheme.ShadowOpacity` / `ShadowColor` | `VisualTheme.CardShadowOpacity` / `CardShadowColor`, including authored color alpha |
| `ShowCard` with `Theme.UseCard` | `VisualFrame.ShowCard`; convenience exports also respect `HostOwnsFrame` |
| `ShowPlotBackground` | `VisualFrame.ShowSurface`, independently of the card |

Convenience exports perform these mappings. Explicit contexts set the frame and theme directly. Card shadows are shared native layers constrained by outer padding; zero opacity remains flat, and exhausted padding omits the shadow with a diagnostic. They do not enlarge the output or move plot coordinates. Transparent canvas settings suppress the canvas background while an explicitly enabled card remains visible.

For a categorical legend heading, pass `legendTitle` to `VisualFrame`. Null permits a producer title, such as a topology legend title; an empty string suppresses it. The common frame measures it within the legend height budget and retains its full semantic text when it cannot fit. Chart and grid frame copies preserve the title, styles and row/height budgets, so hosts do not need a separate heading layer.

`VisualTheme.Graphite()` and the mutable Graphite theme factories share the canonical paired color document. Custom model themes are copied into a detached request, so editing a theme after preparation cannot change the retained output. Explicit SVG color mappings retain paint roles; equal RGB values do not merge series, status, surface and text roles. Raster output uses the resolved static fallback colors.

Raster scale, supersampling and optional hinting are export settings in `VisualRenderOptions`; changing them does not repeat layout or change logical dimensions. Text decoration and superscript/subscript positioning are prepared numeric geometry shared by SVG and PNG, so old renderer-specific CSS selectors are not a stable integration contract.

When a host switches one exported SVG between themes, obtain its binding collection with `selectedThemeTokens.ToSvgColorVariables().GetVariablesForSvg(exportedSvg)`. The returned collection includes the base roles and only the derived contrast inks referenced by that SVG. Evaluate it separately for each theme using the same SVG. This preserves continuous heatmap colours and readable labels without a second layout pass. `Variables` alone contains the base roles and cannot supply every scene-dependent ink.

DateTime timeline and Gantt builders select a time axis by default. An explicitly configured scale, formatter, time zone or bounds remains in effect; numeric overloads retain linear axes. Heatmap, calendar and map scales require both their model visibility settings and the host frame's legend permission even though they have no categorical legend entries. Observed zero and missing data remain separate states.

## Shared static handoff

The target adapter flow is typed model → common renderable → immutable prepared output → SVG/PNG or semantic artifact. Compile once when producing both backends. Keep IDs, alternative text, semantic regions and family data with the artifact; the display scene and its SVG cannot replace native topology/flow/sequence data.

Sequence `ToSvg()` and `ToPng()` use the shared native scene. Their default request uses the model's width and height as minimum dimensions, retains its padding, and expands the measured content to fit. Use `sequence.Prepare(context)` for an exact fixed viewport or a custom theme/font; insufficient space fails explicitly. Export both formats from that prepared result to reuse one layout.

`sequence.ToVisualArtifact()` remains a lazy source envelope with authored dimensions and source semantics. It does not calculate preview geometry or validate a render layout. For a detached display and semantic snapshot, use `sequence.Prepare(context).ToArtifact(id, VisualArtifactKind.Sequence)`. Its envelope dimensions, node bounds, message routes and label bounds describe the prepared viewport; `chartforgex.source.width` and `chartforgex.source.height` retain the authored dimensions. Source-only envelopes do not provide preview regions until a display is prepared.

The owner fixtures exercise these adapter-shaped boundaries:

- A transparent chart with no card/header, explicit size, localized value formatting and an accessible name, exported to SVG and RGBA/PNG from one prepared result.
- A donut using the same frame/theme/size contract, including zero/missing data and a meaningful center value.
- A tiny topology and multiline sequence carrying stable entity IDs and retained semantic interchange.
- An artifact carrying SVG bytes, identity/title, alternative text and versioned interchange JSON without a Visuals or Stories concrete-type reference.
- Canonical light/dark token intake, explicit model overrides and an immutable theme snapshot.

These are owner fixtures. All chart-family producers are implemented; the Phase 2–3 integration, visual inspection and performance gates qualify their combined behavior separately. Running actual Word/Excel/PowerPoint/PDF/Visio consumers, packaging PowerShell modules and rendering full wallpapers remain consumer qualification gates. Fixture source alone does not establish that those gates passed.

The source handoff `chart.ToVisualArtifact("cpu-load")` stores the mutable chart and prepares it on demand when rendering. The detached route stores completed layout: call `chart.Prepare(context)`, then `prepared.ToArtifact(...)`, and retain the existing portable SVG/JSON handoff. Use the detached route to export several formats without repeating preparation. This example is also exercised by `PreparedVisualArtifactTests.StaticArtifactUsesPreparedOutputAndKeepsPortableHostMetadata`:

```csharp
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

var chart = Chart.Create().WithTitle("CPU load")
    .WithAccessibility(a => a.WithTextAlternative(
        "CPU load", "Synthetic CPU utilization.", "pl-PL"))
    .AddLine("CPU", new[] {
        new ChartPoint(0, 20), new ChartPoint(1, 35), new ChartPoint(2, 28)
    });
var context = new VisualRenderContext(
    layout: new VisualLayoutOptions(new VisualSize(320, 200), padding: 16),
    frame: new VisualFrame(title: "CPU load", showLegend: false));
PreparedVisual prepared = chart.Prepare(context);
VisualArtifact artifact = prepared.ToArtifact("cpu-load", VisualArtifactKind.Chart);
byte[] officeVisualSvg = Encoding.UTF8.GetBytes(artifact.ToSvg());
byte[] officeVisualInterchangeJson = artifact.ToInterchangeUtf8Json();
byte[] png = artifact.ToPng();
string html = artifact.ToHtmlPage();
```

The artifact copies prepared region bounds and accessibility, and declares the prepared viewport as its natural size. Its title/accessibility/metadata remain editable host-envelope fields. Artifact SVG/HTML exports apply current host text alternatives, language and decorative state, with generated SVG IDs scoped to the artifact identity. These overrides leave the immutable prepared SVG snapshot, geometry and pixels unchanged. To change visual content or dimensions, prepare again. Replacing `NaturalSize` with dimensions different from the prepared viewport is rejected during rendering/interchange export.

Prepared topology, flow and sequence producers carry their typed semantic snapshot. Calling `prepared.ToArtifact(id, kind)` retains that snapshot automatically, with the requested artifact ID and prepared dimensions. Node/group bounds, resolved routes and label bounds describe the displayed diagram; authored options and source metadata remain available through the versioned interchange. No diagram data is inferred from scene commands.

Topology artwork retains its aspect-ratio policy, highlight dimming and selected outline. Embedded bitmap and inline SVG artwork become immutable pixels shared by both exporters. Safe host-managed image references remain references in SVG and HTML; native raster export uses the canonical glyph fallback and reports the unresolved external resource. Rendering does not fetch URLs or read image paths. Supply embedded artwork when a self-contained SVG/PNG pair must display the same image.

Topology surface tints use premultiplied sRGB mixing, preserving the authored accent alpha in both SVG paint expressions and native raster colors. A translucent accent therefore produces a lighter tint than an opaque accent with the same RGB channels. Highlight dimming applies once after tint resolution; regenerate stored image baselines that assumed an opaque accent blend.

The typed snapshot is copied during preparation, before lazy JSON serialization. Mutating an original semantic DTO or its nested collections after `Prepare` cannot change a later artifact or interchange export. Portable JSON size/depth/collection limits apply when that interchange is requested, including `ToArtifact(...)`; they do not prevent static SVG/PNG rendering of the prepared scene. A consumer that requires native editable semantics must qualify that portable boundary separately from picture export.

When a host supplies its own envelope, pass it as the third argument to `prepared.ToArtifact(id, kind, semanticInterchange)`. The factory snapshots it through the versioned writer/reader, requires matching ID/kind and any supplied dimensions, and returns an independent envelope on each read. The host is responsible for matching those semantics to its visual. An ordinary chart without supplied diagram semantics emits family `None` rather than fabricated diagram nodes or edges.

`PreparedVisualArtifactTests.NativeDiagramSemanticsSurviveCallerAndReaderMutation` exercises explicit envelope capture and independent caller/reader mutation. `PreparedTopologyTests`, `PreparedSequenceTests` and the native diagram fixtures cover producer-owned prepared geometry and semantics. Use the automatically retained envelope when Office placement needs the prepared coordinate system; explicitly supplying an authored source envelope preserves the coordinates in that envelope. Native Office projections still require consumer qualification against the retained bounds, routes, groups and fidelity diagnostics.

## PowerBGInfo: preserve the wallpaper engine

`BgInfoRasterImage.cs` uses ImageComposition for the main image, text measurement/drawing, image placement, resizing, RGBA handoff and file encoding. Adding only a VisualCanvas package reference cannot migrate it. Reference Visuals for composition and retain shared core `ChartColor`, text/font/measurement and pixel types.

Migrate the generic engine calls together: transparent/opaque surface creation and image loading; positioned images with fit/opacity; styled text and matching measurements; resize and RGBA export; still encoding with explicit JPEG background/quality. Preserve font weight, italic, underline/strikethrough, baseline and case rather than flattening text to plain strings. Keep extension dispatch such as `.jpe`/`.jfif` and `.pnm` in the consumer boundary or shared codec mapping as appropriate.

Keep PowerBGInfo's hero template, left/center/right lane policy, synthetic machine data and wallpaper placement in PowerBGInfo. Visuals supplies measured layers, hero text runs, badges/images, icons, key/value content, text fitting and safe placement. Preserve CenterRight, ContrastBox, MiniCharts, Raised3D and RaisedSections examples, including per-tile dimensions, independent sibling sizes, offsets, narrow-lane shrink/non-overlap, alpha and opt-in raised/glass/outline effects. Flat default charts do not remove these chosen wallpaper effects.

Map all consumed chart families: bar/horizontal bar; line/area/sparkline; gauge/circle/radial bar/bullet; pie/donut; progress; pictorial. Preserve formatter and font-role overrides, legend/point-legend/data-label settings, min/max/target/ranges, center/status labels, palette, smoothing, thickness/radius/columns and supersampling. Dense trends retain `ChartResolutionPolicy.Trend()` and provenance; categorical and short series remain exact.

These specialized models have native producers. Keep the caller's declared gauge/circle bounds and raw values; clamping the visible progress does not replace source values or formatted text. Bullet rows share one displayed domain while retaining their declared row bounds, targets and ranges. An explicit `WithValueFormat(...)` or value formatter governs values, targets and generated numeric ticks unless an axis-specific formatter overrides those ticks; the default still uses grouped value labels and compact ticks. Pie/donut aggregation uses `MaximumPieSlices` independently of theme and retains contributing indexes for `Other`. Zero pie values remain legend categories; zero polar-area values retain their angular slot. Donut center text, progress handles and pictorial partial fills remain explicit options. Custom pictorial paths become numeric geometry rendered by both backends; the legacy PNG fallback shape does not replace an accepted custom contour.

Check the remaining plot dimensions before preparing a nested chart. `BgInfoChartRenderer.Render` currently clamps the space after host padding and title/value text to one pixel, then supplies eight pixels of chart padding on every edge. A fixed `VisualLayoutOptions` request requires a positive interior; widths or heights of sixteen pixels or less cannot retain that padding. Keep the requested outer wallpaper dimensions and choose an explicit consumer policy: skip an exhausted plot, or reduce its internal padding to fit. Do not silently enlarge the chart to satisfy the layout constraint. Include a short tile with a tall title in consumer qualification.

Topology overlays remain fixed-size transparent outputs, with viewport fit, groups, labels/status badges and chosen layout/style. The 560×310 bottom-right example with offset 34 must composite without silent content-size growth. Required wallpaper qualification includes 2560×1080 and 4K, detailed/light/dark backgrounds, direct RGBA composition and final JPEG flattening.

No menu, selection-checkbox, navigation-arrow or action-button use was observed in these wallpaper routes. Progress handles are currently explicit configurable marks and default on: retain or deliberately migrate that style option rather than deleting it as dashboard chrome.

## ImagePlayground: migrate types and the module together

The ChartForgeX integration belongs primarily to `ImagePlayground.PowerShell`; the base imaging library need not gain ChartForgeX references. Assign chart/diagram cmdlets to core, canvas/blocks/watermarks to Visuals and story/motion/animated-topology operations to Stories. Keep optional TreeSitter tokenization outside ChartForgeX: `ImagePlayground.Syntax.TreeSitter` follows the story tokenizer contract into Stories.

`New-ImageStory` produces VisualStory; `New-ImageVisualStory` currently produces VisualGrid plus VisualMotionTimeline. Preserve both operator workflows while sharing timing/export ownership. `New-ImageConsoleStory` retains terminal tabs, commands, output, tables, pauses, playback and transcript. Route `New-ImageTopology` GIF/APNG output through Stories; static topology and actual HTML controls retain their own owners.

Topology SVG motion currently uses the explicit `TopologyMotionSvgAdapter` over native prepared routes and node positions. It adds animation to the exported SVG without changing the static prepared scene; raster motion selects a sample from the same route plan. `NativeDiagramExportTests.TopologyMotionSamplesNativeResolvedRouteWithoutChangingBaseSceneOrSourceOptions` is the owner fixture for this boundary. This source is implemented; its current integration execution remains pending. Phase 4 moves the adapter and GIF/APNG animation policy into Stories, then qualifies the ImagePlayground animated workflows against the extracted package. The native SVG adapter alone does not qualify GIF/APNG exports or complete that package move.

Package moves affect compiled parameter/output types, enums, type accelerators, assembly load context, binary bundling, namespace imports, generated help and examples. Update explicit-source, sibling-source and NuGet modes coherently. Qualify installed module imports and moved-type parameter binding in Windows PowerShell 5.1 and PowerShell 7, with no neighboring source projects available.

Replace the optional-concrete-type dispatch in `ConvertTo-ImageVisualArtifact` with package-owned producers/common contracts. Preserve these portable properties exactly or provide an explicit versioned migration: `OfficeVisualSvg`, `OfficeVisualInterchangeJson`, `OfficeVisualInterchangeSchema`, `OfficeVisualInterchangeVersion`, `OfficeVisualKind`, `OfficeVisualId`, `OfficeVisualTitle`, `OfficeVisualAlternativeText`. They are an assembly-load-context-safe handoff, not redundant copies of model objects.

Apply ordered watermark decorators from Visuals before static export; core render options no longer carry VisualWatermark. Preserve DPI, topology preset/diagnostic overlays, one artifact per path and pass-through behavior. Checked list and completed timeline entries are semantic completion data: retain accessible read-only markers. Remove only decorative static controls, preserving real interaction in HTML adapters.

## OfficeIMO: preserve pictures and editable semantics

Keep the base adapter dependent on the core common artifact and versioned interchange. Optional composition/story results arrive as common static artifacts or optional thin extensions. Office chart users should not acquire animation code through an overload.

Retain `ToOfficeVisual`, portable `OfficeVisualSource`, `ToOfficeDrawing`, Word/Excel/PowerPoint/PDF placement and `ToOfficeVisio`. Preserve exact resolved dimensions and pixel-to-point/DPI behavior, alpha, SVG and PNG fallback payloads, alternative text/decorative state and fidelity reports. Native Visio projections must keep topology/flow/sequence nodes, edges, groups, routes and semantic metadata; a screenshot is not an equivalent migration.

Existing fixtures cover SVG fidelity reject/rasterize/preserve policy, text/image watermarks, static tables, repeated placement, accessibility and exported document content. Reuse those observable contracts against packed v2 owner artifacts. Watermark application becomes a Visuals decoration step; native Visio must continue reporting presentation that it cannot project. Keep JSON ingestion across assembly load contexts; do not replace it with casts between consumer-local copies of moved types.

## Private adapters: one host theme mapping

The private HTML adapter's host owns report cards, menus, selection controls, navigation and actions. Keep the adapter core-focused for charts/diagrams, scoped color variables, artifact embedding and interaction wiring. Typed canvas/block/story conveniences may require optional Visuals/Stories adapters; their package identities remain a later type-ledger decision. A common static artifact can avoid optional concrete overloads where sufficient.

Migrate `ReportChartContext.CreateChart` once: transparent no-card/no-header plots, explicit mark backdrop, flat marks, token roles, culture/date labels, accessible descriptions and status category mappings. Preserve scoped SVG IDs and nested light/dark islands. Live color binding does not promise layout reflow when font metrics change; static PNG resolves explicit literal colors.

The private reporting consumer keeps domain data, report layout, category keys and localized labels. Its chart factory/theme mapping changes in the adapter, without per-report palette copies. Preserve severity/outcome/operational-state distinctions, unknown/not-observable/not-evaluated treatments, null versus zero, category and legend order, time zone labels and interval semantics. Qualify grouped availability timelines, sparse health matrices, overlapping/open jobs, hour/weekday/calendar heatmaps and replication/documentation topology in compact/wide and light/dark output.

The private reporting consumer's local dependency overrides reference unavailable development snapshots. A later consumer task must restore a reproducible dependency closure before claiming build proof; nominal package pins do not establish that newer report APIs are published.

## Canonical color import

HtmlForgeX owns the values; ChartForgeX owns its generic theme contract. The imported input is **the generated chart palette**, not the full owner token schema:

| Evidence | Pinned identity |
| --- | --- |
| Owner source | `HtmlForgeX/Resources/Tokens/evotec.tokens.json`, token schema version `1.1.0` |
| Generated export | `Generated/Tokens/ChartForgeX/evotec.chartforgex.tokens.json` |
| Generator / contract | `Tools/HfxTokens/Generator/DesignTokenTargetWriters.cs`, `Build/Build-HfxTokens.ps1`, `Docs/DESIGN_TOKENS.md` |

The export has `name`, `decision`, `source`, `notes`, `light`, `dark`; it has no numeric schema-version field. Each mode supplies surface page/card/cardAlt/line/lineStrong, text primary/secondary/muted, chrome background/text/textMuted/accent, accent base/soft, five severity fill/ink pairs, outcome pass/neutral pairs, state maintenance pair, six ordered series colors, five sequential steps and diverging negative-three/neutral/positive-three. Values are resolved hex colors. Series colors carry category identity, not status.

The full owner source defines aliases and treatments omitted from that palette export: fail uses severity when known, otherwise neutral; notEvaluated is neutral plus hatching; couldNotEvaluate is neutral plus dashed outline; up→pass, degraded→medium, down→critical, recovering→low, unknown/notObservable→neutral plus hatching. Preserve separate semantic keys and legend labels even when colors match. A shared presentation model does not collapse these vocabularies into one enum.

The export also omits typography, spacing, radius, elevation, motion and accessibility overrides. Phase 1 must document its explicit defaults/role mapping for those fields and never invent another authoritative Evotec palette. Expanding the export is later token-owner work. Do not import the full owner source with a parser expecting the generated chart shape.

Phase 1 color intake does not establish universal status-treatment rendering. Hatch patterns, dashed-outline outcomes, forced-colors behavior and status legend treatment must be qualified as their mark families migrate in Phase 2 and later. Matching neutral colors alone is insufficient proof that not-evaluated, could-not-evaluate, unknown and not-observable remain distinguishable.

## Qualification sequence and open gates

- [x] Compile and inspect the Phase 1 shared-frame/theme/direct-scene and adapter-shaped owner fixtures. The [executable gallery producer](../../ChartForgeX.Examples/V2Examples.cs), [diagram fixtures](../../ChartForgeX.Examples/V2Examples.Diagrams.cs), [review gallery](../../Website/static/examples/generated-v2/index.html) and [artifact handoff tests](../../ChartForgeX.Tests/PreparedVisualArtifactTests.cs) provide the implemented examples. This qualifies the owner proof, not downstream consumers.
- [x] Close the [integrated public type/member ledger](api-ledger.md): 773 public types and 7,678 records across the six existing runtime assemblies. Its planned API fates guide later removals and package extraction; they do not claim those migrations are implemented.
- [x] Complete the Phase 1 representative performance comparison: all 30 paired workloads remain within 10% elapsed/allocation, with 756 retained samples and no failures. Earlier flags were investigated before qualifying the stable runtime. Larger redesigned SVG/PNG payloads remain an explicit cost in the [reassessment](architecture.md#phase-1-reassessment).
- [ ] Complete integrated Phase 2–3 validation and visual/performance qualification of the implemented core family producers.
- [ ] Qualify the core-focused private HTML adapter and OfficeIMO candidates, preserving artifact/interchange contracts.
- [ ] Extract Visuals and qualify PowerBGInfo plus static ImagePlayground; extract Stories and qualify animated workflows.
- [ ] Restore the private reporting consumer's reproducible dependency closure and qualify representative reports.
- [ ] Pack all target assets into a clean feed; record hashes/dependency graphs and run package-only consumer proof without project-reference fallbacks.
- [ ] With separate release authority, publish/verify owner packages, then repin and release consumers in dependency order.

Do not publish consumers against a locally packed or unpublished owner candidate. Local-source proof is useful and must be labeled separately. No compatibility probes, fallback copies of rendering logic or temporary project references should conceal package publication lag.
