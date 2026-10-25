using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyInterpolationGeometry {
    /// <summary>Prepares orthogonal control corners once before the selected standard interpolation algorithm.</summary>
    /// <remarks>This changes numeric controls, not the resolved route, and is separate from quadratic Rounded interpolation.</remarks>
    private static IReadOnlyList<ChartPoint> BlendCorners(IReadOnlyList<ChartPoint> points, double? length) {
        if (!length.HasValue || length.Value == 0) return points;
        TopologyModelGuards.NonNegativeFinite(length.Value, nameof(length));
        var distance = length.Value;
        var result = new List<ChartPoint>(points.Count);
        for (var index = 0; index < points.Count; index++) {
            if (result.Count >= 10000) throw new ArgumentException("Blended interpolation controls exceed 10000 vertices.", nameof(points));
            if (index == 0 || index == points.Count - 1) { result.Add(points[index]); continue; }
            var before = points[index - 1]; var corner = points[index]; var after = points[index + 1];
            var vertical = before.X == corner.X && corner.Y == after.Y;
            var horizontal = before.Y == corner.Y && corner.X == after.X;
            if ((!vertical && !horizontal) || Distance(before, corner) <= distance || Distance(corner, after) <= distance) {
                result.Add(corner); continue;
            }
            var incoming = Mix(corner, before, distance / Distance(before, corner));
            var outgoing = Mix(corner, after, distance / Distance(corner, after));
            var middle = corner;
            if (Math.Abs(after.X - before.X) > distance * 2 && Math.Abs(after.Y - before.Y) >= distance * 2) {
                var x = outgoing.X - incoming.X; var y = outgoing.Y - incoming.Y;
                var diagonal = 2 * Math.Sqrt(2) * (distance / 5);
                middle = vertical
                    ? new ChartPoint(incoming.X + (x < 0 ? -distance + diagonal : distance - diagonal), incoming.Y + (y < 0 ? -diagonal : diagonal))
                    : new ChartPoint(incoming.X + (x < 0 ? -diagonal : diagonal), incoming.Y + (y < 0 ? -distance + diagonal : distance - diagonal));
            }
            if (result.Count > 9997) throw new ArgumentException("Blended interpolation controls exceed 10000 vertices.", nameof(points));
            result.Add(incoming); result.Add(middle); result.Add(outgoing);
        }
        return result;
    }
}
