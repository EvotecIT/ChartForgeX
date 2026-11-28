# Browser data charts

`HtmlDataChartAssets.ScriptFile` emits a standalone, content-addressed browser renderer. Load it when a reader first opens a data chart. It includes the native Graphite surface policy and palette and has no runtime package dependencies.

This entry point serves hosts that obtain raw numeric data in the browser, such as a grid selection or a pivot result. Charts authored in .NET use the prepared chart interaction adapter.

```csharp
var asset = HtmlDataChartAssets.ScriptFile;
File.WriteAllText(asset.FileName, asset.Content);
```

```html
<button id="update-chart">Show next period</button>
<button id="close-chart">Close chart</button>
<div id="chart"></div>
```

```javascript
const lifetime = new AbortController();
const data = {
  type: "column",
  label: "Orders by region",
  categories: [{ id: "north", label: "North" }, { id: "south", label: "South" }],
  series: [{ id: "orders", name: "Orders", values: [12, 8] }]
};
const chart = ChartForgeXRenderer.create(document.getElementById("chart"), data, {
  signal: lifetime.signal,
  onSelect(mark) { console.log(mark.categoryId, mark.seriesId, mark.value); }
});
document.getElementById("update-chart").addEventListener("click", () => {
  data.series[0].values = [13, 9];
  chart.update(data);
}, { signal: lifetime.signal });
document.getElementById("close-chart").addEventListener("click", () => {
  lifetime.abort(); // Equivalent to chart.destroy().
}, { once: true });
```

## Data contract

The supported types are `line`, `area`, `bar` (horizontal), `column`, `stacked` (vertical), `win-loss`, `bullet`, `donut`, `gauge` and `state-timeline`. Bullet accepts a measure series and an optional target series. Win/loss draws the sign and returns the original numeric value when selected.

A donut takes one series of nonnegative values, one per category; each category is a slice and a legend entry, and the total sits in the centre. A gauge takes one category, a value series and an optional target series, plus optional `minimum` (default 0) and `maximum` (default the larger of 1 and the values). Its arc and hole follow the native gauge and donut defaults.

A state timeline draws each series as a lane over the categories, which are periods. It needs `states`, up to 32 of `{ id, label, color? }`, and each value is a state index or `null` for no data. Consecutive periods in one state form one mark, named by its first period. Without a `color`, state `n` uses `--cfx-state-n`, then the palette; colours must be hexadecimal.

```javascript
const chart = ChartForgeXRenderer.create(host, {
  type: "state-timeline", label: "Site health",
  states: [{ id: "up", label: "Up", color: ChartColor.FromHex("#2f9e44") }, { id: "down", label: "Down", color: ChartColor.FromHex("#d6336c") }],
  categories: hours,                                  // [{ id, label, instant }]
  series: [{ id: "dc1", name: "DC1", values: [0, 0, 1] }]
});
```

Each category has a unique nonempty `id` and a visible `label`. Each series has a unique nonempty `id`, a visible `name` and exactly one finite number or `null` per category. Null observations leave gaps in lines and areas. Zero observations retain zero ink and have a separate pointer target when selection is enabled. Strings are limited to 2,000 characters, categories to 10,000 and series to 32. Sparse arrays and numeric domains whose span overflows are rejected.

For line and area charts, optional ISO date strings in category `instant` fields give proportional time spacing. Every category must then have a valid instant, strictly increasing in the supplied order. Other chart types use category bands. `compact: true` omits headings, legends and axes for chart cells.

Selection supports click, Enter and Space. Arrow keys, Home and End move between marks. Stable category and series IDs preserve focus across updates. If a focused mark disappears, focus moves to the first remaining mark of its series, then the first mark, or the empty drawing. The selected mark keeps an outline across redraws. `onSelect` receives an immutable `{ categoryId, seriesId, value }` record.

## Layout and lifetime

The renderer measures its host and redraws through `ResizeObserver`. Category band fill, grouped gaps, bar caps, stack separators, palette, line width, marker radius and area opacity come from native ChartForgeX policy when the asset is emitted. Browser conformance tests compare slot bounds against native layout across narrow and wide hosts.

`update` validates a complete descriptor before replacing the accepted drawing. `replaceSeries(series)` replaces one series by id, or adds it, and validates the resulting chart like `update`. Host-authored siblings remain in place. Creating another controller for the same host disposes the prior drawing. `destroy` is idempotent and releases the observer, pending animation frame, event listeners and owned SVG. Updates after disposal throw.

