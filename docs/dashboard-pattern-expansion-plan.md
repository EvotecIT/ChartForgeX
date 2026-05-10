# Dashboard Pattern Expansion Plan

This plan is based on the `chart-visual-polish` worktree state copied into the isolated branch `codex/chart-gallery-expansion-plan`. The goal is to recreate the supplied dashboard screenshots as first-class ChartForgeX examples, then promote repeated patterns into reusable chart and visual-block capabilities.

## Source Design Inventory

The screenshots contain these reusable chart and dashboard patterns:

| Pattern | Seen in | Current coverage | Gap |
| --- | --- | --- | --- |
| Day/hour appointment heatmap | appointment volume dashboard | `Heatmap`, `HeatmapInsightCard`, `WithHeatmapCellGap`, `WithHeatmapCellRadius`, `WithHeatmapValueTextMode` | Added bounded heatmap cell geometry plus a reusable matrix card with controls, right-side busy-time insight rail, and color key. |
| Peak-hour mini histogram | review peak hour panel | `Bar`, `WithDashboardBarStyle`, `ChartSeries.WithPointColorRange`, `WithHighlightedXAxisRange` | Added compact point-color range helper and selected-window band metadata for muted-vs-highlight bar windows. |
| Staff workload list with progress bars | staff workload panel | `WorkloadListBlock` | Added reusable visual-block list rows with avatar/initial slot, per-row progress rail, overload status, optional selection control, and right-aligned values. |
| Segmented tick progress | project progress card | `SegmentedProgressCard` | Added reusable fixed-count segmented progress rows with dimensional filled/empty ticks, delta pills, header symbol/menu chrome, and footer actions. |
| Stacked task distribution card | overall tasks card | `CompositionStatusCard` | Added composition-card visual block with large totals, striped segments, legend rows, values, units, and footer actions. |
| Grouped capsule bars | project track card, HR dashboard | grouped `Bar`, dashboard bar styling | Need rounded capsule grouped-bar preset, group separators, and card metric header/action footer. |
| Dense schedule swimlane | project timeline screenshot | `ScheduleTimelineBlock` | Added schedule/swimlane visual block with header action chips, time-of-day axis, lanes, current-time marker, rounded event pills, badges, avatar stacks, and clipped-event metadata. |
| Vertical activity timeline | shipment side panel | `ActivityTimelineBlock` | Keep only the chart-like event spine as a static SVG/PNG overlay: sections, status nodes, connector spine, nested checklist rows, hidden summaries, timestamps, badges, compact event rows, and node symbols. Move tabs, notes, and action buttons to semantic HTML/interactivity. |
| HR metric grid | HR dashboard | `MetricCard`, `VisualGrid` | Need richer dashboard-card presets, icon badges, delta pills, and gradient/stat panel example. |
| Department stacked bars | HR dashboard | `WithDashboardStackedRowStyle` | Added compact stacked-row preset with dashboard chrome, optional inline legend, segmented horizontal rows, and optional trailing totals. |
| Attendance multi-line trend | HR dashboard | `WithDashboardTrendPanelStyle`, `WithDashboardTrendFocus` | Added small-dashboard line preset with premium strokes, compact axes, inline legend support, vertical focus marker, highlighted x label, and point callout. |
| Table rows with badges and sparklines | HR/analytics tables | `ChartTableCell.WithBadge`, `ChartTableCell.WithSparkline`, `ChartTableCell.WithMiniBars` | Added bounded table-cell badges/chips and microvisuals so rows can host compact labels, sparklines, and mini bars without embedding full charts. |
| Segmented currency distribution | analytics dashboard | `DistributionStripCard` | Added payment-style distribution card with stacked share strip, legend chips, row symbols, percentage rings, and trailing detail values. |
| World/country transaction map | analytics dashboard | `DottedMap`, `RegionMap`, `TileMap` | Existing coverage is good; add analytics-dashboard example using current map APIs. |
| Dashboard shell and cards | all screenshots | `VisualGrid`, HTML gallery | Need page/shell example for sidebar, toolbar, card frame rhythm, and multi-panel static dashboard composition. |

