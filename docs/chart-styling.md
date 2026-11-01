# Chart paint and formatters

Style series with `WithFill` and `WithStrokeStyle`. Use `WithItemStyler<TDatum>`
when paint depends on a complete observation. Labels and tooltip text use
`WithDataLabelFormatter` and `WithTooltipFormatter` on the same series handle.
All callbacks run during `Prepare`; SVG and native PNG exports reuse the captured
scene without invoking application code or performing external I/O.

```csharp
var sales = ChartSeries.CreateBar(new ChartSeriesId("sales"), "Sales",
    ChartPoints.FromValues(62, 78, 71, 91));
sales.WithFill(ChartFill.LinearGradient(new ChartPoint(0, 0), new ChartPoint(0, 1),
    new ChartGradientStop(0, ChartColor.FromHex("#8DC7F3")),
    new ChartGradientStop(1, ChartColor.FromHex("#3979C6"))));
sales.WithItemStyler<ChartPoint>(item => item.Datum.Y >= 90
    ? new ChartItemStyle(color: ChartColor.FromHex("#CC7850"),
        stroke: new ChartStrokeStyle(width: 2, dash: new[] { 6d, 3d }))
    : null);
sales.WithDataLabelFormatter(item => item.Value?.ToString("0"));
sales.WithTooltipFormatter(item => $"{item.Category}: {item.Value:0} orders");
var chart = Chart.Create().WithXLabels("Jan", "Feb", "Mar", "Apr")
    .WithDataLabels(true).AddSeries(sales);
```

## Observation contexts and precedence

The generic datum type is the family's public observation type. Numeric series
use `ChartPoint`; bubbles use `ChartBubble`; ranges use `ChartRangeBand`;
interval bars use `ChartInterval`; hierarchies use `ChartHierarchyItem`;
Tree, Sankey and Chord nodes use `ChartNode`. Specialized financial, map,
calendar, schedule and progress families retain their corresponding typed data.
Categorical heatmaps use `ChartHeatmapCell?`, including absent cells.
The callback rejects a mismatched type when installed, even on an empty series.
Sankey/Chord links use `ChartFlowLink` and Tree branches use `ChartTreeLink`.
Configure node and link callbacks independently with the same generic method;
both remain installed. Link indexes address the authored link collection.

`Index` is the authored display observation ordinal, before sorting or clipping.
Derived histogram bins and fitted endpoints are display observations rather than
the original input samples. Aggregated marks retain aggregate presentation;
they do not borrow an arbitrary contributor's callback result.
`Series` contains immutable identity, display name, kind and semantic state;
`State` includes the effective node or flow override for relationship data.
`IsCurrent`, `IsSelected` and `IsHighlighted` describe static `ChartMarkOptions`
state. Interactive hover stays in the optional HTML adapter.

The family/theme default feeds series configuration, ordinal `WithPoint*`
overrides, then the datum callback. Null callback fields inherit the previous
level. `ChartItemStyle.Color` chooses a solid base color; `Fill` chooses an
authored fill. Per-item fill overrides the series fill. Marker settings are
copied when a style is constructed. Bubble size continues to use its quantitative
size scale; a callback cannot replace it with a marker radius.

Keep callbacks pure and captured configuration immutable. They are evaluated once
per authored display observation for each preparation. Exceptions propagate
without installing resolved values on the authored series. Reprepare after
changing data, callbacks or themes; an existing prepared result stays detached.

## Fills and strokes

`ChartFill.LinearGradient` and `ChartFill.RadialGradient` use ordered
`ChartGradientStop` values. Coordinates are fractions of each mark's bounds;
radial radius is a fraction of its width and height. Stops include alpha.
`ChartFill.Image(rgbaImage)` stretches a detached pixel snapshot into the mark;
`ChartFill.Pattern(rgbaImage, tileWidth, tileHeight)` repeats it at a logical tile
size. Both embed their pixels in SVG and share the native PNG clipping geometry.
No image URL, file access or browser installation is required at render time.

`ChartStrokeStyle` supplies color, width, opacity and dash lengths for connected
lines, area edges, comparison stems, error bars and box outlines. For example,
`forecast.WithStrokeStyle(new ChartStrokeStyle(width: 2.5, dash: new[] { 7d, 4d }))`
creates a dashed forecast. Stroke style width overrides `StrokeWidth`, then the
family/theme width. Radar and polar family outlines retain their more specific
precedence. Marker outlines remain independent.

## Labels and tooltip text

`ChartValueContext` exposes the scalar `Value`, category caption, series facts,
index and `Percent` where a total or declared scale has a meaningful fraction.
One is 100 percent. Compound data without one scalar reading has a null value;
use `GetDatum<TDatum>()` for its complete fields. A null formatter result retains
the existing authored label or family default; an empty label suppresses its
caption. Formatting does not enable hidden labels. Tooltip text is captured as
plain text in the SVG and its semantic metadata for the optional HTML adapter.
Specialized endpoint/statistic captions retain their distinct measured fields.

Run the examples with `--configurability-only` to write compact/wide, light/dark
SVG, PNG and HTML companions. The complete example is
[`ConfigurabilityExamples.cs`](../ChartForgeX.Examples/ConfigurabilityExamples.cs).
