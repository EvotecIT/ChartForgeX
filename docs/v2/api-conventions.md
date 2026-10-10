# V2 public API conventions

These rules govern the shared prepared-rendering contracts and their model entry points. All chart families and chart grids compile through the native scene, alongside topology, flow and sequence diagrams. Model convenience exports resolve a request and use the same preparation route. Composition and animation package extraction remains separate work. Remaining naming changes are tracked in the [API ledger](api-ledger.md); this document does not claim that every existing public member conforms already.

## Operation names

| Operation | Meaning | Example / rule |
| --- | --- | --- |
| `With*` | Set a scalar value or configuration object on a mutable model builder and return that builder. Do not add data. | `chart.WithTitle("Capacity")`, `chart.WithBarVisualStyle(style)` |
| `Add*` | Add model data or a semantic child and return the builder. Do not silently replace configuration. | `chart.AddLine("Used", points)` |
| `Configure*` | Configure a sub-object through a typed callback, preserving the operation's ownership rules, then return the builder. | `chart.ConfigureXAxis(axis => axis.Minimum = 0)` |
| `Prepare` | Validate and compile a model with an explicit render context; return a detached prepared result. | `chart.Prepare(context)` |
| `To*` | Return an in-memory representation without writing a file or changing the model. | `prepared.ToSvg()`, `prepared.ToPng()`, `prepared.ToRgba()` |
| `Save*` | Write to an explicitly supplied path or stream. Keep file ownership and format options visible. | File-saving helpers are outside the Phase 1 prepared contract. |
| Constructors and named factories | Create immutable requests, snapshots or presets. They are deliberate exceptions to builder prefixes. | `new VisualFrame(...)`, `VisualTheme.Graphite()`, `VisualTheme.FromJson(json)` |
| `Resolve` | Select a validated immutable view of an existing value. | `theme.Resolve(VisualThemeMode.Dark)` |

Use domain names after prefixes. Avoid interchangeable `Set`, `Use`, `Build`, `Render`, `Options`, `Configure` and `With` methods for the same operation. A meaningful operation such as preparing, importing or exporting is not forced into a configuration prefix.

Typed `Action<T>` configuration callbacks use `Configure*`, including callbacks that select a row, step or point by index or ID. Value and object setters keep `With*`; for example, `WithLineVisualStyle(style)` supplies a style while `ConfigureLineVisualStyle(style => ...)` configures it. The immutable `ChartColorScale.WithLabels(string?...)` copy operation retains its value arguments and name.

The prefix does not promise uniform object identity or rollback. Axes, family options, labels, gauge options, accessibility and getter-owned text styles are edited in place. `ConfigureBarVisualStyle`, `ConfigureLineVisualStyle` and `ConfigureGridStyle` configure a clone of the current style, then install it through the copying setter after the callback succeeds. Retaining that working style does not give access to the installed copy. Point-label styling reuses an existing override or installs a newly configured one after success; its style list can expand before the callback runs.

Chart and topology `ConfigureTheme` callbacks edit their stored theme in place. Chart-grid and visual-grid callbacks use the existing theme, or create a light theme when none is set and install it after success. Changes to an existing theme remain if the callback throws; a newly created grid theme is not installed on failure. Other callbacks that edit existing objects can also retain changes made before an exception. These ownership rules are preserved by the naming change.

Immutable render requests use constructors and read-only properties. They do not expose `Add*` or `Configure*` mutation. The reviewed `VisualTheme.WithTypography(VisualTypography)` exception returns an independent paired theme with a replacement typography scale, preserving both palettes, geometry and effects without editing the source. Other immutable `With*` APIs need the same explicit copy-return contract and review. Existing mutable `FontSpec`, `VisualDesignTokens`, status-token and accessibility objects enter this boundary through snapshots or leave it through independent copies.

## Shared requests and ownership

| Contract | Ownership and behavior |
| --- | --- |
| `IVisualRenderable` | A core static compilation boundary returning `PreparedVisual`. Optional producers implement the same boundary without making core reference their concrete types. |
| `VisualSize` | Finite positive logical width and height. A default zero-valued struct is rejected when used in a layout. |
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
| `ChartOptions.Chord` | Typed circular span/start, node geometry, opacity, direction and label settings over the canonical weighted-flow facts. See [weighted chord charts](../chord.md). |
| `ChartOptions.Sankey` | Getter-owned mutable alignment, ordering, node geometry and flow paint options. `ConfigureSankey` edits the existing object and returns the chart. |

