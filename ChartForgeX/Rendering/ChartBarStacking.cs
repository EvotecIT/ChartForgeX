using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Resolves vertical bar stack values with the same coordinate identity used by histogram slots and range calculation.
/// </summary>
internal static class ChartBarStacking {
    internal static double BaseValue(Chart chart, int seriesIndex, ChartPoint point) {
        var sum = 0.0;
        var currentSeries = chart.Series[seriesIndex];
        var coordinate = ChartHistogramBarSlot.CanonicalCoordinate(chart, currentSeries, point.X);
        for (var index = 0; index < seriesIndex; index++) {
            var series = chart.Series[index];
            if (series.Kind != ChartSeriesKind.Bar || !TryFindPoint(chart, series, coordinate, out var candidate)) continue;
            if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
        }

        return sum;
    }

    private static bool TryFindPoint(Chart chart, ChartSeries series, double coordinate, out ChartPoint point) {
        for (var index = 0; index < series.Points.Count; index++) {
            var candidateCoordinate = ChartHistogramBarSlot.CanonicalCoordinate(chart, series, series.Points[index].X);
            if (candidateCoordinate != coordinate) continue;
            point = series.Points[index];
            return true;
        }

        for (var index = 0; index < series.Points.Count; index++) {
            var candidateCoordinate = ChartHistogramBarSlot.CanonicalCoordinate(chart, series, series.Points[index].X);
            if (!ChartMath.SameCoordinate(candidateCoordinate, coordinate)) continue;
            point = series.Points[index];
            return true;
        }

        point = default;
        return false;
    }
}
