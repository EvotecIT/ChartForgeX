using System;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static readonly double[] EdgeLabelCollisionOffsets = { 20, 36, 54, 76, 102, 136, 176, 220, 280 };

    private static Point AvoidEdgeLabelNodeCollisions(Point candidate, string? label, Point source, Point target, GraphSceneNode? sourceNode, GraphSceneNode? targetNode, double? sourceBoundaryInset, double? targetBoundaryInset) {
        if (string.IsNullOrWhiteSpace(label) || !EdgeLabelIntersectsEndpoint(candidate, label!, source, sourceNode, sourceBoundaryInset) && !EdgeLabelIntersectsEndpoint(candidate, label!, target, targetNode, targetBoundaryInset)) return candidate;

        // A short curved route can pull its control-point midpoint under a card.
        // The geometric midpoint often has enough room between the endpoints.
        var center = new Point((source.X + target.X) / 2, (source.Y + target.Y) / 2 - 7);
        if (!EdgeLabelIntersectsEndpoint(center, label!, source, sourceNode, sourceBoundaryInset) &&
            !EdgeLabelIntersectsEndpoint(center, label!, target, targetNode, targetBoundaryInset)) return center;

        var dx = target.X - source.X;
        var dy = target.Y - source.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        var normalX = distance < 0.001 ? 0 : -dy / distance;
        var normalY = distance < 0.001 ? -1 : dx / distance;
        var preferredSign = normalY > 0 ? -1d : 1d;
        foreach (var offset in EdgeLabelCollisionOffsets) {
            var preferred = new Point(candidate.X + normalX * offset * preferredSign, candidate.Y + normalY * offset * preferredSign);
            if (!EdgeLabelIntersectsEndpoint(preferred, label!, source, sourceNode, sourceBoundaryInset) && !EdgeLabelIntersectsEndpoint(preferred, label!, target, targetNode, targetBoundaryInset)) return preferred;
            var alternate = new Point(candidate.X - normalX * offset * preferredSign, candidate.Y - normalY * offset * preferredSign);
            if (!EdgeLabelIntersectsEndpoint(alternate, label!, source, sourceNode, sourceBoundaryInset) && !EdgeLabelIntersectsEndpoint(alternate, label!, target, targetNode, targetBoundaryInset)) return alternate;
        }

        var finalOffset = EdgeLabelCollisionOffsets[EdgeLabelCollisionOffsets.Length - 1];
        return new Point(candidate.X + normalX * finalOffset * preferredSign, candidate.Y + normalY * finalOffset * preferredSign);
    }

    private static bool EdgeLabelIntersectsEndpoint(Point labelPoint, string label, Point nodePoint, GraphSceneNode? node, double? boundaryInset) {
        var labelHalfWidth = Math.Max(14, EstimatedEdgeLabelWidth(label) / 2);
        const double labelHalfHeight = 10;
        const double collisionGap = 6;
        var labelCenterY = labelPoint.Y - 4;
        NodeCollisionExtents(node, boundaryInset, out var nodeHalfWidth, out var nodeHalfHeight);
        return Math.Abs(labelPoint.X - nodePoint.X) < labelHalfWidth + nodeHalfWidth + collisionGap
            && Math.Abs(labelCenterY - nodePoint.Y) < labelHalfHeight + nodeHalfHeight + collisionGap;
    }

    private static double EstimatedEdgeLabelWidth(string label) {
        var width = 0d;
        foreach (var character in label) {
            width += character switch {
                ' ' => 3.2,
                '.' or ',' or ':' or ';' or '!' or '|' => 3.6,
                'M' or 'W' or '@' or '%' or '&' => 9,
                >= 'A' and <= 'Z' => 7.2,
                >= '0' and <= '9' => 6.2,
                _ => 5.8
            };
        }

        return width + 8;
    }

    private static void NodeCollisionExtents(GraphSceneNode? node, double? boundaryInset, out double halfWidth, out double halfHeight) {
        if (boundaryInset.HasValue) {
            halfWidth = boundaryInset.Value;
            halfHeight = boundaryInset.Value;
            return;
        }

        var size = Math.Max(4, node?.Size ?? 8);
        var shape = EffectiveNodeShape(node);
        if (TryNodeBoundaryExtents(node, shape, size, out halfWidth, out halfHeight)) return;
        halfWidth = size;
        halfHeight = size;
    }
}
