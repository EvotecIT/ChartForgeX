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

Selection supports click, Enter and Space. Arrow keys, Home and End move between marks. Stable category and series IDs preserve focus across updates. If a focused mark disappears, focus moves to the first remaining mark, or the empty drawing. The selected mark keeps an outline across redraws. `onSelect` receives an immutable `{ categoryId, seriesId, value }` record.

## Layout and lifetime

The renderer measures its host and redraws through `ResizeObserver`. Category band fill, grouped gaps, bar caps, stack separators, palette, line width, marker radius and area opacity come from native ChartForgeX policy when the asset is emitted. Browser conformance tests compare slot bounds against native layout across narrow and wide hosts.

`update` validates a complete descriptor before replacing the accepted drawing. For live data, `append({ categories, values }, { maximum })` adds periods to the accepted chart, with one value per new category for each series named in `values` (other series get `null`), and keeps at most `maximum` periods. `replaceSeries(series)` replaces one series by id, or adds it. Both validate the resulting chart like `update` and keep selection and focus. Host-authored siblings remain in place. Creating another controller for the same host disposes the prior drawing. `destroy` is idempotent and releases the observer, pending animation frame, event listeners and owned SVG. Updates after disposal throw.

Set `--cfx-color-text`, `--cfx-color-border` and `--cfx-series-1` through `--cfx-series-32` on the host to inherit a page's theme. Labels are inserted as text. Titles expose complete labels when the axis or legend shortens them.

The resource under `Assets/cfx-renderer.js` is a template. Distribute the resolved `Script` or `ScriptFile` output so the browser receives the native policy from the same package version.
