---
description: "Follow the public ChartForgeX contracts for preparation, configuration, data transactions, identity, annotations, themes and export ownership."
---

# V2 public API conventions

These rules govern the shared prepared-rendering contracts and their model entry points. All chart families and chart grids compile through the native scene, alongside topology, flow and sequence diagrams. Model convenience exports resolve a request and use the same preparation route. Static composition belongs to `ChartForgeX.Visuals`; animation belongs to `ChartForgeX.Stories`. The [API ledger](api-ledger.md) records naming and ownership changes; this document does not claim that every existing public member conforms already.

## Operation names

| Operation | Meaning | Example / rule |
| --- | --- | --- |
| `With*` | Set a scalar value or configuration object on a mutable model builder and return that builder. Do not add data. | `chart.WithTitle("Capacity")`, `chart.WithBarVisualStyle(style)` |
| `Add*` | Add model data or a semantic child and return the builder. Do not silently replace configuration. | `chart.AddLine("Used", points)` |
| `Configure*` | Configure a sub-object through a typed callback, preserving the operation's ownership rules, then return the builder. | `chart.ConfigureXAxis(axis => axis.Minimum = 0)` |
| `Apply*Changes` | Commit a bounded keyed observation transaction while retaining surviving observations' presentation. | `series.ApplyPointChanges(changes, maximumObservations: 500)` |
| `Prepare` | Validate and compile a model with an explicit render context; return a detached prepared result. | `chart.Prepare(context)` |
| `To*` | Return an in-memory representation without writing a file or changing the model. | `prepared.ToSvg()`, `prepared.ToPng()`, `prepared.ToRgba()` |
| `Save*` | Write to an explicitly supplied path or stream. Keep file ownership and format options visible. | File-saving helpers are outside the Phase 1 prepared contract. |
| Constructors and named factories | Create immutable requests, snapshots or presets. They are deliberate exceptions to builder prefixes. | `new VisualFrame(...)`, `VisualTheme.Graphite()`, `VisualTheme.FromJson(json)` |
| `Resolve` | Select a validated immutable view of an existing value. | `theme.Resolve(VisualThemeMode.Dark)` |

Use domain names after prefixes. Avoid interchangeable `Set`, `Use`, `Build`, `Render`, `Options`, `Configure` and `With` methods for the same operation. A meaningful operation such as preparing, importing or exporting is not forced into a configuration prefix.

Typed `Action<T>` configuration callbacks use `Configure*`, including callbacks that select a row, step or point by index or ID. Value and object setters keep `With*`; for example, `WithLineVisualStyle(style)` supplies a style while `ConfigureLineVisualStyle(style => ...)` configures it. The immutable `ChartColorScale.WithLabels(string?...)` copy operation retains its value arguments and name.

Configuration callbacks edit an isolated candidate and install a separate snapshot after success. Exceptions leave the owner unchanged, and a reference retained before or during a callback cannot mutate the newly installed value. This rule includes axes, family options, labels, accessibility, text overrides, grid pagination and optional canvas/table configuration. Theme callbacks produce an immutable paired theme through `VisualThemeBuilder`.

Immutable render requests use constructors and read-only properties. They do not expose `Add*` or `Configure*` mutation. The reviewed `VisualTheme.WithTypography(VisualTypography)` exception returns an independent paired theme with a replacement typography scale, preserving both palettes, geometry and effects without editing the source. Other immutable `With*` APIs need the same explicit copy-return contract and review. Existing mutable `FontSpec`, `VisualDesignTokens`, status-token and accessibility objects enter this boundary through snapshots or leave it through independent copies.

