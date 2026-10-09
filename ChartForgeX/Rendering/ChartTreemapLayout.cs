using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Partitions every sibling set with the same binary rectangle layout, inside its parent's measured header and padding.</summary>
internal static class ChartTreemapLayout {
    internal static IReadOnlyList<ChartTreemapTile> Compute(ChartSeries series, ChartRect plot, ChartTreemapOptions options, Func<int, double> headerHeight) {
        var index = series.Relationships ?? throw new InvalidOperationException("Treemaps require explicit items.");
        var tiles = new List<ChartTreemapTile>(series.TreemapItems.Count);
        Siblings(index.Roots, plot);
        return tiles;

        void Siblings(IReadOnlyList<int> siblings, ChartRect area) {
            var ordered = siblings.Where(item => index.HierarchyValues[item] > 0)
                .OrderByDescending(item => index.HierarchyValues[item]).ThenBy(item => item).ToArray();
            Split(ordered, 0, ordered.Length, area, index.HierarchyValues, (item, allocated) => {
                var rect = Inset(allocated, options.Gap);
                var children = index.Children(item);
                var isGroup = children.Count > 0;
                var content = rect; var header = new ChartRect(rect.X, rect.Y, 0, 0);
                if (isGroup) {
                    var padding = Math.Min(options.GroupPadding, Math.Min(rect.Width, rect.Height) * .15);
                    content = new ChartRect(rect.X + padding, rect.Y + padding, Math.Max(0, rect.Width - padding * 2), Math.Max(0, rect.Height - padding * 2));
                    var height = options.ShowGroupLabels ? Math.Min(headerHeight(item), content.Height * .3) : 0;
                    header = new ChartRect(content.X, content.Y, content.Width, height);
                    content = new ChartRect(content.X, content.Y + height, content.Width, Math.Max(0, content.Height - height));
                }
                tiles.Add(new ChartTreemapTile(item, rect, content, header));
                if (isGroup) Siblings(children, content);
            });
        }
    }

    private static void Split(int[] items, int start, int count, ChartRect rect, IReadOnlyList<double> values, Action<int, ChartRect> emit) {
        if (count <= 0 || rect.Width <= 0 || rect.Height <= 0) return;
        if (count == 1) { emit(items[start], rect); return; }
        var total = Sum(items, start, count, values);
        var firstCount = SplitCount(items, start, count, total, values);
        var ratio = Sum(items, start, firstCount, values) / total;
        if (rect.Width >= rect.Height) {
            var width = rect.Width * ratio;
            Split(items, start, firstCount, new ChartRect(rect.X, rect.Y, width, rect.Height), values, emit);
            Split(items, start + firstCount, count - firstCount, new ChartRect(rect.X + width, rect.Y, rect.Width - width, rect.Height), values, emit);
        } else {
            var height = rect.Height * ratio;
            Split(items, start, firstCount, new ChartRect(rect.X, rect.Y, rect.Width, height), values, emit);
            Split(items, start + firstCount, count - firstCount, new ChartRect(rect.X, rect.Y + height, rect.Width, rect.Height - height), values, emit);
        }
    }

    private static int SplitCount(int[] items, int start, int count, double total, IReadOnlyList<double> values) {
        var best = 1; var difference = double.PositiveInfinity; var sum = 0d;
        for (var i = 0; i < count - 1; i++) {
            sum += values[items[start + i]];
            var candidate = Math.Abs(total / 2 - sum);
            if (candidate < difference) { difference = candidate; best = i + 1; }
        }
        return best;
    }

    private static double Sum(int[] items, int start, int count, IReadOnlyList<double> values) {
        var sum = 0d;
        for (var i = 0; i < count; i++) sum += values[items[start + i]];
        return sum;
    }

    private static ChartRect Inset(ChartRect rect, double requestedGap) {
        var gap = Math.Min(requestedGap, Math.Min(rect.Width, rect.Height) * .05);
        return gap <= .1 ? rect : new ChartRect(rect.X + gap / 2, rect.Y + gap / 2, Math.Max(0, rect.Width - gap), Math.Max(0, rect.Height - gap));
    }
}
