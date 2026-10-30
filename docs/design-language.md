# ChartForgeX design language

One look and one way of behaving for every chart we ship: ChangeIntel's pages, HtmlForgeX reports, CanopyX grids' companions, TestimoX dashboards, OfficeIMO documents, ImagePlayground images. A reader who learns one ChartForgeX chart can read them all. This is the acceptance bar for chart work: a chart that breaks a rule here isn't done.

Sources: a critique of 41 rendered examples across ~30 families (light and dark), an audit of `ChartForgeX.Interactivity.Html`, a survey of 13 products using ChartForgeX and ~30 drawing charts another way, and CanopyX's linked-views contract. Palette checks are computed, not eyeballed.

## 1. Tokens: one system, the `--hfx-*` roles

ChartForgeX reads the shared design roles CanopyX and HtmlForgeX already use, with its own values only as fallbacks. A chart dropped onto a CanopyX or HtmlForgeX page matches it with no configuration; a host that defines nothing still gets the defaults.

| Role | Variable | Light | Dark |
|---|---|---|---|
| Card surface (recessed plot: `--hfx-color-surface-card-alt`) | `--hfx-color-surface-card` | `#ffffff` | `#16181c` |
| Gridline, axis | `--hfx-color-surface-line` | `#e2e4e7` | `#272a30` |
| Text, primary | `--hfx-color-text-primary` | host | host |
| Text, secondary (ticks, legends) | `--hfx-color-text-secondary` | host | host |
| Accent, focus | `--hfx-color-accent-base`; focus uses the host's `--hfx-color-focus-ring` | host | host |
| Font | `--hfx-font-sans` | inherit | inherit |
| Series 1–6 | `--hfx-color-series-1..6` | see below | see below |
| Sequential ramp | `--hfx-ramp-sequential-1..5` | light → dark | its own dark steps |
| Diverging | `--hfx-ramp-diverging-*` | red ↔ gray ↔ blue | same roles |
| Status | `--hfx-color-severity-*`, `--hfx-color-outcome-*`, `--hfx-color-state-*` (fill and ink), and `--hfx-color-status-positive/warning/negative/disabled` | reserved | reserved |

**Categorical order (validated).** Today series 1 and 6 are both blue, and in dark mode they are indistinguishable (normal-vision ΔE 3.5, a hard fail). The new order keeps the first three slots, replaces the second blue with clay, and is the only arrangement tested that passes every check in both modes, including the wrap from the last slot back to the first (donuts):

| Slot | Light | Dark |
|---|---|---|
| 1 blue | `#2a78d6` | `#3987e5` |
| 2 teal | `#0f9f8c` | `#179c89` |
| 3 purple | `#7b61e8` | `#8a74ee` |
| 4 clay (new) | `#a0522d` | `#c06a42` |
| 5 magenta (was 4) | `#c24fa8` | `#c9579f` |
| 6 green (was 5) | `#5b7f00` | `#6f9120` |
| Other | `--hfx-color-text-secondary` at 45% over the plot surface | same |

Validated with the dataviz palette validator: lightness band, chroma floor, adjacent CVD separation (worst ΔE 10.6 light / 12.6 dark), normal-vision floor (≥15.9), contrast ≥3:1 on both surfaces. A 7th series is never generated: it folds into "Other" (always gray, always last) or the chart becomes small multiples. Pie and donut fold automatically (at most `MaximumPieSlices`, 6 by default, the last being Other). Other families do not regroup data: when more series or slices take automatic colours than the palette has slots, preparation reports an informational `palette.reused` diagnostic, and the host groups the rest into Other, colours them explicitly, or switches to small multiples.

In code: `VisualSvgOptions.WithSharedDesignRoles(theme, mode)` (or `SvgColorVariables.SharedRoles(theme, mode)` with `Chart.WithSvgColorVariables`) writes these roles with the theme's values as fallbacks and `var(--hfx-font-sans, inherit)` for text. CanopyX also defines the shorter aliases `--hfx-color-text` and `--hfx-color-accent`; ChartForgeX writes the canonical names both hosts define. Series paints keep their slot, so two slots with the same colour still name their own `--hfx-color-series-n`. [Shared design roles](embedding.md#shared-design-roles) has an example.

