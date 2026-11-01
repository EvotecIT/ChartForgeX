---
description: "Use the ChartForgeX markup v1 grammar reference for supported fences, attributes, data commands, typed models and static preview contracts."
---

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
| `chartforgex architecture v1` | `Topology` | `TopologyChart` with scenarios | Topology SVG/HTML/PNG, interactive player, text ([reference](architecture-diagrams.md)) |

Supported fences may use backticks or tildes. Native ChartForgeX fences may include optional Markdown-style metadata after the version token, for example `{#service-map title="Service Map" width=1280 height=760}`. Parser payload commands remain the source of truth when both payload and metadata specify the same visual option. Editor tooling can also read the machine-readable schema at `ChartForgeX.Markup/Schemas/chartforgex-markup-v1.schema.json`.

Unsupported `chartforgex` families, missing native schema versions, and unsupported native schema versions produce diagnostics. They are not silently ignored.

## Shared Lexical Rules

- Blank lines are ignored.
- `//` starts a comment. Architecture markup also treats lines starting with `#` as comments.
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
| `series "Name"` | Sets the single series name for default numeric data or typed chart data. |
| `candle <x> <open> <high> <low> <close>` | Adds a typed financial record for `candlestick` or `ohlc`; x is numeric or an ISO date (`yyyy-MM-dd`). |
| `item <id> "Label" [parent=<id>] [value=<number>] [colorValue=<number>]` | Adds a typed treemap hierarchy item. Omitted value remains null for a group. |
| `node <id> "Label"` | Declares a node for `sankey` or `chord`. |
| `flow <id> <source> <target> <value> [colorValue=<number>]` | Adds a directed relationship for `sankey` or `chord`. |
| `annotation <kind> <start> [end] [label] [id=<source-id>] [color=<hex>] [opacity=<number>]` | Adds a line or band annotation. An optional ID uses the native trimmed, case-sensitive identity and must be unique within the chart. |
| `option <name> <value>` | Applies a supported chart render option. |
| `theme <preset-or-mode>` | Selects a preset, or changes only the mode with `light` or `dark`. |
| `themeMode light\|dark` | Sets the mode independently of the preset. |
| `gaugeMinimum <number>` / `gaugeMaximum <number>` | Sets the gauge range; defaults are 0 and 100. Also accepted as options. |
| `end` | Leaves a chart table section and resumes ordinary commands. |

### Numeric Chart Types

| Family | Types |
| --- | --- |
| Bars | `bar` (`column`), `horizontalbar` (`hbar`), `stackedbar`, `stackedhorizontalbar`, `percentstackedbar`, `percentstackedhorizontalbar` |
| Lines and areas | `line`, `smoothline`, `stepline`, `area`, `smootharea`, `steparea`, `stackedarea`, `smoothstackedarea`, `percentstackedarea`, `smoothpercentstackedarea` |
| Point and comparison | `scatter`, `lollipop`, `dumbbell`, `waterfall` |
| Radial and parts | `pie`, `donut`, `polar`, `polararea`, `radar`, `funnel`, `pyramid` |
| Matrix and indicators | `heatmap`, `gauge`, `lineargauge` |

Type names normalize case, hyphens and underscores. For example, `stacked-horizontal-bar` selects `stackedhorizontalbar` and `smoothPercentStackedArea` selects `smoothpercentstackedarea`.

Numeric data tables retain their label-plus-series shape. If a table has a `value` column, it becomes a single-series chart. Otherwise the first label/category/name column becomes the x-axis and each numeric column becomes a named series. Numeric table rows require a value in every series column; blank or omitted cells produce a located diagnostic instead of shifting later observations. Default `values`/`value` declarations cannot be combined with named numeric series. For heatmaps each series is a row; labels supply category positions. Percent-stacked types normalize numeric values within each stack; every member of a stack must use the same units. Heatmap series cannot be mixed with other chart kinds. Gauge types require exactly one series with one value, so a gauge table has one numeric series column and one data row.

`gaugeMinimum` and `gaugeMaximum` default to 0 and 100. Both must be finite and the maximum must exceed the minimum. Use direct commands, `option gaugeMinimum 0` / `option gaugeMaximum 200`, or rows in an `options:` table. These options describe the native gauge range rather than Cartesian axis bounds.

### Typed Chart Data

`candlestick` and `ohlc` accept either numeric x coordinates or ISO dates (`yyyy-MM-dd`), consistently within a chart. Dates select the native UTC time axis. `option heatmapValueName "Coverage %"` sets the heatmap numeric key name without overriding its palette or typography.

`candlestick`, `ohlc`, `treemap`, `sankey`, and `chord` use typed data. Set the chart type and optionally declare one name with `series "Name"`. Numeric `values` or multi-series numeric tables do not substitute for typed facts, and `series "Name" type treemap values ...` is invalid.