Keep the numeric scene and painter implementation internal. Public signatures use core-owned types and do not reference Visuals, Stories, browser hosts, their encoders or their policy objects. Static rendering is script-free. Optional animation, interaction and composition have the ownership described in the architecture and [consumer migration guide](migration.md).

`ChartColor` is the color value type. Palette collections contain that type; do not introduce another RGB wrapper for the prepared pipeline. Diagnostic severity reuses the existing enum. Finding severity, test outcome and operational state remain distinct domain vocabularies even where their presentation uses the same colors.

## Configuration precedence

The prepared context owns size, theme, font and frame behavior. `Chart.WithSize`, `Chart.WithTheme` and other model presentation options supply the request for convenience exports such as `chart.ToSvg()` and `chart.ToPng()`. They do not silently override an explicit `Prepare(context)` request. Hosts that supply a context set these values in `VisualRenderContext`.

The model's title and subtitle supply fallbacks when the corresponding `VisualFrame` values are null. An empty string explicitly suppresses that line. Frame legend visibility, placement, content surface and transparent canvas come from the context, including its defaults. Model card settings do not override an explicit frame. Categorical legends use the frame's visibility permission; continuous heatmap, calendar and map scales additionally honor their model scale and legend visibility settings. Data semantics, explicit series/point colors, label styles, axes and formatters remain model inputs. Convenience exports resolve the model's legend and card settings into their frame before preparation.

Prepared Cartesian guides default to horizontal lines independently of the model's legacy theme. Assigning `chart.Options.GridLineStyle` supplies a complete explicit style. Direct mutations of the returned style preserve values that differ from that legacy default, applied over the prepared defaults; this bridge cannot distinguish assigning a property its existing default value from leaving it unset. Use a whole-style assignment when that distinction matters, such as enabling vertical guides through a legacy style that already enables them. Configured grid stroke widths override the context's default width. If both legacy default style objects were modified, differing Graphite properties take precedence over differing classic properties; a whole-style assignment removes that ambiguity.

Prepared Cartesian point semantics retain the complete resolved display label, even when visible placement shortens or omits it. Explicit point labels take precedence over the value formatter. Formatting runs once per point during preparation and the detached metadata reuses that result; exporting a prepared visual does not call the formatter again. Numeric coordinates and source identities remain separate metadata fields.

Visible Cartesian axes retain a descriptive region for each configured or generated tick and each axis title, including text shortened or omitted by placement. Tick regions include the complete resolved display text and numeric value. Each axis formatter runs once per tick during preparation; measurement, placement and semantics reuse the same result. These descriptive extents do not promise an exact hit-test shape.

Automatic numeric axes keep compact captions when they distinguish ticks, and increase precision when small nonzero values round to zero or distinct ticks share a caption. Default mark values also retain small nonzero observations instead of displaying zero. Authored labels and axis or chart value formats remain authoritative. Precision affects display text; numeric values and prepared SVG/PNG snapshots remain unchanged by later model edits.

Frame headings and legends fit within the resolved size. Overflow is reported through diagnostics; a fixed render request never grows silently. Full textual values remain available through accessible metadata and descriptive regions where provided. A PNG requires concrete dimensions. Responsive hosts measure their viewport and prepare a new result; proportional SVG scaling does not claim compact-layout reflow.

Set `legendTitle` in the `VisualFrame` constructor to give categorical legend entries a measured heading. Null permits the producer's source title; an empty string suppresses it. The heading consumes the same `LegendMaximumHeightFraction` budget as its entries. `LegendMaximumRows` limits entry rows, including any overflow summary. Frame copies used by charts and grids retain all of these settings.

`VisualFrame.ShowCard` and `ShowSurface` independently enable the outer elevated card and content surface. Configure `VisualTheme.CardRadius` through the `cardRadius` constructor argument without changing `BarRadius`. The `cardShadowOpacity` argument accepts 0–1; `cardShadowColor` retains authored alpha, which multiplies that opacity. Shadows use bounded native layers inside available outer padding. Zero opacity disables the shadow, and insufficient padding produces a diagnostic instead of expanding the viewport or shifting the content.

`VisualTheme.Graphite()` reads the checked-in generated HtmlForgeX chart palette. `VisualTheme.FromJson` accepts that generated chart-token shape, not the full HtmlForgeX owner schema. HtmlForgeX governs color values; the chart look specification governs layout and typography defaults. Typography, spacing and geometry settings absent from the color export have explicit ChartForgeX defaults. Register or supply suitable fonts when portable appearance matters; equal geometry does not guarantee identical browser/native glyph painting.

