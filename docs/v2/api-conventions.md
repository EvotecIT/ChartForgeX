# V2 public API conventions

These rules govern the shared prepared-rendering contracts and their model entry points. All chart families and chart grids compile through the native scene, alongside topology, flow and sequence diagrams. Model convenience exports resolve a request and use the same preparation route. Static composition belongs to `ChartForgeX.Visuals`; animation belongs to `ChartForgeX.Stories`. The [API ledger](api-ledger.md) records naming and ownership changes; this document does not claim that every existing public member conforms already.

## Operation names

| Operation | Meaning | Example / rule |
| --- | --- | --- |
| `With*` | Set a scalar value or configuration object on a mutable model builder and return that builder. Do not add data. | `chart.WithTitle("Capacity")`, `chart.WithBarVisualStyle(style)` |
| `Add*` | Add model data or a semantic child and return the builder. Do not silently replace configuration. | `chart.AddLine("Used", points)` |
| `Configure*` | Configure a sub-object through a typed callback, preserving the operation's ownership rules, then return the builder. | `chart.ConfigureXAxis(axis => axis.Minimum = 0)` |
| `ApplyPointChanges` | Commit a bounded keyed numeric data transaction while retaining surviving observations' presentation. | `series.ApplyPointChanges(changes, maximumObservations: 500)` |
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
| `ChartNode` / `ChartFlowLink` | Immutable authored relationship identities and raw finite non-negative flow facts. Chart families validate their graph and weight constraints before adding a series. |
| `ChartSeries.WithNodeState(id, state)` | Mutable semantic styling keyed by an existing authored node ID, scoped to that series. `NodeStates` exposes a read-only view; preparation snapshots the resulting paint. |
| `ChartFlowStyle` / `ChartSeries.WithFlowStyle(id, style)` | Immutable fill, opacity, pattern and outline overrides for an existing Sankey or Chord flow ID. `FlowStyles` is read-only; null removes an override. |
| `ChartSeries.WithFlowState(id, state)` | Semantic state for an existing Sankey or Chord flow ID. `FlowStates` is read-only; null restores source-node/series inheritance and `None` suppresses it. |
| `ChartOptions.Chord` | Typed circular span/start, node geometry, opacity, direction and label settings over the canonical weighted-flow facts. See [weighted chord charts](../chord.md). |
| `ChartOptions.Sankey` | Alignment, ordering, node geometry and flow paint options. `ConfigureSankey` configures and installs an independent snapshot, then returns the chart. |
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

For keyed live updates, use `ApplyPointChanges` after assigning an explicit ID to every observation:

```csharp
completed.WithObservationIds(new[] {
    new ChartObservationId("mon"), new ChartObservationId("tue"),
    new ChartObservationId("wed"), new ChartObservationId("thu"), new ChartObservationId("fri")
});
completed.ApplyPointChanges(new[] {
    ChartPointChange.Remove(new ChartObservationId("mon")),
    ChartPointChange.Replace(new ChartObservationId("fri"), new ChartPoint(5, 39)),
    ChartPointChange.Append(new ChartObservationId("sat"), new ChartPoint(6, 45))
}, maximumObservations: 5);
```

Append requires a new key; replacement and removal require existing keys. A key occurs at most once in a batch. Surviving values retain their source order, IDs, labels, colors, patterns, text styles and slice offsets. The optional rolling limit removes the oldest surviving observations in source order. Enumeration, key, capacity and cancellation failures leave installed data unchanged; prepared results remain detached. The default `maximumChanges` is 4,096, with rejection after at most one additional input item. Hosts can set a deliberate capacity, coalesce or split incoming work and pass a cancellation token. They own collection cadence and asynchronous queues, and serialize access to the mutable model.

This operation supports the eleven point-mapped families: Line, StepLine, Area, StepArea, StackedArea, Scatter, Bar, HorizontalBar, RadialBar, RadialColumn and Lollipop. Typed observations and derived sources retain their existing update APIs. The [compiled rolling-window recipe](../../ChartForgeX.Examples/Recipes/KeyedPointChangesRecipe.cs) shows the complete model workflow. Browser hosts prepare a new section and call the existing controller's `update`; surviving explicit identities retain selection and legend state, while accepted replacement clears transient screen-space brush and readout state. A canceled batch leaves the committed model and browser scene intact.

