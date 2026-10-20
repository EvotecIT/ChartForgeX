using System;
using System.Collections.Generic;
using ChartForgeX.Interactivity;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static readonly double[] EdgeLabelCollisionOffsets = { 20, 36, 54, 76, 102, 136, 176, 220, 280 };
    private static readonly double[] EdgeLabelPathOffsets = { 0, -12, 12, -24, 24, -36, 36, -48, 48, -60, 60 };
    private static readonly double[] EdgeLabelPathFractions = { 0.45, 0.55, 0.35, 0.65, 0.25, 0.75, 0.15, 0.85 };
    private static readonly double[] EdgeLabelRouteFractions = { 0.45, 0.55, 0.35, 0.65, 0.25, 0.75 };

    private static Point AvoidEdgeLabelNodeCollisions(Point candidate, string? label, Point source, Point target, LabelSpatialIndex obstacles, IReadOnlyList<GraphScenePoint>? route = null, Point? control = null) {
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

        // Prefer clear positions on the painted path to remote normal offsets.
        foreach (var offset in EdgeLabelPathOffsets) {
            foreach (var fraction in EdgeLabelPathFractions) {
                var point = EdgeCaptionPathPoint(source, target, control, fraction, offset);
                if (!EdgeLabelIntersectsObstacles(point, label!, obstacles)) return point;
            }
        }

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

    /// <summary>Finds a baseline beside the clipped line or quadratic curve, with a local normal offset.</summary>
    private static Point EdgeCaptionPathPoint(Point source, Point target, Point? control, double t, double offset) {
        var u = 1 - t;
        var x = control.HasValue ? u * u * source.X + 2 * u * t * control.Value.X + t * t * target.X : source.X + (target.X - source.X) * t;
        var y = control.HasValue ? u * u * source.Y + 2 * u * t * control.Value.Y + t * t * target.Y : source.Y + (target.Y - source.Y) * t;
        var dx = control.HasValue ? 2 * u * (control.Value.X - source.X) + 2 * t * (target.X - control.Value.X) : target.X - source.X;
        var dy = control.HasValue ? 2 * u * (control.Value.Y - source.Y) + 2 * t * (target.Y - control.Value.Y) : target.Y - source.Y;
        var length = Math.Max(0.001, Math.Sqrt(dx * dx + dy * dy));
        return new Point(x - dy / length * offset, y + dx / length * offset - 7);
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