HTML interaction presentation stays in the optional adapter. `HtmlChartInteractionOptions.Highlight` and `HtmlInteractiveDashboardOptions.Highlight` are getter-owned mutable `HtmlChartHighlightOptions`; their four getter-owned `HtmlChartHighlightStyle` groups contain nullable `ChartColor`/numeric overrides. Dashboard rendering copies values into independent child groups. The explicit `RootBranch` mode uses immutable Core hierarchy membership only for Sunburst node targets; it does not add transient paint settings to Core or introduce a second interaction model. See [HTML Sunburst branch emphasis](migration.md#html-sunburst-branch-emphasis) for composition, defaults and native-export boundaries.

## Shared requests and ownership

| Contract | Ownership and behavior |
| --- | --- |
| `IVisualRenderable` | A core static compilation boundary returning `PreparedVisual`. Optional producers implement the same boundary without making core reference their concrete types. |
| `VisualSize` | Finite positive logical width and height. A default zero-valued struct is rejected when used in a layout. |
| `VisualTypography` tile sizes and `VisualCanvasInfoTileTextOptions` | Theme defaults provide tile label/value/detail sizes; optional per-role sizes override a role, while `TextScale` multiplies the effective sizes. `VisualCanvasStyleOptions.TileTextScale` supplies the canvas default. `AddInfoTileWithTextOptions` keeps the legacy `AddInfoTile` overloads unambiguous and binary-compatible. |
| `VisualLayoutOptions` | A resolved fixed size and finite non-negative outer padding. Padding must leave a positive interior. |
| `VisualFrame` | Title, subtitle, optional measured legend title, visibility/position and density budgets; separate card/content surfaces and optional transparent canvas. |
| `VisualTheme` | Immutable light/dark colors, typography, geometry and card-shadow settings. Card and mark radii are independent. Graphite is a preset. |
| `VisualRenderContext` | A complete layout/theme/mode/frame/font request. `Font` returns a copy. |
| `PreparedVisual` | Detached compiled scene, size, diagnostics, descriptive regions and accessible metadata. Export does not repeat layout. |
| `VisualRenderOptions` | Raster scale, supersampling and working pixel budget. These do not change logical layout. |
| `VisualDiagnostic` | A stable code, human explanation and the existing shared `Diagnostics.VisualDiagnosticSeverity`. |
| `VisualSemanticRegion` | Stable ID, role, descriptive rectangular extent and optional text. It is not an exact hit-test shape. |
| `ChartNode` / `ChartFlowLink` | Immutable authored relationship identities and finite nonnegative flow weights. Chord can also use an independent nullable signed color value. Chart families validate their graph and weight constraints before adding a series. |
| `ChartSeries.WithNodeState(id, state)` | Mutable semantic styling keyed by an existing authored node ID, scoped to that series. `NodeStates` exposes a read-only view; preparation snapshots the resulting paint. |
| `ChartFlowStyle` / `ChartSeries.WithFlowStyle(id, style)` | Immutable fill, opacity, pattern and outline overrides for an existing Sankey or Chord flow ID. `FlowStyles` is read-only; null removes an override. |
| `ChartSeries.WithFlowState(id, state)` | Semantic state for an existing Sankey or Chord flow ID. `FlowStates` is read-only; null restores source-node/series inheritance and `None` suppresses it. |
| `ChartOptions.Chord` | Typed circular span/start, node geometry, opacity, direction, labels, immutable complete display-ID permutations and ribbon tension over the canonical weighted-flow facts. Source, semantic and paint order remain authored. See [weighted chord charts](../chord.md). |
| `ChartOptions.Sankey` | Alignment, ordering, node geometry and flow paint options. `ConfigureSankey` configures and installs an independent snapshot, then returns the chart. |
| `ChartSankeyFlowColorMode.Gradient` / `ChartSankeyLabelStyle` | Endpoint gradient paint and Backdrop/Plain/Outlined caption paint reuse the native scene. An explicit flow fill stays solid; quantitative measurements remain independent. Absent configuration retains Source and Backdrop defaults. |
| `ChartOptions.Bubble` | Size-domain, radius endpoints, reversal and `SizeDimension` name/format/key settings shared by every bubble series. `ConfigureBubble` configures and installs an independent snapshot, including its owned size presentation, then returns the chart. |
| `ChartGaugeValue` / `ChartGaugeTarget` | Immutable measured value, declared scale and ordered targets. A target contains its numeric value and optional label; the reading copies its supplied targets. |
| `ChartOptions.Gauge` | Form, orientation, circular span, ticks, caption and semantic bands. `ConfigureGauge` configures and installs an independent snapshot, then returns the chart. |

Keep the numeric scene and painter implementation internal. Public signatures use core-owned types and do not reference Visuals, Stories, browser hosts, their encoders or their policy objects. Static rendering is script-free. Optional animation, interaction and composition have the ownership described in the architecture and [consumer migration guide](migration.md).

`ChartColor` is the color value type. Palette collections contain that type; do not introduce another RGB wrapper for the prepared pipeline. Diagnostic severity reuses the existing enum. Finding severity, test outcome and operational state remain distinct domain vocabularies even where their presentation uses the same colors.

## One theme and an explicit mode

Charts, chart grids, topology, sequence diagrams, visual blocks, canvases and stories use `VisualTheme`. It is an immutable paired theme containing light/dark palettes, typography, geometry and effects. `ThemeMode` selects the palette; colors never select a different renderer. Model defaults feed convenience exports. An explicit `VisualRenderContext` supplies the complete theme, mode and font for `Prepare`.

```csharp
var theme = VisualTheme.Graphite().Configure(builder => {
    builder.Light.Accent = ChartColor.FromHex("#2463eb");
    builder.Dark.Accent = ChartColor.FromHex("#78aaff");
    builder.Typography = builder.Typography.WithFamily("Calibri, Carlito, sans-serif");
    builder.CardRadius = 8;
});
chart.WithTheme(theme, VisualThemeMode.Dark);
```

`Configure` snapshots its builder after success; retaining or editing that builder later cannot change the theme. A failed callback returns no replacement. `VisualDesignTokens` describes a mode's editable colors inside this builder, not a second typography/layout model. Canvas backdrop treatments, terminal chrome and syntax highlighting remain local style options. Frame visibility belongs to the model/frame, independently of the palette.

Use `theme.ToJson()` and `VisualTheme.FromJson(json)` for the versioned full-theme format. Use `VisualTheme.FromPaletteJson(json)` for the generated HtmlForgeX color document. These formats have different purposes and validation.

Topology nodes and edges can select the same categorical palette with `PaletteIndex`. Indexes are zero-based, cannot be negative, and wrap to the active palette length during preparation. Explicit node `Color` or resolved icon color takes precedence; explicit node `BackgroundColor` remains its surface. Edge `Color` takes precedence, and muted edges keep their neutral treatment. A null index preserves status-based accents, while status badges always retain health colors. Clone, prepared scene metadata and visual-artifact interchange preserve the optional indexes. Reprepare a retained model after changing its theme; an existing prepared result keeps its original colors and palette bindings.

Chart grids share their own theme, mode and font across all panels by default. Use `WithPanelThemes()` for a theme comparison or a report containing independently styled charts:

```csharp
var grid = ChartGrid.Create()
    .WithTheme(VisualTheme.Report())
    .WithPanelThemes()
    .Add(sales.WithTheme(VisualTheme.Executive()))
    .Add(readership.WithTheme(VisualTheme.Editorial(), VisualThemeMode.Dark));
```

With panel themes enabled, every child uses its chart's `Theme`, `ThemeMode`, typography and native font-file request. The grid context controls the outer frame, layout, visibility permissions and legend limits. An explicit context theme or font therefore applies to the outer frame; call `WithPanelThemes(false)` to impose that presentation on every panel. A context `LegendStyle` remains an explicit common legend override, and chart text-style overrides remain authoritative. Pagination copies this choice. The grid never infers its presentation from the first child.

SVG color variables follow the same boundary. Shared panels use `ChartGrid.SvgColorVariables`. Independent panels retain their own chart mappings, or literal colors when none are authored, so an outer CSS token cannot replace a child's palette merely because their RGB values match. These mappings are captured with the prepared scene and apply to SVG and HTML; native PNG retains the resolved colors.

## Stable series handles and observations

See [chart combinations, orientation and axes](chart-combinations.md) before combining families or selecting a horizontal/radial layout.

Retain a `ChartSeries` when its configuration or data will change. Construct it with a `ChartSeriesId`, configure it, then attach it through `AddSeries`. Display names may repeat or change; reordering does not change identity.

```csharp
var completed = ChartSeries.CreateLine(new ChartSeriesId("completed-orders"), "Completed",
    ChartPoints.FromValues(28, 34, 41, 48, 32));
completed.ConfigureMarkers(markers => { markers.Radius = 4; });
chart.AddSeries(completed);
completed.WithName("Completed orders");
```

`Series` is a read-only view of paint order. Use `RemoveSeries`, `MoveSeries` and `ClearSeries` for membership changes; these operations validate ownership. A series can belong to one chart at a time, and IDs must be unique there. Fluent `Add*` conveniences allocate deterministic local IDs. Local IDs remain stable in that chart and are excluded from cross-chart synchronization; explicit IDs identify equivalent series across charts. The `cfx:auto:` prefix is reserved. Automatically identified series remain owned by their original chart; construct a series with an explicit ID when it must move between charts.

Line and band annotations accept an immutable `ChartAnnotationId` at construction or through the four typed `AddHorizontalLine`, `AddVerticalLine`, `AddHorizontalBand` and `AddVerticalBand` overloads:

```csharp
chart.AddHorizontalLine(new ChartAnnotationId("threshold.warning"), 15, "Limit");
```

`Annotations` is a stable `IReadOnlyList<ChartAnnotation>` in paint order. Use `AddAnnotation`, `ReplaceAnnotation`, `RemoveAnnotation` and `ClearAnnotations` to edit membership. The typed `Add*` conveniences use the same validation. An annotation instance can occur once in a chart; explicit IDs are trimmed, case-sensitive and unique within that chart, independently of kind, caption or coordinates. Invalid membership edits leave the collection unchanged.

`ReplaceAnnotation(replacement)` finds an existing annotation by the replacement's explicit ID. `ReplaceAnnotation(existing, replacement)` addresses an attached instance, including an anonymous guide. Both preserve paint position and authored identity; remove and add to change identity. `RemoveAnnotation` accepts an explicit ID or an attached instance, and `ClearAnnotations` also clears owned focus-guide tracking. A retained read-only collection view reflects successful edits.

Constructors and overloads without IDs create anonymous annotations with source-local ordinal identities. Statistical and focus helpers keep using anonymous guides. Geometry and identity are fixed; optional `StrokeStyle` remains mutable authoring state. Preparation captures independent geometry, style and semantic output. Coordinate-system compatibility is checked during preparation, so an annotation can be authored before its required series or axes are configured.

`AddNote`, `AddLineSegment` and `AddParallelChannel` add drawings at exact Cartesian source coordinates:

```csharp
chart.AddNote(new ChartPoint(3, 40), "Review", id: new ChartAnnotationId("review"));
chart.AddLineSegment(new ChartPoint(2, 20), new ChartPoint(8, 30), "Plan",
    id: new ChartAnnotationId("plan"));
chart.AddParallelChannel(new ChartPoint(2, 65), new ChartPoint(8, 75), 10, "Target",
    id: new ChartAnnotationId("target"));
```

The `ChartAnnotation.Note`, `LineSegment` and `ParallelChannel` factories create the same authored objects for `AddAnnotation` or `ReplaceAnnotation`. `SourceStart`, `SourceEnd`, `ChannelOffset` and `SourceYAxis` retain their source facts. Use the same ID when replacing a drawing. The optional `yAxis` selects primary Y or an existing secondary series' Y domain. Source drawings require a vertical Cartesian chart; they follow its axes and viewport without expanding the data domain. Notes outside the plot and line/channel geometry outside the clip retain their descriptive facts. A caption follows its source anchor or line midpoint when that anchor is in the visible plot, using the shared measured label placement.

A channel offset is signed source Y distance, not perpendicular screen distance. Both boundaries map through the selected axis, so logarithmic boundaries need positive values and need not be parallel on screen. A zero channel offset and coincident line endpoints reject. SVG and native PNG consume the same prepared geometry; later annotation, axis or data edits do not change an earlier preparation. Source drawing APIs provide the native model and export contract; host editing controls use the optional interaction adapter or the consuming editor.

An explicit series ID also selects its categorical palette slot deterministically. Renaming, reordering, rebuilding the same ID, or adding/removing another series does not change that slot. A finite palette can reuse colors for different IDs; use `WithColor` when distinct authored colors are required. Explicit series/point colors and semantic states retain precedence. Fluent `Add*` series keep their ordinal palette behavior. Pie, donut and other part-to-whole categories retain their separate per-observation palette.

Specialized observations remain in their typed collections: `Bubbles`, `FinancialValues`, `Ranges`, `Intervals`, `ErrorBars`, `BoxPlots`, `Dumbbells`, `DifferencePoints`, `GanttItems`, `GaugeValues`, `BulletValues`, `StateTimelineSegments`, `GanttLaneItems`, `MapPoints`, `CalendarItems`, `RegionValues` and categorical `HeatmapCells`. Layered radial series expose `RadialLayers`. Each item represents one complete observation or layer. `Points` contains genuine numeric XY points and is an empty read-only view for these specialized families. Renderer projections are internal and do not define public source ordinals. Gauge and bullet series accept at most one value; empty replacements produce their no-data state.

```csharp
var samples = ChartSeries.CreateBubble(new ChartSeriesId("batch-samples"), "Batches",
    new[] { new ChartBubble(10, 24, 8), new ChartBubble(20, 31, 12) });
samples.WithObservationIds(new[] {
    new ChartObservationId("batch-a"), new ChartObservationId("batch-b")
});
chart.AddSeries(samples);
samples.UpdateBubble(0, new ChartBubble(10, 26, 8));
```

`WithObservationIds(...)` assigns one unique ID per authored source observation
in a single validated replacement. Decimated and derived numeric series use
their complete original input order. Invalid counts, duplicate IDs or a failing
enumeration leave the installed identities intact. Use `WithObservationId(...)`
to assign or replace one ID independently.

`Update*` replaces one complete item and preserves its ordinal, explicit observation ID and point styling. `Replace*` validates and snapshots the complete sequence before installation, then clears per-observation IDs, labels and paint overrides; series configuration remains. An invalid replacement leaves existing data and metadata intact. Empty replacement creates a no-data series. Row selectors run once during ingestion and do not retain application records or delegates.

Ordinary numeric `Points` keeps its `IList<ChartPoint>` surface. Replacing an item by index preserves its identity and overrides. Insert/remove operations shift observation IDs, labels, colors, patterns, text styles and slice offsets with the existing observations; a new inserted observation starts without those overrides. `Clear` clears both data and observation metadata, and `SourcePointCount` follows the current ordinary point count. A retained numeric list becomes read-only if its series switches to typed waterfall data.

For keyed live updates, use `ApplyPointChanges` with the shared `ChartObservationChange<ChartPoint>` payload after assigning an explicit ID to every observation:

```csharp
completed.WithObservationIds(new[] {
    new ChartObservationId("mon"), new ChartObservationId("tue"),
    new ChartObservationId("wed"), new ChartObservationId("thu"), new ChartObservationId("fri")
});
completed.ApplyPointChanges(new[] {
    ChartObservationChange<ChartPoint>.Remove(new ChartObservationId("mon")),
    ChartObservationChange<ChartPoint>.Replace(new ChartObservationId("fri"), new ChartPoint(5, 39)),
    ChartObservationChange<ChartPoint>.Append(new ChartObservationId("sat"), new ChartPoint(6, 45))
}, maximumObservations: 5);
```

Append requires a new key; replacement and removal require existing keys. A key occurs at most once in a batch. Surviving values retain their source order, IDs, labels, colors, patterns, text styles and slice offsets. The optional rolling limit removes the oldest surviving observations in source order. Enumeration, key, capacity and cancellation failures leave installed data unchanged; prepared results remain detached. The default `maximumChanges` is 4,096, with rejection after at most one additional input item. Hosts can set a deliberate capacity, coalesce or split incoming work and pass a cancellation token. They own collection cadence and asynchronous queues, and serialize access to the mutable model.

This operation supports the eleven point-mapped families: Line, StepLine, Area, StepArea, StackedArea, Scatter, Bar, HorizontalBar, RadialBar, RadialColumn and Lollipop. The [compiled rolling-window recipe](../../ChartForgeX.Examples/Recipes/KeyedPointChangesRecipe.cs) shows the complete model workflow. Browser hosts prepare a new section and call the existing controller's `update`; surviving explicit identities retain selection and legend state, while accepted replacement clears transient screen-space brush and readout state. A canceled batch leaves the committed model and browser scene intact.

Typed Bubble, Candlestick/OHLC, RangeBand/RangeArea and DifferenceArea observations use the same payload and bounded transaction policy through `ApplyBubbleChanges`, `ApplyFinancialChanges`, `ApplyRangeChanges` and `ApplyDifferenceChanges`. Supply `ChartObservationChange<T>.Append`, `Replace` or `Remove` with the corresponding complete observation. Its `Value` contains the appended or replacement value and is null for removal; `Kind` uses `ChartObservationChangeKind`. Numeric payloads retain the complete point, including segment breaks and independent color measurements. Surviving IDs retain all presentation, including materialized financial datum styles; new IDs start with family defaults. Both members of a range or difference, and all four financial prices, change atomically. Full typed replacement still clears observation metadata, while a keyed transaction remaps it by identity. Other typed families keep their existing replacement/indexed update APIs.

Decimation, histogram and regression factories expose read-only derived `Points`. Change complete source observations and rebuild the derived series rather than editing a reduced point, bin aggregate or fitted endpoint independently of its source provenance. Original-input counts, mappings and source snapshots remain consistent. Presentation overrides remain configurable.

`WithDataLabels` sets chart-level visibility. A series overrides it with `WithDataLabels` and restores inheritance with `UseChartDataLabels`. Layout and paint use the same resolved setting; when neither is explicit, each family keeps its default. This includes Chord/Sankey captions, gauge summaries, Circle captions, radial centers, Bullet captions, the Donut center and ProgressBar numeric values. Dedicated content switches such as `ShowProgressValues` can still suppress their content. Progress row category names and gauge ticks and target labels keep their separate roles.

Gantt lane item captions are visible by default. `WithDataLabels(false)` hides them at chart or series level; an explicit series setting takes precedence. Hidden captions reserve no extra bar height.

Lane factories and full replacements order items by start, end and input order. `UpdateStateTimelineSegment` and `UpdateGanttLaneItem` retain the observation index even when a changed time moves the item in the rendered order. State, category, label and detail stay with that complete item. Adjacent timeline intervals with the same state and detail may render as one run; its contributor metadata lists only the intervals actually combined. A combined run keeps derived identity; a one-interval run retains its authored observation ID.

### Shared schedule rows and physical reversal

`Timeline`, `Gantt`, `StateTimeline` and `GanttLane` can share one chart. Their original typed observations remain separate: a Timeline range occupies one source row, a Gantt source is a task, milestone or full-height marker, state intervals retain their coalesced contributors, and packed Gantt-lane items retain their own source indices and subrows. The existing lane planner reserves group and summary rows across the composition. A vertical marker overlays the shared plot and consumes no row. Empty peers retain a source row; no zero-duration item is invented for missing data.

Set `XAxis.Reversed = true` to reverse the physical time direction and `YAxis.Reversed = true` to reverse the source lane order. The flags work independently. Progress begins at the authored task start; finish-to-start arrows and open-item tips follow the reversed geometry. Raw dates, IDs, observation order, packed subrow identity and resource groups remain unchanged. Turning both flags off restores the ordinary geometry. Preparation captures the common primary X domain, axis captions and source facts once for SVG, PNG and HTML.

Mixed schedules require a primary linear or continuous time X scale; nonlinear scales and secondary value-axis sources reject before painting. `TimeZone` changes displayed instants rather than elapsed duration. Compressed `TimeSessions` remain restricted to their documented Cartesian families. A dependency joins real Gantt task/milestone sources within the composition; it does not connect arbitrary lane points or reschedule dates. Host scheduling editors and data acquisition remain outside the rendering model.

The complete [`mixed-schedule` recipe](../../ChartForgeX.Examples/Recipes/MixedScheduleRecipe.cs) supplies numeric/date, compact/wide and reversed variants through the ordinary public factories. Re-prepare after source edits and retain the previous preparation for detached exports.

Categorical `HeatmapCells` includes null masked cells in column order. Observation indices and IDs refer to those columns. `UpdateHeatmapCell(index, null)` masks one cell while preserving later cells' identities; restoring the cell restores its existing ID. Use `CreateHeatmapCategoryRow` for categorical data and the numeric row APIs for numeric data.

`MapPoints` keeps each dotted-map label, longitude, latitude, optional nonnegative `Weight` and explicit color together. Supply a point weight through the `ChartMapPoint` constructor's `weight` argument. `WithColorValue` supplies an independent nullable signed color measurement; missing color stays distinct from an observed zero. Missing weight is unweighted, while zero is an observed weight. Repeated coordinates retain separate observations. `CalendarItems` owns wall-clock dates, values and optional colors. `RegionValues` owns canonical region codes, values and optional colors for region and tile maps; their immutable geometry belongs to `series.RegionMapDefinition` or `series.TileMapDefinition`. Chart options own the common map viewport and default presentation. Geographic datasets can own independent scales, size/color dimensions and routes. Changing Cartesian axis labels does not change map observations.

RegionMap and DottedMap datasets share a geographic chart when every region definition declares the same `GeographicProjection`. GeoJSON polygon import records its selected projection. `ChartMapDefinition.WithGeographicProjection` returns an independent definition declaring the transform already applied to its paths; it does not transform them. Multiple arbitrary-coordinate RegionMaps share their authored numeric coordinate frame when none declares a geographic projection. Their union bounds (or explicit `RegionMapBounds`) define the common fit; different definition IDs do not imply different units. Mixing arbitrary regions with geographic regions, points, routes, viewports or declared geographic context layers is rejected.

`tileSeries.WithMapInset(new ChartRect(.02, .73, .48, .24))` places an independent TileMap in a multi-map scene containing at least one RegionMap or DottedMap. The finite positive rectangle must stay inside `[0,1]` and is relative to the final shared map plot after numeric keys. This composes a tile layout in the same scene; it does not register its grid to longitude/latitude. Every composed TileMap needs its own explicit inset. Standalone TileMap layout and chart-level color defaults remain unchanged; independent TileMap `ColorScale`/`ColorDimension` overrides belong to insets. The [map composition recipe](../../ChartForgeX.Examples/Recipes/MapCompositionDepthRecipe.cs) contains compact/wide arbitrary and geographic-plus-tile models. Each dataset keeps its retained series identity and paint order, `ColorScale` and `ColorDimension`; chart `MapColorScale` remains the region/tile-scale fallback. Numeric keys follow the dataset's name, format and visibility independently of its categorical entry.

`ConfigureMapPointSize` controls one dotted dataset's `WithSizeDomain`, optional `MinimumRadius`/`MaximumRadius`, `Reversed` and `SizeDimension` naming, formatting and optional numeric key. Automatic domains include all source weights, including offscreen observations. Missing weight remains unweighted; zero remains an observation. An explicit series `MarkerRadius` takes precedence. Domain clamping affects radius only, preserving raw weight and independent color facts. The size key uses the same logical radii as the points and does not claim proportional painted areas. The [geographic dataset recipe](../../ChartForgeX.Examples/Recipes/GeographicDatasetsRecipe.cs) combines independent coverage, delay and traffic measurements.

`ChartMapGeoJson.ToMapPoints` imports Point/MultiPoint features, using optional `WeightPropertyName` and `ColorValuePropertyName` for separate weight and color. `ToMapConnectors` imports LineString/MultiLineString features. Both retain raw longitude/latitude and source order; projection and polygon coordinate precision do not alter these observations. Names fall back to codes/IDs, and multipart names receive one-based suffixes. Bounds filters retain complete matching feature geometries without clipping. Points and connectors retain immutable `Feature` snapshots. `ChartMapFeature.Id` is the source ID text; `IdJson` distinguishes a numeric ID from a string ID and preserves validated numeric spelling; null means absent ID, while the text `null` preserves an explicit source null. `PropertiesJson` retains nested JSON values and case-sensitive property names; a null property means absent properties, while the text `null` preserves an explicit JSON null. No feature ID is invented from a name or fallback region code. MultiPoint observations and MultiLineString connectors share their feature snapshot; they remain distinct observations. Feature IDs are not observation IDs: assign observation IDs explicitly on the series. `WithFeature` attaches or clears a manually authored immutable snapshot without changing source measurements or identity. Metadata reaches native SVG/HTML source attributes and point legend facts as escaped inert JSON; it never supplies markup or selectors. `data-cfx-feature-id-json` is the authoritative typed ID; the optional raw `data-cfx-feature-id` convenience attribute is omitted when the source ID contains scalars unavailable to SVG markup. JSON escapes preserve those values without replacing them. Observation imports default to at most 100,000 positions or route waypoints and 4,194,304 aggregate UTF-16 label characters. Configure `MaximumObservations`, `MaximumExpandedLabelCharacters` and `MaximumExpandedFeatureCharacters` for a different output policy. Feature metadata expansion also defaults to 4,194,304 UTF-16 JSON characters and counts each imported point or line snapshot before output construction, independently of route waypoint labels. Individual feature snapshots use the existing bounded JSON parser/writer (8 Mi characters, 600,000 values, depth 64); their limits are not an encoder-memory or host-RSS guarantee. Multipart name costs are checked before label expansion, after coordinate filtering; invalid geometry still fails. Hosts retain their own input-size and trust policy. `ReplaceMapConnectors` captures an ordered detached route collection for one dotted dataset; every connector's `RoutePoints` is a read-only snapshot. Routes retain their captured waypoints when point observations change.

`ChartRegionMapItem.Value` accepts any finite signed scalar, including temperatures or balances. Duplicate region observations can cancel to an observed zero; omitting a region means no data. Explicit color scales retain their signed domain, midpoint and formatting policies. Without an explicit scale, a range containing negative values uses its observed minimum and maximum with safe normalization. Dotted-map weights remain nonnegative because they can control point radius.

`WithPointLegend()` gives dotted maps one key per typed location, using that location's label and resolved dot color. The keys retain observation IDs, coordinates and optional weights, including for locations outside the selected viewport. HTML legend actions address the corresponding location and reconcile authored IDs through updates. Region and tile maps retain their independent numeric color keys.

Calendar factories and `ReplaceCalendarItems` sum duplicate dates and sort by date. Region/tile factories and `ReplaceRegionValues` resolve codes, names and aliases through the owned definition, sum duplicate canonical regions and sort by code. Both keep the last explicit input color. Their counts and observation indices refer to normalized items. Single-item updates preserve their ordinal, even when the date or region changes, and reject collisions with another observation. Empty retained series export their no-data state and can be refilled. The fluent map/calendar `Add*` conveniences accept empty observations with their required geometry and accept an optional named `id`; they prepare the same no-data state as an emptied retained series.

`ChartOptions.Calendar` owns week layout, localized day/month names and cell geometry. Use `ConfigureCalendar` independently of creating, updating or replacing the calendar series. A zero gap and Sunday week start are explicit values. Null names restore invariant abbreviations; null size settings restore automatic layout. Route conveniences between dotted-map labels capture the current coordinates when the route is added; later observation updates do not move that route.

`ObservationCount` counts authored observations, including masked categorical columns. `SourcePointCount` retains original-input counts for explicit decimation, histograms, regressions and raw-sample box plots; ordinary typed inputs count typed items. Editing a raw box-plot summary clears its raw-sample provenance because the new summary is no longer derived from those samples.

Relationship series retain immutable `Nodes` and `FlowLinks`, `TreeLinks` or `HierarchyItems`. Keep the series handle when replacing a complete graph:

```csharp
var requests = ChartSeries.CreateSankey(new ChartSeriesId("requests"), "Requests",
    new[] { new ChartNode("received", "Received"), new ChartNode("completed", "Completed") },
    new[] { new ChartFlowLink("processed", "received", "completed", 40) });
chart.AddSeries(requests);
requests.ReplaceFlowData(
    new[] { new ChartNode("completed", "Completed"), new ChartNode("received", "Received") },
    new[] { new ChartFlowLink("processed", "received", "completed", 55) });
```

`PointStrokeColors` is a read-only ordinal view for Treemap and Sunburst hierarchy items. Use `WithPointStrokeColor(index, color)` to choose an explicit native border and `UseSeriesStrokeColor(index)` to restore the family state/theme border. The existing `StrokeWidth` controls explicit borders, bounded by tile or band thickness; fill and caption overrides remain independent. Other families reject this border override.

`ReplaceFlowData` serves Sankey and Chord, `ReplaceTreeData` serves Tree, and `ReplaceHierarchyItems` serves Sunburst and Treemap. Each operation copies and validates the complete graph before installation. A failure leaves facts and metadata intact. A successful replacement clears ordinal observation IDs, point presentation, node states and flow overrides while retaining series identity and configuration. Authored node and flow IDs in the replacement identify the new targets even after reordering or renaming. Already prepared visuals retain the earlier graph; export the chart again to render its replacement.

Empty node/link pairs or empty hierarchy items create the same no-data state through factories and replacements. Partial empty graphs still undergo family validation. Relationship `ObservationCount` counts nodes; `SourcePointCount` is zero because these families have no numeric XY source observations, and `Points` stays empty and read-only.

## Configuration callback ownership

Public option callbacks use one transaction: clone current options, invoke the callback, then install an independent clone after success. A thrown callback leaves the previous owner and collections unchanged. References obtained before the callback, and references retained inside it, do not mutate the newly installed options. Read the current option property after a successful callback when another edit is needed.

Direct option setters remain supported. Assigning a value expresses intent even when it equals the default; nullable overrides can return to inheritance through null where documented. Text-style boolean assignments also preserve explicit false when overriding inherited italic, underline or strikethrough. Callback ownership applies to installed configuration; one-time data selector and formatter functions have their own documented execution lifetime.

## Independent uncertainty bounds

`ChartErrorBar(x, y, xBounds: ..., yBounds: ...)` requires at least one immutable `ChartErrorBounds(lower, upper)`. Supply X, Y or both independently; each finite interval must contain its estimate. Zero-width intervals are valid. The DateTime overload uses the existing UTC-to-OLE-Automation conversion for X. Typed intake and updates snapshot the bounds, validate the whole replacement before mutation, and retain source identity. The renderer fits both endpoint and cap geometry, including when estimate markers are disabled. Native SVG/PNG, tooltips, keyboard readouts and point legends preserve distinct X and Y bounds and their own axis formatters.

```csharp
using ChartForgeX.Core;

var chart = Chart.Create().WithSize(640, 420)
    .AddErrorBar("Estimate", new[] {
        new ChartErrorBar(4, 20,
            xBounds: new ChartErrorBounds(3, 5),
            yBounds: new ChartErrorBounds(16, 25))
    });
var svg = chart.ToSvg();
var png = chart.ToPng();
```

## Grouped category axes

`ChartAxisLabel.Groups` describes a category's parents, from the outermost group to the nearest parent. Supply the original numeric coordinate and leaf caption as usual:

```csharp
var orders = Chart.Create().WithXAxis("Month").WithYAxis("Orders")
    .WithXLabels(new[] {
        new ChartAxisLabel(1.5, "Jan", "2026", "Q1"),
        new ChartAxisLabel(2.5, "Feb", "2026", "Q1"),
        new ChartAxisLabel(4, "Mar", "2026", "Q1"),
        new ChartAxisLabel(5.25, "Apr", "2026", "Q2")
    })
    .AddBar("Actual", new[] {
        new ChartPoint(1.5, 48), new ChartPoint(4, 62), new ChartPoint(5.25, 53)
    });
```

February has a catalog slot and no observation. Grouping preserves numeric spacing, raw values, source order and observation IDs. The constructor copies the parent path; all nonempty paths in a chart must have the same depth. Repeated parent names form separate runs when an intervening category has another path or no group. Conflicting captions at the same coordinate and ambiguous mappings are rejected. Every observed category needs an explicit leaf label.

Groups use the physical category axis: X for vertical charts and Y for horizontal charts. Configure horizontal labels through `ConfigureYAxis`; the existing `WithXLabels` fallback applies when Y has no explicit labels. Automatic bounds include the complete authored catalog, including leaves without observations. Explicit bounds remain exact. Supported native X windows clip the same full-source group cells rather than regrouping the visible observations.

Parent tiers share the native SVG/PNG layout and remain passive descriptions. Captions can wrap, shorten or omit within the measured axis budget; `cartesian.category-group-overflow` reports that limit, while complete paths and source spans remain in semantics. Point readouts include the parent context. HTML target descriptors add `categoryPath`, ordered from the outermost parent through the leaf; `category` and category matching retain the leaf caption.

Grouped labels require an ordinary Cartesian chart with a linear category axis. They do not enable grouped time/logarithmic axes, histogram or time-bucket aggregation, sparklines, matrix axes or radial categories. The [category examples](../../ChartForgeX.Examples/CartesianCategoryExamples.cs) demonstrate missing months, a Bar/Line mixture, and repeated horizontal site names in both themes.

## Numeric color presentation

`ChartColorScale.WithValueRange(double? minimumValue, double? maximumValue)` fixes each uniform continuous endpoint independently. Use `WithValueRange(0, null)` to pin a count scale's floor while its peak follows observations, `WithValueRange(null, 100)` to pin a ceiling, or `WithValueRange(null, null)` to restore automatic bounds. Two supplied bounds must be finite and strictly ascending. Positioned stops require both fixed endpoints; discrete bands reject this method.

An inferred endpoint cannot cross its fixed counterpart. If all observations lie at or below a sole fixed minimum, they use `LowColor`; at or above a sole fixed maximum, they use `HighColor`. Automatic constant domains retain `LowColor`. With no observations, an inferred endpoint stays unresolved: retained keys show missing data without a fabricated numeric extent. Fully fixed continuous ranges and discrete bands retain their authored key domain. `ColorFor(value)` requires two fixed continuous endpoints; partial ranges use `ColorFor(value, sourceMinimum, sourceMaximum)`.

`ChartColorDimensionOptions` owns nullable `Name` and `ValueFormat`, `ShowLegend` (default true), and validated `LegendStyle` (`Auto`, `Gradient`, `Swatches`). Each supported owner exposes one getter-owned instance: `ChartSeries.ColorDimension` for Scatter/Bubble, RegionMap/DottedMap datasets and explicitly placed TileMap insets, `ChartOptions.MapColorDimension` for map defaults and TileMap, `ChartOptions.HeatmapColorDimension` for numeric matrices/calendars, and the Treemap/Sunburst/Chord options' `ColorDimension`. Configure those instances with the corresponding `ConfigureColorDimension`, `ConfigureMapColorDimension`, `ConfigureHeatmapColorDimension` or family callback. Unsupported family combinations fail preparation.

Null restores each name/format property's family default independently. A supplied name must contain non-whitespace text. Scatter/Bubble and Chord color use `ChartLabels.Color` and neutral numeric formatting by default. Map, heatmap and hierarchy color retain their existing value-format defaults, including the chart's `ValueFormat`. Explicit color formatting changes their numeric labels, keys and readouts. It leaves Cartesian Y, Bubble size, hierarchy size, Chord weight, explicit point labels and authored secondary captions unchanged. Region/tile map and heatmap observations have one numeric value, so their readouts do not add a duplicate Color row. A dotted-map point can show its independent weight and color measurements as separate rows.

The shared prepared presentation captures names and display strings. An explicit format runs once per distinct finite measurement during preparation; measurement, SVG/PNG export and HTML readouts reuse the result. Missing measurements retain localized no-data text without invoking a numeric formatter. Custom empty output remains authoritative, and formatter exceptions propagate. Raw observations, numeric scale mapping and selection identities remain separate from display text.

`ShowLegend` controls numeric keys independently of categorical `ChartSeries.ShowInLegend` entries. `Options.ShowLegend` remains the all-legends master for convenience exports; an explicit preparation context retains its frame visibility precedence. Hiding a numeric key leaves mark paint, the observed domain and raw facts intact. Categorical heatmap keys use `ShowHeatmapCategoryLegend` / `WithHeatmapCategoryLegend`.

`LegendStyle.Auto` preserves the family's existing presentation. `Gradient` draws the continuous mapping through all reachable authored stops, with numeric positions and alpha; a constant domain draws one mapped color without inventing bounds. `Swatches` uses the shared bounded sampling policy for continuous scales and exact named intervals for discrete scales. Style does not select a sampling mode or change the scale. Explicit styles require a resolved `ChartColorScale` (including an inferred hierarchy scale); legacy map/heatmap policies support `Auto`. `Gradient` with `Discrete` fails preparation because unbounded bands do not define a continuous gradient. Legend styling alone does not enable formatted color-text overrides.

`ChartColorScale.WithCaptions` adds named captions at absolute numeric positions independently of the color anchors:

```csharp
var scale = ChartColorScale.Sequential(ChartColor.FromHex("#164b75"), ChartColor.FromHex("#f1c75b"))
    .WithValueRange(-20, 80)
    .WithCaptions(new[] {
        new ChartColorScaleCaption(0, "Freezing"),
        new ChartColorScaleCaption(35, "Operating limit")
    });
```

The scale copies and sorts the catalog, rejects duplicate or non-finite positions and blank text, and retains it through later scale copies. Empty input clears it; discrete scales use their named bands. Captions never expand the domain or alter value-to-color mapping. On gradients they follow numeric position; on swatches they follow the actual sampled values. An authored caption takes visible priority over a low, midpoint or high caption at the same value. All captions remain descriptive facts, including those outside the domain or omitted by fitting. Outside captions do not clamp to an endpoint. Horizontal keys use up to two measured caption lanes; right-hand map keys retain their inverted value direction. Neither layout shrinks ordinary captions below the shared key font floor.

Chord flows accept an independent nullable color measurement through the five-argument `ChartFlowLink` constructor:

```csharp
var flow = new ChartFlowLink("west-east", "west", "east", 42, colorValue: -3);
chart.ConfigureChord(options => {
    options.ColorScale = scale;
    options.ColorDimension.Name = "Temperature";
    options.ColorDimension.LegendStyle = ChartColorLegendStyle.Gradient;
});
```

Weight continues to control ribbon allocation and node totals. Color values may be signed or zero; null means missing. Finite values infer the theme's sequential scale unless an explicit scale is supplied. Every flow contributes its color fact, including zero-weight or collapsed flows and flows with manual paint. ID-keyed fill, source-point fill and series fill keep precedence over the color mapping; node arcs retain their categorical paint. Numeric color facts, the separate key and readouts remain available under paint overrides. Hiding the key leaves the observed domain and mapping unchanged. Sankey rejects independent flow color because it has no quantitative color policy.

## Configuration precedence

The prepared context owns size, theme, font and frame behavior. `Chart.WithSize`, `Chart.WithTheme` and other model presentation options supply the request for convenience exports such as `chart.ToSvg()` and `chart.ToPng()`. They do not silently override an explicit `Prepare(context)` request. Hosts that supply a context set these values in `VisualRenderContext`.

The model's title and subtitle supply fallbacks when the corresponding `VisualFrame` values are null. An empty string explicitly suppresses that line. Frame legend visibility, placement, content surface and transparent canvas come from the context, including its defaults. Model card settings do not override an explicit frame. Categorical legends use the frame's visibility permission; numeric keys additionally honor their owned `ColorDimension.ShowLegend` setting. Data semantics, explicit series/point colors, label styles, axes and formatters remain model inputs. Convenience exports resolve the model's legend and card settings into their frame before preparation.

Prepared Cartesian guides default to horizontal lines. Bar, line and grid style objects have the same defaults as the prepared renderer: flat bars, plain lines and horizontal guides. Direct property assignments and successful configuration callbacks are explicit, including values equal to the default. A zero bar radius or a one-pixel grid width therefore overrides a different theme value when assigned. An empty callback preserves inheritance.

Prepared Cartesian point semantics retain the complete resolved display label, even when visible placement shortens or omits it. Generated numeric and paired-value captions are complete or omitted whole; authored point captions and custom endpoint prose keep their text-fitting policies. Explicit point labels take precedence over the value formatter. Formatting runs once per point during preparation and the detached metadata reuses that result; exporting a prepared visual does not call the formatter again. Numeric coordinates and source identities remain separate metadata fields.

`Chart.AddPlotRegion(new ChartPlotRegion(color, ...))` adds a passive solid background rectangle to a Cartesian chart. `Chart.PlotRegions` owns its immutable definitions in paint order. Nullable `xMinimum`, `xMaximum`, `yMinimum` and `yMaximum` extend to the corresponding final domain end; regions never expand domains. `yAxis` selects primary or secondary Y. Bounds follow physical X and Y. Horizontal charts use value coordinates for X and linear category slots for Y. Time bounds use OLE Automation dates, and logarithmic value bounds must be positive. A secondary region requires a secondary-axis series.

Use `new ChartPlotRegion(paletteIndex, ...)` to bind a region to the active render theme's categorical palette. Indices are zero-based, wrap to the palette length, and cannot be negative. Both the fill and caption contrast resolve during preparation; SVG series color variables retain that binding. Literal-color regions keep their authored color. `ChartPlotRegion.Color` is nullable for palette-backed regions; inspect `PaletteIndex` to distinguish the two forms. Automatic accessible descriptions and `ChartDescriptionFacts.PlotRegionLabels` retain authored captions even when the chart has no points.

Rebuild previously compiled callers of `ChartPlotRegion.Color` and the `ChartDescriptionFacts` constructor when upgrading: the nullable getter and the constructor's added optional argument change their binary signatures.

Use `region.WithBorder(new ChartStrokeStyle(...))` to return a new immutable definition with a native solid or dashed inward border. Null removes it; zero width or transparent stroke paints none. Border color and width inherit the theme axis stroke when omitted. Borders follow the **visible clipped intersection**, so an off-screen authored edge is represented by the visible rectangle's edge. Empty intersections paint no border. Prepared SVG and PNG share the same clipped path; there are no new interaction targets.

`region.WithLabelPosition(ChartPlotRegionLabelPosition.TopLeft)` selects a screen-relative anchor inside the visible intersection. `Center`, the four corners and the four edge centers are available. Explicit anchors shorten or omit when blocked rather than moving to another anchor; `Auto` keeps the established center-first collision-aware choices. Reversal moves the data region while its named screen corner remains deliberate. Border space is reserved before fitting captions. Every original caption remains descriptive metadata.

`chart.Options.XAxis.WithRuleCrossAt(6)` paints the X **rule** at primary physical Y=6; `chart.Options.YAxis.WithRuleCrossAt(4)` paints the Y rule at primary physical X=4. Secondary X and Y rules use those same opposite **primary** physical coordinates, including horizontal charts. The existing opposite-axis mapper owns scale, reversal, compressed time and discrete time spacing. An invalid logarithmic sign or closed compressed-time coordinate rejects; a coordinate outside the resolved/windowed domain omits the rule with `cartesian.axis-rule-outside-domain`. An excluded maximum omits too. No rule is clamped to a different source value, and domains do not expand. Null restores the established edge. Only non-sparkline Cartesian charts support this policy.

Rule crossing deliberately leaves tick captions and titles in their measured edge gutters. Use `axis.WithCrossAt(value)` instead to move the complete native axis anatomy: rule, tick captions, category parent bands and title. `CrossAt` and `RuleCrossAt` are mutually exclusive; clear the old policy with null before selecting the other. Both use the same opposite primary physical coordinate and source-window owner.

Complete crossing preserves the existing measured plot reservation and data/mark projection. Primary X captions follow below the rule and secondary X above; primary Y captions follow left and secondary Y right. X titles follow those bands. Y titles retain their horizontal treatment at the moved track's top endpoint, aligned left for primary and right for secondary. Text placement runs after the actual marks, using existing measured tick/title/category-band and collision owners. Prior axis text, actual marks and source guides can cause shortening or omission; complete semantic tick/title/group facts remain. This does not promise that every caption fits every compact/dense pose. Crossing outside a resolved/windowed domain omits **all** opted-in anatomy with `cartesian.axis-crossing-outside-domain` and retains its raw crossing and full semantic captions; it never leaves an edge axis pretending to be the requested interior axis. Existing visibility/ShowLine flags remain independent.

Mermaid options can use `ShowPivotAxes` for complete normalized-pivot axes; it rejects combination with `ShowPivotRules`. The compiled `interior-quadrants` / `compact-interior-quadrants` recipe selects this full anatomy through ordinary public APIs. Source declarations alone do not qualify its integrated native/browser appearance. Existing region constructors keep their signatures, and default `Auto`/no-border/null-rule exports retain their existing commands. The added fluent region methods return new definitions: retain that returned value or pass it to `AddPlotRegion`.

Mermaid `MermaidQuadrantRenderOptions` offers `RegionBorder`, `RegionLabelPosition` and opt-in `ShowPivotRules` (normalized 0.5). These map into the same native model. Quadrant source `classDef` and `:::class` annotations use the existing solid-color style parser and native Scatter markers. The four exact style names are `radius`, `color`, `stroke-color` and `stroke-width`; direct declarations override the referenced class, and a repeated class definition replaces its earlier declarations. Numeric style radii and pixel stroke widths are whole values from 0 through 1024; colors use three- or six-digit hexadecimal spelling. Source order and normalized point coordinates remain unchanged.

`ChartSeries.ConfigurePointMarkers(index, callback)`, `GetPointMarkers(index)` and `UseSeriesMarkers(index)` author Scatter-only overrides through `ChartMarkerOptions`. Callback and getter values are detached. Unspecified fields inherit the series marker; explicit point colors retain fill precedence. Numeric insertion/removal and keyed changes follow surviving source identity. `Points.Clear()` clears the overrides; assigning one existing item preserves them. Other families reject this API explicitly.

Quadrant low/high source captions are independent native axis labels. With two captions they occupy normalized `.25` and `.75`; a single low caption occupies `0`, while a high caption remains at `.75`. These use `ChartAxis.Labels` and the shared measured axis layout, with ordinary numeric readouts retained. `MermaidQuadrantRenderOptions.Width` and `Height` override source `quadrantChart.chartWidth` and `chartHeight` only when explicitly assigned. Without either, the established 960-by-560 native preview remains.

Safe mapped source settings cover native dimensions, point radius, point/title font sizes, axis/title visibility, region fills and the supported point/X-caption/title inks. Independent Y-caption ink, per-axis font sizes, region text ink, padding, border-config fields and axis-side settings lack faithful source owners here and retain located diagnostics. Raw class, point, axis and configuration declarations with source spans travel in bounded `mermaid.quadrant.sourceFacts` metadata; this is inert portable source evidence, not editable scalar-chart JSON. The native recipe `quadrant-point-styles` and the parsed shared source fixture are separate consumers. This does not add undocumented Mermaid grammar or claim upstream pixel identity.

Four regions express quadrants at any numeric pivot:

```csharp
chart.AddPlotRegion(new ChartPlotRegion(ChartColor.FromHex("#328c68"),
    xMaximum: 4, yMinimum: 6, label: "Prioritize", opacity: .18));
```

The visible intersection follows the selected scales and axis reversal while authored bounds and captions remain in metadata, including empty intersections. Optional captions use fitted opaque backplates inside that intersection and avoid data marks; shortening or omission reports `cartesian.plot-region-label-overflow`. Prepared SVG and PNG share frozen geometry and paint. HTML regions and their captions remain descriptive backgrounds: points retain pointer, selection and keyboard actions. Other chart families reject plot regions.

Visible Cartesian axes retain a descriptive region for each configured or generated tick and each axis title, including text shortened or omitted by placement. Tick regions include the complete resolved display text and numeric value. Each axis formatter runs once per tick during preparation; measurement, placement and semantics reuse the same result. These descriptive extents do not promise an exact hit-test shape.

Automatic numeric axes keep compact captions when they distinguish ticks, and increase precision when small nonzero values round to zero or distinct ticks share a caption. Default mark values also retain small nonzero observations instead of displaying zero. Authored labels and axis or chart value formats remain authoritative. Precision affects display text; numeric values and prepared SVG/PNG snapshots remain unchanged by later model edits.

Automatic Cartesian and schedule time axes choose calendar intervals from measured label space, leaving a gap between complete captions instead of filling every available slot. The interval ladder includes three- and four-day steps and two-week steps; multi-day ticks keep the same spacing across month boundaries. Setting `TickCount` explicitly limits the generated count. Call `axis.WithAutomaticTickCount()` to restore automatic density. `ChartLabelDensity.Dense` requests closer spacing; authored formats alone do not change the density policy.

Date captions prefer a coarser interval with full dates before using a compact form. The default compact form is culture-neutral `MM-dd`; ranges crossing a year retain `yyyy-MM-dd`. Explicit labels and formatters retain their text, including localized month names. Narrow axes keep real calendar boundaries instead of labelling marker-padded bounds with arbitrary times of day. Regularly sampled time bars retain their source cadence, so their automatic labels stay centred over observations; irregular time series retain ordinary calendar alignment. Tick labels stay anchored to their ticks in SVG and PNG.

Automatic Gantt axes reserve room for the visible project's first and last dates and label both when their complete captions fit. Default day-scale captions identify dates even when task endpoints contain clock times; the exact instants remain unchanged. Authored formats retain their text. When a coarse format cannot distinguish adjacent dates, automatic selection keeps a meaningful caption instead of repeating the rejected sequence. An authored axis window limits those endpoints to the visible window. A successfully generated fixed interval keeps its authored grid and does not add off-interval endpoints; an interval that exceeds the bounded generation limit uses the automatic measured fallback.

Frame headings and legends fit within the resolved size. Overflow is reported through diagnostics; a fixed render request never grows silently. Full textual values remain available through accessible metadata and descriptive regions where provided. A PNG requires concrete dimensions. Responsive hosts measure their viewport and prepare a new result; proportional SVG scaling does not claim compact-layout reflow.

Ordinary axis and caption sizes remain as requested. Crowded placement can wrap, shorten, thin, or omit labels while retaining their complete descriptive facts; a small per-tick allocation does not shrink the whole axis. Oversized type still fits measured bounds. Numeric key captions fit down to a 10-pixel ordinary floor, and explicitly authored smaller sizes remain authoritative. Compact gallery overviews are prepared for their display size with their own useful label and legend budgets.

Set `legendTitle` in the `VisualFrame` constructor to give categorical legend entries a measured heading. Null permits the producer's source title; an empty string suppresses it. The heading consumes the same `LegendMaximumHeightFraction` budget as its entries. `LegendMaximumRows` limits entry rows, including any overflow summary. Frame copies used by charts and grids retain all of these settings.

`VisualFrame.ShowCard` and `ShowSurface` independently enable the outer elevated card and content surface. Configure `VisualTheme.CardRadius` through the `cardRadius` constructor argument without changing `BarRadius`. The `cardShadowOpacity` argument accepts 0–1; `cardShadowColor` retains authored alpha, which multiplies that opacity. Shadows use bounded native layers inside available outer padding. Zero opacity disables the shadow, and insufficient padding produces a diagnostic instead of expanding the viewport or shifting the content.

`VisualTheme.Graphite()` reads the checked-in generated HtmlForgeX chart palette. `VisualTheme.FromPaletteJson` accepts that generated chart-token shape, not the full HtmlForgeX owner schema. HtmlForgeX governs color values; the chart look specification governs layout and typography defaults. Typography, spacing and geometry settings absent from the color export have explicit ChartForgeX defaults. Register or supply suitable fonts when portable appearance matters; equal geometry does not guarantee identical browser/native glyph painting.

`VisualTheme.ToJson()` exports the complete paired theme, including typography, geometry and effects, using `schemaVersion: 1`. The independent card radius is `geometry.cardRadius`; shadow opacity and color are `effects.cardShadowOpacity` and `effects.cardShadowColor`. `VisualTheme.FromJson(json)` imports that versioned format. Version-1 inputs may omit these card fields and use their defaults. Keep these round-trip methods distinct from the generated color-token importer; unsupported versions, duplicate keys and missing required fields fail explicitly.

## Prepare once, export several ways

Visuals-owned mixed grids expose `grid.ToVisualArtifact(id)` through the same artifact vocabulary as chart grids, canvases and factual blocks. Natural size comes from the grid's logical layout and remains independent of `PngOutputScale`. Artifact creation does not render children. The envelope borrows the grid model; recreate it after changing layout or headings, then use the shared SVG, PNG, HTML or Office handoff.

```csharp
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

var chart = Chart.Create()
    .WithTitle("Storage capacity")
    .WithXLabels("Used", "Available")
    .AddDonut("Capacity", new[] {
        new ChartPoint(1, 60), new ChartPoint(2, 40)
    });
chart.Options.ValueFormatter = value => value.ToString("0") + " GB";
chart.Accessibility.Name = "Storage capacity: 60 GB used and 40 GB available";

var context = new VisualRenderContext(
    layout: new VisualLayoutOptions(new VisualSize(640, 400)),
    theme: VisualTheme.Graphite(),
    themeMode: VisualThemeMode.Light,
    frame: new VisualFrame(showLegend: true));

PreparedVisual prepared = chart.Prepare(context);
string svg = prepared.ToSvg();
byte[] png = prepared.ToPng(new VisualRenderOptions(scale: 2));
// Both exports consume the same prepared layout; the PNG has 1280 x 800 pixels.
```

Changing the chart, its input tokens, its accessible metadata or a font request after preparation does not alter an existing prepared result. Returned raster images and byte arrays belong to the caller; changing those buffers does not edit the scene. `PreparedVisual.Accessibility` also returns an independent copy.

Producer-owned semantic envelopes are defensively copied as typed values during preparation. Lazy JSON serialization closes over that private snapshot, never the caller's mutable envelope. Static preparation and SVG/PNG export do not apply portable interchange budgets; requesting retained interchange or `ToArtifact(...)` applies the versioned JSON boundary and its limits. Returned semantic envelopes are independent copies.

SVG exports retain accessible names, descriptions, language and decorative intent. The default export scopes DOM IDs using a deterministic digest of the prepared scene and accessible metadata. When embedding identical copies in one document, give each a distinct host prefix, such as `prepared.ToSvg("report-capacity-left")`. Prefixes start with an ASCII letter and contain only ASCII letters, digits, hyphens, underscores or periods. DOM IDs and fragment references receive the prefix; `data-cfx-*` source identities and `Regions` retain their model IDs.

All chart families use the shared prepared export route. Invalid family combinations and option values fail during preparation. Empty/all-zero pie and donut input produces a no-data diagnostic rather than invalid angles. `MaximumPieSlices` applies consistently across prepared themes; an aggregate `Other` retains contributing source indices and passes point index `-1` to the slice formatter. The model and semantic interchange remain the source for editable document data; a display list is not a substitute for topology or sequence semantics.

Requested font weights retain their exact CSS value from 1 through 1000. Selecting the nearest available font face does not rewrite the requested weight. Automatic chart text alternatives use `ChartLabels.AccessibleTextFormatter` and its render-neutral facts; explicit accessibility names and descriptions remain authoritative. Hiding or fitting a heading does not discard the complete source text.

## Chart geometry

`ChartSeries.Interpolation` selects `Linear`, `Smooth` or `Step` for connected line, area and range boundaries. Difference areas support the same three choices. `WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle)` places transitions halfway between mapped observations; `Start` and `End` place them at the preceding and following x positions. `AddStepLine` and `AddStepArea` default to End. Existing smooth helpers use the same setting. Explicit interpolation overrides a convenience entry point, and preparation rejects unsupported families or a step position without step interpolation. Regression and slope lines retain their straight-line meaning and reject non-linear interpolation. Segment breaks remain gaps in every mode.

`AddSlope(name, points)` captures an ordered sequence of `ChartPoint` stage observations. X remains the category or stage coordinate; Y is the recorded value. Empty sequences retain a no-data series, one point is rejected, and two or more points draw one marker and source fact per stage. The existing two-value overloads keep their Start/End or authored endpoint captions. Collection-based stages use explicitly configured axis labels. Omitted stages have no observation; `BreakBefore` starts a disconnected segment without creating an interpolated value.

Set `chart.Options.SlopeOrientation = ChartOrientation.Horizontal` to place source X stages on physical Y and values on `XAxis` or `SecondaryXAxis`. The category axis must be linear or time; value axes retain their linear, signed-logarithmic, reversal and window policies. Horizontal mixtures require every series to use horizontal orientation. `UseSecondaryValueAxis()` selects secondary physical X when horizontal and secondary Y when vertical. Native category windows use `y` when horizontal and `x` when vertical; value windows use their physical axis names. Prepared exports retain the captured stages and projection after the source is updated.

```csharp
var stages = Chart.Create().WithXLabels("Received", "Prepared", "Accepted")
    .AddSlope("Capacity", new[] { new ChartPoint(1, 28), new ChartPoint(2, 43), new ChartPoint(3, 64) });
stages.Options.SlopeOrientation = ChartOrientation.Horizontal;
stages.SaveSvg("capacity-stages.svg");
stages.SavePng("capacity-stages.png");
```

`ChartSeries.StackGroup` assigns independent Cartesian bar, numeric radial bar/column or stacked-area groups. Named groups stack even with grouped bar mode, while unnamed grouped series remain separate. Stacks partition by series kind and axis. `WithNormalization(100)` normalizes positive and negative subtotals independently to +100 and -100. Every member of a stack must declare the same normalization target; unstacked normalization is rejected. Stacked-area members must also share interpolation and step placement so their touching boundaries align; use a separate group or axis for independent choices. All-zero coordinates stay at the baseline and report `cartesian.stack-zero-total` or `numeric-radial.stack-zero-total`.

Normalization changes geometry, bounds and total positions together. Raw observations, labels and `data-cfx-y` remain source values. `data-cfx-rendered-y`, `data-cfx-base` and `data-cfx-stack-end` describe the displayed contribution and extent. Format the relevant value axis as percentages when using a target of 100; source labels still describe counts or other authored units. `ChartSeries.StackedAreaMissingData` defaults to `Independent`, retaining each series' authored X positions and breaks. `Gap` splits the complete group at any X missing from one member and connects only positions observed by all members. `Zero` aligns the observed X union, adding zero-height contour vertices for missing members. A missing vertex uses the positive stack baseline; signed authored contributions and normalized totals keep their existing owner and values. No mode interpolates values or creates source observations. Explicit breaks split the complete aligned group. Every member of one group and value axis must use the same policy; distinct groups and primary/secondary axes remain independent. Gap/Zero require unique increasing resolved X positions, including across breaks. Original markers, IDs and readouts remain authored, even where a Gap contour is absent.

The [stacked-area recipe](../../ChartForgeX.Examples/Recipes/StackedAreaRecipe.cs) supplies complete `missing-zero` and `missing-gap` variants, with compact counterparts. It retains an authored zero separately from the absent Wednesday contribution.

`ChartOptions.Funnel` selects `StageBars` or `Cone` and `Vertical` or `Horizontal`. StageBars is the default: each stage's cross-axis extent is exactly its value divided by the largest value. Cone uses one value-bearing line per source stage and connects adjacent lines. Zero stages retain source slots and semantics without an invented width. A single cone stage has no connection region. Both forms use the shared prepared geometry in SVG and PNG.

`AddPyramid` partitions a triangle in authored category order. `ChartOptions.Pyramid.ValueEncoding` defaults to `Height`: 50/30/20 uses .5/.3/.2 of the tip-to-base length. `Area` uses square-root cumulative boundaries so the polygon areas, rather than their lengths, follow those shares. `Orientation` selects vertical or horizontal output; `Reversed` mirrors the geometry while retaining source ordinals and category order. `AspectRatio` is base width divided by tip-to-base length in either orientation; a positive finite value fits and centers the triangle in the content remaining after measured label rails.

```csharp
var pyramid = Chart.Create().WithXLabels("Services", "Platform", "Support").WithDataLabels()
    .AddPyramid("Allocation", new[] { new ChartPoint(1, 50), new ChartPoint(2, 30), new ChartPoint(3, 20) })
    .ConfigurePyramid(options => {
        options.ValueEncoding = ChartPyramidValueEncoding.Area;
        options.Orientation = ChartOrientation.Horizontal;
        options.Reversed = true;
        options.AspectRatio = .75;
    });
```

Pyramid values and their aggregate must be finite and non-negative. Zero values retain source facts and zero geometry; all-zero input emits a no-data diagnostic. A singleton occupies the entire triangle. Series/point paints, patterns, data-label styles and placement preferences use the common controls. Labels use a safe inner rectangle when readable and a measured outer rail otherwise. SVG metadata records the declared encoding, source value, value share, normalized length and normalized area; geometric shares never replace source values in tooltips or detached artifacts. A positive partition below floating-point boundary or coordinate precision retains its source value and emits a precision diagnostic rather than receiving an invented minimum size.

`ChartOrientation` is the shared core orientation type, including `MermaidXYChartDocument.Orientation`. Replace `MermaidXYChartOrientation` references with `ChartForgeX.Core.ChartOrientation` when migrating parsed XY chart code.

## Independent paired boundaries and nullable input

RangeBand, RangeArea and DifferenceArea accept `FirstBoundaryInterpolation` and `SecondBoundaryInterpolation`. Null inherits `ChartSeries.Interpolation` and `StepPosition`. For ranges, First is lower and Second is upper; for differences, both retain their authored names and order. The immutable `ChartBoundaryInterpolation` combines Linear/Smooth/Step with its step placement:

```csharp
var ranges = ChartRangeBands.FromXY(new[] { 1d, 2, 3, 4, 5 },
    new double?[] { 12, 22, null, 25, 15 }, new double?[] { 32, 46, 38, 48, 41 });
var expected = ChartSeries.CreateRangeArea(new ChartSeriesId("expected"), "Expected range", ranges)
    .WithBoundaryInterpolation(new(ChartInterpolation.Smooth), new(ChartInterpolation.Step, ChartStepPosition.Middle));
var chart = Chart.Create().AddSeries(expected);
var svg = chart.ToSvg();
var png = chart.ToPng();
```

Independent policies require increasing X within each connected run. New independent smooth curves and smooth DifferenceArea curves keep cubic X controls within each source interval; Y uses the existing smooth interpolation. Legacy RangeArea smooth defaults retain their existing geometry. RangeArea's optional midpoint path continues to use the series interpolation, with raw source midpoints retained in readouts. Independently interpolated range boundaries can cross between ordered sample endpoints; use DifferenceArea when a crossing should select positive/negative fill. Multi-band overlap ordering still follows series paint order.

`ChartRangeBands.FromXY` and `ChartDifferencePoints.FromXY` accept matching X/nullable boundary collections. A missing member omits the complete tuple. The default `ChartMissingDataPolicy.Gap` starts a new segment at the next complete tuple; Connect joins surviving tuples. Neither aligns independent series, interpolates missing values nor substitutes zero. All supplied numbers, including the other member of an incomplete pair and missing-slot X positions, must be finite. Range pairs retain lower <= upper validation. Empty or all-missing input returns no observations. The optional cancellation token stops enumeration before a result is returned; using the result with `ReplaceRanges` or `ReplaceDifferencePoints` therefore leaves the existing series unchanged on cancellation or invalid input. Assign explicit observation IDs to the returned tuple indices; omitted sample slots acquire no identities.

The source-owned [range-area](../../ChartForgeX.Examples/Recipes/RangeAreaRecipe.cs) `independent-boundaries` and [difference-area](../../ChartForgeX.Examples/Recipes/DifferenceAreaRecipe.cs) `smooth-boundaries` variants include missing tuples, independently selected boundaries and retained IDs.

## Difference areas

`AddDifferenceArea` retains two authored values at every X, using `ChartDifferencePoint(X, First, Second, BreakBefore)`. The first value can be above or below the second; it is never renamed or swapped into a lower/upper bound. Positive fill means `First > Second`; negative fill means `First < Second`. Ties have no filled area and remain source observations. `RangeBand` and `RangeArea` keep their ordered lower/upper interval contract and existing output.

```csharp
var chart = Chart.Create().AddDifferenceArea("Actual and forecast", new[] {
    new ChartDifferencePoint(1, 42, 56),
    new ChartDifferencePoint(2, 68, 54),
    new ChartDifferencePoint(4, 48, 60, breakBefore: true),
    new ChartDifferencePoint(5, 64, 52)
});
chart.Series[0].ConfigureDifferenceArea(options => {
    options.FirstName = "Actual";
    options.SecondName = "Forecast";
    options.PositiveFill = ChartColor.FromHex("#267EAB");
    options.NegativeFill = ChartColor.FromHex("#D47A34");
    options.PositiveFillOpacity = .48;
    options.NegativeFillOpacity = .48;
});
```

Names default to First/Second. Null fill overrides inherit explicit series color or semantic-state paint; otherwise positive uses the series palette color and negative the next palette color. Null opacity inherits the area theme; opacity multiplies color alpha once. The series pattern applies to both fills, and a zero-opacity fill has no pattern. Point color/pattern overrides apply to boundary markers. The split legend swatch remains one series target. Both named values, authored source indices and breaks are retained in SVG facts, accessible labels and single/shared-X HTML readouts. Derived crossing vertices are paint geometry and never become source observations.

Difference areas support Linear, Smooth and Step interpolation with numeric/time X and a linear primary or secondary Y axis. Linear fills split where the two boundaries cross. For held observations, call `chart.Series[0].WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle)`. Both boundaries change together: Start uses the preceding X, Middle the mapped midpoint and End the following X. Each held interval takes the sign of its original pair. Vertical jumps have no filled width; ties have no filled area. These transitions do not add source observations or invent a diagonal crossing.

The DateTime constructor uses the existing UTC conversion policy. X must increase within each connected segment; explicit `BreakBefore` starts a new segment and cuts both boundaries and fill. Either axis can be reversed without changing fill polarity. Bounds and clipping use both values and the selected value axis. Smooth and independently configured boundaries split fill at crossings of the shared flattened native paths; smooth paint geometry introduces no source observations. Nonlinear axes, duplicate/backtracking X within a segment, stacking and normalization are rejected. `ChartDifferencePoints.FromXY` supplies nullable paired input with explicit Gap/Connect policy; automatic alignment of independently supplied series is outside this paired-input contract.

## Waterfall opening balances and checkpoints

`AddWaterfall(name, IEnumerable<ChartWaterfallItem>)` accepts immutable `OpeningBalance(x, value)`, `Delta(x, value)`, `Subtotal(x)` and `Total(x)` items, with `double` or `DateTime` coordinates. Items accumulate in input order and each has a distinct display coordinate. An optional opening balance must be first and may occur only once. Its finite positive, negative or zero value is drawn from zero and seeds the running balance. Opening-only input is valid. Without an opening item, accumulation starts at zero.

A subtotal spans the opening balance or preceding checkpoint balance to the current balance and reports only the changes since that anchor. A total spans zero to the current balance, including the opening balance. Both retain the running balance for the next change. Typed input renders exactly the supplied items; it appends no total. The `ChartPoint` overload retains its signed-delta behavior and automatically appended final total.

```csharp
var waterfall = Chart.Create().WithXLabels("Opening", "Costs", "Stage", "Adjustment", "Balance")
    .AddWaterfall("Movement", new[] {
        ChartWaterfallItem.OpeningBalance(1, 120), ChartWaterfallItem.Delta(2, -30), ChartWaterfallItem.Subtotal(3),
        ChartWaterfallItem.Delta(4, 10), ChartWaterfallItem.Total(5)
    }).WithDataLabels();
waterfall.Series[0].WithPointColor(2, "#A5358A");
```

Typed input lives in the immutable `ChartSeries.WaterfallItems` snapshot; `Points` is empty and cannot be mixed into that series. `Value` holds an authored opening balance or delta and is null on calculated checkpoints. `SourcePointCount` counts the opening balance and deltas, while point colors, labels, fill patterns, data-label styles and point legends index all authored items. Invalid item order is rejected before the series is added. Existing axis labels and formatters own category captions; `ChartOptions.Labels.OpeningBalance`, `.Change`, `.Subtotal` and `.Total` localize the default readout and captions. Opening balances and checkpoints use neutral paint unless series or point paint overrides it. Bar paint and stroke use the shared series controls.

Retain a series handle to update a complete item without losing its ID or presentation. Both lifecycle operations validate the whole resulting collection before changing it, including distinct coordinates and opening-balance placement:

```csharp
var movement = ChartSeries.CreateWaterfall(new ChartSeriesId("movement"), "Movement", new[] {
    ChartWaterfallItem.Delta(1, 10), ChartWaterfallItem.Subtotal(2), ChartWaterfallItem.Delta(3, 20)
}).WithObservationId(2, new ChartObservationId("late-change"));
var chart = Chart.Create().AddSeries(movement);
movement.UpdateWaterfallItem(2, ChartWaterfallItem.Delta(3, 25));
```

`ReplaceWaterfallItems(items)` installs a fresh snapshot and clears per-item IDs and presentation overrides while retaining series configuration. An empty factory or replacement represents no data, keeps `Points` read-only and appends no total; refill it with another replacement. Replacing a numeric waterfall through this operation switches it to typed authoring. `WithObservationId` indexes every authored item, including checkpoints. Native `data-cfx-observation-index` records that ordinal separately from compact numeric contributor ordinals in `data-cfx-source-point` and `data-cfx-source-points`. An explicit checkpoint ID identifies the authored checkpoint; its derived value and contributors remain available for readouts.

`ChartOptions.Waterfall.Orientation` defaults to `ChartOrientation.Vertical`. Configure a horizontal waterfall before or after adding either input form:

```csharp
waterfall.ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal)
    .WithXAxis("Running balance").WithYAxis("Stage");
waterfall.Options.XAxis.WithBounds(-50, 150);
waterfall.Options.YAxis.WithReversal();
```

The physical X axis owns horizontal values, bounds, scale, reversal and tick formatting. The physical Y axis owns categories, bounds, reversal and category captions. Each item's `X` remains its category coordinate; orientation never sorts items or changes accumulation. Explicit Y category labels take precedence; `WithXLabels` supplies a fallback when Y has no explicit labels. Resolved category captions agree across the native axis, marks, point legends and HTML readouts, including custom formatters and time categories. Horizontal waterfalls require one series and use its primary or secondary X value-axis slot.

Native marks and point legends preserve the distinction between item positions and numeric source ordinals. An opening balance or delta retains its raw value and `data-cfx-source-point`; only deltas expose `data-cfx-delta`. A checkpoint exposes its kind, calculated value and contributor ordinals through `data-cfx-derived` and `data-cfx-source-points`, with no invented source point or delta. Total contributors include the opening observation; subtotal contributors contain only changes since the anchor. `data-cfx-category-label` carries the resolved caption and `data-cfx-orientation` records the layout. Captions refer to the owning mark. Prepared SVG/PNG and static artifact output retain those facts after model changes. Portable editable chart-series interchange remains a separate capability.

## Grouped range bars and orientation

`AddRangeBar` groups multiple interval series into distinct category slots, including their end caps. Each `ChartInterval.X` remains its category coordinate; the authored `Start` and `End` retain their order even when the interval runs from a larger value to a smaller one. Point colors, patterns, labels and interaction keys index intervals rather than individual endpoints.

```csharp
var ranges = Chart.Create().WithXLabels("Standard", "Express")
    .ConfigureRangeBar(options => options.Orientation = ChartOrientation.Horizontal)
    .AddRangeBar("Expected", new[] { new ChartInterval(1, 20, 40), new ChartInterval(2, 10, 30) })
    .AddRangeBar("Observed", new[] { new ChartInterval(1, 22, 45), new ChartInterval(2, 32, 12) });
ranges.WithXAxis("Delivery time (minutes)").WithYAxis("Service");
```

`ChartOptions.RangeBar.Orientation` defaults to `Vertical` and may be configured before or after adding series. Horizontal range bars use the primary physical X value axis and physical Y category axis, including their bounds and reversal. Explicit Y category labels take precedence; `WithXLabels` supplies a fallback when Y has no explicit labels. Category formatters and point legends use the same resolved captions as the axis and HTML readout. `ChartOptions.Labels.Category`, `.Start` and `.End` localize interval tooltip row names while numeric metadata retains the raw endpoints.

Horizontal range-bar charts accept only range-bar series on the primary axis. Vertical Cartesian mixtures and secondary Y axes retain their existing support. `ChartBarMode` does not stack intervals, and per-series stack configuration remains unsupported for range bars. Multiple range-bar series that previously overlapped now appear side by side; an overlap layout is not provided.

Range-bar slots include the minimum body and cap-stroke footprint for flat styles, and the visible caps, shadows and highlights for segmented styles. If a category slot cannot fit the minimum painted footprint, rendering throws an actionable `InvalidOperationException`. Increase the category-axis space (height for horizontal intervals, width for vertical intervals), reduce grouped series, reduce stroke widths or cap effects, or choose a simpler mark style. Automatic value bounds in both orientations also contain endpoint paint and the minimum interval body; explicit value bounds remain exact and may intentionally clip that paint.

### Range-bar endpoint captions

`ChartOptions.RangeBar.LabelContent` defaults to `Combined`: one formatted range caption, overridden by `WithPointLabel`. Select `Endpoints` to show separate captions at the authored Start and End. Descending intervals keep those identities even when Start is numerically greater than End or the value axis is reversed.

```csharp
ranges.WithDataLabels().ConfigureRangeBar(options => {
    options.LabelContent = ChartRangeBarLabelContent.Endpoints;
    options.EndpointLabelFormatter = label => label.FormattedValue
        + (label.Endpoint == ChartRangeBarEndpoint.Start ? " S" : " E");
});
```

The optional formatter receives a readonly `ChartRangeBarEndpointLabelContext`: captured `SeriesName`, zero-based observation `PointIndex`, resolved `CategoryLabel`, authored `Interval`, `Endpoint`, computed raw `Value` and chart-formatted `FormattedValue`. A null formatter uses the chart's `ValueFormat`; axis-specific formatters affect ticks independently. A null, empty or whitespace result suppresses that endpoint caption. `WithPointLabel` continues to supply the observation heading and source caption in endpoint mode; it does not replace both endpoint captions.

Preparation evaluates the endpoint formatter once for Start then End per authored observation, including hidden or subsequently omitted captions. Complete text remains in observation metadata. Exceptions propagate from preparation; detached SVG, PNG and HTML exports reuse captured strings without invoking callbacks again. Stateful callbacks remain responsible for their own captured state. Combined mode retains its existing lazy numeric formatting and does not invoke the endpoint formatter.

Endpoint captions inherit chart, series and point label typography and visibility. In Endpoints mode, `StartLabel` and `EndLabel` independently override `Visible`, `Placement` and `Style`; null placement inherits the ordinary series or chart preference. Component typography resolves after the point style. Both still require data labels to be enabled on the series or chart. These options are copied by `ConfigureRangeBar` and preparation, so a retained callback candidate or later model mutation cannot alter a prepared export.

```csharp
chart.ConfigureRangeBar(options => {
    options.LabelContent = ChartRangeBarLabelContent.Endpoints;
    options.StartLabel.Placement = ChartDataLabelPlacement.Left;
    options.StartLabel.Style.WithColor("#235b9f").WithFontSize(10);
    options.EndLabel.Placement = ChartDataLabelPlacement.Right;
    options.EndLabel.Style.WithColor("#975000").WithWeight("700");
});
```

Captions use the shared measured placement and overflow diagnostics. Auto/Outside begins at the actual endpoint in its outward direction; Inside/Center uses that endpoint's half of the interval. Preferred Above/Below/Left/Right positions are physical directions, including horizontal or reversed axes. Short, coincident or clipped intervals may omit or shorten custom captions while preserving complete text and raw facts; generated numeric captions omit whole rather than displaying shortened numbers. Both captions remain associated with one original observation for browser hover, selection and muting. Hiding a caption does not suppress its captured formatter call or raw source facts.

## Gauge readings and scales

`ChartGaugeValue` owns one measured `Value`, its `Minimum` and `Maximum`, and a read-only `Targets` collection. Its constructor accepts `value`, optional bounds of 0 and 100, and optional targets. Each readonly `ChartGaugeTarget` accepts `value` and an optional `label`; null uses the formatted target value. The reading copies the supplied targets so later changes to the input collection cannot change the authored observation. Values and targets must be finite, and the bounds must define a finite positive range.

Use the existing `ChartSeries.CreateGauge(id, name, reading)` factory to retain and configure a gauge series. `GaugeValues` contains at most one complete reading. `UpdateGaugeValue` replaces that reading while preserving the observation's ID and styling; `ReplaceGaugeValues` follows the shared replacement rules. A target belongs to the reading and travels with it when its series moves between charts. `ChartOptions.Gauge.Target` is removed. Circle progress series use the same value type but require an empty target collection.

`ConfigureGauge` owns the presentation shared by the gauge series. `Form` selects Arc, Needle or Linear. Linear gauges use `ChartOrientation.Horizontal` by default, with the minimum at the left; Vertical puts it at the bottom. Arc and Needle use `StartAngleDegrees` and `SweepAngleDegrees`, defaulting to 150 and 240 degrees. Zero degrees points right and positive angles turn clockwise; the sweep must be greater than zero and at most 360 degrees. Linear gauges ignore both angle settings.

`TickCount` accepts a preferred ceiling of 2 through 100 generated ticks; crowded scales use fewer evenly spaced ticks. Null uses two radial ticks or five linear ticks. Linear and partial radial scales include both endpoints. A 360-degree radial sweep omits the maximum tick to avoid duplicating the minimum at the seam. A nonempty `TickLabels` collection replaces generated locations and text with authored `ChartAxisLabel` values. Assign an array or list; the setter retains a detached read-only snapshot in input order, while rendering uses distinct in-domain values in ascending numeric order. An empty collection restores generated ticks. Caption and semantic bands stay in these options.

The declared reading bounds govern value, target and tick placement. Values and targets outside the scale retain their raw facts while their visible marks clamp to its endpoints. `WithValueFormat` and the value formatter apply to values, targets and generated numeric ticks. An authored tick label supplies its own text. See the [migration example](migration.md#gauge-readings-and-presentation) for moving a former chart-wide target into the reading.

## Grouped box plots and orientation

`AddBoxPlot` accepts raw samples for one category, copied `ChartBoxPlotSampleGroup` groups, or authored `ChartBoxPlot` summaries. Multiple box plot series use distinct category slots, including the body, median stroke and whisker caps. If the minimum painted footprint cannot fit, rendering throws an actionable `InvalidOperationException`: increase category-axis space (height for horizontal plots, width for vertical plots), reduce grouped series or use thinner strokes. Automatic value bounds in both orientations contain complete paint, including minimum-width quartile bodies; explicit value bounds stay exact and may clip paint. Point labels, colors, patterns and interaction keys index complete summaries. The default data label is the median; a custom point label takes precedence.

```csharp
var distributions = Chart.Create().WithXLabels("Standard", "Express")
    .ConfigureBoxPlot(options => options.Orientation = ChartOrientation.Horizontal)
    .AddBoxPlot("Expected", new[] { new ChartBoxPlot(1, 10, 20, 25, 30, 40), new ChartBoxPlot(2, 5, 10, 15, 25, 35) })
    .AddBoxPlot("Observed", new[] { new ChartBoxPlot(1, 12, 22, 30, 38, 48), new ChartBoxPlot(2, 8, 12, 22, 30, 42) })
    .WithXAxis("Delivery time (minutes)").WithYAxis("Service");
```

`ChartOptions.BoxPlot.Orientation` defaults to `Vertical` and may be configured before or after adding series. `ChartBoxPlot.X` always denotes the category coordinate. Horizontal box plots use the primary physical X value axis and Y category axis, including bounds and reversal. Move value scales and tick formatting to `XAxis` and category formatting to `YAxis`; explicit Y labels win, with `WithXLabels` as a fallback when Y has no explicit labels. Positive logarithmic values use the existing value-axis scale without an invented zero baseline. Horizontal box plots accept other horizontally oriented Cartesian series and either X value-axis slot. Existing vertical Cartesian mixtures and secondary Y axes remain supported. Box plots do not participate in scalar bar stacking or normalization.

Raw samples are sorted for calculation. For probability `p`, the quantile interpolates between adjacent sorted samples at zero-based position `(n - 1) * p`, with `p` equal to 0.25, 0.5 and 0.75. Thus samples `[30, 0, 20, 10]` produce minimum `0`, Q1 `7.5`, median `15`, Q3 `22.5` and maximum `30`. A single sample supplies all five values. The default `MinMax` policy reaches the sample minimum and maximum. `ConfigureBoxPlotStatistics` can select `Iqr` with a finite positive `IqrMultiplier` (default 1.5). Samples strictly outside the inclusive Q1/Q3 fences appear as outlier markers; automatic bounds still include their original extrema. Authored summaries retain their supplied five values exactly and must satisfy `Minimum <= Q1 <= Median <= Q3 <= Maximum`.

`ChartOptions.Labels.Category`, `.Minimum`, `.Q1`, `.Median`, `.Q3` and `.Maximum` localize HTML readout rows. The readout exposes all five summary statistics; its heading uses the captured series name and median or custom point label. Raw sample summaries retain their derived identity and immutable `BoxPlotSourceSamples`; they do not acquire invented sample indices. Prepared geometry, labels and metadata remain detached from later model changes. Raw groups expose copied `BoxPlotSourceGroups`, flattened `BoxPlotSourceSamples` and immutable `BoxPlotDistributions`. `ObservationCount` counts groups; `SourcePointCount` counts original measurements. `SourceIndices` and `OutlierSourceIndices` use flattened input order. `SourceMinimum/Maximum` retain observed extrema, while `InlierMinimum/Maximum` are nullable actual inlier extrema. Whiskers collapse to a quartile when needed to keep the five-number summary ordered; for `[0,100,101,102]` under IQR, the lower whisker is 75, the observed inlier minimum is 100 and the source minimum is 0.

```csharp
var latency = ChartSeries.CreateBoxPlot(new ChartSeriesId("latency"), "Latency", new[] {
    new ChartBoxPlotSampleGroup(1, new[] { 0d, 1, 2, 3, 4, 5, 6, 7, 8, 100 }),
    new ChartBoxPlotSampleGroup(2, new[] { 2d, 3, 4, 5, 6 })
}).WithBoxPlotGroupIds(new[] { new ChartObservationId("service-a"), new ChartObservationId("service-b") })
  .ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr);
var chart = Chart.Create().WithXLabels("Service A", "Service B").AddSeries(latency);
```

`WithBoxPlotGroupId(s)` identifies a derived distribution independently of original sample IDs assigned with `WithObservationId(s)`. A statistics change preserves both namespaces. `UpdateBoxPlotSampleGroup` preserves the group's ID and presentation, clears its replaced sample IDs and shifts later sample ID indices. Complete raw replacement clears IDs and per-group presentation while retaining policy. Editing or replacing supplied summaries clears all raw provenance and IDs; supplied values stay authoritative. Prepared exports remain detached from every later change.

HTML readouts add sample count, calculation policy, source extrema and outlier count. IQR captions use `ChartLabels.LowerWhisker` and `UpperWhisker`; source/inlier extrema, policy, estimator, multiplier, absent inliers and individual outlier captions have corresponding `ChartLabels` properties. A keyed group preserves summary selection across prepared updates; a keyed sample preserves outlier selection only while that rendered outlier target remains. Point legends emphasize, mute and isolate the complete group's summary and outliers.

Set `ChartOptions.BoxPlot.LabelContent` to `Statistics` to caption the five rendered summary values separately. `MinimumLabel`, `Q1Label`, `MedianLabel`, `Q3Label` and `MaximumLabel` each expose `Visible`, nullable physical `Placement` and inherited `Style`. The default `Median` mode retains its current median/custom-point-label output and ignores these statistic controls. In Statistics mode a custom point label stays the observation heading; it does not replace a summary value.

```csharp
chart.WithDataLabels().ConfigureBoxPlot(options => {
    options.LabelContent = ChartBoxPlotLabelContent.Statistics;
    options.MinimumLabel.Visible = false;
    options.MedianLabel.Placement = ChartDataLabelPlacement.Right;
    options.MedianLabel.Style.WithWeight("700");
    options.StatisticLabelFormatter = value => value.Statistic + ": " + value.FormattedValue;
});
```

`StatisticLabelFormatter` receives a captured `ChartBoxPlotStatisticLabelContext` in Minimum, Q1, Median, Q3, Maximum order, once per summary and member even when data labels or an individual caption are hidden. It includes the complete category caption, summary/group index, rendered `Summary`, selected `Statistic`, raw `Value` and chart-numeric `FormattedValue`. Minimum and Maximum mean the rendered whiskers under IQR policy, not the separate raw source extrema. Null or whitespace output suppresses only that caption. Exceptions propagate from preparation; detached SVG/PNG and browser exports never invoke the formatter again.

The existing measured placement owner rejects collisions and out-of-frame captions. Inside/Center uses each statistic's interval between adjacent projected midpoints; equal or tightly packed values may leave no usable lane. Numeric captions remain complete or omit whole with the existing overflow diagnostic; custom prose may shorten. Full raw facts and custom captions remain in metadata even when ink is omitted. Every painted statistic caption points to the same summary identity for browser readout, selection and muting. Broader density, font and mixed-family qualification remains separate from this supported label policy.

## Sankey layout

`ChartOptions.Sankey` configures the shared native SVG/PNG geometry without changing authored nodes, flows or values:

```csharp
chart.ConfigureSankey(options => {
    options.Alignment = ChartSankeyAlignment.Center;
    options.VerticalAlignment = ChartSankeyVerticalAlignment.Top;
    options.NodeOrder = ChartSankeyNodeOrder.LabelAscending;
    options.NodeWidth = 16;
    options.NodeGap = 12;
    options.NodeCornerRadius = 1;
    options.RibbonFill = ChartColor.FromHex("#607DAA");
    options.RibbonOpacity = .45;
});
```

| Option | Default and meaning |
| --- | --- |
| `Alignment` | `Justify` retains longest source depths and moves sinks to the final layer. `Left`, `Right` and `Center` select other feasible layers. |
| `VerticalAlignment` | `Center` splits each column's unused space equally; `Top` and `Bottom` place it against the corresponding content edge. |
| `NodeOrder` | `Auto` uses bounded weighted ordering passes. `Input` retains input order per layer; `LabelAscending` and `LabelDescending` compare labels ordinally and preserve input order for ties. |
| `LabelPlacement` | `Right` places measured labels to the right of node bars. `Left` places them to the left; `Center` centers them over the bars with the existing readable backdrop. |
| `EdgeLabelPlacement` | `Outside` keeps first-column labels on the left and last-column labels on the right. `Inside` reverses those sides. `null` applies `LabelPlacement` to both edge columns. |
| `NodeWidth` | `10` logical units; must be finite and positive. |
| `NodeGap` | `null` uses `max(12, theme.Spacing * 1.5)`; an explicit gap must be finite and non-negative. |
| `NodeCornerRadius` | `null` uses `theme.BarRadius`; an explicit radius must be finite and non-negative and is bounded to half the bar's smaller dimension. |
| `NodeFill` | `null` retains series/state/theme paint, including neutral default bars. |
| `RibbonFill` | `null` retains source-node series/state/palette paint. |
| `RibbonOpacity` | `.35`; accepts 0–1 for filled ribbons. An authored pattern overlay keeps its separate opacity. |

The earliest feasible layer is the longest path depth from any source. The latest is the graph's final layer minus the longest remaining path to a sink. `Left` uses the earliest layer, `Right` the latest, and `Center` uses `floor((earliest + latest) / 2)`. This midpoint policy also applies to disconnected components and keeps every edge directed to a later layer.

Explicit point colors take precedence over the corresponding family fill, then series colors and state/theme fallbacks. Layout order never reorders `Nodes` or `FlowLinks`, changes source ordinals, or reassigns point styles and ID-keyed states. All node and ribbon thicknesses use the same scale; gaps and widths do not inflate small weights. Preparation rejects node widths or gaps that cannot fit the supplied viewport.

Label placement uses the same node and ribbon geometry under every policy. Opposing labels share the available gap in separate slots; the shared measured label placer avoids caption collisions. Captions wrap, shorten or omit when space is insufficient, while full node text and raw flow facts remain available in native semantics and interactive readouts. Point-specific typography and colors apply under every placement. Any truthful minimum-width treatment remains separate work.

## Flow artifact content sizing

`FlowArtifactStep.FitLabelToContent` defaults to `true`. Step width and height are minimum dimensions; preparation measures titles, subtitles, badges and status inside the shared topology shape geometry, then grows a detached node before layout and routing. Explicitly setting the policy to `false` keeps the dimensions fixed. Crowded fixed shapes fit or shorten content inside their outline and expose overflow diagnostics while retaining full source text in semantic interchange. The policy is copied by `ConfigureStep` and retained in each flow node's semantic JSON.

## Flow paint and semantic state

Sankey and Chord share `ChartFlowStyle` and ID-keyed series overrides. Style values are immutable; `FlowStyles` and `FlowStates` expose read-only live views of the mutable series builder. IDs are case-sensitive, scoped to the series, and must match an existing authored `ChartFlowLink.Id`. Parallel flows can have separate paint and state even when their endpoints and labels match.

```csharp
var series = chart.Series[0];
series.WithFlowStyle("priority", new ChartFlowStyle(
    fill: ChartColor.FromHex("#AF6B24"), fillOpacity: .7,
    fillPattern: ChartFillPattern.DiagonalForward,
    stroke: ChartColor.FromHex("#764415"), strokeWidth: 1));
series.WithFlowState("priority", ChartSeriesState.Warning);
series.WithFlowState("standard", ChartSeriesState.None); // Suppress inherited semantic state.
series.WithFlowStyle("priority", null); // Restore inherited paint.
series.WithFlowState("priority", null); // Restore source-node/series state.
```

Fill precedence is explicit flow fill, source-node point color, family ribbon fill where available, series color, explicit flow semantic state, source-node/series semantic state, then the source-node palette color. Explicit colors retain precedence when a semantic state is present; the resolved state still appears in flow metadata. `None` uses categorical paint instead of inheriting the source-node or series state. Styling never changes source/target identity, raw weight, layout order or ribbon geometry.

`FillOpacity` replaces the family's ribbon opacity and multiplies the resolved color alpha. Null retains the family default. A null `FillPattern` inherits the source-node point pattern or series pattern; `ChartFillPattern.None` disables it. Sankey patterns keep their separate .6 opacity, while Chord patterns follow the resolved fill opacity. Chord target cues use the resolved base flow color and retain its alpha independently of fill opacity.

An explicit `Stroke` enables a one-unit outline. A positive `StrokeWidth` enables an outline using `Stroke` or the resolved base flow color; zero disables it. `StrokeOpacity` multiplies the outline color alpha and defaults to one. Fill, pattern or opacity settings alone do not enable an outline. Dimensions must be finite and non-negative, opacities must be finite values from zero to one, and enum values must be defined.

Existing charts need no migration and retain their paint defaults without overrides. Prepared SVG and PNG remain detached from later style/state edits. These C# overrides do not add a relationship authoring grammar to chart markup.

## Histogram ingestion

`ChartHistogramBinLayout` is the immutable interval owner for scalar and typed data. `FromCount` and `FromWidth` retain automatic decimal rounding; `FromBoundaries` preserves unequal adjacent intervals, and `FromIntervals` permits ordered nonoverlapping intervals with gaps. Authored intervals require positive finite widths. Bins include their lower edge and exclude their upper edge, except the final upper edge is included. Measurements outside the layout or inside a gap are rejected. Explicit layouts accept empty input; inferred layouts require observations. Exact bounds with constant data retain the existing zero-width value bin; density requires positive widths.

```csharp
var values = new[] { 0.2, 0.4, 1.2, 2.5, 5.0 };
var observations = new[] {
    new ChartPoint(0.2, 10), new ChartPoint(0.4, -4),
    new ChartPoint(1.2, 8), new ChartPoint(2.5, 12)
};
var bins = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1, 3, 6 });
var counts = Chart.Create().AddHistogram("Measurements", values, bins);
var density = Chart.Create().AddHistogram("Measurements", values, bins,
    ChartHistogramEncoding.Density);
var means = Chart.Create().AddHistogram("Quantity", observations, bins,
    ChartHistogramAggregation.Mean); // ChartPoint.X is measurement; Y is quantity.
```

`Count` counts observations, `Sum` adds signed quantities, and `Mean` computes their arithmetic mean. `ChartSeries.HistogramBins` retains each bin's raw nullable `Value`, `RenderedValue`, `Count`, actual `Width` and original `SourceIndices`. Empty counts and sums equal zero; an empty mean has `Value == null`, a zero-height placeholder and no numeric label. An observed zero mean remains a defined value. SVG metadata and accessible bin descriptions retain this distinction and the source facts.

`Value` uses the aggregate as height. `Density` divides it by the bin width and uses a full rectangular interval without decorative inset or capsule styling, so area represents the signed aggregate. Density requires linear value and measurement scales. Compatible density series can share one stack with exactly matching intervals, aggregation and axis; normalization and grouped subdivisions are rejected. Authored axis bounds can crop marks, and authored `XAxisLabels` remain authoritative.

Typed data uses `dataset.Bin(selector, layout)` and `AddHistogram(name, bins, encoding)`. Typed and scalar count layouts share the same rounding. A typed histogram requires the complete ordered partition and retains source rows; use `AddBar` for a selected or reordered set of bins.

`ChartSeries.HistogramSourcePoints` retains an immutable copy of the input observations in their original order. Each bin's `SourceIndices` indexes that collection; X is the measurement and Y is the supplied quantity, or one for scalar count input. Input array changes do not change this source snapshot; derived aggregate points are read-only. Prepared bin extents, aggregate values and contributor identities also remain detached from later model edits.

Migration: replace the former global `layout.Width` with `layout.GetWidth(index)`, because the last regular bin and authored bins can have different widths. Histogram layout, aggregation and encoding are read-only series facts. Labels, paints and styles can change after ingestion; to change bin X/Y aggregates, rebuild from observations so source statistics stay consistent.

## Aggregate source snapshots

`ChartSeries.TrendLineSourcePoints` retains the original regression observations in input order. `ChartSeries.BoxPlotSourceSamples` retains original samples in input order, flattened across copied `BoxPlotSourceGroups` for grouped raw input. These read-only collections remain independent of caller-owned arrays and separate from calculated summaries and `BoxPlotDistributions`. A box plot built from authored five-number summaries has empty raw provenance.

Migration: use these typed source collections and `HistogramSourcePoints` in place of raw `source-observation` or `source-sample` region labels. Retain the series' collection when original rows are needed after preparation. `PreparedVisual.Regions` describes rendered marks and semantic layout extents; it does not expose a typed raw observation store. Its mark facts and exported SVG/PNG remain detached, and interactive histogram events retain each bin's contributor indices. Keeping source data separate avoids creating and embedding a rendering-region object for every unrendered row.

Filter `prepared.Regions.Where(region => region.Role == "point")` when counting or selecting Cartesian observations. Layout regions also affect the unfiltered collection's count and order. A usable Cartesian chart exposes a `cartesian-plot` region for its final mapping rectangle; composed panels translate its bounds and prefix its ID. This region supplies host guide bounds independently of mark clipping and has no observation label.

## Explicit Pie and Donut bands

`ChartSeries.RadialBand` accepts immutable `ChartRadialBand(innerRatio, outerRatio)` values within `0 <= inner < outer <= 1`. The ratios use one shared available radius. An explicit band controls both radii for Pie or Donut; null retains that family's ordinary single-series geometry. `WithRadialBand(null)` restores it.

Multiple Pie/Donut series use either explicit, disjoint bands on every series or one automatic allocation. Explicit bands retain their authored positions independently of series order. Each series keeps its own total, percentages, aggregation budget, point formatting, observation IDs and legend entries. Categories may share labels without sharing totals.

`chart.WithAutomaticRadialBands(new ChartRadialBandAllocation(innerRatio: .2, gapRatio: .04))` allocates equal positive band thickness in source-series order, from inside to outside. `ChartOptions.RadialBandAllocation` holds the immutable policy; null preserves ordinary or explicit-band geometry. The inner hole and gaps use the shared available-radius fraction. Each value is finite in `[0,1)` and the complete set of gaps must leave positive width. Automatic allocation requires Pie/Donut series with every authored `RadialBand` unset. Explicit and automatic policies cannot mix. Empty series keep their allocated slots. Moving a series changes its automatic ring position while retaining its identity; it does not write derived bands back into the source model.

```csharp
using ChartForgeX.Core;
using ChartForgeX.Primitives;

var prior = new ChartSeries(new ChartSeriesId("prior"), "Prior", ChartSeriesKind.Donut,
    new[] { new ChartPoint(1, 75), new ChartPoint(2, 25) })
    .WithRadialBand(new ChartRadialBand(.2, .5));
var current = new ChartSeries(new ChartSeriesId("current"), "Current", ChartSeriesKind.Pie,
    new[] { new ChartPoint(1, 132), new ChartPoint(2, 88) })
    .WithRadialBand(new ChartRadialBand(.65, 1));
var chart = Chart.Create().WithSize(800, 500).WithXLabels("Subscriptions", "Consulting")
    .WithDonutCenterText("100", "Prior")
    .AddSeries(prior).AddSeries(current);
var svg = chart.ToSvg();
var png = chart.ToPng();
```

Only the innermost hole owns the center caption. A disk at the center, an empty innermost series or a band without usable space does not transfer that caption to another ring. Every band can request `Outside`, `Left`, `Right`, `Above` or `Below` captions. Active inner captions and positive slice offsets use one shared fitted frame and a complete-chart callout pass. Each source offset still translates its exact sector by the requested fraction of that observation’s resolved outer radius; positive offsets may overlap neighbouring bands, and existing source-series paint order resolves that overlap. Band ratios describe the unshifted geometry, not a promise that explosion preserves gaps. Ordinary single-series slice offsets retain their existing output.

External captions share measured lanes and label/leader collision checks across all rings. Inner leaders intentionally overlay enclosing occupied bands; a background halo distinguishes each leader without cutting a false gap into the data. `DataLabelConnectorStyle` retains straight, elbow and curve paths. Captions whose text or actual leader cannot fit without covering another caption are shortened or omitted with `radial.label-overflow`; their complete text, ring and source observation association remain available. These are overlay callouts, not obstacle-free routes through enclosing rings. The complete [multi-ring callout recipe](../../ChartForgeX.Examples/Recipes/MultiRingCalloutsRecipe.cs) includes explicit/automatic allocation, compact/wide frames, both themes and mixed external placement.

Labels must fit inside their resolved band or authored slice radius; unavailable space omits paint with a diagnostic and retains complete semantic text. Bands thinner than one logical unit retain source and legend facts with `radial.band-insufficient-space`. `MaximumPieSlices` applies independently to each series, including retained `Other` contributors and overflow facts. Native SVG and PNG use the same prepared geometry; raster/output budgets apply to the complete chart.

### Authored observation radius

`series.WithPointSliceRadiusRatio(sourceIndex, ratio)` sets a finite ratio from zero through one. `PointSliceRadiusRatios` is a read-only source-ordinal override view; null entries and unlisted indices use the complete resolved band thickness. The visible outer radius is `inner + (outer - inner) * ratio`. The value in `ChartPoint.Y` still determines the slice angle, total and percentage. This API does not infer a radius scale from values or compare areas between rings.

```csharp
current.WithPointSliceRadiusRatio(0, .35).WithPointSliceRadiusRatio(1, .7);
// Remove explicit bands before selecting automatic allocation.
prior.WithRadialBand(null);
current.WithRadialBand(null);
chart.WithAutomaticRadialBands(new ChartRadialBandAllocation(.2, .04));
var svgWithRadii = chart.ToSvg();
var pngWithRadii = chart.ToPng();
```

`UseDefaultSliceRadius(sourceIndex)` clears one override. Insertion and removal move overrides with their observations; editing a value preserves its override and identity. Clearing or completely replacing source observations clears the overrides. Existing prepared scenes remain detached from later edits. Clearing all overrides and setting `RadialBandAllocation = null` restores the previous single-series or explicit-band geometry.

Zero radius retains the value, identity, legend and semantic facts without a painted slice or visible label. A reduced positive override that leaves less than one logical unit of thickness omits paint with `radial.slice-insufficient-space`. Negative values remain invalid. External label connectors start at the actual visible slice radius and use the existing bounded outer label lanes. Inner-ring external captions and positive multi-ring explosion follow the shared-frame overlay/overlap contract above.

When `MaximumPieSlices` actually combines contributors into `Other`, their effective radius ratios must agree within `1e-12`; default and explicit one both mean full thickness. Differing contributors reject with `radial.aggregate-radius`, the series identity and source indices. Increase the slice budget or author a common ratio. No radius is inferred or averaged for an aggregate, and source contributor facts remain available.

Mutable Chart artifacts refresh their reserved series identities and radial metadata when a common interchange envelope is requested. Both explicit and automatically resolved band ratios are current after edits or reset. An automatic allocation or observation-radius override also records a bounded `chart.series.N.radial.sourceFacts` JSON array with raw source index, explicit observation identity (or null), X, value and nullable radius ratio; the existing canonical writer handles escaping and finite-number checks. Existing entry, text, identity and aggregate value budgets apply before fact serialization. Consumer metadata is retained. Captured envelopes and prepared artifacts remain snapshots.

Newly displaced multi-ring sources additionally retain each raw `offsetRatio` in the bounded radial source-fact array; an aggregated `Other` sector remains unshifted and retains its original contributor offsets there. Removing those offsets removes the optional facts when no other radial policy needs them.

This is common metadata with `VisualArtifactInterchangeFamily.None` and the existing static SVG fallback, not a typed numeric-chart importer. Keep the native model to edit Pie/Donut observations after save/load. The compiled [Pie radius](../../ChartForgeX.Examples/Recipes/PieSliceRadiusRecipe.cs), [Donut radius](../../ChartForgeX.Examples/Recipes/DonutSliceRadiusRecipe.cs) and [automatic rings](../../ChartForgeX.Examples/Recipes/AutomaticRadialBandsRecipe.cs) recipes demonstrate ordinary package use.

## Point markers and radial forms

`ChartSeries.Markers` is the shared point-marker configuration for connected Cartesian series, scatter, bubble, radar and polar. `ConfigureMarkers` configures one of nine built-in shapes, visibility, fill and outline. Ordinary point glyphs also accept a logical radius override. Null dimensions and paints preserve the family defaults; explicit point colors retain precedence over the marker fill. `MarkerRadius`, `WithMarkerRadius` and `UseThemeMarkerRadius` use the same radius value. Bubble series require a null marker radius and use `ChartOptions.Bubble` for size mapping. Dotted maps retain their existing radius override; their map geometry does not accept the other marker options.

```csharp
chart.Series[0].ConfigureMarkers(markers => {
    markers.Shape = ChartMarkerShape.Diamond;
    markers.Radius = 5;
    markers.StrokeWidth = 1;
});
```

`Enabled = false` hides glyphs while retaining source descriptions and connected lines or areas. Ordinary marker radii of zero also hide glyphs. Different shapes can paint different areas at the same logical radius. SVG, native PNG and legend markers use the same shape geometry. Custom marker paths and dashed marker outlines remain separate options.

Scatter and Bubble series with a numeric color scale use the theme foreground for their default marker outlines, so pale midpoint and missing-value fills remain visible. Scatter uses a one-pixel outline; Bubble keeps its family width and series-width override. The mapped fill and its alpha remain unchanged. Set `Markers.Stroke` for a chosen outline color or `Markers.StrokeWidth = 0` to remove it; a positive width without a stroke color keeps the neutral default. Source marks and point-legend glyphs share the same paint and width.

`ChartForgeX.Core.ChartLineAreaForm` is the shared form type for `ChartSeries.Radar.Form`, `ChartSeries.Polar.Form` and `MetricCard.MiniSparklineStyle`. Its values are `Area = 0` and `Line = 1`; Radar and metric-card sparklines default to Area, while continuous Polar defaults to Line. Metric cards select the same form through `WithMiniSparklineStyle`.

`AddRadarArea` and `AddRadarLine` use the same Radar series kind and shared categorical/radial axes. `ChartSeries.Radar.Form` selects filled Area or unfilled Line, and `ConfigureRadar` configures the area's `FillOpacity`; null uses the theme. `AddRadar` retains the Area default. Line form rejects area opacity.

`ConfigureRadar(radar => radar.MissingCategoryPolicy = ChartRadarMissingCategoryPolicy.Gap)` leaves unknown categories unpainted. `Zero`, the default, retains derived zero vertices with missing metadata and no authored source identity. `Gap` breaks the outline at missing categories, keeps observed runs across the category seam, and omits an incomplete area fill and its legend fill with a `radar.open-area` diagnostic. `Connect` joins the observed vertices across missing categories and closes an area only when at least three observed vertices remain. Gap and Connect add no missing point targets or captions. Authored zeros remain observations under every policy; the common category axis remains the union of the participating series.

`WithPolarGridShape(ChartPolarGridShape.Circle)` selects circular guides for radar or polar line charts. `Polygon` joins the spokes; `Automatic` preserves native radar polygons and polar circles. `Options.PolarGridRingCount` optionally requests zero through one hundred equally spaced guides in the configured linear, logarithmic or symmetric-logarithmic scale. Zero hides concentric guides while retaining spokes and observations. Null preserves generated axis ticks. Guide settings preserve source values and series forms and use the same prepared geometry for SVG and PNG. Fractional guide counts can extend beyond the data-axis radius, so fitting the outer guide can resize the plot and change rendered observation coordinates. Observation coordinates remain unchanged when the plot layout is unchanged.
`ConfigureRadar(radar => radar.CurveTension = 0.25)` selects closed cubic curves through the observations. Null or zero keeps straight edges. Each radar uses its selected value-axis domain during curve fitting. Standalone charts and column overlays reserve complete curve bounds with one scale factor shared by their marks, axes and guides; overlays retain the configured inner-radius proportion and transparent hole. Source values and column stack totals remain unchanged.

These radar forms also combine with `AddRadialColumn` through the existing series calls. The native numeric radial owner aligns every series to the union of category X identifiers and places radar vertices at whole-category centers, regardless of grouped column slots. Each value axis builds its domain from column segment endpoints and the raw values of its radar series; radar contributes neither stack slots nor normalized totals. `UseSecondaryValueAxis()` selects an independent numeric domain and reversal for radar in both standalone and mixed charts. Standalone radar starts its first category at the top; mixed charts use the column viewport's category centers. `ConfigureXAxis(axis => axis.WithReversal())` reverses those category positions together with their spokes and captions. Standalone radar then starts its last category at the top. Source observations, category labels, point colors and missing-category identities keep their original associations.

Mixed column/radar charts use the declared positive sweep of at most one turn in `ChartRadialGeometryOptions`. Each series must have unique category identifiers; a populated radar needs at least three categories across the composition. Missing column observations remain absent. Radar uses its configured missing-category policy in both standalone and mixed layouts. The annular sector clips radar fills, patterns, lines and glyphs in both native exporters. A partial sweep has physical start/end boundaries; Gap does not join observed runs across that seam. Zero and Connect retain their authored closed-polygon meaning, clipped to the sector. The operation retains the category union and all source observations rather than filtering data. `RadialBar`, continuous `Polar`, `PolarArea` and Cartesian series cannot join this composition.

Continuous `Polar` uses authored `ChartPoint.X` radians from the positive horizontal ray. Its default direction is counter-clockwise; `XAxis.WithReversal()` makes increasing radians advance clockwise. Points, angular spokes and captions use the same projection, while angle metadata and readouts retain the authored radians. `YAxis` and `SecondaryYAxis` control numeric radius reversal independently.

`ChartSeries.Polar` configures the continuous angle/radius family without changing `PolarArea` sectors. `Form` defaults to `Line`; `Area` closes each authored run with at least three vertices and fills only that run. `ClosePath = true` optionally closes Line runs; otherwise lines remain open. `FillOpacity` is Area-only and defaults to the theme. `OutlineStroke` uses the existing immutable stroke type, including dash paint and width-zero visibility.

`CurveTension` from zero through one selects shared cubic tangents; null or zero retains straight joins. Each `ChartPoint.BreakBefore` starts a new Polar run. No sorting, inferred wrap gap, missing zero or extra observation is introduced. Open curves do not borrow tangents across a break; closed curves wrap only within their own run. A short Area run retains its observations/open outline and reports `polar.open-area` without fill or a false area legend. Curve fitting reserves actual cubic extrema for the shared radius rather than clipping overshoot at the original straight-edge radius. Negative radius semantics remain unsupported.

```csharp
var signal = new ChartSeries(new ChartSeriesId("signal"), "Signal", ChartSeriesKind.Polar,
    new[] { new ChartPoint(0, 30), new ChartPoint(.8, 80), new ChartPoint(1.7, 55),
        new ChartPoint(3.1, 60, true), new ChartPoint(3.9, 40), new ChartPoint(4.8, 90) })
    .ConfigurePolar(options => {
        options.Form = ChartLineAreaForm.Area;
        options.FillOpacity = .22;
        options.CurveTension = .15;
    });
var chart = Chart.Create().WithYAxisBounds(0, 100).AddSeries(signal);
chart.Options.PolarRotationDegrees = 25;
chart.Options.XAxis.LabelOrientation = ChartAxisLabelOrientation.Parallel;
```

`Options.PolarRotationDegrees` rotates dedicated Radar, Polar and PolarArea coordinates clockwise from their existing starting rays. It defaults to zero and accepts finite degrees; projection reduces full turns while source radians/categories remain unchanged. Radar and Polar guides, category captions and value-label rays follow the same rotation. Mixed numeric radial charts keep `ChartRadialGeometryOptions.StartAngleDegrees`/`EndAngleDegrees` and reject a nonzero dedicated-polar rotation.

Dedicated Radar and Polar labels use the same `ChartAxis.LabelOrientation` owner as numeric radial charts. Parallel follows the angular axis's tangent or a radius axis's spoke; Perpendicular turns that baseline by 90 degrees. Relative baselines stay upright, `LabelAngle` adds the authored offset, and Fixed uses screen degrees. Reservation, collision footprints and native SVG/PNG painting share the resolved rotation. Explicit label density keeps its existing overlap policy; complete descriptive captions survive shortening or omission.

The complete [radial forms recipe](../../ChartForgeX.Examples/Recipes/RadialFormsRecipe.cs) creates disconnected smooth Polar areas and a rotated Radar with axis-relative captions. The HTML adapter retains normal pointer and keyboard point navigation across run boundaries and native narrow/wide drawings; it does not turn these source-ordered angles into a Cartesian X-window. Angular-window navigation uses a separate native projection contract. Partial mixed sweeps use the existing numeric radial geometry and retain all source observations.

`PolarArea` uses source order for its equal-angle category slots; X values identify their labels. `XAxis.WithReversal()` reverses those slots together with their source paints, patterns and data captions. The last source category then occupies the first slot clockwise from the top ray. Source indices, values, percentages and zero-slot identities remain stable. Radii retain the square-root mapping that makes sector areas proportional to values; `YAxis.WithReversal()` is unsupported and rejects during preparation.

Numeric radial `ChartPoint.X` identifies an ordinal category; `Y` is the signed source value. The numeric domain belongs to `YAxis` or `SecondaryYAxis` in both angular-bar and radial-column orientations. `ChartRadialGeometryOptions` owns finite clockwise start/end angles, inner radius and category/series spacing. Reversal belongs to the corresponding `ChartAxis`, so tick placement and mark projection agree. Fixed nonzero numeric bounds constrain visible geometry without rewriting observations. Percent rings use the separate `AddProgressRing` API and retain their 0–100 contract.

For categorical sector-area charts, use `AddRadialColumn` with `ChartRadialGeometryOptions(valueEncoding: ChartRadialValueEncoding.Area)`. Grouped, named-stack and normalized series share the same endpoint law on their independent primary or secondary axes. Area requires homogeneous RadialColumn series, linear selected value axes and nonnegative bounds and observations; unsupported families and scales reject. The default `Radius` encoding and legacy single-series `AddPolarArea` behavior remain available.

Nominal sector area measures the normalized visible interval, including an authored annular hole. Raw-value proportionality requires a zero baseline and compatible normalization. Spacing, rounded corners and clipping can change painted pixel area. Source values and observation IDs stay unchanged. The complete [categorical radial area recipe](../../ChartForgeX.Examples/Recipes/CategoricalRadialAreaRecipe.cs) includes grouped, stacked, compact and Radius variants. This encoding does not add numeric radial gestures or editable radial JSON restoration.

Numeric radial `Inside` and `Center` captions fit entirely within their painted sector. Captions that cannot fit are shortened or omitted with `numeric-radial.label-overflow`; their full source values remain in descriptive regions and point metadata. Outside captions avoid painted marks, and automatic inside ink resolves contrast against the composited fill. Explicit label colors remain caller-controlled.

Numeric radial axis text uses the existing chart options. `WithXAxis`, `WithYAxis` and `WithSecondaryYAxis` place horizontal titles in measured strips inside the shared frame. Primary and secondary value titles share the top strip; the category title uses the bottom strip. A value title participates when that axis has observations or a complete authored domain. `AxisTitleStyle` controls all title text; shortened or omitted titles report `numeric-radial.axis-title-overflow` and retain their full descriptive text. An entirely empty chart keeps its no-data surface.

`ConfigureXAxis`, `ConfigureYAxis`, `ConfigureSecondaryXAxis` and `ConfigureSecondaryYAxis` set each axis's independent `LabelOrientation` and `LabelAngle`. `Fixed`, the default orientation, uses screen degrees: zero is horizontal, and equivalent full turns normalize before layout and paint. `Parallel` follows the local axis track; `Perpendicular` uses the direction across it. An angular axis follows its local tangent, while a radius axis follows its spoke. For radial bars the category axis is radial and the value axes are angular; radial columns use the opposite arrangement. Relative modes add `LabelAngle` as an offset, then keep the baseline upright. Reversal uses the projected position, and primary and secondary value axes keep independent settings. Other chart families reject a nonfixed orientation during preparation.

Numeric radial `LabelDensity` controls caption spacing independently on each axis. `Auto` keeps the balanced spacing; `Dense` accepts touching captions; `Relaxed` reserves more separation. `All` accepts caption overlaps without collision thinning, while each label still fits the viewport and avoids painted marks. Text may therefore be shortened or omitted when no safe footprint fits. The coincident first/last tick of a full turn retains one seam caption. Every fitted axis caption remains an obstacle for later ordinary captions, stack totals and data labels. Complete text remains in descriptive regions. Orientation and density do not rotate titles or data labels. `ShowAxes = false` or an axis's `Visible = false` excludes its text and spacing; `ShowLine = false` controls only the axis rule.

```csharp
chart.ConfigureXAxis(axis => {
    axis.LabelOrientation = ChartAxisLabelOrientation.Parallel;
    axis.LabelAngle = 10;
    axis.LabelDensity = ChartLabelDensity.Relaxed;
}).ConfigureYAxis(axis => {
    axis.LabelOrientation = ChartAxisLabelOrientation.Perpendicular;
    axis.LabelDensity = ChartLabelDensity.Dense;
});
```

If measured text leaves no plotted radius, `numeric-radial.insufficient-space` reports the omitted geometry. Point identities, values and descriptive regions remain available with `data-cfx-geometry-status="layout-collapse"`; the HTML adapter retains keyboard readouts without creating pointer hit areas.

`ChartRadialGeometryOptions.CornerRadius` rounds numeric radial bars and columns. `ChartSunburstOptions.CornerRadius` rounds Sunburst sectors through `ConfigureSunburst`. Both values are finite, nonnegative maximum radii in logical pixels and default to zero. The native outline is shared by SVG, PNG, patterns, gradients and label fitting.

```csharp
var bars = Chart.Create()
    .WithRadialGeometry(new ChartRadialGeometryOptions(
        startAngleDegrees: -90, endAngleDegrees: 180, cornerRadius: 6))
    .AddRadialBar("Requests", observations);
var sunburst = Chart.Create()
    .ConfigureSunburst(options => options.CornerRadius = 6)
    .AddSunburst("Allocation", hierarchy);
```

Rounding stays inside each projected sector and clamps independently at its outer and inner corners to the thickness and angular space available. Each painted stack segment rounds separately, including clipped or reversed segments. The scale extents and source values remain unchanged. A zero inner radius retains its sharp central apex; a full circle or annulus has no corners and keeps its seamless outline. Zero-size or encoded-collapsed sectors retain facts without a minimum painted size.

These options apply to their named families. A positive numeric radial radius on another populated family, or a positive Sunburst radius on a different family, fails preparation. Pie, Donut, PolarArea and Chord keep their existing sector geometry; progress rings keep their separate line-cap treatment.

## Financial marks

`ChartSeries.Financial` owns direction-specific `Rising` and `Falling` options for candlestick and OHLC series. A close greater than or equal to the open selects Rising, including doji. `ConfigureFinancial` configures body fill, outline and wick or the OHLC stem and ticks without changing the validated source prices.

```csharp
chart.Series[0].ConfigureFinancial(financial => {
    financial.Rising.FillOpacity = 0;
    financial.Rising.Stroke = ChartColor.FromHex("#2A967B");
    financial.Rising.StrokeWidth = 2.4;
    financial.Rising.DashPattern = new[] { 7d, 5d, 2d };
    financial.Rising.DashOffset = -3;
    financial.Rising.Wick.Stroke = ChartColor.FromHex("#729BB7");
    financial.Rising.Wick.StrokeWidth = 1.2;
    financial.Rising.Wick.DashPattern = Array.Empty<double>(); // Solid wick, independent of the body.
    financial.Falling.FillOpacity = .65;
});
```

Null paints and numeric options inherit the existing family behavior. Body fills keep their rising/falling alpha defaults unless `FillOpacity` is supplied. `StrokeOpacity` and `FillOpacity` multiply the chosen color's alpha and accept finite values from zero to one. Stroke widths accept finite nonnegative logical pixels; zero hides the corresponding stroke. Wick values inherit the item's stroke color, alpha multiplier and width. Item widths inherit an explicit series `StrokeWidth` before the family default. An observation's `PointColors` entry takes precedence over every financial part color; part widths and alpha multipliers remain independent. OHLC supports the item's stroke settings and rejects fill or wick settings. Configured financial options on other series kinds also fail preparation.

`DashPattern` copies positive finite dash and gap lengths in logical pixels; odd-length patterns repeat twice and the complete period must remain finite. An item pattern of null or empty means solid. `DashOffset` accepts finite signed logical pixels, with null meaning zero on an item. Wick pattern and offset each inherit independently when null; an empty wick pattern explicitly restores a solid wick. Dash phase starts separately for each OHLC stem, open tick and close tick, and for each emitted wick segment. A candlestick body outline uses one continuous closed contour. Legend glyphs scale pattern lengths and phase with their geometry and stroke widths.

A zero fill multiplier or a transparent effective fill creates a hollow candlestick with no interior wick or hatch. SVG, native PNG and configured financial legend glyphs consume the same captured pattern, phase and resolved paint. Zero widths and transparent strokes remain hidden regardless of dash settings. A point legend shows its observation's direction; a series legend shows rising and falling glyphs under one series action. Bounds cover the painted body and strokes, including minimum-height and all-equal bodies. Preparation freezes styles, geometry, regions and legend output independently of later option changes.

`ConfigureFinancialValue(index, configure)` stores one observation's item override; `ConfigureFinancialValues((value, index, item) => ...)` materializes a complete callback result for every typed observation. Callbacks run while configuring the model rather than during rendering. A thrown callback leaves installed styles unchanged, and retained callback references cannot mutate the installed snapshot. Explicit per-datum parts override rising/falling defaults; an observation's `PointColors` remains the highest color override. Indexed value updates and keyed financial transactions preserve the surviving datum style; full financial replacement clears it with the other observation metadata. OHLC retains its stroke-only constraints.

HTML financial tooltips expose Open, High, Low and Close rows, including zero prices, through the ordinary and pinned readout. Shared-X mode uses the existing single-target fallback for financial observations. Financial readouts retain raw metadata and accessible descriptions and omit a generic Y row, which would otherwise describe only the opening member.

## Bubble size scale

`ChartOptions.Bubble` owns one size domain for every bubble series, including series on the secondary Y axis and series with hidden glyphs. Automatic bounds use the minimum and maximum positive source sizes across those series; empty typed series contribute no extent. Equal source sizes map to equal reference-circle radii under the selected encoding. Adding an observation outside the automatic domain changes that domain. Pin both the domain and radius endpoints when sizes must remain stable across additional series or viewport changes.

Its getter-owned `SizeDimension` keeps the measurement's `Name`, `ValueFormat` and optional numeric key together. Configure it inside `ConfigureBubble`; the successful callback installs a detached copy. A null name uses `ChartLabels.Size`, and a null format uses the chart value format. Source size, Cartesian Y and independent color measurements retain separate names and formatting. Preparation captures each distinct size caption once; native labels, key captions and HTML readouts reuse it, including an explicitly empty formatter result. Raw positive sizes and observation identities remain unchanged.

Set `SizeDimension.ShowLegend = true` to draw a numeric size key; the default is false. Chart/frame legend visibility remains the master switch. The key shares the categorical and color-key layout budget and uses the reference-circle radii resolved for the final plot and encoding. An oversized explicit key reports `cartesian.size-scale-overflow` and retains its complete facts instead of shrinking the authored marker radii.

```csharp
chart.ConfigureBubble(bubble => {
    bubble.WithSizeDomain(0, 100);
    bubble.MinimumRadius = 3;
    bubble.MaximumRadius = 24;
    bubble.SizeDimension.Name = "Affected assets";
    bubble.SizeDimension.ValueFormat = ChartValueFormat.Number("0");
    bubble.SizeDimension.ShowLegend = true;
});
chart.Series[0].ConfigureMarkers(markers => markers.Shape = ChartMarkerShape.Diamond);
```

`WithSizeDomain(minimumValue, maximumValue)` atomically installs finite, non-negative, increasing bounds; `MinimumValue` and `MaximumValue` expose them read-only. `UseAutomaticSizeDomain()` clears both. Source `ChartBubble.Size` values remain finite and positive. An explicit domain can start at zero, but does not admit zero-valued observations.

`MinimumRadius` defaults to six logical pixels. A null `MaximumRadius` uses 7.5% of the smaller final plot dimension, bounded between 14 and 32 pixels and raised to the minimum radius when needed. Explicit radius endpoints are finite and non-negative, and the maximum must be at least the minimum during preparation. Equal endpoints select a fixed reference-circle radius; two zero endpoints suppress bubble and legend glyphs while retaining source facts and legend text. Use `Markers.Enabled = false` to hide only one series.

The existing square-root interpolation of the radius range is preserved: for a nonconstant domain, `t = Clamp((size - minimumValue) / (maximumValue - minimumValue), 0, 1)` and `radius = minimumRadius + Sqrt(t) * (maximumRadius - minimumRadius)`. `Reversed = true` replaces `t` with `1 - t` before taking the square root. Sizes outside an explicit domain use its endpoint radii. A constant automatic domain uses the arithmetic radius midpoint, including when reversed. This mapping does not promise proportional painted areas or equal areas across different marker shapes.

Choose `bubble.SizeEncoding = ChartBubbleSizeEncoding.Area` to interpolate fill area instead: `referenceRadius = Sqrt(minimumRadius² * (1 - t) + maximumRadius² * t)`. Each built-in marker is normalized to the fill area of that reference circle, including the pin's hole; a noncircular marker may extend farther than the reference radius. A zero domain minimum and zero minimum radius make fill area proportional to source size. Other endpoints interpolate the authored area range. A constant automatic domain uses the area midpoint. Stroke widths, raster antialiasing and clipped portions do not change the encoded fill area. The size key's circles show the same area; `SquareRootRadius` remains the default encoding.

Preparation snapshots the domain and radius settings once. Mark drawing, automatic paint bounds and legend glyph eligibility use that snapshot; the ordinary legend remains a fixed-size style key. Prepared SVG, native PNG, descriptive regions and raw `data-cfx-size` metadata stay detached from later source or option changes. Point-label overrides still change display text without changing raw sizes.

## Sunburst secondary captions

`ChartSunburstOptions.SecondaryLabelFormatter` supplies optional authored text beneath each primary caption. Keep text in your own data keyed by `ChartHierarchyItem.Id`; the shared hierarchy input and its numeric size and color observations stay independent.

```csharp
var captions = new Dictionary<string, string> {
    ["engineering"] = "Delivery teams",
    ["operations-support"] = "Shared service"
};
var chart = Chart.Create().WithDataLabels()
    .ConfigureSunburst(options => {
        options.SecondaryLabelFormatter = context =>
            captions.TryGetValue(context.Item.Id, out var text) ? text : null;
        options.SecondaryLabelStyle.FontSize = 10;
        options.SecondaryLabelSpacing = 2;
    })
    .AddSunburst("Allocation", hierarchy);
```

`ChartSunburstLabelContext.Item` is the copied authored item, whose nullable `Value` retains the supplied size. The context's `Value` is the size resolved under `ParentValuePolicy`; `FormattedValue` uses the existing chart value format. A formatter can include that formatted size deliberately, but its returned text never becomes a numeric observation.

The formatter runs once for each compiled node, including nodes with hidden captions, zero sizes or collapsed geometry. Null, empty or whitespace output suppresses secondary text and preserves the primary-only SVG and PNG. Preparation snapshots text and resolved styles; later changes to a callback, its captured data or styling do not change a prepared export. Formatter exceptions propagate.

Secondary typography inherits chart, series and ordinal node data-label styling at 80% of the primary font size, with a ten-pixel minimum no larger than the primary caption, then applies `SecondaryLabelStyle`. Explicit secondary font sizes and colors override these defaults. `SecondaryLabelSpacing` defaults to two logical pixels and accepts finite, nonnegative values. Both runs share horizontal or upright tangential placement and fit inside the actual sector, including rounded corners. Primary content takes priority: secondary text wraps, shortens at text-element boundaries or is omitted before the primary caption loses content. Font sizes stay fixed by default. Optional `MinimumLabelFontSize` permits complete primary captions to shrink to an explicit positive floor; `SectorGap` adds native logical-pixel spacing without changing source allocation. See [hierarchy views and Sunburst fitting](../hierarchy-views.md#sunburst-caption-size-and-sector-spacing).

Full secondary text remains in `data-cfx-secondary-label` and descriptive node regions even when no caption fits. The optional HTML adapter exposes it as plain text in pointer and keyboard tooltips, separately from size, color, supplied totals and remainders. IDs, numeric metadata and target counts stay unchanged. A non-null formatter on a populated non-Sunburst chart fails preparation; style or spacing configuration without a formatter remains inactive. Treemap retains its own label and numeric-value captions.

## Logarithmic scales

`ChartAxis.LogarithmBase` accepts a finite value greater than one for both `Logarithmic` and `SymmetricLogarithmic`. Base ten remains the default. A logarithmic axis explicitly selects `ChartLogarithmicDomain.Positive` or `Negative`; it never guesses a sign or discards opposite-sign observations. Zero, opposite-sign values and bounds outside the selected domain reject before authored clipping can hide them. Bounds remain increasing source coordinates, so a negative domain can use `-64` through `-1`; `Reversed` independently changes its screen direction.

The [compiled native axis recipe](../../ChartForgeX.Examples/WebsiteRecipes/NativeAxisWebsiteRecipe.cs) shows positive and negative base-two domains in one ordinary console program. The website presents its exact SVG and native PNG output beside the complete downloadable source.

`WithLogarithmicScale(base, domain)` selects scale, base and sign in one validated operation. Clear contradictory existing bounds before changing their sign, or use the chart's transactional `Configure*Axis` callback to install a coherent candidate. Symmetric logarithmic axes retain their existing linear threshold, accept either sign and ignore `LogarithmicDomain`; set their `LogarithmBase` when a base other than ten is needed. Other mathematical scales retain the configured base without applying it. Axis formatting, raw values and source identity remain independent of the transform.

## Cartesian viewports

`ChartCartesianViewport` is the single request accepted by native and HTML preparation. Its optional `x`, `y`, `secondaryX` and `secondaryY` properties use the same immutable `ChartAxisWindow(minimum, maximum, maximumInclusive: true)`. At least one finite increasing window is required; pass null to `Prepare` or `PrepareSection` for full-domain reset. These are physical coordinates: a horizontal chart's categories occupy Y and its value axes occupy X/SecondaryX. A vertical chart's categories occupy X and its values occupy Y/SecondaryY. A secondary window requires a series assigned to that physical value-axis slot. Non-Cartesian, empty and sparkline charts do not expose native viewport projections.

```csharp
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

var counts = ChartSeries.CreateLine(new ChartSeriesId("counts"), "Counts",
    new[] { new ChartPoint(0, 20), new ChartPoint(5, 40), new ChartPoint(10, 30) });
var latency = ChartSeries.CreateLine(new ChartSeriesId("latency"), "Latency",
    new[] { new ChartPoint(0, 300), new ChartPoint(5, 900), new ChartPoint(10, 1500) })
    .UseSecondaryValueAxis();
var chart = Chart.Create().WithSize(800, 440).WithXAxisBounds(0, 10)
    .WithSecondaryYAxisBounds(0, 2000).AddSeries(counts).AddSeries(latency);
var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 440)));
var viewport = new ChartCartesianViewport(
    x: new ChartAxisWindow(2, 8),
    secondaryY: new ChartAxisWindow(200, 800),
    valueDomain: ChartValueDomain.VisibleCategories);
var prepared = chart.Prepare(context, viewport);
var svg = prepared.ToSvg();
var png = prepared.ToPng();
```

`ChartSeries.ValueAxis` selects `ChartAxisSide.Primary` or `Secondary` independently of orientation. `UsePrimaryValueAxis()` and `UseSecondaryValueAxis()` select that slot. Configure physical `XAxis`, `YAxis`, `SecondaryXAxis` or `SecondaryYAxis` on `ChartOptions`; the matching `Configure*Axis`, `With*Axis`, `With*AxisBounds` and `With*AxisScale` methods retain transactional validation. Source drawing `SourceYAxis` and plot-region `YAxis` remain physical Y policies; they do not become orientation-dependent series assignments.

The default `ChartValueDomain.FullData` retains each unrequested axis's resolved full-data range. Set `valueDomain: ChartValueDomain.VisibleCategories` to fit unrequested value axes to geometry intersecting the current category window. Each primary/secondary value axis fits independently. Explicit value windows override fitting only on their own axis, and authored axis bounds remain authoritative. Continuous line, step, smooth and paired-boundary geometry contributes its clipped values, including crossings between observations, without bridging authored gaps. Financial and discrete interval observations contribute their whole value extent when their source/category slot intersects; error bars use their independent X interval. Histograms use actual bin bounds. An empty category window retains that axis's full domain. Full-data stacks, normalized totals, color domains, source facts and observation identities remain unchanged.

All available physical projections capture the selected [logarithmic policy](#logarithmic-scales) and [time spacing](#explicit-time-spacing). Browser inversion uses those captured transforms rather than a second scale configuration.

`ChartAxisWindow.MaximumInclusive` defaults to true. Set it to false to exclude the upper source endpoint while retaining its exact value. The lower endpoint remains inclusive. Interval/line geometry approaching or crossing an excluded endpoint remains clipped at the same physical edge; excluding an endpoint does not move the plot or subtract a floating-point epsilon. Calendar gestures return exact canonical boundaries with an exclusive upper edge, so preparing or transporting that window repeatedly cannot add the next band. Unit spacing accepts an exclusive maximum only at an exact captured calendar boundary; an interior exclusive maximum is rejected. Default inclusive raw Unit windows still select complete bands. A gesture confined against an authored inclusive full maximum inside a band retains that exact raw maximum and its inclusive policy; it does not invent an interior exclusive edge. Continuous, logarithmic and ordinal windows accept exclusive finite maxima normally. Preserve the property when copying a window or translating native JSON.

`PreparedVisual.XProjection`, `YProjection`, `SecondaryXProjection` and `SecondaryYProjection` expose detached `ChartAxisProjection` snapshots where supported. Each records physical `Dimension`, `Side`, full/window domains, `MaximumInclusive`, plot bounds, scale and reversal. `Project` and `Unproject` map exact captured source coordinates. `ConstrainViewport` returns a `ChartAxisWindow`, or null for the complete domain, and shifts an edge-crossing transformed span intact instead of shortening it. Explicit authored requests remain strictly contained in the full resolved domain. The prepared `CartesianViewport` records the immutable request; it does not retain mutable axes or formatters.

Alternate native drawings enclose every captured physical axis in one common full-domain envelope while preserving authored ends exactly. Windows reuse that envelope and clip paint to the plot. Native SVG and PNG consume the same detached scene; changing the model afterward cannot change projection, readout or export results. Complete source observations remain available even when their marks are outside the viewport. See [native viewport interaction](../interactivity.md#native-y-and-xy-windows) for the existing managed callback and lifetime.

## Explicit time spacing

A time axis retains elapsed spacing by default. `axis.WithTimeSpacing(ChartTimeSpacing.Ordinal)` spaces its distinct supplied UTC instants equally; omitted observations occupy no slots. Source observations, source IDs, callback values and exports retain their original instants. Coordinates between supplied instants use piecewise interpolation, so ordinal spacing differs from `TimeSessions`, which preserves elapsed distances inside explicitly included sessions.

For example, these two observations occupy January and March, with an empty February band:

The same [compiled native axis recipe](../../ChartForgeX.Examples/WebsiteRecipes/NativeAxisWebsiteRecipe.cs) renders its two UTC observations with both ordinal and calendar-month policies. Both views retain the original January and March instants and values; only their native spacing policy changes.

Use `axis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month)))` for equally wide calendar bands, including months without observations. The axis display zone aligns the existing calendar boundary schedule; days across DST and months of different lengths retain equal band widths. Unit spacing does not aggregate: each series may supply at most one observation per band. Multiple series can share a band while retaining distinct raw source instants. The existing interval units, including `Year`, are supported; no automatic unit inference occurs.

Unit time is discrete: observations paint at their containing band's center. Its inverse projection selects the canonical lower calendar boundary instead of pretending that different instants inside a band have different positions. Source viewport endpoints remain raw instants and enclose their complete calendar bands; logical gestures select enclosing calendar boundaries. Ordinal inverse projection interpolates actual supplied instant coordinates. Both policies are captured independently in every available physical-axis projection, including secondary value axes. The authored model is unchanged.

Discrete spacing accepts UTC instants represented by OLE dates on or after 1899-12-30, and cannot combine with `TimeSessions` or authored time-bucket intervals. Captured ordinal domains allow at most 100,000 distinct supplied instants; calendar domains allow at most 100,000 bands. A larger domain fails explicitly before layout instead of allocating an unbounded calendar or silently aggregating data. Tick selection remains bounded and measures real captions. Null `TimeSpacing` restores elapsed spacing.

## Enforcement boundary

`V2ApiConventionTests` checks the reviewed immutable contracts, their operation roles, canonical color/severity types, core-only public signatures, in-memory export signatures and detached request/output lifetime. It also checks the selected mutable chart bridge's `With*`, `Add*` and `Configure*` behavior. Public typed configuration callbacks on the Core and Visuals `With*`/`Configure*` builder boundary must use `Configure*` and return their receiver. Data-adding callbacks and substantive exports keep their operation names. Focused family and diagram fixtures protect preparation, retained semantics and explicit limits. These are compiled API and observable-output checks; they do not read this document or enforce editorial wording.

Each migrated family expands the reviewed API selection and adds its own data/layout/output fixtures. The old surface remains inventoried rather than being subjected to a blanket prefix rule during a partial migration. Do not weaken a current contract merely to pass the naming test, and do not claim that a naming check proves visual quality, package publication or consumer compatibility.

The complete [partial mixed radial recipe](../../ChartForgeX.Examples/Recipes/PartialMixedRadialRecipe.cs) compares category columns with a curved Radar reference in a three-quarter annular sector. Category centers, value axes, stacks, source identities and prepared exports keep their existing owners.


### Continuous Polar source windows

`Chart.PreparePolar(context, viewport)` prepares continuous `Polar` series with an immutable `ChartPolarViewport`. `angle`, `radius` and `secondaryRadius` are separate source-coordinate windows; they are not physical Cartesian x/y axes. Null restores every full domain. Category Radar, PolarArea and numeric-radial mixtures reject this directional API. Their existing preparation APIs remain unchanged.

Angles are authored radians. An increasing unwrapped interval of at most `2π` clips directions without remapping or stretching bearings. For example, `new ChartAxisWindow(5.5, 6.8)` crosses the zero ray. Negative and multi-turn observations retain their raw values, original order, readouts and IDs; equivalent bearings intentionally coincide. A full turn includes every direction. Shorter intervals honor `MaximumInclusive` for source marks, while the painted sector retains its mathematical boundary.

Radius windows select independent primary or secondary radius axes and preserve the selected scale and reversal. A window crops the captured full-domain geometry into an annulus: the outer requested boundary is uniformly magnified to the drawing radius, and the inner requested boundary remains an aperture. For a full linear 0..100 domain, a 20..80 window has an inner radius of one quarter of its outer radius. For a full logarithmic 1..1000 domain, a 10..100 window has an inner radius of one half of its outer radius. Reversal selects the same geometric bounds in the opposite source direction. Each radius axis retains its own aperture.

`PreparedVisual.PolarProjection` captures the center, rotation, raw domains and transforms. `GetRadiusWindow` reads current/full source domains; `GetInnerRadius` and `GetOuterRadius` read the selected axis's logical crop radii. `ConstrainRadius` uses the existing captured scalar-axis confinement owner. `Project` maps the original full-domain radius through the uniform magnification; an outside-window point can lie inside the aperture or beyond the outer crop. `Contains` reports source-window membership separately. Direct preparation rejects out-of-domain and numerically unrepresentable requests rather than changing authored bounds or manufacturing a minimum span.

The original typed path is constructed from the captured full-domain scale. One uniform transform moves its endpoints and cubic controls together; the shared annular-sector paint clip then crops the unchanged line or fill. Curve tangents, disconnected subpaths and closed-area boundaries are preserved instead of rebuilding an interpolated curve from clamped vertices. No below-minimum observation is extrapolated to a negative radius or reflected onto the opposite bearing. A crossing remains visible even if none of its source observations lies inside the window. Out-of-window source marks are unpainted and cannot become HTML pointer or keyboard targets; their facts, identities, order and descriptive regions remain captured. Full inclusive source-radius windows and null reset retain the original full view.

The nearer projected boundary remains at its transformed full-domain distance as an inner aperture rather than becoming a nonlinear radial warp to the center. SVG retains the original cubic commands and shared clip. Native PNG uses the same typed geometry and the renderer's existing bounded curve/clip flattening at paint resolution. This does not claim arbitrary SVG/backend pixel identity or an exact analytic circle at infinite precision. Sector masking, per-axis annular cropping and source filtering remain distinct operations.

`HtmlInteractiveChartRenderer.PreparePolarSection` returns matching native and HTML observations, including both responsive drawings. An existing host bridge can connect `controller.connectNative(callback, { basis, coordinateSystem: 'polar', axes: ['angle', 'radius', 'secondaryRadius'] })`, request `controller.setPolarViewport({ angle: { minimum: 5.5, maximum: 6.8 }, radius: { minimum: 20, maximum: 80 } })`, and read `getPolarViewport()`. Managed replies use the same scene revision, opaque basis, cancellation and atomic responsive acceptance owner as other native preparations. `connectPolarViewport` is the corresponding unmanaged HTML-string callback surface. Reset, bookmarks, exports, legends and source readouts retain that committed native scene. Each returned radius window also reports its derived `innerRadius` and `outerRadius` in logical units; these are native facts, not caller-supplied geometry controls. Automatic gestures remain off unless the connection explicitly sets `gestures: true`; see [native Polar gestures](../interactivity.md#native-polar-gestures).

The complete [Polar viewport recipe](../../ChartForgeX.Examples/Recipes/PolarViewportRecipe.cs) exports the shared native view in compact/wide and light/dark poses with `--polar-viewport-only`. Source, native and browser regression fixtures are declared for this increment; execution and physical qualification remain pending until the final candidate is built and observed.

`ChartPolarProjection.Zoom(factor, logicalAnchor, axes)`, `Zoom(factor, axes)` and `Pan(clockwiseAngleDelta, outwardRadiusDelta, axes)` return the next immutable `ChartPolarViewport`, or null for a complete view. `ChartPolarNavigationAxes` selects Angle, Radius and/or SecondaryRadius; None, unknown bits and unavailable axes fail explicitly. These methods use detached captured policy and retain every unselected source window. They do not alter the source chart, accepted drawing or a host session.

Zoom factors greater than one narrow the selected windows around an eligible logical anchor. The overload without an anchor selects the captured sector midpoint and the midpoint between the largest selected inner aperture and the outer radius; toolbar requests omit the anchor so that the native projection owns this choice. The anchor must lie in the visible sector and every selected annulus, away from the undefined center bearing. Angular zoom changes the directional crop without stretching bearings. Radial zoom and pan reuse the scalar transform/confinement owner, including selected logarithmic and reversed domains. The radial anchor chooses crop bounds; uniform annular magnification does not pin an arbitrary source point to its former screen position. Pan angle deltas are clockwise screen radians, independent of authored angle reversal; positive radial deltas are outward logical drawing units and move the current transformed span toward the inner direction before confinement. Angular pan moves the sector aperture, not the source geometry. Radius pan does not promise a nonlinear grab-point-preserving warp. A numerically collapsed or unrepresentable request fails before any prepared view is installed.

## Editable Waterfall interchange

Waterfall uses the existing version-1 visual-artifact envelope with family `Waterfall` and `VisualArtifactInterchangeWaterfallArtifact`. Save a single exclusive waterfall through `chart.ToVisualArtifact(id).ToInterchangeUtf8Json()`, read with `VisualArtifactInterchangeEnvelope.FromUtf8Json(bytes)`, and rebuild an editable model with `ToWaterfallChart()`.

```csharp
var source = Chart.Create().AddWaterfall("Movement", new[] {
    ChartWaterfallItem.OpeningBalance(1, 120),
    ChartWaterfallItem.Delta(2, -30),
    ChartWaterfallItem.Subtotal(3),
    ChartWaterfallItem.Total(4)
});
var bytes = source.ToVisualArtifact("movement").ToInterchangeUtf8Json();
var saved = VisualArtifactInterchangeEnvelope.FromUtf8Json(bytes);
var editable = saved.ToWaterfallChart();
editable.Series[0].UpdateWaterfallItem(1, ChartWaterfallItem.Delta(2, -35));
// The subtotal and total are recalculated from the imported authored items.
var png = editable.ToPng();
```

`TypedItems` keeps the two native authoring modes distinct. Its `Items` collection contains immutable opening/delta/subtotal/total items, including explicit checkpoints, and `DeltaPoints` stays empty. When false, only `DeltaPoints` carries numeric changes; the native accumulation owner appends its calculated final total. Calculated balances, connector geometry and derived contributor lists are never serialized as invented source observations. Editing numeric points therefore produces one recalculated final total. Replacing typed items remains atomic and retains native opening placement and distinct-coordinate validation.

Series IDs, their explicit or chart-local scope, sparse observation IDs, input order, point paints/patterns/labels and caption typography survive import. Typed checkpoint IDs still refer to authored display items, not numeric contributors. The appended numeric total can have paint or a custom caption but cannot acquire an authored observation ID. Imported automatic IDs remain local to the new chart; subsequent native ID allocation continues after their restored sequence.

The portable presentation subset includes the complete native theme, frame ownership and configured padding, orientation, primary/secondary physical axes and titles, static category captions and parent groups, authored bounds/reversal/ticks/rule or complete-axis crossings, built-in Number/Compact/default formatting with unmodified named cultures, native bar/grid treatments, text roles, caption connectors and static localized readout words. Optional and explicit axis policies remain distinct, so default axes retain their existing edit behavior. Import detaches mutable collections and style objects; later source, envelope or imported-model edits do not alter earlier prepared exports.

Callbacks are rejected without invocation. Local font paths/face selection, custom number cultures, SVG variable resolvers, annotations/plot regions, compact host state and prepared viewport or time-zone/session/discrete-time axis state are outside this editable payload and reject explicitly. Use supported native configuration after import, or retain the separately rendered fallback when that additional presentation is required. Caller metadata and source-language facts stay on the envelope and are not promoted into chart data. Waterfall remains an exclusive single-series surface. This payload does not introduce general chart-series interchange; other unsupported charts retain family `None`.

Portable data is bounded to 50,000 authored items, 10,000 labels per physical axis, 32 parent captions per label and the existing JSON byte, depth, ID and value limits. Validation also accounts for native contributor-index references within the remaining 600,000-value aggregate budget before allocating checkpoint contributor arrays. Native authoring outside interchange keeps its established unrestricted contributor policy; many cumulative total checkpoints can still require quadratic contributor storage.

The source-owned Waterfall `portable`, `compact-portable`, `portable-horizontal` and `compact-portable-horizontal` recipes execute the save/load/edit path. Their native/browser fixture sources define qualification; source support does not by itself establish runtime, gallery, package or consumer acceptance.


### Map callouts, grid badges and route clearance

`chart.Options.MapCallouts = new ChartMapCallouts(48, ChartMapRegionLabel.Name)` opts enabled map captions into the existing measured label placement and straight-leader owner. The maximum distance is measured from the source anchor to the nearest label edge in logical scene units. Region captions can use their code, name, or both. Existing series data-label typography and placement still apply; shortened or omitted labels retain the complete source description. This is bounded collision-aware placement, not a guarantee that every dense map caption fits.

`series.WithMapClustering(new ChartMapClustering(cellSize: 48, minimumCount: 2, radius: 14))` applies only to DottedMap. Visible observations in each map-origin screen-grid cell become one fixed-radius **count** badge. Different output sizes or viewports can change grid membership; this is not geographic distance clustering. Badges use the series paint and do not average weights or numeric colors. All raw observations, observation IDs, feature JSON, weight/color measurements and full-data key domains remain captured. Cluster source groups carry derived grid identity, contributor indices and count; they do not borrow an individual observation ID. Hidden raw source groups remain descriptive facts and cannot become keyboard or pointer targets.

`connector.WithEndpointClearance(4)` crops the final native curve against endpoint-marker disks in all composed DottedMap peers, including their actual grid-badge centers. The gap is in logical scene units, additional to marker/route outline clearance. Cubic interiors and tangents remain native curves rather than sampled polylines. Root isolation uses bounded double-precision arithmetic; this is not symbolic exact arithmetic. Complete route waypoints and feature metadata remain source facts. If the endpoint disks cover the whole route, its source group is nonvisible and a `map.route-clearance-collapsed` diagnostic explains the absence of paint. Clearance applies to leading/trailing endpoint disk interiors; it is not subtraction of every later route/marker crossing. Null restores the preceding route geometry. The `map-navigation-depth` recipe demonstrates all three opt-ins in compact/wide light/dark scenes.


### Explicit antimeridian point/route windows

`ChartMapViewport.AcrossAntimeridian("Pacific", 170, -170, -30, 10)` creates a 20-degree increasing longitude window across +180/-180. The existing ordered constructor remains ordered and unchanged. `MinimumLongitude` and `MaximumLongitude` keep the raw western/eastern bounds; use `CrossesAntimeridian` and `LongitudeSpan` to describe the window. The factory optionally selects the existing `Equirectangular` or `WebMercator` projection; Mercator keeps its existing polar latitude clamp.

Every point's raw longitude, latitude, observation ID, feature JSON and measurements remain captured. Equivalent +180/-180 observations intentionally share a painted bearing and remain distinct source observations. Off-viewport points are nonvisible source facts, not removed observations. Consecutive route waypoints unwrap shortest signed longitude differences; exact half-turn ties retain their authored sign. The native connector remains the existing projected curve, not a geodesic. Partially crossing routes retain all raw waypoints and are painted through the same typed rectangular clip in SVG and PNG. The view does not rewrite source endpoints or filter an outside waypoint. A route wholly outside the window retains a nonvisible source group.

The explicit seam contract supports DottedMap peers, matching seam-aware RegionMap/context definitions and independent TileMap insets. RegionMap or context geometry without matching seam-aware source paths, and explicit projected `RegionMapBounds`, fail clearly instead of being silently misaligned. Existing land-dot decoration uses the same point projection; it is not seam-aware polygon context. The complete `dateline-map` source recipe includes compact/wide light/dark variants and both existing projections.


### Seam-aware polygon import

Set `ChartMapGeoJsonOptions.PolygonViewport` to an explicit `ChartMapViewport.AcrossAntimeridian(...)` using the same `Projection`. The existing GeoJSON importer unwraps each complete closed ring with the shared point/route projection, using consecutive shortest signed longitude differences and retaining exact half-turn signs. Holes remain compound even-odd contours; disconnected MultiPolygon members keep their own interiors. Where a near-world window meets both sides of one polygon, the importer emits the necessary periodic copies of the complete polygon and its aligned holes. Native SVG and PNG use the existing typed rectangular clip. The importer does not trim source coordinates or construct replacement polygon intersections.

The immutable `ChartMapDefinition.PolygonViewport` declares the exact longitude/latitude window and projection already used by those paths. All RegionMap peers and base/overlay definitions must declare the same window as the chart; a different display name is harmless. Missing or mismatched declarations reject before rendering. `WithPolygonViewport` is a declaration for already prepared paths, not a transform. Explicit projected `RegionMapBounds` remain incompatible with a seam view. The ordinary importer and ordered viewport retain their previous output when `PolygonViewport` is null.

Every seam-aware imported `ChartMapRegion` retains immutable `Feature` metadata and `GeoJsonGeometry`: complete original rings, additional coordinate dimensions, typed IDs, property-name case and original numeric spelling. The JSON snapshot is serialized by the existing bounded JSON owner; whitespace/escape spelling is normalized. Feature IDs remain separate from region observation IDs. An outside feature remains a nonvisible source group; the view does not filter it. Existing explicit feature/property/coordinate import filters still filter source features when requested. Source facts and full value/key domains survive paint clipping. `WithFeature(null)` clears metadata without changing geometry, raw rings or pixels.

This planar contract requires explicitly closed rings with at least four positions, finite geographic coordinates and an unwrapped span smaller than one full turn. A closed ring with nonzero globe winding rejects clearly; polar caps/global winding need a separate spherical interior policy. Existing WebMercator latitude clamping remains in force. It is not a geodesic or arbitrary projection engine. The existing JSON input/value/depth bounds apply before parsing; `MaximumObservations` bounds emitted vertices including periodic copies, and `MaximumExpandedFeatureCharacters` bounds geometry plus feature snapshots before output expansion. Coordinate-filtered features consume neither output budget.

The complete `seam-polygon-map` source recipe provides compact/wide light/dark variants in both supported projections, with a seam-crossing hole, disconnected islands, raw outside feature, point peer and route. Runtime fixtures supply the observed export/interaction proof; source authoring alone does not establish browser or physical acceptance.

## Ordered stage presentation

Funnel and Pyramid keep their ordered source values and existing geometry. `ChartFunnelOptions.LabelContent` and `ChartPyramidOptions.LabelContent` default to `Combined`, retaining the existing complete category/value caption and Funnel ratio layout. `Separate` enables independently configured `CategoryLabel` and `ValueLabel`; Funnel also exposes `RetentionLabel` and `DropOffLabel`. These components reuse `ChartValueLabelOptions` for visibility, physical placement and typography. Chart or series data labels must be enabled.

A null component placement inherits the series/chart placement. When that policy is `Auto`, category and Funnel metrics prefer an outside rail, while values prefer the mark interior. `Outside` means right for vertical stages and below for horizontal stages. Explicit Left/Right/Above/Below are physical sides. Independent outside rails share the existing forty-percent cross-axis reservation policy; measured requests use the same native label placement and collision owner as other charts. Complete captions can be omitted when the requested frame cannot contain them without collision. Omitted text retains a descriptive region and a family `label-hidden` diagnostic; source categories, values and baseline-defined flags remain unchanged. No new ratio is fabricated for an undefined baseline. Increasing stages can have negative drop-off percentages.

Automatic `Separate` captions placed Inside/Center use the surface actually behind their placed bounds. Cone has stage lines rather than filled stage partitions: the first caption uses the configured frame, while later captions use any visible incoming connection with its source-owned color and alpha overrides. A hidden connection exposes the frame. Translucent filled partitions composite onto that same frame; positive opaque partitions retain their ordinary contrast policy. Explicit component, chart, series and observation text colors remain authoritative.

When a contained Cone caption crosses incompatible surfaces or a hatch, it reuses the native glyph-stroke halo with complementary opaque ink and outline in SVG and PNG. This is a readability fallback, not a claim that one computed color contrasts with every arbitrary pattern. The halo stays inside the existing caption clip and does not move or resize stage geometry. Transparent exports assume the configured theme background for automatic frame contrast; an unknown external host surface cannot be inferred.

`Funnel.StageOutline` and `Pyramid.PartitionOutline` accept the existing immutable `ChartStrokeStyle`. Their corresponding `*OutlineDashOffset` properties use finite logical units. A nonnull outline uses its width, or an explicit series stroke width, or one logical pixel; width zero disables the stroke. Cone uses its existing theme line width when no width is supplied. Point-specific `WithPointStrokeColor` takes precedence over the outline color and can enable an outline by itself. Existing point fill colors, alpha and hatch patterns retain their ownership and precedence. Outlines do not change partition boundaries or proportional values. Setting these properties leaves the default rendering branch unchanged until an outline is authored.

`ChartFunnelConnectorStyle` controls the painted region between adjacent authored stages: optional fill, alpha multiplier, pattern and shared outline, plus logical dash phase and visibility. Null fields inherit the existing form behavior (.18 fill alpha for StageBars, .3 for Cone; Cone inherits the source pattern). Explicit `None` disables a pattern. `options.ConnectorStyle` supplies a default; `series.WithFunnelConnectorStyle(fromSourceIndex, style)` replaces that default for the outgoing connection from one source. This is a whole immutable override, not a second stage paint map. Styles move with their originating observation through insert/remove and keyed updates, clear with replacement/clear, and can be assigned to the last source before appending another. Null restores the configured default. Hiding a connection affects paint only; its source endpoints, complete facts and semantic region remain captured.

```csharp
chart.ConfigureFunnel(options => {
    options.LabelContent = ChartStageLabelContent.Separate;
    options.CategoryLabel.Placement = ChartDataLabelPlacement.Right;
    options.ValueLabel.Placement = ChartDataLabelPlacement.Inside;
    options.RetentionLabel.Placement = ChartDataLabelPlacement.Left;
    options.DropOffLabel.Placement = ChartDataLabelPlacement.Left;
    options.StageOutline = new ChartStrokeStyle(width: 2, dash: new[] { 5d, 3d });
});
chart.Series[0].WithFunnelConnectorStyle(0, new ChartFunnelConnectorStyle(
    fillOpacity: .4, outlineStroke: new ChartStrokeStyle(width: 1.5)));
```

The complete [Funnel presentation recipe](../../ChartForgeX.Examples/Recipes/FunnelPresentationRecipe.cs) includes StageBars, Cone and horizontal variants. The [Pyramid presentation recipe](../../ChartForgeX.Examples/Recipes/PyramidPresentationRecipe.cs) includes area encoding, horizontal placement and reversal. Both have compact/wide light/dark selections and preserve zero-source stages. Cone remains a planar chart; these paint controls do not add a 3D rendering mode.

## Independent difference sources and nonlinear axes

`ChartDifferencePoints.Align(first, second, missingDataPolicy)` captures two ordinary Line, StepLine, Area or StepArea series once, then matches only equal numeric X coordinates. `Gap` breaks after unmatched union positions; `Connect` joins surviving pairs. Explicit source breaks always propagate, including a break on an unmatched point before the next match. Missing values are not replaced with zero or interpolated. Each input must have globally increasing unique X; duplicate or restarted X is ambiguous for this new join and rejects even at a break. Existing authored `ChartDifferencePoint` tuples keep their existing connected-segment ordering rules.

The immutable `ChartDifferenceAlignment` retains both complete raw point arrays, raw names, scoped series IDs, optional source observation IDs, boundary policies and matched source-index maps. Unmatched original data remains in those arrays. No destination/pair observation ID is invented. `CreateAlignedDifferenceArea` and `AddAlignedDifferenceArea` consume that capture; ordinary `CreateDifferenceArea`/`AddDifferenceArea` keep their tuple signatures. Raw empty source names remain captured, while readouts use First/Second fallbacks. Source interpolation is captured, without copying a source chart's axes, themes, marker paint or layout. Both raw Y sources are interpreted in the destination's selected value domain. Callers serialize access while capture runs.

```csharp
var aligned = ChartDifferencePoints.Align(actualSeries, forecastSeries,
    ChartMissingDataPolicy.Gap);
var comparison = ChartSeries.CreateAlignedDifferenceArea(
    new ChartSeriesId("comparison"), "Actual against forecast", aligned);
var chart = Chart.Create().AddSeries(comparison);
chart.Options.YAxis.WithLogarithmicScale();
```

Native matched facts carry bounded original indices/IDs for both roles. Complete input arrays remain on the immutable owner rather than being repeated as large metadata strings. Assign destination observation IDs explicitly when selection should survive a host update. Re-align changed sources and use `ReplaceDifferenceAlignment`; it clears destination observation presentation/IDs through the existing typed replacement lifecycle and captures source names/boundary policies anew. Individual/keyed edits of an aligned derived tuple reject. Successful ordinary `ReplaceDifferencePoints` deliberately leaves derived mode; a failed replacement leaves the old alignment installed. Previously prepared SVG/PNG remains detached from both sources and later replacements. Derived or decimated input series must be rebuilt from complete source observations before alignment.

DifferenceArea accepts numeric Linear, positive/negative Logarithmic and SymmetricLogarithmic value axes, including selected secondary axes and reversal. X accepts the existing numeric and date/time scales. Selected logarithm-domain guards validate all matched raw values, including clipped ones; there is no zero fallback. Automatic destination domains use matched tuples. Unmatched extrema remain in the raw captures without being projected or silently expanding that domain. A date/time value axis rejects. Vertical, nonstacked difference semantics remain unchanged.

Nonlinear fills split at the intersection of the same transformed paths that are actually painted. They do not use raw-value interpolation to guess a screen crossing. Linear default output retains its existing branch; smooth/independent boundaries reuse the existing flattened crossed-path owner. That approximation is a rendering contour, not an inserted source observation. Raw First/Second values and source polarity remain unchanged under reversal, nonlinear scales and clipping. The complete [aligned difference recipe](../../ChartForgeX.Examples/Recipes/AlignedDifferenceRecipe.cs) includes compact/wide light/dark, negative-log, symmetric-log and explicit Connect selections.

### Captured pictorial artwork and fractional direction

`WithPictorialArtwork(ChartPictorialArtwork.FromImage(image))` captures RGBA pixels once. `FromSvg(svg, width, height)` uses strict native SVG rasterization at those explicit pixel dimensions. The accepted SVG subset is captured as an image; the original markup is not exported and vector preservation is not implied. The factories perform no file or network access. The shared SVG parser's document-size, depth and DTD restrictions remain in force; unsupported visible content rejects the capture.

Artwork is centered with its source aspect ratio inside each symbol slot and keeps its own colors. It takes precedence over the configured built-in shape or custom contour. `WithPictorialArtwork(null)` restores that retained symbol without changing columns, units, labels, paint or spacing. The immutable resource is safe to retain across preparation; changing the source pixel array or later chart options does not change an existing prepared artifact. Source images are bounded to 8192 pixels per side and 16,777,216 pixels before allocation. These resource limits do not promise a whole-export encoder memory bound.

`WithPictorialFillDirection(...)` chooses `LeftToRight` (the unchanged default), `RightToLeft`, `BottomToTop` or `TopToBottom`. Built-in/custom contours and artwork share one rectangle clip owner. Image fractions measure its fitted image rectangle; contour fractions measure the existing symbol rectangle. Fractions retain the original numeric value and do not claim proportional painted ink for arbitrary shapes or transparency. Unfilled artwork uses the existing `PictorialEmptyOpacity`; per-row colors do not recolor its captured pixels. The complete [pictorial artwork recipe](../../ChartForgeX.Examples/Recipes/PictorialArtworkRecipe.cs) includes compact, directional, image and custom-contour examples.

## Ordered topology rows and columns

`TopologyChart.Alignments` holds ordered `TopologyAlignment` declarations. Each has an exact stable `Id`, a `TopologyAlignmentKind`, an ordered `NodeIds` list and inert `Metadata`. A `Row` shares body center-Y and advances left-to-right; a `Column` shares body center-X and advances top-to-bottom. These are global viewport axes in every layout direction. Declarations may overlap when their equalities and order are compatible. They retain node identity and source collection order.

```csharp
var chart = TopologyChart.Create()
    .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
    .AddAutoNode("api", "API")
    .AddAutoNode("queue", "Queue")
    .AddAutoNode("worker", "Worker");
chart.Alignments.Add(new TopologyAlignment("request", TopologyAlignmentKind.Row,
    new[] { "api", "queue" }));
chart.Alignments.Add(new TopologyAlignment("execution", TopologyAlignmentKind.Column,
    new[] { "queue", "worker" }));
var svg = chart.ToSvg();
```

Automatic declarations use the existing Layered placement preferences, measured native node footprints and real group shells. Native spacing and measured group headers remain reserved. The bounded constraint solve commits coordinates once; existing final routing and common SVG/PNG scene owners consume them. Partially positioned automatic charts, authored `layer` metadata and edge `MinimumRankSpan > 1` are an explicit unsupported combination in this initial solver. Other automatic layout modes reject declarations. Use Manual to validate supplied geometry. `AddHierarchy` supplies authored `layer` metadata; a caller choosing global alignment instead must deliberately remove those rank hints before automatic constrained preparation.

Compartment placement, alignment order and painted separators inherit the nearest ancestor's explicit `LayoutDirection`, then the chart direction. Reversed flow changes local rank preferences while compartment source order and the global Row/Column axes remain stable.

Final aligned Orthogonal and explicit ObstacleAvoidingOrthogonal routes retain an already body-clear path. If the final named/fanned/shape endpoints cross a foreign visible node body, the same router reconsiders its existing corridors with obstructed endpoint leads shortened. Attached endpoints and authored port sides remain authoritative; real body clearance ranks before padded obstacle/caption/label scores. Authored waypoints retain their route, and a crowded scene without a feasible existing corridor retains best-effort routing.

Manual validation checks body-center equality, authored member order, body/sibling non-overlap and actual group containment. It does not reconstruct an unrecorded typography gap or move invalid members into place. Ordinary prepared JSON import remains Manual and retains its scene-coordinate contract. Native hidden nodes retain semantic constraints but add no painted collision obligation. Filtered views project surviving members in authored order and drop declarations with fewer than two survivors; source declarations remain unchanged.

The deterministic per-solve caps are 4,096 separation decisions and 8,000,000 cumulative work units, including failed branches. Resource exhaustion is distinct from an exhausted infeasible constraint system; it never establishes a conflict or returns partial placement. A native resource exception retains a structured `alignment-resource-limit` inner `TopologyValidationException`; genuine conflicts use `alignment-conflict`. Mermaid reports located source resource/conflict diagnostics. These operational caps are not new public tuning options.

Typed topology JSON carries ordered `alignments` with portable IDs, kind, members and bounded extensions. Empty declarations add no JSON fields. Raw hierarchy JSON retains source alignment IDs separately through its existing source identity owner; `ToTopologyHierarchySource` restores authored IDs and members, while ordinary `ToTopologyChart` uses portable IDs. Clones, prepared snapshots and imports detach mutable lists and metadata.


### Wardley map annotations and pipeline paint

`WardleyMapBlock` remains the dependency-free static producer. `WithAnnotationsBox(visibility, evolution)` places a measured legend at normalized map coordinates, clamps it within the native plot and retains every numbered annotation in source order. Legend entries sort by number with stable ties. Complete text wraps at the authored body size; a frame too small for the complete legend reports an error, so enlarge the frame instead of relying on hidden or arbitrarily tiny text. `WithoutAnnotationsBox()` restores inline captions.

`WithStyle(WardleyMapStyle)` accepts immutable nullable color roles; null fields retain the shared theme and `WithStyle(null)` clears the overrides. Pipeline `Outline` reuses immutable `ChartStrokeStyle`, including alpha, width and dash capture. Null retains the ordinary dashed native outline; an empty dash sequence selects solid paint. `ShowEvolutionLinks` joins existing children in increasing evolution order while retaining authored child order. The identified `AddComponent(id, label, evolution)` overload makes that real child a dependency/evolution endpoint; the existing two-argument overload remains anonymous. Both child and component label offsets use logical native pixels.

`WithStageBoundaries` captures strictly increasing normalized stage right edges matching the current stage labels. Null restores the incumbent quarter grid. The Mermaid adapter maps complete source `@` boundaries and dual stage labels through this owner; omitted or partial boundaries use the pinned grammar's equal stage distribution. Source pipelines opt into solid outlines and internal evolution rails. Components keep their authored map coordinates; native pipeline parent and sourcing glyph geometry intentionally differs from upstream rendering.

Supported Wardley source colors live under `themeVariables.wardley`; `wardley-beta.width`, `height`, `padding`, `showGrid` and `useMaxWidth` map to existing native frame and host-sizing options. An explicit host theme owns paint precedence. External URLs, executable CSS, external assets and unrecognized source settings are not paint providers. SVG and PNG use the same captured native mark/text scene; pinned browser observations establish only the exercised source facts and visible roles, not complete grammar or pixel identity.


### Native mixed-style captions

`TopologyNode.LabelContent`, `TopologyNodeDetail.Content` and `ValueContent` accept immutable `TextContent.FromRuns(...)` values. The existing `Label`, `Text` and `Value` properties project their plain text; assigning one of those strings resets that caption to the ordinary plain path. Preparation retains detached content and resolved faces. The portable topology payload carries structured runs only when authored and rejects a run sequence that contradicts its plain caption.

```csharp
node.LabelContent = TextContent.FromRuns(new[] {
    new TextContentRun("Review "),
    new TextContentRun("carefully", TextInlineEmphasis.Bold),
    new TextContentRun(" and record", TextInlineEmphasis.Italic)
});
```

Ordinary strings preserve existing heading emphasis and SVG `<text>`. Explicit structured captions start at weight 400 unless a base `TextStyleOverride.FontWeight` is authored; strong uses at least 700 and emphasis adds italic. Family, fitted size, color and decoration stay with the existing native policies. Whole-line native shaping owns bidi and joining across run boundaries.

Structured SVG captions contain captured glyph ink and self-contained permanent resources; they are editable through native `TextContent`, rather than through individual SVG `<text>` nodes. Native PNG uses the same captured decisions. Existing regular/bold faces and native synthetic emphasis remain supported. A fontless host uses the existing built-in TinyFont stroke/character repertoire, not a downloaded font or browser layout. Color/bitmap-only glyph faces without supported monochrome ink reject structured SVG export explicitly; ordinary captions retain their existing fallback. This is not a claim of complete typography, arbitrary Unicode or pixel identity with another renderer.

Flowchart node Markdown strings, Mindmap node labels and Requirement text values lower safe strong, emphasis and line breaks into this owner. Code, links, images, HTML and math are not evaluated and retain located diagnostics. Group/edge and other family rich captions remain explicitly outside this tranche. See the complete [Core native recipe](../../ChartForgeX.Examples/Recipes/MixedCaptionRecipe.cs) and [ordinary source consumer](../../ChartForgeX.Examples/MermaidMixedCaptionSourceExample.cs).


## Linked finite tables in topology

`TopologyNode.TableContent` accepts a Core `TableArtifact` inside a Generic rectangular or rounded Card/CompactCard. Its real node ID owns edges, ports, notes and routing footprint; table ID, column IDs and keyed rows remain separate scoped facts. The same native table plan supplies layout and paint for these nodes and ordinary optional Visuals tables, including the existing optional badge and microvisual behavior.

Node label/subtitle and table Title/Subtitle have allocated headings, followed by every captured table row and the existing footer/status/badge. Set `FitLabelToContent = true` before automatic layout to grow the node. A fixed body that cannot contain all rows or authored column widths rejects. Table cell text can shorten within an authored width, with complete display/source facts retained and a diagnostic. Detail rows, glyph/artwork, Actor/Dot and other shapes/display modes are explicit unsupported combinations. Hidden nodes retain typed content without painting it. Static tables contain all rows: no provider or virtual window is retained.

Preparation snapshots tables, columns, rows, cells, policies and metadata. Typed interchange carries this additive data as version 2 through the existing envelope owner; an older version-1 reader rejects the payload. Ordinary null-table graphs retain version 1. `PresentationScale` records the prepared table scale so the existing minimum header-height law survives Manual restoration without interpreting metadata. Import with `envelope.ToTopologyChart()`, edit the detached `TableContent` cells or replace the table, then prepare again. The original source, prepared scene and envelope remain independent. This retains table presentation and facts without reconstructing pre-export authoring options or promising byte-identical surrounding node paint.

Embedded tables are bounded to 256 columns and 1,024 rows, with the existing text/metadata and aggregate interchange budgets. Cells are source-ordered and identify their owning column explicitly. Null, zero, false and empty strings can retain independent type/raw-source metadata; display text never becomes type authority.


## Sequence layout and role typography

`SequenceArtifact.Layout` accepts immutable `SequenceLayoutPolicy` values. Participant width/height are measured minima, participant spacing separates complete headers, message spacing changes native rows, and note spacing reserves text margins and surrounding clearance. Fragment margin/label padding and activation width use the existing native fragment/lifetime owner. A participant's own `MinimumWidth` composes with the diagram minimum by taking the larger allocation. `CanvasPadding` supplies independent natural-export outer edges; an explicit `VisualRenderContext` owns its viewport and `PaddingEdges`. Fixed contexts that cannot fit reject before paint.

`SequenceArtifact.Typography` accepts immutable `SequenceTypography` with optional Participant, Message, Note, Fragment and Number values captured from `TextStyleOverride`. Constructor inputs and getters are detached. The same resolved style measures, wraps and paints each role. Stereotypes retain the participant role's existing 0.85 size ratio. In Inline presentation, Number stays in the message caption unless a native Number style is authored; a styled number remains in the same message identity. Explicit native text colors use the existing override semantics over `SequenceArtifact.Paint`; unspecified number ink inherits message ink.

Null and empty policies retain ordinary native SVG/PNG/JSON and IDs. `WrapLabels=false` preserves authored lines and grows natural content; a contradictory fixed viewport rejects instead of silently wrapping, shortening or dropping text. True uses the common wrapper with the selected role style and padding; null retains incumbent wrapping. Natural preview Width/Height remain minima, so source hosts use ordinary preserve-size scrolling when measured content grows.

Mermaid sequence source maps actor/message/note FontFamily, FontSize and FontWeight, actor/message/note margins, diagramMarginX/Y, participant width/height, boxMargin/boxTextMargin, activationWidth and wrap/wrapPadding. Explicit family Presentation.FontFamily/FontSize overrides the matching shared host field; explicit shared fields override matching source role fields. Other role fields remain. A host Theme supplies the base without erasing independent source role settings. Later native edits intentionally replace the converted policies. An explicitly assigned family Padding overrides both source diagram-margin axes, including when that value equals the constructor default.

Nonempty policies use the existing typed Sequence presentation and require interchange version 2. The common snapshot captures immutable allocations, role styles and native theme/mode; prepared capture uses the actual context theme and retains context Font distinctions in role styles. `VisualTheme.ToJson/FromJson` owns that theme payload. Restoring through `ToSequenceArtifact` uses the captured native theme once, then the same sequence measurement and paint path. Resolved geometry remains an output observation. Uncarried font resources, arbitrary host frame policies and original pre-normalized source are not reconstructed.

Mirrored/footer participants, unused-participant views, additional lifeline geometry, the source circular numbering badge/sequenceNumberColor, message/note alignment, labelBoxWidth/Height and other unmapped settings remain separate capabilities. Static native layout does not execute popup menus or function-valued font configuration. A located warning for those losses does not constitute support.


### Sequence participant views

`SequenceLayoutPolicy.MirrorParticipants = true` repeats each visible participant through the existing measured participant painter, including glyphs, symbols and stereotypes. Footer occurrences share their source participant ID and have the `sequence-participant-footer` semantic role; they do not create additional editable participants. Open lifelines end at the common footer row. A destroyed participant's footer is centered on its terminal message, and its lifeline and active span stop at the footer top. This mirrored presentation replaces the standalone destruction cross while preserving `DestroyedAtMessageIndex`. Terminal captions reserve their actual header footprint; a self-terminal caption uses the outside lane.

`HideUnusedParticipants = true` filters visible columns only. Message endpoints, note targets, activation participants and native reference targets keep their participants visible. Original records, source order, IDs, metadata and group membership remain available to portable consumers and later edits. Empty groups have no visual allocation; groups with visible members compress around those members. An entirely unused view can show an empty frame while retaining all source participants, then refill when references are added.

Both fields are nullable. Null and false retain ordinary native paint, lifetimes and default portable output. Source `mirrorActors` and `hideUnusedParticipants` are mapped only when authored; the omitted source configuration keeps incumbent native defaults. No claim is made that these omitted defaults match every reference preset. Rebuild the immutable policy to reveal retained columns, preserving its other fields when only a view dimension changes. Fixed contexts that cannot contain measured footers reject before returning a partial capture. A participant cannot start an activation at its mirrored destroying message; that contradictory lifetime presentation rejects explicitly.


### Sequence message alignment and number badges

`SequenceLayoutPolicy.MessageAlignment` and `NoteAlignment` use the existing `TextAlignment` values Left, Center and Right. The same native measured allocation controls both line placement and resolved caption bounds. Null retains regular-message centering, self-message leading alignment and the ordinary note placement. A fixed physical message span that cannot contain its complete wrapped caption rejects rather than shortening or moving text across an endpoint.

`NumberPresentation = SequenceNumberPresentation.Badge` draws existing visible occurrence numbers as native measured circles and text. Inline is the default and preserves the existing prefix or explicitly styled Number run. Number values, pause/resume state, increments and source order stay with the existing decimal occurrence owner. Empty and hidden numbers allocate no badge. A badge is another semantic region of its source message, not an extra message or a new counter.

The automatic badge font is sans-serif at 12 logical units, 9 for numeric strings longer than three characters, and 7 for strings longer than five. Matching fields from `SequenceTypography.Number` override that role independently. Prepared captures preserve partial overrides, so restoring a color/weight override does not freeze one message's digit-dependent size onto other messages. Number text, circle, caption, terminal markers and self return have one captured native allocation. Circles start at a six-unit minimum radius and grow to contain measured ink and native clearance; output is not a pixel copy of an upstream marker resource.

`SequenceArtifact.Paint.NumberFill` overrides the badge surface; otherwise it follows MessageLine and then the native theme accent. An explicit Number typography Color overrides `Paint.NumberText`; unspecified badge ink uses the existing native contrast owner. Inline Number keeps its established Message-ink inheritance. `themeVariables.sequenceNumberColor` maps to NumberText, while authored signalColor already supplies the MessageLine/badge fallback. Omitted source badge paints use the canonical native theme. Complete upstream base/neo/redux preset-color derivation remains outside that palette mapping.

Source `autonumber` and true `sequence.showSequenceNumbers` select Badge presentation. False does not cancel explicit visible autonumber occurrences, and false/absent numbering with no source autonumber retains ordinary output. Authored messageAlign/noteAlign values lower left, center and right; omitted source alignment does not change the native defaults. Existing typed version 2 captures layout, role typography, theme and explicit number paints through the same snapshot/restore owners. Portable fonts still require the caller's available font resources.


Sequence self routes use `SequenceLayoutPolicy.SelfMessageRouting`. Omitted routing keeps the ordinary native 56-unit reach and 24-unit return. Explicit `Orthogonal` uses a measured reach and 25-unit return; explicit `Curved` uses the captured native cubic and 20-unit return. A larger measured badge may increase the return clearance. All bounds, terminal tangents, lifetimes and portable diagnostic samples use that captured path. Rebuild immutable policies with the routing field retained when changing another field. Authored Mermaid `rightAngles` selects the explicit policy; omitted source settings keep native defaults. This is native path geometry, not an upstream pixel-identity promise.


Sequence participant `NavigationLinks` is a detached read-only ordered snapshot of immutable `VisualNavigationLink` labels/targets. Static Core keeps SVG/PNG menu-free; its existing `Href` primary target stays independent. Typed links use portable v2 only when nonempty. The optional HTML adapter's `ToNavigationHtmlPage` composes the same native scene with ordinary details/link controls. `HtmlSequenceNavigationOptions.InitiallyExpanded` belongs to that host. Source-aware callers can copy the validated nullable `document.Configuration.SequenceForceMenus` into it once; Mermaid does not reference the HTML adapter. The native disclosure remains keyboard-operable even when initially open. Raw source statements/spans stay in metadata; hosts consume typed links rather than reinterpreting that JSON. Unsafe source targets remain diagnosed/inert, and unsafe native targets reject at construction.

Nonempty participant navigation captures the same existing Sequence theme/mode and prepared role snapshot used by other new Sequence policies, so restored native pixels retain the original/context paint without a second theme replay. Empty navigation keeps the ordinary absent-wire and static HTML paths.


Sequence `FooterBottomMargin` replaces only trailing space after mirrored footers; omitted keeps the ordinary native fragment margin, and unmirrored layouts ignore the value. `FragmentTabMinimumWidth/Height` opts into one measured keyword-tab/title allocation; omitted both keeps the ordinary combined heading. Complete text can enlarge these minima and insufficient fixed spans reject. Reference tabs reuse the same contour and keep their ordinary policy. `Paint.LifelineStroke` uses the canonical immutable `ChartStrokeStyle`: its color overrides `Paint.Lifeline`, then the existing fallback; null keeps native width/dash. Actual styled lifelines retain participant source IDs and painted extents. All fields restore through current typed v2 snapshots.

Parsed Sequence source lowers the retained 12.2 geometry, role-font, mirror, center-alignment and curved-self defaults once. Authored source and explicit shared/family host fields retain precedence. Effective defaults are native presentation facts, never fabricated authored configuration entries. A later native context fills omitted model fields; replace native Typography or use source Presentation to override the materialized source role fonts. Remote fonts are not fetched. Explicit host padding (including32) and explicit native contexts stay authoritative. Native null/empty output remains unchanged. Nonnegative source `bottomMarginAdj` maps to footer trailing space; negative values reject precisely. Source labelBoxWidth uses the pinned minimum50 and zero height uses20; native minimum fields themselves require positive values. Full preset palette/look derivation, including neo appearance-specific tab/actor bands, remains pending the shared appearance owner.

### Sequence preset colors and measured participant bands

The Sequence preset precedence below is specific to that source policy. Journey and Timeline retain their existing explicit host/family `Presentation.Theme` override, including authored source canvas backgrounds; family themes override shared themes. Without a host theme, their authored source background and alpha still apply.

`SequenceLayoutPolicy.ParticipantBands` accepts immutable `SequenceParticipantBands` with a glyph band and independent glyph-to-label and label-to-lifeline gaps. Header captions grow upward from a shared lifeline datum; mirrored icon captions grow downward. The existing native glyphs stay in their measured band. Participant, Queue and Collections boxes span the shared row. Null keeps ordinary native anatomy.

Each participant can author `Border`, `ActivationFill` and `ActivationBorder` independently of shared sequence paints. `Paint.FragmentTitleText` colors fragment titles and section captions, with `FragmentText` as the fallback. These fields and the band policy survive detached typed interchange.

The source adapter resolves supported named preset color expressions once through the common source-theme owner. Sequence's omitted Light preset is redux-color; Dark host mode selects its paired redux-dark-color preset. An explicit source preset name remains literal. A host `Theme` replaces preset-derived defaults and palette arrays; explicit source role colors and arrays still apply. A font-only host override retains the preset. Palette arrays use original participant order, including hidden participants, and each array wraps by its own length. Empty actor backgrounds use the shared actor fill; empty activation backgrounds use the preset main background when present. Arrays are active only for the two color presets.

A preset background used to derive roles does not itself request opaque chart-canvas paint: explicit background presence remains separate. Native `Prepare` context remains authoritative. Native and parsed-source examples are separate consumers; Core-only snippets do not reference the Mermaid adapter. Full font availability, HTML/math rendering and pixel identity are separate qualification boundaries.


## Explicit captured alpha shadows

Use `prepared.CaptureLayer(new VisualDropShadow(color, offsetX, offsetY, sigmaX, sigmaY, spread, orderedDither), options, cancellationToken)` for a static shadow on one complete prepared child. The immutable request uses resolved literal color, integer logical offsets/spread and the same density-scaled Gaussian targets as `VisualLayerBlur`. Positive spread dilates alpha; negative spread erodes it. The source enters the final composite once. `CapturedVisualLayer.SourceBounds` retains the original viewport; `Bounds` retains the union of source and propagated shadow support, including negative coordinates. Place that original source origin through `ImageComposition.DrawLayer(layer, x, y)` when using optional Visuals composition.

The source remains detached and unchanged. `layer.Visual` uses the explicit baked-image SVG/PNG route and descriptive regions; it adds no duplicate point targets, editable filter graph or native SVG-filter profile. `ToImage()` returns a caller-owned copy. Existing cumulative array/work limits reject before capture; cancellation is observed around synchronous native painting, within effect rows and before snapshot publication. Encoder/font/geometry allocations retain their existing owners. Null/ordinary exports and current fixed-frame imperative composition operations do not opt into this effect.

The SVG importer supports only the documented finite opt-in source chains through `SvgRasterFilterOptions`. Null options keep ordinary unsupported-filter behavior. A signed spread maps only to equal integer device radii in that source subset; other regions, inputs or primitive policies remain diagnosed. The normal compact/wide paired-theme effects example and its downloadable Core program retain the original chart independently of its captured appearance.


## Explicit native masks and layer compositing

`VisualMask` captures the required RGBA pixels independently of its producer. Its `Bounds` come from the image size, logical origin and `PixelsPerUnit`. `VisualMaskMode.Alpha` uses alpha alone; `Luminance` uses unassociated sRGB coefficients 0.2126/0.7152/0.0722 followed by mask alpha. `PreparedVisual.CaptureLayer(mask)` masks one complete native viewport. `CapturedVisualLayer.WithMask(mask)` masks an existing blur/shadow result, retaining its original `Source`, `SourceBounds`, overflow `Bounds`, density and semantic identities. Both require the same density and exact device grid; outside mask bounds coverage is zero. A nonzero resulting alpha retains original unassociated RGB bytes, and optional ordered dither applies only to alpha.

The optional Visuals `ImageComposition.DrawLayer(layer,x,y,blendMode,...)` overload requires its mode explicitly. Normal, Multiply, Screen and the standard Overlay/Darken/Lighten/ColorDodge/ColorBurn/HardLight/SoftLight/Difference/Exclusion/Hue/Saturation/Color/Luminosity modes use gamma-sRGB unassociated channel math followed by the existing premultiplied16, alpha-aware source-over. The normalized channel helpers are shared with COLR, whose caller remains linear-light with its existing float precision. PlusLighter adds and clamps premultiplied channels and alpha independently: half-alpha red plus half-alpha blue becomes opaque half-red/half-blue. The three-argument affine `DrawLayer` keeps its incumbent arithmetic. Explicit blending requires a matching grid/density, uses the current composition as its backdrop and atomically replaces its fixed frame. Rejection or cancellation before commit leaves it unchanged.

The constructor and operations preflight declared borrowed pixel allocations, outputs, snapshots and visits through the existing raster budget owner. Borrowed tails count; shared inputs count once. Encoder/metadata/font/geometry allocations are separately owned. Imperative layer-composition exports remain baked images. The retained surface/root owners below also support the explicit NativeFilters profile. Arbitrary source filter graphs, nonisolated backdrop discovery and mask resampling remain separate capabilities.

Ordinary SVG luminance masks use the mask definition's computed inherited `color-interpolation` (default sRGB, or explicit linearRGB), independently of `color-interpolation-filters`. The finite opt-in source filter admits explicit user-space flood followed by SourceGraphic-over-flood with the standard feBlend mode names in computed sRGB. PlusLighter is an additive native composition operator, not an admitted feBlend name; no-composite, other inputs and implicit/linearRGB filter spaces remain diagnosed. Unsupported graphs/spaces/operators retain located SFR002 diagnostics and strict rejection. Solid vector SourceGraphic descendants may use their existing isolated opacity before the finite operation. Group overlaps and fill/stroke complete once; their real compositor canvases and alpha copies share the same cumulative import budget. Nested filters, transforms, viewports, images and paint servers remain outside this source subset.


### Surface effects and output profiles

Keep a native surface's appearance separate from its caption/link/semantic
owner. Sequence's nullable `SurfaceShadow`, `NoteShadow`, `FragmentTabShadow` and
`BackgroundShadow` descriptors capture numeric contour paint; they do not turn
whole participants into images. `VisualDropShadow.Opacity` preserves fractional
alpha independently of color bytes, and `RelativeRegion` clips only the shadow
in object-box fractions. Whole-child `CaptureLayer` still includes its source.

Select `VisualSvgEffectProfile.Baked` (default) or `NativeFilters` through
`VisualSvgOptions`. Baked shadow pixels sit behind retained vectors. Native
filters keep a single vector surface and serialize only the supported finite
shadow chain. Existing no-effect SVG policy is byte-preserving. Reuse
`VisualRenderOptions` for baked density/caps and operation-scoped cancellation
through the explicit `PreparedVisual` overloads. Do not store cancellation or a
renderer/provider callback in models, prepared scenes or interchange.


### Retained prepared masks

`PreparedVisual.WithMask(VisualMask)` reuses the existing detached fixed-grid mask
contract. It preserves the prepared source's facts, regions and projections while
adding a separate immutable presentation scope. Baked SVG makes that complete
masked object a static image; NativeFilters retains source vectors through an
explicit alpha-mask input. Native luma coverage is materialized by its own shared
channel owner; it does not silently become SVG luminance. Native mask PNG math and
browser input quantization are distinct documented boundaries.

Use matching native density and operation-scoped cancellation. Nested masks use
one grid, one compositor/preflight owner and complete-object opacity/clipping.
Unsupported captured scopes reject common interchange rather than dropping
presentation; ordinary unmasked source export is unchanged.

Retained complete-root masks use a `Mask` step in the common ordered
`Presentation.CapturedEffects` list, not a per-family output option.
`ApplyCapturedEffects(envelope, callerPreparedSource, token)` reuses the immutable
native mask owner and requires the exact captured viewport.
Typed editable restoration rejects this policy; explicitly clear it on a detached
source-facts copy before editing, then replay the original envelope. Raw RGBA data,
origin, density, mode, dither and list order remain detached. Empty policies preserve
ordinary JSON/paint bytes; embedded mask scopes and over-budget payloads reject.

### Finite alpha glow through the existing shadow recipe

A zero-offset `VisualDropShadow` with positive sigma and optional positive spread creates a finite alpha glow. `NativeAppearanceExample.Create("glow", variant, mode)` uses sigma3, spread1 and raw opacity.6 with a resolved paired-theme tint. It blurs source alpha independently of brightness and places the tinted field under the original source once. Outline subtracts original alpha to make a hard band; Bloom selects a luminance bright pass and Screens it. No additional Glow request or WithGlow alias is needed.

The same gamma-sRGB16 owner, square alpha-extrema and three-box Gaussian approximation apply. `CaptureLayer` expands outward through the existing propagated union Bounds; `WithDropShadow` retains the original viewport and clips its support. Transparent tint or opacity0 preserves source bytes. Zero offset/sigma alone is not a general identity for semitransparent source. Use literal opacity rather than pre-quantizing tint alpha when that precision matters. Ordering a mask before or after the completed source-plus-glow changes the result and remains explicit in CapturedEffects.

The complete Core program saves native pixels, the finite source filter and its native import, a NativeFilters vector export and typed terminal replay. Existing source/editable facts stay separate from the captured output. This recipe does not add inner shadows, automatic backdrops, arbitrary source graphs or a preset framework.

### Explicit outline, local backdrop and bloom captures

`PreparedVisual.CaptureLayer(VisualOutline)` dilates source alpha with the existing square kernel and tints only `max(dilatedAlpha - sourceAlpha, 0)`. The band and original source add premultiplied channels and alpha. Radius zero or transparent tint preserves source bytes. This silhouette effect is separate from native geometric path strokes. Its logical radius must map to an integer device radius at the selected capture density.

`CaptureLayer(VisualBloom)` uses required threshold and strength values in `[0,1]`. Gamma-encoded sRGB luminance selects a linear bright-pass ramp above the threshold; its premultiplied16 contribution passes through the existing Gaussian approximation and Screen merge before one final RGBA conversion. This is not linear-light or HDR bloom. Threshold one, strength zero and no bright samples preserve source bytes and bounds. Captured layers retain original `Source` facts and paint baked images. The explicit `ToArtifact`/`ApplyCapturedLayer` route below records typed operations separately; it does not turn the baked image into a live vector scene. Adding overloads means a formerly untyped `CaptureLayer(null)` call must name its intended effect type.

The optional Visuals `ImageComposition.BlurRegion(VisualMask, VisualLayerBlur)` filters the explicit current destination. Coverage replaces premultiplied color and alpha as `(1-C)B + C*F`, rather than painting another source-over layer. The blur option controls its filtered RGBA quantization; the mask option controls final mixed alpha only. Coverage zero, outside-mask pixels and zero sigma preserve raw pixels. Density and grid must match. The existing numeric `Blur(double, ...)` continues to use stored device pixels; typed blur sigma resolves logical units once. The operation reserves the complete child blur, borrowed mask and replacement buffers together and commits atomically.

The complete native appearance program uses Core only. The local-glass composition program requires the existing optional Visuals package. Retained surface shadows, fixed-root effects and common ordered replay use the shared owners described here. The finite ordered source path below imports emitted outline and bloom filters; general backdrop graphs and captured destination/backdrop replay remain separate contracts; a bitmap capture does not imply those memberships.

### Ordered retained and terminal effect replay

`PreparedVisual.WithOutline`, `WithBloom` and `WithDropShadow` retain a complete-root presentation in the original viewport; support outside it clips. `WithMask` uses the same ordered owner. The immutable source facts, regions, projections and captured typography remain independent of paint. Zero-alpha shadow tint/opacity is identity; zero offset and sigma with a nonzero shadow tint still paints under semitransparent source. Use `CaptureLayer` for an outward baked result.

The unpublished mask-only `Presentation.CapturedMasks` collection is replaced by one `Presentation.CapturedEffects` list. A mask is an effect step's `Mask` payload; each step has exactly one typed payload. The old `capturedMasks` JSON key is rejected, with no alias or silent discard. Ordinary empty presentation remains absent and existing version1 output is unchanged. Nonempty captured effects use the same unpublished version2 membership.

For fixed-root presentation, save the ordinary prepared artifact and use `ApplyCapturedEffects(savedEnvelope, freshPreparedSource)`. To edit, clear `CapturedEffects` on a detached envelope, restore its existing typed family model and prepare it through that owner. Replay the original presentation on the new preparation; the envelope is not a scene command serializer. Model-only import rejects carried effects until explicitly cleared.

`CapturedVisualLayer.ToArtifact()` is the explicit source/output-frame factory. Its envelope Width/Height describe source coordinates; the terminal effect step carries actual outward Bounds and native raster options. The artifact paints its immutable output natural size. Generic `PreparedVisual.ToArtifact` dimension checks remain strict, and source node/point geometry is not translated to fit the bitmap. `ApplyCapturedLayer` prepares no model or layout; it takes fresh caller-prepared source and returns an actual new capture. If an edit changes the recorded output footprint, including a bloom changing between no-bright and expanded support, replay rejects and requires a fresh capture. The returned layer never receives stale envelope source facts or source-coordinate animation plans. Captured-layer masks may follow the one terminal capture; other post-capture operations are not represented.

`NativeFilters` keeps vector children once under finite typed filters. `Baked` uses the canonical source-plus-effect result once. Browser Gaussian and channel quantization can differ from native boxes/16-bit conversion. Baked completed-object output does not promise point/legend DOM owners; the existing HTML adapter validates actual native owners. Initial presentation remains static and script-free in Core.


`VisualInnerShadow` is an immutable finite inset request. `PreparedVisual.CaptureLayer` returns detached original-sized pixels and independent source facts; `WithInnerShadow` retains one ordered root presentation. Positive `Spread` means source-alpha erosion, and the complementary blurred/offset alpha is tinted SourceAtop to keep original alpha. Nullable/default rendering does not select this effect. The common `CapturedEffects.InnerShadow` payload, existing v2 selector and explicit replay owners retain it; no family-specific effect bag or generic scene importer is introduced. NativeFilters preserves vectors with its finite filter chain while Baked uses the shared capture plan. Neither profile implies arbitrary SVG filter-graph support or cross-browser bit identity.


### Portable procedural source intent

`VisualTurbulence` is an immutable procedural source. Its native preparation captures the authored frequencies, raw finite seed, authored octave count, type, stitch policy and channel space in `Presentation.TurbulenceSource`. The common version2 envelope owns this field; an ordinary envelope without it gains no new source fields or version requirement. The effective nine-octave output-resolution policy does not rewrite the authored count.

Load a pure `Unknown`/`None` source with `envelope.PrepareTurbulenceSource(context)`. The caller supplies an explicit same-size native context: its insets, theme, mode, fonts and frame own the fresh preparation. Explicit empty headings remain empty; omitted headings use the envelope captions. Saved resolved theme/geometry is evidence, not an editable frame codec. Source ID, extension values and accessibility remain detached, while new prepared frame facts describe the actual caller context.

To edit, replace the immutable `TurbulenceSource` on a detached envelope. If the envelope carries `CapturedEffects`, clear that one ordered list on the editable source copy, prepare through `PrepareTurbulenceSource`, then replay the original presentation with `ApplyCapturedEffects` or `ApplyCapturedLayer`. A terminal capture keeps its source viewport separate from its actual output bounds and rejects changed footprints. Diagram model importers reject carried procedural sources instead of silently dropping them. Arbitrary scene edits, partially faded source scenes and caller-forged policy facts are not source reconstruction.

The complete Core program saves `noise.source.json`, loads the same source and edits its seed. Baked/PNG and NativeFilters remain explicit output profiles; portable policy does not imply universal browser pixel identity. General SVG graphs, portable arbitrary scene transforms and the remaining procedural/effect memberships retain their separate support boundaries.


### Transformed procedural SVG regions

The finite single-`feTurbulence` importer samples a finite invertible affine transform through the existing native matrix inverse. Each actual device pixel center maps to the original logical procedural field. Rotation, skew, reflection and fractional placement retain the authored seed, frequencies and stitch tile; the output bounding box does not become a new noise domain. The transformed quadrilateral is clipped through the same native contour coverage and alpha-only mask conversion as ordinary source geometry. Source RGB remains unassociated when resulting alpha is nonzero.

The pre-existing device-aligned positive-axis path keeps its original sampling arithmetic and allocation behavior. General affine output uses one outward integer storage box and one preflighted contour coverage canvas; it does not rasterize an untransformed field and resample that bitmap. Singular/nonfinite transforms, unsupported graphs and actual device/lattice/work limits reject through the existing source diagnostics and strict mode. Existing geometry scratch and encoding allocations retain their separate owners.

`NativeTurbulenceExample.AffineSource` and `RasterizeAffine` demonstrate the ordinary Core source-import workflow and the complete downloadable program writes each source SVG beside its native PNG. This source-import membership does not invent an editable scene-transform schema for `TurbulenceSource`, or promise universal browser/native pixel identity. Reconstructing arbitrary native transformed scenes and portable transformation intent remains a separate contract.

### Procedural sampling and browser output

`VisualTurbulence` uses one deterministic stream for R, G, B and A. It truncates the seed before normalization, normalizes gradient vectors sampled from a square, retries a zero vector, and combines octaves with cubic interpolation and halved weights. Stitching adjusts frequencies against the original logical tile. Direct generation and affine SVG import sample the same field at actual physical pixel centers.

PNG and Baked SVG quantize native unassociated channels once; alpha stays coverage. NativeFilters asks the browser to render `feTurbulence`. Browser sampling phase, gradient precision and intermediate premultiplied color conversion can produce different pixels, especially at low alpha and in linearRGB. For example, a browser can sample a half-cell later than native generation and quantize premultiplied linear channels before converting to sRGB. Choose PNG or Baked SVG when the exact native pixels must be preserved.

### Finite SVG color primitives

Opt into source color conversion with `SvgRasterFilterOptions`. The fused color-only path accepts a flat ordered sequence of `feColorMatrix` and `feComponentTransfer` runs over an explicit device-aligned user-space filter region. Its first stage explicitly consumes `SourceGraphic`; each later stage explicitly consumes the immediately preceding unique named result. The final stage has no result name. Stage count shares the existing work and cancellation limits rather than a separate two-stage feature cap. Color matrices accept matrix, saturate, hueRotate and luminanceToAlpha. Component functions accept identity, table, discrete, linear and gamma; omitted channels are identity and the last function for a channel wins. Tables accept up to 256 entries. Gamma accepts any finite exponent. A zero amplitude gives the constant offset; at zero input with a negative exponent, positive amplitude clamps to one and negative amplitude clamps to zero. These values are the right-hand limits of the clamped channel function. A scaled power uses a logarithmic product if the power alone overflows. Matrix rows and linear-transfer coefficients must admit finite evaluation on the normalized channel range.

Each stage uses straight color channels in its computed filter space. Channels clamp after every stage, zero-alpha RGB becomes zero before the next, and changing filter space converts through the same continuous transfer functions. One final native quantizer follows the sequence; intermediate scalar channels do not create byte surfaces. The SVG default is linearRGB; explicit or inherited sRGB works through the existing style cascade. Alpha stays linear coverage. Transparent source RGB is zero before remapping, and constant terms can create color or alpha across the filter region. Final channels use the native transfer and quantizer. Browser intermediate surfaces may round differently.

The existing source capture, own clip/mask/opacity order, cumulative pixel/work limits and cancellation apply. The finite ordered input path below also supports earlier named results and primitive subregions. Broader source-resource memberships retain their existing diagnostics. Null filter options retain the ordinary unsupported-filter behavior. `ChartForgeX.Examples/Snippets/RecipeColorSourceProgram.cs.txt` is a complete Core program that writes each source SVG beside its native PNG.

### Ordered native SVG filter inputs

`SvgRasterFilterOptions` also admits finite source-order chains containing `feFlood`, `feColorMatrix`, `feComponentTransfer`, `feGaussianBlur`, `feMorphology`, `feOffset`, `feBlend`, `feComposite` and `feMerge`. Inputs select `SourceGraphic`, black-RGB `SourceAlpha`, or a named earlier result. Repeated names bind to the closest preceding producer. Missing and forward names use the omitted-input default: the previous primitive, or `SourceGraphic` for the first primitive. Merge paints its first input at the bottom and each later input over it. Its input count and the sequence length share existing source and work limits.

Named results retain premultiplied RGBA16 channels and their actual working color space. A consumer converts RGB through the common sRGB transfer only when its computed space changes. Alpha stays coverage. Color matrices and transfer functions temporarily use straight channels; every primitive clamps its result and discards hidden RGB at zero alpha. Literal flood channels and flood opacity reach this surface without an intermediate byte conversion. The final native output uses the existing RGBA8 quantizer. Existing exact byte operations, fused scalar sequences and finite typed recipes keep their established arithmetic. Native precision and the three-box Gaussian approximation can differ from a browser's intermediate precision, transfer and Gaussian kernel.

The filter frame uses explicit finite numeric `userSpaceOnUse` lengths under a positive aligned axis scale and translation. Standard input clips to that frame. Primitive subregions independently clip outputs; omitted subregions follow the union of declared input subregions, or the full filter frame for standard/source-free inputs. Bounds remain separate from clipped pixel contents, so an arithmetic constant can generate coverage across a default region even when the referenced pixels are transparent. Offset and morphology radii require integer device-pixel lengths; Gaussian deviation uses the existing finite native range and transparent edges. Arithmetic applies `k1*i1*i2 + k2*i1 + k3*i2 + k4` to premultiplied channels and alpha, clamps them and restricts RGB to resulting alpha. Over/in/out/atop/xor and all standard blend modes share the common channel and alpha laws.

One applied sequence consumes one filter application. Capture, conversion, intermediate RGBA16 arrays, kernel scratch and final viewport output share the existing cumulative byte/work debit, including named results reused by multiple consumers. Releasing a result after its last read does not refund declared allocations or assume collection. Admission occurs before each new pixel allocation, and cancellation is checked during parsing, operations, rows and final publication. This limit covers declared pixel arrays and sample visits, rather than process memory, encoder buffers or elapsed time.

The public `NativeFilters` bloom and outline emitters use this same source-import path. Their existing typed `CapturedEffects` v2 payloads and replay APIs remain the portable owner. Original SVG can be saved beside the native PNG; a generic editable SVG-graph payload is not added to interchange. Source paint servers, embedded images, nested source transforms/viewports/filters, `BackgroundImage`, `BackgroundAlpha`, `FillPaint`, `StrokePaint`, unrepresented filter primitives or authored `filterRes` sampling, object-bounding-box units and nonaligned general filter transforms retain explicit diagnostics. Standalone turbulence keeps its separately documented source-free membership; this ordered sequence does not yet compose turbulence. Null filter options keep the ordinary unsupported-filter contract.

`ChartForgeX.Examples/Snippets/RecipeOrderedFiltersProgram.cs.txt` is a complete Core-only program. It saves actual public bloom/outline SVG, replays their captured effects, and writes named merge, arithmetic and small-channel SVG/PNG companions.
