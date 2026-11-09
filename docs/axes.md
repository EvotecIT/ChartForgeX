# Axis ticks and grids

Axes choose readable "nice" ticks on their own. These options fix or steer the ticks, add minor
grid lines and style each axis's grid. Configure them on the axis, usually through
`ConfigureXAxis`, `ConfigureYAxis` or `Options.SecondaryYAxis`. Every option paints the same
geometry in SVG, HTML and native PNG, and an axis without them keeps its automatic ticks unchanged.

The gallery's Line page shows each option in both themes and at compact width; the examples come
from [AxisControlRecipe](../ChartForgeX.Examples/Recipes/AxisControlRecipe.cs).

## Fixed step

```csharp
chart.ConfigureXAxis(axis => axis.WithTickStep(6))     // 0, 6, 12, 18, 24
     .ConfigureYAxis(axis => axis.WithTickStep(15));   // 0, 15, 30, …
```

Linear ticks sit on whole multiples of the step. An automatic value axis ends on the next multiple
beyond its data, as it does for automatic steps. On a logarithmic axis the step counts whole powers
of the base, so `WithTickStep(2)` marks 1, 100, 10,000. Time axes take a calendar interval:

```csharp
chart.ConfigureXAxis(axis => axis.WithTimeScale()
    .WithTickStep(new ChartTimeTickInterval(ChartTimeTickUnit.Week, 2)));
```

A time axis rejects a numeric step, other axes reject a calendar step, and a symmetric logarithmic
axis rejects any step, because its spacing is not uniform. These rejections happen when the chart is
prepared. A step that would place more than 1,000 ticks across the axis also throws.

## Explicit values

```csharp
chart.ConfigureYAxis(axis => axis.WithTickValues(0, 20, 50, 80));
```

Ticks appear at exactly these values inside the visible domain. Captions still come from the axis
formatter. Use `Labels` instead when each position needs its own authored text; labels take
precedence over tick values. An axis accepts up to 1,000 values. Duplicates are merged, and
non-finite values throw. A step and explicit values cannot be combined: call `WithAutomaticTicks()`
to clear one before setting the other.

## Tick spacing

```csharp
chart.ConfigureXAxis(axis => axis.WithTickSpacing(minimum: null, maximum: 45))
     .ConfigureYAxis(axis => axis.WithTickSpacing(minimum: 90, maximum: null));
```

Spacing bounds steer the automatic search, in drawing units, on Cartesian axes. The minimum rejects
any denser step. The maximum starts the search at the sparsest nice step that meets it. Captions
still need their room, so a crowded axis can end up wider apart than the maximum. Spacing bounds
only steer automatic ticks, so they cannot be combined with a fixed step or explicit values.

## Minor ticks

```csharp
chart.ConfigureYAxis(axis => axis.WithTickStep(20).WithMinorTicks(4));
```

Each major interval is divided evenly in data units, including the partial intervals at both ends of
the domain. On a logarithmic decade this gives the familiar 2…9 lines. ChartForgeX axes draw no tick
marks, so minor ticks appear as lighter grid lines (`grid-y-minor`, `grid-x-minor`) wherever the
axis draws grid lines. The X axis draws no grid lines by default; call
`ConfigureGrid(grid => grid.WithLines(true))` to show them.

## Axis grid style

```csharp
var teal = ChartColor.FromHex("#2E8B8B");
chart.ConfigureYAxis(axis => axis.ConfigureGrid(grid => grid
    .WithColor(teal).WithOpacity(.55).WithStrokeWidth(1).WithDash(5, 4)
    .WithBands(teal, opacity: .07)));
```

`ChartAxisGridStyle` overrides the chart's `ChartGridLineStyle` for one Cartesian axis: line
visibility, colour, opacity, width, dash and minor-line opacity (half the major opacity by default).
Unset values inherit the chart style. `WithBands` fills every other interval between adjacent major
ticks, starting with the first. The chart's `ShowGrid` switch still hides every grid.

## Value lines and bands

Lines and ranges on an axis value already exist as annotations and plot regions, each with an
optional label:

```csharp
chart.AddHorizontalLine(80, "Latency objective")
     .AddVerticalBand(9, 17, "Business hours", opacity: .08)
     .AddPlotRegion(new(ChartColor.FromHex("#AC9148"), yMinimum: 65, yMaximum: 80, label: "High load", showLabel: true));
```

`AddHorizontalLine`, `AddVerticalLine`, `AddHorizontalBand` and `AddVerticalBand` follow the
physical axes. Plot regions take any combination of X and Y ranges. Both have stable IDs for
interactive scenarios.

## Saved interactive pages

When a saved HTML page zooms, it re-ticks the window with the same step, explicit values, minor ticks
and spacing bounds. Thinned captions keep their grid lines, as they do natively. The page can't
re-project calendar steps, tick policies on a time axis, or grid bands, so zoom declines visibly for
those charts. A connected .NET host prepares every window natively instead. See
[interactivity](interactivity.md).
