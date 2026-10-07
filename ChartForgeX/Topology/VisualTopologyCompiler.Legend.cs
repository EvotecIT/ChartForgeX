using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    // The common frame measures and clips this rectangle. No delegate survives preparation.
    private void BuildLegendMarker(TopologyLegendItem item, ChartRect bounds) {
        var color = Color(item.Color, item.Status.HasValue ? Status(item.Status.Value) : _colors.Accent);
        var centerX = bounds.X + bounds.Width / 2; var centerY = bounds.Y + bounds.Height / 2;
        if (item.Kind == TopologyLegendItemKind.Node) {
            var node = new TopologyNode { Kind = item.NodeKind ?? TopologyNodeKind.Generic, Symbol = item.Symbol, IconId = item.IconId };
            if (_options.RequireResolvedIcons && node.IconId != null && TopologyRenderPrimitives.ResolveNodeIcon(node, _options) == null)
                throw new System.InvalidOperationException("Unresolved topology legend icon: " + node.IconId);
            _builder.Rect(bounds, Color(item.BackgroundColor, _colors.Surface), color, _context.Theme.AxisStrokeWidth,
                _context.Theme.BarRadius, "topology-legend-node");
            BuildGlyph(node, centerX, centerY, color, System.Math.Min(bounds.Width, bounds.Height) / 26);
        } else {
            using (PinnedState()) {
                if (item.Kind == TopologyLegendItemKind.Edge) {
                    var edge = new TopologyEdge { Kind = item.EdgeKind ?? TopologyEdgeKind.Generic, LineStyle = item.LineStyle };
                    _builder.Line(bounds.X, centerY, bounds.Right, centerY, color, _context.Theme.SeriesStrokeWidth,
                        "topology-legend-edge", dash: TopologyRenderPrimitives.EffectiveEdgePngDashArray(edge));
                } else _builder.Ellipse(centerX, centerY, bounds.Width / 2, bounds.Height / 2, color, role: "topology-legend-status");
            }
        }
    }
}
