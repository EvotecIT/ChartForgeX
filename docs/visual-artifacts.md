---
description: "Use typed visual artifacts to carry ChartForgeX models, metadata, natural sizes and export capabilities between renderers and host applications."
---

# Visual Artifacts

The [Graphite chart look specification](design/chart-look-spec.html) defines the shared light and dark colours, frame, typography, marks, legends, and interaction appearance. Open it with `#light` or `#dark` to inspect either theme.

Generate the focused Graphite gallery into a review folder:

```powershell
dotnet run --project ChartForgeX.Examples -c Release -- --graphite-only --output ./graphite-review
```

`graphite-gallery.html` pairs SVG and PNG for both themes, including arc, needle and linear gauges, compact donut legends, flat temporal marks, and a mixed dashboard. `svg-png-comparison.html` and its JSON manifest report dimensions, text edges and raster health. The full quality loop accepts `-ExamplesOutput` and `-PackageOutput` when review artifacts belong outside the checkout. Inspect the gallery before using `-UpdateVisualBaseline`.

Visual artifacts are reusable visual payloads that may be authored in code, Markdown, Mermaid, or future ChartForgeX fence languages. They are intentionally product-neutral. A report, dashboard, email, documentation generator, static site, or native host can decide how much of the artifact contract it wants to render.

The core model lives in `ChartForgeX.VisualArtifacts` and keeps three ideas separate:

| Concept | Purpose |
| --- | --- |
| `VisualArtifact` | Common envelope for id, title, subtitle, source language, export formats, natural size, accessibility, linked regions, legend, metadata, and the strongly typed model. |
| `VisualArtifactKind` | Broad family such as chart, topology, flow, sequence, table, timeline, visual block, or Mermaid-authored diagram. |
| `VisualArtifactSourceLanguage` | Where the artifact came from, such as native code, ChartForgeX markup, Mermaid, or Markdown. |

First-class artifact wrappers cover `Chart`, `ChartGrid`, `VisualCanvas`, `VisualStory`, `TopologyChart`, `FlowArtifact`, `SequenceArtifact`, `TableArtifact`, and every `IVisualBlock`. Each wrapper retains the typed model, natural logical size, static export capabilities, and the metadata that a document or authoring host can use without scraping SVG.

The envelope is not a renderer. It is the host-facing contract that lets native apps, static generators, and adapters inspect a visual without guessing from HTML or SVG.

## Chart Artifacts

Native ChartForgeX `Chart` models can be wrapped as visual artifacts with `Chart.ToVisualArtifact(...)`. This is the common artifact surface for code-created charts, `chartforgex chart v1` fences, `chartforgex timeline v1` fences, Mermaid pie charts, and Mermaid timelines when they naturally map to existing ChartForgeX chart renderers.

```csharp
using ChartForgeX.Core;
using ChartForgeX.VisualArtifacts;

var chart = Chart.Create()
    .WithTitle("Result Mix")
    .WithXLabels("Passed", "Warnings", "Failed")
    .AddPie("Checks", ChartPoints.FromValues(1260, 68, 10));

var artifact = chart.ToVisualArtifact(
    id: "result-mix",
    kind: VisualArtifactKind.Chart,
    sourceLanguage: VisualArtifactSourceLanguage.Native);

var svg = chart.ToSvg();
var png = chart.ToPng();
```

The artifact envelope declares SVG, PNG, HTML, and Office-adapter handoff support and keeps the strongly typed `Chart` in `VisualArtifact.Model`. Hosts should inspect the `Chart` model when they need data-aware behavior rather than scraping rendered markup.

### Retained native series visibility

`Chart.PrepareVisibility(context, view, viewport)` prepares an immutable whole-series visibility view for authored Line/Bar series. The native scene retains the complete source catalog, original observations, legend, full-data domains and completed layout. A null view uses ordinary preparation exactly; an empty default view keeps every series visible. Point, group and derived legend policies are rejected.