Rules: colour follows the entity, never its rank (a filter never repaints survivors); status colours appear only on status-encoded data and always with a label or icon (no amber totals, no red bullet values); text never wears a series colour.

## 2. Visual rules (library-wide)

Ranked by impact from the critique.

1. **Round axis ranges.** The value axis always ends on the next clean tick above the data (1/2/2.5/5 × 10ⁿ). No mark ever passes the top gridline.
2. **Gridlines follow the value axis.** 1px, solid, `--hfx-color-surface-line`. Horizontal for columns, lines and areas; vertical for horizontal bars; none on Gantt and timelines except tick guides. Never dashed (dashes mean "target" or "projection").
3. **Outer padding.** Categorical axes use half-band padding; continuous axes pad so a marker (plus its 2px ring) is never cut by the plot edge.
4. **Bars and columns.** Band fill ≤ 62%, thickness ≤ 24px on dense charts; 4px radius on the data end only, square at the baseline; 2px surface-colour gap between stacked segments and touching bars. Never a stroke around a mark.
5. **Lines and points.** Lines 2px, round joins. Markers ≥ 8px with a 2px surface ring. No lone dot on the last point unless it's labelled.
6. **Areas.** Series hue at ~12% (light) / ~18% (dark) under a 2px line.
7. **Type scale.** Title 16/600; subtitle 13/400 secondary; axis ticks and legends 12/400 secondary, tabular numbers on axes and tables; sentence or title case, never all-caps ticks. Fonts inherit from the page. One number format per chart; values never show more precision than the data.
8. **Direct labels before legends.** Label line ends, slope ends and the one bar the story is about; a legend for ≥2 series, never repeating row labels. Labels that don't fit move outside the mark or into the tooltip; nothing is clipped.
9. **Sequential and missing data.** Sequential ramps start near the surface; the legend has a title and units; missing data has one library-wide look (hatched 45°, secondary ink) that is visibly different from zero.
10. **Density by row count.** Bullet, progress, Gantt, state and strip rows get a fixed 28–40px slot; chart height follows row count instead of stretching to the canvas. Group headers sit 8px above their first row.

In code, each rule has one owner that every family uses, so a new family inherits it:

| Rule | Shared mechanism |
|---|---|
| 1 Round axis ranges | `ChartValueAxisRange` snaps every automatic linear value axis (Y, horizontal X, scatter X) to its next tick after the readable step is chosen; count data gets whole-number steps. The old 8% headroom is gone. |
| 2 Gridlines, category order | `ChartGridPolicy` turns the default grid into value gridlines for transposed charts and tick guides for schedules; `ChartCartesianOrientation.HorizontalCategoryProjection` puts named horizontal categories first-at-top for marks, projection and viewports alike. |
| 3 Outer padding | `ChartRange.ApplyCategoryPadding` gives lines, areas and strips over named categories half-band padding; marker padding includes the 2px ring. |
| 4 Bars | `ChartMarkSurface.BandFill`, `BarCorners`/`VisualCorners` (data-end radius on a scene rectangle, drawn the same in SVG and PNG), `StackSegmentBounds` (2px surface gap), `MaximumThickness` (stacks at most 24px unless authored). |
| 5–6 Lines, markers, areas | Theme defaults (2px line, 8px marker), the marker surface ring in `VisualMarkerScene`, `VisualTheme.AreaWash(mode)`; no automatic last-point dot unless labelled. |
| 7 Type | Typography defaults (16/13/12/12), title weight 600, secondary legend ink, `tabular-nums` on axis ticks; once one category caption wraps, every two-word caption wraps the same way. |
| 8 Labels | `ChartLegendVisibility.RepeatsRowLabels` / `UsesDirectSeriesLabels`, slope end labels, dumbbell Before/After legend (`ChartDumbbellOptions.StartLabel`/`EndLabel`). |
| 9 Missing data | `ChartMissingData` paints the one hatched no-data look (heatmap, calendar, treemap, legend keys); sequential heatmaps span their observed range. |
| 10 Density, status | `ChartRowDensity` (28–40px rows, rows stack from the top) for bullet, progress, Gantt, timeline and state timeline; totals, gauge arcs, bullet values and neutral flow nodes no longer wear status colours. |

