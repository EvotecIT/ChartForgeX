using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartTreeLayout {
    public static ChartTreeModel Build(Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.Tree);
        if (series == null || series.Points.Count < 2) return ChartTreeModel.Empty;
        var nodeCount = chart.Options.TreeNodeLabels.Count;
        var links = new List<ChartTreeLayoutLink>();
        for (var i = 0; i + 1 < series.Points.Count; i += 2) {
            var endpoints = series.Points[i];
            var valuePoint = series.Points[i + 1];
            var parent = Math.Max(0, (int)Math.Round(endpoints.X));
            var child = Math.Max(0, (int)Math.Round(endpoints.Y));
            var value = Math.Max(0.000001, valuePoint.Y);
            nodeCount = Math.Max(nodeCount, Math.Max(parent, child) + 1);
            links.Add(new ChartTreeLayoutLink(parent, child, value));
        }

        if (nodeCount == 0 || links.Count == 0) return ChartTreeModel.Empty;
        var nodes = new List<ChartTreeNode>();
        for (var i = 0; i < nodeCount; i++) nodes.Add(new ChartTreeNode(i, TreeNodeLabel(chart, i)));
        var children = new List<int>[nodeCount];
        var incoming = new bool[nodeCount];
        for (var i = 0; i < nodeCount; i++) children[i] = new List<int>();
        foreach (var link in links) {
            children[link.Parent].Add(link.Child);
            incoming[link.Child] = true;
        }

        var root = 0;
        for (var i = 0; i < incoming.Length; i++) if (!incoming[i]) { root = i; break; }
        ApplyDepths(nodes, children, root, 0);
        LayoutNodes(nodes, children, root, plot, out var nodeWidth, out var nodeHeight, out var maxDepth);
        var maxLinkValue = links.Max(link => link.Value);
        return new ChartTreeModel(nodes, links, nodeWidth, nodeHeight, maxDepth, maxLinkValue);
    }

    private static void ApplyDepths(IReadOnlyList<ChartTreeNode> nodes, IReadOnlyList<int>[] children, int node, int depth) {
        nodes[node].Depth = depth;
        foreach (var child in children[node]) ApplyDepths(nodes, children, child, depth + 1);
    }

    private static void LayoutNodes(IReadOnlyList<ChartTreeNode> nodes, IReadOnlyList<int>[] children, int root, ChartRect plot, out double nodeWidth, out double nodeHeight, out int maxDepth) {
        maxDepth = Math.Max(1, nodes.Max(node => node.Depth));
        var leafCount = Math.Max(1, nodes.Count(node => children[node.Index].Count == 0));
        nodeWidth = Math.Max(ChartVisualPrimitives.TreeNodeMinWidth, Math.Min(ChartVisualPrimitives.TreeNodeMaxWidth, plot.Width / (maxDepth + 1) * ChartVisualPrimitives.TreeNodeWidthFactor));
        nodeHeight = Math.Max(ChartVisualPrimitives.TreeNodeMinHeight, Math.Min(ChartVisualPrimitives.TreeNodeMaxHeight, plot.Height / Math.Max(1, leafCount) * ChartVisualPrimitives.TreeNodeHeightFactor));
        var effectiveNodeHeight = nodeHeight;
        var availableHeight = Math.Max(1, plot.Height - effectiveNodeHeight - ChartVisualPrimitives.TreeLayoutVerticalPadding);
        var nextLeaf = 0;
        AssignY(root);
        foreach (var node in nodes) {
            var horizontalPadding = Math.Min(ChartVisualPrimitives.TreeLayoutHorizontalPadding, Math.Max(0, (plot.Width - nodeWidth) / 2));
            var availableWidth = Math.Max(1, plot.Width - nodeWidth - horizontalPadding * 2);
            node.X = maxDepth == 0 ? plot.Left + plot.Width / 2 - nodeWidth / 2 : plot.Left + horizontalPadding + node.Depth / (double)maxDepth * availableWidth;
            node.Y -= nodeHeight / 2;
        }

        double AssignY(int nodeIndex) {
            if (children[nodeIndex].Count == 0) {
                var y = leafCount == 1 ? plot.Top + plot.Height / 2 : plot.Top + effectiveNodeHeight / 2 + ChartVisualPrimitives.TreeLayoutLeafInset + nextLeaf / (double)Math.Max(1, leafCount - 1) * availableHeight;
                nodes[nodeIndex].Y = y;
                nextLeaf++;
                return y;
            }

            var total = 0.0;
            foreach (var child in children[nodeIndex]) total += AssignY(child);
            nodes[nodeIndex].Y = total / Math.Max(1, children[nodeIndex].Count);
            return nodes[nodeIndex].Y;
        }
    }

    private static string TreeNodeLabel(Chart chart, int index) =>
        index >= 0 && index < chart.Options.TreeNodeLabels.Count ? chart.Options.TreeNodeLabels[index] : "Node " + (index + 1).ToString(CultureInfo.InvariantCulture);
}

internal sealed class ChartTreeNode {
    public ChartTreeNode(int index, string label) { Index = index; Label = label; }
    public int Index { get; }
    public string Label { get; }
    public int Depth { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

internal readonly struct ChartTreeLayoutLink {
    public ChartTreeLayoutLink(int parent, int child, double value) { Parent = parent; Child = child; Value = value; }
    public int Parent { get; }
    public int Child { get; }
    public double Value { get; }
}

internal readonly struct ChartTreeModel {
    public ChartTreeModel(IReadOnlyList<ChartTreeNode> nodes, IReadOnlyList<ChartTreeLayoutLink> links, double nodeWidth, double nodeHeight, int maxDepth, double maxLinkValue) {
        Nodes = nodes;
        Links = links;
        NodeWidth = nodeWidth;
        NodeHeight = nodeHeight;
        MaxDepth = maxDepth;
        MaxLinkValue = maxLinkValue;
    }

    public static ChartTreeModel Empty { get; } = new(Array.Empty<ChartTreeNode>(), Array.Empty<ChartTreeLayoutLink>(), 0, 0, 0, 1);
    public IReadOnlyList<ChartTreeNode> Nodes { get; }
    public IReadOnlyList<ChartTreeLayoutLink> Links { get; }
    public double NodeWidth { get; }
    public double NodeHeight { get; }
    public int MaxDepth { get; }
    public double MaxLinkValue { get; }
}
