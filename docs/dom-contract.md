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
| `legend-entry` | `<g>` chart legend entry | Carries `data-cfx-series-key` (the series identifier) and an `aria-label`. |
| `axis-y-label` | Primary value-axis tick label | |
| `topology-node` | Topology node | |
| `topology-edge` | Topology edge | |

## Migration

- HtmlForgeX selected `[data-cfx-role="state-lane-label"]` for lane links. ChartForgeX has emitted `schedule-row-label` since the shared schedule layout, so those links never attached. Select `[data-cfx-role="schedule-row"]` and read `data-cfx-lane-id` instead of matching lanes by position.
- Topology legends use their own `legend-item` role, which is not part of this contract yet.
- A contract role is renamed only with a release note and, where practical, one release in which both names are emitted.

## Responsive heatmap layout 3

Rectangular heatmaps use `chartforgex/layout-3`, which adds matrix column count, cell gap and row-gutter information to layout 2. The mounted SVG moves row labels, group captions and legend entries into a `matrix-sticky-labels` group; hosts should continue selecting the documented roles rather than relying on their former parent. The plot scrolls within the responsive host when cells would become narrower than 14 px, while the row gutter and legend stay in view. The link role and accessible-name contract from layout 2 is unchanged. Other chart families continue to use layout 2.

## Responsive layout 2

`HtmlResponsiveChartRenderer` uses the `chartforgex/layout-2` specification (or layout 3 for rectangular heatmaps) and
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
