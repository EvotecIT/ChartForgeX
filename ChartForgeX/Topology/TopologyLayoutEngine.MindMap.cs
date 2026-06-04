using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private const double MindMapRootBranchGap = 112;
    private const double MindMapColumnGap = 54;
    private const double MindMapSiblingGap = 18;

    private static void ApplyMindMap(TopologyChart chart) {
        if (chart.Nodes.Count == 0) return;
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var root = MindMapRoot(chart, nodes);
        if (root == null) {
            ApplyMatrix(chart);
            return;
        }

        var children = MindMapChildren(chart, nodes);
        var levels = MindMapLevels(root, children);
        var maxWidthByLevel = MindMapMaxWidthByLevel(chart.Nodes, levels);
        var pad = Math.Max(24, chart.Viewport.Padding);
        var titleOffset = string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72;
        var legendOffset = TopologyRenderPrimitives.LegendReservedHeight(chart.Legend, chart.Viewport);
        var top = pad + titleOffset;
        var availableH = Math.Max(root.Height, chart.Viewport.Height - top - pad - legendOffset);
        root.X = chart.Viewport.Width / 2 - root.Width / 2;
        root.Y = top + (availableH - root.Height) / 2;
        root.Metadata["mindmap.side"] = "center";

        var rootChildren = MindMapOrderedChildren(root, children);
        var left = new List<TopologyNode>();
        var right = new List<TopologyNode>();
        for (var i = 0; i < rootChildren.Count; i++) {
            var child = rootChildren[i];
            var side = MindMapRequestedSide(child);
            if (side == "left") left.Add(child);
            else if (side == "right") right.Add(child);
            else if (right.Count <= left.Count) right.Add(child);
            else left.Add(child);
        }

        PlaceMindMapSide(root, right, 1, top, availableH, 1, children, maxWidthByLevel);
        PlaceMindMapSide(root, left, -1, top, availableH, 1, children, maxWidthByLevel);
        ApplyMindMapEdgePorts(chart, nodes);
    }

    private static TopologyNode? MindMapRoot(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes) {
        var explicitRoot = chart.Nodes.FirstOrDefault(node => node.Metadata.TryGetValue("mindmap.root", out var value) && value.Equals("true", StringComparison.OrdinalIgnoreCase));
        if (explicitRoot != null) return explicitRoot;
        var targets = new HashSet<string>(chart.Edges.Select(edge => edge.TargetNodeId), StringComparer.Ordinal);
        var roots = chart.Nodes.Where(node => !targets.Contains(node.Id)).OrderBy(node => node.Id, StringComparer.Ordinal).ToList();
        if (roots.Count > 0) return roots.FirstOrDefault(node => node.Kind == TopologyNodeKind.Hub) ?? roots[0];
        return nodes.Count == 0 ? null : chart.Nodes.OrderBy(node => node.Id, StringComparer.Ordinal).First();
    }

    private static Dictionary<string, List<TopologyNode>> MindMapChildren(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes) {
        var children = new Dictionary<string, List<TopologyNode>>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (node.Metadata.TryGetValue("mindmap.parentId", out var parentId) && nodes.ContainsKey(parentId)) AddMindMapChild(children, parentId, node);
        }

        foreach (var edge in chart.Edges) {
            if (!nodes.TryGetValue(edge.SourceNodeId, out _) || !nodes.TryGetValue(edge.TargetNodeId, out var target)) continue;
            if (target.Metadata.TryGetValue("mindmap.parentId", out var parentId) && !string.Equals(parentId, edge.SourceNodeId, StringComparison.Ordinal)) continue;
            AddMindMapChild(children, edge.SourceNodeId, target);
        }

        foreach (var pair in children.ToList()) {
            children[pair.Key] = pair.Value
                .GroupBy(node => node.Id, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(MindMapOrder)
                .ThenBy(node => node.Id, StringComparer.Ordinal)
                .ToList();
        }

        return children;
    }

    private static void AddMindMapChild(IDictionary<string, List<TopologyNode>> children, string parentId, TopologyNode child) {
        if (!children.TryGetValue(parentId, out var list)) {
            list = new List<TopologyNode>();
            children[parentId] = list;
        }

        list.Add(child);
    }

    private static Dictionary<string, int> MindMapLevels(TopologyNode root, IReadOnlyDictionary<string, List<TopologyNode>> children) {
        var levels = new Dictionary<string, int>(StringComparer.Ordinal) { [root.Id] = 0 };
        var queue = new Queue<TopologyNode>();
        queue.Enqueue(root);
        while (queue.Count > 0) {
            var parent = queue.Dequeue();
            var level = levels[parent.Id] + 1;
            if (!children.TryGetValue(parent.Id, out var childNodes)) continue;
            foreach (var child in childNodes) {
                if (levels.ContainsKey(child.Id)) continue;
                levels[child.Id] = level;
                queue.Enqueue(child);
            }
        }

        return levels;
    }

    private static Dictionary<int, double> MindMapMaxWidthByLevel(IEnumerable<TopologyNode> nodes, IReadOnlyDictionary<string, int> levels) {
        var result = new Dictionary<int, double>();
        foreach (var node in nodes) {
            var level = levels.TryGetValue(node.Id, out var found) ? found : MindMapNodeLevel(node);
            result[level] = Math.Max(result.TryGetValue(level, out var existing) ? existing : 0, node.Width);
        }

        return result;
    }

    private static void PlaceMindMapSide(TopologyNode root, List<TopologyNode> branches, int side, double top, double availableH, int level, IReadOnlyDictionary<string, List<TopologyNode>> children, IReadOnlyDictionary<int, double> maxWidthByLevel) {
        if (branches.Count == 0) return;
        var blocks = branches.Select(branch => new MindMapBlock(branch, MindMapSubtreeHeight(branch, children))).ToList();
        var total = blocks.Sum(block => block.Height) + Math.Max(0, blocks.Count - 1) * MindMapSiblingGap;
        var y = top + Math.Max(0, (availableH - total) / 2);
        foreach (var block in blocks) {
            PlaceMindMapSubtree(root, block.Node, side, level, y, block.Height, children, maxWidthByLevel);
            y += block.Height + MindMapSiblingGap;
        }
    }

    private static double MindMapSubtreeHeight(TopologyNode node, IReadOnlyDictionary<string, List<TopologyNode>> children) {
        if (!children.TryGetValue(node.Id, out var childNodes) || childNodes.Count == 0) return node.Height;
        var childHeight = childNodes.Sum(child => MindMapSubtreeHeight(child, children)) + Math.Max(0, childNodes.Count - 1) * MindMapSiblingGap;
        return Math.Max(node.Height, childHeight);
    }

    private static void PlaceMindMapSubtree(TopologyNode root, TopologyNode node, int side, int level, double blockTop, double blockHeight, IReadOnlyDictionary<string, List<TopologyNode>> children, IReadOnlyDictionary<int, double> maxWidthByLevel) {
        var columnX = MindMapColumnX(root, side, level, maxWidthByLevel);
        var columnWidth = maxWidthByLevel.TryGetValue(level, out var found) ? found : node.Width;
        node.X = side > 0 ? columnX : columnX + columnWidth - node.Width;
        node.Y = blockTop + (blockHeight - node.Height) / 2;
        node.Metadata["mindmap.side"] = side > 0 ? "right" : "left";
        node.Metadata["mindmap.level"] = level.ToString(CultureInfo.InvariantCulture);

        if (!children.TryGetValue(node.Id, out var childNodes) || childNodes.Count == 0) return;
        var total = childNodes.Sum(child => MindMapSubtreeHeight(child, children)) + Math.Max(0, childNodes.Count - 1) * MindMapSiblingGap;
        var y = blockTop + Math.Max(0, (blockHeight - total) / 2);
        foreach (var child in childNodes) {
            var childHeight = MindMapSubtreeHeight(child, children);
            PlaceMindMapSubtree(root, child, side, level + 1, y, childHeight, children, maxWidthByLevel);
            y += childHeight + MindMapSiblingGap;
        }
    }

    private static double MindMapColumnX(TopologyNode root, int side, int level, IReadOnlyDictionary<int, double> maxWidthByLevel) {
        if (side > 0) {
            var x = root.X + root.Width + MindMapRootBranchGap;
            for (var current = 1; current < level; current++) x += MindMapWidth(maxWidthByLevel, current) + MindMapColumnGap;
            return x;
        }

        var right = root.X - MindMapRootBranchGap;
        for (var current = 1; current < level; current++) right -= MindMapWidth(maxWidthByLevel, current) + MindMapColumnGap;
        return right - MindMapWidth(maxWidthByLevel, level);
    }

    private static double MindMapWidth(IReadOnlyDictionary<int, double> maxWidthByLevel, int level) =>
        maxWidthByLevel.TryGetValue(level, out var width) ? width : 120;

    private static void ApplyMindMapEdgePorts(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes) {
        foreach (var edge in chart.Edges) {
            if (!nodes.TryGetValue(edge.SourceNodeId, out var source) || !nodes.TryGetValue(edge.TargetNodeId, out var target)) continue;
            var targetSide = MindMapNodeSide(target);
            if (targetSide < 0 || (targetSide == 0 && CenterX(target) < CenterX(source))) {
                edge.SourcePort = TopologyEdgePort.Left;
                edge.TargetPort = TopologyEdgePort.Right;
            } else {
                edge.SourcePort = TopologyEdgePort.Right;
                edge.TargetPort = TopologyEdgePort.Left;
            }

            if (edge.Routing == TopologyEdgeRouting.Curved) edge.Routing = TopologyEdgeRouting.Orthogonal;
            edge.LayoutInference |= TopologyEdgeLayoutInference.SourcePort | TopologyEdgeLayoutInference.TargetPort;
        }
    }

    private static List<TopologyNode> MindMapOrderedChildren(TopologyNode parent, IReadOnlyDictionary<string, List<TopologyNode>> children) =>
        children.TryGetValue(parent.Id, out var childNodes) ? childNodes : new List<TopologyNode>();

    private static string? MindMapRequestedSide(TopologyNode node) =>
        node.Metadata.TryGetValue("mindmap.side", out var value) ? value.Trim().ToLowerInvariant() : null;

    private static int MindMapNodeSide(TopologyNode node) =>
        MindMapRequestedSide(node) == "left" ? -1 : MindMapRequestedSide(node) == "right" ? 1 : 0;

    private static int MindMapNodeLevel(TopologyNode node) =>
        node.Metadata.TryGetValue("mindmap.level", out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) ? level : 1;

    private static int MindMapOrder(TopologyNode node) =>
        node.Metadata.TryGetValue("mindmap.order", out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var order) ? order : 0;

    private readonly struct MindMapBlock {
        public MindMapBlock(TopologyNode node, double height) {
            Node = node;
            Height = Math.Max(node.Height, height);
        }

        public readonly TopologyNode Node;
        public readonly double Height;
    }
}