Decimation, histogram and regression factories expose read-only derived `Points`. Change complete source observations and rebuild the derived series rather than editing a reduced point, bin aggregate or fitted endpoint independently of its source provenance. Original-input counts, mappings and source snapshots remain consistent. Presentation overrides remain configurable.

`WithDataLabels` sets chart-level visibility. A series overrides it with `WithDataLabels` and restores inheritance with `UseChartDataLabels`. Layout and paint use the same resolved setting; when neither is explicit, each family keeps its default. This includes Chord/Sankey captions, gauge summaries, Circle captions, radial centers, Bullet captions, the Donut center and ProgressBar numeric values. Dedicated content switches such as `ShowProgressValues` can still suppress their content. Progress row category names and gauge ticks and target labels keep their separate roles.

Gantt lane item captions are visible by default. `WithDataLabels(false)` hides them at chart or series level; an explicit series setting takes precedence. Hidden captions reserve no extra bar height.

Lane factories and full replacements order items by start, end and input order. `UpdateStateTimelineSegment` and `UpdateGanttLaneItem` retain the observation index even when a changed time moves the item in the rendered order. State, category, label and detail stay with that complete item. Adjacent timeline intervals with the same state and detail may render as one run; its contributor metadata lists only the intervals actually combined. A combined run keeps derived identity; a one-interval run retains its authored observation ID.

Categorical `HeatmapCells` includes null masked cells in column order. Observation indices and IDs refer to those columns. `UpdateHeatmapCell(index, null)` masks one cell while preserving later cells' identities; restoring the cell restores its existing ID. Use `CreateHeatmapCategoryRow` for categorical data and the numeric row APIs for numeric data.

`MapPoints` keeps each dotted-map label, longitude, latitude, optional value and color together. Repeated coordinates retain separate observations. `CalendarItems` owns wall-clock dates, values and optional colors. `RegionValues` owns canonical region codes, values and optional colors for region and tile maps; their immutable geometry belongs to `series.RegionMapDefinition` or `series.TileMapDefinition`. Chart options own map viewport, paint, color scales, layers and routes. Changing Cartesian axis labels does not change map observations.

`WithPointLegend()` gives dotted maps one key per typed location, using that location's label and resolved dot color. The keys retain observation IDs, coordinates and optional values, including for locations outside the selected viewport. HTML legend actions address the corresponding location and reconcile authored IDs through updates. Region and tile maps retain their independent numeric color keys.

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

`ReplaceFlowData` serves Sankey and Chord, `ReplaceTreeData` serves Tree, and `ReplaceHierarchyItems` serves Sunburst and Treemap. Each operation copies and validates the complete graph before installation. A failure leaves facts and metadata intact. A successful replacement clears ordinal observation IDs, point presentation, node states and flow overrides while retaining series identity and configuration. Authored node and flow IDs in the replacement identify the new targets even after reordering or renaming. Already prepared visuals retain the earlier graph; export the chart again to render its replacement.

Empty node/link pairs or empty hierarchy items create the same no-data state through factories and replacements. Partial empty graphs still undergo family validation. Relationship `ObservationCount` counts nodes; `SourcePointCount` is zero because these families have no numeric XY source observations, and `Points` stays empty and read-only.

## Configuration callback ownership

Public option callbacks use one transaction: clone current options, invoke the callback, then install an independent clone after success. A thrown callback leaves the previous owner and collections unchanged. References obtained before the callback, and references retained inside it, do not mutate the newly installed options. Read the current option property after a successful callback when another edit is needed.

Direct option setters remain supported. Assigning a value expresses intent even when it equals the default; nullable overrides can return to inheritance through null where documented. Text-style boolean assignments also preserve explicit false when overriding inherited italic, underline or strikethrough. Callback ownership applies to installed configuration; one-time data selector and formatter functions have their own documented execution lifetime.

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

`ChartColorDimensionOptions` owns nullable `Name` and `ValueFormat`, `ShowLegend` (default true), and validated `LegendStyle` (`Auto`, `Gradient`, `Swatches`). Each supported owner exposes one getter-owned instance: `ChartSeries.ColorDimension` for Scatter/Bubble, `ChartOptions.MapColorDimension` for RegionMap/TileMap, `ChartOptions.HeatmapColorDimension` for numeric matrices/calendars, and the Treemap/Sunburst/Chord options' `ColorDimension`. Configure those instances with the corresponding `ConfigureColorDimension`, `ConfigureMapColorDimension`, `ConfigureHeatmapColorDimension` or family callback. Unsupported family combinations fail preparation.

