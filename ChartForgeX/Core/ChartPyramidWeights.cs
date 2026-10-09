using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>Validates the proportional-weight contract at authoring and mutable-model preparation boundaries.</summary>
internal static class ChartPyramidWeights {
    internal static double Total(IReadOnlyList<ChartPoint> points) {
        var total = 0d;
        foreach (var point in points) {
            ChartGuards.Finite(point.X, nameof(points));
            ChartGuards.Finite(point.Y, nameof(points));
            if (point.Y < 0) throw new ArgumentOutOfRangeException(nameof(points), "Pyramid values must be non-negative.");
            if (point.Y > double.MaxValue - total)
                throw new ArgumentOutOfRangeException(nameof(points), "The sum of pyramid values must be finite.");
            total += point.Y;
        }
        return total;
    }
}