```csharp
var retained = Chart.Create().AddSeries(ChartSeries.CreateLine(
    new ChartSeriesId("reviewed"), "Reviewed", ChartPoints.FromValues(18, 70, 44)));
var view = new ChartVisibilityView(new[] { new ChartSeriesId("reviewed") });
var native = retained.PrepareVisibility(context, view);
var svg = native.ToSvg();
var png = native.ToPng();
```

`ChartVisibilityLabelLayering.PreserveOverlay` is the default. Visible series labels keep their existing global placement and overlay order; hidden series labels are omitted with their exact native mark owner. Labels still participate in the original full-source placement, so hiding a series does not reflow the survivors. Chart-owned annotations, axes, aggregate totals and other context retain their full-source facts and paint.

`GroupWithSeries` explicitly places each series plot and its already positioned labels in one isolated native composite. Series draw order is retained, but a series label now paints before later series and later chart context, instead of the global label overlay. Partial visibility motion with enabled labels requires this policy. SVG and native raster apply opacity once to the completed composite, including plot/label overlap. Retained font captures and the existing native opacity allocation/work limits apply; visibility does not promise unbounded layers or chart size.

## Static Decorations And Raster Metadata

The optional `ChartForgeX.Visuals` package decorates artifacts without changing the underlying chart, topology, table, flow, sequence, or visual-block model. Text and raster-image watermarks share anchors, offsets, edge padding, opacity, rotation, scale, and optional repetition across SVG, static HTML, and PNG output. `VisualArtifactRenderOptions` supplies native rendering options and raster metadata, including physical DPI:

```csharp
var options = new VisualArtifactRenderOptions {
    Raster = new RasterImageOptions { Dpi = 144 }
};
var watermark = VisualWatermark.FromText("CONFIDENTIAL");
watermark.Anchor = VisualCanvasAnchor.Center;
watermark.RotationDegrees = -28;
watermark.Opacity = 0.16;
var review = artifact.ToWatermarkedArtifact(options, watermark);

review.SaveSvg("review.svg");
review.SavePng("review.png", options);
```

These are visual export decorations. Word section watermarks, PDF page watermarks, document permissions, and security policy remain document-host concerns in OfficeIMO.

## Static Producers And Raster Limits

`IStaticVisualSource` lets an optional producer supply SVG and native pixels through `RenderSource` or `Model`. A producer that supports raster preallocation checks also implements `IMeasuredStaticVisualSource`. Its `MeasureRasterSize()` returns the exact positive, whole destination pixel dimensions, including output scale, using the same captured state as `RenderRgba()` without allocating a raster canvas. `artifact.MeasureRasterSize(options)` uses that capability or the existing native model's measurement.

Watermarks keep the underlying canvas dimensions and delegate measurement through nested decorations. The copied artifact retains its export policy and captured native options, so pixel limits are checked before raster allocation and encoded-output limits include the added watermark layers. Captured story artifacts expose their retained raster dimensions through the same contract.

Existing producers can render through `IStaticVisualSource` alone when no policy requires preallocation checks. A policy that needs those checks rejects an unmeasured producer before rendering pixels; authored natural size and serialized SVG are not used as allocation estimates.

## Flow And Topology Artifacts

Topology and flow are intentionally separate artifact contracts. Native topology models use `TopologyChart.ToVisualArtifact()`; `chartforgex topology v1` emits the same `VisualArtifactKind.Topology` envelope with a typed `TopologyChart` model. The topology envelope carries its natural size, accessibility text, Office handoff capability, and inspectable group/node/edge regions including links when present. `chartforgex flow v1` emits `VisualArtifactKind.Flow` with a typed `FlowArtifact` model containing lanes, steps, and connectors. Static flow previews currently project into `TopologyChart` for SVG/PNG rendering, but host APIs should inspect `FlowArtifact` rather than treating process flows as infrastructure topology.