## Delivery Strategy

Treat this as two parallel tracks:

1. Recreate screenshot-inspired examples with current APIs first. This exposes the real friction without blocking on perfect abstractions.
2. Promote repeated solutions into reusable primitives only after at least two examples need the same behavior.

This avoids turning one-off UI screenshots into a large chart-type explosion while still expanding the library where the screenshots reveal durable patterns.

## Phase 1: Example Recreation With Existing Surface

Create `ChartForgeX.Examples/DashboardPatternExamples.cs` and add a new catalog group named `Dashboard Inspiration`.

Target examples:

- `dashboard-appointment-operations-grid`
  - Use heatmap for appointment volume.
  - Use compact bar chart for peak hours with muted gray bars and highlighted red range.
  - Use `VisualGrid` to compose heatmap, peak-hour chart, workload rows, KPI summaries, and availability list.

- `dashboard-project-progress-card`
  - Use current `MetricCard.WithMiniBars` as the first approximation for segmented progress.
  - Include two rows, delta pills as text/status, and footer action.

- `dashboard-project-task-composition-card`
  - Use stacked horizontal bars with diagonal fill patterns.
  - Add legend rows through `ChartList` or `MetricCard` details.

- `dashboard-project-track-card`
  - Use grouped bars with dashboard panel style, hidden axes where appropriate, and a large metric subtitle.

- `dashboard-hr-operations-grid`
  - Compose metric cards, department stacked bars, candidate-status strip, attendance multi-line chart, vacancy table, schedule list, and news list.

- `dashboard-payment-analytics-grid`
  - Compose overview metric cards with sparklines, segmented currency strip, world region/dotted map, and merchant table.

Acceptance:

- Every example exports SVG, HTML, and PNG.
- Examples appear in the catalog and quality dashboard.
- Each example has stable IDs and names suitable for future baseline testing.
- No new public API unless an existing API cannot express the pattern at all.

## Phase 2: Visual-Block Primitives

Add reusable visual blocks for patterns that are not really chart series:

### `SegmentedProgressCard`

Purpose: project progress, completion scorecards, quota rows.

Proposed model:

- `SegmentedProgressCard.Create()`
- `AddRow(label, value, maximum, segments = 40, color = null, delta = null)`
- `WithHeaderSymbol(symbol)` and `WithMenu(...)`
- `WithAction(label, symbol = ">")`
- `WithEmptySegmentOpacity(...)`

Renderer work:

- SVG/PNG/HTML row layout.
- Shared segmented-strip geometry helper.
- Fixed segment count with stable gap/radius math.
- Renderer-owned segment shadow/highlight chrome for filled and empty ticks.
- Accessibility metadata in SVG/HTML.

### `CompositionStatusCard`

Purpose: task status, candidate status, order status, compact status distribution summary.

Proposed model:

- `WithMetric(label, value)`
- `AddSegment(label, value, color, status = VisualStatus.None, pattern = ChartFillPattern.None)`
- `WithLegendValues(bool enabled = true)`
- `WithFooterAction(...)`

Renderer work:

- One stacked strip with optional hatching.
- Legend rows with color swatches, labels, counts, and units.
- Optional percentage/value modes.

### `DistributionStripCard`

Purpose: payment analytics, currency share, portfolio allocation, channel split cards.

Implemented model:

- `WithMetric(label, value, caption = null)`
- `AddSegment(label, value, color, symbol = null, detail = null, status = VisualStatus.None)`
- `WithAction(...)`

Renderer work:

- One striped stacked strip for proportional distribution.
- Wrapped legend chips with segment shares.
- Per-segment rows with compact symbol badges, percentage rings, and trailing value text.
- SVG metadata for card, strip segments, legend chips, rows, rings, totals, and shares.

### `WorkloadListBlock`

Purpose: staff workload rows, available staff, ranked merchants, recent applications.

Proposed model:

