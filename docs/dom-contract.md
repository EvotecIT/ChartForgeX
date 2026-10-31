---
description: "Use supported ChartForgeX DOM roles, accessible mark links and validated host hooks without depending on generated IDs or element order."
---

# DOM contract

Rendered SVG and HTML carry many `data-cfx-*` attributes. Most of them are renderer details and can change between releases. The names on this page are the supported contract for hosts such as HtmlForgeX, CanopyX and report composers. They are published as constants in `ChartForgeX.Rendering.ChartDomRoles`, and `ChartDomContractTests` fails when one of them stops being emitted.

Select these names in host scripts and styles. Don't select other roles, element order or generated ids. Text roles sit on a `<g>` that wraps the `<text>`, so select `[data-cfx-role="schedule-row-label"] text` rather than `text[data-cfx-role=…]`.

## Attributes

| Attribute | Where | Value |
|---|---|---|
| `data-cfx-role` | Any rendered element with a role | One of the roles below, or an unsupported renderer role. |
| `data-cfx-lane-id` | `schedule-row` groups, `state-timeline-segment` and `gantt-lane-item` marks | The lane's series identifier. Pass `id: new ChartSeriesId("dc/DC1")` to `AddStateTimelineLane` or `AddGanttLane` to use your own key, for example a drawer route. |
| `data-cfx-lane-group` | `schedule-row` groups inside a group, and a `<g>` enclosing the group header | The group name passed as `group:`. |
| Caller `data-*` attributes and `class` | Mark, series or row configured with `ChartMarkOptions` | Written verbatim after validation. `data-cfx` and `data-cfx-*` are reserved; values are escaped. |
| `aria-current` | An authored current or selected mark or row | `true`. Current uses a dashed outline; selected uses a solid outline. |
| `data-cfx-current`, `data-cfx-selected` | Authored current/selected mark or row | `true` for each enabled visual state. Navigation remains a link, without selection-widget roles or aria-selected. |
| `href`, `target`, `rel`, `tabindex`, `aria-label` | Linked mark or row | A semantic target becomes `<a role="link" tabindex="0" focusable="true">`. New-context links use `_blank` and `noopener noreferrer`; other authored links use `_self`. |

### Mark identity and readouts

These attributes identify data marks in every family and carry the display text the chart already formatted. Hosts and the enhance runtime read them instead of re-formatting values.

| Attribute | Where | Value |
|---|---|---|
| `data-cfx-series` | Series groups and marks | The series ordinal. |
| `data-cfx-point` | Point-like marks | The mark's ordinal in its series. |
| `data-cfx-series-key` | Series groups, marks and legend entries | The series identifier (`ChartSeriesId`). |
| `data-cfx-series-name` | Series groups | The series display name. |
| `data-cfx-x` | Cartesian and radial marks | The raw category coordinate; an OLE date on time axes. |
| `data-cfx-x-instant` | Time-axis marks | The coordinate as an ISO 8601 instant. |
| `data-cfx-formatted-value` | Marks | The value as the chart formats it, with its unit. |
| `data-cfx-formatted-x` | Marks | The category coordinate as the axis formats it. |
| `data-cfx-display-label` | Marks | What a reader sees for the category: the authored label or the formatted coordinate. |
| `data-cfx-range-label` | Range, schedule and bucket marks | A formatted interval such as `Jan 26 – May 26`. |
| `data-cfx-keys` | Marks with `ChartMarkOptions.Keys` | The record keys the mark represents, as a JSON array of strings (for example grid row ids). |
| `data-cfx-fields` | Marks with `ChartMarkOptions.Fields` (or series fields) | The values that define the mark, as a JSON object of strings, numbers and booleans; dates are ISO 8601 UTC instants. An item's fields extend its series' fields. |

### Enhance hooks

`VisualSvgOptions.WithEnhanceHooks()` adds the hooks the [enhance runtime](enhance.md) reads. Without it the SVG is byte-for-byte what it was. With it the SVG gains attributes and, optionally, one inert `<metadata>` element; it never gains script.

