# Interactivity Reference

ChartForgeX keeps static SVG, PNG, and HTML output deterministic by default. Browser behavior lives in `ChartForgeX.Interactivity.Html` and is opt-in through reusable feature flags on `ChartInteractionOptions`.

The HTML adapter works from renderer metadata such as `data-cfx-series`, `data-cfx-series-key`, `data-cfx-point`, `data-cfx-label`, `data-cfx-id`, and `data-cfx-role`. Chart families can expose their own shapes and still reuse the same hover, selection, keyboard traversal, compare tray, crosshair, lasso, focus trail, reveal label, scenario, and playback contracts.

Selection and host highlights retain visible cues without changing native fill colors. Painted shapes use outlines, connected points retain their prepared markers, and text-only marks use a measured outline around their text. These cues follow the current drawing after resizing or replacement and disappear when their mark becomes hidden or their state clears. Interactive SVG and PNG downloads retain persistent selection and host-highlight cues; transient hover and keyboard-focus outlines are omitted.

The selected-target compare tray appears below the chart viewport, preserving axes, outer labels, and pointer access to marks. Its controls scroll horizontally inside compact hosts, including when reached by keyboard. Clearing the selection returns focus to the chart when the clear button held focus. Graphite charts use the chart's surface, text, and accent colors for these controls.

The tray pages through every selected target, showing up to six inspection chips at a time. Previous and next buttons and a translated range caption keep larger selections reachable without changing the selection. The current page survives resizing and clamps to the available targets after an update. Activating a chip inspects its target without changing the selection. With `Tooltip.PinOnSelect` enabled, inspection switches the pinned readout; activating the same chip keeps it pinned. With pinning disabled, the inspection readout closes when pointer and keyboard focus leave the tray. When enough viewport space is available, the readout stays above the visible tray so its controls remain usable.

Native painted marks retain their observation identity when pointed at. Cartesian producers declare `data-cfx-coordinate-system="cartesian"` on their series groups; inferred observations and crosshair guides require that scope. A producer declares polar geometry with `data-cfx-coordinate-system="polar"` on the SVG or a containing semantic group. Polar and other native-only families use SVG hit testing, leave empty stage space without an inferred target, and suppress the Cartesian crosshair. Nested observations inherit their containing series identity.

Every rendered interaction surface is normalized to `data-cfx-target-kind` and `data-cfx-target-id` before bindings run. The semantic target kinds are `series`, `point`, `annotation`, `region`, `node`, `link`, and `legend`. The adapter also retains `element` as the fallback for an identified native surface without one of those semantic kinds. Hosts can therefore route one scenario across Cartesian marks, topology nodes and links, annotations, map regions, and legend entries without depending on renderer-specific markup. Use `AddRegionStep`, `AddNodeStep`, `AddLinkStep`, or `AddLegendStep` when building those routes.

For native annotation lines and bands, use the opaque `targetId` returned by `resolveTargets()`. Captions can repeat; each returned target addresses its own annotation. Automatic annotation IDs belong to the current chart source, remain local to that chart, and clear on full source replacement. They do not establish identity across reordering. Code that constructed annotation target IDs from captions should resolve current targets and filter their `label` fields before passing them to `select`. Annotation scenario steps retain label, kind and value aliases; an exact annotation ID takes precedence over another annotation's matching caption.

Supply a `ChartAnnotationId` when an authored line or band must retain selection, host highlight or keyboard focus across native source replacement. The same ID identifies the annotation when its caption, coordinates or collection position change. IDs must be unique within one chart. Resolved authored targets expose `annotationId`, and snapshots retain that identity. A removed ID clears its state; another annotation with the same caption or value does not inherit it. Equal explicit IDs share interactions only when both charts enable `SynchronizedCharts` and use the same nonempty group.

```csharp
var threshold = new ChartAnnotationId("threshold.warning");
chart.AddHorizontalLine(threshold, 15, "Limit");
options.Interaction.AddScenario("warning", "Warning limit",
    route => route.AddAnnotationStep(threshold));
```

The typed scenario overload lives in `ChartForgeX.Interactivity.Html` and uses the native identity formatter. The `annotation-id-` target namespace is reserved for exact authored references; a missing target in that namespace never falls back to a caption, kind or value alias. Hosts can also match `{ targets: [{ targetKind: 'annotation', annotationId: 'threshold.warning' }] }` through the ordinary command API. Native catalog bindings determine durability and sharing; caller-provided scope hints do not promote anonymous annotations. This contract covers existing native line/band render paths, including schedule vertical lines. Statistical/focus helpers continue creating anonymous annotations; other diagram annotation models have their own owners.

Each painted word-cloud term owns one interaction target. Its descriptive source record remains available with the complete source inventory, including terms omitted by weight or layout limits. Those records do not create duplicate selections or keyboard stops.

Explicitly decimated series keep their original point identity. `data-cfx-point` remains the rendered ordinal, while the series source-index map and each host event's `sourcePoint` identify the caller's original point.

Aggregate marks such as histogram bins, merged timeline runs, and pie slices expose contributing source observations through `target.sourcePoints`, a numeric array in hover and selection events. An empty histogram bin reports `[]`. For these marks, `target.sourcePoint` is `undefined` because one source observation cannot identify an aggregate. Target IDs include the stable series ID and derivation kind. Histogram identities also retain interval bounds, terminal-boundary inclusion, aggregation, and encoding; matching intervals can synchronize across different bin ordinals, while different summary policies remain independent. Grouped pie slices use their contributing source indices, and merged timeline runs use their start/end interval. Derived summaries cannot select an unrelated observation or a different summary kind merely because their rendered ordinals match. Point legends share their mark's contributors and target ID. Hosts should route aggregate selection by the opaque `targetId` and read `sourcePoints` when they need the underlying observations.

Point-legend muting and isolation use the same identity rules across charts. Zero-valued pie and donut legends retain their derived identity and contributors even when no slice is painted. Isolating a point keeps its mark at full strength, including a previously muted point; clearing isolation restores its prior mute state.

Raw trend-line and box-plot summaries omit both singular `sourcePoint` and contributor arrays. Their source data remains available through the series' immutable `TrendLineSourcePoints` and `BoxPlotSourceSamples` collections. Migration: derived target IDs now include their derivation kind; use event-provided IDs instead of constructing IDs from point ordinals.

Radar's default `Zero` missing-category policy fills missing categories with zero geometry. Those categories are `region` targets identified by the stable series ID and category, for example `target:category:2`. They report `sourcePoints: []`, with no `point` or `sourcePoint`; their browser readout says "No observation" and carries no measured value. An authored zero remains an ordinary source point with a value of zero. Category identities synchronize correctly when peers order their observations differently or include another category. `Gap` and `Connect` are explicit alternatives that preserve missing data without fabricating zero geometry; configure them through `ConfigureRadar`.

## Keyboard navigation

When `ChartInteractionFeatures.KeyboardNavigation` is enabled, data marks and legend entries are separate roving components. Tab enters each component once and returns to its last active target; Left/Right and Home/End move within a series or legend. Up/Down switches data series at a matching coordinate or source observation, with an ordinal fallback for uneven series. Families without a series/point grid use their deterministic rendered-target order. Hidden targets and aggregate series wrappers do not become extra data stops.

Muted data leaves the data component, while its legend stays reachable for unmuting. Reset restores the data component after all series are muted. Charts initialized inside a host hidden with `hidden` or `display: none` acquire their Tab stops when the host regains layout; hiding and revealing the host preserves each component's active target.

Moving to an offscreen target scrolls the chart's readable viewport locally. A target wider than the usable viewport keeps its current visible portion when focused; a fully offscreen target still scrolls into view. Native data links retain Enter navigation, while Space selects a data target when selection is enabled. Legend Space toggles muting and Shift+Space toggles series isolation. `cfxnavigate.index/count` refer to the active data or legend component. Disabling keyboard navigation leaves host key handling and authored links available without adding adapter navigation stops.

## Graphite appearance

Graphite charts use a surface tooltip with a 1 px border, 6 px radius and shadow. Shared-x tooltips show a bold x label, 10 px series swatches, and bold full numeric values in a right-aligned tabular column. Rows sort declared states before ordinary values; swatches follow the rendered series colours, including host SVG properties. Quiet series use muted text. A 1 px dashed crosshair follows the axis token; hovered points grow to a 4 px radius with a 2 px surface ring. Pointing at a mark or legend item, or focusing one with the keyboard, keeps that series and the hovered point's marker at full strength while other series dim to 30%; pie-like point legends emphasize one point. Over the plot background the crosshair is a shared readout and every series stays at full strength. The chart root exposes the current mode as `data-cfx-hover-mode` (`series` or `shared`).

Legend tooltips summarize the series for readers: the name with its colour swatch, plus the latest value and its x label for trend series (line, area, step, slope), the total for bar and lollipop series, or the point's value for point legends. They never show renderer metadata such as role or kind.

