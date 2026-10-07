using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Derives cumulative intervals once from raw deltas without changing their coordinates or values.</summary>
internal static class ChartWaterfallSteps {
    internal static IReadOnlyList<ChartWaterfallStep> Create(ChartSeries series) {
        var steps = new List<ChartWaterfallStep>(); var cumulative = 0d;
        for (var index = 0; index < series.Points.Count; index++) {
            var point = series.Points[index]; var end = cumulative + point.Y;
            if (!ChartMath.IsFinite(end)) throw new InvalidOperationException("Waterfall cumulative values must remain finite.");
            steps.Add(new ChartWaterfallStep(index, point.X, point.Y, cumulative, end, false));
            cumulative = end;
        }
        if (steps.Count == 0) return steps;
        var coordinates = series.Points.Select(point => point.X).Distinct().OrderBy(value => value).ToArray();
        var spacing = double.PositiveInfinity;
        for (var index = 1; index < coordinates.Length; index++) spacing = Math.Min(spacing, coordinates[index] - coordinates[index - 1]);
        if (double.IsInfinity(spacing)) spacing = 1;
        var totalX = coordinates[coordinates.Length - 1] + spacing;
        if (!ChartMath.IsFinite(totalX) || totalX <= coordinates[coordinates.Length - 1])
            throw new InvalidOperationException("Waterfall coordinates must leave a finite position for the total.");
        steps.Add(new ChartWaterfallStep(-1, totalX, cumulative, 0, cumulative, true));
        return steps;
    }
}

internal readonly struct ChartWaterfallStep {
    internal ChartWaterfallStep(int sourceIndex, double x, double delta, double start, double end, bool total) {
        SourceIndex = sourceIndex; X = x; Delta = delta; Start = start; End = end; IsTotal = total;
    }
    internal int SourceIndex { get; }
    internal double X { get; }
    internal double Delta { get; }
    internal double Start { get; }
    internal double End { get; }
    internal bool IsTotal { get; }
}
