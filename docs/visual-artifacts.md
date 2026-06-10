# Visual Artifacts

Visual artifacts are reusable visual payloads that may be authored in code, Markdown, Mermaid, or future ChartForgeX fence languages. They are intentionally product-neutral. A report, dashboard, email, documentation generator, static site, or native host can decide how much of the artifact contract it wants to render.

The core model lives in `ChartForgeX.VisualArtifacts` and keeps three ideas separate:

| Concept | Purpose |
| --- | --- |
| `VisualArtifact` | Common envelope for id, title, subtitle, source language, export formats, natural size, regions, legend, metadata, and the strongly typed model. |
| `VisualArtifactKind` | Broad family such as chart, topology, flow, sequence, table, timeline, visual block, or Mermaid-authored diagram. |
| `VisualArtifactSourceLanguage` | Where the artifact came from, such as native code, ChartForgeX markup, Mermaid, or Markdown. |

The envelope is not a renderer. It is the host-facing contract that lets native apps, static generators, and adapters inspect a visual without guessing from HTML or SVG.

## Chart Artifacts

Native ChartForgeX `Chart` models can be wrapped as visual artifacts with `Chart.ToVisualArtifact(...)`. This is the common artifact surface for code-created charts, `chartforgex chart` fences, Mermaid pie charts, and Mermaid timelines when they naturally map to existing ChartForgeX chart renderers.

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

The artifact envelope declares SVG, PNG, and HTML export support and keeps the strongly typed `Chart` in `VisualArtifact.Model`. Hosts should inspect the `Chart` model when they need data-aware behavior rather than scraping rendered markup.

## TableArtifact

`TableArtifact` is the first reusable artifact model in this layer. It replaces browser-table assumptions with a deterministic, host-neutral contract:

```csharp
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

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
    .AddColumn("count", "Count", TableArtifactColumnType.Number, VisualTextAlignment.Right)
    .AddRow("row-1", "warning", "Directory", 12)
    .AddRow("row-2", "healthy", "Mail routing", 3);

var artifact = table.ToVisualArtifact();
var svg = table.ToSvg();
var png = table.ToPng();
```

The core renderer produces a static preview by converting the table to a `ChartTable` visual block. Static output is suitable for generated reports, email, documentation, export previews, and visual galleries. It does not implement search boxes, sort headers, filters, keyboard selection, clipboard behavior, or virtual scrolling in ChartForgeX core.

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

Markdown can declare a table artifact through `chartforgex table` or `cfx table` fences:

````markdown
```chartforgex table {#alerts title="Open Alerts"}
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

The markup parser should stay a thin authoring layer. Reusable behavior belongs in `ChartForgeX.VisualArtifacts`; richer interaction belongs in adapter packages or consuming native hosts.

## SequenceArtifact

`SequenceArtifact` is the reusable interaction-diagram model for participants, messages, notes, block spans, and host metadata. It is product-neutral and can be created directly by .NET callers or produced from Mermaid sequence diagrams.

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
