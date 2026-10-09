# Interactivity Reference

ChartForgeX keeps static SVG, PNG, and HTML output deterministic by default. Browser behavior lives in `ChartForgeX.Interactivity.Html` and is opt-in through reusable feature flags on `ChartInteractionOptions`.

The HTML adapter works from renderer metadata such as `data-cfx-series`, `data-cfx-series-key`, `data-cfx-point`, `data-cfx-label`, `data-cfx-id`, and `data-cfx-role`. Chart families can expose their own shapes and still reuse the same hover, selection, keyboard traversal, compare tray, crosshair, lasso, focus trail, reveal label, scenario, and playback contracts.

The selected-target compare tray appears below the chart viewport, preserving axes, outer labels, and pointer access to marks. Its controls wrap inside compact hosts. Clearing the selection returns focus to the chart when the clear button held focus. Graphite charts use the chart's surface, text, and accent colors for these controls.

Native painted marks retain their observation identity when pointed at. A producer declares polar geometry with `data-cfx-coordinate-system="polar"` on the SVG or a containing semantic group. The adapter uses native SVG hit testing for those observations, leaves empty polar space without a nearest-point target, and suppresses the Cartesian crosshair. Nested observations inherit their containing series identity. Cartesian charts retain the nearest-point readout over the plot background.

Every rendered interaction surface is normalized to `data-cfx-target-kind` and `data-cfx-target-id` before bindings run. The current target kinds are `series`, `point`, `annotation`, `region`, `node`, `link`, and `legend`. Hosts can therefore route one scenario across Cartesian marks, topology nodes and links, annotations, map regions, and legend entries without depending on renderer-specific markup. Use `AddRegionStep`, `AddNodeStep`, `AddLinkStep`, or `AddLegendStep` when building those routes.

Explicitly decimated series keep their original point identity. `data-cfx-point` remains the rendered ordinal, while the series source-index map and each host event's `sourcePoint` identify the caller's original point.

Aggregate marks such as histogram bins, merged timeline runs, and pie slices expose contributing source observations through `target.sourcePoints`, a numeric array in hover and selection events. An empty histogram bin reports `[]`. For these marks, `target.sourcePoint` is `undefined` because one source observation cannot identify an aggregate. Target IDs include the interaction key and derivation kind. Histogram identities also retain interval bounds, terminal-boundary inclusion, aggregation, and encoding; matching intervals can synchronize across different bin ordinals, while different summary policies remain independent. Grouped pie slices use their contributing source indices, and merged timeline runs use their start/end interval. Derived summaries cannot select an unrelated observation or a different summary kind merely because their rendered ordinals match. Point legends share their mark's contributors and target ID. Hosts should route aggregate selection by the opaque `targetId` and read `sourcePoints` when they need the underlying observations.

Raw trend-line and box-plot summaries omit both singular `sourcePoint` and contributor arrays. Their source data remains available through the series' immutable `TrendLineSourcePoints` and `BoxPlotSourceSamples` collections. Migration: derived target IDs now include their derivation kind; use event-provided IDs instead of constructing IDs from point ordinals.

Radar fills missing categories with zero geometry. Those categories are `region` targets identified by the series interaction key and category, for example `target:category:2`. They report `sourcePoints: []`, with no `point` or `sourcePoint`; their browser readout says "No observation" and carries no measured value. An authored zero remains an ordinary source point with a value of zero. Category identities synchronize correctly when peers order their observations differently or include another category.

## Keyboard navigation

When `ChartInteractionFeatures.KeyboardNavigation` is enabled, data marks and legend entries are separate roving components. Tab enters each component once and returns to its last active target; Left/Right and Home/End move within a series or legend. Up/Down switches data series at a matching coordinate or source observation, with an ordinal fallback for uneven series. Families without a series/point grid use their deterministic rendered-target order. Hidden targets and aggregate series wrappers do not become extra data stops.

Muted data leaves the data component, while its legend stays reachable for unmuting. Reset restores the data component after all series are muted. Charts initialized inside a host hidden with `hidden` or `display: none` acquire their Tab stops when the host regains layout; hiding and revealing the host preserves each component's active target.

Moving to an offscreen target scrolls the chart's readable viewport locally. Native data links retain Enter navigation, while Space selects a data target when selection is enabled. Legend Space toggles muting and Shift+Space toggles series isolation. `cfxnavigate.index/count` refer to the active data or legend component. Disabling keyboard navigation leaves host key handling and authored links available without adding adapter navigation stops.

## Graphite appearance

Graphite charts use a surface tooltip with a 1 px border, 6 px radius and shadow. Shared-x tooltips show a bold x label, 10 px series swatches, and bold full numeric values in a right-aligned tabular column. Rows sort declared states before ordinary values; swatches follow the rendered series colours, including host SVG properties. Quiet series use muted text. A 1 px dashed crosshair follows the axis token; hovered points grow to a 4 px radius with a 2 px surface ring. Pointing at a mark or legend item, or focusing one with the keyboard, keeps that series and the hovered point's marker at full strength while other series dim to 30%; pie-like point legends emphasize one point. Over the plot background the crosshair is a shared readout and every series stays at full strength. The chart root exposes the current mode as `data-cfx-hover-mode` (`series` or `shared`).

Legend tooltips summarize the series for readers: the name with its colour swatch, plus the latest value and its x label for trend series (line, area, step, slope), the total for bar and lollipop series, or the point's value for point legends. They never show renderer metadata such as role or kind.

