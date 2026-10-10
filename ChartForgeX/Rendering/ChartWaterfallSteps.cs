using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Owns the accumulation and checkpoint intervals used by ranges, marks, labels and source metadata.</summary>
internal static class ChartWaterfallSteps {
    internal static IReadOnlyList<ChartWaterfallStep> Create(ChartSeries series) {
        series.ValidateWaterfall();
        var typed = series.WaterfallItems.Count > 0;
        var items = typed ? series.WaterfallItems : series.Points.Select(point => ChartWaterfallItem.Delta(point.X, point.Y)).ToArray();
        var steps = new List<ChartWaterfallStep>(items.Count + (typed ? 0 : 1));
        var cumulative = 0d; var checkpointBalance = 0d; var subtotal = 0d; var sourceIndex = 0; var checkpointSource = 0;
        for (var index = 0; index < items.Count; index++) {
            var item = items[index];
            if (item.Kind == ChartWaterfallItemKind.OpeningBalance) {
                var value = item.Value!.Value;
                steps.Add(new ChartWaterfallStep(index, sourceIndex, item.X, item.Kind, value, 0, value, new[] { sourceIndex }));
                cumulative = value; checkpointBalance = value; sourceIndex++; checkpointSource = sourceIndex;
            } else if (item.Kind == ChartWaterfallItemKind.Delta) {
                var value = item.Value!.Value; var end = cumulative + value;
                RequireFinite(end);
                steps.Add(new ChartWaterfallStep(index, sourceIndex, item.X, item.Kind, value, cumulative, end, new[] { sourceIndex }));
                cumulative = end; subtotal += value; sourceIndex++;
            } else {
                var total = item.Kind == ChartWaterfallItemKind.Total;
                var value = total ? cumulative : subtotal;
                RequireFinite(value);
                var sources = Enumerable.Range(total ? 0 : checkpointSource, total ? sourceIndex : sourceIndex - checkpointSource).ToArray();
                steps.Add(new ChartWaterfallStep(index, -1, item.X, item.Kind, value, total ? 0 : checkpointBalance, cumulative, sources));
                checkpointBalance = cumulative; subtotal = 0; checkpointSource = sourceIndex;
            }
        }
        if (typed || steps.Count == 0) return steps;
        var coordinates = series.Points.Select(point => point.X).Distinct().OrderBy(value => value).ToArray();
        var spacing = double.PositiveInfinity;
        for (var index = 1; index < coordinates.Length; index++) spacing = Math.Min(spacing, coordinates[index] - coordinates[index - 1]);
        if (double.IsInfinity(spacing)) spacing = 1;
        var totalX = coordinates[coordinates.Length - 1] + spacing;
        if (!ChartMath.IsFinite(totalX) || totalX <= coordinates[coordinates.Length - 1])
            throw new InvalidOperationException("Waterfall coordinates must leave a finite position for the total.");
        steps.Add(new ChartWaterfallStep(steps.Count, -1, totalX, ChartWaterfallItemKind.Total, cumulative, 0, cumulative,
            Enumerable.Range(0, sourceIndex).ToArray(), appendedTotal: true));
        return steps;
    }

    private static void RequireFinite(double value) {
        if (!ChartMath.IsFinite(value)) throw new InvalidOperationException("Waterfall cumulative and checkpoint values must remain finite.");
    }
}

internal readonly struct ChartWaterfallStep {
    internal ChartWaterfallStep(int itemIndex, int sourceIndex, double x, ChartWaterfallItemKind kind, double value, double start, double end,
        IReadOnlyList<int> sourceIndices, bool appendedTotal = false) {
        ItemIndex = itemIndex; SourceIndex = sourceIndex; X = x; Kind = kind; Value = value; Start = start; End = end;
        SourceIndices = sourceIndices; IsAppendedTotal = appendedTotal;
    }
    internal int ItemIndex { get; }
    internal int SourceIndex { get; }
    internal double X { get; }
    internal ChartWaterfallItemKind Kind { get; }
    internal double Value { get; }
    internal double Start { get; }
    internal double End { get; }
    internal IReadOnlyList<int> SourceIndices { get; }
    internal bool IsCheckpoint => Kind == ChartWaterfallItemKind.Subtotal || Kind == ChartWaterfallItemKind.Total;
    internal bool IsAppendedTotal { get; }
}