`ToFlowArtifact()` restores an editable flow from its typed interchange envelope. Lane and connector order, step kinds, original step sizes, metadata and export formats remain native values. The existing flow compiler recomputes automatic layout. Captured presentation retains the paired theme, selected mode, exact caller insets and original frame request; source titles remain separate from caller headings. An explicit `Prepare(context)` uses the new context, and changing flow dimensions or padding replaces the retained layout request.

```csharp
var flow = FlowArtifact.Create("review")
    .WithTitle("Request review")
    .AddStep("receive", "Receive request", FlowArtifactStepKind.Start)
    .AddStep("publish", "Publish result", FlowArtifactStepKind.End)
    .AddConnector("receive", "publish", "Approved");

string json = flow.ToVisualArtifact().ToInterchangeJson();
FlowArtifact editable = VisualArtifactInterchangeEnvelope.FromJson(json).ToFlowArtifact();
editable.ConfigureStep("publish", step => step.Label = "Publish approved request");
byte[] png = editable.ToPng();
```

Topology and flow capture material context, theme and role font requests through the native text measurement and glyph pipeline. Explicit file fonts retain the bytes already read during preparation. Portable requests carry the actual primary and fallback faces and observed cluster decisions, without source paths or font registrations. Known requests reuse those faces after restoration; edited labels can record new fallback decisions. Caller font and frame objects remain independent of prepared output.

Portable font data allows 2 MiB per decoded root, 4 MiB across distinct roots and 32 roots inside the existing 8 MiB envelope. The general 64 KiB text, metadata and URL limits remain. Unsupported or uncovered native font captures reject portable export; static native output remains available. These limits bound the transported font data, not native file admission, font parsing, shaping or total process memory. Material captured font data uses common version 2; the ordinary default font request keeps its existing omission policy.

## OfficeIMO Handoff

ChartForgeX does not reference an Office, PDF, or Visio runtime. Its responsibility ends at a deterministic `VisualArtifact`, static SVG/PNG output, and a versioned semantic interchange envelope. `ToInterchangeEnvelope()`, `ToInterchangeJson()`, and `ToInterchangeUtf8Json()` preserve product-neutral groups, nodes, ports, details, edges, notes, presentation, accessibility, and prepared coordinates for topology, flow, and sequence artifacts. The UTF-8 JSON form is the boundary for processes and separately loaded PowerShell modules; it avoids CLR type-identity coupling without turning SVG into a data model.

The envelope is a typed semantic snapshot, not a property-bag protocol. `Kind` retains the broad artifact or authoring identity, while `Family` selects the reusable `Topology`, `Flow`, or `Sequence` semantic root. A Mermaid-authored sequence therefore remains `Kind = Mermaid` and `Family = Sequence`, so adapters can reuse sequence behavior without parsing source metadata. Entity `Role` values select stable group, node, edge, and annotation roles, and their typed family records carry the exact enums and values needed by adapters. Ports and scenario targets use typed topology enums; sequence message purpose (`Call`, `Return`, `Async`, or `Event`) is independent from solid or dashed presentation. Source adapters do not infer `Return` or `Event` merely from a dashed or cross-ended Mermaid operator; the exact operator remains in caller-owned source metadata when the grammar does not prove domain intent. Metrics use typed name/value records instead of a reserved key prefix. Free-form entity `Kind` remains available for labels and future custom kinds. `Extensions` contains caller-owned data only; ChartForgeX does not reserve extension keys for built-in behavior.

Use the in-memory envelope when ChartForgeX and the consumer share a compatible assembly identity. Use JSON at process, package, plugin, or PowerShell assembly-load-context boundaries:

```csharp
VisualArtifact artifact = topology.ToVisualArtifact();

VisualArtifactInterchangeEnvelope snapshot = artifact.ToInterchangeEnvelope();
snapshot.Validate();

byte[] portableSnapshot = snapshot.ToUtf8Json();
VisualArtifactInterchangeEnvelope restored =
    VisualArtifactInterchangeEnvelope.FromUtf8Json(portableSnapshot);
```

