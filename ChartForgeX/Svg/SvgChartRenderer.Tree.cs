using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawTree(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var model = BuildTreeModel(chart, plot);
        if (model.Nodes.Count == 0 || model.Links.Count == 0) return;
        var t = chart.Options.Theme;
        var showLabels = chart.Series.First(series => series.Kind == ChartSeriesKind.Tree).ShowDataLabels != false;
        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "tree-chart")
            .EndStartElement()
            .Line();
        DrawTreeNodeGradients(writer, chart, id);
        foreach (var link in model.Links) DrawTreeLink(writer, chart, model, link);
        foreach (var node in model.Nodes) {
            var fillIndex = node.Depth % Math.Max(1, t.Palette.Length);
            var summary = node.Label + ": level " + node.Depth.ToString(CultureInfo.InvariantCulture);
            var radius = Math.Min(ChartVisualPrimitives.TreeNodeCornerRadiusMax, model.NodeHeight / 2);
            var labelColor = HeatmapTextColor(t.Palette[fillIndex]);
            var borderStroke = ChartVisualPrimitives.TreeNodeBorderStrokeWidth;
            var borderInset = borderStroke / 2.0;
            writer
                .StartElement("rect")
                .Attribute("data-cfx-role", "tree-node")
                .Attribute("data-cfx-node", node.Index)
                .Attribute("data-cfx-depth", node.Depth)
                .Attribute("data-cfx-label", node.Label)
                .Attribute("role", "img")
                .Attribute("aria-label", summary)
                .Attribute("x", node.X)
                .Attribute("y", node.Y)
                .Attribute("width", model.NodeWidth)
                .Attribute("height", model.NodeHeight)
                .Attribute("rx", radius)
                .Attribute("fill", $"url(#{id}-treeFill{fillIndex})")
                .EndEmptyElement()
                .Line();
            writer
                .StartElement("rect")
                .Attribute("data-cfx-role", "tree-node-border")
                .Attribute("x", node.X + borderInset)
                .Attribute("y", node.Y + borderInset)
                .Attribute("width", Math.Max(0, model.NodeWidth - borderStroke))
                .Attribute("height", Math.Max(0, model.NodeHeight - borderStroke))
                .Attribute("rx", Math.Max(0, radius - borderInset))
                .Attribute("fill", "none")
                .Attribute("stroke", TreeLabelHalo(labelColor).ToCss())
                .Attribute("stroke-opacity", ChartVisualPrimitives.TreeNodeBorderOpacity)
                .Attribute("stroke-width", borderStroke)
                .Attribute("vector-effect", "non-scaling-stroke")
                .EndEmptyElement()
                .Line();
            if (showLabels) DrawTreeNodeLabel(writer, chart, node, model, labelColor);
        }

        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void DrawTreeNodeGradients(SvgMarkupWriter writer, Chart chart, string id) {
        writer.StartElement("defs").EndStartElement().Line();
        for (var i = 0; i < chart.Options.Theme.Palette.Length; i++) {
            var color = chart.Options.Theme.Palette[i];
            writer
                .StartElement("linearGradient")
                .Attribute("id", $"{id}-treeFill{i}")
                .Attribute("x1", 0)
                .Attribute("x2", 0)
                .Attribute("y1", 0)
                .Attribute("y2", 1)
                .EndStartElement()
                .StartElement("stop")
                .Attribute("offset", "0%")
                .Attribute("stop-color", TreeNodeGradientTop(color).ToHex())
                .EndEmptyElement()
                .StartElement("stop")
                .Attribute("offset", "100%")
                .Attribute("stop-color", TreeNodeGradientBottom(color).ToHex())
                .EndEmptyElement()
                .EndElement()
                .Line();
        }
        writer.EndElement().Line();
    }

    private static void DrawTreeLink(SvgMarkupWriter writer, Chart chart, TreeModel model, TreeLink link) {
        var parent = model.Nodes[link.Parent];
        var child = model.Nodes[link.Child];
        var color = chart.Options.Theme.Palette[parent.Depth % chart.Options.Theme.Palette.Length];
        var x0 = parent.X + model.NodeWidth;
        var y0 = parent.Y + model.NodeHeight / 2;
        var x1 = child.X;
        var y1 = child.Y + model.NodeHeight / 2;
        var gap = Math.Max(ChartVisualPrimitives.TreeLinkMinGap, (x1 - x0) / 2);
        var width = Math.Max(ChartVisualPrimitives.TreeLinkMinStrokeWidth, Math.Min(ChartVisualPrimitives.TreeLinkMaxStrokeWidth, ChartVisualPrimitives.TreeLinkMinStrokeWidth + link.Value / Math.Max(0.000001, model.MaxLinkValue) * ChartVisualPrimitives.TreeLinkStrokeWidthRange));
        var path = "M " + F(x0) + " " + F(y0) + " C " + F(x0 + gap) + " " + F(y0) + " " + F(x1 - gap) + " " + F(y1) + " " + F(x1) + " " + F(y1);
        writer
            .StartElement("path")
            .Attribute("data-cfx-role", "tree-link")
            .Attribute("data-cfx-parent", link.Parent)
            .Attribute("data-cfx-child", link.Child)
            .Attribute("data-cfx-value", link.Value)
            .Attribute("data-cfx-parent-label", parent.Label)
            .Attribute("data-cfx-child-label", child.Label)
            .Attribute("d", path)
            .Attribute("fill", "none")
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-opacity", ChartVisualPrimitives.TreeLinkStrokeOpacity)
            .Attribute("stroke-width", width)
            .Attribute("stroke-linecap", "round")
            .EndEmptyElement()
            .Line();
    }

    private static TreeModel BuildTreeModel(Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.Tree);
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
        var maxLinkValue = links.Max(link => link.Value);
        return new TreeModel(nodes, links, nodeWidth, nodeHeight, maxDepth, maxLinkValue);
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
            node.X = maxDepth == 0 ? plot.Left + plot.Width / 2 - nodeWidth / 2 : plot.Left + node.Depth / (double)maxDepth * (plot.Width - nodeWidth);
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

    private static void DrawTreeNodeLabel(SvgMarkupWriter writer, Chart chart, TreeNode node, TreeModel model, ChartColor labelColor) {
        var fontSize = TreeNodeLabelFontSize(chart.Options.Theme.TickLabelFontSize);
        var maxWidth = model.NodeWidth - ChartVisualPrimitives.TreeNodeLabelHorizontalPadding * 2;
        var lines = TreeNodeLabelLines(node.Label, fontSize, maxWidth);
        var lineHeight = fontSize * ChartVisualPrimitives.TreeNodeLabelLineHeightFactor;
        var firstY = node.Y + model.NodeHeight / 2 - (lines.Length - 1) * lineHeight / 2;
        for (var i = 0; i < lines.Length; i++) {
            DrawTreeNodeLabelLine(writer, chart, lines[i], node.X + model.NodeWidth / 2, firstY + i * lineHeight, labelColor, fontSize, maxWidth);
        }
    }

    private static void DrawTreeNodeLabelLine(SvgMarkupWriter writer, Chart chart, string text, double centerX, double y, ChartColor labelColor, double fontSize, double maxWidth) {
        var fittedFontSize = TextFontSizeForSvgWidth(text, Math.Max(8, maxWidth), fontSize);
        var fittedText = TrimSvgLabelToWidth(text, fittedFontSize, Math.Max(8, maxWidth));
        if (fittedText.Length == 0) return;

        writer
            .StartElement("text")
            .Attribute("data-cfx-role", "tree-node-label")
            .Attribute("x", centerX)
            .Attribute("y", y)
            .Attribute("text-anchor", "middle")
            .Attribute("dominant-baseline", "middle")
            .Attribute("fill", labelColor.ToCss())
            .Attribute("stroke", TreeLabelHalo(labelColor).ToCss())
            .Attribute("stroke-width", ChartVisualPrimitives.TreeLabelStrokeWidth)
            .Attribute("paint-order", "stroke fill")
            .Attribute("stroke-linejoin", "round")
            .Attribute("font-family", SvgFontFamily(chart.Options.Theme.FontFamily))
            .Attribute("font-size", fittedFontSize)
            .Attribute("font-weight", "800")
            .Text(fittedText)
            .EndElement()
            .Line();
    }

    private static string[] TreeNodeLabelLines(string label, double fontSize, double maxWidth) {
        if (EstimateTextWidth(label, fontSize) <= maxWidth || !label.Contains(' ')) return new[] { label };
        var words = label.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2) return new[] { label };
        var bestSplit = 1;
        var bestScore = double.PositiveInfinity;
        for (var split = 1; split < words.Length; split++) {
            var first = string.Join(" ", words, 0, split);
            var second = string.Join(" ", words, split, words.Length - split);
            var score = Math.Max(EstimateTextWidth(first, fontSize), EstimateTextWidth(second, fontSize)) + Math.Abs(first.Length - second.Length) * 0.25;
            if (score >= bestScore) continue;
            bestScore = score;
            bestSplit = split;
        }

        return new[] { string.Join(" ", words, 0, bestSplit), string.Join(" ", words, bestSplit, words.Length - bestSplit) };
    }

    private static ChartColor TreeLabelHalo(ChartColor labelColor) {
        var luminance = (0.2126 * labelColor.R + 0.7152 * labelColor.G + 0.0722 * labelColor.B) / 255.0;
        return luminance > 0.70 ? ChartColor.FromRgb(15, 23, 42) : ChartColor.White;
    }

    private static ChartColor TreeNodeGradientTop(ChartColor color) => Blend(ChartColor.White, color, ChartVisualPrimitives.TreeNodeGradientTopBlend);

    private static ChartColor TreeNodeGradientBottom(ChartColor color) => Blend(ChartColor.Black, color, ChartVisualPrimitives.TreeNodeGradientBottomBlend);

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
