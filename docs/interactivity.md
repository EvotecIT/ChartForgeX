# Interactivity Reference

ChartForgeX keeps static SVG, PNG, and HTML output deterministic by default. Browser behavior lives in `ChartForgeX.Interactivity.Html` and is opt-in through reusable feature flags on `ChartInteractionOptions`.

For static SVG on ordinary pages and reports, the much smaller [enhance runtime](enhance.md) adds the shared states, tooltips, keyboard model, legend toggles and linked views to every chart family without re-rendering, and takes family behaviours through `CfxEnhance.behaviour`. This page describes the full runtime for dashboards.

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

## HTML font delivery

`HtmlChartInteractionOptions.TextPresentation` defaults to `Automatic`. The adapter takes eligible single-face fonts from the prepared native drawing and registers their copied bytes with `FontFace`. It loads no font URL and sends no caller font path. A selected TTC member, mixed fallback run, synthetic style, nondefault variation/palette, explicit OpenType language, or unavailable browser font stays on captured native ink. Supported monochrome runs use the existing glyph contours. Ordinary color or embedded-bitmap text that cannot use that outline transport stays as a bounded native raster image. Its prepared faces, shaping, baseline and bitmap/color paint are retained; it is not vector zoom equivalence. A multiline text node is captured together so overlapping lines and strokes keep their native composition. These non-outline images do not switch to a browser face merely because a font loads.

```csharp
chart.SaveInteractiveHtml("report.html", options => {
    options.TextPresentation = HtmlChartTextPresentation.Automatic;
});
```

Choose `NativeContours` for supported monochrome captured glyph placement throughout the live SVG, or `Browser` for host-selected editable browser text and the existing native PNG-export capture. Explicit `NativeContours` and structured native captions retain their clear rejection when the outline transport cannot represent a color/bitmap glyph. Browser rasterization and shaping can differ from native output even when the delivered face is the same. `Automatic` and `NativeContours` retain native glyph placement in detached SVG/PNG downloads; browser-only CSS text decoration belongs to `Browser`. Core static SVG/PNG exports keep their existing defaults.

Delivery snapshots admit up to 32 distinct roots, 2 MiB per root and 4 MiB of distinct decoded roots across the wide/narrow pair. An additional view that cannot be delivered retains native contours. These are transport limits; they do not bound all browser memory. `NativeContours` rejects glyphs without that outline transport; `Automatic` uses the captured native image fallback described above. Caller fonts must be licensed for distribution in the generated document.

The existing topology and report adapters use `Automatic`. Explicit `RenderPresentation*` callbacks retain the caller's SVG. Configurable prepared chart and hierarchy sections expose the three choices through `HtmlChartInteractionOptions`. The Website playground uses the same binary registration asset with its existing same-origin shipped-face map. Live registration uses no data/blob font source and requires no CSP relaxation.

Migration: automatic HTML now carries bounded font bindings and native contour fallback resources. Select `Browser` when retaining the previous live browser-text representation is part of the host's contract. Native/static defaults, source IDs, layout, measurements, and portable font admission remain independent.

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
Native preparation emits one portable `chartforgex/layout-2` specification with
source series values, candidate axis captions, semantic paint, plot bounds and
category-slot constraints. The browser measures captions in their actual token
font, allocates bar slots and legend rows, and projects geometry into the panel's
CSS-pixel plot bounds. A debounced ResizeObserver repeats that layout at arbitrary
widths, including fractional widths. No C# callback or network request is needed.
The payload contains no sampled width bands or coordinate derivatives. Layout 2 uses
one inline SVG as both the static drawing and the browser's source paint; the JSON
no longer contains an `svg` string. The native base uses the maximum width so it
retains the source geometry needed at wider panel sizes. The runtime still accepts layout 1 fragments.

Text and strokes keep their token sizes. Height stays at the configured chart
height, or the optional `height` argument. Tick captions thin at a measured regular
stride from the first caption with at least 1.5 em between neighbours; rectangular heatmaps also keep the last caption. Legends wrap before consuming plot space. An oversized caption truncates with its
full name in an SVG tooltip; legend rows use at most 35% of the fixed height,
with a named overflow summary when more entries remain. Regular
bars use the native 68% category pitch and bounded group gaps, with the configured
maximum bar width. Wider panels centre the drawing at the cap; panels below the
minimum scroll within their host. Hidden fragments wait until visible before
measuring text. The caller's chart size and axis policies are restored after
serialization. Canonical bar slots and mapped source anchors are serialized, so
sparse categories retain the same series slots. Every mounted fragment gets
independent SVG definition IDs, even when two charts have identical content.

Rectangular heatmaps use layout 3. At narrow widths the plot scrolls horizontally, keeping a minimum cell width and a sticky row-label gutter; the full row name remains in a tooltip. The first and last ordered column captions remain visible at the ends of the scroll range. Static fixed-width heatmaps use their configured column-window policy; the responsive host retains all columns.

Responsive heatmaps initially show the latest columns. Set `WithHeatmapColumnWindow(ChartHeatmapColumnWindow.Squeeze)` to open at the left edge instead. A reader at the newest edge stays there through resize; scrolling back into the history preserves the same first visible column when the card changes width. Print sizing preserves the screen position for the return to the page.

The specification declares a plot band for Cartesian, matrix and state-timeline charts.
Content before the band stays in place, the band stretches, and content after it
(a lane summary column, for example) moves with the right edge. State timelines also
declare their lane-label gutter: below the drawing's natural width
it narrows toward a minimum so the lanes keep about 60% of the remaining width, and
lane labels, lane summaries, group headers and the chart heading truncate with an
ellipsis and a full-text tooltip instead of shrinking. Heatmap colour legends and column
captions move with the plot but keep their own width. Rotated matrix captions are
measured in the current viewport and thin at a regular stride, including when
columns are reversed. Charts without a declared plot band, such
as radial charts and, for now, Gantt lanes and mixed schedules, stay centred at their native size; their heading keeps its left inset
and truncates instead of being cropped.

If the layout data is missing, is not valid JSON or cannot be decompressed, the host
keeps its static drawing and logs one console warning. A specification element added
to the host later mounts the chart.

Browser printing lays out at the actual print panel width, including below the
screen scrolling minimum, and restores the screen minimum after printing. `printWidth` chooses the maximum CSS width of the script-free fallback; a report
host can supply its page's printable width. The fallback scales the maximum-width base drawing through its viewBox, so its text
and strokes scale when the available print width is narrower than that base. It remains
available without script. Static SVG and PNG rendering continue to use the native
renderer and do not use this responsive layout contract.

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

Mixed Timeline/Gantt/StateTimeline/GanttLane sections retain that same horizontal-only projection. X-axis reversal changes the captured time direction and Y-axis reversal changes the lane order; it does not give rows numeric Y coordinates. A fresh native `update` retains surviving explicit source identities and coalesced state contributors while replacing marks, ticks and export content together. Hover and keyboard targets still identify the original typed observation, including packed items and full-height markers. Browser selection or source-domain brushing does not create a scheduling editor or silently expand native viewport support.

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

SVG downloads keep editable, searchable text and the resolved presentation needed to display the chart without the adapter stylesheet or runtime. A host still supplies the fonts used to display that text. PNG downloads build a separate detached SVG composition at the device pixel ratio. For captured monochrome outline faces, that composition uses the native preparation's shaped glyphs and face instances, including registered fallback and variation choices. It does not look up a font by family again or fetch a font file. Repeated glyph geometry at the same captured size and italic policy is shared within that composition; ordinary Core `ToSvg` and `ToPng` output is unchanged.

The live page remains text, so register the chosen faces for native rendering and deliver the same faces to the page through the host's font stylesheet. CSS fill, stroke, opacity and persistent selection or legend paint are captured for export. Native logical-unit glyph paths keep stroke width in logical units while the browser applies only captured placement. Changing the font family, size or shaping policy requires a fresh native preparation; a CSS override cannot lay out new native glyphs. Accepted ordinary updates, native replies and adaptive drawing changes replace their captured glyph resources with the scene. Disposal or replacement cancels pending exports through the existing chart lifetime.