Schema version 1 permits additive JSON members. Readers ignore members they do not use, but reject unknown schema versions, undefined typed enum values, duplicate properties, invalid references, unsafe links or artwork, and payloads outside the public limits. A breaking semantic change requires a new schema version. Consumers should retain the original envelope or JSON beside a lossy native projection so a future adapter can use semantics the current target cannot represent.

The optional `OfficeIMO.ChartForgeX` adapter uses the two outputs for different jobs. SVG remains the flat vector or raster fallback for Word, Excel, PowerPoint, PDF, and unsupported diagram families. The semantic envelope projects supported topology, flow, and sequence artifacts into native editable OfficeIMO.Visio shapes, containers, connectors, Shape Data, hyperlinks, sequence messages, activations, notes, and fragments. Its conversion report exposes stable diagnostic codes for every normalization or semantic loss instead of requiring callers to parse warning text. OfficeIMO owns document placement, native Visio authoring, page sizing, document watermarks, PDF composition, and security.

For an extension or specialized CFX surface that only exposes SVG, pass its deterministic SVG markup through `OfficeVisualSource`. That route supports flat Office placement; native editable Visio requires a supported semantic interchange envelope.

```csharp
using ChartForgeX.Markup;
using ChartForgeX.VisualArtifacts;

var result = new VisualMarkupParser().Parse(markdown);
var flow = result.Artifacts.First(artifact => artifact.Kind == VisualArtifactKind.Flow);
var model = (FlowArtifact)flow.Model;
var svg = flow.ToSvg();
```

Topology can carry independent ink through `TopologyChart.PathDecorations`. Each immutable `TopologyPathDecoration` captures absolute M/L/C commands, fill/stroke colors, stroke width, an optional dash pattern, closure and an ID. It paints behind relationships and nodes without adding graph facts. Paths participate in the existing content bounds, fitting and clipping, and SVG/HTML/PNG use the same scene. `PathData` and typed JSON retain round-trip double precision; rendered SVG continues to use the renderer's existing numeric formatting.

Portable topology stores these captures in `Topology.PathDecorations`, in paint order, with typed numeric `commands` arrays. Empty collections omit the field and retain version-1 bytes. Nonempty paths, non-null node `SubtitleTextStyle`, and an optional `Presentation.Theme.TitleSize` require version 2. Nondefault topology title sizes use that nullable scalar and restore through the existing common frame typography; Graphite and omitted-size output retain version 1. Report/dashboard presets with a different title size intentionally use version 2 to retain their authored title size. Other theme roles and explicit `VisualFrame.TitleStyle` are not added to this narrow contract. The current reader accepts versions 1 and 2, rejects version-1 payloads carrying version-2 fields, and rejects unknown future versions. Older version-1 readers reject these payloads at the version gate. Rebuild consumers against a matching ChartForgeX assembly before loading version-2 snapshots, including package/process/PowerShell handoffs.

`ToTopologyChart()` restores the fitted snapshot coordinates and styles. `ToTopologyHierarchySource()` restores retained authored coordinates and role sizes instead. Neither route reparses source text or extracts ink from SVG. Validation rejects unsupported commands, malformed arity/subpaths, non-finite or excessive coordinates/paint lengths, resource-budget excesses and path IDs colliding with mapped or restored entity identities. `TopologyNode.SubtitleTextStyle` uses the existing `TextStyleOverride` for independent subtitle measurement and paint; null preserves ordinary output.

## TableArtifact

`TableArtifact` is a core model with a deterministic, host-neutral contract. Add `ChartForgeX.Visuals` for the static preview extensions shown here:

```csharp
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using ChartForgeX.Typography;

var table = TableArtifact.Create("alerts")
    .WithTitle("Open Alerts")
    .WithCapabilities(
        TableArtifactCapabilities.Search |
        TableArtifactCapabilities.Sort |
        TableArtifactCapabilities.Filter |
        TableArtifactCapabilities.MultiSelection |
        TableArtifactCapabilities.Copy |
        TableArtifactCapabilities.Export |
        TableArtifactCapabilities.Virtualization)
    .AddColumn("severity", "Severity", TableArtifactColumnType.Status)
    .AddColumn("system", "System")
    .AddColumn("count", "Count", TableArtifactColumnType.Number, TextAlignment.Right)
    .AddRow("row-1", "warning", "Directory", 12)
    .AddRow("row-2", "healthy", "Mail routing", 3);

var artifact = table.ToVisualArtifact();
var svg = table.ToSvg();
var png = table.ToPng();
```

