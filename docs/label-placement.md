# Measured label placement

SVG and PNG charts use one final text scene. It measures the resolved family, weight, italic face, variation settings and language with the same font engine that paints PNG glyphs. Native PNG marks retain their raster renderer; the positioned text and its backdrops are painted from the shared scene. A bounded measurement cache and the existing shaped-glyph cache avoid measuring repeated captions again. Register font files when exports must have identical geometry on different machines. Portable estimation is a fallback when no face resolves; topology and canvas also expose explicit `TextMeasurementMode.PortableEstimate` for legacy estimated layout.

Labels are placed by descending priority, with stable input order breaking ties. Each tries its original position and then ordered alternative lanes. Plain labels shorten with an ellipsis if no full-text candidate fits, and drop if shortening also fails. Rich multiline captions and grouped legend items remain intact or drop as a unit. Axis labels retain their tick positions and thin when they collide; an explicitly configured rotation remains in effect. Titles and legends take priority over data and route labels. Donut center lines share the measured budget inside the hole.

Other labels and unrelated marks are obstacles. A label intentionally inside its associated bar, funnel stage, map region or topology node is accepted only when its entire box fits that mark. Halos and label backdrops participate in the footprint; leader strokes do not count as text boxes. SVG records the full value in `data-cfx-label-original`, the result in `data-cfx-label-status`, and the positioned bounds in `data-cfx-label-x`, `-y`, `-width` and `-height`. Shortened and dropped labels retain their value in the associated mark's accessible name and `data-cfx-label-text`; existing value metadata and hover titles remain available.

Imported topology icon artwork stays atomic: text inside an SVG asset keeps its authored coordinates and is painted with the asset. Chart node and edge captions use the placement service. The scene reserves a small measured canvas margin, including when a larger topology viewBox fits a smaller output viewport, to keep displaced captions away from the export edge.

The reusable service is available for callers that build their own label scenes:

```csharp
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

var service = new LabelPlacementService();
var style = new TextStyle {
    Font = FontSpec.FromFamily("Arial, sans-serif"), FontSize = 12
};
var request = new LabelPlacementRequest(
    "Observed: 42", new ChartPoint(80, 40), style,
    new[] {
        new LabelCandidate(0, -20, horizontalAlignment: 0.5),
        new LabelCandidate(14, 0)
    }, priority: 100) {
    HasLeaderLine = true,
    Fallback = LabelFallbackRule.EllipsisThenDrop
};
var placed = service.Place(
    new[] { request }, new ChartRect(0, 0, 320, 180),
    new[] { new LabelObstacle("point", new ChartRect(75, 35, 10, 10)) });
// Inspect IsDropped, Text, Bounds and LeaderEnd before drawing.
```

`Padding` reserves halo or badge space. Optional request `Bounds` constrain a label further than the scene rectangle. `AssociatedMarkId` allows deliberate full containment in the matching obstacle. `Measure` returns font metrics without placing a label. For a fixed font registry, input order and options, results are deterministic.

The `label-placement-*` examples cover bullet targets, funnels, gauges, European routes, Sankey diagrams, topology and composed scorecards in both themes. Their overlap checker re-measures emitted text and compares it against filled and stroked mark contours; intentional full containment is reported separately. Grid panels render at their destination PNG density, so a 2x export preserves the same measured positions with sharper glyphs and marks.
