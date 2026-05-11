# Chart Release Boundaries

This document records the first-release chart catalog decision. It is intentionally about public API boundaries and maturity, not a marketing list.

## Decision Summary

No current `ChartSeriesKind` is marked for removal before the first release.

The catalog is broad, but the split is mostly coherent:

- chart series stay in `ChartForgeX.Core`
- chart-only composition stays in `ChartGrid`
- exact-fact and dashboard widgets stay in `ChartForgeX.VisualBlocks`
- infrastructure/dependency diagrams stay in `ChartForgeX.Topology`
- optional browser behavior stays in `ChartForgeX.Interactivity` and adapter packages

The release cleanup is therefore not to remove chart families. It is to make maturity explicit, keep non-chart content out of chart-series APIs, and clean naming/overload sprawl before 1.0.

## Stable First-Release Chart Families

These are good first-release material. They represent ordinary charting concepts, have SVG/PNG coverage, and should avoid breaking changes unless a correctness or naming issue is found.

| Family | Stays | Notes |
| --- | --- | --- |
| Cartesian trend and comparison | `Line`, `StepLine`, `Area`, `StepArea`, `StackedArea`, `Scatter`, `Slope`, `TrendLine` | Keep as the basic multi-series chart surface. Smooth variants remain fluent helpers, not separate series kinds. |
| Bars and distributions | `Bar`, `HorizontalBar`, `Lollipop`, `Bubble`, `ErrorBar`, `RangeBand`, `RangeArea`, `RangeBar`, `Dumbbell`, `BoxPlot`, `Candlestick`, `Ohlc`, plus bar-backed helpers such as `AddHistogram` and `AddPareto` | Keep statistical/financial shapes in the chart catalog because they are data marks, not dashboard widgets. |
| Heatmaps and calendars | `Heatmap`, `HexbinHeatmap`, `CalendarHeatmap` | Keep as exclusive chart families. Preserve zero-versus-empty semantics and metadata. |
| Part-to-whole and hierarchy | `Pie`, `Donut`, `Treemap`, `Sunburst` | Keep, with validation against invalid or ambiguous hierarchy/value input. |
| Flow and structure charts | `Sankey`, `Tree` | Keep as chart families. Topology diagrams remain separate because they model nodes/edges/layout semantics beyond chart series. |
| Schedules and intervals | `Timeline`, `Gantt` | Keep as chart families for data intervals. `ScheduleTimelineBlock` stays a visual block because it is a dashboard card/scheduler widget. |
| KPI and radial charts | `Gauge`, `Circle`, `RadialBar`, `LayeredRadial`, `Bullet`, `Waterfall`, `Radar`, `PolarArea` | Keep, but avoid inventing chart kinds for every KPI card style. Many KPI cards belong in `MetricCard` or other visual blocks. |

## Supported But Still Worth Refining

These should stay, but they deserve extra naming and consumer-fit review before 1.0 because they are either newer, broader, or easier to overfit.

| Area | Stays | Cleanup decision |
| --- | --- | --- |
| Map charts | `DottedMap`, `RegionMap`, `TileMap`, map viewports, route helpers, GeoJSON import helpers | Keep. Built-in map definitions and external catalog loading are useful, but avoid product-specific maps or hidden asset requirements. External/large datasets must stay opt-in and discoverable through external catalog entries. |
| Pictorial/progress/word visuals | `Pictorial`, `ProgressBar`, `WordCloud` | Keep. Treat as chart families only while their input is data-driven and bounded. Do not turn arbitrary infographic layout into chart-series APIs. |
| Dashboard style helpers | dashboard themes, dashboard bar/row/trend presets, highlight ranges, point-color ranges | Keep helpers that reduce repeated renderer code. Avoid one-off helpers named after screenshots or products. |
| Opaque raster exports | BMP, PPM, TIFF and `RasterImageFormat` helpers | Keep as utility exports over the shared raster buffer. PNG remains the main report-grade raster target. |
| Generic save helpers | `Save`, `SaveImage`, `SaveRasterImage`, extension-inferred helpers | Keep for convenience. The extension-inferred behavior is part of the first-release contract and is documented in `docs/export-api.md`. |

## Not Chart Series

These must not be promoted into `ChartSeriesKind` just because they appear in dashboards:

- tables, lists, scorecards, metric cards, KPI strips
- segmented progress cards and composition/status cards
- workload lists, activity timelines, schedule swimlanes
- dashboard shells, sidebars, filters, inspectors, tabs, and report panel layout
- topology diagrams, icon catalogs, stencil browsers, and infrastructure maps
- live data collection or product-specific calculations

Those belong in `VisualBlocks`, `Topology`, examples, host applications, or adapter packages. This is the main protection against a confusing charting API.

## Naming And Grouping Rules

- Prefer data/visual names such as `RangeBand`, `Dumbbell`, and `CalendarHeatmap` over product or screenshot names.
- Keep `Add*` methods for adding data series or exclusive chart payloads.
- Keep `With*` methods for options, styling, and behavior.
- Avoid duplicate aliases unless they make a common charting term easier to discover.
- Keep exclusive chart families centralized in `ChartSeriesKindTraits` so range calculation, axes, legends, and renderer dispatch do not drift.
- Keep map classification in shared traits instead of per-renderer conditionals.
- Keep non-chart composition out of `ChartGrid`; use `VisualGrid` when a report mixes charts with exact facts.

## Cleanup Before 1.0

- Keep `Save`, `SaveImage`, `SaveRasterImage`, and `ToRasterImage` documented as one export API group.
- Keep map catalog names and external-catalog discovery split so built-in and host-provided map definitions are easy to distinguish.
- Keep visual-block names generic and reusable instead of naming APIs after specific screenshots, products, or host applications.
- Maintain focused docs for extension-inferred export behavior and unsupported file extensions.
- Keep release notes in GitHub Releases and keep package release notes short.
- Promote only stable visual examples into numeric visual baselines; keep topology/geographic baseline promotion explicit.

## Removal List

None for the first release.

If a chart family is proposed for removal later, require all of the following:

- no meaningful generated example or real consumer use
- weak SVG/PNG parity or weak validation that cannot be fixed cheaply
- a better existing chart or visual-block API already covers the same need
- migration guidance for any public method or enum value being removed