Null restores each name/format property's family default independently. A supplied name must contain non-whitespace text. Scatter/Bubble and Chord color use `ChartLabels.Color` and neutral numeric formatting by default. Map, heatmap and hierarchy color retain their existing value-format defaults, including the chart's `ValueFormat`. Explicit color formatting changes their numeric labels, keys and readouts. It leaves Cartesian Y, Bubble size, hierarchy size, Chord weight, explicit point labels and authored secondary captions unchanged. A map or heatmap observation has one numeric value, so its readout does not add a duplicate Color row.

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

Prepared Cartesian point semantics retain the complete resolved display label, even when visible placement shortens or omits it. Explicit point labels take precedence over the value formatter. Formatting runs once per point during preparation and the detached metadata reuses that result; exporting a prepared visual does not call the formatter again. Numeric coordinates and source identities remain separate metadata fields.

`Chart.AddPlotRegion(new ChartPlotRegion(color, ...))` adds a passive solid background rectangle to a Cartesian chart. `Chart.PlotRegions` owns its immutable definitions in paint order. Nullable `xMinimum`, `xMaximum`, `yMinimum` and `yMaximum` extend to the corresponding final domain end; regions never expand domains. `yAxis` selects primary or secondary Y. Bounds follow physical X and Y. Horizontal charts use value coordinates for X and linear category slots for Y. Time bounds use OLE Automation dates, and logarithmic value bounds must be positive. A secondary region requires a secondary-axis series.

Use `new ChartPlotRegion(paletteIndex, ...)` to bind a region to the active render theme's categorical palette. Indices are zero-based, wrap to the palette length, and cannot be negative. Both the fill and caption contrast resolve during preparation; SVG series color variables retain that binding. Literal-color regions keep their authored color. `ChartPlotRegion.Color` is nullable for palette-backed regions; inspect `PaletteIndex` to distinguish the two forms. Automatic accessible descriptions and `ChartDescriptionFacts.PlotRegionLabels` retain authored captions even when the chart has no points.

Rebuild previously compiled callers of `ChartPlotRegion.Color` and the `ChartDescriptionFacts` constructor when upgrading: the nullable getter and the constructor's added optional argument change their binary signatures.

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

`ChartSeries.Interpolation` selects `Linear`, `Smooth` or `Step` for connected line, area and range boundaries. Difference areas support `Linear` and `Step` through the same setting. `WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle)` places transitions halfway between mapped observations; `Start` and `End` place them at the preceding and following x positions. `AddStepLine` and `AddStepArea` default to End. Existing smooth helpers use the same setting. Explicit interpolation overrides a convenience entry point, and preparation rejects unsupported families or a step position without step interpolation. Regression and slope lines retain their straight-line meaning and reject non-linear interpolation. Segment breaks remain gaps in every mode.

`ChartSeries.StackGroup` assigns independent Cartesian bar, numeric radial bar/column or stacked-area groups. Named groups stack even with grouped bar mode, while unnamed grouped series remain separate. Stacks partition by series kind and axis. `WithNormalization(100)` normalizes positive and negative subtotals independently to +100 and -100. Every member of a stack must declare the same normalization target; unstacked normalization is rejected. Stacked-area members must also share interpolation and step placement so their touching boundaries align; use a separate group or axis for independent choices. All-zero coordinates stay at the baseline and report `cartesian.stack-zero-total` or `numeric-radial.stack-zero-total`.

Normalization changes geometry, bounds and total positions together. Raw observations, labels and `data-cfx-y` remain source values. `data-cfx-rendered-y`, `data-cfx-base` and `data-cfx-stack-end` describe the displayed contribution and extent. Format the relevant value axis as percentages when using a target of 100; source labels still describe counts or other authored units. Normalized stacks retain each series' existing segment breaks; an authored stacked-missing-observation policy remains separate work.

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

Difference areas support Linear and Step interpolation with numeric/time X and a linear primary or secondary Y axis. Linear fills split where the two boundaries cross. For held observations, call `chart.Series[0].WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle)`. Both boundaries change together: Start uses the preceding X, Middle the mapped midpoint and End the following X. Each held interval takes the sign of its original pair. Vertical jumps have no filled width; ties have no filled area. These transitions do not add source observations or invent a diagonal crossing.

