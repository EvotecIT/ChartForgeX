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

        var mindMapChildren = MindMapChildren(chart, nodes);
        AddDetachedMindMapRoots(root, chart, nodes, mindMapChildren);
        var children = PruneMindMapChildren(root, mindMapChildren);
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
        var metadataRoots = MindMapMetadataRoots(chart, nodes);
        if (metadataRoots.Count > 0) return metadataRoots.FirstOrDefault(node => node.Kind == TopologyNodeKind.Hub) ?? metadataRoots[0];
        var roots = MindMapSourceFreeNodes(chart, nodes);
        if (roots.Count > 0) return roots.FirstOrDefault(node => node.Kind == TopologyNodeKind.Hub) ?? roots[0];
        return nodes.Count == 0 ? null : chart.Nodes.OrderBy(node => node.Id, StringComparer.Ordinal).First();
    }

    private static List<TopologyNode> MindMapMetadataRoots(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes) {
        var participants = new HashSet<string>(StringComparer.Ordinal);
        var childTargets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (!node.Metadata.TryGetValue("mindmap.parentId", out var parentId) || !nodes.ContainsKey(parentId)) continue;
            participants.Add(parentId);
            participants.Add(node.Id);
            childTargets.Add(node.Id);
        }

        return chart.Nodes
            .Where(node => participants.Contains(node.Id) && !childTargets.Contains(node.Id))
            .OrderBy(node => node.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static List<TopologyNode> MindMapSourceFreeNodes(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes) {
        var targets = new HashSet<string>(StringComparer.Ordinal);
        var metadataParticipants = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (!node.Metadata.TryGetValue("mindmap.parentId", out var parentId) || !nodes.ContainsKey(parentId)) continue;
            targets.Add(node.Id);
            metadataParticipants.Add(node.Id);
            metadataParticipants.Add(parentId);
        }

        foreach (var edge in chart.Edges) {
            if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;
            if (metadataParticipants.Contains(edge.SourceNodeId) || metadataParticipants.Contains(edge.TargetNodeId)) continue;
            targets.Add(edge.TargetNodeId);
        }

        return chart.Nodes.Where(node => !targets.Contains(node.Id)).OrderBy(node => node.Id, StringComparer.Ordinal).ToList();
    }

    private static Dictionary<string, List<TopologyNode>> MindMapChildren(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes) {
        var children = new Dictionary<string, List<TopologyNode>>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (node.Metadata.TryGetValue("mindmap.parentId", out var parentId) && nodes.ContainsKey(parentId)) AddMindMapChild(children, parentId, node);
        }

        foreach (var edge in chart.Edges) {
            if (!nodes.TryGetValue(edge.SourceNodeId, out _) || !nodes.TryGetValue(edge.TargetNodeId, out var target)) continue;
            if (target.Metadata.TryGetValue("mindmap.parentId", out var parentId) && nodes.ContainsKey(parentId) && !string.Equals(parentId, edge.SourceNodeId, StringComparison.Ordinal)) continue;
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

    private static void AddDetachedMindMapRoots(TopologyNode root, TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes, IDictionary<string, List<TopologyNode>> children) {
        foreach (var sourceRoot in MindMapSourceFreeNodes(chart, nodes)) {
            if (string.Equals(sourceRoot.Id, root.Id, StringComparison.Ordinal)) continue;
            AddMindMapChild(children, root.Id, sourceRoot);
        }

        var reachable = MindMapReachableNodeIds(root, children);
        foreach (var detached in chart.Nodes.OrderBy(node => node.Id, StringComparer.Ordinal)) {
            if (reachable.Contains(detached.Id)) continue;
            AddMindMapChild(children, root.Id, detached);
            foreach (var id in MindMapReachableNodeIds(detached, children)) reachable.Add(id);
        }
    }

    private static void AddMindMapChild(IDictionary<string, List<TopologyNode>> children, string parentId, TopologyNode child) {
        if (!children.TryGetValue(parentId, out var list)) {
            list = new List<TopologyNode>();
            children[parentId] = list;
        }

        list.Add(child);
    }

    private static HashSet<string> MindMapReachableNodeIds(TopologyNode root, IDictionary<string, List<TopologyNode>> children) {
        var result = new HashSet<string>(StringComparer.Ordinal) { root.Id };
        var stack = new Stack<TopologyNode>();
        stack.Push(root);

        while (stack.Count > 0) {
            var parent = stack.Pop();
            if (!children.TryGetValue(parent.Id, out var childNodes)) continue;
            foreach (var child in childNodes) {
                if (!result.Add(child.Id)) continue;
                stack.Push(child);
            }
        }

        return result;
    }

    private static Dictionary<string, List<TopologyNode>> PruneMindMapChildren(TopologyNode root, IReadOnlyDictionary<string, List<TopologyNode>> children) {
        var result = new Dictionary<string, List<TopologyNode>>(StringComparer.Ordinal);
        var path = new HashSet<string>(StringComparer.Ordinal);
        var placed = new HashSet<string>(StringComparer.Ordinal) { root.Id };
        PruneMindMapChildren(root, children, result, path, placed);
        return result;
    }

    private static void PruneMindMapChildren(TopologyNode parent, IReadOnlyDictionary<string, List<TopologyNode>> children, Dictionary<string, List<TopologyNode>> result, HashSet<string> path, HashSet<string> placed) {
        if (!path.Add(parent.Id)) return;
        if (!children.TryGetValue(parent.Id, out var childNodes)) {
            path.Remove(parent.Id);
            return;
        }

        foreach (var child in childNodes) {
            if (path.Contains(child.Id) || !placed.Add(child.Id)) continue;
            AddMindMapChild(result, parent.Id, child);
            PruneMindMapChildren(child, children, result, path, placed);
        }

        path.Remove(parent.Id);
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
            var targetDelta = CenterX(target) - CenterX(source);
            TopologyEdgePort sourcePort;
            TopologyEdgePort targetPort;
            if (targetDelta < -0.001 || (Math.Abs(targetDelta) <= 0.001 && targetSide < 0)) {
                sourcePort = TopologyEdgePort.Left;
                targetPort = TopologyEdgePort.Right;
            } else {
                sourcePort = TopologyEdgePort.Right;
                targetPort = TopologyEdgePort.Left;
            }

            var inference = TopologyEdgeLayoutInference.None;
            if (edge.SourcePort == TopologyEdgePort.Auto) {
                edge.SourcePort = sourcePort;
                inference |= TopologyEdgeLayoutInference.SourcePort;
            }

            if (edge.TargetPort == TopologyEdgePort.Auto) {
                edge.TargetPort = targetPort;
                inference |= TopologyEdgeLayoutInference.TargetPort;
            }

            edge.LayoutInference |= inference;
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
