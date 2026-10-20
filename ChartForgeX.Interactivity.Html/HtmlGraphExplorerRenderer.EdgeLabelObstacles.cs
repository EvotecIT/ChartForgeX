using System.Collections.Generic;
using ChartForgeX.Interactivity;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    /// <summary>Measures rendered node obstacles once per output, including collapsed aggregate bodies.</summary>
    private static LabelSpatialIndex BuildEdgeLabelObstacles(GraphScene scene, IReadOnlyDictionary<string, Point> positions,
        IReadOnlyDictionary<string, Point> collapsedNodePositions, IReadOnlyDictionary<string, double> collapsedNodeRadii) {
        var obstacles = new LabelSpatialIndex(6);
        var aggregates = new HashSet<(double X, double Y, double Radius)>();
        foreach (var node in scene.Nodes) {
            if (collapsedNodePositions.TryGetValue(node.Id, out var aggregate) && collapsedNodeRadii.TryGetValue(node.Id, out var radius)) {
                // A visible aggregate can contain intrinsically hidden members.
                if (aggregates.Add((aggregate.X, aggregate.Y, radius)))
                    obstacles.Add(new ChartRect(aggregate.X - radius, aggregate.Y - radius, radius * 2, radius * 2), node.Id);
                continue;
            }
            if (node.Hidden || !positions.TryGetValue(node.Id, out var point)) continue;
            var size = SafeNodeSize(node);
            TryNodeBoundaryExtents(node, EffectiveNodeShape(node), size, out var halfWidth, out var halfHeight);
            var extents = MeasurePreparedNodeExtents(node, includeLabels: true, new PreparedNodeExtents(halfWidth, halfHeight));
            obstacles.Add(new ChartRect(point.X - extents.HalfWidth, point.Y - extents.HalfHeight,
                extents.HalfWidth * 2, extents.HalfHeight * 2), node.Id);
        }
        return obstacles;
    }
}
