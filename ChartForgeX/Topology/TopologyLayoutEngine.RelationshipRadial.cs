using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private sealed class RadialNodeState {
        public int Depth { get; set; } = int.MaxValue;
        public string? ParentId { get; set; }
        public string? FirstHopId { get; set; }
        public double Angle { get; set; }
        public bool Overflow { get; set; }
    }

    private sealed class RadialEdgeLink {
        public RadialEdgeLink(TopologyEdge edge, string neighborId) {
            Edge = edge;
            NeighborId = neighborId;
        }

        public TopologyEdge Edge { get; }
        public string NeighborId { get; }
    }

    private static void ApplyRelationshipRadial(TopologyChart chart, TopologyRenderOptions? options) {
        if (chart.Nodes.Count == 0) return;
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var adjacency = BuildRadialAdjacency(chart, nodes);
        var rootId = ResolveRelationshipRoot(chart, adjacency, options);
        if (string.IsNullOrWhiteSpace(rootId) || !nodes.ContainsKey(rootId)) return;

        var maxDepth = Math.Max(1, Math.Min(5, options?.RelationshipRadialMaxDepth ?? 2));
        var maxFanout = Math.Max(1, Math.Min(72, options?.RelationshipRadialMaxFanout ?? 18));
        var states = BuildRadialStates(rootId, adjacency, maxDepth, maxFanout);
        ApplyRadialAngles(rootId, states);

        var pad = Math.Max(24, chart.Viewport.Padding);
        var titleOffset = string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72;
        var legendOffset = LegendReservedHeight(chart.Legend, chart.Viewport);
        var left = pad;
        var right = Math.Max(left + 80, chart.Viewport.Width - pad);
        var top = pad + titleOffset;
        var bottom = Math.Max(top + 80, chart.Viewport.Height - pad - legendOffset);
        var centerX = (left + right) / 2;
        var centerY = (top + bottom) / 2;
        var ringGap = Math.Max(88, Math.Min(right - left, bottom - top) / (maxDepth + 2.15));
        var overflowRing = ringGap * (maxDepth + 1);

        foreach (var node in chart.Nodes) {
            if (!states.TryGetValue(node.Id, out var state)) {
                state = new RadialNodeState { Depth = maxDepth + 1, Overflow = true, Angle = StableRadialAngle(node.Id) };
                states[node.Id] = state;
            }

            var radius = state.Depth == 0 ? 0 : state.Overflow ? overflowRing : ringGap * state.Depth;
            var x = centerX + Math.Cos(state.Angle) * radius;
            var y = centerY + Math.Sin(state.Angle) * radius;
            node.X = ClampRadial(x - node.Width / 2, left, right - node.Width);
            node.Y = ClampRadial(y - node.Height / 2, top, bottom - node.Height);
            node.Metadata["layout.radial.root"] = rootId;
            node.Metadata["layout.radial.depth"] = state.Depth.ToString(CultureInfo.InvariantCulture);
            node.Metadata["layout.radial.angle"] = state.Angle.ToString("0.###", CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(state.ParentId)) node.Metadata["layout.radial.parent"] = state.ParentId!;
            if (!string.IsNullOrWhiteSpace(state.FirstHopId)) node.Metadata["layout.radial.firstHop"] = state.FirstHopId!;
            if (state.Overflow) node.Metadata["layout.radial.overflow"] = "true";
        }

        foreach (var edge in chart.Edges) {
            edge.Routing = TopologyEdgeRouting.Straight;
            edge.Metadata["layout.radial"] = "true";
            edge.Metadata["layout.radial.root"] = rootId;
            if (states.TryGetValue(edge.SourceNodeId, out var source) && states.TryGetValue(edge.TargetNodeId, out var target)) {
                edge.Metadata["layout.radial.depth"] = Math.Min(source.Depth, target.Depth).ToString(CultureInfo.InvariantCulture);
                if (source.Overflow || target.Overflow) edge.Metadata["layout.radial.overflow"] = "true";
            }
        }

        ApplyRadialGroupBounds(chart, left, top, right, bottom);
    }

    private static Dictionary<string, List<RadialEdgeLink>> BuildRadialAdjacency(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes) {
        var adjacency = nodes.Keys.ToDictionary(id => id, _ => new List<RadialEdgeLink>(), StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            if (!adjacency.ContainsKey(edge.SourceNodeId) || !adjacency.ContainsKey(edge.TargetNodeId)) continue;
            adjacency[edge.SourceNodeId].Add(new RadialEdgeLink(edge, edge.TargetNodeId));
            adjacency[edge.TargetNodeId].Add(new RadialEdgeLink(edge, edge.SourceNodeId));
        }

        return adjacency;
    }

    private static string ResolveRelationshipRoot(TopologyChart chart, IReadOnlyDictionary<string, List<RadialEdgeLink>> adjacency, TopologyRenderOptions? options) {
        var requestedRoot = options?.RelationshipRootNodeId;
        if (!string.IsNullOrWhiteSpace(requestedRoot) && adjacency.ContainsKey(requestedRoot!)) return requestedRoot!;
        var selectedNodeIds = options == null ? Enumerable.Empty<string>() : options.SelectedNodeIds;
        foreach (var selected in selectedNodeIds) {
            if (adjacency.ContainsKey(selected)) return selected;
        }

        return chart.Nodes
            .OrderByDescending(node => adjacency.TryGetValue(node.Id, out var links) ? links.Count : 0)
            .ThenBy(node => node.Id, StringComparer.Ordinal)
            .First()
            .Id;
    }

    private static Dictionary<string, RadialNodeState> BuildRadialStates(string rootId, IReadOnlyDictionary<string, List<RadialEdgeLink>> adjacency, int maxDepth, int maxFanout) {
        var states = new Dictionary<string, RadialNodeState>(StringComparer.Ordinal) {
            [rootId] = new RadialNodeState { Depth = 0, FirstHopId = rootId }
        };
        var queue = new Queue<string>();
        queue.Enqueue(rootId);
        while (queue.Count > 0) {
            var current = queue.Dequeue();
            var state = states[current];
            if (state.Depth >= maxDepth) continue;
            var expanded = adjacency[current]
                .OrderByDescending(link => RadialStatusPriority(link.Edge.Status))
                .ThenByDescending(link => link.Edge.Emphasis)
                .ThenBy(link => link.NeighborId, StringComparer.Ordinal)
                .Take(maxFanout)
                .ToList();
            foreach (var link in expanded) {
                if (states.ContainsKey(link.NeighborId)) continue;
                var depth = state.Depth + 1;
                var firstHopId = state.Depth == 0 ? link.NeighborId : state.FirstHopId;
                states[link.NeighborId] = new RadialNodeState { Depth = depth, ParentId = current, FirstHopId = firstHopId };
                queue.Enqueue(link.NeighborId);
            }
        }

        return states;
    }

    private static void ApplyRadialAngles(string rootId, IDictionary<string, RadialNodeState> states) {
        var firstHopIds = states
            .Where(pair => pair.Value.Depth == 1)
            .Select(pair => pair.Key)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        for (var i = 0; i < firstHopIds.Count; i++) {
            states[firstHopIds[i]].Angle = -Math.PI / 2 + Math.PI * 2 * i / Math.Max(1, firstHopIds.Count);
        }

        foreach (var branch in firstHopIds) {
            var descendants = states
                .Where(pair => pair.Value.Depth > 1 && string.Equals(pair.Value.FirstHopId, branch, StringComparison.Ordinal))
                .OrderBy(pair => pair.Value.Depth)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .ToList();
            var baseAngle = states[branch].Angle;
            for (var i = 0; i < descendants.Count; i++) {
                var spread = Math.Min(Math.PI / 2.8, 0.22 + descendants.Count * 0.018);
                var offset = descendants.Count == 1 ? 0 : -spread / 2 + spread * i / (descendants.Count - 1);
                descendants[i].Value.Angle = baseAngle + offset;
            }
        }

        states[rootId].Angle = 0;
    }

    private static void ApplyRadialGroupBounds(TopologyChart chart, double left, double top, double right, double bottom) {
        foreach (var group in chart.Groups) {
            var groupNodes = chart.Nodes.Where(node => string.Equals(node.GroupId, group.Id, StringComparison.Ordinal)).ToList();
            if (groupNodes.Count == 0) continue;
            var minX = groupNodes.Min(node => node.X);
            var minY = groupNodes.Min(node => node.Y);
            var maxX = groupNodes.Max(node => node.X + node.Width);
            var maxY = groupNodes.Max(node => node.Y + node.Height);
            group.X = ClampRadial(minX - 28, left, right);
            group.Y = ClampRadial(minY - 46, top, bottom);
            group.Width = Math.Min(right - group.X, Math.Max(120, maxX - minX + 56));
            group.Height = Math.Min(bottom - group.Y, Math.Max(92, maxY - minY + 76));
            group.Metadata["layout.radial.nodeCount"] = groupNodes.Count.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static int RadialStatusPriority(TopologyHealthStatus status) => status switch {
        TopologyHealthStatus.Critical => 5,
        TopologyHealthStatus.Warning => 4,
        TopologyHealthStatus.Unknown => 3,
        TopologyHealthStatus.Disabled => 2,
        _ => 1
    };

    private static double StableRadialAngle(string value) {
        unchecked {
            var hash = 2166136261u;
            foreach (var ch in value) {
                hash ^= ch;
                hash *= 16777619;
            }

            return Math.PI * 2 * ((hash & 0xFFFFFF) / (double)0x1000000);
        }
    }

    private static double ClampRadial(double value, double min, double max) {
        if (max < min) return min;
        return value < min ? min : value > max ? max : value;
    }
}