| Attribute | Where | Value |
|---|---|---|
| `data-cfx-enhance` | Root `<svg>` | The hook version, `1`. |
| `data-cfx-view` | Root | The view id used in linked-view payloads: the export's ID namespace. |
| `data-cfx-size` | Root | The logical width and height. Fluid roots have no viewBox, so the runtime maps logical X by the rendered width. |
| `data-cfx-plot` | Root of Cartesian and schedule charts | The logical plot rectangle: x, y, width, height. |
| `data-cfx-x-domain` | Root | The category-axis minimum and maximum. |
| `data-cfx-x-kind` | Root | `linear`, `time`, `log` or `symlog`. |
| `data-cfx-orientation` | Root | `horizontal` when categories run vertically. |
| `data-cfx-crosshair` | Root | `true` for vertical line and area charts: a shared-X crosshair applies. |
| `data-cfx-brush` | Root | `true` when the crosshair axis is linear or time: dragging selects a range. |
| `data-cfx-mark` | Every interaction target in every family | `true`. The innermost series group that identifies one datum: points, bars, slices, cells, tiles, flow and hierarchy nodes and links, schedule items, single-value gauges and rows. Legend entries are never marks. |
| `data-cfx-bounds` | Marks | The logical semantic extent (x, y, width, height), so marks without painted geometry, such as line points without markers, still have an anchor. |
| `data-cfx-legend-for` | Legend entries | The series ordinal the entry controls. Entries that also carry `data-cfx-point` control one slice. |
| `data-cfx-enhance-data` | `<metadata>` element (with `WithEnhanceHooks(includeData: true)`) | `true`; its text is JSON: `{ v, series: [{ i, key, name, color }], points: [{ s, p, x, xi, l, v, k }] }`, points sorted by X. |

## Roles

| Role | Element | Notes |
|---|---|---|
| `schedule-row` | `<g>` or linked `<a>` around one lane's label and summary | Carries `data-cfx-lane-id` and, when grouped, `data-cfx-lane-group`. It identifies the lane; it is not a numeric chart interaction target. `WithRowOptions` adds a link, host hooks or state. |
| `schedule-row-label` | `<g>` wrapping the lane label `<text>` | The complete name is in the chart's semantic regions when the drawn text is shortened. |
| `lane-summary` | `<g>` wrapping the right-hand lane summary `<text>` | Present only when a lane has a summary. |
| `state-timeline-group` | `<g>` wrapping a group header in a state timeline | An enclosing `<g>` carries `data-cfx-lane-group`. |
| `gantt-lane-group` | `<g>` wrapping a group header in Gantt lanes | An enclosing `<g>` carries `data-cfx-lane-group`. |
| `schedule-group` | `<g>` wrapping a group header in a mixed schedule | An enclosing `<g>` carries `data-cfx-lane-group`. |
| `state-timeline-segment` | `<g>` or linked `<a>` segment target | Carries `data-cfx-status`, `data-cfx-start`, `data-cfx-end`, `data-cfx-series`, `data-cfx-point` and `data-cfx-lane-id`. |
| `state-timeline-segment-shape` | Painted shape inside a segment | |
| `gantt-lane-item` | `<g>` or linked `<a>` Gantt-lane item target | Carries the same identity attributes as a segment. |
| `point` | Cartesian data point target | |
| `legend-entry` | `<g>` chart legend entry | Carries `data-cfx-series-key` (the series identifier) and an `aria-label`. |
| `axis-y-label` | Primary value-axis tick label | |
| `topology-node` | Topology node | |
| `topology-edge` | Topology edge | |
| `strip-axis` | Group in an axis-only strip | Uses the same `ChartStripScale` as its plot strips. |
| `strip-axis-line` | Horizontal axis line | The full strip width. |
| `strip-axis-tick` | Tick line | Source positions map to the shared horizontal domain. |
| `strip-axis-label` | Group wrapping tick text | Boundary captions align into the strip. |

Families that render other target roles (for example `heatmap-cell`, `radial-point`, `sankey-node`, `treemap-tile` or `gantt-task`) are not separate contract roles: select `[data-cfx-mark]` under enhance hooks, or `[data-cfx-series][data-cfx-point]` without them.

## Navigation, state and host styling

Use `WithMarkOptions` on points, typed items or series, and `WithRowOptions` on a series for independent row metadata. A non-null item policy replaces the series default. `Tooltip` supplies a plain-text `<title>`; `AccessibleLabel` overrides the generated name. A chart containing links uses a root group role so its links remain available to assistive technology. Each linked mark has one focus target; the renderer does not nest links.

Class tokens use ASCII CSS identifiers separated by single spaces. Complete host `data-*` names start with a lowercase letter after `data-` and then contain lowercase letters, digits, underscores or hyphens. Invalid names throw without sanitization. Attribute values remain text, including quotes and angle brackets.

Fluid-width output can place text and marker shapes inside nested SVG viewports. Continue selecting the contract role and your authored hooks rather than assuming a direct child or a `<g>` element. The outer SVG has `width="100%"`, a fixed logical height and no viewBox; stretched strokes have `vector-effect="non-scaling-stroke"`. Proportional responsive output retains its existing viewBox contract.

Current and selected schedule rows paint a band and left accent behind their label and track, with semibold row text. The band roles are `schedule-current-row-band` and `schedule-selected-row-band`; the accent roles use the corresponding `-accent` suffix instead of `-band`. The `schedule-row` group retains the row's hooks and navigation around its label and summary. Hosts style keyboard focus separately through `svg a:focus-visible`.