| Type | Command data | Table section and columns |
| --- | --- | --- |
| `candlestick`, `ohlc` | `candle X OPEN HIGH LOW CLOSE` | `candles:` — `x`, `open`, `high`, `low`, `close` |
| `treemap` | `item ID "LABEL" [parent=ID] [value=NUMBER] [colorValue=NUMBER]` | `items:` — `id`, `label`, optional `parent`, `value`, `colorValue` |
| `sankey`, `chord` | `node ID "LABEL"`; `flow ID SOURCE TARGET VALUE [colorValue=NUMBER]` | `nodes:` — `id`, `label`; `flows:` — `id`, `source`, `target`, `value`, optional `colorValue` |

Typed tables use a header row and an optional Markdown separator row. A new section starts another table; use `end` before returning to unrelated chart commands. Financial x values and OHLC fields are numeric and map to native `ChartCandlestick` records, including their high/low and finite-number validation.

Treemap items map to native `ChartHierarchyItem` records. An omitted or empty parent means a root. An omitted or empty value remains null for a group; leaf values supply area. Empty optional color values are allowed. IDs identify items, and a parent must reference a declared item. For example:

````markdown
```chartforgex chart v1
type treemap
series "Checks"
items:
| id | label | parent | value | colorValue |
| --- | --- | --- | --- | --- |
| runtime | Runtime | | | |
| windows | Windows | runtime | 42 | 96 |
| linux | Linux | runtime | 34 | 92 |
end
```
````

Sankey and Chord share native `ChartNode` and `ChartFlowLink` facts. Nodes and flows each need unique IDs; flow endpoints reference declared nodes. Values and optional color values undergo native validation. A flow's ID is separate from its source and target, preserving individual relationships. Chord supports parallel, reciprocal, self and zero flows and empty-chart no-data behavior. `chordNodeOrder`, `chordOutgoingFlowOrder`, `chordIncomingFlowOrder` and `chordRibbonTension` remain Chord-specific; see [typed Chord markup](https://github.com/EvotecIT/ChartForgeX/blob/main/docs/chord.md#typed-chart-markup).

### Chart Themes

`theme` and `themeMode` are supported as direct commands, `option` lines, `options:` table rows, and opening-fence attributes. Payload commands take precedence over fence attributes. `theme PRESET` selects the preset; `theme light` or `theme dark` changes only the mode. `themeMode light|dark` selects the mode independently:

````markdown
```chartforgex chart v1 {theme=Report themeMode=dark}
type gauge
series "Ready" values 78
```
````

The supported presets are `Graphite`, `Report`, `Executive`, `Product`, `Accessible`, `Colorblind`, `Aurora`, `Editorial`, `Candy`, `PeopleInfographic`, `Terminal`, `TransparentOverlay`, `Minimal`, `Dashboard`, `SaasDashboard`, and `RestaurantDashboard`. Names normalize case, hyphens and underscores; `saas-dashboard` selects `SaasDashboard`.

### Chart Validation And Diagnostics

The parser constructs native ChartForgeX series and typed records, preserving the native financial, hierarchy, relationship and gauge validation contracts. Invalid numeric values, unsupported types or themes, malformed commands, incompatible data declarations, duplicate IDs, unresolved references and invalid native models produce `MarkupDiagnostic` entries. Parsing errors identify their source command or table row; whole-chart validation can use the opening fence line. Hosts must inspect `HasErrors` and `Diagnostics` before exporting artifacts. The schema mirrors and TextMate grammar support authoring; they do not validate native data or replace runtime diagnostics.

Numeric tables with a `value` column use the default single-value form. Only `label`, `category`, or `name` columns may accompany it; extra columns produce a located diagnostic. Use named columns such as `Actual` and `Budget`, without `value`, for multiple series. Typed and options tables reject unknown headers so misspelled fields cannot discard authored facts. Tables also reject duplicate fields, competing label/key columns and surplus row cells. Empty optional typed cells remain supported.

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

Use `VisualMarkupParser.Parse(markdown)` when ChartForgeX should scan Markdown itself. When another Markdown host owns fence discovery, use `VisualMarkupScanner.ParseFenceBlock(...)` to validate and project its fence metadata, then pass the complete scan result to `VisualMarkupParser.Parse(scan)` so validation diagnostics cannot be discarded. `ParseBlocks(blocks)` is available for hosts that already own and have handled scanner diagnostics.

`VisualArtifact` exposes `ToSvg()`, `ToHtmlPage()`, `ToPng()`, `SaveSvg()`, `SaveHtml()`, and `SavePng()` for supported static models. The artifact envelope preserves the artifact kind, source language, title, subtitle, natural size, export formats, regions, legend entries, and metadata.
