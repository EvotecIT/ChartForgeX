using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private const double HierarchyBucketGap = 28;
    private const double HierarchyBucketNodeGapX = 24;
    private const double HierarchyBucketNodeGapY = 28;

    private static bool TryPlaceHierarchyBucketsTopToBottom(LayerPlacement layer, IReadOnlyDictionary<string, TopologyNode> nodesById, double left, double availableW, double y, out double occupiedHeight) {
        occupiedHeight = 0;
        var buckets = BuildHorizontalHierarchyBuckets(layer, nodesById, availableW);
        if (buckets.Count == 0 || buckets.Sum(bucket => bucket.Nodes.Count) != layer.Nodes.Count) return false;
        occupiedHeight = buckets.Max(bucket => bucket.Height);
        var totalWidth = buckets.Sum(bucket => bucket.Width) + (buckets.Count - 1) * HierarchyBucketGap;
        var minLeft = left;
        var maxRight = left + availableW;
        var currentLeft = minLeft + Math.Max(0, (availableW - totalWidth) / 2);
        foreach (var bucket in buckets) {
            bucket.Left = Math.Max(bucket.Center - bucket.Width / 2, currentLeft);
            currentLeft = bucket.Left + bucket.Width + HierarchyBucketGap;
        }

        if (totalWidth <= availableW + 0.0001) FitHorizontalBuckets(buckets, minLeft, maxRight);
        var occupied = new HashSet<string>(StringComparer.Ordinal);
        foreach (var bucket in buckets) {
            for (var i = 0; i < bucket.Nodes.Count; i++) {
                var node = bucket.Nodes[i];
                var row = i / bucket.Columns;
                var col = i % bucket.Columns;
                var rowCount = Math.Min(bucket.Columns, bucket.Nodes.Count - row * bucket.Columns);
                var rowLeft = BucketRowLeft(bucket, row, rowCount, layer.MaxWidth, HierarchyBucketNodeGapX);
                ApplyLayerMetadata(node, layer, row, col);
                node.Metadata["layout.hierarchyPolicy"] = bucket.AppliedPolicy.ToString();
                occupied.Add(node.Id);
                if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
                node.X = rowLeft + col * (layer.MaxWidth + HierarchyBucketNodeGapX) + (layer.MaxWidth - node.Width) / 2;
                node.Y = y + row * (layer.MaxHeight + HierarchyBucketNodeGapY) + (layer.MaxHeight - node.Height) / 2;
            }
        }

        return occupied.Count == layer.Nodes.Count;
    }

    private static bool TryPlaceHierarchyBucketsLeftToRight(LayerPlacement layer, IReadOnlyDictionary<string, TopologyNode> nodesById, double x, double top, double availableH, out double occupiedWidth) {
        occupiedWidth = 0;
        var buckets = BuildVerticalHierarchyBuckets(layer, nodesById, availableH);
        if (buckets.Count == 0 || buckets.Sum(bucket => bucket.Nodes.Count) != layer.Nodes.Count) return false;
        occupiedWidth = buckets.Max(bucket => bucket.Width);
        var totalHeight = buckets.Sum(bucket => bucket.Height) + (buckets.Count - 1) * HierarchyBucketGap;
        var minTop = top;
        var maxBottom = top + availableH;
        var currentTop = minTop + Math.Max(0, (availableH - totalHeight) / 2);
        foreach (var bucket in buckets) {
            bucket.Top = Math.Max(bucket.Center - bucket.Height / 2, currentTop);
            currentTop = bucket.Top + bucket.Height + HierarchyBucketGap;
        }

        if (totalHeight <= availableH + 0.0001) FitVerticalBuckets(buckets, minTop, maxBottom);
        var occupied = new HashSet<string>(StringComparer.Ordinal);
        foreach (var bucket in buckets) {
            for (var i = 0; i < bucket.Nodes.Count; i++) {
                var node = bucket.Nodes[i];
                var col = i / bucket.Rows;
                var row = i % bucket.Rows;
                var columnCount = Math.Min(bucket.Rows, bucket.Nodes.Count - col * bucket.Rows);
                var columnTop = BucketColumnTop(bucket, col, columnCount, layer.MaxHeight, HierarchyBucketNodeGapY);
                ApplyLayerMetadata(node, layer, row, col);
                node.Metadata["layout.hierarchyPolicy"] = bucket.AppliedPolicy.ToString();
                occupied.Add(node.Id);
                if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
                node.X = x + col * (layer.MaxWidth + HierarchyBucketNodeGapX) + (layer.MaxWidth - node.Width) / 2;
                node.Y = columnTop + row * (layer.MaxHeight + HierarchyBucketNodeGapY) + (layer.MaxHeight - node.Height) / 2;
            }
        }

        return occupied.Count == layer.Nodes.Count;
    }

    private static List<HierarchyBucket> BuildHorizontalHierarchyBuckets(LayerPlacement layer, IReadOnlyDictionary<string, TopologyNode> nodesById, double availableW) {
        var grouped = layer.Nodes
            .GroupBy(node => ParentId(node), StringComparer.Ordinal)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && nodesById.ContainsKey(group.Key!))
            .Select(group => new { ParentId = group.Key!, Nodes = group.ToList() })
            .ToList();
        var bucketWidth = grouped.Count <= 0 ? availableW : Math.Max(layer.MaxWidth, (availableW - Math.Max(0, grouped.Count - 1) * HierarchyBucketGap) / grouped.Count);
        return grouped
            .Select(group => {
                var parent = nodesById[group.ParentId];
                var fitted = LayerColumns(group.Nodes.Count, bucketWidth, layer.MaxWidth, HierarchyBucketNodeGapX);
                var policy = AppliedHierarchyLayoutPolicy(parent, group.Nodes.Count, fitted);
                var columns = HierarchyColumns(group.Nodes.Count, fitted, policy);
                var rows = (int)Math.Ceiling(group.Nodes.Count / (double)columns);
                return new HierarchyBucket(group.Nodes, columns, rows, CenterX(parent), policy);
            })
            .OrderBy(bucket => bucket.Center)
            .ThenBy(bucket => bucket.Nodes[0].Id, StringComparer.Ordinal)
            .ToList();
    }

    private static List<HierarchyBucket> BuildVerticalHierarchyBuckets(LayerPlacement layer, IReadOnlyDictionary<string, TopologyNode> nodesById, double availableH) {
        var grouped = layer.Nodes
            .GroupBy(node => ParentId(node), StringComparer.Ordinal)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && nodesById.ContainsKey(group.Key!))
            .Select(group => new { ParentId = group.Key!, Nodes = group.ToList() })
            .ToList();
        var bucketHeight = grouped.Count <= 0 ? availableH : Math.Max(layer.MaxHeight, (availableH - Math.Max(0, grouped.Count - 1) * HierarchyBucketGap) / grouped.Count);
        return grouped
            .Select(group => {
                var parent = nodesById[group.ParentId];
                var fitted = LayerColumns(group.Nodes.Count, bucketHeight, layer.MaxHeight, HierarchyBucketNodeGapY);
                var policy = AppliedHierarchyLayoutPolicy(parent, group.Nodes.Count, fitted);
                var rows = HierarchyRows(group.Nodes.Count, fitted, policy);
                var columns = (int)Math.Ceiling(group.Nodes.Count / (double)rows);
                return new HierarchyBucket(group.Nodes, columns, rows, CenterY(parent), policy);
            })
            .OrderBy(bucket => bucket.Center)
            .ThenBy(bucket => bucket.Nodes[0].Id, StringComparer.Ordinal)
            .ToList();
    }

    private static string? ParentId(TopologyNode node) => node.Metadata.TryGetValue("hierarchy.parentId", out var parentId) ? parentId : null;

    private static TopologyHierarchyLayoutPolicy AppliedHierarchyLayoutPolicy(TopologyNode parent, int childCount, int fitted) {
        var configured = TopologyHierarchyLayoutPolicy.Auto;
        if (parent.Metadata.TryGetValue("hierarchy.layoutPolicy", out var value) && Enum.TryParse(value, out TopologyHierarchyLayoutPolicy parsed) && Enum.IsDefined(typeof(TopologyHierarchyLayoutPolicy), parsed)) configured = parsed;
        if (configured != TopologyHierarchyLayoutPolicy.Auto) return configured;
        return childCount <= fitted ? TopologyHierarchyLayoutPolicy.Standard : TopologyHierarchyLayoutPolicy.Compact;
    }

    private static int HierarchyColumns(int count, int fitted, TopologyHierarchyLayoutPolicy policy) {
        if (policy == TopologyHierarchyLayoutPolicy.Standard) return Math.Max(1, count);
        if (policy == TopologyHierarchyLayoutPolicy.Vertical) return 1;
        return Math.Max(1, Math.Min(fitted, (int)Math.Ceiling(Math.Sqrt(Math.Max(1, count)))));
    }

    private static int HierarchyRows(int count, int fitted, TopologyHierarchyLayoutPolicy policy) {
        if (policy is TopologyHierarchyLayoutPolicy.Standard or TopologyHierarchyLayoutPolicy.Vertical) return Math.Max(1, count);
        return Math.Max(1, Math.Min(fitted, (int)Math.Ceiling(Math.Sqrt(Math.Max(1, count)))));
    }

    private static void FitHorizontalBuckets(IReadOnlyList<HierarchyBucket> buckets, double minLeft, double maxRight) {
        var overflow = buckets[buckets.Count - 1].Left + buckets[buckets.Count - 1].Width - maxRight;
        if (overflow > 0) {
            foreach (var bucket in buckets) bucket.Left -= overflow;
        }

        if (buckets[0].Left >= minLeft) return;
        var shift = minLeft - buckets[0].Left;
        foreach (var bucket in buckets) bucket.Left += shift;
    }

    private static void FitVerticalBuckets(IReadOnlyList<HierarchyBucket> buckets, double minTop, double maxBottom) {
        var overflow = buckets[buckets.Count - 1].Top + buckets[buckets.Count - 1].Height - maxBottom;
        if (overflow > 0) {
            foreach (var bucket in buckets) bucket.Top -= overflow;
        }

        if (buckets[0].Top >= minTop) return;
        var shift = minTop - buckets[0].Top;
        foreach (var bucket in buckets) bucket.Top += shift;
    }

    private static void ApplyLayerMetadata(TopologyNode node, LayerPlacement layer, int row, int column) {
        node.Metadata["layout.layer"] = layer.Layer.ToString(CultureInfo.InvariantCulture);
        node.Metadata["layout.layerIndex"] = layer.Index.ToString(CultureInfo.InvariantCulture);
        node.Metadata["layout.row"] = row.ToString(CultureInfo.InvariantCulture);
        node.Metadata["layout.column"] = column.ToString(CultureInfo.InvariantCulture);
    }

    private static double BucketRowLeft(HierarchyBucket bucket, int row, int rowCount, double itemWidth, double gap) {
        var rowWidth = rowCount * itemWidth + Math.Max(0, rowCount - 1) * gap;
        var left = bucket.Left + Math.Max(0, (bucket.Width - rowWidth) / 2);
        if (rowCount >= bucket.Columns && bucket.Columns > 1) left = bucket.Left + StaggerOffset(row, bucket.Width - rowWidth);
        return left;
    }

    private static double BucketColumnTop(HierarchyBucket bucket, int column, int columnCount, double itemHeight, double gap) {
        var columnHeight = columnCount * itemHeight + Math.Max(0, columnCount - 1) * gap;
        var top = bucket.Top + Math.Max(0, (bucket.Height - columnHeight) / 2);
        if (columnCount >= bucket.Rows && bucket.Rows > 1) top = bucket.Top + StaggerOffset(column, bucket.Height - columnHeight);
        return top;
    }

    private static double StaggerOffset(int index, double available) {
        if (available <= 0) return 0;
        return (index % 3) switch {
            1 => available / 2,
            2 => available,
            _ => 0
        };
    }

    private sealed class HierarchyBucket {
        public HierarchyBucket(List<TopologyNode> nodes, int columns, int rows, double center, TopologyHierarchyLayoutPolicy appliedPolicy) {
            Nodes = nodes;
            Columns = Math.Max(1, columns);
            Rows = Math.Max(1, rows);
            Center = center;
            AppliedPolicy = appliedPolicy;
            var itemWidth = Nodes.Select(node => node.Width).DefaultIfEmpty(80).Max();
            var itemHeight = Nodes.Select(node => node.Height).DefaultIfEmpty(46).Max();
            Width = Columns * itemWidth + Math.Max(0, Columns - 1) * HierarchyBucketNodeGapX + (Rows > 1 && Columns > 1 ? (itemWidth + HierarchyBucketNodeGapX) / 2 : 0);
            Height = Rows * itemHeight + Math.Max(0, Rows - 1) * HierarchyBucketNodeGapY + (Columns > 1 && Rows > 1 ? (itemHeight + HierarchyBucketNodeGapY) / 2 : 0);
        }

        public List<TopologyNode> Nodes { get; }
        public int Columns { get; }
        public int Rows { get; }
        public double Center { get; }
        public TopologyHierarchyLayoutPolicy AppliedPolicy { get; }
        public double Width { get; }
        public double Height { get; }
        public double Left { get; set; }
        public double Top { get; set; }
    }
}
