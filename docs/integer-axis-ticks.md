# Whole-number axis ticks

Use integer ticks for counts so an axis ending at one or two items does not show
fractional positions such as 0.2 or 0.5. The renderer chooses positions before
formatting labels, so SVG, HTML and native PNG use the same ticks and grid lines.

```csharp
using ChartForgeX;
using ChartForgeX.Core;

var chart = Chart.Create()
    .WithTitle("Items by queue")
    .WithXLabels("Ready", "Running", "Complete")
    .WithYAxisIntegerTicks()
    .AddBar("Items", ChartPoints.FromValues(1, 2, 1));

chart.SaveSvg("queue-counts.svg");
chart.SavePng("queue-counts.png");
```

For horizontal bars, use `WithXAxisIntegerTicks()` because the value axis is
physically horizontal. `WithSecondaryYAxisIntegerTicks()` configures a secondary
value axis. The same policy is available through
`ConfigureYAxis(axis => axis.WithIntegerTicks())` or `ChartAxis.IntegerTicks`.
Pass `false` to any of these fluent methods to restore ordinary numeric ticks.

Generated linear ticks have whole-number positions and steps of at least one.
Large ranges use coarser steps, such as 200,000 across a million-item range; the
renderer can increase the step when captions need more room. `TickCount` remains
a preferred upper limit rather than a demand to create fractional ticks.

Data values and explicit bounds keep their original precision. With bounds of
0.2 through 2.8, the generated ticks are 1 and 2. A bounded interval containing
only one integer shows one generated tick; one containing no integers shows no
generated ticks. Explicit label positions keep their authored values, including
fractions. Time, logarithmic and symmetric logarithmic scales keep their own tick
policies. Integer ticks are opt-in and leave the default continuous axis unchanged.

The typed [IntegerAxisExamples](../ChartForgeX.Examples/IntegerAxisExamples.cs)
generates vertical and horizontal charts for maximum counts 1–4 and 1,000,000 in
both themes. Run the focused example set with:

```powershell
dotnet run --project ChartForgeX.Examples -c Release -- --integer-axis-only --output ./integer-axis-examples
```

## Sparse category bars

Regular bars use the theme's `MaximumBarWidth` token (64 drawing pixels by
default). One or two categories therefore keep readable bars instead of filling
the plot. Series groups stay centred on the category, with the theme's normal
series gaps. Stacked series share one centred slot. Horizontal bars apply the
same token to their thickness; histograms retain their bin widths and sparklines
retain their compact sampling policy.

```csharp
var sparse = Chart.Create().WithSize(1120, 280)
    .WithXLabels("January", "February")
    .WithYAxisIntegerTicks()
    .WithMaximumBarWidth(48)
    .AddBar("Items", ChartPoints.FromValues(1, 3));
var theme = VisualTheme.Graphite().Configure(tokens => tokens.MaximumBarWidth = 56);
```

`WithMaximumBarWidth(null)` restores the theme token. Widths must be positive
and finite. The token survives theme configuration, typography changes and JSON
round trips (`geometry.maximumBarWidth`); older theme JSON uses the default.