`ChartForgeX.Visuals` produces a static preview by converting the core table model to a `ChartTable` visual block. Static output is suitable for generated reports, email, documentation, export previews, and visual galleries. Search boxes, sort headers, filters, keyboard selection, clipboard behavior, and virtual scrolling belong in a host or adapter.

Rich table interaction means grid-like behavior around the artifact: search, sort, filter, row or cell selection, keyboard navigation, copy, export, paging, remote data windows, and virtualization. If ChartForgeX grows that surface, it should be a production adapter or consuming-host feature. Examples that show the wiring belong in `ChartForgeX.Examples` or docs, not inside library packages.

## Capabilities

Capabilities are declarations, not UI implementation:

| Capability | Meaning |
| --- | --- |
| `Search` | A host may expose text search across searchable columns. |
| `Sort` | A host may expose one or more sortable columns. |
| `Filter` | A host may expose filters for filterable columns. |
| `SingleSelection` | A host may keep one selected row. |
| `MultiSelection` | A host may keep multiple selected rows. |
| `CellSelection` | A host may select cells instead of only rows. |
| `Copy` | A host may expose copy behavior. |
| `Export` | A host may expose supported export formats. |
| `Virtualization` | A host may page or window rows instead of loading all data at once. |

Columns also carry `Searchable`, `Sortable`, `Filterable`, `Copyable`, and `Exportable` flags so hosts can combine table-level capability with column-level permission.

## Query Boundary

Large tables should not force ChartForgeX to become a data grid. Use the query contract when rows come from a database, API, log store, or native host:

```csharp
public sealed class AlertTableProvider : ITableArtifactDataProvider {
    public TableArtifactQueryResult Query(TableArtifactQuery query) {
        // Apply query.SearchText, query.Sorts, query.Filters, query.Skip, and query.Take.
        // Return only the current window and the full matching row count.
        return new TableArtifactQueryResult(rows, totalRowCount);
    }
}
```

This keeps data access, authorization, paging, cancellation, incremental loading, keyboard focus, clipboard, and export workflow in the consuming host while preserving one reusable artifact model.

## Markup

Markdown can declare a table artifact through `chartforgex table v1` fences:

````markdown
```chartforgex table v1 {#alerts title="Open Alerts"}
capabilities search sort filter multiselect copy export virtualization
totalRows 1280

columns:
| id       | label    | type   | alignment | searchable | sortable | filterable |
| -------- | -------- | ------ | --------- | ---------- | -------- | ---------- |
| severity | Severity | status | left      | true       | true     | true       |
| system   | System   | text   | left      | true       | true     | true       |
| count    | Count    | number | right     | false      | true     | false      |

rows:
| severity | system       | count |
| -------- | ------------ | ----- |
| warning  | Directory    | 12    |
| healthy  | Mail routing | 3     |
```
````

The markup parser should stay a thin authoring layer. Reusable behavior belongs in `ChartForgeX.VisualArtifacts`; richer interaction belongs in production adapter packages or consuming native hosts. Demonstrations of that wiring belong in `ChartForgeX.Examples` or docs, not in the library packages themselves.

## SequenceArtifact

`SequenceArtifact` is the reusable interaction-diagram model for participants, messages, notes, block spans, and host metadata. It is product-neutral and can be created directly by .NET callers, produced from `chartforgex sequence v1` markup, or produced from Mermaid sequence diagrams.