Muting a series retains it at 30% opacity and strikes through its legend label. Keyboard targets use a 2 px focus outline in the host accent.

When a native legend exceeds its `WithLegendBudget`, open **More legend entries** to read the omitted captions in pages of six. Previous and next controls show the current range and total. Captions retain their full native descriptions, including values where the native legend supplies them. The disclosure uses normal HTML focus and follows the host's language, direction and control colours.

With `ChartInteractionFeatures.LegendToggles` enabled, each actionable entry offers mute and isolate buttons with pressed states. These use the same series, observation, distribution or named-node identity as the native legend; linked charts and later native preparations retain the existing identity rules. State-category keys and entries shown with legend actions disabled remain descriptive. A grouped pie **Other** entry remains one native aggregate with its contributors. Opening or paging the disclosure retains the chart's selection and plotting area. Native SVG/PNG downloads and print keep the bounded native legend; the disclosure stays in the browser.

There is no permanent toolbar unless zoom, pan, brush, or export are enabled. `IncludeResetButton` (on by default) adds a 28 px "Reset view" ghost button below the chart, keeping titles and marks clear. Its row is reserved so selecting a mark does not move the report content below it. The button appears only while the view differs from its initial state (zoomed, panned, brushed, a point selected, or a series muted or isolated) and hides again after reset, returning keyboard focus to the chart. The optional compare tray also reserves one row; its chips scroll horizontally, including when reached by keyboard. Hosts that supply their own controls can disable `IncludeResetButton` and `CompareMarkers`. Resetting also clears selections, focus trails, and pinned tooltips. Hosts can set `--cfx-host-accent` on the interactive container to align that outline with their own controls. Static exports retain the same flat marks and colour roles without browser behavior.

A cancelled brush gesture restores the previous brush presentation without committing new bounds or changing the existing selection.

## Tooltip modes

`HtmlChartInteractionOptions.Tooltip.Mode` selects the readout independently of the theme or palette. The default, `HtmlChartTooltipMode.SharedX`, shows one visible observation per series at the target's numeric x coordinate. When several observations share that x, the tooltip uses the pointed or focused observation for its series and the first eligible observation for each other series. Rows retain full source values and sort by declared state, then descending value. Muted series, hidden marks, and missing or non-finite values are omitted. Swatches follow the painted marks, including point colour overrides, gradient fills, and host SVG properties; marker-free lines use their line paint.

Visibility follows the primary data mark. An ancestor's `display:none` or zero opacity hides its descendants; a mark can restore inherited `visibility:hidden` with `visibility:visible`. Hidden line markers retain a shared row while the series path remains visible at the observation's location. Marks outside a native rectangular plot clip cannot supply pointer readouts. Word-cloud terms and annotation captions use their actual text fill or stroke; ordinary point labels and focus decorations do not replace hidden data paint. Charts marked `AsDecorative()` retain pointer tooltips because `aria-hidden` affects accessibility exposure, not painted visibility.

Use `Single` to inspect the pointed or focused target and its metadata:

```csharp
chart.SaveInteractiveHtml("observations.html", options => {
    options.Tooltip.Mode = HtmlChartTooltipMode.Single;
});
```

Shared-x readouts use Cartesian source observations, including scatter and bubble values. Targets with coordinates that describe layout or categories, such as heatmaps, maps, pie slices, and radial charts, keep a single-target tooltip with their value and metadata. Range and financial summaries also retain their individual bounds and measures rather than reducing them to one shared value. Derived regression endpoints use the same single-target fallback. Single-target readouts show the series, values, bounds, and caller-supplied metadata. Renderer roles, source ordinals, and chart-kind names are omitted from the tooltip; normalized identities and host events retain those fields. Legend tooltips keep their series summaries in either mode, including muted series. `HtmlInteractiveDashboardOptions.Tooltip.Mode` applies the same choice to every child chart. Both modes require `ChartInteractionFeatures.Tooltips`; choosing a mode does not enable the feature.

## Pointer acquisition and crosshair labels

`Tooltip.Range` controls how pointer movement acquires a readout:

- `HtmlChartTooltipRange.Exact` requires a native hit on a painted target.
- `HtmlChartTooltipRange.Nearest` acquires the nearest painted Cartesian observation anywhere inside the chart stage.
- `HtmlChartTooltipRange.WithinDistance(cssPixels)` limits that lookup to a finite, non-negative distance from the observation's screen centre. The default is 120 CSS pixels. A zero distance still permits native hits.

Distances use CSS pixels after responsive layout and SVG transforms, so the same setting works with `Fit` and `Readable`. Heatmap, map, schedule, hierarchy, scalar, polar, annotation, and legend targets retain native hit semantics. Inferred lookup does not extend outside the stage. Hidden paint and muted data cannot become pointer readouts through a retained bounding box or a legend colour. A visible child can restore inherited visibility, and marker-free observations remain eligible while their series path is painted at that location. An `Exact` native hit on a connected line, area, or range envelope retains its series summary during pointer movement; the crosshair can independently guide to a real nearby observation. `Nearest` and `WithinDistance` can refine that native summary to an eligible observation. Keyboard focus and pinned readouts retain their explicit targets, including keyboard-only authored zero or precision-collapse facts.

Tooltip acquisition works with `ChartInteractionFeatures.Tooltips` even when `Crosshair` is disabled. The crosshair keeps its own 120 CSS pixel guide range. `Crosshair.ShowLabel` controls the guide's label and defaults to true for every palette; set it to false when a compact layout already provides the same information in the tooltip. These options do not enable either feature.

`cfxcrosshair` and crosshair synchronization follow the guide's observation independently of a retained `Exact` tooltip summary. They update when the guide's observation or series/shared emphasis changes and restore the guide after a native hover change. Pointer movement within the same state does not repeat these notifications.

```csharp
chart.SaveInteractiveHtml("observations.html", options => {
    options.Tooltip.Range = HtmlChartTooltipRange.WithinDistance(64);
    options.Crosshair.ShowLabel = false;
});
```

`HtmlInteractiveDashboardOptions.Tooltip` and `.Crosshair` apply the same settings to every child chart. Replace the former flat `TooltipMode` property with `Tooltip.Mode` when migrating existing adapter configuration.

`Crosshair.Stroke`, `StrokeWidth` and `LineStyle` override both guide lines. Each defaults to null, retaining the existing appearance: ordinary charts use solid blue guides with a soft glow, while Graphite uses dashed guides in the axis color. Explicit styling uses crisp strokes without glow. Widths are finite, non-negative CSS pixels; zero width or a transparent stroke hides the lines while retaining the guide label, target identity and notifications. The line styles are `Solid`, `Dashed` and `Dotted`.

```csharp
chart.SaveInteractiveHtml("readings.html", options => {
    options.Crosshair.Stroke = ChartColor.FromRgb(14, 116, 144);
    options.Crosshair.StrokeWidth = 2;
    options.Crosshair.LineStyle = HtmlChartCrosshairLineStyle.Dotted;
    options.Crosshair.ShowLabel = false;
});
```

These paint options do not enable crosshairs, change tooltip acquisition or alter native SVG and PNG exports. Dashboard children receive the same settings. `HtmlCrosshairExamples.CreatePage` and the Graphite gallery provide executable examples of all three patterns in light and dark palettes.

Set `Tooltip.DelayMilliseconds` to reduce transient pointer readouts when scanning a chart. Its default is `0`, so tooltips appear immediately. The delay accepts non-negative integers and starts once for each acquired target; movement within that target updates the eventual position without postponing it. Switching targets or acquiring with a new pointer contact starts a new delay. Events from an older contact do not move or dismiss the newer contact's readout. Leaving, resetting, hiding or removing the chart, or making the target unavailable cancels a pending readout. Content and painted availability are checked again when the delay expires.

```csharp
var html = chart.ToInteractiveHtmlPage(options => {
    options.Tooltip.DelayMilliseconds = 250;
    options.Tooltip.Range = HtmlChartTooltipRange.WithinDistance(64);
});
```

Keyboard focus and explicit pins appear immediately and cancel pending pointer readouts. Hover emphasis, crosshair guides and dashboard synchronization remain immediate. The dashboard tooltip options apply the same delay to each child. Configuring a delay does not enable the `Tooltips` feature or affect native SVG and PNG exports.

The [light Graphite example](../Website/static/examples/generated/graphite-light-line-interactive.html) and [dark Graphite example](../Website/static/examples/generated/graphite-dark-line-interactive.html) use a 360 ms delay with shared readouts. Generate both with `dotnet run --project ChartForgeX.Examples -c Release -- --graphite-only`.
## Tooltip position

`Tooltip.Position` selects the readout's screen anchor and placement independently of its content or acquisition range. `HtmlChartTooltipAnchor.Pointer` is the default and follows the latest pointer; keyboard focus uses the target's centre. `Node` uses the acquired native target's current bounds. `Chart` uses the displayed chart stage, excluding controls and compare panels; a narrow `Readable` chart uses its contained viewport rather than its wider SVG.