`VisualTheme.ToThemeJson()` exports the complete paired theme, including typography, geometry and effects, using `schemaVersion: 1`. The independent card radius is `geometry.cardRadius`; shadow opacity and color are `effects.cardShadowOpacity` and `effects.cardShadowColor`. `VisualTheme.FromThemeJson(json)` imports that versioned format. Version-1 inputs may omit these card fields and use their defaults. Keep these round-trip methods distinct from the generated color-token importer; unsupported versions, duplicate keys and missing required fields fail explicitly.

## Prepare once, export several ways

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

`ChartSeries.Interpolation` selects `Linear`, `Smooth` or `Step` for connected line, area and range boundaries. `WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle)` places transitions halfway between mapped observations; `Start` and `End` place them at the preceding and following x positions. `AddStepLine` and `AddStepArea` default to End. Existing smooth helpers use the same setting. Explicit interpolation overrides a convenience entry point, and preparation rejects unsupported families or a step position without step interpolation. Regression and slope lines retain their straight-line meaning and reject non-linear interpolation. Segment breaks remain gaps in every mode.

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
| `NodeWidth` | `10` logical units; must be finite and positive. |
| `NodeGap` | `null` uses `max(12, theme.Spacing * 1.5)`; an explicit gap must be finite and non-negative. |
| `NodeCornerRadius` | `null` uses `theme.BarRadius`; an explicit radius must be finite and non-negative and is bounded to half the bar's smaller dimension. |
| `NodeFill` | `null` retains series/state/theme paint, including neutral default bars. |
| `RibbonFill` | `null` retains source-node series/state/palette paint. |
| `RibbonOpacity` | `.35`; accepts 0–1 for filled ribbons. An authored pattern overlay keeps its separate opacity. |

The earliest feasible layer is the longest path depth from any source. The latest is the graph's final layer minus the longest remaining path to a sink. `Left` uses the earliest layer, `Right` the latest, and `Center` uses `floor((earliest + latest) / 2)`. This midpoint policy also applies to disconnected components and keeps every edge directed to a later layer.

Explicit point colors take precedence over the corresponding family fill, then series colors and state/theme fallbacks. Layout order never reorders `Nodes` or `FlowLinks`, changes source ordinals, or reassigns point styles and ID-keyed states. All node and ribbon thicknesses use the same scale; gaps and widths do not inflate small weights. Preparation rejects node widths or gaps that cannot fit the supplied viewport. Per-flow styling, additional label placement policies and any truthful minimum-width treatment remain separate work.

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

`ChartSeries.HistogramSourcePoints` retains an immutable copy of the input observations in their original order. Each bin's `SourceIndices` indexes that collection; X is the measurement and Y is the supplied quantity, or one for scalar count input. Input array changes and edits to the series' aggregate points do not change this source snapshot. Prepared bin extents, aggregate values and contributor identities also remain detached from later model edits.

Migration: replace the former global `layout.Width` with `layout.GetWidth(index)`, because the last regular bin and authored bins can have different widths. Histogram layout, aggregation and encoding are read-only series facts. Labels, paints and styles can change after ingestion; to change bin X/Y aggregates, rebuild from observations so source statistics stay consistent.

## Aggregate source snapshots

`ChartSeries.TrendLineSourcePoints` retains the original regression observations in input order, and `ChartSeries.BoxPlotSourceSamples` retains the unsorted samples supplied to the raw-value box plot overload. These read-only collections are independent of caller-owned arrays and the fitted endpoints or summary points. A box plot built from authored five-number summaries has an empty raw sample collection.

Migration: use these typed source collections and `HistogramSourcePoints` in place of raw `source-observation` or `source-sample` region labels. Retain the series' collection when original rows are needed after preparation. `PreparedVisual.Regions` describes rendered aggregate marks; it does not expose a typed raw observation store. Its mark facts and exported SVG/PNG remain detached, and interactive histogram events retain each bin's contributor indices. Keeping source data separate avoids creating and embedding a rendering-region object for every unrendered row.

## Point markers and radial forms

`ChartSeries.Markers` is the shared point-marker configuration for connected Cartesian series, scatter, bubble, radar and polar. `ConfigureMarkers` configures one of nine built-in shapes, logical radius, visibility, fill and outline. Null dimensions and paints preserve the family defaults; explicit point colors retain precedence over the marker fill. `MarkerRadius`, `WithMarkerRadius` and `UseThemeMarkerRadius` use the same radius value. Dotted maps retain their existing radius override; their map geometry does not accept the other marker options.

