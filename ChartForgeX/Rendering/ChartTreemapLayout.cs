using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartTreemapLayout {
    public static List<ChartTreemapTile> Compute(ChartSeries series, ChartRect plot) {
        var items = new List<TreemapItem>();
        for (var i = 0; i < series.Points.Count; i++) {
            var point = series.Points[i];
            if (point.Y > 0) items.Add(new TreemapItem(i, point, point.Y));
        }

        items.Sort((left, right) => { int order = right.Value.CompareTo(left.Value); return order != 0 ? order : left.PointIndex.CompareTo(right.PointIndex); });
        var tiles = new List<ChartTreemapTile>(items.Count);
        Split(items, 0, items.Count, plot, tiles);
        return tiles;
    }

    private static void Split(IReadOnlyList<TreemapItem> items, int start, int count, ChartRect rect, List<ChartTreemapTile> tiles) {
        if (count <= 0 || rect.Width <= 0 || rect.Height <= 0) return;
        if (count == 1) {
            var item = items[start];
            tiles.Add(new ChartTreemapTile(item.PointIndex, item.Point, Inset(rect)));
            return;
        }

        var total = Sum(items, start, count);
        if (total <= 0) return;
        var firstCount = SplitCount(items, start, count, total);
        var firstTotal = Sum(items, start, firstCount);
        var ratio = firstTotal / total;
        if (rect.Width >= rect.Height) {
            var firstWidth = rect.Width * ratio;
            Split(items, start, firstCount, new ChartRect(rect.X, rect.Y, firstWidth, rect.Height), tiles);
            Split(items, start + firstCount, count - firstCount, new ChartRect(rect.X + firstWidth, rect.Y, rect.Width - firstWidth, rect.Height), tiles);
        } else {
            var firstHeight = rect.Height * ratio;
            Split(items, start, firstCount, new ChartRect(rect.X, rect.Y, rect.Width, firstHeight), tiles);
            Split(items, start + firstCount, count - firstCount, new ChartRect(rect.X, rect.Y + firstHeight, rect.Width, rect.Height - firstHeight), tiles);
        }
    }

    private static int SplitCount(IReadOnlyList<TreemapItem> items, int start, int count, double total) {
        var bestCount = 1;
        var bestDiff = double.PositiveInfinity;
        var sum = 0.0;
        for (var i = 0; i < count - 1; i++) {
            sum += items[start + i].Value;
            var diff = Math.Abs(total / 2.0 - sum);
            if (diff < bestDiff) {
                bestDiff = diff;
                bestCount = i + 1;
            }
        }

        return Math.Max(1, Math.Min(count - 1, bestCount));
    }

    private static double Sum(IReadOnlyList<TreemapItem> items, int start, int count) {
        var sum = 0.0;
        for (var i = 0; i < count; i++) sum += items[start + i].Value;
        return sum;
    }

    private static ChartRect Inset(ChartRect rect) {
        var gap = Math.Min(3, Math.Min(rect.Width, rect.Height) * 0.05);
        if (gap <= 0.1) return rect;
        return new ChartRect(rect.X + gap / 2, rect.Y + gap / 2, Math.Max(0, rect.Width - gap), Math.Max(0, rect.Height - gap));
    }

    private readonly struct TreemapItem {
        public TreemapItem(int pointIndex, ChartPoint point, double value) {
            PointIndex = pointIndex;
            Point = point;
            Value = value;
        }

        public int PointIndex { get; }

        public ChartPoint Point { get; }

        public double Value { get; }
    }
}
