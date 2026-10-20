using System;
using System.Collections.Generic;
using ChartForgeX.Interactivity;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static readonly double[] EdgeLabelCollisionOffsets = { 20, 36, 54, 76, 102, 136, 176, 220, 280 };
    private static readonly double[] EdgeLabelRouteFractions = { 0.45, 0.55, 0.35, 0.65, 0.25, 0.75 };

    private static Point AvoidEdgeLabelNodeCollisions(Point candidate, string? label, Point source, Point target, LabelSpatialIndex obstacles, IReadOnlyList<GraphScenePoint>? route = null) {
        if (string.IsNullOrWhiteSpace(label) || !EdgeLabelIntersectsObstacles(candidate, label!, obstacles)) return candidate;

        if (route is { Count: > 1 }) {
            // Keep a routed edge's label on its rendered path even when the route
            // passes beneath an endpoint card. A straight midpoint can be remote.
            foreach (var fraction in EdgeLabelRouteFractions) {
                var routed = PolylinePointAt(route, fraction, -7);
                if (!EdgeLabelIntersectsObstacles(routed, label!, obstacles)) return routed;
            }

            return candidate;
        }

        // A short curved route can pull its control-point midpoint under a card.
        // The geometric midpoint often has enough room between the endpoints.
        var center = new Point((source.X + target.X) / 2, (source.Y + target.Y) / 2 - 7);
        if (!EdgeLabelIntersectsObstacles(center, label!, obstacles)) return center;

        var dx = target.X - source.X;
        var dy = target.Y - source.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        var normalX = distance < 0.001 ? 0 : -dy / distance;
        var normalY = distance < 0.001 ? -1 : dx / distance;
        var preferredSign = normalY > 0 ? -1d : 1d;
        foreach (var offset in EdgeLabelCollisionOffsets) {
            var preferred = new Point(candidate.X + normalX * offset * preferredSign, candidate.Y + normalY * offset * preferredSign);
            if (!EdgeLabelIntersectsObstacles(preferred, label!, obstacles)) return preferred;
            var alternate = new Point(candidate.X - normalX * offset * preferredSign, candidate.Y - normalY * offset * preferredSign);
            if (!EdgeLabelIntersectsObstacles(alternate, label!, obstacles)) return alternate;
        }

        var finalOffset = EdgeLabelCollisionOffsets[EdgeLabelCollisionOffsets.Length - 1];
        return new Point(candidate.X + normalX * finalOffset * preferredSign, candidate.Y + normalY * finalOffset * preferredSign);
    }

    private static bool EdgeLabelIntersectsObstacles(Point labelPoint, string label, LabelSpatialIndex obstacles) {
        var labelHalfWidth = Math.Max(14, EstimatedEdgeLabelWidth(label) / 2);
        return obstacles.Intersects(new ChartRect(labelPoint.X - labelHalfWidth, labelPoint.Y - 14, labelHalfWidth * 2, 20), null);
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

}
