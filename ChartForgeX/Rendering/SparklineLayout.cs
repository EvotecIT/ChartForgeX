using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Projects shared sample slots without dropping zeroes, moving missing slots or joining gap segments.</summary>
internal static class SparklineLayout {
    internal static ChartPoint[] Project(SparklineData data, ChartRect plot, bool centerSingle = false) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        var points = data.ToPoints();
        for (var index = 0; index < points.Length; index++) {
            var point = points[index];
            var ratio = data.Values.Count == 1 ? centerSingle ? 0.5 : 0 : (point.X - 1) / (data.Values.Count - 1);
            points[index] = new ChartPoint(plot.Left + plot.Width * ratio, plot.Bottom - plot.Height * data.Ratio(point.Y), point.BreakBefore);
        }
        return points;
    }

    internal static ChartPoint[] Area(IReadOnlyList<ChartPoint> segment, double baseline) {
        if (segment.Count == 0) return Array.Empty<ChartPoint>();
        var area = new ChartPoint[segment.Count + 2];
        area[0] = new ChartPoint(segment[0].X, baseline);
        for (var index = 0; index < segment.Count; index++) area[index + 1] = segment[index];
        area[area.Length - 1] = new ChartPoint(segment[segment.Count - 1].X, baseline);
        return area;
    }
}