Host CSS text decorations, including decorations propagated by SVG ancestors, retain the original browser `<text>` export path so the browser paints underline, overline and line-through styles. These decorated captions retain the existing browser font fallback and are outside the captured-outline font guarantee. Native-authored decorations remain separate native scene marks.

In `Browser` mode, colour glyphs, embedded bitmap strikes, built-in tiny fallback and synthesized bold retain the existing browser-text export fallback. `Automatic` uses the captured native representation described above. A custom topology presentation that supplies independent SVG must retain the native capture; a callback that exports its supplied `PreparedTopology` uses the capture automatically. These cases and browser antialiasing or hinting differences do not establish universal native/browser pixel equality. Browser PNG is still separate from native `ToPng`; no font bytes, stylesheet scanning, external font request or CSP change is introduced by the native outline transport. The PNG image input uses an encoded `data:image/svg+xml` resource through the existing Image/canvas path; SVG and PNG download links retain their existing Blob transport.

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

`left` and `right` are logical SVG positions sent by the browser. `ConstrainViewport` uses the captured native projection, handles a reversed X axis, and shifts an edge-crossing window into the full bounds while retaining its span. It returns null when the request covers the full span. Reset also uses null, restoring ordinary preparation. Explicit `ChartAxisWindow` values remain strict and must lie within the full resolved bounds.

Connect the mounted controller with `connectNative`. Its callback receives `channel` (`viewport` or `annotation`), the committed native `basis`, `chartId`, an opaque `requestId`, `sceneRevision` and the operation in `kind`. The application bridge looks up that immutable native section, prepares the requested operation, retains the result and returns `{ html, basis }` with its opaque native token. The callback is the response path; request events are notifications.

```javascript
const controller = ChartForgeXCharts.mount(root);
controller.connectNative((request, signal) => renderNativeSection(request, signal), {
    basis: initialNativeToken,
    axes: ['x']
});
```

The existing native preparation owner carries only the committed basis. It advances that token with the accepted section before notifying change listeners, so a listener can immediately submit a command against the new scene. Rejected, canceled and stale responses never advance it. The application owns transport, chart data and retention of native sections; it can retire rejected candidates and old tokens according to its own retention policy. Browser-side revision and candidate maps are unnecessary.

For a producer that can outpace native preparation, opt in on the same connection:

```javascript
controller.connectNative(renderNativeSection, {
    basis: initialNativeToken,
    axes: ['x', 'y'],
    preparationBackpressure: 'latest-window'
});
```

Omitting `preparationBackpressure`, or using `'immediate'`, retains immediate supersession. `'latest-window'` admits one unresolved callback for that configured connection and retains one latest absolute viewport window or resolved native reset intent. Same-turn windows coalesce before admission. A queued reset retains the channel selected by the existing controller, including hierarchy reset; a later absolute window or reset can replace it. Request IDs and request events are created only when an intent is admitted, using the still-current committed basis and scene revision. The committed basis changes only after an accepted reply. The callback still returns `{ html, basis }` or its promise, and controller methods still return the controller.

Superseding or canceling a request aborts it logically. A callback that ignores its signal must physically settle before the next intent can invoke the host. If it never settles, this connection stalls rather than starting another native preparation. Invalid, rejected, canceled and stale replies never advance the committed basis. Ordinary `update`, Escape, disconnect, binding replacement, removal and disposal discard queued work; an ordinary update keeps the same connection's physical-flight bound. Reconnecting creates a new configured connection. Earlier uncooperative or legacy callbacks may still run outside that new connection's bound; the adapter does not own their execution, retention or total memory.

While this policy has active or queued work, relative zoom/pan and other non-reset hierarchy, annotation or progress operations are not retained. Public incremental controller calls throw `TypeError` before changing pending work, basis or request/change/error events; internal relative controls decline busy input without a page error. Callers wait for their current host callback to settle before retrying an incremental operation. Native inputs and new downloads remain disabled while preparation is pending, including the queued interval. Reset remains available and uses the same single slot. The policy does not combine with nonzero `progressCadenceMs`. A hierarchy-only connection can use `axes: null, hierarchy: true` because it has an existing absolute reset owner; annotation-only `axes: null` rejects this policy.

The generated `--native-preparation-backpressure-only` examples show numeric and typed sections at compact/wide sizes in both themes. Their finite catalog is prepared by .NET before display and demonstrates callback admission and release; it is not a live data collector or a performance measurement. Hosts retain responsibility for ingestion cadence, data queues, native section tokens and retirement of old candidates.


Zoom and pan contain logical `left` and `right`; explicit windows use `sourceViewport.x`, and reset omits bounds. Constrain logical positions through the committed drawing's `XProjection`. For explicit source bounds, call `ConstrainViewport(new ChartAxisWindow(minimum, maximum, maximumInclusive))` directly, then wrap the returned X window in `ChartCartesianViewport`, preserving null for full view. Toolbar zoom uses the plot centre; wheel zoom keeps the source coordinate beneath the pointer as its anchor.

An external replacement on a managed connection must carry the replacement's matching native basis. A missing or empty token rejects before changing the committed section:

```javascript
controller.update(replacement.html, { basis: replacement.basis });
```

`axes` defaults to `['x']`; select a distinct array of `x`, `y`, `secondaryX` and `secondaryY`, or null for annotation-only native preparation with ordinary browser zoom. Adaptive sections capture both `PreparedVisual` and `NarrowPreparedVisual` under one native token. A request's `drawing: "wide" | "narrow"` chooses the captured projection used to constrain its coordinates. Layout changes retain that basis and cancel pending work for the replaced drawing.

Hosts that intentionally manage channel-specific lifetimes can use the existing lower-level `connectXViewport`, `connectViewport` and `connectAnnotations` callbacks returning generated HTML strings. Selecting one of those methods detaches the managed connection completely; it cannot leave one channel using an obsolete managed basis.

Only the latest request for the current scene and connection can commit. A newer request, ordinary update, reconnect, disconnect, disposal, removal, or Escape aborts pending work. A rejected, invalid or stale reply leaves the committed SVG and export intact. `cfxviewportchange` carries the accepted `requestId`, new `sceneRevision` and `kind`. A failed current reply emits `cfxviewporterror` with its request identity and message. Pass null to `connectNative` to disconnect its native channels and cancel pending work.

Native Cartesian windows support non-sparkline Cartesian series, including vertical and horizontal bar, range, box, line and step mixtures. All captured axis transforms participate; a secondary window requires a series using its physical value-axis slot. Smooth interpolation and authored path gaps are retained. The adapter disables native zoom/pan controls while a connected section lacks that projection, including empty and unsupported charts. A window can contain no observations while a connected path still crosses it. Native preparation keeps the full source facts and authored observation identities, excludes off-window marks from pointer and keyboard access, regenerates X ticks and keeps the full-data Y bounds. A native commit clears transient brush and tooltip state and removes the previous whole-SVG transform. Export uses the committed native window.

`controller.getXWindow()` returns a detached `{ minimum, maximum, scale, maximumInclusive? }` snapshot of the committed native scene, or null when no native projection is available. `controller.setXWindow(minimum, maximum)` requests finite, increasing source bounds through the same callback, with `kind: "window"`. It returns the controller immediately; `cfxviewportchange` signals the accepted scene and `cfxviewporterror` reports failure. A connection and supported projection are required. The host still calls `ConstrainViewport`, so edge-crossing source requests retain their span within the full domain. Time bounds use the chart's OLE Automation date coordinates, as in `ChartAxisWindow`, rather than JavaScript epoch milliseconds. Omitted `maximumInclusive` means true; false is retained by `getViewport()`, `getXWindow()`/`getYWindow()`, native `sourceViewport`/`sourceWindow`, synchronization and V2 bookmarks. `setViewport({ x: { minimum, maximum, maximumInclusive: false } })` selects an explicit exclusive endpoint. The two-number `setXWindow`/`setYWindow` conveniences remain inclusive. Reject non-boolean values at the host boundary rather than coercing them. When decoding JSON, pass the property to `ChartAxisWindow`; do not rebuild a gesture window from only its two bounds.

```javascript
const current = controller.getXWindow();
const inset = (current.maximum - current.minimum) / 4;
controller.setXWindow(current.minimum + inset, current.maximum - inset);
```

