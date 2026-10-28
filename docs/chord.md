---
description: "Build weighted chord charts with directed flows, proportional ribbons, complete source facts, semantic node state and native SVG and PNG output."
---

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

The node arc represents incoming plus outgoing weight, so each flow contributes to two endpoint slots. By default, authored node order determines clockwise arc order. Outgoing slots come before incoming slots on each arc; flows retain authored order within both sets. Explicit `Ordering` changes display positions independently of that source order. No crossing minimizer or automatic reordering runs. The global angular allocation divides by a finite node reference before summing, preserving proportional geometry for subnormal weights and independent huge flows without requiring a finite global raw total.

Flow values are finite and non-negative. Zero flows and nodes without positive endpoints retain source identities and raw values but draw no filled ribbon or arc. Empty and all-zero inputs emit `chord.no-positive-flow`. Positive weights or node thickness that collapse in native geometry emit `chord.precision-collapse`; they receive no minimum-width substitute. Increasing canvas size cannot recover a ratio that underflows the numeric angular allocation.

`ChartOptions.Chord` holds the mutable options configured by `ConfigureChord`:

| Option | Behavior |
| --- | --- |
| `StartAngleDegrees` | Clockwise start; zero points right and -90 points up. Finite values wrap by full turns. |
| `SweepAngleDegrees` | Circular span greater than zero and at most 360 degrees. |
| `NodeGapDegrees` | Gap after each positive node. Preparation rejects gaps that consume the configured span. |
| `NodeThicknessRatio` | Node thickness as a fraction strictly between zero and one of the outer radius. |
| `RibbonOpacity` | Filled ribbon opacity from zero to one. |
| `Ordering` | Immutable optional complete node, outgoing-flow and incoming-flow ID permutations; null uses source order. |
| `RibbonTension` | Zero makes straight connectors, one pulls their controls to the center; default .8 preserves the existing controls at 20 percent of endpoint radius. |
| `DirectionCue` | `TargetChevron` points into the target slot; `None` omits the visible cue while retaining directed facts. |
| `LabelContent` | `None`, `Label`, `LabelAndValue`, or `LabelAndTotals`. The value is the combined endpoint total. |

Defaults are a full circle starting at -90 degrees, a 3-degree node gap, 0.06 node thickness, 0.35 ribbon opacity, target chevrons, and labels with combined values. Labels use shared measured fitting outside the circle. Compact output may shorten or omit a label and emits `chord.label-overflow`; full labels and raw totals remain available in node semantics. `ChartSeries.ShowDataLabels = false` hides visible labels.

`ChartSeries.WithNodeState(id, state)` scopes semantic styling to that series. `NodeStates` is read-only. Existing point color, fill-pattern, label, and label-style APIs refer to authored node ordinals. Ribbons inherit the source node's color, pattern and semantic state. `WithFlowStyle(id, new ChartFlowStyle(...))` overrides one authored flow's fill, opacity, pattern or outline; `WithFlowState(id, state)` overrides its semantic state. Explicit colors take precedence over semantic fills. Null removes either flow override; an explicit state of `None` suppresses inheritance. See [shared flow paint conventions](v2/api-conventions.md#flow-paint-and-semantic-state) for precedence and outline defaults. Prepared exports keep their geometry, styles, formatting, and semantics after the mutable model changes.

Node and link groups publish `data-cfx-target-kind`, `data-cfx-target-id`, authored source indexes, and `data-cfx-coordinate-system="polar"`. Links retain the actual source/target IDs and labels, raw `data-cfx-value`, both endpoint angles, and direction. Node groups retain raw incoming/outgoing totals and arc geometry. Scale metadata is `data-cfx-weight-reference` and `data-cfx-normalized-angular-scale`: divide a raw weight by the reference before applying the angular scale. There are no invented Cartesian `point` or `sourcePoint` ordinals.

The HTML adapter consumes these same targets for selection and tooltips. Display ordering never changes semantic or keyboard source order. Node-indexed colors/styles retain their authored ordinals; flow styles remain keyed by stable IDs. Existing independent quantitative flow colors and outline dashes are unaffected.

## Display order and tension

```csharp
chart.ConfigureChord(options => {
    options.Ordering = new ChartChordOrdering(
        nodeIds: new[] { "central", "north", "south" },
        outgoingFlowIds: new[] { "priority", "north-south", "south-north", "internal", "none" },
        incomingFlowIds: new[] { "north-south", "priority", "internal", "south-north", "none" });
    options.RibbonTension = .55;
});
```

Each nonnull sequence is copied and must include every corresponding captured ID exactly once, including isolated nodes and zero flows. Blank/duplicate IDs reject at construction; missing/unknown captured IDs reject at preparation. Incoming and outgoing orders are independent: each sequence controls only slots at that endpoint, and a self flow still receives two slots. Null sequences use source order. Replacing flow data with different IDs requires a matching new ordering or null reset; no removed ID is silently ignored. The immutable prepared scene remains detached after input, options or model edits.

Tension affects only the two connecting cubic segments of each ribbon. It leaves endpoint angles, node arcs, source values, direction cues and styles unchanged. It is not a crossing optimizer. The complete [Chord depth recipe](../ChartForgeX.Examples/Recipes/ChordDepthRecipe.cs) provides source-order, explicit-order, straight and center-pulled compact/wide variants in both themes.

## Typed chart markup

```chartforgex chart v1
id transfers
series Transfers
type chord
node north "Support"
node south "Support"
flow primary north south 18 colorValue=-10
flow reverse south north 7
flow internal north north 3
flow none south north 0
option chordNodeOrder south north
option chordOutgoingFlowOrder internal primary reverse none
option chordIncomingFlowOrder reverse internal primary none
option chordRibbonTension .55
```

`nodes:` tables use `id` and `label`; `flows:` tables use `id`, `source`, `target`, `value` and optional `colorValue`, followed by `end`. Declarations may reference nodes declared later in the same block. Duplicate/unknown IDs, invalid weights and malformed order/options produce source-located diagnostics without a partial chart. These are the existing `ChartNode`/`ChartFlowLink` facts and native Chord owner; numeric `values`, categories and per-series numeric grammar cannot invent relationships. Empty `type chord` is a valid native no-data model. Static fence exports use the same script-free prepared SVG and PNG scene.