```csharp
chart.Series[0].ConfigureMarkers(markers => {
    markers.Shape = ChartMarkerShape.Diamond;
    markers.Radius = 5;
    markers.StrokeWidth = 1;
});
```

`Enabled = false` or a radius of zero hides glyphs while retaining source descriptions and connected lines or areas. Bubble values retain their existing series-local size mapping; a radius override scales that mapping. Different shapes can paint different areas at the same radius, and this is not a shared cross-series bubble size domain. SVG, native PNG and legend markers use the same shape geometry. Custom marker paths and dashed marker outlines remain separate options.

`ChartForgeX.Core.ChartLineAreaForm` is the shared form type for `ChartSeries.Radar.Form` and `MetricCard.MiniSparklineStyle`. Its values are `Area = 0` and `Line = 1`; both models default to Area. Metric cards select the same form through `WithMiniSparklineStyle`.

`AddRadarArea` and `AddRadarLine` use the same Radar series kind and shared categorical/radial axes. `ChartSeries.Radar.Form` selects filled Area or unfilled Line, and `ConfigureRadar` configures the area's `FillOpacity`; null uses the theme. `AddRadar` retains the Area default. Line form rejects area opacity. Missing categories retain the existing zero-imputation behavior; an explicit missing-category policy is separate work.

Numeric radial `ChartPoint.X` identifies an ordinal category; `Y` is the signed source value. The numeric domain belongs to `YAxis` or `SecondaryYAxis` in both angular-bar and radial-column orientations. `ChartRadialGeometryOptions` owns finite clockwise start/end angles, inner radius and category/series spacing. Reversal belongs to the corresponding `ChartAxis`, so tick placement and mark projection agree. Fixed nonzero numeric bounds constrain visible geometry without rewriting observations. Percent rings use the separate `AddProgressRing` API and retain their 0–100 contract.

Numeric radial `Inside` and `Center` captions fit entirely within their painted sector. Captions that cannot fit are shortened or omitted with `numeric-radial.label-overflow`; their full source values remain in descriptive regions and point metadata. Outside captions avoid painted marks, and automatic inside ink resolves contrast against the composited fill. Explicit label colors remain caller-controlled.

Numeric radial axis text uses the existing chart options. `WithXAxis`, `WithYAxis` and `WithSecondaryYAxis` place horizontal titles in measured strips inside the shared frame. Primary and secondary value titles share the top strip; the category title uses the bottom strip. A value title participates when that axis has observations or a complete authored domain. `AxisTitleStyle` controls all title text; shortened or omitted titles report `numeric-radial.axis-title-overflow` and retain their full descriptive text. An entirely empty chart keeps its no-data surface.

`ConfigureXAxis`, `ConfigureYAxis` and `ConfigureSecondaryYAxis` set each axis's independent `LabelAngle` in fixed screen degrees. Zero is horizontal; equivalent full turns normalize before layout and paint. The angle affects category/value captions, including ellipsis measurements, without rotating titles or data labels. `ShowAxes = false` or an axis's `Visible = false` excludes its text and spacing; `ShowLine = false` controls only the axis rule. Radial/tangential orientation and numeric radial `LabelDensity` policies remain separate work.

If measured text leaves no plotted radius, `numeric-radial.insufficient-space` reports the omitted geometry. Point identities, values and descriptive regions remain available with `data-cfx-geometry-status="layout-collapse"`; the HTML adapter retains keyboard readouts without creating pointer hit areas.

## Enforcement boundary

`V2ApiConventionTests` checks the reviewed immutable contracts, their operation roles, canonical color/severity types, core-only public signatures, in-memory export signatures and detached request/output lifetime. It also checks the selected mutable chart bridge's `With*`, `Add*` and `Configure*` behavior. Public typed configuration callbacks on the Core and Visuals `With*`/`Configure*` builder boundary must use `Configure*` and return their receiver. Data-adding callbacks and substantive exports keep their operation names. Focused family and diagram fixtures protect preparation, retained semantics and explicit limits. These are compiled API and observable-output checks; they do not read this document or enforce editorial wording.

Each migrated family expands the reviewed API selection and adds its own data/layout/output fixtures. The old surface remains inventoried rather than being subjected to a blanket prefix rule during a partial migration. Do not weaken a current contract merely to pass the naming test, and do not claim that a naming check proves visual quality, package publication or consumer compatibility.
