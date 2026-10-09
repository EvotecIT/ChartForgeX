using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartStackLayout {
    private static int[][] CoordinateIds(Chart chart, ChartBarCoordinateMap bars) {
        var ids = chart.Series.Select(series => Supports(series.Kind) ? new int[series.Points.Count] : Array.Empty<int>()).ToArray();
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            if (chart.Series[seriesIndex].Kind != ChartSeriesKind.Bar) continue;
            for (var point = 0; point < ids[seriesIndex].Length; point++) ids[seriesIndex][point] = bars.Resolve(seriesIndex, point).Id;
        }
        // A kind namespaces these identities. Adjacent numeric equivalents share a
        // coordinate, while repeated positions in one source series remain distinct.
        foreach (var kind in new[] { ChartSeriesKind.HorizontalBar, ChartSeriesKind.StackedArea }) {
            var active = new List<CoordinateGroup>(); var ordinal = 0;
            var observations = Enumerable.Range(0, chart.Series.Count).Where(index => chart.Series[index].Kind == kind)
                .SelectMany(series => Enumerable.Range(0, chart.Series[series].Points.Count)
                    .Select(point => (Series: series, Point: point, Coordinate: chart.Series[series].Points[point].X)))
                .OrderBy(point => point.Coordinate).ThenBy(point => point.Series).ThenBy(point => point.Point);
            foreach (var observation in observations) {
                active.RemoveAll(group => !ChartMath.SameCoordinate(group.Last, observation.Coordinate));
                var group = active.FirstOrDefault(candidate => !candidate.Series.Contains(observation.Series));
                if (group == null) { group = new CoordinateGroup(ordinal++); active.Add(group); }
                group.Last = observation.Coordinate; group.Series.Add(observation.Series);
                ids[observation.Series][observation.Point] = group.Id;
            }
        }
        return ids;
    }

    private sealed class CoordinateGroup {
        internal CoordinateGroup(int id) { Id = id; }
        internal int Id { get; }
        internal double Last { get; set; }
        internal HashSet<int> Series { get; } = new();
    }
}