The DateTime constructor uses the existing UTC conversion policy. X must increase within each connected segment; explicit `BreakBefore` starts a new segment and cuts both boundaries and fill. Either axis can be reversed without changing fill polarity. Bounds and clipping use both values and the selected value axis. Smooth interpolation, nonlinear axes, duplicate/backtracking X within a segment, stacking and normalization are rejected. Nullable paired ingestion and automatic alignment of separately supplied series remain separate work; use finite pairs and explicit breaks for missing sections.

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

The physical X axis owns horizontal values, bounds, scale, reversal and tick formatting. The physical Y axis owns categories, bounds, reversal and category captions. Each item's `X` remains its category coordinate; orientation never sorts items or changes accumulation. Explicit Y category labels take precedence; `WithXLabels` supplies a fallback when Y has no explicit labels. Resolved category captions agree across the native axis, marks, point legends and HTML readouts, including custom formatters and time categories. Horizontal waterfalls require one series on the primary axis. Vertical secondary-axis behavior is unchanged.

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

Endpoint captions inherit chart, series and point label typography and visibility. They use the shared measured placement and overflow diagnostics. Auto/Outside begins at the actual endpoint in its outward direction; Inside/Center uses that endpoint's half of the interval. Short, coincident or clipped intervals may omit or shorten captions while preserving complete text and raw facts. Both captions remain associated with one original observation for browser hover, selection and muting.

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

`ChartOptions.BoxPlot.Orientation` defaults to `Vertical` and may be configured before or after adding series. `ChartBoxPlot.X` always denotes the category coordinate. Horizontal box plots use the primary physical X value axis and Y category axis, including bounds and reversal. Move value scales and tick formatting to `XAxis` and category formatting to `YAxis`; explicit Y labels win, with `WithXLabels` as a fallback when Y has no explicit labels. Positive logarithmic values use the existing value-axis scale without an invented zero baseline. Horizontal charts require only box plot series on the primary axis. Existing vertical Cartesian mixtures and secondary Y axes remain supported. Box plots do not participate in scalar bar stacking or normalization.

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

HTML readouts add sample count, calculation policy, source extrema and outlier count. IQR captions use `ChartLabels.LowerWhisker` and `UpperWhisker`; source/inlier extrema, policy, estimator, multiplier, absent inliers and individual outlier captions have corresponding `ChartLabels` properties. A keyed group preserves summary selection across prepared updates; a keyed sample preserves outlier selection only while that rendered outlier target remains. Point legends emphasize, mute and isolate the complete group's summary and outliers. Separate per-statistic data labels are not supported.

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

`ChartForgeX.Core.ChartLineAreaForm` is the shared form type for `ChartSeries.Radar.Form` and `MetricCard.MiniSparklineStyle`. Its values are `Area = 0` and `Line = 1`; both models default to Area. Metric cards select the same form through `WithMiniSparklineStyle`.

`AddRadarArea` and `AddRadarLine` use the same Radar series kind and shared categorical/radial axes. `ChartSeries.Radar.Form` selects filled Area or unfilled Line, and `ConfigureRadar` configures the area's `FillOpacity`; null uses the theme. `AddRadar` retains the Area default. Line form rejects area opacity.

`ConfigureRadar(radar => radar.MissingCategoryPolicy = ChartRadarMissingCategoryPolicy.Gap)` leaves unknown categories unpainted. `Zero`, the default, retains derived zero vertices with missing metadata and no authored source identity. `Gap` breaks the outline at missing categories, keeps observed runs across the category seam, and omits an incomplete area fill and its legend fill with a `radar.open-area` diagnostic. `Connect` joins the observed vertices across missing categories and closes an area only when at least three observed vertices remain. Gap and Connect add no missing point targets or captions. Authored zeros remain observations under every policy; the common category axis remains the union of the participating series.

`WithPolarGridShape(ChartPolarGridShape.Circle)` selects circular guides for radar or polar line charts. `Polygon` joins the spokes; `Automatic` preserves native radar polygons and polar circles. `Options.PolarGridRingCount` optionally requests zero through one hundred equally spaced guides in the configured linear, logarithmic or symmetric-logarithmic scale. Zero hides concentric guides while retaining spokes and observations. Null preserves generated axis ticks. Guide settings preserve source values and series forms and use the same prepared geometry for SVG and PNG. Fractional guide counts can extend beyond the data-axis radius, so fitting the outer guide can resize the plot and change rendered observation coordinates. Observation coordinates remain unchanged when the plot layout is unchanged.
`ConfigureRadar(radar => radar.CurveTension = 0.25)` selects closed cubic curves through the observations. Null or zero keeps straight edges. Each radar uses its selected value-axis domain during curve fitting. Standalone charts and column overlays reserve complete curve bounds with one scale factor shared by their marks, axes and guides; overlays retain the configured inner-radius proportion and transparent hole. Source values and column stack totals remain unchanged.

