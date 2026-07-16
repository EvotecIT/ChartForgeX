# ChartForgeX Markup v1 Reference

ChartForgeX native markup fences use the canonical form `chartforgex <kind> v1`. The `v1` token is required for native ChartForgeX fences and is the compatibility boundary for future grammar changes.

Mermaid fences intentionally keep Mermaid's own fence names and syntax. Use `ChartForgeX.Markup.Mermaid` when a host wants Mermaid parsing through the same visual artifact pipeline.

## Fence Contract

| Fence | Artifact kind | Model | Static preview |
| --- | --- | --- | --- |
| `chartforgex chart v1` | `Chart` | `Chart` | Chart SVG/HTML/PNG |
| `chartforgex timeline v1` | `Timeline` | `Chart` | Chart SVG/HTML/PNG |
| `chartforgex topology v1` | `Topology` | `TopologyChart` | Topology SVG/HTML/PNG |
| `chartforgex flow v1` | `Flow` | `FlowArtifact` | Topology-projected SVG/HTML/PNG |
| `chartforgex sequence v1` | `Sequence` | `SequenceArtifact` | Sequence SVG/HTML/PNG |
| `chartforgex table v1` | `Table` | `TableArtifact` | Visual-block SVG/HTML/PNG |

Supported fences may use backticks or tildes. Native ChartForgeX fences may include optional Markdown-style metadata after the version token, for example `{#service-map title="Service Map" width=1280 height=760}`. Parser payload commands remain the source of truth when both payload and metadata specify the same visual option. Editor tooling can also read the machine-readable schema at `ChartForgeX.Markup/Schemas/chartforgex-markup-v1.schema.json`.

Unsupported `chartforgex` families, missing native schema versions, and unsupported native schema versions produce diagnostics. They are not silently ignored.

## Shared Lexical Rules

- Blank lines are ignored.
- Lines starting with `#` are comments.
- Quoted values may contain spaces: `"Release gate"`.
- Attribute tokens use `key:value` or `key=value`.
- Pipe tables use a header row plus optional separator row.
- Command names are case-insensitive.
- Enum-like values are case-insensitive and generally accept punctuation-insensitive forms such as `smoothLine`, `smooth-line`, and `smooth_line`.

## Chart

`chartforgex chart v1` produces a `Chart` wrapped as `VisualArtifactKind.Chart`.

Core commands:

| Command | Shape |
| --- | --- |
| `id <id>` | Sets artifact id. |
| `title <text>` | Sets chart title. |
| `subtitle <text>` | Sets chart subtitle. |
| `type <kind>` | Sets default chart series kind. |
| `size <width>x<height>` | Sets chart size. |
| `labels <label...>` | Sets x-axis/category labels. |
| `values <number...>` | Adds one unnamed/default series. |
| `value <label> <number>` | Adds one labeled value to the default series. |
| `series <name> type <kind> values <number...> [color <hex>]` | Adds or extends a named series. |
| `annotation <kind> <start> [end] [label] [color=<hex>] [opacity=<number>]` | Adds a line or band annotation. |
| `option <name> <value>` | Applies a supported chart render option. |

Pipe-table data is supported. If a table has a `value` column, it becomes a single-series chart. Otherwise the first label/category/name column becomes the x-axis and numeric columns become named series.

## Timeline

`chartforgex timeline v1` produces a `Chart` wrapped as `VisualArtifactKind.Timeline`.

Core commands:

| Command | Shape |
| --- | --- |
| `id <id>` | Sets artifact id. |
| `title <text>` | Sets chart title. |
| `subtitle <text>` | Sets chart subtitle. |
| `type timeline|gantt` | Selects timeline or Gantt output. |
| `size <width>x<height>` | Sets chart size. |
| `today <date-or-number>` | Sets the Gantt today marker. |
| `item <label> <start> <end> [color=<hex>]` | Adds a timeline range. |
| `task <label> <start> <end> [progress=<number>] [dependsOn=<index>] [color=<hex>]` | Adds a Gantt task. |
| `milestone <label> <date> [dependsOn=<index>] [color=<hex>]` | Adds a milestone. |

Dates are parsed as invariant dates when possible; numeric axis values are also accepted.

## Topology

`chartforgex topology v1` produces a `TopologyChart` wrapped as `VisualArtifactKind.Topology`.

Core commands:

| Command | Shape |
| --- | --- |
| `id <id>` | Sets artifact id. |
| `title <text>` | Sets topology title. |
| `subtitle <text>` | Sets topology subtitle. |
| `viewport <width>x<height> [padding]` | Sets viewport. |
| `layout <mode> [direction]` | Sets topology layout. |
| `group <id> <label> [status:<status>] [color:<hex>] [icon:<id>]` | Adds a group. |
| `node <id> <label> [kind:<kind>] [status:<status>] [group:<id>] [subtitle:<text>] [icon:<id>] [symbol:<text>] [badge:<text>]` | Adds a node. |
| `edge <source> -> <target> [label] [kind:<kind>] [status:<status>] [direction:<direction>]` | Adds an edge. |