Muting a series retains it at 30% opacity and strikes through its legend label. Keyboard targets use a 2 px focus outline in the host accent.

There is no permanent toolbar unless zoom, pan, brush, or export are enabled. `IncludeResetButton` (on by default) adds a 28 px "Reset view" ghost button in the top-right corner of the chart frame. It appears only while the view differs from its initial state (zoomed, panned, brushed, or a series muted or isolated) and hides again after reset, returning keyboard focus to the chart. Resetting also clears selections, focus trails, and pinned tooltips. Hosts can set `--cfx-host-accent` on the interactive container to align that outline with their own controls. Static exports retain the same flat marks and colour roles without browser behavior.

## Tooltip modes

`HtmlChartInteractionOptions.TooltipMode` selects the readout independently of the theme or palette. The default, `HtmlChartTooltipMode.SharedX`, shows one visible observation per series at the target's numeric x coordinate. When several observations share that x, the tooltip uses the pointed or focused observation for its series and the first eligible observation for each other series. Rows retain full source values and sort by declared state, then descending value. Muted series, hidden marks, and missing or non-finite values are omitted. Swatches follow the painted marks, including point colour overrides, gradient fills, and host SVG properties; marker-free lines use their line paint.

Visibility follows the primary data mark. An ancestor's `display:none` or zero opacity hides its descendants; a mark can restore inherited `visibility:hidden` with `visibility:visible`. Hidden line markers retain a shared row while the series path remains visible. Charts marked `AsDecorative()` retain pointer tooltips because `aria-hidden` affects accessibility exposure, not painted visibility.

Use `Single` to inspect the pointed or focused target and its metadata:

```csharp
chart.SaveInteractiveHtml("observations.html", options => {
    options.TooltipMode = HtmlChartTooltipMode.Single;
});
```

Shared-x readouts use Cartesian source observations, including scatter and bubble values. Targets with coordinates that describe layout or categories, such as heatmaps, maps, pie slices, and radial charts, keep a single-target tooltip with their value and metadata. Range and financial summaries also retain their individual bounds and measures rather than reducing them to one shared value. Derived regression endpoints use the same single-target fallback. Single-target readouts show the series, values, bounds, and caller-supplied metadata. Renderer roles, source ordinals, and chart-kind names are omitted from the tooltip; normalized identities and host events retain those fields. Legend tooltips keep their series summaries in either mode, including muted series. `HtmlInteractiveDashboardOptions.TooltipMode` applies the same choice to every child chart. Both modes require `ChartInteractionFeatures.Tooltips`; choosing a mode does not enable the feature.

## Semantic Series Identity

Series ordinals are local rendering details. Synchronized dashboards therefore match legend, hover, and selection state by `data-cfx-series-key`, never by an ordinal from a different chart. The series name is the automatic key, so charts with the same named measure work without extra configuration. Set an explicit key when display labels differ but the underlying measure is the same:

```csharp
var current = Chart.Create()
    .AddLine("Current pass rate", points);
current.Series[0].WithInteractionKey("quality.pass-rate");

var history = Chart.Create()
    .AddSmoothArea("Pass rate history", historyPoints);
history.Series[0].WithInteractionKey("quality.pass-rate");
```

Point-level legend entries retain their point identity, so toggling one pie or radial item does not mute the complete containing series. A peer chart that does not contain the semantic key is left unchanged. Call `UseAutomaticInteractionKey()` to return to name-based identity.

Interaction keys cannot contain only digits because numeric scenario targets are reserved for local zero-based series ordinals. Digits-only display names receive an automatic `series:` identity prefix; prefer an explicit domain key such as `quality.pass-rate` in public dashboards. Point legends are emitted only for chart families whose visible geometry is point-scoped. Connected line, area, radar, polar, and other aggregate paths retain a series legend so a point toggle never leaves the main shape visibly active.

Scenario routes use the same identity contract. `AddSeriesStep(...)` accepts a stable non-numeric interaction key or, for local legacy routes, a zero-based series ordinal. Prefer the interaction key when series can be reordered or reused across dashboards.

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

Scenarios are ordered, host-neutral timelines rather than browser-only tours. Configure pacing and visual intent on the scenario model; adapters consume the same contract:

```csharp
interaction.AddScenario("recovery", "Recovery route", scenario => scenario
    .WithDescription("Inspect pressure, then confirm the recovered signal.")
    .WithPlayback(stepDurationMilliseconds: 1100, loop: false, autoPlay: false)
    .WithFocusMode(ChartInteractionScenarioFocusMode.Highlight)
    .AddSeriesStep("incident-pressure", "Inspect pressure",
        configure: step => step.WithDuration(1600))
    .AddSeriesStep("probe-success", "Confirm recovery"));
```

`Highlight` is the default: it emphasizes route members while preserving titles, axes, and surrounding data context. Use `Spotlight` only when dimming non-route data is intentional. Playback and autoplay are opt in, autoplay is suppressed when the browser requests reduced motion, and the HTML adapter exposes previous/next controls plus an accessible range scrubber for direct step selection. Individual step durations override the scenario default; both accept 200-60000 milliseconds.

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

## State Bookmarks

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

When synchronized charts are enabled, state payloads use the same `cfxsync` channel with action `state`; each chart applies only the target identities and scenario ids it understands.