`Placements` is a copied, read-only sequence of directions: `Top`, `TopRight`, `Right`, `BottomRight`, `Bottom`, `BottomLeft`, `Left`, and `TopLeft`. The adapter tries them in order using the rendered tooltip size and an 8 CSS pixel inset inside the visual viewport and any containing host that clips the fixed readout. If none fits, it clamps the first choice to those bounds. The default is one `BottomRight` placement with a 14 CSS pixel `Gap`. A diagonal placement applies the gap on both axes; `OffsetX` and `OffsetY` add signed CSS pixel offsets after anchoring. The gap must be finite and non-negative, and both offsets must be finite.

```csharp
chart.SaveInteractiveHtml("readings.html", options => {
    options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
    options.Tooltip.Position.Placements = new[] {
        HtmlChartTooltipPlacement.Top,
        HtmlChartTooltipPlacement.Bottom
    };
    options.Tooltip.Position.Gap = 12;
    options.Tooltip.Position.OffsetX = 6;
});
```

Visible node and chart readouts follow page or contained scrolling, resizing, and adapter zoom/pan changes, including pinned readouts. A partially visible source keeps its readout; a source that becomes fully hidden, clipped away or removed loses it. Placement follows the browser's visual viewport and any containing host that clips the fixed readout. Scrolling through slotted shadow hosts and slot reassignment refresh the active readout's geometry and visibility.

Keyboard navigation brings the selected source into view immediately through accessible containers, including hidden overflow or smooth scrolling. When a source is wider than a clipping viewport and already partly visible, navigation preserves that overlap. Geometry listeners belong to the active readout and are released when it closes. Availability refreshes on DOM, scrolling, resize and transition events; idle CSSOM-only stylesheet edits require a later event.

Closed shadow hosts use the browser's native intersection information and native reveal for small marks. When a small painted source sits inside a larger transparent hit area, clipping only the source inside an opaque closed host may leave it eligible.

Delayed pointer readouts use the latest pointer and live anchor bounds when shown. Dashboard children receive independent copies of the same position settings. Placement settings do not enable tooltips or change native SVG and PNG exports. Executable pointer, node, and chart examples are in the Graphite gallery and `HtmlTooltipPositionExamples.CreatePage`.

## Semantic Series Identity

Create a series with a `ChartSeriesId` when equivalent measures should synchronize across charts. Retain the series handle to configure it, then attach it with `AddSeries`. Each chart receives its own series instance; equivalent series share the same explicit ID:

```csharp
using ChartForgeX.Core;
using ChartForgeX.Primitives;

var passRateId = new ChartSeriesId("quality.pass-rate");
var currentSeries = ChartSeries.CreateLine(passRateId, "Current pass rate",
    ChartPoints.FromValues(96, 98, 99));
var historySeries = ChartSeries.CreateArea(passRateId, "Pass rate history",
    ChartPoints.FromValues(92, 96, 98)).WithInterpolation(ChartInterpolation.Smooth);

var current = Chart.Create().AddSeries(currentSeries);
var history = Chart.Create().AddSeries(historySeries);

currentSeries.WithName("Pass rate today");
```

`series.Id` is immutable. Renaming the display text with `WithName`, configuring the handle or moving it with `chart.MoveSeries` preserves its ID. Display names may repeat; IDs must be unique within a chart. `Chart.Series` is a read-only view of attached handles, with structural changes made through `AddSeries`, `RemoveSeries`, `MoveSeries` and `ClearSeries`.

Fluent `chart.AddLine(...)`, `AddBar(...)` and other `Add*` helpers allocate deterministic chart-local IDs. These remain fixed through renaming and reordering, but do not synchronize series or observation interactions across charts. Equal display names do not connect charts. The `cfx:auto:` namespace is reserved for these automatic IDs; supply an explicit ID when connecting equivalent measures in a dashboard.

Synchronized legend, hover and selection state uses `data-cfx-series-key`, which carries `series.Id.Value`. Series positions remain local rendering details. A peer without the explicit series ID remains unchanged. Point-level legends retain their observation identity, so toggling one pie or radial item affects that item. Connected line, area, radar, polar and other aggregate paths retain a series legend.

### Observation identity

Use `WithObservationId` when equivalent charts can present their source observations in a different order:

```csharp
var latency = ChartSeries.CreateScatter(new ChartSeriesId("service.latency"), "Latency",
    new[] { new ChartPoint(1, 24), new ChartPoint(2, 31) })
    .WithObservationId(0, new ChartObservationId("east"))
    .WithObservationId(1, new ChartObservationId("west"));
var latencyChart = Chart.Create().AddSeries(latency);
```

The index refers to the authored source observation, including an original observation retained by decimation. Assign the same observation ID to the corresponding source position in each peer chart. IDs are unique within a series. Typed `Update*` methods preserve the observation's ID; `Replace*` methods clear observation IDs when replacing the complete source.

Prepared output captures explicit IDs in `data-cfx-observation-id`; host event targets expose `observationId`. Point legends carry their referenced observation's identity. Use the event's opaque `targetId` to address a rendered target, and `sourcePoint` to read its source position. Aggregate identities continue to describe their derivation and contributors.

### Series scenario steps

Pass the retained series ID to `AddSeriesStep`. The typed overload is available from `ChartForgeX.Interactivity.Html` and delegates to the host-neutral scenario contract:

```csharp
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

current.SaveInteractiveHtml("pass-rate.html", options => {
    options.Interaction.AddScenario("quality-review", "Quality review", scenario => scenario
        .AddSeriesStep(currentSeries.Id, "Inspect pass rate"));
});
```

The string overload also addresses an exact stable series ID. Numeric strings have the same meaning: `AddSeriesStep("0", ...)` targets the series created with `new ChartSeriesId("0")`, regardless of its position or display name. To target an automatically identified series in its own chart, pass that handle's `Id`.

## Host-Owned Assets

Assetless fragments declare `data-cfx-asset-source="host"` on their root. Self-contained fragments use `inline`, while complete pages use `document`. Hosts such as HtmlForgeX can register CSS and JavaScript once and consume the fragment directly without parsing or rewriting ChartForgeX markup.

Complete pages stay self-contained by default. Report bundles that ship many pages can reference the runtimes as shared files instead, so the bundle carries one copy of each runtime:

```csharp
var assets = new HtmlAssetReferences("assets/");               // relative path or http/https URL
HtmlInteractiveAssetFiles.WriteTo(Path.Combine(bundle, "assets"),
    HtmlInteractiveAssetFiles.Charts().Concat(HtmlInteractiveAssetFiles.GraphExplorer()));
chart.SaveInteractiveHtml(Path.Combine(bundle, "latency.html"), options => options.ExternalAssets = assets);
```

`HtmlInteractiveDashboardOptions.ExternalAssets` and `HtmlGraphExplorerOptions.ExternalAssets` work the same way, and `HtmlInteractiveTopologyRenderer.RenderPage(chart, options, assets)` links the topology runtime from `HtmlInteractiveAssetFiles.TopologyScript(options)` (topology page styles stay inline because they depend on the render options). File names carry a content hash (`cfx-interactive.<hash>.js`), so every page of a bundle points at the same file and an upgraded runtime never collides with a cached copy. `WriteTo` leaves identical files untouched. Set `IncludeIntegrity = true` to add `sha384` Subresource Integrity attributes for hosted bundles; it is off by default because browsers refuse integrity-checked assets on `file://` pages, and integrity-checked assets are requested with `crossorigin="anonymous"`, so assets on another origin need CORS headers. Script nonces are kept on external script tags.

## Updating embedded charts

Use `ChartForgeXCharts.mount` when a host inserts a chart after registering the shared runtime. Pages and fragments that include their runtime mount automatically. Mounting the same connected section again returns its existing controller:

```js
const root = document.querySelector('.cfx-interactive-chart');
const controller = ChartForgeXCharts.mount(root);

// replacementHtml is a fresh ToInteractiveHtmlFragmentWithoutAssets result.
controller.update(replacementHtml);
controller.resize();

// Release browser work when the host no longer needs the chart.
controller.dispose();
```

Prepare replacement content through the same C# chart API used for the original view. Update typed observations or replace their collection, assign the intended observation IDs, and call `chart.ToInteractiveHtmlFragmentWithoutAssets(...)` again. `update` accepts that HTML string or its unmounted section element. It validates the section, SVG and prepared metadata before changing the current chart. Keep the adapter's matching SVG and metadata together; an SVG alone is insufficient. Chart data, axes, ticks and text layout remain owned by native preparation.

The [light lifecycle example](../Website/static/examples/generated/graphite-light-chart-lifecycle.html) and [dark lifecycle example](../Website/static/examples/generated/graphite-dark-chart-lifecycle.html) let you select measurements, update their values and order, dispose the chart, and mount it again. Generate both through the existing `--graphite-only` examples command.