These radar forms also combine with `AddRadialColumn` through the existing series calls. The native numeric radial owner aligns every series to the union of category X identifiers and places radar vertices at whole-category centers, regardless of grouped column slots. Each value axis builds its domain from column segment endpoints and the raw values of its radar series; radar contributes neither stack slots nor normalized totals. `UseSecondaryYAxis()` selects an independent numeric domain and reversal for radar in both standalone and mixed charts. Standalone radar starts its first category at the top; mixed charts use the column viewport's category centers. `ConfigureXAxis(axis => axis.WithReversal())` reverses those category positions together with their spokes and captions. Standalone radar then starts its last category at the top. Source observations, category labels, point colors and missing-category identities keep their original associations.

Mixed column/radar charts require a full-turn `ChartRadialGeometryOptions` viewport. Each series must have unique category identifiers; a populated radar needs at least three categories across the composition. Missing column observations remain absent. Radar uses its configured missing-category policy in both standalone and mixed layouts. The inner radius clips radar fills, patterns, lines and glyphs in both native exporters. `RadialBar`, continuous `Polar`, `PolarArea` and Cartesian series cannot join this composition.

Continuous `Polar` uses authored `ChartPoint.X` radians from the positive horizontal ray. Its default direction is counter-clockwise; `XAxis.WithReversal()` makes increasing radians advance clockwise. Points, angular spokes and captions use the same projection, while angle metadata and readouts retain the authored radians. `YAxis` and `SecondaryYAxis` control numeric radius reversal independently.

`PolarArea` uses source order for its equal-angle category slots; X values identify their labels. `XAxis.WithReversal()` reverses those slots together with their source paints, patterns and data captions. The last source category then occupies the first slot clockwise from the top ray. Source indices, values, percentages and zero-slot identities remain stable. Radii retain the square-root mapping that makes sector areas proportional to values; `YAxis.WithReversal()` is unsupported and rejects during preparation.

Numeric radial `ChartPoint.X` identifies an ordinal category; `Y` is the signed source value. The numeric domain belongs to `YAxis` or `SecondaryYAxis` in both angular-bar and radial-column orientations. `ChartRadialGeometryOptions` owns finite clockwise start/end angles, inner radius and category/series spacing. Reversal belongs to the corresponding `ChartAxis`, so tick placement and mark projection agree. Fixed nonzero numeric bounds constrain visible geometry without rewriting observations. Percent rings use the separate `AddProgressRing` API and retain their 0–100 contract.

Numeric radial `Inside` and `Center` captions fit entirely within their painted sector. Captions that cannot fit are shortened or omitted with `numeric-radial.label-overflow`; their full source values remain in descriptive regions and point metadata. Outside captions avoid painted marks, and automatic inside ink resolves contrast against the composited fill. Explicit label colors remain caller-controlled.

Numeric radial axis text uses the existing chart options. `WithXAxis`, `WithYAxis` and `WithSecondaryYAxis` place horizontal titles in measured strips inside the shared frame. Primary and secondary value titles share the top strip; the category title uses the bottom strip. A value title participates when that axis has observations or a complete authored domain. `AxisTitleStyle` controls all title text; shortened or omitted titles report `numeric-radial.axis-title-overflow` and retain their full descriptive text. An entirely empty chart keeps its no-data surface.

`ConfigureXAxis`, `ConfigureYAxis` and `ConfigureSecondaryYAxis` set each axis's independent `LabelOrientation` and `LabelAngle`. `Fixed`, the default orientation, uses screen degrees: zero is horizontal, and equivalent full turns normalize before layout and paint. `Parallel` follows the local axis track; `Perpendicular` uses the direction across it. An angular axis follows its local tangent, while a radius axis follows its spoke. For radial bars the category axis is radial and the value axes are angular; radial columns use the opposite arrangement. Relative modes add `LabelAngle` as an offset, then keep the baseline upright. Reversal uses the projected position, and primary and secondary value axes keep independent settings. Other chart families reject a nonfixed orientation during preparation.

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

