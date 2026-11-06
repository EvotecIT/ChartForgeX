using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildHealthRing(TopologyNode node, ChartRect bounds, bool active) {
        if (!node.ShowHealthRing) return;
        var diameter = Math.Min(bounds.Width, bounds.Height);
        var width = Math.Min(3 * _scale, diameter / 8);
        var status = Highlight(Status(node.Status), active);
        using (PinnedState()) {
            _builder.Ellipse(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2,
                Math.Max(0, bounds.Width / 2 - width), Math.Max(0, bounds.Height / 2 - width),
                null, status, width, role: "topology-node-health-ring",
                paint: new VisualScenePaintBinding(stroke: StatusPaint(node.Status, status, HighlightFactor(active))));
        }
    }
}
