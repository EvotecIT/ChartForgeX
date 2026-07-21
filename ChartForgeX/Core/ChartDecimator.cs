using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>Names explicit, deterministic point-reduction algorithms for dense ordered series.</summary>
public enum ChartDecimationMode {
    /// <summary>Preserve visual shape by selecting the point with the largest triangle area in each bucket.</summary>
    LargestTriangleThreeBuckets,

    /// <summary>Preserve local low and high values in source order for each bucket.</summary>
    MinMax
}

/// <summary>Describes an explicit point-reduction result, including its source-index mapping.</summary>
public sealed class ChartDecimationResult {
    internal ChartDecimationResult(ChartPoint[] points, int[] sourceIndices, int sourcePointCount, ChartDecimationMode mode) {
        Points = points;
        SourceIndices = sourceIndices;
        SourcePointCount = sourcePointCount;
        Mode = mode;
    }

    /// <summary>Gets the points retained for rendering.</summary>
    public IReadOnlyList<ChartPoint> Points { get; }

    /// <summary>Gets the zero-based source index represented by each retained point.</summary>
    public IReadOnlyList<int> SourceIndices { get; }

    /// <summary>Gets the number of points supplied before decimation.</summary>
    public int SourcePointCount { get; }

    /// <summary>Gets the algorithm used to choose retained points.</summary>
    public ChartDecimationMode Mode { get; }

    /// <summary>Gets whether the result contains fewer points than its source.</summary>
    public bool WasDecimated => Points.Count < SourcePointCount;
}

/// <summary>Provides explicit point reduction for dense ordered chart series.</summary>
public static class ChartDecimator {
    /// <summary>Reduces an ordered point sequence while retaining its source-index mapping.</summary>
    /// <param name="points">The ordered source points.</param>
    /// <param name="maximumPoints">The maximum number of rendered points. Must be at least three.</param>
    /// <param name="mode">The deterministic reduction algorithm.</param>
    /// <returns>A reduction result that reports exactly what was retained.</returns>
    public static ChartDecimationResult Decimate(IEnumerable<ChartPoint> points, int maximumPoints, ChartDecimationMode mode = ChartDecimationMode.LargestTriangleThreeBuckets) {
        if (points == null) throw new ArgumentNullException(nameof(points));
        if (maximumPoints < 3) throw new ArgumentOutOfRangeException(nameof(maximumPoints), maximumPoints, "Maximum points must be at least three.");
        if (!Enum.IsDefined(typeof(ChartDecimationMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown chart decimation mode.");
        var source = ChartGuards.Points(points, nameof(points)).ToArray();
        if (source.Length <= maximumPoints) return Identity(source, mode);
        return mode == ChartDecimationMode.MinMax
            ? MinMax(source, maximumPoints)
            : LargestTriangleThreeBuckets(source, maximumPoints);
    }

    private static ChartDecimationResult Identity(ChartPoint[] source, ChartDecimationMode mode) {
        var indices = new int[source.Length];
        for (var index = 0; index < indices.Length; index++) indices[index] = index;
        return new ChartDecimationResult(source, indices, source.Length, mode);
    }

    private static ChartDecimationResult MinMax(ChartPoint[] source, int maximumPoints) {
        if (maximumPoints == 3) {
            var extreme = 1;
            var first = source[0];
            var last = source[source.Length - 1];
            var span = last.X - first.X;
            var largestDeviation = -1d;
            for (var index = 1; index < source.Length - 1; index++) {
                var ratio = Math.Abs(span) < double.Epsilon ? 0d : (source[index].X - first.X) / span;
                var expected = first.Y + ((last.Y - first.Y) * ratio);
                var deviation = Math.Abs(source[index].Y - expected);
                if (deviation <= largestDeviation) continue;
                largestDeviation = deviation;
                extreme = index;
            }
            return Result(source, new List<int> { 0, extreme, source.Length - 1 }, ChartDecimationMode.MinMax);
        }
        var pairCount = Math.Max(1, (maximumPoints - 2) / 2);
        var selected = new List<int>(maximumPoints) { 0 };
        var interiorCount = source.Length - 2;
        for (var bucket = 0; bucket < pairCount; bucket++) {
            var start = 1 + (int)Math.Floor(bucket * interiorCount / (double)pairCount);
            var end = 1 + (int)Math.Floor((bucket + 1) * interiorCount / (double)pairCount);
            if (end <= start) continue;
            var min = start;
            var max = start;
            for (var index = start + 1; index < end; index++) {
                if (source[index].Y < source[min].Y) min = index;
                if (source[index].Y > source[max].Y) max = index;
            }
            if (min == max) selected.Add(min);
            else if (min < max) { selected.Add(min); selected.Add(max); }
            else { selected.Add(max); selected.Add(min); }
        }
        selected.Add(source.Length - 1);
        return Result(source, selected, ChartDecimationMode.MinMax);
    }

    private static ChartDecimationResult LargestTriangleThreeBuckets(ChartPoint[] source, int maximumPoints) {
        var selected = new List<int>(maximumPoints) { 0 };
        var every = (source.Length - 2d) / (maximumPoints - 2d);
        var anchorIndex = 0;
        for (var bucket = 0; bucket < maximumPoints - 2; bucket++) {
            var averageStart = Math.Min(source.Length - 1, (int)Math.Floor((bucket + 1) * every) + 1);
            var averageEnd = Math.Min(source.Length, (int)Math.Floor((bucket + 2) * every) + 1);
            if (averageEnd <= averageStart) averageEnd = Math.Min(source.Length, averageStart + 1);
            var averageX = 0d;
            var averageY = 0d;
            var averageCount = Math.Max(1, averageEnd - averageStart);
            for (var index = averageStart; index < averageEnd; index++) { averageX += source[index].X; averageY += source[index].Y; }
            averageX /= averageCount;
            averageY /= averageCount;

            var rangeStart = Math.Min(source.Length - 2, (int)Math.Floor(bucket * every) + 1);
            var rangeEnd = Math.Min(source.Length - 1, (int)Math.Floor((bucket + 1) * every) + 1);
            if (rangeEnd <= rangeStart) rangeEnd = Math.Min(source.Length - 1, rangeStart + 1);
            var anchor = source[anchorIndex];
            var nextIndex = rangeStart;
            var largestArea = -1d;
            for (var index = rangeStart; index < rangeEnd; index++) {
                var candidate = source[index];
                var area = Math.Abs((anchor.X - averageX) * (candidate.Y - anchor.Y) - (anchor.X - candidate.X) * (averageY - anchor.Y));
                if (area <= largestArea) continue;
                largestArea = area;
                nextIndex = index;
            }
            selected.Add(nextIndex);
            anchorIndex = nextIndex;
        }
        selected.Add(source.Length - 1);
        return Result(source, selected, ChartDecimationMode.LargestTriangleThreeBuckets);
    }

    private static ChartDecimationResult Result(ChartPoint[] source, List<int> selected, ChartDecimationMode mode) {
        var indices = selected.Distinct().OrderBy(index => index).ToArray();
        var points = new ChartPoint[indices.Length];
        for (var index = 0; index < indices.Length; index++) points[index] = source[indices[index]];
        return new ChartDecimationResult(points, indices, source.Length, mode);
    }
}
