---
description: "Choose supported chart-family combinations, orientations, shared axes and label configurations, with explicit native rendering boundaries."
seo_title: "Chart combinations and axes | ChartForgeX"
---

# Chart combinations, orientation and axes

Combine series when they share a coordinate system. Each series keeps its own stable ID, observations and configuration; one chart owns the axes, frame and theme. The tables below describe the supported layouts. The [family ledger](chart-capabilities.csv) records individual options, and [API conventions](api-conventions.md) covers observations, stacks and missing values.

## Cartesian layouts

| Layout | Supported composition | Axes and constraints |
| --- | --- | --- |
| Vertical Cartesian | Line/StepLine, Area/StepArea/StackedArea, Bar, Scatter/Bubble, ErrorBar, Candlestick/OHLC, RangeBand/RangeArea/RangeBar, DifferenceArea, Lollipop, Dumbbell, BoxPlot, Slope and TrendLine | Share one X projection. Each series chooses primary or secondary Y. Typed ranges and financial values contribute their complete bounds. Family-specific interpolation, stacking and value constraints still apply. |
| Vertical stacks | Bars or stacked areas, optionally alongside other Cartesian series | Stack membership is separate for each kind, value axis and named group. Positive and negative normalization is independent. Interpolation and normalization targets must agree within a stack. Histogram bars retain their authored bin bounds. |
| Horizontal bars and lines | HorizontalBar with horizontal Line/StepLine | Authored X is the category on physical Y; authored Y is the value on physical X. Each series chooses primary or secondary physical X through ValueAxis. A line does not consume a bar slot. RangeBar and BoxPlot companions also require horizontal orientation. |
| Horizontal comparisons | Horizontal Slope, Lollipop or Dumbbell with other horizontal Cartesian series | Source X remains the stage/category on physical Y. Values use primary or secondary physical X. Slopes retain two or more authored stages and explicit segment breaks; vertical slopes can join ordinary vertical Cartesian mixtures. |
| Horizontal ranges | RangeBar with other horizontal Cartesian series | Primary or secondary physical X. Each companion must use horizontal orientation. Vertical RangeBar can join ordinary Cartesian mixtures. |
| Horizontal box plots | BoxPlot with other horizontal Cartesian series | Primary or secondary physical X. Each companion must use horizontal orientation. Vertical BoxPlot can join ordinary Cartesian mixtures. |
| Waterfall | Exactly one Waterfall series, vertical or horizontal | Its cumulative layout is exclusive. ValueAxis selects primary or secondary Y when vertical and primary or secondary X when horizontal. |

For example, bars and a line can share categories while retaining separate value scales:

```csharp
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Themes;

var orders = ChartSeries.CreateBar(new ChartSeriesId("orders"), "Orders",
    ChartPoints.FromValues(28, 34, 41));
var latency = ChartSeries.CreateLine(new ChartSeriesId("latency"), "Latency",
    ChartPoints.FromValues(120, 95, 110));
latency.UseSecondaryValueAxis();

var chart = Chart.Create()
    .WithTheme(VisualTheme.Graphite())
    .WithTitle("Orders and response time")
    .WithXLabels("Monday", "Tuesday", "Wednesday")
    .WithYAxis("Orders")
    .WithSecondaryYAxis("Latency (ms)")
    .AddSeries(orders)
    .AddSeries(latency);
chart.ConfigureYAxis(axis => axis.Minimum = 0);
chart.ConfigureSecondaryYAxis(axis => axis.Minimum = 0);
chart.SaveSvg("orders-and-latency.svg");
chart.SavePng("orders-and-latency.png");
```

A horizontal mixture uses the same `ValueAxis` assignment and configures its two physical X axes:

```csharp
using ChartForgeX.Core;
using ChartForgeX.Primitives;

var chart = Chart.Create().WithSize(800, 440)
    .AddHorizontalBar("Orders", new[] { new ChartPoint(1, 28), new ChartPoint(2, 34), new ChartPoint(3, 41) })
    .AddLine("Latency", new[] { new ChartPoint(1, 120), new ChartPoint(2, 95), new ChartPoint(3, 110) })
    .WithXAxis("Orders").WithSecondaryXAxis("Latency (ms)")
    .WithXAxisBounds(0, 50).WithSecondaryXAxisBounds(0, 150)
    .WithXLabels("Monday", "Tuesday", "Wednesday");
chart.Options.LineOrientation = ChartOrientation.Horizontal;
chart.Series[1].UseSecondaryValueAxis();
var svg = chart.ToSvg();
var png = chart.ToPng();
```