Bookmarks captured from a connected chart use version 2 with one `nativeViewport` containing physical axis windows. Applying one requests that source domain through the native callback; wait for `cfxviewportchange` before treating its native scene as committed. Connected synchronized peers exchange source bounds with a matching scale, so different plot sizes and reversed X directions retain the same source window. Each host constrains the request against its own full domain. Link charts whose source coordinates have the same meaning and units.

### Native Y and XY windows

`ChartCartesianViewport` accepts physical `x`, `y`, `secondaryX` and `secondaryY` windows, all using immutable `ChartAxisWindow`. At least one is required. Primary and secondary value axes are independent: use SecondaryY on a vertical chart and SecondaryX on a horizontal chart. Pass null to preparation for complete reset. An omitted axis keeps its full-data domain by default.

```csharp
var viewport = new ChartCartesianViewport(
    x: new ChartAxisWindow(2, 8),
    secondaryY: new ChartAxisWindow(200, 800),
    valueDomain: ChartValueDomain.VisibleCategories);
var section = renderer.PrepareSection(chart, viewport, Configure);
```

Connect with `controller.connectNative(callback, { basis: initialNativeToken, axes: ['x', 'secondaryY'] })`. Gesture axes must be a nonempty array of distinct exact physical names. Logical zoom/pan requests include that frozen array and the applicable `left/right` and/or `top/bottom` logical positions. Select the matching detached `XProjection`, `YProjection`, `SecondaryXProjection` or `SecondaryYProjection`; every slot uses `ChartAxisProjection`. Do not map a secondary value request through a primary inverse. Reset omits bounds and restores all full domains. Preserve each unrequested axis's committed request when preparing an ordinary reply.

`getViewport()` returns detached `x`, `y`, `secondaryX` and `secondaryY` windows, null for unavailable projections, and the optional `valueDomain`. `setViewport({ x: { minimum, maximum }, secondaryY: { minimum, maximum }, valueDomain: 'visibleCategories' })` requests source windows; either physical slot can be omitted. `getXWindow`/`setXWindow` and `getYWindow`/`setYWindow` remain primary physical-axis conveniences. Programmatic windows are explicit independently of gesture selection. Unsupported secondary slots reject without changing the committed scene.