`DesignRuleGuardTests` runs the rules over every gallery family (wide and compact, light and dark): nice value-axis ends, a sequential ramp that stays visible on both surfaces, square bar baselines, stacked gaps, status roles only on status-encoded data, no legend repeating row labels, no text overflowing its mark or the canvas.

House-style references: the dark support schedule with its "Now" line and current row, and the bar/column family once rule 4 is applied. Not references until fixed: gauge, bullet, progress, treemap (dark), sankey, the restaurant dashboard.

## 3. States: one visual language

Every interactive chart uses the same five states, rendered the same way in every family, in both schemes and in forced-colors mode.

| State | Look | Forced colors |
|---|---|---|
| Rest | as specified above | system colours |
| Hover / focus | mark lifts: +8% lightness and a 2px surface ring; everything else stays | `Highlight` outline |
| Emphasis (hovering a series or legend entry) | other series dim to 25% opacity | others `GrayText` |
| Selected (clicked) | 2px primary-ink outline, offset 2px, on a 2px surface halo; persists. Never a series colour | `Highlight` outline |
| Linked (highlighted from another view) | info-tone fill wash + a 3px bar at the mark's start, the same tone CanopyX uses for linked rows; non-linked marks dim to 35% | dashed `Highlight` outline |
| Current (`aria-current`) | a band across the row (schedules), or on a mark a 2px secondary-ink outline offset 2px on a surface halo, + semibold label; never a dashed box (that reads as focus) | `Mark` band |

Focus rings are `:focus-visible` only, 2px `--hfx-color-focus-ring`, offset 2px. Selection and focus never look alike.

## 4. Interaction: the enhance layer

The static SVG is complete without script: links, `<title>`, `aria-*`, and an optional table. A small `cfx-enhance.js` (budget ≤ 12 KB gzip JS + ≤ 2 KB CSS, loaded once per page, no dependencies) upgrades any ChartForgeX SVG in place from its DOM contract. It never re-renders the chart.

- **Tooltips.** One per page, `role="tooltip"`, tied to the focused mark with `aria-describedby`. Value first (strong), series second (secondary), a short line key in the series colour, never a filled box. Shared-X tooltips list every series at that X. Hosts can supply a `<template data-cfx-tooltip>`; content is filled with `textContent` only. Placement flips and clamps to the viewport; on touch, tap shows and a second tap follows the link.
- **Crosshair** on line, area and time charts: a 1px vertical hairline that snaps to the nearest X, plus dots on each series at that X. Readers aim at a date, never at a 2px line. Works at fluid width.
- **Hit targets** are bigger than marks: ≥24px for points (nearest-point lookup on dense scatter), the whole band for bars.
- **Legend.** Entries are buttons: click toggles a series (`aria-pressed`), Shift+click isolates, hover emphasises. Toggling keeps colours stable and rescales only if the host opts in.
- **Keyboard.** One tab stop per chart (roving tabindex). Arrows move between marks along X; Up/Down switch series; Home/End jump; Enter follows the mark's link; Space selects; Escape clears selection and closes the tooltip. A polite live region announces "Tuesday 14 July, Updates 12".
- **Motion.** Transitions only inside `prefers-reduced-motion: no-preference`; 150ms ease-out for hover, 250ms for filter/highlight changes; nothing animates on first paint.
- **Events.** `cfx:hover`, `cfx:select`, `cfx:linkchange` (below), each with the mark's keys and values.

The full `Interactivity.Html` runtime (zoom, brush-to-zoom, scenarios, export, annotation, adaptive relayout) stays as the heavy option for dashboards; it adopts the same states and tokens.

## 5. Linked views with CanopyX (and any view)

ChartForgeX speaks CanopyX's linked-views contract, so a chart and a grid follow each other with a few lines of host code, and HtmlForgeX can wire them in reports.

