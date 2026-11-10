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

## Theme defaults per series kind

A theme can style every series of one kind, the way AG Charts `theme.overrides`
does. Set them in code with `VisualThemeBuilder.ConfigureSeries` inside
`VisualTheme.Configure`, or in the optional `series` object of a
`schemaVersion: 1` theme file loaded with `VisualTheme.FromJson`.
`theme.SeriesDefaults` returns independent copies keyed by kind:

```json
"series": {
  "line": {
    "strokeWidth": 3,
    "curve": "smooth",
    "dash": [6, 3],
    "marker": { "shape": "diamond", "radius": 4.5, "enabled": true, "strokeWidth": 1.5 },
    "dataLabel": { "fontSize": 11, "fontWeight": "600", "color": "#334155" }
  },
  "bar": { "cornerRadius": 10, "dataLabel": { "fontWeight": "700" } }
}
```

```csharp
var theme = VisualTheme.Graphite().Configure(builder => builder.ConfigureSeries(ChartSeriesKind.Line, line => {
    line.StrokeWidth = 3;
    line.Interpolation = ChartInterpolation.Smooth;
    line.ConfigureMarkers(markers => { markers.Shape = ChartMarkerShape.Diamond; markers.Enabled = true; });
}));
```

Keys are camel-case `ChartSeriesKind` names (`line`, `stepLine`, `area`,
`horizontalBar`, `rangeBar` and so on). Curves are `linear`, `smooth` or `step`;
marker shapes are the `ChartMarkerShape` names. An unknown kind, curve or shape
fails, and so does a setting the kind never draws: markers on bars, a curve on a
scatter, or a corner radius on anything other than bar, horizontal bar, range bar
and waterfall marks. Bubbles keep their size scale, so their marker radius is
rejected too. `ToJson` writes the object only when the theme has defaults, so
existing theme files and exports are unchanged.

Precedence, from highest to lowest:

1. An explicit series setting: `StrokeWidth`, `StrokeStyle` (width and dash),
   `Markers` fields, `Interpolation` or a step position, and `DataLabelStyle`
   fields. Each field wins on its own, so a series that only sets a marker radius
   still takes the theme's marker shape.
2. Explicit chart settings that already sit above the theme: chart-level
   `DataLabelStyle` fields and an authored bar corner radius.
3. The theme's defaults for the series kind.
4. The global theme: `SeriesStrokeWidth`, `MarkerRadius`, `BarRadius` and the
   typography scale.
5. The family's built-in default.

Per-kind defaults resolve once, when a chart is prepared, into a detached view of
each affected series. Renderers read ordinary series settings, so SVG, native PNG
and the HTML runtime agree. The authored series is never changed.

These defaults live in the ChartForgeX theme format rather than the generated
HtmlForgeX token document (`VisualTheme.FromPaletteJson`). That document carries
shared brand colours, typography and radii for several products; series-kind
styling is specific to ChartForgeX charts.

`ConfigurabilityExamples.cs` loads
[`series-defaults.theme.json`](../ChartForgeX.Examples/Assets/Themes/series-defaults.theme.json)
and renders `theme-series-defaults-line-*` and `theme-series-defaults-bar-*`.

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