A zero fill multiplier or a transparent effective fill creates a hollow candlestick with no interior wick or hatch. SVG, native PNG and configured financial legend glyphs consume the same captured pattern, phase and resolved paint. Zero widths and transparent strokes remain hidden regardless of dash settings. A point legend shows its observation's direction; a series legend shows rising and falling glyphs under one series action. Bounds cover the painted body and strokes, including minimum-height and all-equal bodies. Preparation freezes styles, geometry, regions and legend output independently of later option changes. Session-time spacing and datum style callbacks remain separate work.

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

Secondary typography inherits chart, series and ordinal node data-label styling at 80% of the primary font size, with a ten-pixel minimum no larger than the primary caption, then applies `SecondaryLabelStyle`. Explicit secondary font sizes and colors override these defaults. `SecondaryLabelSpacing` defaults to two logical pixels and accepts finite, nonnegative values. Both runs share horizontal or upright tangential placement and fit inside the actual sector, including rounded corners. Primary content takes priority: secondary text wraps, shortens at text-element boundaries or is omitted before the primary caption loses content. Font sizes stay fixed; automatic shrinking and configurable sector padding are separate policies.

Full secondary text remains in `data-cfx-secondary-label` and descriptive node regions even when no caption fits. The optional HTML adapter exposes it as plain text in pointer and keyboard tooltips, separately from size, color, supplied totals and remainders. IDs, numeric metadata and target counts stay unchanged. A non-null formatter on a populated non-Sunburst chart fails preparation; style or spacing configuration without a formatter remains inactive. Treemap retains its own label and numeric-value captions.

## Numeric X viewports

`Chart.Prepare(context, new ChartXViewport(minimum, maximum))` prepares a numeric X window for charts containing Line and StepLine series with linear or step interpolation on a linear X axis. Bounds must be finite, increasing and inside the resolved full X domain. Other families, smooth interpolation, nonlinear X scales, sparklines, charts without observations and layouts without a usable plot reject the viewport request explicitly. Ordinary preparation remains available for every supported chart family.

```csharp
var overview = chart.Prepare(context);
var detail = chart.Prepare(context, new ChartXViewport(2, 8));
var projection = detail.XProjection!;
double logicalX = projection.Project(5);
double sourceX = projection.Unproject(logicalX);
string svg = detail.ToSvg("detail");
byte[] png = detail.ToPng();
```

The immutable viewport is a preparation input. It does not change authored axis bounds, series, source indices or decimation settings. Preparation resolves the full-data primary and secondary Y domains, then lays out the requested X ticks and marks through the shared Cartesian compiler. Marks, annotations and labels clip to the visible plot even when ordinary mark clipping is disabled. Crossing segments remain visible when both source endpoints lie outside the window, while authored gaps stay disconnected. Plot regions intersect the visible domain and retain their authored bounds. Off-window reference annotations retain their source facts without guide or caption paint; partially visible bands use their visible intersection.

`ChartXProjection` captures `FullMinimum`, `FullMaximum`, `Minimum`, `Maximum`, `PlotBounds` and `Reversed` as detached values. Supported ordinary preparation exposes the full-domain projection through `PreparedVisual.XProjection`; other charts and unusable plots return null. Source or axis mutations after preparation cannot change the projection or its SVG/PNG exports. Prepare the full domain again to reset the window. The available detail is the retained source model, including any earlier decimation.

These native windows repeat Core layout for each request. The HTML adapter's existing geometry zoom keeps its current behavior; standalone data-domain interaction requires its own prepared tick/layer plan and lifecycle support.

## Enforcement boundary

`V2ApiConventionTests` checks the reviewed immutable contracts, their operation roles, canonical color/severity types, core-only public signatures, in-memory export signatures and detached request/output lifetime. It also checks the selected mutable chart bridge's `With*`, `Add*` and `Configure*` behavior. Public typed configuration callbacks on the Core and Visuals `With*`/`Configure*` builder boundary must use `Configure*` and return their receiver. Data-adding callbacks and substantive exports keep their operation names. Focused family and diagram fixtures protect preparation, retained semantics and explicit limits. These are compiled API and observable-output checks; they do not read this document or enforce editorial wording.

Each migrated family expands the reviewed API selection and adds its own data/layout/output fixtures. The old surface remains inventoried rather than being subjected to a blanket prefix rule during a partial migration. Do not weaken a current contract merely to pass the naming test, and do not claim that a naming check proves visual quality, package publication or consumer compatibility.