**Marks carry records.** Each mark lists the record keys it represents (`data-cfx-keys`, or the compact data island for large sets) and the field values that define it (`data-cfx-field="day"`, value `2026-07-14`). A stacked segment knows its day and kind; a Gantt bar knows its version; a heat cell knows its row and column.

**Chart → grid.**
- Click (or Space) on a mark: emit `cfx:linkchange` with the same payload shape as `canopyx:linkchange`: `{ view, action: "select", scope: "select", keys, values: { day: ["2026-07-14"], kind: ["Notes"] }, label: "Tue 14 Jul · Notes" }`. A host calls `grid.setExternalFilter("activity-chart", { field: "day", values: [...], sourceLabel: "Activity chart" })` or `grid.setHighlight(keys, { source: "Activity chart" })`. The grid shows its chip ("Filtered by Activity chart: Tue 14 Jul ×").
- Drag across a time axis (brush): emit a `range` `{ column, from, to }`, which the grid applies as a date-range filter. Shift+click adds to the selection.
- Escape or clicking empty plot: `action: "clear"`.

**Grid → chart.**
- The chart implements the same `LinkedView` methods CanopyX does: `setHighlight(keys, { source })` lifts the marks containing those keys (Linked state) and dims the rest; `setExternalFilter(source, { field, values | range })` dims non-matching marks (or hides them with `mode: "hide"`, rescaling only on request); `clearHighlight()`, `clearExternalFilters()`.
- Selecting grid rows (`canopyx:linkchange`, action `select`) highlights the marks those rows belong to; a grid filter dims marks outside it.
- Removing the grid's chip (`canopyx:externalfilter`, reason `"user"`) clears the chart's selection, so the two never disagree.

**No echo, no loops.** External filters and highlights never emit `cfx:linkchange`, exactly like CanopyX. A shared helper does the wiring for the common cases:

```js
CfxLink.connect(chart, grid, { field: "day", mode: "filter" });      // chart click filters grid; grid selection highlights chart
CfxLink.connect(chart, grid, { field: "version", mode: "highlight" });
```

The helper is tiny and neutral (it lives with the enhance layer and could move to a shared `hfx-link` part); HtmlForgeX can emit the same call from .NET.

**What the reader sees.** Click Tuesday's "Notes" segment: the segment takes the Selected outline, the grid shows "Filtered by Activity chart: Tue 14 Jul · Notes ×" and its 3 rows; everything else in the chart dims. Select two grid rows from Thursday: Thursday's column lifts with the Linked wash, the rest dims, and the live region says "2 rows highlighted in Activity chart". Remove the chip: both views return to rest together.

## 6. Output media profiles

One chart definition, rendered for where it goes. The profile picks behaviour; the visual rules don't change.

| Profile | Used by | Output | Interaction |
|---|---|---|---|
| Web page | ChangeIntel, Licensing | inline SVG, fluid width, inherited fonts, CSP-safe (no inline script or style attributes the host forbids) | `cfx-enhance.js` |
| Offline report | HtmlForgeX, PSWriteHTML successors, CanopyX reports, TestimoX | inline SVG + inlined enhance runtime once per document | enhance, or full runtime for dashboards |
| Dashboard | TestimoX monitoring, HtmlForgeX dark themes | inline SVG | full runtime (zoom, brush, compare) |
| Email | HtmlForgeX.Email consumers | PNG at 2x with alt text and a table; no SVG, no script | none |
| Document | OfficeIMO, PSWriteOffice | PNG/EMF or native chart; texture on for print | none |
| Image | ImagePlayground, PowerBGInfo, ConsoleMonster, social cards | PNG | none |

Print and forced-colors turn on texture (one 45°/135° directional fill) so identity never depends on hue.

## 7. Acceptance

A chart change is accepted when it is checked running, at 1200px and 390px, light and dark, with keyboard only, and with a linked CanopyX grid where the family supports linking: the states in §3 look as specified, nothing clips or overlaps, the palette validator passes for any new categorical set, and the static SVG still works with script off.
