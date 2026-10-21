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
