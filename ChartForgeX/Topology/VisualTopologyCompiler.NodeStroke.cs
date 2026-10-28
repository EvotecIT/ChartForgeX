using System;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    /// <summary>Resolves logical node-surface stroke once for both native paint and measured content admission.</summary>
    private double NodeSurfaceStrokeWidth(TopologyNode node) => NodeSurfaceStrokeWidth(node, true);

    /// <summary>Fixed shape details inherit the existing theme/selection width, excluding authored contour width.</summary>
    private double NodeSurfaceStrokeWidth(TopologyNode node, bool contourPaint) =>
        contourPaint && node.StrokeWidth.HasValue
            ? Math.Max(node.StrokeWidth.Value, _options.SelectedNodeIds.Contains(node.Id) ? 2.8 * TopologyPreparedNodeBasis.Units(node) : 0)
            : (_options.SelectedNodeIds.Contains(node.Id) ? 2.8 : _context.Theme.AxisStrokeWidth) * TopologyPreparedNodeBasis.Units(node);
}
