using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Resolves vertical bar stack values with the same coordinate identity used by histogram slots and range calculation.
/// </summary>
internal static class ChartBarStacking {
    internal static double BaseValue(Chart chart, ChartBarCoordinateMap coordinateMap, int seriesIndex, ChartPoint point) {
        var sum = 0.0;
        var coordinate = coordinateMap.Resolve(point.X);
        for (var index = 0; index < seriesIndex; index++) {
            var series = chart.Series[index];
            if (series.Kind != ChartSeriesKind.Bar || !TryFindPoint(coordinateMap, series, coordinate, out var candidate)) continue;
            if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
        }

        return sum;
    }

    private static bool TryFindPoint(ChartBarCoordinateMap coordinateMap, ChartSeries series, double coordinate, out ChartPoint point) {
        for (var index = 0; index < series.Points.Count; index++) {
            if (coordinateMap.Resolve(series.Points[index].X) != coordinate) continue;
            point = series.Points[index];
            return true;
        }

        point = default;
        return false;
    }
}
