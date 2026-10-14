# ChartForgeX v2 consumer migration

This guide records the breaking-release target and the observed consumer contracts. Charts, grids, topology, flow and sequence diagrams use the shared native scene. Optional package extraction and actual consumer upgrades have their own qualification gates. The [capability ledger](consumer-capabilities.csv) maps inspected calls to their destination, migration recipe and acceptance fixture. Its `owner_implementation` and `owner_evidence` columns distinguish available source and owner fixtures from downstream qualification. A row with `status=planned` still requires consumer migration or qualification; an implemented owner path does not close that gate. The ledger covers observed consumer capabilities, not every exported member of ChartForgeX.

Qualify consumer candidates from their intended branches. A primary checkout, remote source, local project reference and installed NuGet package are different evidence boundaries; source migration alone does not establish an installed consumer or a public release.

## Unified authoring contracts

Use the same immutable `VisualTheme` and explicit `VisualThemeMode` for every visual model. Replace `ChartTheme` and `TopologyTheme` with paired presets such as `VisualTheme.Graphite()` or `VisualTheme.Report()`, then select `Dark` through `WithTheme(theme, VisualThemeMode.Dark)` or `ThemeMode`. Replace mutable theme and brand-kit edits with `theme.Configure(builder => ...)`; assign the returned snapshot. Palette colors live in `builder.Light` and `builder.Dark`, typography in `builder.Typography`, and geometry/effects in their named builder properties. Local canvas/story/window controls live in their owning style options.

Replace full-theme `FromThemeJson`/`ToThemeJson` with `FromJson`/`ToJson`. The former generated-color `FromJson` importer is `FromPaletteJson`. Version-1 full themes remain readable; independent plot border/radius, axis-title size and monospace family have explicit backward defaults. Shared effects apply to both modes. The Report preset uses shadow opacity 0.10 in both modes. Grids use their own theme and mode instead of inheriting the first child implicitly.

Keep a stable series handle instead of configuring by display name or collection position. Use `ChartSeries.CreateLine(new ChartSeriesId("orders"), "Orders", points)` and `chart.AddSeries(series)`, or the corresponding typed factory. Direct numeric construction requires an explicit ID. Replace `InteractionKey`/`WithInteractionKey` with that construction-time ID. Duplicate labels are permitted; duplicate series IDs within one chart are rejected. Use `WithName` to rename display text, and `MoveSeries`/`RemoveSeries`/`ClearSeries` for the read-only series view. Automatic IDs from fluent `Add*` are local and do not synchronize across charts.

For chart scenario routes, use `AddSeriesStep(series.Id, ...)` with `ChartForgeX.Interactivity.Html` in scope. The string overload targets the exact stable ID, including numeric strings: `"0"` identifies a series whose ID is `new ChartSeriesId("0")`. Replace positional series targets with the intended handle's `Id`. See [series identity and scenario steps](../interactivity.md#semantic-series-identity) for synchronized charts and source observation IDs.

Replace `WithPointKey` with `WithObservationId(index, new ChartObservationId("item-id"))` where a stable authored identity is needed. Typed-family source ordinals now count observations, not encoded members. Read `FinancialValues` for Candlestick/OHLC, `Bubbles` for Bubble, and the named range/error/box/dumbbell/difference collections for their families. Numeric `Points` is empty and read-only for these kinds. Replace tuple-member edits with `Update*` for one complete observation or `Replace*` for the entire sequence. Single-item updates preserve ID and point styling; whole replacements clear per-observation metadata after successful validation.

Calendar and map exports retain IDs assigned to their normalized observations, including duplicate dates and region aliases combined during ingestion. Missing days or regions have no authored observation ID. A calendar cell with multiple current numeric contributors keeps its aggregate identity instead of borrowing one contributor's ID. Native SVG and the HTML selection event expose the same captured observation ID.

Typed point metadata uses one `data-cfx-source-point` ordinal plus named raw facts such as `start`/`end` or `open`/`high`/`low`/`close`. Remove dependencies on `data-cfx-source-0-*` tuple-member fields. Raw-sample box plots retain their original sample count and raw samples; replacing or updating their summary clears stale sample provenance.

State timelines and Gantt lanes retain complete `StateTimelineSegments` and `GanttLaneItems`; their numeric `Points` view is empty and read-only. Replace interval/point-label edits with `UpdateStateTimelineSegment` or `UpdateGanttLaneItem`, and use the corresponding `Replace*` operation for a complete sequence. Factories and full replacements sort by time; an individual update preserves its observation index and ID while the renderer places it by time. Use `CreateStateTimelineLane` or `CreateGanttLane` to configure a retained handle with an explicit ID. Empty lanes export a no-data state.

Categorical `HeatmapCells` now exposes every column, including null masked cells. Replace edits to raw points or parallel labels with `UpdateHeatmapCell` or `ReplaceHeatmapCells`; the cell owns its state, text, tooltip and link. `ObservationCount`, `SourcePointCount` and categorical `data-cfx-point` indices include masked columns. Select a cell by its column index or authored observation ID rather than its previous position among visible cells. `CreateHeatmapCategoryRow` accepts a stable series ID; the lane and categorical-row `Add*` conveniences also accept a named `id`.

Relationship series and typed Waterfall series also expose an empty read-only `Points` view. Their nodes, links, hierarchy items or waterfall items own the data; incompatible raw-point mutation fails immediately. Numeric Waterfall series keep their editable XY observations.

Gantt dependencies belong to the chart and refer to retained series handles. Replace `AddGanttDependency(predecessorIndex, successorIndex)` with `AddGanttDependency(predecessor, successor)` after attaching both series. The fluent `AddGanttTask` and `AddGanttMilestone` `dependsOn` argument accepts an already attached predecessor handle and adds the same chart-owned relationship. `ChartGanttItem` and standalone Gantt factories describe schedule observations without dependencies. Read `PredecessorId` and `SuccessorId` from the detached `GanttDependencies` snapshot. Moving a series preserves its links; removing either endpoint removes the link, and `ClearSeries` clears every link. Clearing observations temporarily hides their connectors while retaining the attached series and its relationships; restoring observations on that handle restores those connectors.

Option callbacks now clone, configure and install an independent result. Callback exceptions leave the owner unchanged. Do not mutate a reference retained from a previous getter or from inside a callback to configure the newly installed state; use the current property or another callback. Constructors and owners share flat-bar/plain-line/horizontal-guide defaults. Assigning default values remains explicit, including false text-style overrides.