The controller retains the original section element, host attributes such as `id` and `slot`, the host's `--cfx-host-accent` override, and host CSS properties outside the `--cfx-` namespace. The accent override also survives adaptive redraw and native-window replies, including its CSS priority. Existing host event listeners remain attached. The replacement supplies the chart's generated attributes, theme variables, controls, SVG and readout; charts without an accent override follow the replacement's theme. `cfxupdated` fires on the section after its replacement is ready.

An adaptive update selects the native drawing for the host's current width before `cfxupdated` fires. A hidden host retains its active drawing until its width can be measured. A later container resize can select another drawing and emits `cfxlayoutchange`.

Selection and legend mute/isolation follow surviving explicitly identified series. Point selection and point-legend mute/isolation also require an authored observation ID; full replacement clears state for anonymous ordinal points. Named nodes, links and regions retain their identity within their explicit series. Removed targets lose their state. Hover, pinned or delayed tooltips and focus trails clear; keyboard focus follows a surviving identified target or returns to the chart section. The geometric viewport remains in place.

The replacement's selection mode applies during reconciliation. `Single` retains the first surviving selected target. A connected native reset publishes `cfxreset` and the cleared selection after its prepared reply is accepted; a rejected or canceled reply keeps the working selection intact. Invalid adaptive replies report `cfxviewporterror` so the host can release their candidate state.

Linked highlights follow the same replacement boundary. Category and unscoped range filters retain their value-based intent. Explicit series keys, authored observation IDs, and named targets within an explicitly identified series remain usable as the source changes. Positional `sourcePoints`, anonymous ordinal targets and chart-local series keys clear on full replacement, so an unrelated observation cannot inherit a previous highlight. Adaptive switches between two prepared drawings of the same source retain every match form.

Synchronized host highlights and restored selection use the source chart's actual target ownership. Automatic series IDs and native annotation IDs stay local; shared observation references retain contributor intent within their explicit series. A named target in another series does not acquire hover, reveal or navigation state merely because its item ID matches. Unrelated peers keep their own focus, reveal and trail state.

`resize()` refreshes browser hit geometry, navigation and active readout placement after host layout changes. The adapter also observes ordinary container resizing and hidden-host reveal. `Adaptive` layout switches between two native prepared drawings as described below. Other changes to the native design size use a fresh generated section passed to `update`; scaling a fixed SVG does not regenerate its ticks or typography.

### Offline responsive drawings

For a report that needs a drawing without interactive controls, use
`new HtmlResponsiveChartRenderer().RenderFragment(chart, minimumWidth: 240, maximumWidth: 1600)`.
It prepares native SVG geometry at bounded width bands, including the authored width
and a 320-pixel phone content width. Native preparation chooses ticks, label density
and legend wrapping. Explicit X tick requests act as preferred maxima and reduce with available width;
the caller's axis object and authored settings are retained after preparation. Quarter,
middle and three-quarter native samples validate each band's geometry within half a
CSS pixel. Collision-avoiding annotation labels allow two pixels of placement variation.
Bands split where geometry clamps or label
decisions change. The ResizeObserver interpolates between compatible native drawings
at the actual container width; it never extrapolates a local derivative. It never
scales the SVG drawing, fonts or strokes. Height stays at the configured chart height,
or the optional `height` argument. Wider panels centre the drawing at the cap; panels
below the minimum scroll within their host.

This uses the native scene's stable path structure within each band. Text and scene
changes refine to the native engine's one-pixel width resolution, where the nearest
native drawing supplies the labels. Preparation is bounded to 512 retained bands;
a layout needing more transitions reports a clear error rather than generating a
drawing for every possible viewport. Resizing runs offline, with no host
callback or network request. The fallback SVG remains available without script.
Responsive SVG uses native pattern tiles so hatched bars keep the same native brush
without a different number of explicit hatch lines at every width. Static SVG and
native PNG retain their default paint path; `VisualSvgOptions.WithPatternTiles()`
also makes that SVG representation available to other hosts.
`compressLayouts: true` compresses the layout payload and needs DecompressionStream.
The host exposes `cfxResponsiveReady` and `cfxResponsive.dispose()` for asynchronous
preparation and removal. Hosts inserting inert markup load `InitializationScript`
once; `ChartForgeXResponsive.mount(scope)` and the shared insertion observer mount
those drawings without executing scripts from templates. The interactive adapter's Fit and Adaptive modes retain
their existing behavior.

Regular stacked bars share one rounded envelope per signed stack, in SVG and native
PNG. Segment joins stay square, including horizontal and negative stacks; an authored
zero corner radius keeps the whole stack square. Segmented caps appear only at the
outer end. `WithMaximumBarWidth` continues to cap the shared stack width and centre
sparse category slots.

`dispose()` releases chart listeners, observers, scheduled frames, pending readouts, exports and scenario playback immediately. It also clears the compare tray, brush rectangle, selection readouts and mute/isolation presentation while the section remains connected. It is safe to call more than once. Removing a chart also releases its work when the browser delivers the removal. Reinserted sections require a fresh `mount`; a disposed controller cannot update or resize them. Register shared assets once, and mount newly inserted sections explicitly instead of executing another copy of the runtime.

## Linking charts to other views

Listen on the chart root or a containing element. Chart host events bubble from
their root and cross open and closed shadow boundaries. These include
`cfxselect`, `cfxselectionchange`, `cfxhover`, `cfxcompare`,
`cfxbrush`, `cfxbrushclear`, `cfxlasso`, `cfxhighlight`, `cfxhighlightclear`,
`cfxtooltip`, `cfxreset` and state notifications. A host can
map their facts into another chart, a table, a tree or its own application state.
ChartForgeX does not require another view library.

Outside a shadow root, the browser retargets `event.target` to the web-component
host. Use `event.detail.chartId` to identify the source chart rather than reading
chart attributes from that target. Send commands through the mounted controller
or chart root; outward notifications do not broadcast commands to other charts.

`cfxselectionchange` reports the complete selected mark set, independently of
`CompareMarkers`. Its detail contains `chartId`, `targets` and `reason`: `click`,
`keyboard`, `lasso`, `reset`, `clear` or `command`. Series wrapper groups are not
selected marks. Escape clears the selection, the brush rectangle and its range,
and exits Brush mode. `cfxselect` remains the individual
mark notification; `cfxcompare` remains the optional compare-tray notification.
The selected set includes native points, nodes, regions, links, annotations and
value-bearing scalar series such as bullets and circles. A gauge exposes its
value as a point. A generic native element
with its own interaction identity remains selectable too. Legends, passive
guidance and series wrappers without a value are excluded. The same eligibility
applies to activation, host commands, highlighting, lasso and state restoration.
Tooltip headings and compare chips use readable authored or category captions;
the value and series remain in readout rows and the target payload's source fields.
Native activation reconciles the source chart's previous and current selected
identities in synchronized peers. Unrelated raw observations or derived summaries
stay selected. This uses the immediate gesture snapshot, without retaining another
selection store. Chart-local automatic series never cross that transport boundary;
moving from a shared target to a local target still retires the previous shared
selection. Host commands are silent and local by default. With `sync: true`, `add`
and `remove` reconcile represented identities. When an explicit synchronized
replacement or clear changes selection, peers replace or clear their selection,
including local selections. Only shared target identities are transported, so a
replacement containing only local marks sends an empty shared set. Reset clears
the group selection. Single mode retains at most one local mark.

Target identities retain their `targetKind`, `targetId`, `seriesKey`,
`observationId`, `sourcePoint`, `sourcePoints`, `value` and `kind`. Additional facts
are present when the source supports them:

| Field | Meaning |
| --- | --- |
| `category` | The authored category or x-axis label. |
| `categoryPath` | For a grouped Cartesian category, the complete parent path followed by the leaf caption. Each resolved descriptor receives its own copy. Category matching still uses `category`. |
| `x` | The numeric x value, or an ISO date with a time zone. |
| `numericValue` | The numeric value, separately from its formatted text. |
| `value` | The prepared display value, or the named state for a state-timeline run. Use `numericValue` for numeric comparisons. |
| `label` | Reader-facing text captured from the authored label, category or axis formatter. |
| `rangeLabel` | Reader-facing bounds for a bin, bucket or duration, using the prepared axis formatter. |
| `observationIds` | Authored contributor IDs of an aggregate, in source-point order. |
| `range` | The inclusive numeric or ISO date interval represented by a bin, bucket or duration mark. |