`FullData`/`'fullData'` is the default value-domain policy. `VisibleCategories`/`'visibleCategories'` fits each unrequested value slot from geometry intersecting the current category window. Explicit value windows win only on their own axis; authored axis bounds remain constraints, and an empty visible window retains the full domain. Full-data stacks, colors, source identities and gaps remain unchanged. The [native contract](v2/api-conventions.md#cartesian-viewports) defines continuous crossings and discrete interval membership.

Prepared metadata exposes `xProjection`, `yProjection`, `secondaryXProjection` and `secondaryYProjection` independently. Each captures full/window source and transformed bounds, scale, reversal and plot bounds, plus nondefault logarithm and time-session policy. A general brush omits an ambiguous value coordinate when two slots are populated; explicit physical projections remain available. Logarithmic axes can select a positive or negative domain and a configured base; the browser uses the accepted native transform rather than guessing it.

Adaptive drawings enclose every captured physical axis in one full-domain envelope before preparing their windows. A narrow drawing's fitted edge remains portable to its wide counterpart. Authored axis ends remain exact. Bookmarks carry the complete available physical window state in `nativeViewport`; linked peers apply compatible selected physical slots. Hosts retain the paired native section and accepted basis through the existing lifetime.

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

`Note` uses only `start`; `ParallelChannel` also requires a finite nonzero `yOffset` in source Y units. The callback receives immutable `sourceDrawing` facts, `annotationId`, the operation in `kind`, and the shared request/scene identity. Adaptive `drawing` still identifies `wide` or `narrow`. The host applies the command through `AddAnnotation`, `ReplaceAnnotation` or `RemoveAnnotation`, then returns fresh generated section HTML and its matching basis token. Native preparation validates scales and axis ownership. The adapter verifies the reply's authored ID and requested geometry in every adaptive drawing before committing; it never renders a substitute browser drawing.

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


Native time windows retain source instants when the axis uses `ChartTimeSpacing.Ordinal` or explicit calendar units. Captured `timeSpacing` metadata accompanies each physical projection; the browser uses that detached map for readouts, source-window validation and inverse gestures. Ordinal values interpolate between the actual supplied instants. Unit-time values share their calendar band's center; inverse gestures select canonical calendar boundaries. No missing observation is synthesized and no data aggregation occurs. See [explicit time spacing](v2/api-conventions.md#explicit-time-spacing) for limits and discrete inverse semantics.


## Native hierarchy views

Treemap and Sunburst can prepare an existing source item as a detached visible root. The [native hierarchy view reference](hierarchy-views.md) covers `ChartHierarchyView`, matching Core/HTML preparation, opt-in group/Up/Home navigation, accepted basis tokens, bookmarks, source identity and committed exports.


## Native Polar gestures

A continuous Polar section has no automatic viewport gestures until its existing native connection opts in:

```javascript
const chart = ChartForgeXCharts.get(section);
chart.connectNative(async (request, signal) => {
  const response = await fetch('/native-view', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request), signal
  });
  if (!response.ok) throw new Error('The native view was not prepared.');
  return response.json(); // { html: freshPreparedSection, basis: opaqueNewBasis }
}, { basis: initialBasis, coordinateSystem: 'polar',
     axes: ['angle', 'radius', 'secondaryRadius'], gestures: true });
```

`gestures` accepts a boolean only on a Polar connection and defaults to false. The configured `axes` select gesture coordinates; ordinary programmatic source windows remain explicit. The existing Zoom and Pan feature flags still control the toolbar and handlers. A wheel over the visible sector and every selected radius annulus requests zoom around that logical pointer. The center has no defined bearing. Outside hits, zero vertical wheel deltas and modifier wheel gestures retain page scrolling or browser shortcuts. Keyboard activation of the existing +/- toolbar buttons uses the midpoint of the visible angular sector and selected annular overlap. No new arrow shortcut overrides source-point keyboard navigation.

In Pan mode, one primary pointer drag records continuous clockwise bearing deltas across the screen-angle seam and the outward change in logical radius. The accepted drawing does not move while dragging. Pointer release sends one native `pan` request. Pointer cancellation, lost capture, Escape or disposal discards the drag; Escape also cancels a pending native preparation. A received exact-center sample invalidates the angular drag because its bearing is undefined. Discrete input cannot reconstruct a path that was not sampled. Pan mode uses the existing `touch-action: none`; other modes keep page touch scrolling. This route does not add pinch recognition or inertia.

A `zoom` payload contains the selected axes and positive `factor`. Wheel zoom also supplies logical `anchor: { x, y }`; toolbar zoom omits `anchor` so the host calls `ChartPolarProjection.Zoom(factor, axes)` to resolve the default from its captured projection. A `pan` payload contains `clockwiseAngleDelta` in screen radians and `outwardRadiusDelta` in logical drawing units. Both carry the same basis, scene revision, request ID, adaptive drawing and cancellation signal as other native operations. Hosts use the requested drawing from the retained basis, since compact and wide logical coordinates differ. The [complete host recipe](../ChartForgeX.Examples/PolarNavigationRecipe.cs) decodes those payloads and calls the captured `ChartPolarProjection.Zoom` or `.Pan`; all source-scale conversion and full-domain confinement remain native. The host retains its immutable source and opaque-basis snapshots, checks cancellation, then returns the fresh section and new basis through its own transport. An unsupported request is an error, without a browser-only fallback.

Angular pan moves the directional crop rather than rotating source curves. Radial zoom uses the pointer to select crop bounds, and radial pan shifts the captured transformed-domain span. Uniform annular magnification does not hold an arbitrary source point fixed at its original screen position; this route does not imply a nonlinear grab-point warp. All original observations and stable IDs stay captured. Every installable responsive drawing must retain raw source identities, full-domain scale policy and unselected windows and pass existing radius, angular and native lifetime validation before the accepted basis or scene changes. Rejected, stale and cancelled replies leave native exports, accepted source domains and selection intact. Reset uses the same owner to restore the complete view.

The new native and actual-host browser fixtures cover compact/wide and both themes, source retention, linear/logarithmic and reversed axes, seam direction, annular crop geometry, scroll ownership, native export paths, cancellation and invalid alternate drawings. Their source is authored; execution and physical acceptance of this increment remain pending final qualification. The managed 82 KiB payload gate remains unchanged.

## Independent stage captions

Funnel and Pyramid component labels and outlines are prepared natively. The HTML adapter consumes those same source groups, geometry and paint; it adds no stage renderer or alternate label layout. Stable observation IDs retain stage selection across accepted value changes. Hiding a Funnel connection changes only its paint, so its source facts remain available. SVG/PNG downloads use the accepted native drawing. Standalone responsive Fit changes the displayed scale; it does not recompute a different category/value layout in the browser.

### Multi-ring slice captions and explosion

Pie/Donut external captions may originate in any ring. One native callout pass keeps each caption attached to its own series/observation. Inner leaders overlay enclosing rings with a background halo; measured captions and their actual bent/curved leaders cannot cover another accepted caption. A fixed frame can shorten or omit text with a diagnostic while retaining the full text and source association.

Positive `WithPointSliceOffset` values translate the existing native sectors without changing source values or band ratios. All rings share one fitted frame. Shifted sectors may overlap another ring, using the existing source-series paint order; this option does not promise that the original annular gaps remain empty. Keyboard selection, readouts, responsive presentation and downloads continue to describe the source ring and observation. Caption/halo decorations are not independent selectable observations. Browser PNG output remains browser rasterization, distinct from native PNG pixels.

## Aligned DifferenceArea sources

Aligned difference observations retain matched source series/index/observation references for both roles in native point facts. The HTML adapter consumes the same accepted native source groups and crossing geometry. A host re-aligns changed original sources, replaces the alignment and explicitly reassigns stable destination observation IDs before supplying a new native fragment. Selection then follows the normal accepted-source identity contract. The adapter does not align sources in JavaScript or infer pair IDs from labels. Current SVG/PNG downloads use the accepted prepared geometry; responsive Fit does not change the exact-X join.


### Native organization hierarchy sections

`HtmlInteractiveTopologyRenderer.PrepareHierarchySection` uses a genuine `TopologyChart` and the existing chart interaction shell.
It prepares native SVG/PNG and adaptive drawings with real topology node IDs. It does not translate the organization into numeric series or a GraphScene.
Existing `RenderPage` and `RenderFragment` topology producers keep their current behavior.

```csharp
var section = new HtmlInteractiveTopologyRenderer().PrepareHierarchySection(
    organization,
    new TopologyHierarchyView(collapsedNodeIds: new[] { "engineering" }),
    options => {
        options.Interaction.ChartId = "organization";
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Adaptive.NarrowWidth = 360;
    });
// Embed section.Html and register the existing chart stylesheet/runtime once.
// Retain section.PreparedVisual and section.NarrowPreparedVisual with the source snapshot and opaque host basis.
```

Connect the existing native transport with `controller.connectNative(callback, { basis, hierarchy: true })`.
For an organization section, omitted axes remain unavailable. The callback receives the same request ID, scene revision, cancellation signal and basis rules
as other native preparations. A hierarchy request carries `collapsedNodeIds`, `nodeOrder` and nullable `layoutDirection`; the native owner validates exact
source IDs and prepares the requested layout. Return `{ html: nextSection.Html, basis: nextBasis }` only for the corresponding retained source.
The adapter validates every installable adaptive drawing before committing the reply. A late, cancelled, wrong-source or wrong-view reply cannot advance the basis.

`controller.setHierarchyView({ collapsedNodeIds, nodeOrder, layoutDirection })` sends a complete view request; omitted collections are empty and an omitted direction
retains the authored direction. `null` restores the full source view. Branch buttons and keyboard/pointer hierarchy activation use this same path.
`getHierarchyView()` returns a copied topology snapshot. State bookmarks restore its collapse, order and direction through native acceptance before selection state applies.
The native compiler owns positions, routing, crop and exports. Hidden node IDs do not become selections on other visible nodes.
Numeric brushes, comparisons, crosshairs, axis navigators, scenario playback and progress editing reject for this producer rather than manufacturing coordinates.
Physical assistive-technology acceptance remains a separate validation boundary.

The compiled `OrganizationHierarchyNavigationRecipe` in `ChartForgeX.Examples` shows the ordinary source/basis preparation boundary used by the browser contracts.
The host still owns transport, opaque basis storage, immutable source snapshots, cancellation and authorization. It can supply localized branch messages through
`HtmlChartText.HierarchyCollapse` and `HierarchyExpand`, with a `{label}` placeholder. These messages are emitted only for organization hierarchy sections.

### Captured pictorial image paint

Pictorial image artwork participates in the same keyboard, selection and readout eligibility as native contour symbols. The immutable native image capture records its nonzero-alpha pixel-cell extent once; static SVG includes a closed private fact that the adapter maps through the current image rectangle, opacity, visibility and supported rectangular clips. Fully transparent, hidden, opacity-zero and completely clipped-away artwork does not borrow label text or transparent hit surfaces as data paint. Selection cues are real rectangles around the visible image extent, rather than unpainted image copies without resources.

This is image-paint availability under the native pictorial edge-to-edge partial rectangles, not a per-pixel alpha hit mask. Arbitrary interior alpha holes, independent rotated clipping frames and arbitrary SVG mask intersections are not established by that extent. No browser image decoding, tile fetching or browser pixel cache is added. Native contour defaults and captured source identities retain their existing contracts.

## Captured native presentation

PrepareNativeTransitionJson(from, to, options) captures complete native frames for an explicit intent. HtmlNativeTransitionOptions is immutable. Frame count is 2..16, or 3..16 for ValueFlash and Reflow legend motion; the complete UTF-8 payload is bounded to one MiB. Reduce frame count or source size when the bound is exceeded. Both native sections retain their actual HTML export scopes/mode. Every candidate frame passes shared section validation before source, basis, history or acceptance events change.

Values supports one or more identified readings through typed Progress, LayeredRadial, Circle, ProgressRing, Gauge and Bullet, plus the finite Line/Bar and Pie/Donut plans below. InitialValues uses an explicitly prepared native baseline; it does not invent zero for logarithmic/calendar coordinates or a hierarchy root. ValueFlash retains accepted values/geometry and changes changed native value ink, including caps/markers, toward an explicit peak ChartColor before returning to exact accepted paint. Peak alpha is part of that color. Authored series+observation identities remain distinct, including the same observation ID in different Bullet series. Automatic/unproven identities, unsupported formatter/presentation policy, different layouts/domains and unrelated families reject. Ordinary formatter callbacks remain callable in ordinary rendering. PrepareValueTransitionJson and its one-edit payload remain unchanged.

```csharp
var transition = renderer.PrepareNativeTransitionJson(before, after,
    new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialValues, frameCount: 3));
```

Install the authoritative final section, then call `chart.presentInitial(transition, { sceneRevision: chart.sceneRevision, motion: { duration: 650 } })`. Presentation does not accept another source or change managed basis, history or acceptance events. Update and managed reply paths use the same scheduler for Values, ValueFlash or LegendVisibility. They retain reduced motion, hidden document, export, failure, reset, supersession and disposal cleanup. Export uses the accepted native endpoint. Initial frames do not become hit targets or new source facts.

Native legend uses ChartVisibilityView and PrepareVisibilitySection for authored whole-series Line/Bar. Catalog, legend, observations and full-data domains/layout stay retained. PreserveOverlay is the default: labels keep global placement/order, and hidden-series labels are omitted with their exact native mark owner. Partial motion with enabled labels requires explicit GroupWithSeries, which paints each plot and its already positioned labels in one isolated native composite. Later series/context can occlude those captions. Native SVG and raster apply opacity once to the completed composite; labels do not fade independently. This Preserve layout targets whole series. The explicit Reflow policy below adds its native observation/group catalogs.

Opt in with `connectNative(callback, { basis, axes: ['x'], legend: true, motion: { duration: 650 } })`. A click sends kind=visibility on channel=legend with an absolute sourceVisibility containing hiddenSeries and labelLayering. The plot changes after a matching fresh native section and basis are accepted. The reply may include a captured LegendVisibility transition. Native legend requires a retained visibility view with no prior local mute/isolation. Relative mute/isolate synchronization is unavailable while native legend ownership is active.

Reset preserves hierarchy/axis channel priority and adds an absolute sourceVisibility descriptor only when native legend is opted in. A legend-only connection uses its own reset channel. A reset that also changes geometry can return the endpoint without a captured visibility transition; Preserve visibility frames require identical captured domains/layout; Reflow uses its captured native source and destination. The existing latest-window policy keeps one physical flight and one replacement slot. Busy legend clicks decline without mutation/events; reset may replace the absolute queued intent and waits for the same callback to settle. It adds no queue. axes:null with latest-window still requires the existing hierarchy reset owner.

`--native-transition-only --output <absolute-directory>` generates compact/wide light/dark adapter programs for multi-reading values, explicit native initial baselines, value flash and a Bar legend with labels off or explicitly grouped. Source stays outside the Core-only Recipes glob. Mixed Line/Bar coverage belongs to focused native/browser contracts. The finite plans do not imply coverage for every chart family or a global memory/performance bound.


### Whole-visual native initial fade

An already prepared native section can supply an explicit initial presentation without inventing baseline data:

```csharp
var final = renderer.PrepareSection(chart);
var initial = renderer.PrepareNativeTransitionJson(final,
    new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialFade, frameCount: 12));
// Embed final.Html and the existing adapter assets. Then use the accepted controller:
// controller.presentInitial(initial, { sceneRevision: controller.sceneRevision, motion: { duration: 650 } });
```

`InitialFade` fades one completed immutable native scene with isolated opacity. It retains logarithmic, calendar, hierarchy, layout and formatting facts; it does not animate values or grow geometry. The final-only overload accepts only this effect. Explicit value baselines continue to use the two-section overload. Native snapshots contain 2–16 frames and fit the existing one MiB transition budget. SVG and native PNG consume the same isolated scene; raster exports remain subject to the shared finite allocation/work budget.

`presentInitial` changes presentation over the exact already accepted destination. It leaves source values, revision, history, events and export unchanged. Reduced motion, hidden state, reset, supersession, resize, export and disposal settle through the existing lifetime. A stale scene revision or malformed candidate rejects before a presentation layer is installed. The final endpoint is the original unwrapped native scene. Every installable candidate uses the shared section validation. Initial fade is opt-in; ordinary static and interactive output stays on its existing path.


### Fixed Line/Bar native value frames

`PrepareValueTransitionJson(from, to)` also accepts one authored Line/Bar value edit with compatible fixed structure. `PrepareNativeTransitionJson(from, to, options)` captures multiple ordered readings or an explicit `InitialValues` baseline through that same native plan. A reading is identified by its authored series and observation IDs; the same observation ID in a different series remains a different reading.

The finite plan retains categories, disconnected segments, native line interpolation, markers, line layers, bar slots and surfaces, all captured physical axis policies, plot/frame, built-in formatting and paint. Values interpolate in source units and repaint through native scale and mark owners. Fixed positive/negative logarithmic and symmetric logarithmic value policies keep their real sign/domain rules; category time/calendar/session coordinates stay authored. Independent secondary value axes remain independent. Every endpoint must exactly reconstruct the complete native scene before frames are returned. Captured plans contain no caller chart or series and do not run custom callbacks later.

Changing observation/category topology, domains or frame policy rejects. Stacked/histogram/decimated data, explicit viewports, data labels, annotations, point legends and custom formatter callbacks remain outside this finite value plan; ordinary rendering remains available. A preserved whole-series visibility view uses the same captured full value frame and reapplies its existing native visibility policy. Reflow plus value morph is a separate policy. Cartesian `ValueFlash` rejects because shared line ink and layered bar surfaces do not yet own an independently captured point flash.

The host prepares and retains both native sections, then supplies the existing update transition with the current accepted scene revision. There is no new Cartesian edit command or producer queue. Existing reduced-motion, supersession, export, failure and disposal handling stays with the one presentation lifetime. These frames represent supported native value updates; whole-visual `InitialFade` remains available when a geometry/value baseline is not represented.


### Native visibility Reflow

`ChartVisibilityView` defaults to `ChartVisibilityLayout.Preserve`, which keeps the incumbent whole-series Line/Bar domains and placement. Explicit `Reflow` prepares copied visible source data through the native domain, slot, statistics and label owners. It retains the complete source catalog and legend. `ChartVisibilityTarget.Observation(seriesId, observationId)` targets real Line/Bar or supplied BoxPlot observations; `BoxPlotGroup` targets the separate authored raw-group namespace. Raw sample IDs cannot hide a group. Automatic IDs, hierarchy, decimation/histograms, numeric color dimensions, custom formatting callbacks, annotations/plot regions, per-point marker overrides and explicit viewports are outside this finite Reflow capture.

Reflow legend motion uses `HtmlNativeTransitionEffect.LegendVisibility`: captured native source fades out, the identity set switches at zero, then the native destination fades in. It describes transient presentation rather than geometry interpolation. At least three frames are required. The accepted destination, history, revision, source basis, hit regions and exports remain with the existing HTML lifetime throughout presentation. Reduced motion, cancellation, hidden/export/reset, supersession and disposal use the same scheduler. The default global label overlay remains intact; explicit `GroupWithSeries` retains the existing documented native paint-order change. A point/group legend under an explicitly hidden complete series stays disabled until the absolute view restores the series or Reset clears the hidden targets.

The opt-in source key includes copied full values, authored identities, point policies, axes, formatting, theme/render facts and captured native paint. A hidden raw-value or unpainted policy change cannot masquerade as the same source merely because its SVG is unchanged. No public Chart interchange family is introduced. The normal callback-based native connection receives absolute typed `sourceVisibility.hiddenTargets` with `layout:"reflow"`; the existing reset channel clears that absolute set. Busy relative legend toggles retain the existing pre-mutation rejection behavior; no extra queue is created.

`--native-reflow-only` writes compact/wide light/dark examples for Line, Bar, supplied BoxPlot and raw BoxPlot groups, together with ordinary Core + HTML adapter consumer programs. These finite examples do not establish value-morph or universal-family membership support.


### Pie/Donut native values and retained Other membership

Pie/Donut Reflow retains the complete native palette, catalog, legend readouts and authored observations, including supported automatic or explicit native bands. `ChartVisibilityTarget.RadialOther(seriesId)` targets the contributors captured from the full source's actual native aggregation. It remains distinct from an authored observation whose ID is `Other`; filtering does not redefine that contributor set. The native slice/label/legend owner recomputes visible geometry while source indices retain their original identity. Each band keeps its original series position and resolved band slot, even when every observation in that band is hidden. An Other target belongs to its captured series; identical observation IDs in another band remain distinct. Authored Treemap/Sunburst membership uses its native hierarchy view below.

Positive, unaggregated Pie/Donut observations in one or more compatible native bands support `Values`, `InitialValues` and the existing one-edit API when ordered native slice membership, layout, presentation and built-in formatter policy stay compatible. Complete native frames retain their own accessibility, regions and projections. Zero or aggregated value transitions, changed native sort order and radial ValueFlash remain outside this value plan. Negative Pie/Donut values remain invalid under native validation. Ordinary zero/aggregation rendering and supported Reflow retain their native semantics. Value morph and membership Reflow are separate captured intents; simultaneous membership/value edits reject.

Every measured font request, including omitted measurements and ascent requests, is captured from actual resolver inputs. Replay uses owned font bytes and captured raw-cluster fallback decisions in the same text shaper. It does not consult a later registry. A missing request, unknown output, missing glyph or unsupported repertoire rejects before a transition payload is returned. This finite fallback capture admits at most4096 clusters and65536 raw/output scalars per request. Changing formatted text or fallback repertoires beyond the captured compatible set remains unsupported; register the intended fonts before preparation and retain compatible native labels at both endpoints.

`--native-radial-motion-only` emits compact/wide light/dark radial value and Reflow programs. `--native-radial-bands-only` emits eight complete Core plus HTML adapter programs covering Pie, Donut and mixed bands, automatic/explicit slots, values, explicit initial baselines and scoped Other visibility. `--native-fallback-radial-only --primary-font <path> --fallback-font <path>` accepts explicit licensed host font files and embeds their normal host CSS in the generated example. These examples use Core plus the existing HTML adapter; they add no shipped font asset, renderer or producer queue.


Invariant Compact and automatic numeric value captions pre-observe their actual formatter branches, along with the native radial percentage and overflow captions, before the complete Pie/Donut value plan freezes. The captured request owner shapes these strings for actual None/Uppercase/Lowercase requests and retains their owned fallback decisions. Suffix, grouping, decimal and automatic significant-notation changes can then replay without later font-registry lookup. Observations remain within the existing per-request cluster/scalar limits; they do not become painted scene text. An unsupported unused probe preserves the existing stable-repertoire admission. Richer literal/section/scaled Number grammar remains outside this finite extension; the admitted named-culture and native contextual-case phase is described below.

`--native-value-text-repertoire-only --primary-font <path> --fallback-font <path>` emits ordinary Core/HTML programs for numeric captions crossing a compact suffix. Host fonts are explicit and embedded by the same example owner as the mixed-script fallback programs. No font asset or production dependency is added.


### Native hierarchy values and visibility

Authored Treemap and Sunburst items use `ChartVisibilityTarget.HierarchyItem(seriesId, nodeId)` with `ChartVisibilityLayout.Reflow`. A target hides its real descendant subtree through the native hierarchy relationship and layout owners. The retained source catalog keeps the original IDs, order, colors and full-source legend/readouts. A hidden ancestor disables its retained descendant legend controls; restoring that ancestor or Reset makes them available again. Observation IDs and hierarchy node IDs remain distinct namespaces.

The four-argument `ChartVisibilityView(hiddenTargets, layout, labelLayering, hierarchyView)` combines membership with an existing `ChartHierarchyView`. A null hierarchy view uses the full source. Hiding the requested root produces native no-data; it does not choose another root. `LeafAggregate` recomputes visible leaf sums. `AuthoredTotal` preserves valid authored group capacity and represents excluded children as remainder. Full original-source totals and all requested IDs are validated before filtering, so hiding cannot admit an invalid authored hierarchy.

Compatible fixed ordered `(id, parent, nullable authored-size)` structures support complete native `Values`, explicit `InitialValues`, and the existing one-edit API. Raw nonnegative authored sizes, including zero, are interpolated through the same immutable source snapshot and native Treemap/Sunburst compilers. New hierarchy reading facts carry `nodeId` and bind to actual native authored-size metadata. Every complete frame owns its accessibility and hit/region facts. The accepted destination, captured source, revision, export and cancellation behavior use the existing transition lifetime.

Native hierarchy Reflow uses the same fade-out, zero switch and fade-in plan with source/destination frame basis. Root navigation can carry the same absolute visibility descriptor when native legend preparation is connected. The existing reset channel clears both root and hidden targets, retaining hierarchy reset priority and the single backpressure replacement slot. The browser does not derive geometry, values, subtree membership or formatted captions.

This finite value admission excludes changing parent/ID/nullable-size structure, custom caption/format callbacks, simultaneous Reflow/value morph, and unobserved font/fallback repertoires. Ordinary rendering and endpoint Reflow retain those native policies where otherwise supported. The captured request owner observes latent native caption requests before freeze, so an authored zero can become positive without consulting the registry during replay. Font/fallback repertoires remain bounded; uncaptured formatting repertoires require their own native admission.

`--native-hierarchy-motion-only --primary-font <licensed.ttf-or-otf>` emits 52 standalone Core + HTML adapter programs: three parent/family policies, values/initial/subtree/root intents, compact/wide sizes and light/dark themes, plus four explicit Treemap Ellipsis programs. The supplied font is a host asset; these examples add no production dependency or installed font requirement to Core.


Treemap `Ellipsis` uses the existing plain wrapping/trimming owner during captured value replay. Every measured candidate and ellipsis marker uses its actual captured face and glyph decisions, with the incumbent simple-run kerning/addition order, synthetic emphasis allowance, line-height and whole-text-element trimming. The display transform is applied once before candidate slicing. Ordinary null capture retains the original layout path; a bitmap/default-font path remains ordinary rendering rather than entering outline replay. No frame-time font resolver or fallback registry is consulted.


### Captured Number, named cultures and native case context

Complete Pie/Donut and Treemap/Sunburst value frames can capture built-in `Number` C/E/F/G/N/P/R formats (bounded precision0..17 where the existing formatter accepts it), plus `0`, `0.#`, `0.##`, `0.0`, `0.00`, `#,##0`, `#,##0.#`, `#,##0.##`, `#,##0.0`, and `#,##0.00`. Named-culture Compact uses the same native formatter. The immutable numeric policy must pass its canonical `PortablePolicy` validation: a matching culture name or `CultureInfo.Equals` does not admit modified NumberFormat data. Ordinary formatting remains available for richer formats and custom providers; this finite repertoire does not reinterpret their syntax.

The existing formatter produces every captured observation. Numeric-culture symbols, separators, direction marks and actual native caption compositions enter the same captured request/fallback owner. Native LabelAndValue and Treemap header functions supply real text contexts for TitleCase, SentenceCase and ToggleCase; no generic prefix or second casing algorithm is substituted. The actual native text owner applies invariant casing and freezes raw/displayed facts with that culture binding. Replay does not read later CurrentCulture or FontRegistry state.

The original invariant Compact/automatic catalog and its default probe bytes remain unchanged. New Unicode observations use the existing scalar reader and the same per-request4096/65536 bounds, including lazy bounded native context enumeration. An unsupported unused observation preserves stable-repertoire admission; an uncaptured required run rejects before a transition payload is accepted. Richer custom/literal/section/scale/per-mille grammar and other remaining value/membership cohorts continue through their native owners.

`--native-value-number-culture-case-only --primary-font <path> --fallback-font <path>` emits 84 standalone Core + HTML adapter programs across Pie, Donut and Treemap, seven numeric/case policies, compact/wide sizes and light/dark modes. Both supplied fonts are host assets; the program requires their combined glyph coverage and adds no production dependency.


Custom Number capture admits formats bounded to128 UTF-16 units, three sections,17 digit placeholders per section and three scientific exponent digits. Native formatting still supplies every witnessed output, including quoted or escaped literals, scaling commas, percent/per-mille and signed/zero sections. Named culture providers must retain their canonical numeric facts. Captured font/case replay and scalar budgets still apply; an ordinary formatter being callable does not guarantee admission for native value playback.

The `--native-richer-number-only` example route emits eight compact/wide, light/dark Pie, Donut and Treemap programs through the existing numeric-caption recipe. It requires explicit primary/fallback font files, as does the ordinary numeric-caption route.


`HtmlChartInteractionOptions.CaptureNativeValueSource` explicitly captures full authored Pie/Donut raw rows, including zero values, omitted radius paint and actual native Other membership. It defaults to false. It requires authored series and observation identities, immutable native presentation and admitted captured text. Raw rows are not painted interaction targets. Values and InitialValues use complete native source compilation at each captured source-value observation, including the native no-data and aggregate policies; negative Pie/Donut remains invalid. The one-edit API retains its old contract and rejects this opt-in raw route in favor of PrepareNativeTransitionJson.

`HtmlNativeTransitionEffect.SourceReflow` accepts a changed raw source and changed absolute Reflow view together. Both endpoints must retain ordered authored structure, full target catalog, presentation and compatible captured face/cluster facts. Derived Other contributors may change through the real native source resolver. Its frames fade the old native view to zero, switch to the new native view at zero, then fade in. This is source reflow, not general simultaneous value/shape/membership interpolation. Ordinary LegendVisibility still requires the exact same full source. Native producer replies use the existing validated transition, request channel/basis and acceptance lifetime. No callback, JavaScript sum/formatter/geometry or alternate renderer is retained.

The `--native-raw-values-only` examples route emits eight compact/wide light/dark standalone programs and exact native before/after artifacts. The opt-in raw source and per-frame facts remain bounded by canonical source capture and the existing one-MiB transition budget. A native domain, source/presentation, missing captured request/cluster or budget failure rejects before a complete payload is published. Broader family values, unbounded fallback repertoires and general joint geometry transitions remain separate capabilities.


ValueFlash also uses actual owned Line/Bar, Pie/Donut and Treemap/Sunburst ink. A changed Line reading flashes that series' shared native line layers and only its changed point markers; it does not split paths into fabricated point segments. Bar surfaces include their native gradient stops, patterns and caps, while static tracks remain unchanged. Pie/Donut uses actual slice/outline/pattern groups; opt-in raw Other uses its actual contributors and derived-value/membership change. Hierarchy uses authored nodes and actual changed resolved native ancestor values. Unchanged series, labels, axes, metadata and geometry retain their captured values.

The peak color is explicit. Frames begin and finish at the exact accepted destination, with transient native paint cloned from that immutable destination. Geometry, clipped group ownership, paint order, native gradients and source IDs are retained. The existing lifetime handles cancellation, hidden/reduced motion, export and disposal. Raw-source flash facts retain the accepted full source, never a fake zero/Other observation. The `--native-owned-flash-only --primary-font <licensed-font>` route emits eight native/program cases. Standalone programs take that font path as their sole argument. This adds owned ink flash; it does not establish value geometry interpolation for another family or general simultaneous morphology.


Finite native Scatter values and explicit initial baselines reuse the existing Cartesian value plan, marker geometry/paint, actual captured source transforms and HTML lifetime. Ordered authored series/observation IDs, X coordinates, all-axis policy/domains, plot, marker/paint policy and source structure remain fixed. Y values interpolate through the actual native mapper. Custom Scatter semantic marker roles retain their native marker owner, including value flash; static context and unchanged observations stay untouched. Decimation, numeric color dimensions, stacks, enabled labels, explicit viewport and incompatible source/domain changes remain outside this finite capture. This does not add Bubble size-tuple, error/range or numeric-radial interpolation or Scatter Reflow membership. The `--native-scatter-values-only --primary-font <licensed-file>` route emits four programs/32 native/source artifacts; programs take that font path as one argument.


`HtmlNativeTransitionEffect.ValuesAndVisibility` explicitly combines finite Line/Bar values and absolute whole-series Preserve visibility. It repaints each intermediate raw value through the existing native value plan, then applies the existing isolated native series opacity painter to that real geometry. Ordered authored identities, category/X coordinates, native all-axis domains/plot, paint/format and label policy stay fixed. Both at least one reading and one series visibility must change. Enabled labels use the complete native route below and require explicit GroupWithSeries; captured Bar stacks use the full-source Preserve contract below, while derived topology and Reflow point/group/hierarchy joint membership remain separate owner work.

Values and LegendVisibility retain their separate strict contracts. Joint selection uses one destination acceptance/revision/basis/history and the same finite scheduler/lifetime, not a second controller or a new geometry renderer. Every candidate must retain the exact full authored point catalog, native domain policy, valid opacity/resources and complete prepared-section validation before commit. Old/new endpoints and exports stay exact. The existing managed payload cap and native JSON budget remain unchanged; no aggregate timeout is added.

`--native-joint-values-visibility-only --primary-font <licensed-file>` emits four compact/wide light/dark programs with before/after native artifacts. Standalone programs take that font file path as their sole argument. This is continuous native value geometry plus whole-series Preserve membership/opacity; it does not claim general joint point/group/Other/hierarchy reflow morphology.


### Complete native Cartesian values

Line/Bar values with enabled captions, StepLine, Area, StepArea, stacked/normalized Bar and StackedArea use a complete detached native value plan. Ordered authored series/observation IDs, X coordinates, breaks, stack groups/normalization, StackedArea missing-data policy, axes, paint and caption policy stay fixed. Each frame replays the captured font requests and invokes the existing native chart compiler for paths, stack baselines, totals, labels, readouts, accessibility and regions. Synthesized Gap/Zero vertices retain their actual native geometry and acquire no observation IDs.

This route requires authored fixed used value-axis bounds and exact matching native axis/plot facts on every frame. Changed automatic domains, changing label gutters, horizontal layouts, prepared time/session closures, callbacks, decimation, histograms, numeric color dimensions, per-point marker overrides, explicit windows and unsupported tuple families reject this value capture while ordinary rendering remains available. The existing labels-off unstacked Line/Bar/Scatter fragment route retains its behavior and output. This does not establish arbitrary topology-changing joint interpolation.

Enabled-label joint Line/Bar Preserve motion requires `ChartVisibilityLabelLayering.GroupWithSeries`. Actual complete native geometry and labels are placed first, then each series' plot and associated captions share one native composite. The existing inter-series/global-overlay paint-order tradeoff still applies; PreserveOverlay partial label motion rejects. Area value flash owns the changed series' actual area fill/pattern and shared line ink, plus changed observation markers; accepted labels and static context retain their paint.

`--native-complete-cartesian-values-only --primary-font <licensed-file>` emits 18 source programs and 144 source/native artifacts for nine finite native examples in compact light/wide dark. Each emitted program takes that font file as its single argument and uses the existing native producer/controller.


### Joint native Bar stacks under Preserve

`ValuesAndVisibility` also supports compatible native Bar stacks with named or default stack groups and native normalization. Each intermediate raw value is prepared by the same complete native chart plan before the existing series composite changes opacity. Ordered source identities, category coordinates, stack/normalization policy and exact native axis/plot facts remain fixed. Actual native guard failures, changing layout and uncaptured formatting still reject the capture.

Preserve retains all full-source stack contributions. Shared stack totals, their captions and total regions remain global context with the native full-source values for that frame; hiding one series does not subtract its contribution or renormalize the visible series. Only actual series ink and its owned point captions change opacity. Enabled point captions require `GroupWithSeries`; shared total captions keep their existing global placement and paint order. `PreserveOverlay` partial enabled-point-label motion still rejects. Reflow/renormalized hidden-series geometry and topology-changing membership are separate capabilities.

The separate Values and LegendVisibility intents still reject accidental mixed updates; callers select `ValuesAndVisibility` explicitly. Accepted endpoint/default exports, full catalogs, native hits, captured fonts and the existing lifetime/revision/cancellation/backpressure/export rules are retained. `--native-joint-stack-values-only --primary-font <licensed-file>` emits six compact-light/wide-dark public programs and 48 source/native files; each program takes that font file as its sole argument.


### Typed Bubble Y/Size native values

Set `HtmlChartInteractionOptions.CaptureNativeValueSource = true` when preparing both Bubble sections, then use `PrepareNativeTransitionJson` with `Values`, an explicit `InitialValues` baseline, or `ValueFlash`. Each reading is the real authored `(series ID, observation ID, component)` tuple. The finite Bubble components are `y` and `size`; sharing an observation ID across series does not merge readings. `PrepareValueTransitionJson` remains the scalar one-edit entry and rejects component plans.

This cohort accepts one or more Bubble series with fixed ordered authored observations and X coordinates, fixed X and used numeric Y domains, and an explicit shared `Bubble.WithSizeDomain`. Raw Size stays strictly positive; values outside that visual size domain retain the native radius clamp. `SquareRootRadius` and `Area`, reversed scales, series marker shapes, fixed ColorValue, captions and readouts use the existing native compiler for every complete frame. A secondary value axis keeps its own domain. Every frame must retain the exact native axes and plot. Export uses the exact accepted endpoint.

The opt-in snapshot owns copied full typed tuples, IDs, paint, options and captured formatter/font requests. Hidden or unpainted facts participate in its source key even when SVG/PNG ink stays unchanged. Replay does not retain a caller chart or query the font registry. Custom or modified-provider formatting, uncaptured fallback decisions, automatic/changing axes or size domains, changing X/ColorValue/identity/presentation, point legends, point-specific marker/outline policies and mixed tuple families reject this finite capture. Ordinary native rendering and default metadata remain available without the opt-in. Size0/negative is already invalid in `ChartBubble`; animation does not create a synthetic observation for it.

Bubble visibility/Reflow/joint value-plus-membership motion, other tuple components and topology changes require their own native membership admissions. This capability is native value geometry interpolation, not a claim that every chart family or arbitrary glyph repertoire supports it. It retains the existing reduced-motion, hidden/export/reset, supersession, cancellation, backpressure, revision and disposal lifetime; no new callback channel or queue is added.

`--native-bubble-components-only --primary-font <licensed-font-file> --output <absolute-directory>` emits compact/wide light/dark public programs and captured endpoints for both size encodings and explicit initial baselines. The Core recipe remains adapter-free; the executable program references the existing HTML adapter.


### Typed RangeBand/RangeArea native values

The existing `CaptureNativeValueSource` opt-in and `PrepareNativeTransitionJson` support `Values`, explicit `InitialValues`, and `ValueFlash` for authored RangeBand/RangeArea sources. Each observation contributes its real `lower` and `upper` readings, scoped by series and observation IDs. X, BreakBefore, ordering, interpolation, axes, formatting and presentation remain fixed. Raw lower/upper values retain their captured numeric display-domain facts and intrinsic native validity; the native clip policy handles values beyond that window. The native `ChartRangeBand`/`ReplaceRanges` and paired-boundary compiler validate and paint every tuple, curve, envelope, midpoint, caption and region; encoded point slots and derived midlines do not become new observations.

Range uses the same immutable complete component plan and detached full source/font snapshot as Bubble. Its source facts carry no size mapping or reversal fields. All candidate frames pass the same shared native section validator before acceptance, and the accepted endpoint owns export, source, revision, history and hits throughout transient presentation. Ordinary null/default SVG/PNG and existing Bubble component payload paths retain their output.

This admission requires fixed ordered authored identities/X/BreakBefore and captured X/used numeric Y axes. Time-coordinate updates, automatic domain/layout changes, custom or modified-provider formatting, uncaptured font decisions, changing topology/presentation and simultaneous visibility/membership remain unsupported at this admission. The scalar one-edit API rejects component plans. Range `ValueFlash` follows the shared Line ownership law: changing either lower or upper repaints the complete changed series envelope, hatches, both boundaries and their existing line layers across every real disconnected run, plus its RangeArea midline. Both actual endpoint markers and their hatches flash for each changed tuple; unchanged tuple markers, all captions and unrelated series retain their paint. The existing premultiplied sRGB flash law mixes RGBA, including translucent envelope alpha, while geometry, mark opacity, clips, identities, regions and accepted values remain fixed. First and last frames use the exact accepted destination; export always uses that destination.

`--native-range-components-only --primary-font <licensed-font-file> --output <absolute-directory>` emits compact/wide light/dark public programs and native endpoints for RangeBand/RangeArea Values and explicit initial baselines, together with a separate captured ValueFlash payload. Each program writes an independent `visual.flash.html` page and its `visual.flash.transition.json` payload using the same public capture entry. The Core-only recipe retains its existing package boundary.


### Typed ErrorBar native values

The same `CaptureNativeValueSource` opt-in and complete component plan support ErrorBar `Values`, explicit `InitialValues`, and changed-point `ValueFlash`. A real observation contributes `value` for Y, `x-lower`/`x-upper` only when XBounds exists, and `y-lower`/`y-upper` only when YBounds exists. Absent intervals produce no synthetic mean/bound reading. X, ordered series/observation IDs, nullable interval shape, captured X/numeric member axes and presentation stay captured. X bounds use the actual X domain; Y and Y bounds use their real primary/secondary value domain.

Every frame reconstructs through `ChartErrorBar`, `ChartErrorBounds` and `ReplaceErrorBars`, preserving native order/containing guards, caps, markers, label/readout/region owners and exact export policy. Browser preflight validates the captured nullable shape, complete ordered members, domains and containing facts; it computes no whisker/cap geometry or text. Flash repaints the actual changed point group once, including its whiskers/caps/marker; unchanged observations and labels retain exact paint.

The source snapshot/canonical writer/font replay and accepted-scene scheduler/lifetime are shared with Bubble/Range. No new API, version, controller, queue or formatter is introduced. Ordinary native rendering and default metadata remain unchanged. Changing X/nullable interval presence, automatic axes, time-valued components, uncaptured/custom formatting/font policy and simultaneous visibility/topology changes remain required continuation. Native-invalid intervals still fail in the typed model; they are not animation observations.

`--native-error-components-only --primary-font <licensed-font-file> --output <absolute-directory>` emits X-only, Y-only and combined public programs for Values, InitialValues and point flash at compact/wide light/dark sizes.


### Typed financial native values

The same `CaptureNativeValueSource` opt-in and native component entry support Candlestick and OHLC `Values`, explicit `InitialValues`, and changed-point `ValueFlash`. Readings are the authored `open`, `high`, `low`, and `close` under their real series/observation identities. X, ordered catalog, captured X/numeric value axes, labels, presentation, both financial direction policies and nullable observation overrides remain captured. Source keys include full prices and all retained paint/dash policies, including a currently unused direction. Old scalar one-edit admission remains strict.

Each frame reconstructs via `ChartCandlestick` and `ReplaceFinancialValues`. The existing financial compiler owns body width, slots, rising/falling/doji paint, hollow wicks, direction and datum dashes, label/readout text, bounds, regions and legend. A rising-to-falling update passes through its real native doji. Browser preflight validates exact OHLC members, domains and native high/low containment; it computes no price geometry or formatting. Flash affects the complete changed native point mark once, leaving other observations and all labels unchanged.

Ordinary null/default rendering is unchanged. This component admission requires captured X/numeric value domains and unchanged X/catalog/presentation. Native clipping owns raw prices beyond the displayed window; fixed temporal X follows the existing native time policy. Changing authored membership and simultaneous tuple/visibility topology require separate admissions. Native-invalid prices reject in `ChartCandlestick` before an observation exists. The finite native tuple contract does not imply universal topology or physical/pixel parity.

`--native-financial-components-only --primary-font <licensed-font-file> --output <absolute-directory>` emits both financial forms for Values/InitialValues/flash at compact light and wide dark sizes through public APIs.


### Fixed native time categories for components

Opt-in component value plans retain primary-X elapsed, included-session, ordinal and calendar-unit time policy while updating numeric Bubble/Range/ErrorBar/Candlestick/OHLC values. X stays authored and fixed. The immutable source copies time-zone, sessions and spacing; its private copied axis discards only the generated time domain, so the existing native time owner recomputes it for every complete frame. Original prepared axes remain untouched. Exact captured axes, sessions, calendar instants and plot transforms must match after compilation. No baseline, calendar, DST, session or projection arithmetic is recreated in JavaScript.

The source and transition validators compare the actual native axis facts and fixed per-observation X/scale/instant metadata across endpoints and every candidate before acceptance. Only this component branch admits temporal X. Time-valued Y or ErrorBar X interval components, custom time label callbacks, prepared viewport state and changing category coordinates/time policy remain explicit errors. Ordinary rendering keeps its current formatting and time policies.

`--native-component-time-only --primary-font <licensed-font-file> --output <absolute-directory>` emits a bounded set of elapsed/session/ordinal/calendar public programs at compact light and wide dark sizes. Geometry remains the real native family compiler; source capture and lifetime/export policy are shared with numeric components.


### Raw component values and native clipping

Captured native component values retain finite authored readings beyond the displayed axis minimum/maximum. The same complete native compiler, tuple guards and captured plot/clip policy own each frame. Exact full/current axes and endpoint exports must still agree. `CaptureNativeValueSource` never invents a clipped observation or sums geometry in the browser. The full catalog, raw values, native regions/readouts and source keys remain captured even when ink lies outside the plot.

Positive raw Bubble Size still follows its native size-domain clamp; zero/negative Size remains invalid. Logarithmic axes retain their intrinsic positive/negative sign and nonzero rules independently of their display window. Native high/low/containing/nullable interval guards remain unchanged. Component-only browser preflight validates those actual facts without projection or formatting.

Fixed Time X, numeric animated members, immutable source/paint/font policy, exact native endpoints, current acceptance/basis/export and the single existing scheduler remain required. Changing authored tuple/catalog topology and genuinely joint membership/value morphology are separate remaining capabilities. Native-invalid domains and arbitrary callbacks are not admitted through clipping.

`--native-component-clipping-only --primary-font <licensed-font-file> --output <absolute-directory>` emits real values entering a fixed native clip for Bubble/RangeArea/ErrorBar/Candlestick/OHLC at compact light and wide dark sizes.