The [API conventions](api-conventions.md#one-theme-and-an-explicit-mode) contain current examples and ownership details. Migrate and qualify real consumers against candidate packages before delivering the break; source compilation does not establish public availability or installed-host readiness.

## Typed configuration callbacks

Replace `With*` calls that accept typed configuration callbacks with `Configure*`. Rebuild callers compiled against the former methods. The value and object overloads keep their `With*` names; there are no forwarding aliases.

| Previous callback | Current callback |
| --- | --- |
| `Chart.WithGauge`, `WithLabels`, `WithAccessibility` | `ConfigureGauge`, `ConfigureLabels`, `ConfigureAccessibility` |
| `Chart.WithTextStyle` | `ConfigureTextStyle` |
| `Chart.WithTitleStyle`, `WithSubtitleStyle` | `ConfigureTitleStyle`, `ConfigureSubtitleStyle` |
| `Chart.WithAxisTitleStyle`, `WithTickLabelStyle`, `WithLegendStyle` | `ConfigureAxisTitleStyle`, `ConfigureTickLabelStyle`, `ConfigureLegendStyle` |
| `Chart.WithDataLabelStyle` | `ConfigureDataLabelStyle` |
| `Chart.WithBarVisualStyle`, `WithLineVisualStyle`, `WithGridStyle` | `ConfigureBarVisualStyle`, `ConfigureLineVisualStyle`, `ConfigureGridStyle` |
| `ChartSeries.WithDataLabelStyle`, `WithPointDataLabelStyle` | `ConfigureDataLabelStyle`, `ConfigurePointDataLabelStyle` |
| `ChartGrid.WithTitleStyle`, `WithSubtitleStyle` | `ConfigureTitleStyle`, `ConfigureSubtitleStyle` |
| Chart, chart-grid, topology and visual-grid `WithTheme` callbacks | `ConfigureTheme` |
| `TopologyChart.WithLabels`, `WithAccessibility` | `ConfigureLabels`, `ConfigureAccessibility` |
| `VisualCanvas.WithAccessibility` | `ConfigureAccessibility` |
| `ChartTable.WithRow`, `TableArtifact.WithRow` | `ConfigureRow(index, callback)` |
| `FlowArtifact.WithStep`, `WithConnector` | `ConfigureStep(id, callback)`, `ConfigureConnector(index, callback)` |

For example, use `chart.ConfigureLabels(labels => labels.NoData = "Brak danych")` and `chart.ConfigureBarVisualStyle(style => style.CornerRadius = 4)`. Supplying a complete style still uses `chart.WithBarVisualStyle(style)`. `ChartColorScale.WithLabels("Low", "Middle", "High")` still returns an immutable scale copy.

Configuration callbacks now have one ownership rule: edit an isolated copy, then install a separate snapshot after success. Exceptions leave the current owner unchanged. A reference retained before or inside a callback cannot mutate the newly installed options. Theme configuration returns an immutable paired snapshot. See the [API conventions](api-conventions.md#configuration-callback-ownership) for the complete boundary and explicit override intent.

## Bubble size mapping

Bubble series share one chart-wide size domain. Previously, each series inferred its own domain, so equal source sizes could render at different radii. Single-series defaults retain their existing square-root radius mapping and responsive endpoints. A constant automatic domain retains the radius midpoint.

Replace bubble `Markers.Radius`, `MarkerRadius` or `WithMarkerRadius(...)` overrides with `ChartOptions.Bubble` endpoints. Any non-null series radius now throws during preparation, including zero. Clear an existing override with `UseThemeMarkerRadius()` and configure the shared range:

```csharp
chart.Series[0].UseThemeMarkerRadius();
chart.ConfigureBubble(bubble => {
    bubble.WithSizeDomain(0, 100);
    bubble.MinimumRadius = 3;
    bubble.MaximumRadius = 24;
});
```

Use `series.ConfigureMarkers(markers => markers.Enabled = false)` to hide one series' glyphs. Marker shape, fill, outline and point-label overrides remain available. Explicit domains clamp geometry while retaining raw source sizes; `Reversed = true` maps larger values to smaller radii. Pin the domain and both radius endpoints for stable sizing across data and layout changes. Source sizes remain finite and positive, and the mapping does not promise equal painted areas across shapes. See [bubble sizing](api-conventions.md#bubble-size-scale) for validation and automatic defaults.

## HTML tooltip options

Configure tooltip content through `HtmlChartInteractionOptions.Tooltip.Mode` and `HtmlInteractiveDashboardOptions.Tooltip.Mode`; these replace the flat `TooltipMode` property. The existing `HtmlChartTooltipMode.Single` and `.SharedX` values are unchanged. Getter-owned `Tooltip` options also expose `Range`: `Exact`, `Nearest`, or `HtmlChartTooltipRange.WithinDistance(cssPixels)`.

The default range remains 120 CSS pixels. Enabling only `ChartInteractionFeatures.Tooltips` now also acquires nearby observations without requiring `Crosshair`. Choose `Exact` for direct pointer hits only. Crosshair labels default to visible for every palette; set `Crosshair.ShowLabel = false` to retain a compact label-free presentation. Keyboard and pinned readouts keep explicit target semantics.

## Range-bar endpoint captions

Existing range-bar models retain `LabelContent = Combined`, including custom point captions and lazy numeric formatting. Opt into `ChartRangeBarLabelContent.Endpoints` through `ConfigureRangeBar` to render authored Start and End separately. Use `EndpointLabelFormatter` for endpoint-specific text or suppression; its readonly context carries the complete interval and the existing chart-formatted numeric value. This adds no ingestion or source-index migration.

In endpoint mode, `WithPointLabel` remains the observation heading rather than replacing both endpoint captions. Both captions share the original observation target and inherited label typography; their SVG element IDs are distinct. Callbacks run once per endpoint during preparation, including when labels are hidden or cannot fit, and exceptions propagate. Prepared exports remain detached. See [endpoint captions](api-conventions.md#range-bar-endpoint-captions).

## Box plot grouping and orientation

Multiple box plot series at the same category now appear side by side instead of overlapping. Category pitch controls their thickness, so dense plots can use narrower bodies. Existing `AddBoxPlot` overloads and five-number summaries retain their values and source order; `ChartBarMode` does not stack them.

Set `ConfigureBoxPlot(options => options.Orientation = ChartOrientation.Horizontal)` for primary physical X values and Y categories. `ChartBoxPlot.X` remains the category coordinate. Move value bounds, scales, reversal and tick formatting to `XAxis`, and category bounds, reversal and formatting to `YAxis`. Explicit Y labels win; `WithXLabels` remains a fallback. Horizontal charts accept only box plot series on the primary axis. Vertical mixtures and secondary Y projection retain their support.

HTML tooltip rows now expose Category, Minimum, Q1, Median, Q3 and Maximum instead of a generic Y row containing only the minimum. Localize them through the corresponding `ChartLabels` properties. Default data labels still show the median, and custom point labels still win. Raw-sample calculations retain interpolated quartiles at `(n - 1) * p` and min/max whiskers; this change introduces no IQR filtering or new statistical estimator. See [box plot conventions](api-conventions.md#grouped-box-plots-and-orientation).

## Waterfall opening balances and checkpoints

Keep `AddWaterfall(name, IEnumerable<ChartPoint>)` for signed deltas with an automatically appended final total. For an explicit opening balance or intermediate checkpoints, pass `ChartWaterfallItem.OpeningBalance(x, value)`, `.Delta(x, value)`, `.Subtotal(x)` and `.Total(x)` to the typed overload. Include every desired display position explicitly; typed input appends no extra total. Items accumulate in input order with distinct X coordinates. An opening balance must be first and may occur only once; positive, negative, zero and opening-only input are valid. Subtotals sum changes since the opening balance or preceding checkpoint without resetting the running balance; totals include the opening balance and all changes. Input without an opening item still starts at zero. Use existing axis labels, point labels, point colors, fill patterns and label styles.

Replace `ChartWaterfallItem.DeltaValue` with nullable `Value`; the old member has no compatibility alias. `Value` holds an authored opening balance or delta and is null for calculated checkpoints. Default object serialization therefore writes `Value` instead of `DeltaValue`; update consumers of that member. Existing numeric enum values remain `Delta = 0`, `Subtotal = 1` and `Total = 2`; `OpeningBalance = 3` is appended.

For typed input, read the immutable `ChartSeries.WaterfallItems` snapshot instead of `Points`. `SourcePointCount` counts numeric observations, including the opening balance. Point overrides and point legends index every authored item, including checkpoints. A delta after a checkpoint therefore has different item and numeric-source ordinals. `ChartOptions.Labels.OpeningBalance` localizes the default opening caption and readout row.

Use `UpdateWaterfallItem(index, item)` to retain the item's ordinal, ID and presentation, or `ReplaceWaterfallItems(items)` to replace the full snapshot and clear its per-item metadata. Both operations reject invalid opening placement or duplicate coordinates before changing the source. Empty typed factories and replacements now create a no-data series with read-only `Points` and no appended total. Replacing numeric input through `ReplaceWaterfallItems` changes that series to typed authoring.

Observation IDs now follow authored item indices after checkpoints, including IDs explicitly assigned to a subtotal or total. `data-cfx-observation-index` exposes the authored ordinal; compact numeric contributor indices retain their existing meaning. A calculated checkpoint can therefore have its own authored ID alongside its derived identity and contributor facts. Automatically appended totals have no authored observation ID.

Checkpoint metadata, including the legacy appended total, exposes a derived identity and contributor ordinals in `data-cfx-source-points`. It omits `data-cfx-source-point` and `data-cfx-delta`; replace selectors that expected the appended total's former point/source index `-1` with a checkpoint-kind selector. `data-cfx-waterfall-kind` distinguishes `opening-balance`, `delta`, `subtotal` and `total`; `data-cfx-value`, `data-cfx-start` and `data-cfx-end` describe the displayed calculation. Opening balances retain a numeric `data-cfx-source-point` and omit `data-cfx-delta` and `data-cfx-derived`. Total contributors include the opening observation; subtotal contributors exclude it. The HTML adapter uses these facts for pointer, keyboard, caption and point-legend readouts.

Vertical orientation remains the default. Set `ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal)` to use the physical X value axis and Y category axis. Item `X` coordinates and source order retain their meaning. Move value bounds, scales and tick formatters to `XAxis`, and category bounds, reversal and formatters to `YAxis`. Explicit Y labels win; existing `WithXLabels` remains a category-caption fallback. Horizontal waterfalls support the primary axis only. Native `data-cfx-category-label` supplies the resolved axis caption to marks, point legends and HTML readouts.

## Hierarchy and flow identities

Replace the label-based relationship overloads with explicit nodes and links:

| Previous call or member | Current contract |
| --- | --- |
| `AddSankey(name, links, color)` | `AddSankey(name, nodes, links, color)` |
| `ChartSankeyLink(source, target, value)` | `ChartFlowLink(id, sourceId, targetId, value)` |
| `AddTree(name, links, color)` | Pass `IEnumerable<ChartNode>` before the links. |
| `AddSunburst(name, nodes, links, color)` | Pass `IEnumerable<ChartHierarchyItem>` instead; see [parent values](#sunburst-parent-values). |
| `ChartTreeLink(parent, child, value)` | `ChartTreeLink(parentId, childId, value = 1)` |
| `Chart.WithSankeyNodeState(...)` / `ChartOptions.SankeyNodeStates` | Use `chart.Series[0].WithNodeState(id, state)`; `ChartSeries.NodeStates` is a read-only view with ordinal ID comparison. |
| Endpoint/weight pairs in `ChartSeries.Points` | Read immutable `Nodes`, `FlowLinks`, or `TreeLinks`. `Points` is empty and `SourcePointCount` is zero. |

Every `ChartNode(id, label)` needs a non-empty, unique ID. Labels may repeat. Flow IDs are also non-empty and unique, including parallel flows between the same nodes. A tree child's ID identifies its one incoming branch. References, finite weights, and family graph constraints are validated before a series is added. Every Sankey node must participate in at least one positive flow; disconnected flow components remain supported. Invalid additions leave existing chart data unchanged. Empty relationship series still produce the native prepared no-data scene; nonempty raw point lists are rejected.

Keep node order explicit when preserving an existing layout or ordinal styling. The former order was first endpoint appearance in the link list. `WithPointColor`, fill-pattern, and data-label style overrides use node input ordinals for these families; semantic node states use IDs and are scoped to their series. State assignment validates ID membership and enum values before mutation. The collections copy the supplied inputs, and prepared exports remain detached from later model changes. The Mermaid CSV adapter maps its language-defined endpoint identities into nodes and assigns separate flow IDs at the adapter boundary.

Tree weights remain authored values and affect link emphasis rather than node placement. Sunburst now uses the shared parent-linked item contract below; its default geometry still aggregates descendant leaves.

Sankey node totals must remain finite and are validated before adding a series. Tiny or large finite flows retain proportional node and ribbon thickness. SVG scale metadata uses `data-cfx-weight-reference` and `data-cfx-normalized-weight-scale` instead of an absolute `data-cfx-weight-scale`; divide a raw weight by the reference before multiplying by the normalized scale.

`ConfigureSankey` edits the getter-owned `ChartOptions.Sankey`. Its defaults preserve the existing geometry and paint. Alignment and node order affect layout only; input collections, IDs, source ordinals and point-style assignments retain their authored order. Explicit widths/gaps that cannot fit the viewport fail during preparation. See [Sankey layout](api-conventions.md#sankey-layout) for defaults and the exact `Center` policy.

Sankey label placement defaults to `LabelPlacement = Right` and `EdgeLabelPlacement = Outside`, preserving the existing caption policy. Choose `Left`, `Right` or `Center`, then set the nullable edge override to `Inside`, `Outside` or `null` to use the same placement at both edges. These settings affect measured captions without moving nodes, changing flow thickness or replacing authored facts.

`ChartFlowLink` accepts finite non-negative raw facts. Sankey still requires positive weights and an acyclic graph with distinct endpoints. `AddChord(name, nodes, links, color)` accepts cycles, reciprocal/parallel flows, self flows, and zero values. Chord's combined incoming plus outgoing endpoint total must remain finite for each node; independent flows do not need a finite global raw sum. Zero or collapsed geometry retains authored identities without an inflated mark. Chord is explicitly appended as enum value 52; existing kind values remain unchanged. See [weighted chord charts](../chord.md) for options and native metadata.

Node and link groups retain authored IDs, labels, owning series, and actual `data-cfx-source-node-index` / `data-cfx-source-link-index` ordinals. SVG `data-cfx-target-kind` and `data-cfx-target-id` supply normalized node/link identities; HTML selection events expose those same IDs without invented `point` or `sourcePoint` ordinals. Parent, child, source, and target attributes now contain authored node IDs. Update selectors that assumed numeric node ordinals or labels as identities.

## Numeric radial series and progress rings

Replace former percent-ring `AddRadialBar` calls with `AddProgressRing`. The ring renderer retains its 0–100 values, independent ring paints and center average. Rename `WithRadialBarCenterLabel` / `ShowRadialBarCenterLabel` to `WithProgressRingCenterLabel` / `ShowProgressRingCenterLabel`. Radius and thickness settings become `WithRadialProgressRadiusScale` / `RadialProgressRadiusScale` and `WithRadialProgressStrokeScale` / `RadialProgressStrokeScale`; these also size layered radial progress charts. `ChartSeriesKind.ProgressRing` retains the former enum value 24. The appended `RadialBar = 50` and `RadialColumn = 51` members identify numeric series. SVG role names for percent rings use `progress-ring-*`.

`AddRadialBar` now maps numeric values to angles in radial category bands. `AddRadialColumn` maps numeric values to radii in angular category bands. In both methods, `ChartPoint.X` identifies a category and `Y` holds the signed source value. Use `YAxis` or the series' `SecondaryYAxis` for numeric bounds, scale and formatting, and `XAxis` for category labels and reversal. Categories are ordered by their numeric X identifiers; omitted observations remain absent, while authored zeros retain their identities and facts without painted sectors. Standalone numeric radial series retain distinct slots for repeated X observations.

`AddRadialColumn` also combines with the existing `AddRadarArea` and `AddRadarLine` calls. Mixed charts require a full-turn viewport and unique categories within each series. Category-axis reversal keeps columns and radar vertices aligned. A populated radar needs at least three categories across the chart. Radar follows whole-category centers, fills missing categories with derived zeros and selects its own primary or secondary numeric axis. Column stacks and normalization retain their existing membership. Standalone Radar and Polar now honor `UseSecondaryYAxis()` and that axis's bounds, scale, formatter and numeric reversal.

Set start/end angles, inner radius and category/series spacing through immutable `ChartRadialGeometryOptions` and `WithRadialGeometry`. Default numeric series are grouped. Named `StackGroup` values or stacked bar mode share slots; normalization uses the existing positive/negative stack owner. `NormalizedTo` targets partition by kind, axis and group and must agree within each stack. Raw labels and `data-cfx-y` retain counts; rendered contribution, baseline, endpoint and source total remain separate metadata. An all-zero normalized stack reports `numeric-radial.stack-zero-total`.

Set the optional trailing `cornerRadius` constructor argument to round numeric radial bars and columns. It defaults to zero, preserving existing output. Rebuild compiled consumers because the constructor signature now includes this argument; existing source calls remain valid. The finite, nonnegative pixel radius is a maximum, clamped inside each painted segment. Stacked segments round separately. Full rings remain seamless, and sectors without an inner radius keep their central apex. Source values, projected extents and clipping facts are unchanged.

Numeric domains may start above or below zero. A baseline outside explicit bounds clips to the visible edge, while raw values stay intact and `data-cfx-clipped` records the clipped extent. Linear, positive logarithmic and symmetric logarithmic numeric axes are supported. Time value axes, explicit category bounds and logarithmic category spacing reject clearly. Empty series preserve their legend/slot alongside populated series; an entirely empty chart reports no data.

`ChartAxis.Reversed` and `WithReversal()` also apply to ordinary Cartesian numeric/time projections and horizontal category axes. Marks, grids and tick positions use the same transform; logarithmic bars retain their positive domain baseline. Radar and Polar support angular reversal and reversal of the selected numeric radius axis. Schedule reversal and polar-area radius reversal reject because those layouts have separate span or area contracts.

Numeric radial series honor existing `XAxisTitle`, `YAxisTitle` and `SecondaryYAxisTitle` values, including the `WithXAxis` / `WithYAxis` / `WithSecondaryYAxis` builders. Horizontal titles reserve measured space inside the frame, so callers that already authored titles receive a smaller plot with visible titles. `ChartAxis.LabelOrientation` defaults to `Fixed`, preserving existing `LabelAngle` screen rotation. Choose `Parallel` or `Perpendicular` to follow the physical axis track, with `LabelAngle` as an offset and the resulting baseline kept upright. Relative orientation currently requires numeric radial bars or columns; other families reject it. Hidden and unused axes reserve no text space; `ShowLine = false` leaves text visible.

Numeric radial axes honor each axis's existing `LabelDensity`. `Auto` preserves its previous output, `Dense` permits tighter spacing, and `Relaxed` keeps more space around captions. `All` now accepts caption overlaps instead of applying automatic collision thinning. It still fits individual text within the viewport, avoids painted marks, and preserves one caption at a full-turn seam. Shortened or omitted captions retain their full descriptions; fitted captions still block later data labels and stack totals. See [chart combinations, orientation and axes](chart-combinations.md) for the supported mixed radial layout.

Native compact exports prepare the chart at compact dimensions. Text that cannot fit may be shortened or omitted with `numeric-radial.label-overflow`; descriptive regions retain the full observation. The HTML adapter's `Readable` layout preserves a minimum width in a contained horizontal viewport. `Fit` scales a fixed design, including its text, and may require a larger host viewport for legibility.
## Numeric color presentation

Treemap and Sunburst replace `ColorLegendTitle` with `ColorDimension.Name`:

```csharp
chart.ConfigureTreemap(options => {
    options.ColorDimension.Name = "Temperature";
    options.ColorDimension.ValueFormat = ChartValueFormat.Number("0.0 '°C'",
        System.Globalization.CultureInfo.GetCultureInfo("pl-PL"));
});
```

The owned `ChartColorDimensionOptions` instance keeps naming, formatting and numeric-key presentation together. Scatter and Bubble configure it with `series.ConfigureColorDimension(...)`; maps use `chart.ConfigureMapColorDimension(...)`, and numeric matrices and calendars use `chart.ConfigureHeatmapColorDimension(...)`. `ChartOptions.MapColorDimension` and `HeatmapColorDimension` expose the same owned instances. Categorical heatmaps reject this configuration.

Null name and format values restore their defaults independently. A supplied name must contain non-whitespace text. Independent color formatting leaves Cartesian Y, Bubble size, hierarchy size and authored secondary captions unchanged. Map and heatmap values encode color directly, so their default numeric labels and single Value row use the color format; explicit point labels remain authoritative. Missing data retains the localized no-data text and never calls a numeric formatter. A custom formatter's empty string remains empty in native semantics and browser readouts; it does not fall back to the raw number. An explicit format runs once per distinct finite measurement during preparation; exports reuse the captured text. Raw numeric metadata, scale mapping and selection identities remain unchanged.

Numeric key visibility now belongs to that same instance:

| Previous setting | Current setting |
| --- | --- |
| `Options.ShowMapScaleLegend` / `WithMapScaleLegend(false)` | `ConfigureMapColorDimension(options => options.ShowLegend = false)` |
| Treemap/Sunburst `ShowColorScaleLegend` | `options.ColorDimension.ShowLegend` |
| Numeric `ShowHeatmapScale` / `WithHeatmapScaleLegend(false)` | `ConfigureHeatmapColorDimension(options => options.ShowLegend = false)` |
| Categorical `ShowHeatmapScale` / `WithHeatmapScaleLegend(false)` | `ShowHeatmapCategoryLegend` / `WithHeatmapCategoryLegend(false)` |

`ChartSeries.ShowInLegend` controls categorical entries only. Code that used it to hide Scatter/Bubble or hierarchy numeric keys must set the corresponding `ColorDimension.ShowLegend` instead. `Options.ShowLegend` / `WithLegend(false)` remains the all-legends master for convenience exports. An explicit `Prepare(context)` retains the context frame's precedence. DottedMap follows the categorical entry-count policy and has no numeric scale key.

`LegendStyle.Auto` preserves the existing family presentation. Set `LegendStyle = ChartColorLegendStyle.Gradient` for a continuous key or `Swatches` for sampled continuous colors and exact discrete intervals. Explicit styles require a resolved `ChartColorScale`; legacy map/heatmap coloring supports `Auto`. `Gradient` rejects discrete bands. These controls leave raw values, mark paint, scale sampling semantics and interaction identities unchanged.

## Hierarchical Treemap

Replace `ChartTreemapItem` with the shared `ChartHierarchyItem(id, label, parentId, value, colorValue)` without changing those facts. For the earlier label-only constructor, supply an explicit ID and `parentId: null`. There is no label-derived identity overload. Flat items remain roots when `ParentId` is null. Preserve input order to retain ordinal point-color, pattern, and label-style overrides.

Add group items with null `Value` and reference their IDs from child items. A leaf requires finite `Value >= 0`; a group rejects supplied `Value` and aggregates its descendant leaves. IDs must be unique and non-empty, parent references must exist, and cycles, self-parenting, depth above 512, and non-finite group or forest sums are rejected before adding a series. Labels may repeat. Single leaves and multiple roots are supported. Zero sizes retain metadata without a fabricated positive area.

Replace `ChartSeries.TreemapItems` with `ChartSeries.HierarchyItems`; read that collection and `Nodes` rather than `Points` or `XAxisLabels`. `SourcePointCount` is zero and `TreeLinks` is empty: parent references are item facts, not authored weight-one links. Treemap groups and leaves expose normalized `node` targets, owning series, authored item IDs/labels, `data-cfx-source-node-index`, parent IDs, depth, and rendered aggregate or raw leaf `data-cfx-value`. Leaves also retain `data-cfx-authored-value`. Replace selectors based on `data-cfx-point` with `data-cfx-target-id`; HTML selection emits the same node IDs without fake point ordinals.

Supply optional finite `ColorValue` independently of size. `ConfigureTreemap` or `ChartOptions.Treemap` configures `GroupPadding`, `Gap`, `ShowGroupLabels`, `ColorScale` and `ColorDimension`. The scale uses supplied color observations, honors fixed bounds, and retains missing values as missing rather than zero. Default independent color uses the theme's sequential ramp. A custom no-data color and discrete named bands use the generic scale owner. Native SVG/PNG share geometry and scale swatches; prepared exports are detached from later option or source changes.

`WithPointLegend()` uses leaf keys when no numeric color legend is active; set `ColorDimension.ShowLegend = false` to use leaf keys with an independent color scale. Native keys retain `data-cfx-legend-target-kind="node"` and the authored ID in `data-cfx-legend-target-id`. HTML legend controls read the raw leaf value, toggle or isolate that item, and emit its `targetKind` / `targetId` alongside the owning series key. Synchronized charts resolve the ID rather than labels or input ordinals; a peer without that ID remains unchanged. Leaf keys retain their own distinct normalized `legend` identity and do not acquire Cartesian point facts.

Captions follow the chart-level `WithDataLabels(...)` setting. A series-level `WithDataLabels(...)` overrides it; `UseChartDataLabels()` restores the chart setting. Group headers reserve space only when labels and `ShowGroupLabels` are enabled. Use `WithDataLabels()` in examples that display node captions.

The Mermaid Treemap adapter retains section nodes and parent containment. Its language has no authored ID syntax, so it assigns distinct source-order IDs at the adapter boundary and keeps labels unchanged, including repeated labels.

## Sunburst parent values

Replace `AddSunburst(name, nodes, links, color)` with `AddSunburst(name, items, color)`. Each `ChartHierarchyItem` carries the existing node ID and label, its incoming link's `ParentId` and weight as `Value`, and an optional independent `ColorValue`. The root has null `ParentId`; a root without children requires a finite non-negative `Value`. Keep group values when preserving supplied facts. No node/link overload or compatibility adapter remains. Tree continues to accept `ChartNode` and `ChartTreeLink`.

Preserve the old node input order for ordinal paint, pattern and label-style overrides. Sunburst siblings now follow their item input order; if the former link order differed, reorder the items and reapply ordinal styling deliberately. Parent items may appear after their children. IDs remain the interaction identity even when labels repeat or item order changes.

`ChartOptions.Sunburst.ParentValuePolicy` defaults to `ChartHierarchyValuePolicy.LeafAggregate`: groups sum descendant leaves and ignore provided group values for geometry. `AuthoredTotal` interprets a supplied group value as its inclusive total; a null value derives the resolved children. Children consume their proportion of the parent angle, and a positive remainder leaves an unpainted part of the next ring. A child total larger than a supplied parent fails ingestion or preparation after an option change. Only bounded representational closure is accepted; zero parents cannot contain positive children. A positive remainder stays in the facts even when its gap is too small to distinguish visually.

Both policies require one root, unique IDs, existing parents, acyclic relationships, depth at most 512 and finite sums. Leaves allow zero. Positive singleton roots render a full circle; zero and precision-collapsed positive nodes retain metadata and zero-size semantic bounds without painted sectors. Small positive values have no minimum clamp, and angle ratios normalize before multiplication.

Read copied `ChartSeries.HierarchyItems` and `Nodes`; `Points` and `TreeLinks` are empty. Replace `data-cfx-authored-weight` with `data-cfx-authored-value`. Groups and leaves retain the provided size when present, while `data-cfx-value` is the resolved geometry value. Groups retain `data-cfx-remainder-value` including zero. The owning series exposes `data-cfx-parent-value-policy`; targets retain item IDs, parents, depths, source item ordinals and geometry status. There is no authored incoming link or `data-cfx-source-link-index` on a Sunburst node.

`ConfigureSunburst` configures `CornerRadius`, `ColorScale`, `ColorDimension` and the parent policy. `CornerRadius` uses the same finite, nonnegative maximum pixel radius and inward clamping as numeric radial sectors, with a zero default. Every group's and leaf's nullable `ColorValue` is an independent observation, with no inheritance or aggregation. The generic scale honors fixed ranges, discrete bands and missing colors. Ordinal explicit colors override the scale; semantic states preserve numeric fill and add an outline. Prepared SVG/PNG and option values remain detached from later model changes.

HTML tooltips show size and numeric color separately, including color zero and localized missing values. A differing supplied group value uses `ChartLabels.AuthoredValue` (default `Provided value`), and a positive remainder uses `ChartLabels.Remainder`. Equal supplied values and leaf values retain raw metadata without duplicate tooltip rows.

Optional secondary text uses `ConfigureSunburst(options => options.SecondaryLabelFormatter = context => captions.TryGetValue(context.Item.Id, out var text) ? text : null)`. `ChartSunburstLabelContext` provides the copied `Item`, its resolved `Value` and the shared `FormattedValue`. This additive API does not change the hierarchy item constructor or Treemap behavior. Null, empty or whitespace results preserve existing primary-only output. `SecondaryLabelStyle` inherits node typography at 80% of the primary size, with a ten-pixel minimum no larger than the primary caption, before applying explicit overrides; `SecondaryLabelSpacing` defaults to two finite, nonnegative logical pixels. Captions keep fixed font sizes and fit the native sector with primary content taking priority. Full secondary text stays separate from numeric facts in node metadata, descriptive regions and plain-text HTML pointer/keyboard tooltips. See [secondary captions](api-conventions.md#sunburst-secondary-captions).

A positive radius on an incompatible populated family fails preparation. Numeric `RadialGeometry.CornerRadius` rounds RadialBar and RadialColumn, including columns combined with radar overlays; `Sunburst.CornerRadius` supports only Sunburst. Pie, Donut, PolarArea, Chord and progress-ring geometry keep their own contracts.

### HTML Sunburst branch emphasis

Use the adapter-owned `Highlight` group on chart or dashboard options to emphasize the hovered or focused item and its root branch:

```csharp
chart.SaveInteractiveHtml("allocation.html", options => {
    options.Highlight.Mode = HtmlChartHighlightMode.RootBranch;
    options.Highlight.HighlightedItem.Stroke = ChartColor.FromHex("#38bdf8");
    options.Highlight.HighlightedItem.StrokeWidth = 2;
    options.Highlight.UnhighlightedBranch.Opacity = .22;
});
```

`RootBranch` applies to Sunburst node targets. Other families and legend targets keep their existing emphasis, including mixed dashboards. `Default` preserves existing presentation and ignores the style overrides. Pointer, keyboard and synchronized hover share the same local target identity. A branch is the first ancestor below the authored central root; repeated labels and reordered items do not change membership. The central root belongs to no branch. Focusing it leaves surrounding branches at native strength. Zero and collapsed items retain membership and keyboard facts without gaining painted sectors.

The active item composes `HighlightedBranch` then `HighlightedItem`; other items in that branch compose `HighlightedBranch` then `UnhighlightedItem`. Other branches use `UnhighlightedBranch`. Null properties retain the preceding style. Default opacity is one for the active item, .82 for its other branch members and .38 for other branches; Graphite uses one and .3 for those last two groups. The neutral central root stays at native strength. Explicit styles replace these transient values rather than multiplying group opacity.

`Stroke` adds a separate outline behind the native primitive. Native fill, hatch and semantic stroke remain intact. `StrokeWidth` is its outward width in SVG units; a color without a width uses 2.5. Null retains the preceding color/width; a transparent color or zero width explicitly hides the added outline. `Opacity` accepts finite values from zero through one, and `StrokeWidth` accepts finite non-negative values. Transient opacity zero still permits another branch to be acquired; current authored or host-hidden paint and clipping remain unavailable. Selection keeps its existing full-strength emphasis and glow. Pinning retains its readout while hover follows another item. Escape dismisses the pin and branch emphasis; Reset view also clears selections.

These options affect browser presentation only. Native PNG bytes and native SVG geometry/paint are unchanged; Sunburst targets additionally expose `data-cfx-branch-root` when they belong to a branch. Pages, fragments, externally registered assets and dashboard children share the same runtime and options.

## Numeric color scales

Replace `ChartMapColorScale` with `ChartColorScale`. Map calls keep their names: `WithMapColorScale`, `ChartOptions.MapColorScale`, `AddRegionHeatmap`, and `AddTileHeatmap` accept the generic scale. Design-token conversion uses `VisualDivergingRamp.ToColorScale(midpoint)` and `VisualDesignTokens.ToSequentialColorScale()`.

`ChartColorScaleMode` distinguishes sequential, diverging, and discrete selection. Continuous factory and label calls retain their normal-domain interpolation and stop colors. Finite tiny ranges and extreme ranges keep their endpoints and midpoint without widening the domain. An inferred constant domain uses `LowColor` and reports that same constant throughout its legend; two fixed bounds still require the maximum to exceed the minimum. `ColorFor(value)` works with two fixed continuous endpoints or discrete bands, while partly or fully inferred continuous domains use `ColorFor(value, sourceMinimum, sourceMaximum)`. Values and source bounds must be finite and ordered.

`WithValueRange(double, double)` becomes `WithValueRange(double?, double?)`. Ordinary calls such as `scale.WithValueRange(0, 100)` remain source-compatible; rebuild compiled callers because the method's binary signature changes. Update typed method groups to accept nullable arguments, for example `Func<double?, double?, ChartColorScale> setRange = scale.WithValueRange`.

Use `WithValueRange(0, null)` to fix only the minimum, `WithValueRange(null, 100)` to fix only the maximum, and `WithValueRange(null, null)` to reset both endpoints to inference. An inferred endpoint is clamped to its fixed counterpart rather than crossing it. Observations below a sole fixed minimum use `LowColor`; observations above a sole fixed maximum use `HighColor`, including collapsed domains. Empty sources leave inferred endpoints unresolved; retained keys show missing data without inventing a zero extent. Positioned scales still require two supplied bounds and preserve their interior anchors. Discrete scales still reject value ranges.

`Discrete(bands)` copies immutable `ChartColorBand` instances. Finite upper bounds are strictly ascending and exclusive; the final band has a null upper bound. The first band has no lower limit, and equality with a boundary selects the next band. `Bands` exposes a read-only list, and optional band names appear with their intervals in map legends. Discrete scales use those bounds directly and reject `WithValueRange`, `WithMidpoint`, and continuous endpoint labels. Only diverging scales accept `WithMidpoint`. Missing data keeps the optional `NoDataColor` and renderer/theme fallback policy; non-finite numbers are rejected rather than treated as missing.

Map, Sunburst and Treemap discrete legends use `ChartLabels.AllValues` for a single unbounded band and `ChartLabels.Value` between interior bounds. For example, `chart.ConfigureLabels(labels => { labels.AllValues = "Wszystkie wartości"; labels.Value = "wartość"; })` produces localized interval captions while their numeric metadata remains invariant. Band names and numeric value formatting remain independent choices.

## Raster image inputs and animation delays

Pass `RgbaImage` directly to `VisualCanvas.AddImage` or the image overload of `AddHeroBadge` for an independent pixel snapshot used by SVG and raster output. `RgbaImage` itself retains the supplied array; the typed canvas call copies it. The raw paired SVG href and RGBA contract remains available for vector producers. See [Visual Canvas](../visual-canvas.md) for ownership and bounded file-input options.

Explicit zero-duration image frames are preserved in GIF and APNG. Negative and excessive format durations fail instead of silently clamping through image-array `ToGif` or `ToApng` conveniences. Positive story transition timing is unchanged. `RasterAnimationOptions.PngCompressionLevel` controls APNG's stored, fastest, or optimal compression profile; [raster animation](../raster-animation.md) documents timing, limits, and caller-buffer lifetime.

## Shared visual defaults

Replace `VisualStoryTheme` and `TerminalTheme` with the shared immutable `VisualTheme` and an explicit mode, for example `.WithTheme(VisualTheme.Graphite(), VisualThemeMode.Dark)`. `VisualStoryThemes` and `TerminalThemes` provide authored paired palettes when a desktop or shell appearance is useful. Configure colors and typography with `theme.Configure(...)` and assign its returned snapshot; `tokens.ApplyTo(theme)` is removed. Move story window chrome, background gradients and syntax colors to `VisualStoryStyleOptions`, and terminal header and cursor overrides to `TerminalStyleOptions`. Desktop style factories such as `VisualStoryStyleOptions.MacOS(light: true)` configure local chrome separately from the theme's mode. Replay surfaces inherit the containing story's colors when their theme is null; pass a theme and `themeMode` for an independent replay palette. Prepared stories retain immutable themes and copy mutable styles. See [Stories](../stories.md) for current examples.

Default charts and diagrams use 24 logical units of outer padding, 17px bold titles, 13.5px subtitles, 13px legends and 12px axis/data labels. The paired theme owns these values; authored padding, text styles and model legend settings remain authoritative. Native compact exports lay out the data again instead of scaling a desktop chart and its text down.

`VisualFrame` accepts nullable legend visibility and placement. Omit them to use the producer's data-aware policy and the model's configuration; supply `showLegend: true` or a position to override that policy. Rebuild callers compiled against the former constructor signature. Single-series Cartesian legends and direct-label gauge/bullet legends are hidden by default. Explicit legends, slice lists, multiple series and meaningful state mappings remain available.

Pie and donut display lists descend by value, with an aggregate `Other` last. Original point indices, authored colors, formatter inputs and semantic model order remain unchanged. Their legend items show the raw value followed by a muted percentage. Bullet rows show `value / target`; format and culture settings apply to both numbers. Gauge values use the categorical palette unless declared warning/danger bands or an authored state/color supplies emphasis.

`VisualTypography` includes separate scalar and center-value sizes, defaulting to 34px and 20px. `VisualTheme.WithTypography(scale)` returns an independent paired theme and preserves its palettes, geometry and effects. Version 1 theme JSON accepts optional `scalarValueSize`, `centerValueSize`, `gaugeStrokeWidth` and `gaugeBandWidth`; older documents use the corresponding defaults. The curated gallery demonstrates full SVG/PNG exports, native compact layouts, paired themes and executable C# source for every chart kind.

`PreparedTopology.Analyze()` reports the coordinates and dimensions of the exported canvas, including its common frame and any output fit. Diagnostic nodes, ports, routes and labels therefore align with SVG/PNG and the interchange envelope. Remove consumer offsets that compensated for the former content-local report. Standalone `TopologyLayoutDiagnostics.Analyze(...)` retains its content-local layout contract; analysis does not change authored node positions or rerun routing.

| Consumer | Inspected source | Declared dependency / boundary |
| --- | --- | --- |
| [PowerBGInfo](https://github.com/EvotecIT/PowerBGInfo) | `v2-speedygonzales`, `63f30cef2a92df8cbd9b7a8d0780cd2dfdaec4d8` | Core + Visuals produce wallpapers; Stories retains the existing GIF file export. Preserve explicit source qualification and installed-module authoring. |
| [ImagePlayground](https://github.com/EvotecIT/ImagePlayground) | Initial `master` inventory at `fb433aad14a071e301cc4e7686a4680bf1fb8bab`; qualified managed-imaging candidate at `04a2f5f6500ba1d3a4a89f006787014791bb9f58` | The PowerShell surface requires core + Visuals + Stories and the HTML interaction adapter. The base image-processing library remains independent. |
| [OfficeIMO.ChartForgeX](https://github.com/EvotecIT/OfficeIMO) | `master`, `ee066fe0dced8c5948500aaecdd0757f1289d142` | Rebuild the core adapter against `2.0.0`; the Markdown companion also consumes Mermaid. Optional producers hand off through neutral artifacts. |
| Private HTML adapter | Isolated consumer candidate | Keep the base adapter core-focused; qualify composition and story peer packages independently. |
| Private reporting consumer | Isolated consumer candidate | Qualify the real report factories against a reproducible dependency closure and retain the wider reporting release gate. |

Paths in this guide and the CSV are repository-relative. Resolve the repository root through `EVOTEC_GITHUB_ROOT`, with the platform default described in `AGENTS.md` when unset. Source findings are not consumer builds, installed-module tests or package publication proof.

## Optional package migration

V2 moves public types between assemblies and uses package version `2.0.0`. Rebuild compiled callers. Domain namespaces stay stable for canvas, factual blocks, stories and terminal models; add the owning package reference rather than copying types or using forwarding shims.

| Existing capability | Owning package and migration |
| --- | --- |
| Charts, topology, flow, sequence and the six genuine block diagrams | `ChartForgeX`; existing source models and shared prepared export remain core |
| `VisualCanvas`, `ImageComposition`, factual metric/table/list blocks and static `VisualGrid` | Add `ChartForgeX.Visuals`; keep the domain namespaces |
| `VisualArtifactRenderOptions.Watermarks` | Use the Visuals `artifact.WithWatermarks(...)` decorator; semantic `Model` remains available |
| `VisualStory`, `TerminalStory`, transcripts and GIF/APNG output | Add `ChartForgeX.Stories`; existing story/terminal export extension names remain |
| Timed RGBA animation frames | Add `ChartForgeX.Stories` for `RasterAnimationEncoder`, `RasterAnimationFrame`, `RasterAnimationFormat` and `RasterAnimationOptions`; keep the `ChartForgeX.Raster` namespace and use Core `RgbaImage` inputs |
| `ImageComposition.ToGif()` or core GIF format dispatch | Resolve pixels with `composition.ToImage()` or `chart.ToRgbaImage()`, then call the Stories `ToGif()` extension |
| `VisualGrid.WithMotion(...)` | Keep static target IDs and create a Stories `VisualMotionPresentation` over the grid's common static output |
| `TopologyRenderOptions.Motion` / `WithMotion(...)` | Use `chart.WithMotion(motion, staticOptions)` to create a Stories topology presentation; static topology options stay core |
| Decorative static menus, selection controls, navigation arrows and action buttons | Remove their configuration; preserve dates, trend text and status as ordinary content |

Factual completion checks, activity completion, progress handles, selected-period emphasis and identity initials remain meaningful marks. Markup takes a Visuals reference for its existing table preview producer. Mermaid, core-only charts and static topology do not require either optional package.

The core artifact renderer accepts `IStaticVisualSource` through `RenderSource` or a producer model. Optional packages own their concrete drawing; hosts can retain semantic table/diagram data in `Model` while choosing a different static presentation. This export contract does not convert legacy blocks into native prepared scenes.

For a still artifact, apply the decoration before choosing its output format:

```csharp
using ChartForgeX;
using ChartForgeX.VisualArtifacts;

artifact.WithWatermarks(VisualWatermark.FromText("Draft"));
byte[] png = artifact.ToPng();
string svg = artifact.ToSvg();
```

The decorator copies watermark declarations and preserves their order. The overload accepting `VisualArtifactRenderOptions` captures topology and raster settings with the producer. Changing an already captured watermark object does not alter the decoration.

Use `artifact.ToWatermarkedArtifact(...)` when several exports share one artifact and each needs its own decoration. `artifact.Clone()` separates mutable host metadata, accessibility, regions and legend items; its semantic model and producer-owned render source remain shared. Use a prepared input when the host also needs detached geometry.

Visuals records the applied layer count as the invariant integer metadata value `presentation.watermarks`. The value travels through the neutral interchange envelope and JSON. A native host that cannot project those layers can report the loss without taking a Visuals dependency. Repeated decoration counts the producer's actual captured layers.

For animated topology, create one presentation from the static request:

```csharp
using ChartForgeX.Topology;

var motion = TopologyMotionOptions.RoutePulseForEdges("network");
var presentation = topology.WithMotion(motion, staticOptions);
byte[] gif = presentation.ToGif();
byte[] apng = presentation.ToApng();
string animatedHtml = presentation.ToHtmlPage();
```

The presentation keeps the detached prepared geometry. It does not add animation policy to `TopologyRenderOptions` or repeat layout for each output. For a one-frame GIF from a composition, import `ChartForgeX.Raster` and call `composition.ToImage().ToGif()`.

For animated SVG with marks, use `VisualWatermarkDecoration.ApplyToSvg(presentation.ToSvg(), marks)`. Animated HTML accepts the same optional SVG transformation through `presentation.ToHtmlPage(svg => VisualWatermarkDecoration.ApplyToSvg(svg, marks))`. The decorator retains script-free SVG animation. A presentation's common static-source contract exports its configured progress sample; use its explicit animation exports when motion is required.

The HTML interaction adapter accepts a trusted SVG factory through `topology.ToInteractiveHtmlPage(staticOptions, prepared => prepared.WithMotion(motion, staticOptions.ActiveScenarioId).ToSvg())`. The detached topology includes the content needed by scenario and label/group controls; the original options retain their initial selection and visibility. Stories uses the preferred scenario for route selection independently of static highlighting, including its normal fallback when that scenario has no route. Supply producer-generated SVG rather than untrusted arbitrary markup.

`prepared.ToArtifact()` derives kind and identity from captured semantics; `prepared.ToArtifact("host-id")` supplies a new host identity. A scene without captured semantics has kind `Unknown`; the adapter does not infer editable diagram data from drawing commands. To embed several artifacts in one HTML document, pass a distinct scope to `artifact.ToSvg(options, scope)`.

`VisualMotionPresentation.Create(grid, timeline)` adds SVG/HTML motion to the grid's static target IDs. Its PNG and common static-source exports retain the completed picture. VisualStory and TerminalStory artifact factories in `ChartForgeX.Stories` capture the completed display and transcript; rebuild the artifact after changing a story. `Model` retains the original semantic object independently of that captured picture.

## Static HTML embedding

`chart.ToHtmlFragment()` and `grid.ToHtmlFragment()` embed a responsive prepared SVG. The inline SVG shrinks to the host's width and preserves its aspect ratio without a page stylesheet. Its logical `width`, `height` and `viewBox` remain the prepared viewport; proportional scaling does not perform a new compact layout.

Prepared chart-grid children use the shared `data-cfx-role="panel"` group and a scoped `data-cfx-source-id="panel-N"` identity. Update selectors that used the legacy `grid-panel` role. Semantic panel regions retain the same source index and contain the translated child regions.

Hosts embedding a detached artifact can request the same policy explicitly: `prepared.ToSvg(new VisualSvgOptions(idPrefix: "capacity-left", colorVariables: hostVariables, responsive: true))`. Supply the host namespace and colour mapping when replacing an export policy. Standalone `prepared.ToSvg()` retains exact viewport sizing.

Topology convenience SVG exports retain `TopologyRenderOptions.UseResponsiveSvg`, which defaults to true. Turning it off preserves the fixed logical viewport. Neither mode changes the prepared diagram layout.

## Package selection

| Workflow | Target dependency |
| --- | --- |
| Charts, diagrams, shared color/text/pixels, static export and semantic interchange | `ChartForgeX` |
| Wallpapers, social canvases, low-level image composition, factual tiles/tables/lists and watermark decoration | `ChartForgeX.Visuals` → core |
| VisualStory, TerminalStory, motion timelines, topology route/scenario animation and GIF/APNG output | `ChartForgeX.Stories` → core |
| Real browser interaction | Existing `ChartForgeX.Interactivity` / `.Html` and the host |

Visuals and Stories are peers. Stories consumes common renderable/static inputs; Visuals must not depend on Stories to render a still canvas. Core must not switch over optional concrete types, reference watermark options, or load animation assemblies for a chart. Static GIF input/first-frame decoding remains a deliberately retained core input capability; GIF encoding, including single-frame output, belongs to Stories.

Do not move genuine diagrams solely because their current names end in `Block`. Wardley, Venn, packet, git graph, fishbone and block-layout diagrams remain core. Plain factual table layout belongs to Visuals; the neutral tabular semantic/interchange contract remains available to core adapters and markup parsers.

## Chart export and presentation changes

`chart.ToSvg()`, `chart.ToPng()` and `chart.ToRgbaImage()` resolve the model's size, padding, theme values, visible frame, font request and raster density into the common rendering contracts. HTML hosts and image composition use the same prepared chart output. `SvgChartRenderer`, `PngChartRenderer`, `SvgChartGridRenderer` and `PngChartGridRenderer` are removed; use the model convenience methods or `Prepare(context)`. There is no separate Graphite family renderer.

Use `Prepare(context)` when a host supplies an exact viewport, paired theme, frame and font. The context owns those presentation values; model data, axis configuration and explicit text overrides remain inputs to preparation. `VisualLayoutOptions` accepts uniform padding or `ChartPadding` with independent edges. `VisualFrame` carries measured legend row and height budgets, and omitted legend entries retain their full semantic descriptions.

Preserve the distinction between the outer card and the plot when migrating theme or host settings:

| Existing model theme/input | Shared prepared contract |
| --- | --- |
| `ChartTheme.CornerRadius` | `VisualTheme.CardRadius` for the outer frame card |
| `ChartTheme.PlotCornerRadius` | `VisualTheme.PlotRadius` for the plot surface; set `BarRadius` separately for data marks when the authored style uses the same radius |
| `ChartTheme.CardBorder` / `PlotBorder` | `builder.Light.Border` / `builder.Light.PlotBorder` and the corresponding `Dark` palette fields; card and plot strokes remain independent |
| `ChartTheme.AxisTitleFontSize` / `TickLabelFontSize` | `VisualTypography.AxisTitleSize` / `AxisSize`; replacing the font family preserves both sizes |
| `ChartTheme.ShadowOpacity` / `ShadowColor` | `VisualTheme.CardShadowOpacity` / `CardShadowColor`, including authored color alpha |
| `ShowCard` with `Theme.UseCard` | Set the model's `ShowCard` or `WithCard(...)`; `VisualFrame.ShowCard` controls explicit preparation, and convenience exports respect `HostOwnsFrame` |
| `ShowPlotBackground` | `VisualFrame.ShowSurface`, independently of the card |

Convenience exports perform these mappings. Explicit contexts set the frame and theme directly. Card shadows are shared native layers constrained by outer padding; zero opacity remains flat, and exhausted padding omits the shadow with a diagnostic. They do not enlarge the output or move plot coordinates. Transparent canvas settings suppress the canvas background while an explicitly enabled card remains visible.

For a categorical legend heading, pass `legendTitle` to `VisualFrame`. Null permits a producer title, such as a topology legend title; an empty string suppresses it. The common frame measures it within the legend height budget and retains its full semantic text when it cannot fit. Chart and grid frame copies preserve the title, styles and row/height budgets, so hosts do not need a separate heading layer.

`VisualTheme.Graphite()` resolves the canonical paired color document. Themes are immutable, and preparation retains the resolved presentation independently of later model theme replacements. Explicit SVG color mappings retain paint roles; equal RGB values do not merge series, status, surface and text roles. Raster output uses the resolved static fallback colors.

Raster scale, supersampling and optional hinting are export settings in `VisualRenderOptions`; changing them does not repeat layout or change logical dimensions. Text decoration and superscript/subscript positioning are prepared numeric geometry shared by SVG and PNG, so old renderer-specific CSS selectors are not a stable integration contract.

When a host switches one exported SVG between themes, obtain its binding collection with `selectedThemeTokens.ToSvgColorVariables().GetVariablesForSvg(exportedSvg)`. The returned collection includes the base roles and only the derived contrast inks referenced by that SVG. Evaluate it separately for each theme using the same SVG. This preserves continuous heatmap colours and readable labels without a second layout pass. `Variables` alone contains the base roles and cannot supply every scene-dependent ink.

DateTime timeline and Gantt builders select a time axis by default. An explicitly configured scale, formatter, time zone or bounds remains in effect; numeric overloads retain linear axes. Heatmap, calendar and map scales require both their model visibility settings and the host frame's legend permission even though they have no categorical legend entries. Observed zero and missing data remain separate states.

`ChartAnnotation.ShowLabel` controls the visible caption and defaults to `true`. Setting the constructor's optional `showLabel` argument to `false` preserves `Label` in accessible descriptions and semantic metadata. `WithDashboardTrendFocus` uses this separation to retain its labeled crosshair with one measured point-callout caption. Rebuild compiled consumers for v2, including callers of the annotation constructor; existing six-argument source calls remain valid.

Replace `ChartForgeX.Core.ChartRadarForm` and `ChartForgeX.VisualBlocks.MetricCardSparklineStyle` with `ChartForgeX.Core.ChartLineAreaForm`. `ChartRadarOptions.Form`, `MetricCard.MiniSparklineStyle` and `MetricCard.WithMiniSparklineStyle(...)` use this shared core enum. Add `using ChartForgeX.Core;` to metric-card source files that imported only `ChartForgeX.VisualBlocks`, and rebuild compiled consumers. Both old enum types are removed.

`Area = 0` and `Line = 1` retain their numeric values. Radar series and metric mini sparklines keep their Area defaults and existing SVG/PNG behavior.

## Shared static handoff

The target adapter flow is typed model → common renderable → immutable prepared output → SVG/PNG or semantic artifact. Compile once when producing both backends. Keep IDs, alternative text, semantic regions and family data with the artifact; the display scene and its SVG cannot replace native topology/flow/sequence data.

Sequence `ToSvg()` and `ToPng()` use the shared native scene. Their default request uses the model's width and height as minimum dimensions, retains its padding, and expands the measured content to fit. Use `sequence.Prepare(context)` for an exact fixed viewport or a custom theme/font; insufficient space fails explicitly. Export both formats from that prepared result to reuse one layout.

Compact topology dot symbols retain their standard 8-pixel logical text size when a wider resolved font cannot fit the complete symbol. A fitting text-element prefix is drawn, `topology.label-truncated` reports the loss, and the complete `Symbol` remains in semantic interchange. Use a larger authored dot when the full symbol must be visible. Explicitly smaller dots retain their proportional typography.

`sequence.ToVisualArtifact()` remains a lazy source envelope with authored dimensions and source semantics. It does not calculate preview geometry or validate a render layout. For a detached display and semantic snapshot, use `sequence.Prepare(context).ToArtifact(id, VisualArtifactKind.Sequence)`. Its envelope dimensions, node bounds, message routes and label bounds describe the prepared viewport; `chartforgex.source.width` and `chartforgex.source.height` retain the authored dimensions. Source-only envelopes do not provide preview regions until a display is prepared.

The owner fixtures exercise these adapter-shaped boundaries:

- A transparent chart with no card/header, explicit size, localized value formatting and an accessible name, exported to SVG and RGBA/PNG from one prepared result.
- A donut using the same frame/theme/size contract, including zero/missing data and a meaningful center value.
- A tiny topology and multiline sequence carrying stable entity IDs and retained semantic interchange.
- An artifact carrying SVG bytes, identity/title, alternative text and versioned interchange JSON without a Visuals or Stories concrete-type reference.
- Canonical light/dark token intake, explicit model overrides and an immutable theme snapshot.

These are owner fixtures. All chart-family producers are implemented; the Phase 2–3 integration, visual inspection and performance gates qualify their combined behavior separately. Running actual Word/Excel/PowerPoint/PDF/Visio consumers, packaging PowerShell modules and rendering full wallpapers remain consumer qualification gates. Fixture source alone does not establish that those gates passed.

The source handoff `chart.ToVisualArtifact("cpu-load")` stores the mutable chart and prepares it on demand when rendering. The detached route stores completed layout: call `chart.Prepare(context)`, then `prepared.ToArtifact(...)`, and retain the existing portable SVG/JSON handoff. Use the detached route to export several formats without repeating preparation. This example is also exercised by `PreparedVisualArtifactTests.StaticArtifactUsesPreparedOutputAndKeepsPortableHostMetadata`:

```csharp
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

var chart = Chart.Create().WithTitle("CPU load")
    .ConfigureAccessibility(a => a.WithTextAlternative(
        "CPU load", "Synthetic CPU utilization.", "pl-PL"))
    .AddLine("CPU", new[] {
        new ChartPoint(0, 20), new ChartPoint(1, 35), new ChartPoint(2, 28)
    });
var context = new VisualRenderContext(
    layout: new VisualLayoutOptions(new VisualSize(320, 200), padding: 16),
    frame: new VisualFrame(title: "CPU load", showLegend: false));
PreparedVisual prepared = chart.Prepare(context);
VisualArtifact artifact = prepared.ToArtifact("cpu-load", VisualArtifactKind.Chart);
byte[] officeVisualSvg = Encoding.UTF8.GetBytes(artifact.ToSvg());
byte[] officeVisualInterchangeJson = artifact.ToInterchangeUtf8Json();
byte[] png = artifact.ToPng();
string html = artifact.ToHtmlPage();
```

The artifact copies prepared region bounds and accessibility, and declares the prepared viewport as its natural size. Its title/accessibility/metadata remain editable host-envelope fields. Artifact SVG/HTML exports apply current host text alternatives, language and decorative state, with generated SVG IDs scoped to the artifact identity. These overrides leave the immutable prepared SVG snapshot, geometry and pixels unchanged. To change visual content or dimensions, prepare again. Replacing `NaturalSize` with dimensions different from the prepared viewport is rejected during rendering/interchange export.

Prepared topology, flow and sequence producers carry their typed semantic snapshot. Calling `prepared.ToArtifact(id, kind)` retains that snapshot automatically, with the requested artifact ID and prepared dimensions. Node/group bounds, resolved routes and label bounds describe the displayed diagram; authored options and source metadata remain available through the versioned interchange. No diagram data is inferred from scene commands.

Layered flows advance in declared step order and the selected direction. Flows with lanes use the shared swimlane layout, keeping lane bounds disjoint and their member steps contained. Explicit dense and force modes retain their separate layout policies; flow declarations no longer inherit infrastructure-node ranking or lexical ID ordering.

Topology artwork retains its aspect-ratio policy, highlight dimming and selected outline. Embedded bitmap and inline SVG artwork become immutable pixels shared by both exporters. Safe host-managed image references remain references in SVG and HTML; native raster export uses the canonical glyph fallback and reports the unresolved external resource. Rendering does not fetch URLs or read image paths. Supply embedded artwork when a self-contained SVG/PNG pair must display the same image.

Topology surface tints use premultiplied sRGB mixing, preserving the authored accent alpha in both SVG paint expressions and native raster colors. A translucent accent therefore produces a lighter tint than an opaque accent with the same RGB channels. Highlight dimming applies once after tint resolution; regenerate stored image baselines that assumed an opaque accent blend.

The typed snapshot is copied during preparation, before lazy JSON serialization. Mutating an original semantic DTO or its nested collections after `Prepare` cannot change a later artifact or interchange export. Portable JSON size/depth/collection limits apply when that interchange is requested, including `ToArtifact(...)`; they do not prevent static SVG/PNG rendering of the prepared scene. A consumer that requires native editable semantics must qualify that portable boundary separately from picture export.

When a host supplies its own envelope, pass it as the third argument to `prepared.ToArtifact(id, kind, semanticInterchange)`. The factory snapshots it through the versioned writer/reader, requires matching ID/kind and any supplied dimensions, and returns an independent envelope on each read. The host is responsible for matching those semantics to its visual. An ordinary chart without supplied diagram semantics emits family `None` rather than fabricated diagram nodes or edges.

`PreparedVisualArtifactTests.NativeDiagramSemanticsSurviveCallerAndReaderMutation` exercises explicit envelope capture and independent caller/reader mutation. `PreparedTopologyTests`, `PreparedSequenceTests` and the native diagram fixtures cover producer-owned prepared geometry and semantics. Use the automatically retained envelope when Office placement needs the prepared coordinate system; explicitly supplying an authored source envelope preserves the coordinates in that envelope. Native Office projections still require consumer qualification against the retained bounds, routes, groups and fidelity diagnostics.

## PowerBGInfo: preserve the wallpaper engine

`BgInfoRasterImage.cs` uses ImageComposition for the main image, text measurement/drawing, image placement, resizing, RGBA handoff and file encoding. Adding only a VisualCanvas package reference cannot migrate it. Reference Visuals for composition and retain shared core `ChartColor`, text/font/measurement and pixel types.

Migrate the generic engine calls together: transparent/opaque surface creation and image loading; positioned images with fit/opacity; styled text and matching measurements; resize and RGBA export; still encoding with explicit JPEG background/quality. Preserve font weight, italic, underline/strikethrough, baseline and case rather than flattening text to plain strings. Keep extension dispatch such as `.jpe`/`.jfif` and `.pnm` in the consumer boundary or shared codec mapping as appropriate.

Keep PowerBGInfo's hero template, left/center/right lane policy, synthetic machine data and wallpaper placement in PowerBGInfo. Visuals supplies measured layers, hero text runs, badges/images, icons, key/value content, text fitting and safe placement. Preserve CenterRight, ContrastBox, MiniCharts, Raised3D and RaisedSections examples, including per-tile dimensions, independent sibling sizes, offsets, narrow-lane shrink/non-overlap, alpha and opt-in raised/glass/outline effects. Flat default charts do not remove these chosen wallpaper effects.

Map all consumed chart families: bar/horizontal bar; line/area/sparkline; gauge/circle/progress ring/bullet; numeric radial bar/column; pie/donut; progress; pictorial. Preserve formatter and font-role overrides, legend/point-legend/data-label settings, min/max/target/ranges, center/status labels, palette, smoothing, thickness/radius/columns and supersampling. Dense trends retain `ChartResolutionPolicy.Trend()` and provenance; categorical and short series remain exact.

These specialized models have native producers. Keep the caller's declared gauge/circle bounds and raw values; clamping the visible progress does not replace source values or formatted text. Bullet rows share one displayed domain while retaining their declared row bounds, targets and ranges. An explicit `WithValueFormat(...)` or value formatter governs values, targets and generated numeric ticks unless an axis-specific formatter overrides those ticks; the default still uses grouped value labels and compact ticks. Pie/donut aggregation uses `MaximumPieSlices` independently of theme and retains contributing indexes for `Other`. Zero pie values remain legend categories; zero polar-area values retain their angular slot. Donut center text, progress handles and pictorial partial fills remain explicit options. Custom pictorial paths become numeric geometry rendered by both backends; the legacy PNG fallback shape does not replace an accepted custom contour.

Check the remaining plot dimensions before preparing a nested chart. `BgInfoChartRenderer.Render` currently clamps the space after host padding and title/value text to one pixel, then supplies eight pixels of chart padding on every edge. A fixed `VisualLayoutOptions` request requires a positive interior; widths or heights of sixteen pixels or less cannot retain that padding. Keep the requested outer wallpaper dimensions and choose an explicit consumer policy: skip an exhausted plot, or reduce its internal padding to fit. Do not silently enlarge the chart to satisfy the layout constraint. Include a short tile with a tall title in consumer qualification.

Topology overlays remain fixed-size transparent outputs, with viewport fit, groups, labels/status badges and chosen layout/style. The 560×310 bottom-right example with offset 34 must composite without silent content-size growth. Required wallpaper qualification includes 2560×1080 and 4K, detailed/light/dark backgrounds, direct RGBA composition and final JPEG flattening.

No menu, selection-checkbox, navigation-arrow or action-button use was observed in these wallpaper routes. Progress handles are currently explicit configurable marks and default on: retain or deliberately migrate that style option rather than deleting it as dashboard chrome.

## ImagePlayground: migrate types and the module together

The ChartForgeX integration belongs primarily to `ImagePlayground.PowerShell`; the base imaging library need not gain ChartForgeX references. Assign chart/diagram cmdlets to core, canvas/blocks/watermarks to Visuals and story/motion/animated-topology operations to Stories. Keep optional TreeSitter tokenization outside ChartForgeX: `ImagePlayground.Syntax.TreeSitter` follows the story tokenizer contract into Stories.

`New-ImageStory` produces VisualStory; `New-ImageVisualStory` currently produces VisualGrid plus VisualMotionTimeline. Preserve both operator workflows while sharing timing/export ownership. `New-ImageConsoleStory` retains terminal tabs, commands, output, tables, pauses, playback and transcript. Route `New-ImageTopology` GIF/APNG output through Stories; static topology and actual HTML controls retain their own owners.

Topology SVG motion uses the Stories `TopologyMotionSvgAdapter` over native prepared routes and node positions. It adds animation to the exported SVG without changing the static prepared scene; raster motion selects a sample from the same route plan. `NativeDiagramExportTests.TopologyMotionSamplesNativeResolvedRouteWithoutChangingBaseSceneOrSourceOptions` protects route sampling. `NativeTopologyMotionSizingTests` verifies that GIF/APNG and animated HTML share the ordinary natural-size policy, preserve an explicitly fitted viewport and leave source models/options unchanged. Add Stories for the adapter and GIF/APNG output. Owner fixtures and the packed Stories probe qualify the extracted owner; ImagePlayground still requires installed-module execution in both PowerShell runtimes.

Package moves affect compiled parameter/output types, enums, type accelerators, assembly load context, binary bundling, namespace imports, generated help and examples. Update explicit-source, sibling-source and NuGet modes coherently. Qualify installed module imports and moved-type parameter binding in Windows PowerShell 5.1 and PowerShell 7, with no neighboring source projects available.

Replace the optional-concrete-type dispatch in `ConvertTo-ImageVisualArtifact` with package-owned producers/common contracts. Preserve these portable properties exactly or provide an explicit versioned migration: `OfficeVisualSvg`, `OfficeVisualInterchangeJson`, `OfficeVisualInterchangeSchema`, `OfficeVisualInterchangeVersion`, `OfficeVisualKind`, `OfficeVisualId`, `OfficeVisualTitle`, `OfficeVisualAlternativeText`. They are an assembly-load-context-safe handoff, not redundant copies of model objects.

Apply ordered watermark decorators from Visuals before static export; core render options no longer carry VisualWatermark. Preserve DPI, topology preset/diagnostic overlays, one artifact per path and pass-through behavior. Checked list and completed timeline entries are semantic completion data: retain accessible read-only markers. Remove only decorative static controls, preserving real interaction in HTML adapters.

## OfficeIMO: preserve pictures and editable semantics

Keep the base adapter dependent on the core common artifact and versioned interchange. Optional composition/story results arrive as common static artifacts or optional thin extensions. Office chart users should not acquire animation code through an overload.

Retain `ToOfficeVisual`, portable `OfficeVisualSource`, `ToOfficeDrawing`, Word/Excel/PowerPoint/PDF placement and `ToOfficeVisio`. Preserve exact resolved dimensions and pixel-to-point/DPI behavior, alpha, SVG and PNG fallback payloads, alternative text/decorative state and fidelity reports. Native Visio projections must keep topology/flow/sequence nodes, edges, groups, routes and semantic metadata; a screenshot is not an equivalent migration.

Existing fixtures cover SVG fidelity reject/rasterize/preserve policy, text/image watermarks, static tables, repeated placement, accessibility and exported document content. Reuse those observable contracts against packed v2 owner artifacts. Watermark application becomes a Visuals decoration step; native Visio must continue reporting presentation that it cannot project. Keep JSON ingestion across assembly load contexts; do not replace it with casts between consumer-local copies of moved types.

## Private adapters: one host theme mapping

The private HTML adapter's host owns report cards, menus, selection controls, navigation and actions. Keep the adapter core-focused for charts/diagrams, scoped color variables, artifact embedding and interaction wiring. Typed canvas/block/story conveniences may require optional Visuals/Stories adapters; their package identities remain a later type-ledger decision. A common static artifact can avoid optional concrete overloads where sufficient.

Migrate `ReportChartContext.CreateChart` once: transparent no-card/no-header plots, explicit mark backdrop, flat marks, token roles, culture/date labels, accessible descriptions and status category mappings. Preserve scoped SVG IDs and nested light/dark islands. Live color binding does not promise layout reflow when font metrics change; static PNG resolves explicit literal colors.

The private reporting consumer keeps domain data, report layout, category keys and localized labels. Its chart factory/theme mapping changes in the adapter, without per-report palette copies. Preserve severity/outcome/operational-state distinctions, unknown/not-observable/not-evaluated treatments, null versus zero, category and legend order, time zone labels and interval semantics. Qualify grouped availability timelines, sparse health matrices, overlapping/open jobs, hour/weekday/calendar heatmaps and replication/documentation topology in compact/wide and light/dark output.

The private reporting consumer's local dependency overrides reference unavailable development snapshots. A later consumer task must restore a reproducible dependency closure before claiming build proof; nominal package pins do not establish that newer report APIs are published.

## Canonical color import

HtmlForgeX owns the values; ChartForgeX owns its generic theme contract. The imported input is **the generated chart palette**, not the full owner token schema:

| Evidence | Pinned identity |
| --- | --- |
| Owner source | `HtmlForgeX/Resources/Tokens/evotec.tokens.json`, token schema version `1.1.0` |
| Generated export | `Generated/Tokens/ChartForgeX/evotec.chartforgex.tokens.json` |
| Generator / contract | `Tools/HfxTokens/Generator/DesignTokenTargetWriters.cs`, `Build/Build-HfxTokens.ps1`, `Docs/DESIGN_TOKENS.md` |

The export has `name`, `decision`, `source`, `notes`, `light`, `dark`; it has no numeric schema-version field. Each mode supplies surface page/card/cardAlt/line/lineStrong, text primary/secondary/muted, chrome background/text/textMuted/accent, accent base/soft, five severity fill/ink pairs, outcome pass/neutral pairs, state maintenance pair, six ordered series colors, five sequential steps and diverging negative-three/neutral/positive-three. Values are resolved hex colors. Series colors carry category identity, not status.

The full owner source defines aliases and treatments omitted from that palette export: fail uses severity when known, otherwise neutral; notEvaluated is neutral plus hatching; couldNotEvaluate is neutral plus dashed outline; up→pass, degraded→medium, down→critical, recovering→low, unknown/notObservable→neutral plus hatching. Preserve separate semantic keys and legend labels even when colors match. A shared presentation model does not collapse these vocabularies into one enum.

The export also omits typography, spacing, radius, elevation, motion and accessibility overrides. `VisualTheme` supplies the chart geometry and typography roles described in this guide while retaining the canonical palette. Expanding the palette export belongs to its token owner. Do not import the full owner source with a parser expecting the generated chart shape.

Color intake alone does not establish universal status-treatment rendering. Hatch patterns, dashed-outline outcomes, forced-colors behavior and status legend treatment require their own family and host fixtures. Matching neutral colors alone is insufficient proof that not-evaluated, could-not-evaluate, unknown and not-observable remain distinguishable.

## Qualification sequence and open gates

- [x] Compile and inspect the Phase 1 shared-frame/theme/direct-scene and adapter-shaped owner fixtures. The [executable gallery producer](../../ChartForgeX.Examples/V2Examples.cs), [diagram fixtures](../../ChartForgeX.Examples/V2Examples.Diagrams.cs), [review gallery](../../Website/static/examples/generated/catalog.html) and [artifact handoff tests](../../ChartForgeX.Tests/PreparedVisualArtifactTests.cs) provide the implemented examples. This qualifies the owner proof, not downstream consumers.
- [x] Close the [integrated public type/member ledger](api-ledger.md): 773 public types and 7,678 records across the six existing runtime assemblies. Its planned API fates guide later removals and package extraction; they do not claim those migrations are implemented.
- [x] Complete the Phase 1 representative performance comparison: all 30 paired workloads remain within 10% elapsed/allocation, with 756 retained samples and no failures. Earlier flags were investigated before qualifying the stable runtime. Larger redesigned SVG/PNG payloads remain an explicit cost in the [reassessment](architecture.md#phase-1-reassessment).
- [x] Qualify the integrated Phase 2–3 core producers with framework/package/AOT checks, observed SVG/native PNG and representative performance; see the [reassessment](architecture.md#phases-2-3-reassessment) for scope and limits.
- [x] Qualify the core-focused private HTML adapter and OfficeIMO candidates from isolated local packages, preserving artifact/interchange contracts. Normal feed-only release builds remain separate gates.
- [x] Extract the peer Visuals and Stories owners, preserving static compositions, factual blocks, completed story output, transcripts and animation exports.
- [x] Qualify PowerBGInfo's installed candidate on PowerShell 5.1/7, including ultrawide/4K wallpapers, retained composition styles, mini charts and topology overlays.
- [x] Qualify ImagePlayground's managed-imaging candidate through installed PowerShell 5.1/7 static and animated workflows. Each host passes all 60 maintained chart contracts; native image pipelines preserve metadata, frame timings and play count, enforce resource limits and retain the source on cancellation. Browser inspection covers the actual generated composition, topology, motion and terminal artifacts.
- [x] Restore the private reporting candidate's reproducible local dependency closure and observe representative reports. The coordinated locked release graph remains a feed-only qualification gate.
- [x] Qualify all eight packed libraries and their dependency/resource boundaries. Isolated core-only, core+Visuals and core+Stories probes compile all four frameworks and execute on net472, net8.0 and net10.0; adapter probes execute on net8.0 and net10.0. These owner probes do not establish downstream consumer execution.
- [x] Prepare the unsigned eight-package release archives and validate matching source/archive API payloads.
- [ ] With separate release authority, publish/verify owner packages, then repin and release consumers in dependency order.

Do not publish consumers against a locally packed or unpublished owner candidate. Local-source proof is useful and must be labeled separately. No compatibility probes, fallback copies of rendering logic or temporary project references should conceal package publication lag.

The consumer candidates exercise real output boundaries. PowerBGInfo preserves module-visible authoring types as well as the pixels in its installed wallpaper exports. OfficeIMO saves and reopens Word, Excel, PowerPoint, PDF and Visio output; native topology projection retains resolved routes and labels, while recomputed flow/sequence layout reports its fidelity limits. A saved document or native SVG preview does not establish appearance in every Office application.

ImagePlayground's installed qualification uses its unsigned managed-imaging module with the integrated, locally packed ChartForgeX assemblies in each framework lane. This replaces the earlier imaging-security decision with observed PowerShell 5.1 and 7 compatibility. Its normal release build still requires containing public versions of ChartForgeX, OfficeIMO.Core and CodeGlyphX, verified from the feed; a version number shared with an earlier package does not establish that the required implementation is present.

The private HTML adapter separates its core, Visuals and Stories hosts and consumes the matching producer packages. The reporting candidate uses those shared hosts for compact/wide, light/dark assessment and monitoring reports. Bounded summaries retain complete views, omitted-content notes and detail links. Its local fixture graph proves these paths without claiming that an older committed release lock or an unpublished package is ready for distribution.
## Financial mark options

Existing `AddCandlestick` and `AddOhlc` callers retain their default colors, body alpha, widths and legend appearance. Configure `ChartSeries.Financial.Rising` and `.Falling` through `ConfigureFinancial` when body/wick or OHLC strokes need independent colors, alpha, widths or dash patterns and phase. Use `Rising.FillOpacity = 0` for hollow rising candles. Observation colors still override part colors; wick settings inherit the item stroke and item widths inherit an explicit series width. There is no new add-overload family or required caller migration. See [financial marks](api-conventions.md#financial-marks) for the inheritance and validation rules.

`DashPattern` and `DashOffset` are available on the existing financial item and candlestick wick options. Patterns are copied on assignment; null resets inheritance and an empty wick pattern means solid. Wick pattern and phase inherit independently. Each emitted stem, tick or wick segment starts its own phase; a body outline keeps continuous phase around its closed contour. Prepared exports capture these values and scale them in financial legends. Unconfigured marks retain solid strokes.

Observation regions cover the painted union, including the minimum-height doji body and stroke extents. Consumers using `PreparedVisual.Regions` for hit testing or label obstacles should use these bounds; source financial observations and axis domains are unchanged. HTML interaction continues to own pointer targets and controls. Trading sessions and datum style callbacks remain outside these options.

The HTML adapter shows Open, High, Low and Close rows for financial targets in ordinary and pinned tooltips, including zero prices. Shared-X mode retains the single-target fallback for financial observations. Hosts consuming raw `data-cfx-y` still receive the opening tuple member; rendered readouts use the explicit financial facts rather than duplicating that value as a generic Y row.
