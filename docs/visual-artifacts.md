# Visual Artifacts

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

## Static Decorations And Raster Metadata

`VisualArtifactRenderOptions` applies export-wide decoration without changing the underlying chart, topology, table, flow, sequence, or visual-block model. Text and raster-image watermarks share anchors, offsets, edge padding, opacity, rotation, scale, and optional repetition across SVG, static HTML, and PNG output. PNG output can also carry physical DPI metadata:

```csharp
var options = new VisualArtifactRenderOptions {
    Raster = new RasterImageOptions { Dpi = 144 }
};
var watermark = VisualWatermark.FromText("CONFIDENTIAL");
watermark.Anchor = VisualCanvasAnchor.Center;
watermark.RotationDegrees = -28;
watermark.Opacity = 0.16;
options.Watermarks.Add(watermark);

artifact.SaveSvg("review.svg", options);
artifact.SavePng("review.png", options);
```

These are visual export decorations. Word section watermarks, PDF page watermarks, document permissions, and security policy remain document-host concerns in OfficeIMO.

## Flow And Topology Artifacts

Topology and flow are intentionally separate artifact contracts. Native topology models use `TopologyChart.ToVisualArtifact()`; `chartforgex topology v1` emits the same `VisualArtifactKind.Topology` envelope with a typed `TopologyChart` model. The topology envelope carries its natural size, accessibility text, Office handoff capability, and inspectable group/node/edge regions including links when present. `chartforgex flow v1` emits `VisualArtifactKind.Flow` with a typed `FlowArtifact` model containing lanes, steps, and connectors. Static flow previews currently project into `TopologyChart` for SVG/PNG rendering, but host APIs should inspect `FlowArtifact` rather than treating process flows as infrastructure topology.

## OfficeIMO Handoff

ChartForgeX does not reference an Office or PDF runtime. Its responsibility ends at a deterministic `VisualArtifact` with SVG/PNG renderers, natural pixel size, accessibility text, metadata, and inspectable regions. The optional `OfficeIMO.ChartForgeX` adapter converts that envelope into OfficeIMO drawing primitives, preferring editable/vector SVG and reporting any fallback or unsupported SVG feature. OfficeIMO then owns Word, Excel, PowerPoint, and PDF placement, page sizing, native document watermarks, PDF composition, and document security. This keeps CFX dependency-free while allowing every supported chart and diagram family to travel through one document pipeline.

For an extension or specialized CFX surface that only exposes SVG, pass its deterministic SVG markup through `OfficeVisualSource`. That portable route also avoids CLR type identity coupling between separately loaded PowerShell modules or processes.

```csharp
using ChartForgeX.Markup;
using ChartForgeX.VisualArtifacts;

var result = new VisualMarkupParser().Parse(markdown);
var flow = result.Artifacts.First(artifact => artifact.Kind == VisualArtifactKind.Flow);
var model = (FlowArtifact)flow.Model;
var svg = flow.ToSvg();
```

## TableArtifact

`TableArtifact` is the first reusable artifact model in this layer. It replaces browser-table assumptions with a deterministic, host-neutral contract:

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

The core renderer produces a static preview by converting the table to a `ChartTable` visual block. Static output is suitable for generated reports, email, documentation, export previews, and visual galleries. It does not implement search boxes, sort headers, filters, keyboard selection, clipboard behavior, or virtual scrolling in ChartForgeX core.

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
