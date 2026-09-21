using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartPointSegments {
    internal static IEnumerable<IReadOnlyList<ChartPoint>> Split(IReadOnlyList<ChartPoint> points) {
        var start = 0;
        for (var index = 1; index <= points.Count; index++) {
            if (index < points.Count && !points[index].BreakBefore) continue;
            if (start == 0 && index == points.Count) { yield return points; yield break; }
            var segment = new ChartPoint[index - start];
            for (var offset = 0; offset < segment.Length; offset++) segment[offset] = points[start + offset];
            yield return segment;
            start = index;
        }
    }
}