## Live windows

`append({ categories, values }, { maximum, span })` adds periods to the accepted chart, with one value per new category for each series named in `values` (other series get `null`). `maximum` keeps at most that many periods. `span`, for temporal categories, drops periods whose instant is more than `span` milliseconds older than the newest one. With both, the stricter one wins. The oldest periods leave first, so the window, its marks and the DOM stay the same size however long the feed runs.

```javascript
const chart = ChartForgeXRenderer.create(host, latency, { onSelect: showDetails });
feed.onmessage = event => {
  const sample = JSON.parse(event.data);          // { at, dc1, dc2 }
  chart.append({
    categories: [{ id: sample.at, label: sample.at.slice(11, 19), instant: sample.at }],
    values: { dc1: [sample.dc1], dc2: [sample.dc2] }
  }, { maximum: 1000, span: 15 * 60 * 1000 });
};
```

Marks keep their category and series IDs (`data-cfx-category`, `data-cfx-series`) as the window moves, so selection, keyboard focus and the roving tab stop follow the same observation across commits:

- A selected mark stays selected while its period is in the window; `selection()` returns its `{ categoryId, seriesId, value }` with the current value, or `null`. When the period leaves the window the selection is released without calling `onSelect`.
- A focused mark keeps focus. When its period leaves, focus moves to the first remaining mark of the same series, then to the first mark, then to the empty drawing.
- The mark holding the tab stop keeps it while focus is elsewhere on the page, with the same fallback.

`batch(callback)` runs several `update`, `append` and `replaceSeries` calls on the controller and draws the result once, for a feed that delivers several samples or series at a time. Each change is validated as it is made; one that throws is not staged and the callback may continue. When the callback itself throws, nothing it staged is drawn and the error propagates. Nested batches join the outer one.

```javascript
chart.batch(live => {
  for (const sample of samples) live.append(toPeriod(sample), { maximum: 1000 });
  live.replaceSeries({ id: "target", name: "Target", values: targets });
});
```

Each commit draws a fresh SVG and the host keeps one delegated click and keydown listener, so listener and node counts do not grow with the number of updates.

### Sustained-update measurement

`DataRendererSoakBrowserTests` (`Category=Performance`, Chromium) appends one period per update to a two-series line holding 1,000 periods per series, with a selected and a focused mark. A commit is the `append` call plus the style and layout it forces. Every 300 updates it checks the mark count, the newest period, focus and selection, then selects the newest point. Heap, DOM node and listener counts come from the Chromium protocol after a forced collection. By default it runs 1,800 updates back to back; `CFX_SOAK_REALTIME=1` paces them at one per second (30 minutes), `CFX_SOAK_UPDATES` changes the count and `CFX_SOAK_OUTPUT` keeps the JSON result.

```powershell
$env:HFX_TEST_ENABLE_PLAYWRIGHT = '1'; $env:CFX_SOAK_REALTIME = '1'
dotnet test ./ChartForgeX.Tests -c Release --filter "FullyQualifiedName~DataRendererSoakBrowserTests" --logger "console;verbosity=detailed"
```

Measured on 28 November 2026 on an Apple M5 Max with macOS 27.2, .NET 8 and headless Chromium. The machine was shared and heavily loaded: the 1-minute load average was about 430 at the start of the paced run and 260 at the end. These numbers are local evidence for that host, not guarantees.

| Run | Updates | Commit median | Commit p95 | Commit max | JS heap start → end | DOM nodes start → end | Listeners |
| --- | ---: | ---: | ---: | ---: | --- | --- | --- |
| Paced, 1 Hz for 30 minutes | 1,800 | 11.6 ms | 17.6 ms | 64 ms | 1.15 → 1.36 MiB | 8,073 → 8,073 | 2 → 2 |
| Fast-forward, back to back | 1,800 | 10.5 ms | 14.6 ms | 35.9 ms | 1.15 → 1.37 MiB | 8,073 → 8,073 | 2 → 2 |

Every retained-state check passed in both runs: 2,000 marks, the newest period last, focus on a DC2 mark and a selection consistent with `selection()`.

Set `--cfx-color-text`, `--cfx-color-border` and `--cfx-series-1` through `--cfx-series-32` on the host to inherit a page's theme. Labels are inserted as text. Titles expose complete labels when the axis or legend shortens them.

The resource under `Assets/cfx-renderer.js` is a template. Distribute the resolved `Script` or `ScriptFile` output so the browser receives the native policy from the same package version.
