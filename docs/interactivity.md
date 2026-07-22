# Interactivity Reference

ChartForgeX keeps static SVG, PNG, and HTML output deterministic by default. Browser behavior lives in `ChartForgeX.Interactivity.Html` and is opt-in through reusable feature flags on `ChartInteractionOptions`.

The HTML adapter works from renderer metadata such as `data-cfx-series`, `data-cfx-series-key`, `data-cfx-point`, `data-cfx-label`, `data-cfx-id`, and `data-cfx-role`. Chart families can expose their own shapes and still reuse the same hover, selection, keyboard traversal, compare tray, crosshair, lasso, focus trail, reveal label, scenario, and playback contracts.

Every rendered interaction surface is normalized to `data-cfx-target-kind` and `data-cfx-target-id` before bindings run. The current target kinds are `series`, `point`, `annotation`, `region`, `node`, `link`, and `legend`. Hosts can therefore route one scenario across Cartesian marks, topology nodes and links, annotations, map regions, and legend entries without depending on renderer-specific markup. Use `AddRegionStep`, `AddNodeStep`, `AddLinkStep`, or `AddLegendStep` when building those routes.

Explicitly decimated series keep their original point identity. `data-cfx-point` remains the rendered ordinal, while the series source-index map and each host event's `sourcePoint` identify the caller's original point.

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

The HTML adapter uses `HtmlChartResponsiveLayout.Readable` by default. On narrow screens it keeps the fixed-design SVG legible inside a contained horizontal viewport instead of shrinking labels into a thumbnail. Set `ResponsiveLayout = HtmlChartResponsiveLayout.Fit` when seeing the entire chart at once is more important than minimum label size.

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