Point marks do not acquire a fabricated interval. Histogram bins, timeline runs
and Gantt bars carry their renderer-owned bounds. For a histogram's half-open
bin, the inclusive range ends at the representable number immediately below its
upper edge; the last bin includes that edge. This lets an inclusive range filter
select the same observations as the bin. State-timeline runs also have an
exclusive end: their inclusive numeric range stops immediately before it, and
their ISO range ends one native DateTime tick (100 nanoseconds) before it.
Adjacent runs therefore keep their boundary observations separate. A time-axis bar can declare its
bucket interval through `ChartSeries.WithTimeBuckets(...)`. Each bucket includes its start and excludes
the next bucket's start; its public inclusive ISO range ends one DateTime tick before that boundary.
Range matching preserves fractional seconds beyond milliseconds, including equivalent instants with different time zone offsets.
Use `label` and `rangeLabel` for chips and captions. For example, a histogram's
display label can read `40 – 60` while its exact inclusive filter endpoint is
`59.99999999999999`. BoxPlot targets report the median as `numericValue` and
`value` (formatted for display). State-timeline targets report the named state as `value`; their time
bounds remain in `range`.
On a legacy numeric time domain, the inclusive end is the representable number immediately toward the start from the boundary.
Negative OLE date buckets must stay within one UTC day and cannot use the epoch-day aliases between -1 and 0.
Preparation rejects buckets that cross these discontinuities because a single numeric range cannot represent their calendar interval.
Assign matching row
and observation IDs with `WithObservationIds(...)` when another view shares those
identities. The bulk method validates the complete candidate before replacing
the series' IDs.

```csharp
var readings = ChartSeries.CreateLine(new ChartSeriesId("requests"), "Requests", points)
    .WithObservationIds(ids.Select(id => new ChartObservationId(id)));
```

`ChartForgeXCharts.get(root)` returns an already mounted controller, or `null`.
`mount(root)` remains the explicit late-insertion entry point. Selection and
highlight commands require selection support, without requiring state bookmarks.
Host commands default to `emit: false` and `sync: false`, so applying a linked
state does not send it back to the originating view.

```js
const controller = ChartForgeXCharts.get(chartRoot);

controller.select({ observationIds: ['reading-42'] }, { mode: 'replace' });
controller.clearSelection();
controller.clearBrush();
controller.highlight({ categories: ['API'] }, { dim: true, source: 'service-list' });
controller.clearHighlight();
const selectedMarks = controller.getSelection();
const matchingMarks = controller.resolveTargets({ observationIds: ['reading-42'] });
controller.unpinTooltip();
```

`resolveTargets(match?)` reads rendered data targets without changing selection,
highlights, classes or the live announcement, and emits no events or synchronization.
It works even when `Selection` is disabled. Omitting the matcher (or passing
`undefined`) returns all data targets in rendered order; `{}`, empty matcher arrays
and unmatched filters return `[]`. Invalid matchers use the same validation as
`select` and `highlight`. The returned target snapshots are detached copies with
the same identity and fact fields as `getSelection()`. Repeated target snapshots
are collapsed by the same identity key used for selection readouts. Like other
controller methods, resolution requires a mounted, connected chart.

Resolve independent filters separately to intersect their rendered target
identities. An aggregate can match each filter through different contributors;
its `targetId` remains the aggregate's opaque identity. Keep `targetKind`,
`targetId` and `seriesKey` together when passing resolved targets to
`highlight({ targets })` or `select({ targets })`.

```js
const identity = target => JSON.stringify([target.targetKind, target.targetId, target.seriesKey]);
const first = controller.resolveTargets({ sourcePoints: [{ seriesKey: 'durations', index: 0 }] });
const second = new Set(controller.resolveTargets({
  sourcePoints: [{ seriesKey: 'durations', index: 1 }]
}).map(identity));
controller.highlight({ targets: first.filter(target => second.has(identity(target))) }, { dim: true });
```

The state-changing commands also accept ordinary `CustomEvent`s dispatched on the root:

```js
chartRoot.dispatchEvent(new CustomEvent('cfx-select', {
  detail: { match: { observationIds: ['reading-42'] }, mode: 'add' }
}));
chartRoot.dispatchEvent(new CustomEvent('cfx-clear-selection'));
chartRoot.dispatchEvent(new CustomEvent('cfx-clear-brush'));
chartRoot.dispatchEvent(new CustomEvent('cfx-highlight', {
  detail: { match: { categories: ['API'] }, dim: true, source: 'service-list' }
}));
chartRoot.dispatchEvent(new CustomEvent('cfx-clear-highlight'));
chartRoot.dispatchEvent(new CustomEvent('cfx-unpin-tooltip'));
```

`select` accepts `replace`, `add` and `remove`. A matcher can use `targets`
(`{ targetKind, targetId }`), `observationIds` with an optional `seriesKey`,
`categories`, `seriesKeys`, `sourcePoints` (`{ seriesKey, index }`) or `ranges`
(`[[from, to]]`). Aggregates match an authored contributor; interval marks match
overlapping ranges. Highlighting is separate from selection. Matching marks
expose `cfx-highlighted` and `data-cfx-highlighted="true"`; `dim: true` dims the
other marks, and the live region announces the highlighted count. Clearing
highlight restores the ordinary presentation.

`highlight(match, { emit: true })` emits `cfxhighlight` with `chartId`, `count`,
`targets` and the supplied `source`. `clearHighlight({ emit: true })` emits
`cfxhighlightclear` with `chartId`. Both remain silent by default.

`clearBrush()` removes the committed range and rectangle, cancels an active brush
gesture and exits Brush mode. It keeps selected marks, zoom, pan and highlights.
Use it when the host removes a range chip while keeping other selections.
`clearSelection()` clears both selected marks and the brush, matching Escape.
Brush commands require `Brush`, independently of `Selection` or state bookmarks.
They default to `emit: false` and `sync: false`; `emit: true` sends
`cfxbrushclear` with `chartId` only when a brush or Brush mode was present.
`sync: true` also clears the brush in synchronized peers. Escape emits the clear
notification for an existing brush and the selection notification when needed.

Set `emit: true` when a host deliberately wants a selection notification, or
`sync: true` to opt into synchronized chart events. Normal user interaction still
emits its notifications. `ChartInteractionOptions.SelectionMode` controls pointer
and keyboard activation: `Toggle` retains the current behavior, `Replace` replaces
on a plain activation while Ctrl/Meta toggles and Shift extends, and `Single`
retains one selected mark.

`cfxbrush` and `cfxlasso` keep their CSS-pixel `bounds` and add `range.x` and
`range.y` where the prepared drawing provides that projection. These are physical screen axes:
horizontal layouts report values in `range.x` and categories in `range.y`. A vertical chart with only
secondary-axis observations reports that secondary Y domain. The Y range requires one unambiguous
rendered value domain: if both primary and secondary axes contain observations or authored Y bounds,
`range.y` is omitted because the same pixels represent two value domains. Primary Y annotations and
plot regions with explicit Y bounds count toward this policy; X-only overlays and empty series do not.
Unusable, non-finite projections are omitted.
Both events also expose `categoryRange`, the inclusive interval on the authored category axis:
`range.x` in vertical layouts and `range.y` in horizontal layouts. Hosts can use this field
for a category or time filter without depending on the chart's orientation. It is omitted
when that axis has no usable projection or the brush does not include it.
Time bounds are ISO dates with a time zone. A prepared time domain containing pre-1899-12-30
legacy OLE coordinates keeps all target coordinates, intervals and brushes numeric, including its positive coordinates,
so ranges round-trip in one unit. Wholly positive time domains use ISO dates and
retain the native axis's 100-microsecond precision, including brush endpoints.
Category axes also report `categories`; partial display-label maps on a numeric
axis keep numeric interval semantics. These facts come from the prepared plot projection,
including the active viewport, rather than guessing from label text. Set
`ChartInteractionOptions.BrushAxis = ChartBrushAxis.X` to restrict the brush to the physical X axis;
the default `XY` retains a rectangular brush. Timeline, Gantt, Gantt-lane
and state-timeline drawings report their horizontal data interval; their rows
do not acquire a numeric Y range. This projection contract
does not expand the separate connected native X-window capabilities.

For readable range chips, both events also provide `label`, `rangeLabel` and
`displayRange` when the prepared axes have captions. Numeric and time brushes
expand outward to prepared ticks or the plot boundaries. The visible brush,
selected observations, `range` and `displayRange` use that committed interval,
so a caption describes the same bounds used for filtering. This makes brushing
tick-aligned; a coarser axis gives a coarser brush interval. Captions retain the
native formatter's output, including display-zone dates and deliberately empty
captions. Category brushes retain the dragged interval and report the actual
included `categories`; their caption lists those names rather than neighbouring
axis ticks. Use categories for a category filter and `categoryRange` for a
numeric or time filter. Keep the exact bounds available in accessible details:

```js
chartRoot.addEventListener('cfxbrush', ({ detail }) => {
  if (detail.categories) filterCategories(detail.categories);
  else filterRows(detail.categoryRange);
  rangeChip.textContent = detail.rangeLabel ?? '';
  rangeChip.title = detail.categories
    ? detail.categories.join(', ')
    : `Range: ${detail.categoryRange.join(' – ')}`;
  rangeChip.setAttribute('aria-label', rangeChip.title);
});
```

