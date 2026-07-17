using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Resolves vertical bar stack values with the same coordinate identity used by histogram slots and range calculation.
/// </summary>
internal static class ChartBarStacking {
    internal static double BaseValue(Chart chart, ChartBarCoordinateMap coordinateMap, int seriesIndex, int pointIndex) {
        var sum = 0.0;
        var point = chart.Series[seriesIndex].Points[pointIndex];
        var coordinate = coordinateMap.Resolve(seriesIndex, pointIndex);
        for (var index = 0; index < seriesIndex; index++) {
            var series = chart.Series[index];
            if (series.Kind != ChartSeriesKind.Bar || !TryFindPoint(coordinateMap, index, series, coordinate, out var candidate)) continue;
            if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
        }

        return sum;
    }

    private static bool TryFindPoint(ChartBarCoordinateMap coordinateMap, int seriesIndex, ChartSeries series, ChartBarCoordinateKey coordinate, out ChartPoint point) {
        for (var index = 0; index < series.Points.Count; index++) {
            if (!coordinateMap.Resolve(seriesIndex, index).Equals(coordinate)) continue;
            point = series.Points[index];
            return true;
        }

        point = default;
        return false;
    }
}