- `AddPerson(name, subtitle, value, maximum, status, avatarText = null, icon = null)`
- `AddRow(label, subtitle, value, maximum, status)`
- `WithProgressRails(...)`
- `WithSelectionControls(...)` for checkbox-like rows.

Renderer work:

- Avatar/initials slot.
- Progress rail with overload status.
- Right-aligned value/status text.
- Optional row divider and selected state.

### `ActivityTimelineBlock`

Purpose: shipment/order timeline and status feeds.

Proposed model:

- `AddSection(label)`
- `AddEvent(title, timestamp = null, status = VisualStatus.Neutral, badge = null, detail = null, symbol = null)`
- `AddChecklistItem(text, completed, muted = false)`
- `AddHiddenSummary(count, label)`
- `WithEventSurfaces(enabled = true)` for switching between card-like event containers and compact inline event rows.

Renderer work:

- Vertical connector spine.
- Section labels.
- Status nodes: active, completed, pending, warning.
- Compact node symbols for shipment/order markers.
- Nested checklist indentation and muted/struck text.
- No app-panel chrome in SVG/PNG; tabs, notes, and action buttons belong in the HTML/interactivity layer.

### `ScheduleTimelineBlock`

Purpose: project timeline/scheduler cards and right-column schedules.

This can start as a visual block rather than replacing `Gantt`.

Proposed model:

- `WithTimeRange(start, end, interval)`
- `AddLane(label = null)`
- `AddEvent(title, start, end, lane, color, icon = null)`
- `WithAvatarStack(...)`
- `WithCurrentTime(...)`
- `WithHeaderActions(...)`

Renderer work:

- Time-of-day axis.
- Dashed vertical grid.
- Overlap-safe lane rows.
- Rounded event pills with left status stripe.
- Event clipping and tooltip metadata.
- Optional date/filter/add-schedule header action chips.

## Phase 3: Chart Capability Enhancements

These should land after Phase 1 proves they are necessary:

- Heatmap polish options:
  - `WithHeatmapCellRadius(...)` and `WithHeatmapCellGap(...)` for dashboard-style rounded matrices.
  - `WithHeatmapValueTextMode(...)` for explicit cell value text.
  - `HeatmapInsightCard` covers the appointment-volume side rail/color-key pattern for dashboards where the heatmap needs contextual summary text.

- Bar polish options:
  - rounded grouped/capsule bar style usable outside full dashboard panels.
  - group separators.
  - selected/highlighted x-range metadata via `WithHighlightedXAxisRange(...)`.

- Table microvisual cells:
  - sparkline and mini-bar cells for `ChartTable`.
  - keep data model bounded; do not allow arbitrary nested charts per cell.

- Shared badges and chips:
  - compact table-cell status badge/chip geometry with soft, solid, and outline styles.
  - delta pill with up/down/neutral semantics.
  - reusable icon/initials/avatar stack primitives beyond the current schedule/workload blocks.

- Dashboard shell export:
  - optional static HTML dashboard page helper with sidebar/topbar/card layout.
  - keep this in examples or `ChartForgeX.Interactivity.Html` unless it becomes a stable renderer concern.

## Phase 4: Documentation And Gallery

Update:

- `README.md` dashboard patterns section.
- `docs/visual-blocks.md` with the new visual-block contracts.
- Example catalog group descriptions.
- Code sample extraction for the new dashboard examples.

Add visual-baseline entries once the layouts stabilize.

## Verification Plan

Minimum checks per phase:

- `dotnet test ChartForgeX.Tests/ChartForgeX.Tests.csproj`
- `.\Build.ps1 -UpdateVisualBaseline` when examples are added or regenerated.
- `dotnet run --project ChartForgeX.Examples -- --quality-dashboard`
- SVG and PNG parity tests for each new block.
- HTML smoke check for catalog links and generated code samples.

Focused tests to add:

- segmented progress geometry count, empty segment opacity, and SVG/PNG parity.
- composition strip segment totals, zero-value behavior, and pattern rendering.
- activity timeline ordering, section grouping, and hidden summary output.
- schedule timeline axis bounds, event clipping, overlap lanes, and tooltip metadata.
- table microvisual bounds and text collision guards.

## Suggested Implementation Order

1. Add Phase 1 examples with current APIs and catalog integration.
2. Extract a shared dashboard inspiration theme from duplicated examples.
3. Implement `SegmentedProgressCard` and `CompositionStatusCard`.
4. Implement `WorkloadListBlock` and `ActivityTimelineBlock`.
5. Implement `ScheduleTimelineBlock`.
6. Add heatmap/bar/table microvisual enhancements only where examples still require awkward code.
7. Regenerate baselines and update docs.

## Current Progress

Completed in `codex/chart-gallery-expansion-plan`:

- Added `ChartForgeX.Examples/DashboardPatternExamples.cs` with 8 screenshot-inspired examples:
  - `dashboard-appointment-operations-grid`
  - `dashboard-project-progress-card`
  - `dashboard-project-task-composition-card`
  - `dashboard-project-track-card`
  - `dashboard-project-schedule-timeline`
  - `dashboard-shipment-activity-panel`
  - `dashboard-hr-operations-grid`
  - `dashboard-payment-analytics-grid`
- Added the `Dashboard Inspiration` catalog group.
- Added `--dashboard-patterns-only` to generate just the new examples plus gallery/quality pages.
- Verified focused generation reports 8 chart pairs, 8 healthy SVGs, 8 healthy PNGs, 8 healthy HTML pages, and 0 warnings.
- Verified `dotnet test ChartForgeX.Tests\ChartForgeX.Tests.csproj -m:1 -nr:false /p:UseSharedCompilation=false` passes 336/336 tests.
- Promoted two Phase 2 primitives into public visual blocks:
  - `SegmentedProgressCard` for fixed-count dashboard progress rows with delta pills and footer actions.
  - `CompositionStatusCard` for stacked part-to-whole cards with legend rows, values, units, pattern hints, and footer actions.
- Promoted two more Phase 2 primitives into public visual blocks:
  - `WorkloadListBlock` for staff, people, or ranked rows with avatar slots, progress rails, overload/status notes, optional checkbox controls, and right-aligned values.
  - `ActivityTimelineBlock` for static shipment/order event spines with section labels, status nodes, connector spines, nested checklist rows, hidden summaries, timestamps, badges, compact event rows, and node symbols.
- Promoted `ScheduleTimelineBlock` for dense planner-style time-of-day swimlanes with header actions, lanes, rounded event pills, status stripes, badges, avatar stacks, current-time markers, and clipped-event metadata.
- Promoted `HeatmapInsightCard` for appointment-style matrix cards with segment controls, side insight rail, and renderer-owned color key.
- Added `ChartTableCell` microvisuals for mini bars and sparklines with renderer-owned SVG/PNG output and validation.
- Updated the appointment heatmap, project progress, overall task, staff workload, shipment activity, project schedule, and recent vacancies examples to use reusable blocks/cells instead of ad hoc `MetricCard`/`VisualGrid`/`ChartList`/progress-bar/Gantt approximations.
- Promoted `DistributionStripCard` for payment-style allocation widgets with stacked share strips, wrapped legend chips, row badges, percentage rings, and detail values.

The examples prove that current APIs can approximate the supplied designs, and most repeated dashboard patterns are now reusable. Remaining Phase 2 gap: grouped capsule bars and shared chips/badges should become reusable primitives rather than ad hoc renderer fragments.

## Risks And Boundaries

- Do not force UI lists, activity feeds, or dashboards into `ChartSeriesKind`; they belong in `VisualBlocks`.
- Keep SVG/PNG/HTML parity as a release requirement for every new public block.
- Avoid arbitrary HTML injection in visual blocks. Use bounded models and renderer-owned layout.
- Keep dashboard shell helpers optional so report/image consumers are not coupled to a web-app layout model.
- Preserve the existing dependency-free rendering goal.