The [two-chart example](../ChartForgeX.Examples/LinkedChartViewsExamples.cs) uses
these public contracts to update prepared service totals from a date brush and
highlight time-series readings from a selected service. It includes both themes
and an adaptive native drawing for narrow containers.

## Responsive interactive presentation

Use `HtmlChartResponsiveLayout.Adaptive` when a narrow container needs its own
native ticks, text layout and legend. The adapter prepares a second drawing from
the same chart, keeps one mounted root and switches at a container breakpoint.
Selection, legend mute/isolation and linked highlight survive the switch.

```csharp
chart.ToInteractiveHtmlPage(options => {
    options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
    options.Adaptive.NarrowWidth = 360;
    options.Adaptive.NarrowHeight = 360;
    options.Adaptive.Breakpoint = 640;
    options.Toolbar = HtmlChartToolbarLayout.Compact;
    options.Tooltip.PinOnSelect = false;
    options.Highlight.DimOthersWhileSelected = true;
});
```

`Readable` remains the default contained viewport, and `Fit` scales one drawing.
`Toolbar = Band` retains the framed toolbar; `Compact` uses small right-aligned
buttons. Both respond to their containing chart's width. `PinOnSelect` defaults
to `true`; disabling it keeps click selection without pinning a readout.
Keyboard focus retains its readout and accent focus ring. Pointer focus does not
hold a readout after the pointer leaves or draw the browser's rectangular focus
outline. Tooltip titles and compare chips use readable categories and names;
numeric values remain in the detail rows.

## Linking topology and graph explorers

`ChartForgeXTopology.mount(root)` binds a topology inserted after the shared
runtime. `ChartForgeXGraphExplorer.mount(root)` does the same for graph explorers.
Both return the matching roots and leave an already mounted root bound once.
Their standalone assets mount existing roots automatically without requiring the
ordinary-chart runtime.

Dispose a root before replacing its host when immediate cleanup is needed:

```js
ChartForgeXTopology.dispose(topologyRoot); // Returns the roots released.
ChartForgeXGraphExplorer.dispose(graphRoot); // Returns whether it released a mounted graph.
```

Disposal releases listeners, observers, playback timers, scheduled frames and
owned graph workers and GPU buffers. It clears transient hover while retaining
committed graph positions and topology selection, including an explicitly cleared
selection, for a later mount. Removing a root or its shadow host also
releases that work when the browser delivers the removal. Mount a reinserted or
explicitly disposed root again before sending commands. Repeated disposal is
safe. Topology mount/dispose also handles custom `CssClassPrefix` assets through
the same API; registering another prefix does not replace the earlier prefix's
bindings.

Topology commands default to silent updates as well:

```js
topologyRoot.dispatchEvent(new CustomEvent('cfx-topology-set-selection', {
  detail: { kind: 'node', ids: ['api', 'worker'], emit: false }
}));
topologyRoot.dispatchEvent(new CustomEvent('cfx-topology-clear-selection'));
topologyRoot.dispatchEvent(new CustomEvent('cfx-topology-highlight', {
  detail: { nodeIds: ['api'], edgeIds: ['api-worker'], dim: true }
}));
topologyRoot.dispatchEvent(new CustomEvent('cfx-topology-clear-highlight'));
```

`cfx-topology-select.selected` reports interactive selection;
`authoredSelected` retains the authored state. Enable viewport controls and set
`TopologyRenderOptions.HtmlNarrowMinimumHeight` to retain a readable viewport on
phones. Fit can zoom below the manual zoom floor when showing the whole diagram
requires it.

```js
ChartForgeXGraphExplorer.select(graphId, [{ role: 'graph-node', id: 'api' }], {
  mode: 'replace', emit: false
});
ChartForgeXGraphExplorer.clearSelection(graphId, { emit: false });
ChartForgeXGraphExplorer.highlight(graphId, [{ role: 'graph-node', id: 'worker' }], { dim: true });
ChartForgeXGraphExplorer.clearHighlight(graphId);
```

Graph host selection follows the scene's `Selection` and `MultiSelection` features.
Without `MultiSelection`, `select` keeps the first matching graph item and a matched `add` replaces the current selection.
An unmatched addition leaves selection unchanged. Independent highlighting may still include several items.
Graph selection edge details include `sourceNodeId` and `targetNodeId`.
`cfxgraphhover` and `cfxgraphhoverclear` report `graphId`, `id`, `role`, `label` and
`metadata`, so a host can coordinate hover without inspecting the graph's paint.
Graph notifications also cross shadow boundaries. Use `event.detail.graphId`
outside the shadow root, where `event.target` refers to the component host.

Graph relationship captions check visible node bodies, captions and badges,
including collapsed aggregates. Filtering refreshes their SVG positions. When a
midpoint is obstructed, placement searches clear positions along the painted
line, curve or polyline before moving farther away. A fully crowded route can
still require a displaced caption; placement does not reroute authored edges.

Fit includes complete node captions and badges and adjusts its zoom ceiling for
the host's screen size. Small scenes can use a compact host without inheriting
the wide scene's zoom limit. Node positions and the logical scene frame stay
unchanged. Resizing refits an untouched camera; after a user zooms or pans, it
preserves that camera until Fit is requested. Typing or clearing search changes
visibility without moving the camera; Enter selects and centers the first matching
node. Large scenes can still require zooming or filtering to read individual captions; Fit does not promise a minimum
font size for an arbitrary graph. Static SVG and PNG measure their complete
chosen node captions and badges within the design frame. Their requested output
dimensions scale that frame without reflowing authored node positions.

Small-graph physics includes captions and badges in overlap avoidance through the
same geometry used for layout separation. Pausing preserves the current moving
pose; it does not complete the final arrangement. Resume until stabilization if
spacing still needs to settle. Fixed nodes and crowded scenes can still overlap.

## Topology motion with HTML controls

Use `ChartForgeX.Stories` and `ChartForgeX.Interactivity.Html` together when a topology needs script-free SVG motion and browser controls:

```csharp
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;

var motion = TopologyMotionOptions.RoutePulseForScenario("delivery");
var options = new TopologyRenderOptions {
    EnableHtmlScenarioControls = true,
    EnableHtmlViewportControls = true,
    EnableHtmlSelectionPanel = true,
    ActiveScenarioId = "delivery",
    IdScope = "service-route"
};
var html = new HtmlInteractiveTopologyRenderer().RenderPresentationPage(
    topology, prepared => prepared.WithMotion(motion).ToSvg(), options);
```

The adapter prepares a detached topology once and passes it to the SVG producer. The prepared geometry, metadata and identity scope align with the HTML controls. Scenario highlighting stays reversible because the initial active scenario is applied by the browser runtime. The callback must return trusted SVG exported from the supplied prepared topology; it must preserve that topology's geometry and entity metadata.

`RenderPresentationFragment` includes the topology CSS and interaction runtime. `RenderPresentationFragmentWithoutAssets` lets the embedding host register them once. `RenderPresentationPage` also accepts `HtmlAssetReferences` for a shared external runtime. Motion keeps its configured route while scenario controls change the visible highlight; choosing another scenario does not retarget the animation.

## Scenario Timelines

Scenarios are ordered, host-neutral timelines. Configure pacing and visual intent on the scenario model; adapters consume the same contract. Retain the `pressure` and `success` series handles from chart construction and use their IDs in the route:

```csharp
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

interaction.AddScenario("recovery", "Recovery route", scenario => scenario
    .WithDescription("Inspect pressure, then confirm the recovered signal.")
    .WithPlayback(stepDurationMilliseconds: 1100, loop: false, autoPlay: false)
    .WithFocusMode(ChartInteractionScenarioFocusMode.Highlight)
    .AddSeriesStep(pressure.Id, "Inspect pressure",
        configure: step => step.WithDuration(1600))
    .AddSeriesStep(success.Id, "Confirm recovery"));
```

`Highlight` is the default: it emphasizes route members while preserving titles, axes, and surrounding data context. Use `Spotlight` only when dimming non-route data is intentional. Playback and autoplay are opt in, autoplay is suppressed when the browser requests reduced motion, and the HTML adapter exposes previous/next controls plus an accessible range scrubber for direct step selection. Individual step durations override the scenario default; both accept 200-60000 milliseconds.

Graphite timeline panels use the chart surface and text colours. Route accents identify controls and steps, while a progress strip stays below the label. Forced-colour progress uses the system surface and text colours.

The HTML adapter uses `HtmlChartResponsiveLayout.Readable` by default. In narrow hosts, including dashboard columns in a wide window, it keeps the fixed-design SVG legible inside a contained horizontal viewport. Set `ResponsiveLayout = HtmlChartResponsiveLayout.Fit` when seeing the entire chart at once is more important than minimum label size.

## Scenario Events

Scenario controls dispatch browser events from the chart root:

- `cfxscenario`: a scenario was selected. Detail includes `scenarioId`, `label`, `description`, `color`, `steps`, and `metadata`.
- `cfxscenarioclear`: scenario state was cleared.
- `cfxscenariostep`: a scenario step was selected. Detail includes `scenarioId`, zero-based `index`, `step`, and `progress`.
- `cfxscenarioplayback`: playback state changed. Detail includes `state`, `scenarioId`, `stepIndex`, `progress`, and `delay`.
- `cfxscenariolink`: a deep link was copied. Detail includes `url`.
- `cfxstate`: an opt-in interaction snapshot was captured. Detail includes `source` and `snapshot`.
- `cfxstateapplied`: an opt-in interaction snapshot was replayed. Detail includes `snapshot`.

`cfxscenarioplayback.detail.state` is one of `idle`, `playing`, `paused`, or `finished`. The chart root also exposes the current state through `data-cfx-scenario-playback`, current step progress through `data-cfx-scenario-progress`, and the effective current-step cadence through the event's `delay` value.

## Host Commands

Host pages can drive the same controls by dispatching events on the chart root:

```js
chart.dispatchEvent(new CustomEvent('cfx-set-scenario', {
  detail: { scenarioId: 'risk-review' }
}));

chart.dispatchEvent(new CustomEvent('cfx-set-scenario-step', {
  detail: { scenarioId: 'risk-review', index: 1 }
}));

chart.dispatchEvent(new CustomEvent('cfx-play-scenario', {
  detail: { scenarioId: 'risk-review' }
}));

chart.dispatchEvent(new CustomEvent('cfx-pause-scenario'));
chart.dispatchEvent(new CustomEvent('cfx-clear-scenario-step'));
chart.dispatchEvent(new CustomEvent('cfx-clear-scenario'));
```

These commands preserve the same host events, deep-link updates, synchronized chart behavior, focus trails, and reveal labels as the built-in controls.

## Exporting interactive charts

Enable `ChartInteractionFeatures.Export` to offer SVG and PNG downloads. Both export the committed native scene, including an accepted connected X window. Downloads retain selection, legend mute/isolation and resolved host colors. Hover emphasis, focus outlines and focus trails stay in the browser; standalone geometric zoom and pan do not change the exported native canvas.

SVG downloads include the resolved presentation needed to display the chart without the adapter stylesheet or runtime. PNG downloads rasterize that SVG in the browser at the device pixel ratio. This browser PNG path is separate from ChartForgeX's native `ToPng` rendering; fonts and rasterization can differ between them.

The default theme lists Arial before the generic system font, keeping the browser on the same available fallback used by native preparation when Calibri, Carlito and Segoe UI are absent. For an exact font contract, register the chosen faces for native rendering and deliver those same faces to the browser through the host's font stylesheet. Native font registration alone does not make a font available to the browser.

## State Bookmarks

Standalone zoom and pan controls transform the complete rendered SVG, including its axes and labels. Connected hosts can use native X or primary-Y/XY windows as described below. Static legend display uses `WithLegend` and per-series `WithLegendEntry`; browser legend actions mute or isolate marks while retaining their source facts.

## Translated controls and text direction

`Localization` supplies HTML control labels, tooltips on controls, comparison text, scenario playback text, pinned-tooltip status, the default scalar `Value` caption and highlight announcements. A native dimension label takes precedence over that default caption. Unspecified messages use English. The page and chart inherit the native chart's accessibility language unless `Localization.Language` overrides it. A dashboard copies its configured bundle into every child chart.

```csharp
chart.SaveInteractiveHtml("measurements.html", options => {
    options.Localization.Language = "pl-PL";
    options.Localization[HtmlChartText.ChartControls] = "Narzędzia wykresu";
    options.Localization[HtmlChartText.ResetView] = "Przywróć widok";
    options.Localization[HtmlChartText.Compare] = "Porównaj {count}";
    options.Localization[HtmlChartText.Clear] = "Wyczyść";
    options.Localization[HtmlChartText.Value] = "Wartość";
});
```

Templates use named placeholders such as `{count}` and `{current}`. Messages are displayed as text, including literal angle brackets. Updating a mounted chart installs its replacement bundle while retaining surviving selected observations. The Graphite gallery includes a complete Polish controls example.

The default `Automatic` direction inherits the surrounding document's direction; standalone pages use the browser's normal default. A chart title or label does not choose the direction of the page shell. For right-to-left controls, set `Localization.Direction = HtmlChartTextDirection.RightToLeft` and an appropriate language tag; `LeftToRight` provides an explicit override in an RTL host. Adapter chrome follows that direction; the native SVG keeps the authored axis order. Data labels, number/date formatting and native font shaping remain properties of the chart and its render context. Translate chart headings and dimension names through those native APIs. These browser contracts do not establish physical screen-reader or bidirectional font-shaping qualification.

## Connected native X windows

Use `HtmlInteractiveChartRenderer.PrepareSection` when a host needs the asset-free section and its matching detached native scene. Both come from one preparation. Enable `Zoom` and `Pan` explicitly in the interaction options; establishing a connection does not enable those features.

```csharp
var renderer = new HtmlInteractiveChartRenderer();
void Configure(HtmlChartInteractionOptions options) {
    options.Interaction.ChartId = "measurements";
    options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan);
}
var initial = renderer.PrepareSection(chart, configure: Configure);

HtmlChartSection PrepareWindow(HtmlChartSection basis, string kind, double left, double right) {
    var viewport = kind == "reset" ? null
        : basis.PreparedVisual.XProjection!.ConstrainViewport(left, right);
    return renderer.PrepareSection(chart, viewport == null ? null : new ChartCartesianViewport(x: viewport), Configure);
}
```

`left` and `right` are logical SVG positions sent by the browser. `ConstrainViewport` uses the captured native projection, handles a reversed X axis, and shifts an edge-crossing window into the full bounds while retaining its span. It returns null when the request covers the full span. Reset also uses null, restoring ordinary preparation. Explicit `ChartXViewport` values remain strict and must lie within the full resolved bounds.

Connect the mounted controller with `connectNative`. Its callback receives `channel` (`viewport` or `annotation`), the committed native `basis`, `chartId`, an opaque `requestId`, `sceneRevision` and the operation in `kind`. The application bridge looks up that immutable native section, prepares the requested operation, retains the result and returns `{ html, basis }` with its opaque native token. The callback is the response path; request events are notifications.

```javascript
const controller = ChartForgeXCharts.mount(root);
controller.connectNative((request, signal) => renderNativeSection(request, signal), {
    basis: initialNativeToken,
    axes: 'x'
});
```

The existing native preparation owner carries only the committed basis. It advances that token with the accepted section before notifying change listeners, so a listener can immediately submit a command against the new scene. Rejected, canceled and stale responses never advance it. The application owns transport, chart data and retention of native sections; it can retire rejected candidates and old tokens according to its own retention policy. Browser-side revision and candidate maps are unnecessary.

Zoom and pan contain logical `left` and `right`; explicit windows use `sourceViewport.x`, and reset omits bounds. Constrain logical positions through the committed drawing's `XProjection`. For explicit source bounds, call `ConstrainViewport(new ChartXViewport(minimum, maximum))` directly, then wrap the returned X window in `ChartCartesianViewport`, preserving null for full view. Toolbar zoom uses the plot centre; wheel zoom keeps the source coordinate beneath the pointer as its anchor.

An external replacement on a managed connection must carry the replacement's matching native basis. A missing or empty token rejects before changing the committed section:

```javascript
controller.update(replacement.html, { basis: replacement.basis });
```

`axes` defaults to `x`; use `y` or `xy` for eligible primary-axis navigation, or null for annotation-only native preparation with ordinary browser zoom. Adaptive sections capture both `PreparedVisual` and `NarrowPreparedVisual` under one native token. A request's `drawing: "wide" | "narrow"` chooses the captured projection used to constrain its coordinates. Layout changes retain that basis and cancel pending work for the replaced drawing.

Hosts that intentionally manage channel-specific lifetimes can use the existing lower-level `connectXViewport`, `connectViewport` and `connectAnnotations` callbacks returning generated HTML strings. Selecting one of those methods detaches the managed connection completely; it cannot leave one channel using an obsolete managed basis.

Only the latest request for the current scene and connection can commit. A newer request, ordinary update, reconnect, disconnect, disposal, removal, or Escape aborts pending work. A rejected, invalid or stale reply leaves the committed SVG and export intact. `cfxviewportchange` carries the accepted `requestId`, new `sceneRevision` and `kind`. A failed current reply emits `cfxviewporterror` with its request identity and message. Pass null to `connectNative` to disconnect its native channels and cancel pending work.

Native X windows support vertical, non-sparkline Line, StepLine, Area, StepArea, StackedArea, Scatter, Bubble, Candlestick, OHLC, RangeBand, RangeArea, DifferenceArea, ErrorBar, Lollipop, Dumbbell, Slope and TrendLine charts on linear or time X axes, including smooth Line and Area interpolation. The adapter disables native zoom/pan controls while a connected section lacks that projection, including empty and unsupported charts. A window can contain no observations while a connected path still crosses it. Native preparation keeps the full source facts and authored observation identities, excludes off-window marks from pointer and keyboard access, regenerates X ticks and keeps the full-data Y bounds. A native commit clears transient brush and tooltip state and removes the previous whole-SVG transform. Export uses the committed native window.