See [embedding charts in server-rendered pages](https://chartforgex.com/docs/embedding/) for links, shared-scale strips, page fonts and colours, data tables, CSP and ID scopes.

## Migration

- HtmlForgeX selected `[data-cfx-role="state-lane-label"]` for lane links. ChartForgeX has emitted `schedule-row-label` since the shared schedule layout, so those links never attached. Select `[data-cfx-role="schedule-row"]` and read `data-cfx-lane-id` instead of matching lanes by position.
- Topology legends use their own `legend-item` role, which is not part of this contract yet.
- A contract role is renamed only with a release note and, where practical, one release in which both names are emitted.

## Responsive heatmap layout 3

Rectangular heatmaps use `chartforgex/layout-3`, which adds matrix column count, cell gap and row-gutter information to layout 2. The mounted SVG moves row labels, group captions and legend entries into a `matrix-sticky-labels` group; hosts should continue selecting the documented roles rather than relying on their former parent. The plot scrolls within the responsive host when cells would become narrower than 14 px, while the row gutter and legend stay in view. The link role and accessible-name contract from layout 2 is unchanged. Pie and donut charts use layout 4; remaining chart families use layout 2.

The matrix tuple is `[columnCount, cellGap, gutterStart, rowCount, headerBands]`; each header band is `[sourceTop, height]`, ordered by its position in the native plot. The last two fields extend layout 3 and are optional when reading earlier specifications. Responsive screen layout preserves header heights and cell gaps, chooses a row pitch between the readable floor and 32 px (bounded by 1.6 times cell width), and fits the card to its painted content. Print and script-free fallback retain the native frame.

Explicit layout widths and automatic host measurements use CSS layout pixels, so a scaled container keeps the same gutter and captions through resize and print passes.

The optional `matrixWindow` field retains the caller's `HeatmapColumnWindow` policy (`Latest` by default); `matrixReversed` retains the X-axis direction. A scrolling heatmap initially exposes its latest source column, at the right edge unless the axis is reversed. `Squeeze` opens at the left edge. Resizing keeps readers at the end when they were pinned there, otherwise it preserves the first visible column, including through a temporarily non-scrolling width or a print pass. Programmatic scrolling also updates the pinned-label transform.

Missing positions render as `heatmap-missing-cell` outlines in both static and responsive output. They carry no value, link or `heatmap-cell` identity. A categorical key includes one hatched, outlined “No data” entry when any position is missing. Numeric keys use the same missing-data swatch. Fixed frames retain every row and report `matrix.cell-density` when their cell height is below 12 px; `WithHeatmapNaturalHeight()` remains the opt-in for a taller static frame.

## Responsive layout 2

`HtmlResponsiveChartRenderer` uses the `chartforgex/layout-2` specification (layout 3 for rectangular heatmaps, layout 4 for pie and donut charts) and
one inline base SVG. A linked heatmap cell is an `<a data-cfx-role="heatmap-cell">`
with its native `<title>` as the link name and hover tooltip. It retains row,
column, status and non-default pattern metadata; the former wrapper,
`heatmap-cell-link` role and repeated point, series, value, source and label
attributes are not present on that link. Hosts should select the link by its role
and use its `href` for navigation, instead of assuming a nested anchor or generated
ID. Static SVG and the interactive adapter retain their own existing contracts.

Responsive drawings with links use an SVG group role so that links remain exposed
to assistive technology. Generated definition IDs are implementation details;
the mounted runtime namespaces each chart independently.

## Responsive radial layout 4

Pie and donut charts use `chartforgex/layout-4`. Its `radial` payload adds native frame constraints, text measurements, complete legend captions and numeric columns, and typed slice geometry to layout 2. The browser flows the legend at the current width, recalculates the ring radius and center text, and fits the card to its content. It retains every legend entry, including Other, and grows the screen frame when the legend needs more room. An explicit native legend row budget still applies to static rendering; it does not hide entries in the responsive screen layout. Rendering a fragment does not change that caller option.

Slice elements, point identities and Other contributor indices survive resizing. Hosts should keep selecting documented roles and identities rather than reading transforms or relying on parent groups. A shortened legend caption has its complete name in a native SVG `title`; values and percentages remain complete. The radial initializer is included only in pie and donut fragments. Static SVG and PNG rendering keep their native layout; printing and script-free fallback use the native print frame.

Frame measurements use the resolved native title, subtitle and legend styles, including configured overrides and fitted paint styles. Shared concentric-ring callouts retain their series/slice association and move with their connectors and halos. Overflow totals use complete native scientific bounds; if no complete bound fits the hole, the painted value is omitted while its complete semantic value remains available.

`RenderFragmentWithoutAssets` emits the drawing and inert layout data without any script or style elements. Use it when a host section forbids embedded scripts. Include `HtmlResponsiveChartRenderer.Stylesheet` and `InitializationScript` once outside that section. The initializer supports the ordinary JSON script payload and the assetless hidden-span payload, including compressed data. See `ResponsiveRadialExamples` for composition with the interactive renderer: its native interaction metadata and responsive drawing use the same maximum-width source frame.
