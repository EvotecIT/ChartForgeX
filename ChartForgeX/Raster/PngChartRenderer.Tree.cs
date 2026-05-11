using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawTree(RgbaCanvas c, Chart chart, ChartRect plot) {
        var model = BuildTreeModel(chart, plot);
        if (model.Nodes.Count == 0 || model.Links.Count == 0) return;
        var showLabels = chart.Series.First(series => series.Kind == ChartSeriesKind.Tree).ShowDataLabels != false;
        foreach (var link in model.Links) DrawTreeLink(c, chart, model, link);
        foreach (var node in model.Nodes) DrawTreeNode(c, chart, model, node, showLabels);
    }

    private static bool IsTreeChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Tree);

    private static void DrawTreeNode(RgbaCanvas c, Chart chart, TreeModel model, TreeNode node, bool showLabels) {
        var theme = chart.Options.Theme;
        var color = theme.Palette[node.Depth % theme.Palette.Length];
        var labelColor = ChartColorMath.TextOnBackground(color);
        var radius = Math.Min(ChartVisualPrimitives.TreeNodeCornerRadiusMax, model.NodeHeight / 2);
        c.FillRoundedRectVerticalGradient(node.X, node.Y, model.NodeWidth, model.NodeHeight, radius, TreeNodeGradientTop(color), TreeNodeGradientBottom(color));
        c.StrokeRoundedRect(node.X, node.Y, model.NodeWidth, model.NodeHeight, radius, ApplyOpacity(ChartColorMath.TextOnBackground(labelColor, 0.70), ChartVisualPrimitives.TreeNodeBorderOpacity), ChartVisualPrimitives.TreeNodeBorderStrokeWidth);
        if (showLabels) DrawTreeNodeLabel(c, chart, model, node, labelColor);
    }

    private static void DrawTreeLink(RgbaCanvas c, Chart chart, TreeModel model, TreeLink link) {
        var parent = model.Nodes[link.Parent];
        var child = model.Nodes[link.Child];
        var color = chart.Options.Theme.Palette[parent.Depth % chart.Options.Theme.Palette.Length];
        var x0 = parent.X + model.NodeWidth;
        var y0 = parent.Y + model.NodeHeight / 2;
        var x1 = child.X;
        var y1 = child.Y + model.NodeHeight / 2;
        var gap = Math.Max(ChartVisualPrimitives.TreeLinkMinGap, (x1 - x0) / 2);
        var width = Math.Max(ChartVisualPrimitives.TreeLinkMinStrokeWidth, Math.Min(ChartVisualPrimitives.TreeLinkMaxStrokeWidth, ChartVisualPrimitives.TreeLinkMinStrokeWidth + link.Value / Math.Max(0.000001, model.MaxLinkValue) * ChartVisualPrimitives.TreeLinkStrokeWidthRange));
        var stroke = ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(255 * ChartVisualPrimitives.TreeLinkStrokeOpacity));
        var previousX = x0;
        var previousY = y0;
        const int segments = ChartVisualPrimitives.TreeLinkCurveSegments;
        for (var step = 1; step <= segments; step++) {
            var t = step / (double)segments;
            var x = Cubic(x0, x0 + gap, x1 - gap, x1, t);
            var y = Cubic(y0, y0, y1, y1, t);
            c.DrawLine(previousX, previousY, x, y, stroke, width);
            previousX = x;
            previousY = y;
        }
    }

    private static TreeModel BuildTreeModel(Chart chart, ChartRect plot) {
        ChartSeries? series = null;
        foreach (var candidate in chart.Series) if (candidate.Kind == ChartSeriesKind.Tree) { series = candidate; break; }
        if (series == null || series.Points.Count < 2) return TreeModel.Empty;
        var nodeCount = chart.Options.TreeNodeLabels.Count;
        var links = new List<TreeLink>();
        for (var i = 0; i + 1 < series.Points.Count; i += 2) {
            var endpoints = series.Points[i];
            var valuePoint = series.Points[i + 1];
            var parent = Math.Max(0, (int)Math.Round(endpoints.X));
            var child = Math.Max(0, (int)Math.Round(endpoints.Y));
            var value = Math.Max(0.000001, valuePoint.Y);
            nodeCount = Math.Max(nodeCount, Math.Max(parent, child) + 1);
            links.Add(new TreeLink(parent, child, value));
        }

        if (nodeCount == 0 || links.Count == 0) return TreeModel.Empty;
        var nodes = new List<TreeNode>();
        for (var i = 0; i < nodeCount; i++) nodes.Add(new TreeNode(i, TreeNodeLabel(chart, i)));
        var children = new List<int>[nodeCount];
        var incoming = new bool[nodeCount];
        for (var i = 0; i < nodeCount; i++) children[i] = new List<int>();
        foreach (var link in links) {
            children[link.Parent].Add(link.Child);
            incoming[link.Child] = true;
        }

        var root = 0;
        for (var i = 0; i < incoming.Length; i++) if (!incoming[i]) { root = i; break; }
        ApplyTreeDepths(nodes, children, root, 0);
        LayoutTreeNodes(nodes, children, root, plot, out var nodeWidth, out var nodeHeight, out var maxDepth);
        return new TreeModel(nodes, links, nodeWidth, nodeHeight, maxDepth, links.Max(link => link.Value));
    }

    private static void ApplyTreeDepths(List<TreeNode> nodes, List<int>[] children, int node, int depth) {
        nodes[node].Depth = depth;
        foreach (var child in children[node]) ApplyTreeDepths(nodes, children, child, depth + 1);
    }

    private static void LayoutTreeNodes(List<TreeNode> nodes, List<int>[] children, int root, ChartRect plot, out double nodeWidth, out double nodeHeight, out int maxDepth) {
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

    private static double TreeNodeLabelFontSize(double baseSize) => Math.Max(ChartVisualPrimitives.TreeNodeLabelMinFontSize, baseSize);

    private static void DrawTreeNodeLabel(RgbaCanvas c, Chart chart, TreeModel model, TreeNode node, ChartColor labelColor) {
        var fontSize = TreeNodeLabelFontSize(chart.Options.Theme.TickLabelFontSize);
        var maxWidth = model.NodeWidth - ChartVisualPrimitives.TreeNodeLabelHorizontalPadding * 2;
        var lines = TreeNodeLabelLines(node.Label, fontSize, maxWidth);
        var lineHeight = fontSize * ChartVisualPrimitives.TreeNodeLabelLineHeightFactor;
        var textHeight = lines.Length * lineHeight;
        var top = node.Y + (model.NodeHeight - textHeight) / 2.0;
        for (var i = 0; i < lines.Length; i++) {
            var y = top + i * lineHeight;
            var x = node.X + (model.NodeWidth - EstimatePngEmphasizedTextWidth(lines[i], fontSize)) / 2.0;
            DrawReadablePngLabel(c, x, y, lines[i], labelColor, ChartColorMath.TextOnBackground(labelColor, 0.70), fontSize);
        }
    }

    private static string[] TreeNodeLabelLines(string label, double fontSize, double maxWidth) {
        if (EstimatePngEmphasizedTextWidth(label, fontSize) <= maxWidth || !label.Contains(' ')) return new[] { label };
        var words = label.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2) return new[] { label };
        var bestSplit = 1;
        var bestScore = double.PositiveInfinity;
        for (var split = 1; split < words.Length; split++) {
            var first = string.Join(" ", words, 0, split);
            var second = string.Join(" ", words, split, words.Length - split);
            var score = Math.Max(EstimatePngEmphasizedTextWidth(first, fontSize), EstimatePngEmphasizedTextWidth(second, fontSize)) + Math.Abs(first.Length - second.Length) * 0.25;
            if (score >= bestScore) continue;
            bestScore = score;
            bestSplit = split;
        }

        return new[] { string.Join(" ", words, 0, bestSplit), string.Join(" ", words, bestSplit, words.Length - bestSplit) };
    }

    private static ChartColor TreeNodeGradientTop(ChartColor color) => ChartMarkSurface.TreeNodeGradientTop(color);

    private static ChartColor TreeNodeGradientBottom(ChartColor color) => ChartMarkSurface.TreeNodeGradientBottom(color);

    private sealed class TreeNode {
        public TreeNode(int index, string label) { Index = index; Label = label; }
        public int Index { get; }
        public string Label { get; }
        public int Depth { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    private readonly struct TreeLink {
        public TreeLink(int parent, int child, double value) { Parent = parent; Child = child; Value = value; }
        public int Parent { get; }
        public int Child { get; }
        public double Value { get; }
    }

    private readonly struct TreeModel {
        public TreeModel(List<TreeNode> nodes, List<TreeLink> links, double nodeWidth, double nodeHeight, int maxDepth, double maxLinkValue) {
            Nodes = nodes;
            Links = links;
            NodeWidth = nodeWidth;
            NodeHeight = nodeHeight;
            MaxDepth = maxDepth;
            MaxLinkValue = maxLinkValue;
        }

        public static TreeModel Empty { get; } = new(new List<TreeNode>(), new List<TreeLink>(), 0, 0, 0, 1);
        public List<TreeNode> Nodes { get; }
        public List<TreeLink> Links { get; }
        public double NodeWidth { get; }
        public double NodeHeight { get; }
        public int MaxDepth { get; }
        public double MaxLinkValue { get; }
    }
}
