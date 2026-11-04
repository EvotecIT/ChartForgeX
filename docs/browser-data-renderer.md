# Browser data charts

`HtmlDataChartAssets.ScriptFile` emits a standalone, content-addressed browser renderer. Load it when a reader first opens a data chart. It includes the native Graphite surface policy and palette and has no runtime package dependencies.

This entry point serves hosts that obtain raw numeric data in the browser, such as a grid selection or a pivot result. Charts authored in .NET use the prepared chart interaction adapter.

```csharp
var asset = HtmlDataChartAssets.ScriptFile;
File.WriteAllText(asset.FileName, asset.Content);
```

```javascript
const lifetime = new AbortController();
const chart = ChartForgeXRenderer.create(document.getElementById("chart"), {
  type: "column",
  label: "Orders by region",
  categories: [{ id: "north", label: "North" }, { id: "south", label: "South" }],
  series: [{ id: "orders", name: "Orders", values: [12, 8] }]
}, {
  signal: lifetime.signal,
  onSelect(mark) { console.log(mark.categoryId, mark.seriesId, mark.value); }
});
chart.update(nextDescriptor);
lifetime.abort(); // Equivalent to chart.destroy().
```

## Data contract

The supported types are `line`, `area`, `bar` (horizontal), `column`, `stacked` (vertical), `win-loss` and `bullet`. Bullet accepts a measure series and an optional target series. Win/loss draws the sign and returns the original numeric value when selected.

Each category has a unique nonempty `id` and a visible `label`. Each series has a unique nonempty `id`, a visible `name` and exactly one finite number or `null` per category. Null observations leave gaps in lines and areas. Zero observations retain zero ink and have a separate pointer target when selection is enabled. Strings are limited to 2,000 characters, categories to 10,000 and series to 32. Sparse arrays and numeric domains whose span overflows are rejected.

For line and area charts, optional ISO date strings in category `instant` fields give proportional time spacing. Every category must then have a valid instant, strictly increasing in the supplied order. Other chart types use category bands. `compact: true` omits headings, legends and axes for chart cells.

Selection supports click, Enter and Space. Arrow keys, Home and End move between marks. Stable category and series IDs preserve focus across updates. If a focused mark disappears, focus moves to the first remaining mark, or the empty drawing. The selected mark keeps an outline across redraws. `onSelect` receives an immutable `{ categoryId, seriesId, value }` record.

## Layout and lifetime

The renderer measures its host and redraws through `ResizeObserver`. Category band fill, grouped gaps, bar caps, stack separators, palette, line width, marker radius and area opacity come from native ChartForgeX policy when the asset is emitted. Browser conformance tests compare slot bounds against native layout across narrow and wide hosts.

`update` validates a complete descriptor before replacing the accepted drawing. Host-authored siblings remain in place. Creating another controller for the same host disposes the prior drawing. `destroy` is idempotent and releases the observer, pending animation frame, event listeners and owned SVG. Updates after disposal throw.

Set `--cfx-color-text`, `--cfx-color-border` and `--cfx-series-1` through `--cfx-series-32` on the host to inherit a page's theme. Labels are inserted as text. Titles expose complete labels when the axis or legend shortens them.

The resource under `Assets/cfx-renderer.js` is a template. Distribute the resolved `Script` or `ScriptFile` output so the browser receives the native policy from the same package version.
