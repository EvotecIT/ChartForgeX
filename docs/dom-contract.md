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