```csharp
using ChartForgeX.VisualArtifacts;

var sequence = SequenceArtifact.Create("incident")
    .WithTitle("Incident Flow")
    .WithSubtitle("Native sequence preview")
    .WithSize(760, 420)
    .AddParticipant("user", "User", SequenceArtifactParticipantKind.Actor)
    .AddParticipant("api", "API")
    .AddParticipant("db", "Database", SequenceArtifactParticipantKind.Database)
    .AddMessage("user", "api", "Request")
    .AddMessage("api", "db", "Store", SequenceArtifactMessageLineStyle.Dashed)
    .AddNote(SequenceArtifactNotePlacement.RightOf, new[] { "api" }, "Processing")
    .AddBlock(SequenceArtifactBlockKind.Loop, "Retry", 0, 1);

var artifact = sequence.ToVisualArtifact();
var svg = sequence.ToSvg();
var png = sequence.ToPng();
```

The static preview renderer emits deterministic SVG/PNG with `data-cfx-role` hooks for participants, lifelines, messages, notes, and blocks. It is intentionally a preview and export surface. Rich host behavior such as step playback, selection, synchronized state, zoom, or native command surfaces belongs in adapter packages or consuming applications.

## IX Reuse Pattern

For IntelligenceX and similar hosts, the useful split is:

- ChartForgeX owns the artifact model, validation, deterministic preview rendering, SVG/PNG parity, and package contract.
- OfficeIMO or another Markdown-native layer owns Markdown parsing and passes already-discovered visual fences to `VisualMarkupParser.ParseBlocks(...)` or `MermaidVisualMarkupParser.ParseBlocks(...)`.
- The IX host maps product data into `Chart`, `TableArtifact`, `SequenceArtifact`, `TopologyChart`, Mermaid diagrams, or future ChartForgeX artifact models.
- Native UI packages own live controls, selection state, keyboard behavior, clipboard, export commands, virtualization, and shell integration.
- Markup packages provide authoring syntax and diagnostics, not product-specific layout or data collection.

That gives IX native visuals without turning ChartForgeX into an IX-only surface.


### Editable Waterfall authoring

A single native Waterfall chart exports typed editable data through `chart.ToVisualArtifact(id).ToInterchangeUtf8Json()`. The version-1 `Waterfall` family retains numeric-delta versus typed-item authoring, explicit checkpoint requests, stable source identities and supported native presentation. Read with `VisualArtifactInterchangeEnvelope.FromUtf8Json(bytes).ToWaterfallChart()`, edit through the existing series APIs, then prepare again. Native accumulation supplies balances, derived contributor identities and connectors; rendered geometry is not a second data source.

The payload is separate from the general diagram entities. It keeps Waterfall's exclusive single-series composition and does not add a generic chart serializer. Unsupported callbacks, process-local resources and presentation reject explicitly. A prepared chart can carry this detached payload through `prepared.ToArtifact(id, VisualArtifactKind.Chart, semanticInterchange)`; a prepared scene without supplied semantic data does not reconstruct authored Waterfall items from its marks. See [the portable field contract](../api-conventions/#editable-waterfall-interchange).


A topology edge can carry an immutable `TopologyPathGeometry` through `PathGeometry`. It captures absolute M/L/C commands with round-trip numeric precision. The complete path includes its endpoints and uses Manual coordinates; waypoints, route lanes and endpoint ports cannot be supplied at the same time. SVG and PNG paint the same path, while route observations use the existing bounded native flattener. Markers follow the path's endpoint tangents. Fitting and normalization transform its endpoints and controls together.

`TopologyEdge.Interpolation` chooses `Linear`, `Basis`, `BumpX`, `BumpY`, `Cardinal`, `CatmullRom`, `MonotoneX`, `MonotoneY`, `Natural`, `Step`, `StepAfter`, `StepBefore`, or `Rounded` over the resolved route. Null keeps ordinary route painting. Optional `InterpolationCornerBlendLength` applies a finite nonnegative logical prepass; zero leaves the resolved vertices unchanged. It requires an interpolation choice. Authored `PathGeometry` and interpolation cannot be combined.

