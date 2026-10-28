using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartStackLayout {
    private readonly Dictionary<int, IReadOnlyList<ChartStackAreaVertex>> _areaVertices = new();

    /// <summary>Returns aligned contour vertices only; source marks and readouts continue to use Point.</summary>
    internal IReadOnlyList<ChartStackAreaVertex>? AreaVertices(int series) =>
        _areaVertices.TryGetValue(series, out var vertices) ? vertices : null;

    private void PrepareAreaAlignment(Chart chart, int[][] coordinates) {
        foreach (var group in _seriesGroups.Where(group => group != null && group.Kind == ChartSeriesKind.StackedArea
            && group.MissingData != ChartStackedAreaMissingDataPolicy.Independent).Distinct()) {
            var members = Enumerable.Range(0, chart.Series.Count).Where(index => _seriesGroups[index] == group).ToArray();
            var union = new SortedDictionary<int, Dictionary<int, int>>();
            foreach (var member in members) {
                var source = chart.Series[member].RenderPoints;
                for (var point = 0; point < source.Count; point++) {
                    if (point > 0 && CategoryCoordinate(chart, source[point].X) <= CategoryCoordinate(chart, source[point - 1].X))
                        throw new InvalidOperationException("Aligned stacked areas require unique increasing X coordinates, including across explicit breaks.");
                    var coordinate = coordinates[member][point];
                    if (!union.TryGetValue(coordinate, out var observations)) union.Add(coordinate, observations = new Dictionary<int, int>());
                    // Existing coordinate IDs preserve duplicates; reject equivalent source positions independently.
                    if (point > 0 && ChartMath.SameCoordinate(CategoryCoordinate(chart, source[point - 1].X), CategoryCoordinate(chart, source[point].X)))
                        throw new InvalidOperationException("Aligned stacked areas require unique X coordinates.");
                    observations.Add(member, point);
                }
            }
            var vertices = members.ToDictionary(member => member, _ => new List<ChartStackAreaVertex>());
            var pendingBreak = true;
            foreach (var observations in union.Values) {
                if (group.MissingData == ChartStackedAreaMissingDataPolicy.Gap && observations.Count != members.Length) {
                    pendingBreak = true; continue;
                }
                var representative = observations.OrderBy(item => item.Key).First();
                var x = chart.Series[representative.Key].RenderPoints[representative.Value].X;
                var split = pendingBreak || observations.Any(item => chart.Series[item.Key].RenderPoints[item.Value].BreakBefore);
                var positive = 0d;
                foreach (var member in members) {
                    var present = observations.TryGetValue(member, out var point);
                    var stack = present ? Point(member, point) : default;
                    // A missing member is a zero-height positive contour; it never acquires a source identity.
                    var baseline = present ? stack.Base : positive;
                    var end = present ? stack.End : baseline;
                    vertices[member].Add(new ChartStackAreaVertex(x, baseline, end, split, present ? point : (int?)null));
                    if (present) {
                        if (chart.Series[member].RenderPoints[point].Y >= 0) positive = end;
                    }
                }
                pendingBreak = false;
            }
            foreach (var member in members) _areaVertices.Add(member, vertices[member]);
        }
    }
}

/// <summary>A contour vertex can be derived at a missing X without becoming an authored observation.</summary>
internal readonly struct ChartStackAreaVertex {
    internal ChartStackAreaVertex(double x, double baseline, double end, bool breakBefore, int? sourcePoint) {
        X = x; Base = baseline; End = end; BreakBefore = breakBefore; SourcePoint = sourcePoint;
    }
    internal double X { get; }
    internal double Base { get; }
    internal double End { get; }
    internal bool BreakBefore { get; }
    internal int? SourcePoint { get; }
}
