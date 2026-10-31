# DOM contract

Rendered SVG and HTML carry many `data-cfx-*` attributes. Most of them are renderer details and can change between releases. The names on this page are the supported contract for hosts such as HtmlForgeX, CanopyX and report composers. They are published as constants in `ChartForgeX.Rendering.ChartDomRoles`, and `ChartDomContractTests` fails when one of them stops being emitted.

Select these names in host scripts and styles. Don't select other roles, element order or generated ids. Text roles sit on a `<g>` that wraps the `<text>`, so select `[data-cfx-role="schedule-row-label"] text` rather than `text[data-cfx-role=…]`.

## Attributes

| Attribute | Where | Value |
|---|---|---|
| `data-cfx-role` | Any rendered element with a role | One of the roles below, or an unsupported renderer role. |
| `data-cfx-lane-id` | `schedule-row` groups, `state-timeline-segment` and `gantt-lane-item` marks | The lane's series identifier. Pass `id: new ChartSeriesId("dc/DC1")` to `AddStateTimelineLane` or `AddGanttLane` to use your own key, for example a drawer route. |
| `data-cfx-lane-group` | `schedule-row` groups inside a group, and a `<g>` enclosing the group header | The group name passed as `group:`. |

## Roles

| Role | Element | Notes |
|---|---|---|
| `schedule-row` | `<g>` around one lane's label and summary | Carries `data-cfx-lane-id` and, when grouped, `data-cfx-lane-group`. It identifies the lane; it is not a chart interaction target. Wrap or decorate this group to make a lane clickable. |
| `schedule-row-label` | `<g>` wrapping the lane label `<text>` | The complete name is in the chart's semantic regions when the drawn text is shortened. |
| `lane-summary` | `<g>` wrapping the right-hand lane summary `<text>` | Present only when a lane has a summary. |
| `state-timeline-group` | `<g>` wrapping a group header in a state timeline | An enclosing `<g>` carries `data-cfx-lane-group`. |
| `gantt-lane-group` | `<g>` wrapping a group header in Gantt lanes | An enclosing `<g>` carries `data-cfx-lane-group`. |
| `schedule-group` | `<g>` wrapping a group header in a mixed schedule | An enclosing `<g>` carries `data-cfx-lane-group`. |
| `state-timeline-segment` | `<g>` segment target | Carries `data-cfx-status`, `data-cfx-start`, `data-cfx-end`, `data-cfx-series`, `data-cfx-point` and `data-cfx-lane-id`. |
| `state-timeline-segment-shape` | Painted shape inside a segment | |
| `gantt-lane-item` | `<g>` Gantt-lane item target | Carries the same identity attributes as a segment. |
| `point` | Cartesian data point target | |
| `legend-entry` | `<g>` chart legend entry | Carries `data-cfx-series-key` (the series identifier) and `data-cfx-description`. |
| `axis-y-label` | Primary value-axis tick label | |
| `topology-node` | Topology node | |
| `topology-edge` | Topology edge | |

## Accessible SVG semantics

Static charts use `role="img"` with their title and description references. Generic descendant groups carry their complete scene description in `data-cfx-description`, alongside the existing labels, coordinates and identity attributes; they do not carry `aria-label`. Hosts can read these facts for tooltips without treating every mark as an accessible control.

Keyboard-enabled HTML charts and SVG scenes containing focusable links use a `graphics-document` root. Labelled generic groups receive `role="group"` and retain their `aria-label`; links retain native link semantics. Explicit semantic roles remain intact, while `generic`, `none` and `presentation` groups are not named. Decorative SVGs remain hidden from assistive technology.

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
