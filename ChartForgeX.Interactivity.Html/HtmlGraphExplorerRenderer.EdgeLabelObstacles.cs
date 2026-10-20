using System;
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
            if (IsCardNode(node) || node.Shape is GraphNodeShape.Image or GraphNodeShape.RectangularImage or GraphNodeShape.Text) {
                var extents = MeasurePreparedNodeExtents(node, includeLabels: true, new PreparedNodeExtents(halfWidth, halfHeight));
                Add(0, 0, extents.HalfWidth, extents.HalfHeight);
                continue;
            }
            // Painted details occupy separate rows below ordinary bodies. The
            // symmetric layout envelope also covers empty space above/between them.
            Add(0, 0, halfWidth + 2, halfHeight + 2);
            if (!string.IsNullOrWhiteSpace(node.Label)) Add(0, size + 15,
                Math.Max(24, Math.Max(EstimatedCardTextWidth(node.Label, false) / 2 + 6,
                    string.IsNullOrWhiteSpace(node.Style.LabelBackgroundColor) ? 0 : node.Label.Length * 3.8)), 11);
            if (!string.IsNullOrWhiteSpace(node.SecondaryLabel))
                Add(0, size + 31, EstimatedCardTextWidth(node.SecondaryLabel!, true) / 2 + 6, 10);
            if (!string.IsNullOrWhiteSpace(node.BadgeText)) {
                var badge = NodeBadgeBounds(node, size);
                Add(badge.Left + badge.Width / 2, badge.Top + 8, badge.Width / 2 + 1, 9);
            }
            if (!string.IsNullOrWhiteSpace(node.Status) && !string.Equals(node.Status, "unknown", StringComparison.OrdinalIgnoreCase))
                Add(-size * .8, -size * .8, Math.Min(4.5, Math.Max(1.35, size * .28)) + 1, Math.Min(4.5, Math.Max(1.35, size * .28)) + 1);

            void Add(double x, double y, double radiusX, double radiusY) => obstacles.Add(
                new ChartRect(point.X + x - radiusX, point.Y + y - radiusY, radiusX * 2, radiusY * 2), node.Id);
        }
        return obstacles;
    }
}
