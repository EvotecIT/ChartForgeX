# DOM contract

Rendered SVG and HTML carry many `data-cfx-*` attributes. Most of them are renderer details and can change between releases. The names on this page are the supported contract for hosts such as HtmlForgeX, CanopyX and report composers. They are published as constants in `ChartForgeX.Rendering.ChartDomRoles`, and `ChartDomContractTests` fails when one of them stops being emitted.

Select these names in host scripts and styles. Don't select other roles, element order or generated ids.

## Attributes

| Attribute | Where | Value |
|---|---|---|
| `data-cfx-role` | Any rendered element with a role | One of the roles below, or an unsupported renderer role. |
| `data-cfx-lane-id` | `schedule-row` groups, `state-timeline-segment` and `gantt-lane-item` marks | The lane's series identifier. Pass `id: new ChartSeriesId("dc/DC1")` to `AddStateTimelineLane` or `AddGanttLane` to use your own key, for example a drawer route. |
| `data-cfx-lane-group` | `schedule-row` groups inside a group, and a `<g>` enclosing the group header | The group name passed as `group:`. |

## Roles

| Role | Element | Notes |
|---|---|---|
| `schedule-row` | `<g>` around one lane's label and summary | Carries `data-cfx-lane-id`, `data-cfx-series` and, when grouped, `data-cfx-lane-group`. Wrap or decorate this group to make a lane clickable. |
| `schedule-row-label` | `<text>` lane label | The complete name is in the chart's semantic regions when the drawn text is shortened. |
| `lane-summary` | `<text>` right-hand lane summary | Present only when a lane has a summary. |
| `state-timeline-group` | `<text>` group header in a state timeline | An enclosing `<g>` carries `data-cfx-lane-group`. |
| `gantt-lane-group` | `<text>` group header in Gantt lanes | An enclosing `<g>` carries `data-cfx-lane-group`. |
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
