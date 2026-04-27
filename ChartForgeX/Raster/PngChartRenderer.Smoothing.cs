using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static List<ChartPoint> MapSeriesPathPoints(ChartSeries series, ChartMapper map) {
        var mapped = new List<ChartPoint>(series.Points.Count);
        foreach (var point in series.Points) mapped.Add(new ChartPoint(map.X(point.X), map.Y(point.Y)));
        if (!series.Smooth || mapped.Count < 3) return mapped;

        var sampled = new List<ChartPoint>((mapped.Count - 1) * 10 + 1) { mapped[0] };
        for (var i = 0; i < mapped.Count - 1; i++) {
            var p0 = mapped[Math.Max(0, i - 1)];
            var p1 = mapped[i];
            var p2 = mapped[i + 1];
            var p3 = mapped[Math.Min(mapped.Count - 1, i + 2)];
            var c1 = new ChartPoint(p1.X + (p2.X - p0.X) / 6, p1.Y + (p2.Y - p0.Y) / 6);
            var c2 = new ChartPoint(p2.X - (p3.X - p1.X) / 6, p2.Y - (p3.Y - p1.Y) / 6);
            var steps = Math.Max(6, (int)Math.Ceiling(Math.Abs(p2.X - p1.X) / 18.0));
            for (var step = 1; step <= steps; step++) {
                var t = step / (double)steps;
                sampled.Add(CubicPoint(p1, c1, c2, p2, t));
            }
        }

        return sampled;
    }

    private static ChartPoint CubicPoint(ChartPoint p0, ChartPoint c1, ChartPoint c2, ChartPoint p1, double t) {
        var inverse = 1 - t;
        var a = inverse * inverse * inverse;
        var b = 3 * inverse * inverse * t;
        var c = 3 * inverse * t * t;
        var d = t * t * t;
        return new ChartPoint(
            p0.X * a + c1.X * b + c2.X * c + p1.X * d,
            p0.Y * a + c1.Y * b + c2.Y * c + p1.Y * d);
    }
}