`controller.getXWindow()` returns a detached `{ minimum, maximum, scale }` snapshot of the committed native scene, or null when no native projection is available. `controller.setXWindow(minimum, maximum)` requests finite, increasing source bounds through the same callback, with `kind: "window"`. It returns the controller immediately; `cfxviewportchange` signals the accepted scene and `cfxviewporterror` reports failure. A connection and supported projection are required. The host still calls `ConstrainViewport`, so edge-crossing source requests retain their span within the full domain. Time bounds use the chart's OLE Automation date coordinates, as in `ChartXViewport`, rather than JavaScript epoch milliseconds.

```javascript
const current = controller.getXWindow();
const inset = (current.maximum - current.minimum) / 4;
controller.setXWindow(current.minimum + inset, current.maximum - inset);
```

Bookmarks captured from a connected chart include `xWindow`. Applying one requests that source domain through the native callback; wait for `cfxviewportchange` before treating its native scene as committed. Connected synchronized peers exchange source bounds with a matching scale, so different plot sizes and reversed X directions retain the same source window. Each host constrains the request against its own full domain. Link charts whose source coordinates have the same meaning and units.

### Native Y and XY windows

`ChartCartesianViewport` combines an optional `ChartXViewport` and `ChartYViewport`. At least one is required. Pass it to `chart.Prepare(context, viewport)` or `renderer.PrepareSection(chart, viewport, Configure)`. An explicit primary Y window uses the existing Linear, Time, positive base-10 Logarithmic or SymmetricLogarithmic transform. It keeps full-data calculations; it does not autoscale Y from the visible X observations. Y and XY windows require the supported vertical families above and no secondary-axis series. X-only windows retain their existing secondary-axis support.

```csharp
var viewport = new ChartCartesianViewport(
    x: new ChartXViewport(2, 8),
    y: new ChartYViewport(20, 80));
var section = renderer.PrepareSection(chart, viewport, Configure);
```

Connect with `controller.connectNative(callback, { basis: initialNativeToken, axes: 'xy' })`; `axes` selects the gesture axes. The callback returns the managed `{ html, basis }` reply described above. Logical zoom/pan requests include `axes` and the requested `left/right` and/or `top/bottom` coordinates. Constrain them through the committed drawing's `XProjection` and `YProjection`. Each projection handles its own reversal and nonlinear transform. Reset requests omit logical bounds. Preserve an unrequested axis's committed window when preparing the reply.

`controller.getViewport()` returns detached `x` and `y` windows, with null for an unavailable projection. `setViewport({ x: { minimum, maximum }, y: { minimum, maximum } })` requests explicit source bounds through `sourceViewport`; either axis may be omitted. `getYWindow()` and `setYWindow(minimum, maximum)` are Y conveniences. The existing X methods also work with this connection. Programmatic windows are explicit independently of the configured gesture axes. Bookmarks and linked peers carry both windows when available. A replacement that loses its eligible Y projection disables Y/XY gesture controls until a supported scene returns.

Adaptive drawings resolve one native automatic-domain envelope before preparing their windows. A marker padded against a narrow drawing can therefore keep the same source window in its wide counterpart. Authored axis ends remain exact. This requires additional native preparation; a host should retain the paired section rather than preparing an unrelated wide basis for a narrow gesture.

### Navigator, presets and trading sessions

Set `options.XNavigation.Enabled = true` to include a native overview, two range handles and a position scrollbar below the plot. They use the connected source-domain callback and remain inactive without a supported connection. `XNavigation.RangePresets` accepts `new HtmlChartXRangePreset("Latest four", 4)` in source X units or `HtmlChartXRangePreset.FromDuration("Last day", TimeSpan.FromDays(1))` for elapsed time. A duration is fixed elapsed time, not a calendar month or an exchange schedule. The overview retains the full native scene; exports use the committed main drawing.

Range handles and the position scrollbar use a consistent keyboard step: arrows move 1% of the control's range, Page Up/Down move 10%, and Home/End select its minimum/maximum. Horizontal arrows follow the displayed axis direction; Up/Down always increase/decrease. A changed range commits through the connected native preparation callback and retains focus. Pointer movement remains continuous. For the position scrollbar, the percentage applies to the available travel while preserving the selected span.

For an explicit trading schedule, assign `XAxis.TimeSessions` or call `axis.WithTimeSessions(new ChartTimeSessionDomain(sessions))`. Each immutable `ChartTimeSession` contains increasing DateTime/DateTimeOffset instants or nonnegative OLE Automation coordinates. Local DateTime inputs convert to UTC; unspecified inputs are treated as UTC. The domain copies intervals in authored order and rejects overlaps or reversed order. Included time remains proportional; declared closures take no horizontal space. No market calendar is inferred. Source observation dates, prices and authored window bounds remain unchanged.

Observations and source drawing endpoints must belong to an included interval. A window may start or end inside a closure if its projected span is positive; that raw bound remains in the source request and native result. A window wholly inside a closure is invalid. At a collapsed join, inverse projection selects the next session's opening; the final endpoint remains the final close. Ticks and pointer inversion use the native session mapping. Hosts receiving `sourceWindow` or `sourceViewport` should constrain those source bounds directly instead of projecting them to pixels and back.

### Native source drawing commands

Notes, finite line segments and channels use the [native source annotation model](v2/api-conventions.md). A host can use `controller.connectNative(callback, { basis: initialNativeToken, axes: null })` for annotation-only preparation, or share the existing managed viewport connection, then submit `controller.applyAnnotationChange({ operation, id, drawing })`. Operations are `add`, `replace` and `remove`. Add requires an unused authored ID; replace/remove require an existing one. Remove omits `drawing`.

```javascript
controller.applyAnnotationChange({
    operation: 'add', id: 'review-line',
    drawing: {
        kind: 'LineSegment', start: { x: 2, y: 20 }, end: { x: 8, y: 40 },
        label: 'Review', yAxis: 'Primary'
    }
});
```

`Note` uses only `start`; `ParallelChannel` also requires a finite nonzero `yOffset` in source Y units. The callback receives immutable `sourceDrawing` facts, `annotationId`, the operation in `kind`, and the shared request/scene identity. Adaptive `drawing` still identifies `wide` or `narrow`. The host applies the command through `AddAnnotation`, `ReplaceAnnotation` or `RemoveAnnotation`, then returns fresh generated section HTML and its matching basis token. Native preparation validates scales and axis ownership. The adapter verifies the reply's authored ID and requested geometry before committing; it never renders a substitute browser drawing.

`cfxannotationrequest`, `cfxannotationchange` and `cfxannotationerror` report this channel. The managed connection associates the accepted native basis with the committed scene, as for viewport requests. Annotation and viewport work share one pending request: a newer command, update, disconnect, adaptive replacement or disposal aborts prior work. Accepted edits preserve the reader's ordinary SVG zoom and surviving identified selection/focus. Export uses the accepted native drawing. Pointer tools, selection handles, forms and undo history belong to the consuming editor; this command API supplies their native commit boundary.

### Optional chart action menu

Set `options.IncludeContextMenu = true` to expose the existing reset/export actions on right-click or the Context Menu/Shift+F10 keys inside the stage. It reuses their feature flags, translated labels, disabled state and command handlers. Arrow keys, Home/End, Escape and Tab retain normal menu navigation and focus recovery. The menu closes on replacement, disposal, outside input, scrolling or resize. When no action is available, the browser's native menu remains available. Static output has no menu or script.

Enable `ChartInteractionFeatures.StateBookmarks` when a host wants to save and replay user exploration state. The adapter captures only reusable chart state:

- viewport: `zoom`, `panX`, and `panY`
- mode and brush bounds
- selected target identities
- compare count
- active scenario id, step, progress, and playback state

Host pages can capture the current state:

```js
chart.addEventListener('cfxstate', event => {
  localStorage.setItem('review-state', JSON.stringify(event.detail.snapshot));
});

chart.dispatchEvent(new CustomEvent('cfx-capture-state', {
  detail: { source: 'review-bookmark' }
}));
```

They can reapply the same state later:

```js
const snapshot = JSON.parse(localStorage.getItem('review-state'));
chart.dispatchEvent(new CustomEvent('cfx-apply-state', {
  detail: { snapshot }
}));
```

For a connected native window, replay copies the snapshot and waits for its matching prepared reply before restoring selection, mode and scenario playback or emitting `cfxstateapplied`. Rejected, canceled or superseded replies leave the committed state intact. A native replay keeps the geometric camera at its base scale; source bounds determine the data view.

When synchronized charts are enabled, state payloads use the same `cfxsync` channel with action `state`; each chart applies only the target identities and scenario ids it understands.
