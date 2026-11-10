# Legend entries and keys

A chart's legend lists one entry per series, slice or category and places itself in one of eight
positions (`WithLegendPosition`) inside a height and row budget (`WithLegendBudget`). The options
here add entries that do not come from a series, change the key drawn beside each entry, and rewrite
entry text. They live on `chart.Options.Legend` (`ChartLegendOptions`) and apply to every chart
family. SVG, HTML and native PNG paint the same keys and text.

The gallery's Line page shows each option in both themes and at compact width; the examples come
from [LegendControlRecipe](../ChartForgeX.Examples/Recipes/LegendControlRecipe.cs).

## Custom entries

```csharp
var target = ChartColor.FromHex("#D97706");
chart.AddHorizontalBand(1_100_000, 1_300_000, color: target, opacity: .12)
     .AddHorizontalLine(1_200_000, color: target)
     .AddLegendItem(ChartLegendItem.Line("Target 1.2M", target, ChartLegendItem.ReferenceDash, 1.6))
     .AddLegendItem(ChartLegendItem.Band("Tolerance ±100k", target, .12));
```

`ChartLegendItem` is immutable and has three keys:

| Factory | Key | Typical use |
|---|---|---|
| `Line(label, color, dash, strokeWidth)` | A short line, solid unless a dash is given. `ReferenceDash` matches the dash chart reference lines draw by default. | Target, threshold or reference line |
| `Band(label, color, fillOpacity)` | A rounded square at that opacity, outlined in the full colour when translucent. | Threshold band, target range |
| `Marker(label, color, shape)` | A `ChartMarkerShape`. | Annotation or event marker |

`WithKeySize(size)` gives one item its own key size. Items follow the series entries in the order
they were added, wrap and overflow like any other entry, and count towards the legend: a single
series with one custom item shows its legend without `WithLegend(true)`.

An item is a description of the chart, not of the data, so it carries no series identity. Its entry
group has `data-cfx-legend-custom="true"` and an empty `data-cfx-series-key`. The browser runtime
never turns it into a visibility toggle, and its compact tray lists it as plain text after the
series buttons. Items do not follow the annotation they describe: when you remove the annotation,
remove the item too.

## Keys

```csharp
chart.WithLegendKey(ChartLegendKeyShape.Marker, 12);
```

`KeyShape` sets the key every series entry draws:

- `Auto` (the default) keeps each family's own key: a short line for line series, a dot for points,
  a rounded square for filled marks, with the series' marker once one is configured.
- `Square` draws a rounded square with the entry's fill pattern.
- `Circle` draws a filled dot.
- `Line` draws a short line at the series' stroke width, dashed when the series' `StrokeStyle` has a dash.
- `Marker` draws the series' own marker shape, including a custom marker path.

`KeySize` (4 through 24 chart pixels, default 10) sizes every key; a line key is 1.4 times as long
and grows to show a dash, its gap and a second dash. Rows grow to fit a larger key, and all keys stay
centred on their label line. Categorical state keys (state timelines and heatmap states) keep their
state swatches.

## Entry text

```csharp
chart.WithValueFormat(ChartValueFormat.Compact())
     .WithLegendFormatter(item => item.Series == null ? null
         : item.Label + " — " + item.FormatValue(item.Total) + " total");
```

The formatter receives a `ChartLegendItemContext` for every entry, custom items included:

| Member | Meaning |
|---|---|
| `Label` | The text the legend would show without a formatter |
| `Series`, `SeriesIndex` | The series the entry describes, or null and -1 |
| `PointIndex` | The observation for a point legend or pie slice |
| `Item` | The custom item, for custom entries |
| `FormattedValue` | The value column a part-to-whole legend already shows |
| `Values`, `Total` | The entry's finite Y values and their sum |
| `FormatValue(value)` | The chart's value format, as used by data labels and the value axis |

Returning null keeps the default label. The result becomes the entry's visible text, its accessible
name and its descriptive region; value and percentage columns are unchanged. The formatter runs
whenever the legend is measured or painted, so it must be a pure function of its context.

Native visibility reflow keeps custom items and keys, but it rejects a legend formatter, like the
other formatting callbacks it cannot capture.
