using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneGeometry {
    /// <summary>Finds positive even-odd interior boxes inside source footprints, sweeping local contour edges once.</summary>
    /// <remarks>Input contours already have the consumer's paint precision. Returned boxes are witnesses, never a replacement for native paint queries.</remarks>
    internal static ChartRect?[] FindFillInteriors(IReadOnlyList<List<ChartPoint>> contours, IReadOnlyList<ChartRect> footprints, double minimumRadius) {
        // Transpose so the existing retrace cancellation serves a vertical ray. Sweeping X keeps
        // ordinary dense Cartesian series local instead of rescanning their complete contours per point.
        var transposed = new List<List<ChartPoint>>();
        foreach (var contour in contours) {
            var points = new List<ChartPoint>();
            foreach (var point in contour) points.Add(new ChartPoint(point.Y, point.X));
            transposed.Add(points);
        }
        var sorted = new List<(double X1, double Y1, double X2, double Y2)>(FillEdges(transposed));
        sorted.Sort((left, right) => left.Y1.CompareTo(right.Y1));
        var order = new List<int>();
        for (var index = 0; index < footprints.Count; index++) order.Add(index);
        order.Sort((left, right) => footprints[left].Left.CompareTo(footprints[right].Left));
        var results = new ChartRect?[footprints.Count];
        var active = new List<(double X1, double Y1, double X2, double Y2)>();
        var next = 0;
        foreach (var index in order) {
            var footprint = footprints[index];
            if (footprint.Width <= 0 || footprint.Height <= 0) continue;
            active.RemoveAll(edge => edge.Y2 <= footprint.Left);
            while (next < sorted.Count && sorted[next].Y1 < footprint.Right) active.Add(sorted[next++]);
            var local = active.FindAll(edge => edge.Y1 < footprint.Right && edge.Y2 > footprint.Left);
            var box = FillInterior(local, new ChartRect(footprint.Y, footprint.X, footprint.Height, footprint.Width), minimumRadius);
            if (box.HasValue) results[index] = new ChartRect(box.Value.Y, box.Value.X, box.Value.Height, box.Value.Width);
        }
        return results;
    }

    private static ChartRect? FillInterior(List<(double X1, double Y1, double X2, double Y2)> edges, ChartRect footprint, double minimumRadius) {
        if (edges.Count < 2) return null;
        var levels = new SortedSet<double> { footprint.Top, footprint.Bottom };
        void Add(double y) { if (y > footprint.Top && y < footprint.Bottom) levels.Add(y); }
        foreach (var edge in edges) {
            Add(edge.Y1); Add(edge.Y2);
            if (edge.X1 == edge.X2) continue;
            foreach (var x in new[] { footprint.Left, footprint.Right }) {
                var t = (x - edge.X1) / (edge.X2 - edge.X1);
                if (t > 0 && t < 1) Add(edge.Y1 + t * (edge.Y2 - edge.Y1));
            }
        }
        ChartRect? Scan() {
            var previous = double.NaN;
            var crossings = new List<double>();
            foreach (var level in levels) {
                if (!double.IsNaN(previous) && level > previous) {
                    var y = previous / 2 + level / 2;
                    crossings.Clear();
                    foreach (var edge in edges) if (edge.Y1 < y && edge.Y2 > y)
                        crossings.Add(edge.X1 + (y - edge.Y1) * (edge.X2 - edge.X1) / (edge.Y2 - edge.Y1));
                    crossings.Sort();
                    for (var index = 0; index + 1 < crossings.Count; index += 2) {
                        var left = Math.Max(footprint.Left, crossings[index]);
                        var right = Math.Min(footprint.Right, crossings[index + 1]);
                        if (right <= left) continue;
                        var x = left / 2 + right / 2;
                        var radius = Math.Min((right - left) / 4, (level - previous) / 4);
                        foreach (var edge in edges) {
                            var dx = edge.X2 - edge.X1; var dy = edge.Y2 - edge.Y1;
                            var t = Math.Max(0, Math.Min(1, ((x - edge.X1) * dx + (y - edge.Y1) * dy) / (dx * dx + dy * dy)));
                            var distanceX = x - (edge.X1 + t * dx); var distanceY = y - (edge.Y1 + t * dy);
                            radius = Math.Min(radius, Math.Sqrt(distanceX * distanceX + distanceY * distanceY) / 2);
                        }
                        if (radius >= minimumRadius && radius > 0) return new ChartRect(x - radius, y - radius, radius * 2, radius * 2);
                    }
                }
                previous = level;
            }
            return null;
        }
        var simple = Scan();
        if (simple.HasValue) return simple;
        // A bow-tie can collapse exactly at a simple band's midpoint. Only that unresolved
        // local case needs crossing knots; ordinary bands avoid a pairwise edge pass entirely.
        var local = edges.FindAll(edge => Math.Max(edge.X1, edge.X2) >= footprint.Left && Math.Min(edge.X1, edge.X2) <= footprint.Right);
        for (var first = 0; first < local.Count; first++) for (var second = first + 1; second < local.Count; second++) {
            var left = local[first]; var right = local[second];
            var from = Math.Max(footprint.Top, Math.Max(left.Y1, right.Y1));
            var to = Math.Min(footprint.Bottom, Math.Min(left.Y2, right.Y2));
            if (to <= from) continue;
            var leftSlope = (left.X2 - left.X1) / (left.Y2 - left.Y1);
            var rightSlope = (right.X2 - right.X1) / (right.Y2 - right.Y1);
            if (leftSlope == rightSlope) continue;
            var leftX = left.X1 + (from - left.Y1) * leftSlope;
            var rightX = right.X1 + (from - right.Y1) * rightSlope;
            var y = from + (rightX - leftX) / (leftSlope - rightSlope);
            var x = leftX + (y - from) * leftSlope;
            if (y > from && y < to && x > footprint.Left && x < footprint.Right) Add(y);
        }
        return Scan();
    }
}