Large topology diagrams may use `groups:`, `nodes:`, and `edges:` sections with pipe tables.

## Flow

`chartforgex flow v1` produces a `FlowArtifact` wrapped as `VisualArtifactKind.Flow`.

Core commands:

| Command | Shape |
| --- | --- |
| `id <id>` | Sets artifact id. |
| `title <text>` | Sets flow title. |
| `subtitle <text>` | Sets flow subtitle. |
| `size <width>x<height>` | Sets flow preview size. |
| `layout layered|dense|force` | Sets preview layout mode. |
| `direction left-to-right|top-to-bottom|right-to-left|bottom-to-top` | Sets flow direction. |
| `lane <id> <label> [status:<status>] [color:<hex>]` | Adds a lane. |
| `step <id> <label> [kind:<kind>] [lane:<id>] [status:<status>]` | Adds a generic step. |
| `<kind> <id> <label> [lane:<id>] [status:<status>]` | Adds a typed step. |
| `connect <source> -> <target> [label] [kind:<kind>] [direction:<direction>] [status:<status>]` | Adds a connector. |

Supported step kinds are `process`, `decision`, `start`, `end`, `input`, `output`, `data`, `external`, `document`, `manual`, `delay`, and `event`.

Supported connector kinds are `flow`, `dependency`, `data`, `rejection`, `retry`, `error`, and `async`.

Large flows may use `lanes:`, `steps:`, and `connectors:` sections with pipe tables.

## Sequence

`chartforgex sequence v1` produces a `SequenceArtifact` wrapped as `VisualArtifactKind.Sequence`.

Core commands:

| Command | Shape |
| --- | --- |
| `id <id>` | Sets artifact id. |
| `title <text>` | Sets sequence title. |
| `subtitle <text>` | Sets sequence subtitle. |
| `size <width>x<height>` | Sets preview size. |
| `viewport <width>x<height> [padding]` | Sets preview size and optional padding. |
| `participant <id> [label] [kind:<kind>]` | Adds a participant. |
| `<kind> <id> [label]` | Adds a typed participant. |
| `message <from> -> <to> [text] [style:solid|dashed] [activate:true|false] [deactivate:true|false]` | Adds a message. |
| `note leftOf|rightOf|over <participant-list> <text> [step:<index>]` | Adds a note. |
| `block <kind> <text> <start-index> <end-index>` | Adds a sequence block span. |

Supported participant kinds are `participant`, `actor`, `boundary`, `control`, `entity`, `database`, `collections`, and `queue`.

Supported block kinds are `loop`, `alt`, `opt`, `par`, `critical`, `rect`, and `break`.

Large sequence diagrams may use `participants:`, `messages:`, `notes:`, and `blocks:` sections with pipe tables.

## Table

`chartforgex table v1` produces a `TableArtifact` wrapped as `VisualArtifactKind.Table`.

Core commands:

| Command | Shape |
| --- | --- |
| `id <id>` | Sets artifact id. |
| `title <text>` | Sets table title. |
| `subtitle <text>` | Sets table subtitle. |
| `capabilities <capability...>` | Declares host capabilities. |
| `totalRows <number>` | Declares total row count for large/remote data sets. |

Use a `columns:` table to declare typed columns. Supported column fields include `id`, `label`, `type`, `alignment`, `width`, `searchable`, `sortable`, `filterable`, `copyable`, and `exportable`.

Use a `rows:` table to provide static preview rows. Column ids or labels map row values to declared columns.

Supported table capabilities are `search`, `sort`, `filter`, `singleSelection`, `multiSelection`, `cellSelection`, `copy`, `export`, and `virtualization`.

These capabilities describe what a production host or adapter may expose. They do not make `ChartForgeX` core a data grid, and they do not put demos inside library packages. Keep wiring examples in `ChartForgeX.Examples` or docs.

## API Surface

Use `VisualMarkupParser.Parse(markdown)` when ChartForgeX should scan Markdown itself. When another Markdown host owns fence discovery, use `VisualMarkupScanner.ParseFenceBlock(...)` to validate and project its fence metadata, then pass the returned blocks to `VisualMarkupParser.ParseBlocks(blocks)`.

`VisualArtifact` exposes `ToSvg()`, `ToHtmlPage()`, `ToPng()`, `SaveSvg()`, `SaveHtml()`, and `SavePng()` for supported static models. The artifact envelope preserves the artifact kind, source language, title, subtitle, natural size, export formats, regions, legend entries, and metadata.