Here `WithXLabels` supplies the existing fallback category catalog on physical Y. Explicit Y labels take precedence. A horizontal source point still stores category in `ChartPoint.X` and value in `Y`; orientation changes their physical projection, not their source meaning. Native viewport requests use physical names, so this chart's category window is `y` and secondary value window is `secondaryX`.

Category alignment follows source X coordinates rather than series positions. The shared category owner groups corresponding bar and radial coordinates within four floating-point ULPs while retaining the original observations. Missing observations stay absent unless the selected family explicitly defines derived values. A line gap requires an authored segment break; an omitted point alone does not promise a gap in a connected line. An authored zero remains an observation.

StackedArea keeps that independent behavior by default. Members of one stack group and value axis can instead agree on `StackedAreaMissingData = Gap` or `Zero`. Gap breaks the aligned contour wherever any member lacks a position; Zero builds contour vertices on the observed union with zero contribution for absent members. Explicit breaks cut the complete aligned group. These policies preserve raw values, signed/normalized totals, source targets and authored zeros; derived vertices never become observations. Policies may differ across named groups or primary/secondary axes and cannot differ inside one group. Aligned groups require unique increasing resolved X positions.

RangeBand/RangeArea and DifferenceArea retain one complete typed tuple per observation. Nullable paired factories omit incomplete tuples using explicit Gap/Connect policy. Independent `ChartBoundaryInterpolation` overrides change only connected geometry, not tuple identity or shared X alignment. Smooth DifferenceArea crossings use the same flattened curves as native SVG/PNG; unsupported nonlinear value axes still reject. See [paired boundary policies](../api-conventions/#independent-paired-boundaries-and-nullable-input).

## Radial layouts

| Layout | Supported composition | Axes and missing values |
| --- | --- | --- |
| RadialBar | Homogeneous RadialBar series | Categories occupy radius bands; values occupy angles. Primary and secondary numeric scales are independent. Standalone repeated X observations retain distinct slots. |
| RadialColumn | Homogeneous RadialColumn series | Categories occupy angle bands; values occupy radii. Primary and secondary scales, named stacks and signed normalization are supported. Standalone repeated X observations retain distinct slots. |
| RadialColumn with Radar | RadialColumn plus Radar line and/or area series, in either order | Supports a finite positive sweep of at most one turn and requires unique categories within each series. Populated radar requires at least three combined categories. Radar uses whole-category centers and its selected primary/secondary scale; it does not consume column slots or enter column stack totals. Missing radar categories use the selected Zero/Gap/Connect policy; missing columns stay absent. Partial sectors clip paint; Gap does not join the two physical ends. |
| Radar | Homogeneous Radar line and/or area series | Category angles align across series; each series selects a primary/secondary radius scale. Require at least three combined categories and unique categories within each series. Zero preserves derived zeros without authored observation IDs; Gap leaves open observed runs and Connect joins existing vertices. Incomplete Gap area forms omit fill. |
| Polar | Homogeneous Polar line and/or area series | X is an authored angle in radians; Y is radius. Source order and explicit BreakBefore boundaries determine independent runs. Line defaults to open straight joins; optional closure and shared cubic curves remain per run. Area closes only runs with at least three vertices. Primary and secondary radius scales are independent. Missing categories are not synthesized. Radii and the selected minimum must be nonnegative. |
| PolarArea | Exactly one PolarArea series | Equal angular sectors encode area from nonnegative values. Authored zeros retain their slots. This layout has its own area contract rather than an ordinary numeric-radius axis. |
| Pie/Donut bands | One ordinary Pie or Donut, or multiple Pie/Donut series with explicit disjoint `ChartRadialBand` values or one `ChartRadialBandAllocation` | Each series retains its total, slice shares, IDs and aggregation. Automatic rings follow source-series order and cannot mix with explicit bands. Nullable observation-radius ratios change only band thickness. Only the innermost hole owns center text. Inner external captions share measured overlay lanes. Exact positive slice translations share one fitted frame and may overlap neighbouring bands in source-series paint order; band ratios describe the unshifted geometry. This composition does not share the numeric RadialBar/RadialColumn axes. |

RadialBar and RadialColumn support signed linear, explicit positive or negative logarithmic and symmetric logarithmic value scales. Time value axes, explicit category bounds and logarithmic category spacing reject. RadialBar cannot mix with RadialColumn; Radar cannot mix with Polar. The numeric cross-kind radial layout is RadialColumn with Radar; explicit or automatically allocated Pie/Donut bands form a separate part-to-whole composition. See [band configuration](api-conventions.md#explicit-pie-and-donut-bands).

Category-axis reversal is supported for numeric radial charts, Radar and Polar, including mixed columns/radar. Numeric radius reversal uses the selected value axis. Polar-area radius reversal rejects. Schedule X reversal changes time direction; schedule Y reversal changes source lane order. Equivalent coordinates within one radar or mixed-radial series are ambiguous and reject before preparation or export; cross-series equivalents align through the shared category owner.

The compiled [mixed radial recipe](../../ChartForgeX.Examples/Recipes/MixedRadialRecipe.cs) demonstrates retained series handles, curved radar forms and category reversal. The [comparison recipe](../../ChartForgeX.Examples/Recipes/ComparisonRecipe.cs) demonstrates ordinary line/area/bar mixtures. Their gallery downloads include the selected factory and ordinary package imports.

## Constructing and updating supported combinations

The compiled [supported combination recipe](../../ChartForgeX.Examples/Recipes/SupportedCombinationRecipe.cs) uses the same series and chart vocabulary for three layouts:

- `vertical-secondary` combines grouped bars with typed Bubble observations on a secondary percentage axis. Bubble size remains independent of its percentage value.
- `horizontal` combines grouped horizontal bars with a Line on the primary physical X value axis. The category captions and parent groups belong to physical Y.
- `mixed-radial` combines RadialColumn with a Radar line on a shared numeric scale. Complete location names identify each radial category.

Each layout retains explicit series and observation IDs. Repeated leaf captions retain their parent context, omitted observations stay absent, and an authored zero keeps its identity. Prefix a variant with `updated-` to exercise changed values and paint order. The vertical variant replaces the complete typed Bubble collection, then assigns observation IDs in its new order; full typed replacement clears earlier observation metadata. The numeric variants update an item through `Points[index]`, preserving its ID. `MoveSeries` changes paint order without changing identity or configuration.

Prepare a new scene after source changes and reuse the immutable paired theme with the intended mode. A previously prepared scene remains detached from later source, axis and theme edits. `ToSvg()` and `ToPng()` on that preparation export the same committed facts. In a browser host, pass a newly generated section to the existing controller's `update`; surviving explicit identities retain selection, legend state and keyboard focus. These recipes exercise supported mixtures without widening horizontal or radial compatibility rules.

## Independent layouts and qualification

Matrix, schedule, map, relationship, part-to-whole and scalar families use their own layouts. They cannot share a chart with unrelated Cartesian or radial series. Heatmap/HexbinHeatmap rows and Bullet retain their documented homogeneous multi-series layouts. Timeline, StateTimeline, GanttLane and Gantt also support a shared schedule composition: one primary linear/time X domain, real source rows and packed lane subrows, with full-height Gantt markers as overlays. Secondary/nonlinear schedule mixtures reject. See [schedule configuration](../api-conventions/#shared-schedule-rows-and-physical-reversal). Numeric and categorical heatmap rows cannot mix. Pie/Donut allow their explicit or automatically allocated disjoint-band composition; other exclusive layouts require one series. Use a chart grid when independent coordinate systems belong together in a report.

Preparation, SVG and native PNG use the same compatibility checks. Existing regression tests cover representative shared domains, missing values, secondary scales, horizontal constraints, stacks, radial mixtures and rejection paths. This is a supported-layout contract rather than a claim that every pair of family options has been exhaustively tested. The [roadmap](../../TODO.md) retains deeper visual, lifecycle and platform qualification work.

Dedicated Radar and Polar axis captions accept fixed or upright tangent/spoke-relative orientation through the shared axis-text owner. `PolarRotationDegrees` rotates dedicated Radar, Polar and PolarArea coordinates from their existing starting rays; mixed columns/radar use `RadialGeometry` instead. Source point navigation does not imply an angular X-window. Partial mixed sectors retain all source observations and use the shared numeric radial projection.
