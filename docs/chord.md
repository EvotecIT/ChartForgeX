# Weighted chord charts

`AddChord` renders directed flows as proportional endpoint slots, node arcs, and curved ribbons. SVG and PNG use one prepared native scene. Static exports are deterministic and script-free.

```csharp
var chart = Chart.Create().AddChord("Transfers", new[] {
    new ChartNode("north", "Support"), new ChartNode("south", "Support"),
    new ChartNode("central", "Central")
}, new[] {
    new ChartFlowLink("north-south", "north", "south", 18),
    new ChartFlowLink("south-north", "south", "north", 7),
    new ChartFlowLink("priority", "north", "south", 4),
    new ChartFlowLink("internal", "central", "central", 3),
    new ChartFlowLink("none", "central", "north", 0)
}).ConfigureChord(options => {
    options.StartAngleDegrees = -120;
    options.SweepAngleDegrees = 300;
    options.NodeGapDegrees = 5;
    options.NodeThicknessRatio = .09;
    options.RibbonOpacity = .45;
    options.DirectionCue = ChartChordDirectionCue.TargetChevron;
    options.LabelContent = ChartChordLabelContent.LabelAndTotals;
});
chart.Series[0].WithNodeState("north", ChartSeriesState.Warning);
```

Each node has an explicit ID and display label. Labels may repeat. Flow IDs remain distinct for parallel and reciprocal links; the renderer does not aggregate them. Cycles and self flows are accepted. A self flow occupies separate outgoing and incoming slots on its own node. Incoming, outgoing, and their combined endpoint totals must remain finite. Invalid identities, references, or aggregates fail before a series is added.

The node arc represents incoming plus outgoing weight, so each flow contributes to two endpoint slots. Authored node order determines clockwise arc order. Outgoing slots come before incoming slots on each arc; flows retain authored order within both sets. No crossing minimizer or automatic reordering runs. The global angular allocation divides by a finite node reference before summing, preserving proportional geometry for subnormal weights and independent huge flows without requiring a finite global raw total.

Flow values are finite and non-negative. Zero flows and nodes without positive endpoints retain source identities and raw values but draw no filled ribbon or arc. Empty and all-zero inputs emit `chord.no-positive-flow`. Positive weights or node thickness that collapse in native geometry emit `chord.precision-collapse`; they receive no minimum-width substitute. Increasing canvas size cannot recover a ratio that underflows the numeric angular allocation.

`ChartOptions.Chord` holds the mutable options configured by `ConfigureChord`:

| Option | Behavior |
| --- | --- |
| `StartAngleDegrees` | Clockwise start; zero points right and -90 points up. Finite values wrap by full turns. |
| `SweepAngleDegrees` | Circular span greater than zero and at most 360 degrees. |
| `NodeGapDegrees` | Gap after each positive node. Preparation rejects gaps that consume the configured span. |
| `NodeThicknessRatio` | Node thickness as a fraction strictly between zero and one of the outer radius. |
| `RibbonOpacity` | Filled ribbon opacity from zero to one. |
| `DirectionCue` | `TargetChevron` points into the target slot; `None` omits the visible cue while retaining directed facts. |
| `LabelContent` | `None`, `Label`, `LabelAndValue`, or `LabelAndTotals`. The value is the combined endpoint total. |

Defaults are a full circle starting at -90 degrees, a 3-degree node gap, 0.06 node thickness, 0.35 ribbon opacity, target chevrons, and labels with combined values. Labels use shared measured fitting outside the circle. Compact output may shorten or omit a label and emits `chord.label-overflow`; full labels and raw totals remain available in node semantics. `ChartSeries.ShowDataLabels = false` hides visible labels.

`ChartSeries.WithNodeState(id, state)` scopes semantic styling to that series. `NodeStates` is read-only. Existing point color, fill-pattern, label, and label-style APIs refer to authored node ordinals. Explicit node or series colors take precedence over semantic fills. Ribbons use the source node's color and pattern. Prepared exports keep their geometry, styles, formatting, and semantics after the mutable model changes.

Node and link groups publish `data-cfx-target-kind`, `data-cfx-target-id`, authored source indexes, and `data-cfx-coordinate-system="polar"`. Links retain the actual source/target IDs and labels, raw `data-cfx-value`, both endpoint angles, and direction. Node groups retain raw incoming/outgoing totals and arc geometry. Scale metadata is `data-cfx-weight-reference` and `data-cfx-normalized-angular-scale`: divide a raw weight by the reference before applying the angular scale. There are no invented Cartesian `point` or `sourcePoint` ordinals.

The HTML adapter consumes these same targets for selection and tooltips. Chart markup's numeric grammar does not support typed relationships; author chord charts through the C# API. Custom ordering strategies, per-flow style overrides, and an independent quantitative color dimension remain outside the current chord options.
