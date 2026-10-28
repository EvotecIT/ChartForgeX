# Readable defaults: ticks, row labels, units and locale

These are the rules charts follow without configuration. Authored settings (explicit tick counts, label density, value formats, colours) always win.

## Tick density

Automatic numeric axes choose their tick step from the axis length, then keep the densest nice step whose measured captions fit:

- **Value (vertical) axes** of at least 160 px get four to nine ticks, about one per 50 px. Steps come from {1, 2, 2.5, 5} × 10ⁿ.
- **Horizontal numeric axes** show every whole unit on small domains (up to 13 units, for example a year of months). Wider domains get at most one tick per 80 px, so a 0–48 axis no longer prints 25 captions.
- Explicit `TickCount`, `ChartLabelDensity.Dense` or `All`, and integer tick policies keep their own search.

In responsive HTML, tick captions thin at one regular stride from the first caption with at least 1.5 em between neighbours. The last caption stays only when it falls on that stride.

## Category captions

A named category identifies its bar, so a categorical axis keeps every name rather than skipping some:

- A name that doesn't fit wraps to two lines.
- If it still doesn't fit, it is shortened with an ellipsis, and the full name becomes the caption's tooltip.
- A shortened name keeps at least three characters. Short names such as month abbreviations would lose their meaning, so they thin out at a regular stride instead.
- Continuous and time axes thin their ticks as described above.

When many named steps have to fit a phone-width chart, a horizontal orientation usually reads better than shortened names.

## Row labels

Lane labels, lane summaries, group headers and heatmap row labels share one policy:

- They never shrink below 10 px. Long names are truncated with an ellipsis; the complete name stays in the chart's semantic regions, and responsive HTML adds a tooltip.
- They may use the row spacing around a thin band, so a label isn't dropped because the painted band is thinner than a line of text.
- When even 10 px text doesn't fit the row pitch, the label is hidden rather than drawn over its neighbour.

Heatmap cells keep theme rounding to at most 20% of the shorter side, so small cells read as squares rather than dots. `WithHeatmapCellRadius` still sets an exact radius.

## Series colours

Palette-driven series (those without an explicit colour or a state role) take theme palette colours in series order, so the first series is always the theme accent and every chart family starts from the same colour. A series with a state role, such as success or danger, keeps its state colour. Neighbouring series don't repeat a colour until the palette wraps.

Linked dashboards that need a series to keep its colour when others are added, removed or filtered can opt in to identity colours:

```csharp
chart.WithSeriesColorAssignment(ChartSeriesColorAssignment.Identity);
```

Each series with an explicit `ChartSeriesId` then derives its palette slot from that identifier. Two series can share a colour in that mode; `WithColor` sets an exact colour when it matters.

Waterfall changes carry a direction: increases use the positive status colour and decreases the negative one. Opening balances, subtotals and totals are levels rather than changes, so they take the series colour (the accent for the first series) instead of a warning colour.

## Units

```csharp
chart.WithValueUnit("ms");
```

The unit follows values in tooltips, readouts and accessible names ("52.7 ms"). Single symbols such as `%` attach directly ("99.8%"). A value format that already writes the unit keeps a single copy. Axis tick labels stay unit-free; put the unit in the axis title instead.

Today this covers:

- **Cartesian charts:** formatted tooltip values of the marks that capture readouts, for example a point's value or a waterfall step's value, start and end. Accessible names carry the unit on line, bar, area and scatter points. Marks with their own descriptions keep unit-free accessible names for now: bubbles, points that also describe a colour dimension, waterfall steps, ranges and comparisons.
- **Heatmaps and calendar heatmaps:** tooltips and accessible names of numeric cells. Categorical state cells, such as "Up" or "Degraded", name their state and take no unit.

Schedules and pie, gauge, radial, map and hierarchy readouts don't add the unit yet.

## Locale

Charts default to invariant output: ISO dates (`2026-10-27`, `14:00`) and `.` as the decimal separator. To localise dates and times:

```csharp
chart.WithCulture(CultureInfo.GetCultureInfo("pl-PL"));
```

- Day labels use the culture's short month-day form ("27 paź", "27. Okt.", "Oct 27"). The first day of a year and ranges that cross years include the year.
- Times use the culture's short time pattern; full instants in tooltips and accessible names use its short date and time.
- `WithCulture(CultureInfo.InvariantCulture)` restores ISO dates.

Numbers keep their value format. Pass the culture there too, for example `WithValueFormat(ChartValueFormat.Number("0.#", culture))`. A single axis can also be localised with `ChartAxis.Culture`.

## Accessible names

Points on a time axis are announced from their formatted parts: series, the full instant and the value, for example "dc-03, 2026-10-27 20:20 UTC, 52.7 ms" (or "27.10.2026 20:20 UTC" with a culture). OLE date numbers are no longer part of the accessible name. An authored or compacted caption keeps the exact value beside it ("12.3k (12345.68)"). Points on numeric axes keep their exact round-trip coordinates. The raw values always remain in `data-cfx-x` and `data-cfx-y`.

Readouts in the interactive reveal layer are separated for screen readers, so several readouts are announced as "dc-03 60.8; dc-02 44.1" rather than run together.
