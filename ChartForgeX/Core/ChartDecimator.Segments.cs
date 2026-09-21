using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public static partial class ChartDecimator {
    private static ChartDecimationResult Segmented(ChartPoint[] source, int maximumPoints, ChartDecimationMode mode) {
        var starts = new List<int> { 0 };
        for (var index = 1; index < source.Length; index++) if (source[index].BreakBefore) starts.Add(index);
        starts.Add(source.Length);
        var budgets = new int[starts.Count - 1];
        var minimumTotal = 0;
        for (var index = 0; index < budgets.Length; index++) {
            budgets[index] = Math.Min(starts[index + 1] - starts[index], mode == ChartDecimationMode.MinMax ? 4 : 3);
            minimumTotal += budgets[index];
        }
        if (minimumTotal > maximumPoints)
            throw new ArgumentOutOfRangeException(nameof(maximumPoints), maximumPoints,
                "The point budget must be at least " + minimumTotal.ToString(System.Globalization.CultureInfo.InvariantCulture) + " to preserve every disconnected segment with this algorithm.");
        var remaining = maximumPoints - minimumTotal;
        var capacity = source.Length - minimumTotal;
        var assigned = 0;
        for (var index = 0; index < budgets.Length; index++) {
            var extra = (int)((long)remaining * (starts[index + 1] - starts[index] - budgets[index]) / capacity);
            budgets[index] += extra;
            assigned += extra;
        }
        for (var index = 0; assigned < remaining; index++) {
            if (budgets[index] >= starts[index + 1] - starts[index]) continue;
            budgets[index]++;
            assigned++;
        }
        var indices = new List<int>(maximumPoints);
        for (var index = 0; index < budgets.Length; index++) {
            var count = starts[index + 1] - starts[index];
            var segment = new ChartPoint[count];
            Array.Copy(source, starts[index], segment, 0, count);
            var result = count <= budgets[index] ? Identity(segment, mode)
                : mode == ChartDecimationMode.MinMax ? MinMax(segment, budgets[index]) : LargestTriangleThreeBuckets(segment, budgets[index]);
            foreach (var retained in result.SourceIndices) indices.Add(starts[index] + retained);
        }
        return Result(source, indices, mode);
    }
}
