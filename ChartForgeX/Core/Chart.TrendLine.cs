using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a least-squares trend line computed from the supplied points.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The source points used to compute the trend line.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddTrendLine(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) {
        var materialized = ChartGuards.Points(points, nameof(points));
        if (materialized.Count < 2) throw new ArgumentException("Trend lines require at least two points.", nameof(points));

        var count = materialized.Count;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumXX = 0.0;
        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        foreach (var point in materialized) {
            sumX += point.X;
            sumY += point.Y;
            sumXY += point.X * point.Y;
            sumXX += point.X * point.X;
            if (point.X < minX) minX = point.X;
            if (point.X > maxX) maxX = point.X;
        }

        var denominator = count * sumXX - sumX * sumX;
        if (Math.Abs(denominator) < 0.000001) throw new ArgumentException("Trend lines require at least two distinct x values.", nameof(points));

        var slope = (count * sumXY - sumX * sumY) / denominator;
        var intercept = (sumY - slope * sumX) / count;
        Add(name, ChartSeriesKind.TrendLine, new[] {
            new ChartPoint(minX, slope * minX + intercept),
            new ChartPoint(maxX, slope * maxX + intercept)
        }, color);
        Series[Series.Count - 1].SetTrendLineSourcePoints(materialized);
        return this;
    }
}