The same numeric path supplies SVG/PNG paint, endpoint tangents, route observations, label placement and fitted bounds. A shared incoming tail is interpolated once through its source-order owner. Common version2 interchange retains the authored policy and finite numeric paint facts, including distinct trunk parts and measured caption allocations. Unchanged Manual imports reuse those facts. Node, port, waypoint, caption or policy edits recompute geometry; automatic layout resets the captured unit basis. Selection and highlight remain caller requests. The path contract keeps the existing 4096 commands per path, 1024 paths and 65536 aggregate command/vertex limits at portable boundaries; general JSON and rendering budgets still apply.

`TopologyNodeDetail.BadgeStyle` adds a surface around the detail's existing text and `TextStyle`. Card, CompactCard and Pill detail bodies share the measured badge plan; other visible display roles reject that new surface explicitly. Null geometry and null detail styles retain ordinary output. Non-null edge geometry or detail badge styles select portable envelope version 2, carrying ordered numeric commands or typed style fields instead of silently discarding ink in older readers. The `TopologyAuthoredPathBadgeRecipe` example builds a queue handoff without source-language input.


`TopologyEdge.LabelPosition` captures an immutable `TopologyLabelPosition`: finite coordinates and a `Center` or `TextBaseline` vertical anchor. A pinned position bypasses automatic label relocation; `LabelAnchorX/Y` still identify the independent leader attachment. TextBaseline uses the actual resolved first text row and ascent, including its final fitted font size. Explicit positions cannot be combined with label offsets.

Optional `SourceMarkerStyle` and `TargetMarkerStyle` reuse the native marker kind and edge paint. `Scale` multiplies nominal marker geometry, `Offset` moves it along the outward endpoint tangent in topology units, and optional `StrokeWidth` supplies topology units independently of Scale. Fitting captures geometry, offset and stroke once. Null keeps ordinary output. These typed layout values select the same version-2 envelope owner and survive clone/save/restore. Live force graph rewriting remains unsupported for authored geometry and these explicit layouts.


## Editable categorical diagrams

Journey and Timeline use typed version-2 families independently from the broad artifact `Kind`. A Mermaid Journey remains `Kind = Mermaid`, while `Family = Journey` carries ordered native sections, stable task IDs, ordinal scores and actor membership. `Family = Timeline` carries ordered categorical periods, their events and the reading direction. Native theme, configured padding, transparency, outer card, host-frame ownership, PNG scale and accessibility survive the same portable handoff.

```csharp
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

var journey = JourneyDiagramBlock.Create().WithTitle("Review journey")
    .AddSection("draft", "Review")
    .AddTask("read", "Read proposal", 3, new[] { "Reviewer" }, "draft");
var json = VisualArtifact.Create("review", VisualArtifactKind.VisualBlock, journey)
    .ToInterchangeJson();
var saved = VisualArtifactInterchangeEnvelope.FromJson(json);
saved.Journey!.Tasks[0] = new JourneyTask("read", "Read imported proposal", 4,
    new[] { "Reviewer" }, "draft");
var imported = saved.ToJourneyDiagramBlock();
```

Use `ToTimelineDiagramBlock()` for the corresponding Timeline family. Repeated section labels retain distinct stable IDs; labels such as `Before launch` or `2026` remain text and never become inferred dates or durations. The imported native block and its mutable options/collections are independent of the envelope and source producer. Immutable native row values and themes may be shared safely. Source-language identity, keyed source-span facts and caller metadata stay on the envelope beside that block; the reconstruction API does not produce a source AST.

The existing native limits remain in force: one through 512 tasks or periods, at most 128 sections, 64 distinct Journey actors and 2048 Timeline events. Invalid scores, references, duplicate IDs, empty diagrams and unsupported presentation fail before restoration. Generic portable JSON byte/value/text limits also apply. Version-1 downgrade, a mismatched family or multiple semantic roots cannot silently discard the typed data. SVG remains a separate static fallback for consumers that do not support the family.
