using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static class TopologyLayoutEngine {
    public static TopologyChart Prepare(TopologyChart chart, TopologyView? view = null, TopologyRenderOptions? options = null) {
        var copy = Clone(chart);
        if (options != null) {
            ApplyNodeDisplayMode(copy, options.NodeDisplayMode);
        }

        if (view != null) ApplyView(copy, view);
        if (options != null) copy.Legend = TopologyLegend.Resolve(copy, options.LegendMode);
        switch (copy.LayoutMode) {
            case TopologyLayoutMode.GroupGrid:
                ApplyGroupGrid(copy);
                break;
            case TopologyLayoutMode.HubAndSpoke:
                ApplyHubAndSpoke(copy);
                break;
            case TopologyLayoutMode.Layered:
                ApplyLayered(copy);
                break;
            case TopologyLayoutMode.Matrix:
                ApplyMatrix(copy);
                break;
        }

        TopologyLayoutNormalizer.Normalize(copy);
        return copy;
    }

    private static void ApplyNodeDisplayMode(TopologyChart chart, TopologyNodeDisplayMode displayMode) {
        foreach (var node in chart.Nodes) {
            switch (node.DisplayMode ?? displayMode) {
                case TopologyNodeDisplayMode.CompactCard:
                    node.Width = Math.Min(node.Width, 108);
                    node.Height = Math.Min(node.Height, 52);
                    break;
                case TopologyNodeDisplayMode.Tile:
                    node.Width = Math.Min(node.Width, 64);
                    node.Height = Math.Min(node.Height, 46);
                    break;
                case TopologyNodeDisplayMode.Pill:
                    node.Width = Math.Min(node.Width, 112);
                    node.Height = Math.Min(node.Height, 34);
                    break;
                case TopologyNodeDisplayMode.Icon:
                    node.Width = 44;
                    node.Height = 44;
                    break;
                case TopologyNodeDisplayMode.Dot:
                    node.Width = 22;
                    node.Height = 22;
                    break;
            }
        }
    }

    private static void ApplyView(TopologyChart chart, TopologyView view) {
        if (!string.IsNullOrWhiteSpace(view.Id)) chart.Id = string.IsNullOrWhiteSpace(chart.Id) ? view.Id : chart.Id + "-" + view.Id;
        if (!string.IsNullOrWhiteSpace(view.Title)) chart.Title = view.Title;
        if (!string.IsNullOrWhiteSpace(view.Subtitle)) chart.Subtitle = view.Subtitle;

        var selectedGroupIds = new HashSet<string>(view.GroupIds, StringComparer.Ordinal);
        var selectedNodeIds = new HashSet<string>(view.NodeIds, StringComparer.Ordinal);
        var selectedEdgeIds = new HashSet<string>(view.EdgeIds, StringComparer.Ordinal);
        var focusNodeIds = new HashSet<string>(view.FocusNodeIds, StringComparer.Ordinal);
        var hasGroupFilter = selectedGroupIds.Count > 0;
        var hasNodeFilter = selectedNodeIds.Count > 0;
        var hasEdgeFilter = selectedEdgeIds.Count > 0;
        var hasFocusFilter = focusNodeIds.Count > 0;
        var hasNodeKindFilter = view.NodeKinds.Count > 0;
        var hasEdgeKindFilter = view.EdgeKinds.Count > 0;
        var hasStatusFilter = view.HealthStatuses.Count > 0;
        if (!hasGroupFilter && !hasNodeFilter && !hasEdgeFilter && !hasFocusFilter && !hasNodeKindFilter && !hasEdgeKindFilter && !hasStatusFilter) return;

        var baseNodes = chart.Nodes
            .Where(node => IsNodeEligible(node, selectedGroupIds, hasGroupFilter, selectedNodeIds, hasNodeFilter, view, false))
            .ToList();
        var baseNodeIds = new HashSet<string>(baseNodes.Select(node => node.Id), StringComparer.Ordinal);
        var eligibleEdges = chart.Edges
            .Where(edge => IsEdgeEligible(edge, selectedEdgeIds, hasEdgeFilter, view))
            .Where(edge => baseNodeIds.Contains(edge.SourceNodeId) && baseNodeIds.Contains(edge.TargetNodeId))
            .ToList();

        List<TopologyNode> visibleNodes;
        List<TopologyEdge> visibleEdges;
        if (hasFocusFilter) {
            var focused = ExpandFocusNodes(focusNodeIds, baseNodeIds, eligibleEdges, Math.Max(0, view.NeighborDepth), view.IncludeIncomingEdges, view.IncludeOutgoingEdges);
            visibleNodes = baseNodes.Where(node => focused.Contains(node.Id)).ToList();
            visibleEdges = eligibleEdges.Where(edge => focused.Contains(edge.SourceNodeId) && focused.Contains(edge.TargetNodeId)).ToList();
        } else {
            var visibleNodeIds = new HashSet<string>(baseNodes.Where(node => !hasStatusFilter || view.HealthStatuses.Contains(node.Status)).Select(node => node.Id), StringComparer.Ordinal);
            if (hasEdgeFilter || hasEdgeKindFilter || hasStatusFilter) {
                foreach (var edge in eligibleEdges) {
                    visibleNodeIds.Add(edge.SourceNodeId);
                    visibleNodeIds.Add(edge.TargetNodeId);
                }
            }

            visibleNodes = baseNodes.Where(node => visibleNodeIds.Contains(node.Id)).ToList();
            visibleEdges = hasEdgeFilter || hasEdgeKindFilter || hasStatusFilter
                ? eligibleEdges.Where(edge => visibleNodeIds.Contains(edge.SourceNodeId) && visibleNodeIds.Contains(edge.TargetNodeId)).ToList()
                : view.IncludeConnectedEdges
                    ? chart.Edges.Where(edge => visibleNodeIds.Contains(edge.SourceNodeId) && visibleNodeIds.Contains(edge.TargetNodeId)).ToList()
                    : new List<TopologyEdge>();
        }

        var visibleGroupIds = new HashSet<string>(chart.Groups
            .Where(group => (!hasGroupFilter || selectedGroupIds.Contains(group.Id)) && (!hasStatusFilter || view.HealthStatuses.Contains(group.Status)))
            .Select(group => group.Id), StringComparer.Ordinal);
        if (view.IncludeNodeGroups) {
            foreach (var groupId in visibleNodes.Select(node => node.GroupId).Where(groupId => !string.IsNullOrWhiteSpace(groupId))) visibleGroupIds.Add(groupId!);
        }

        var allGroups = chart.Groups.Select(Clone).ToList();
        chart.Groups.Clear();
        foreach (var group in allGroups.Where(group => visibleGroupIds.Contains(group.Id))) chart.Groups.Add(group);
        chart.Nodes.Clear();
        chart.Nodes.AddRange(visibleNodes);
        chart.Edges.Clear();
        chart.Edges.AddRange(visibleEdges);
    }

    private static bool IsNodeEligible(TopologyNode node, HashSet<string> selectedGroupIds, bool hasGroupFilter, HashSet<string> selectedNodeIds, bool hasNodeFilter, TopologyView view, bool includeStatus) {
        if (hasNodeFilter && !selectedNodeIds.Contains(node.Id)) return false;
        if (hasGroupFilter && (string.IsNullOrWhiteSpace(node.GroupId) || !selectedGroupIds.Contains(node.GroupId!))) return false;
        if (view.NodeKinds.Count > 0 && !view.NodeKinds.Contains(node.Kind)) return false;
        if (includeStatus && view.HealthStatuses.Count > 0 && !view.HealthStatuses.Contains(node.Status)) return false;
        return true;
    }

    private static bool IsEdgeEligible(TopologyEdge edge, HashSet<string> selectedEdgeIds, bool hasEdgeFilter, TopologyView view) {
        if (hasEdgeFilter && !selectedEdgeIds.Contains(edge.Id)) return false;
        if (view.EdgeKinds.Count > 0 && !view.EdgeKinds.Contains(edge.Kind)) return false;
        if (view.HealthStatuses.Count > 0 && !view.HealthStatuses.Contains(edge.Status)) return false;
        return true;
    }

    private static HashSet<string> ExpandFocusNodes(HashSet<string> focusNodeIds, HashSet<string> eligibleNodeIds, List<TopologyEdge> eligibleEdges, int depth, bool includeIncoming, bool includeOutgoing) {
        var visible = new HashSet<string>(focusNodeIds.Where(eligibleNodeIds.Contains), StringComparer.Ordinal);
        var frontier = new HashSet<string>(visible, StringComparer.Ordinal);
        for (var hop = 0; hop < depth && frontier.Count > 0; hop++) {
            var next = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in eligibleEdges) {
                if (includeOutgoing && frontier.Contains(edge.SourceNodeId) && eligibleNodeIds.Contains(edge.TargetNodeId)) next.Add(edge.TargetNodeId);
                if (includeIncoming && frontier.Contains(edge.TargetNodeId) && eligibleNodeIds.Contains(edge.SourceNodeId)) next.Add(edge.SourceNodeId);
            }

            next.ExceptWith(visible);
            foreach (var nodeId in next) visible.Add(nodeId);
            frontier = next;
        }

        return visible;
    }

    private static void ApplyGroupGrid(TopologyChart chart) {
        if (chart.Groups.Count == 0) {
            ApplyMatrix(chart);
            return;
        }

        var pad = Math.Max(16, chart.Viewport.Padding);
        var titleOffset = string.IsNullOrWhiteSpace(chart.Title) ? 0 : 64;
        var legendOffset = LegendReservedHeight(chart.Legend);
        var columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(chart.Groups.Count)));
        var rows = (int)Math.Ceiling(chart.Groups.Count / (double)columns);
        var cellW = (chart.Viewport.Width - pad * 2 - (columns - 1) * 28) / columns;
        var cellH = (chart.Viewport.Height - pad * 2 - titleOffset - legendOffset - (rows - 1) * 28) / rows;

        for (var i = 0; i < chart.Groups.Count; i++) {
            var group = chart.Groups[i];
            var col = i % columns;
            var row = i / columns;
            if (group.Width <= 0) group.Width = Math.Max(220, cellW);
            if (group.Height <= 0) group.Height = Math.Max(180, cellH);
            if (IsUnset(group.X) && IsUnset(group.Y)) {
                group.X = pad + col * (cellW + 28);
                group.Y = pad + titleOffset + row * (cellH + 28);
            }

            PlaceNodesInGroup(chart.Nodes.Where(node => string.Equals(node.GroupId, group.Id, StringComparison.Ordinal)).ToList(), group);
        }
    }

    private static void ApplyHubAndSpoke(TopologyChart chart) {
        foreach (var group in chart.Groups) {
            var nodes = chart.Nodes.Where(node => string.Equals(node.GroupId, group.Id, StringComparison.Ordinal)).ToList();
            if (nodes.Count == 0) continue;
            var hub = nodes.FirstOrDefault(node => node.Kind == TopologyNodeKind.Hub || node.Kind == TopologyNodeKind.Location || node.Metadata.ContainsKey("hub")) ?? nodes[0];
            if (IsUnset(hub.X) && IsUnset(hub.Y)) {
                hub.X = group.X + group.Width / 2 - hub.Width / 2;
                hub.Y = group.Y + 72;
            }

            var branches = nodes.Where(node => !ReferenceEquals(node, hub)).ToList();
            var columns = Math.Max(1, Math.Min(4, branches.Count));
            for (var i = 0; i < branches.Count; i++) {
                var node = branches[i];
                if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
                var col = i % columns;
                var row = i / columns;
                var gapX = (group.Width - 48) / columns;
                node.X = group.X + 24 + col * gapX + (gapX - node.Width) / 2;
                node.Y = hub.Y + hub.Height + 62 + row * (node.Height + 38);
            }
        }

        if (chart.Groups.Count == 0) ApplyMatrix(chart);
    }

    private static void ApplyLayered(TopologyChart chart) {
        var nodes = chart.Nodes;
        var layers = nodes.GroupBy(node => GetLayer(node)).OrderBy(group => group.Key).ToList();
        var pad = Math.Max(24, chart.Viewport.Padding);
        var top = pad + (string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72);
        if (chart.LayoutDirection == TopologyLayoutDirection.LeftToRight) {
            ApplyLayeredLeftToRight(chart, layers, pad, top);
            return;
        }

        ApplyLayeredTopToBottom(chart, layers, pad, top);
    }

    private static void ApplyLayeredTopToBottom(TopologyChart chart, IReadOnlyList<IGrouping<int, TopologyNode>> layers, double pad, double top) {
        var availableH = Math.Max(100, chart.Viewport.Height - top - pad - LegendReservedHeight(chart.Legend));
        var layerGap = layers.Count <= 1 ? 0 : availableH / (layers.Count - 1);

        for (var layerIndex = 0; layerIndex < layers.Count; layerIndex++) {
            var layer = layers[layerIndex].OrderBy(node => node.Id, StringComparer.Ordinal).ToList();
            var gap = (chart.Viewport.Width - pad * 2) / Math.Max(1, layer.Count);
            for (var i = 0; i < layer.Count; i++) {
                var node = layer[i];
                if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
                node.X = pad + gap * i + (gap - node.Width) / 2;
                node.Y = top + layerIndex * layerGap - node.Height / 2;
            }
        }
    }

    private static void ApplyLayeredLeftToRight(TopologyChart chart, IReadOnlyList<IGrouping<int, TopologyNode>> layers, double pad, double top) {
        var availableW = Math.Max(120, chart.Viewport.Width - pad * 2);
        var availableH = Math.Max(100, chart.Viewport.Height - top - pad - LegendReservedHeight(chart.Legend));
        var maxNodeWidth = chart.Nodes.Select(node => node.Width).DefaultIfEmpty(120).Max();
        var usableW = Math.Max(0, availableW - maxNodeWidth);
        var layerGap = layers.Count <= 1 ? 0 : usableW / (layers.Count - 1);

        for (var layerIndex = 0; layerIndex < layers.Count; layerIndex++) {
            var layer = layers[layerIndex].OrderBy(node => node.Id, StringComparer.Ordinal).ToList();
            var gap = availableH / Math.Max(1, layer.Count);
            for (var i = 0; i < layer.Count; i++) {
                var node = layer[i];
                if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
                node.X = pad + layerIndex * layerGap + (maxNodeWidth - node.Width) / 2;
                node.Y = top + gap * i + (gap - node.Height) / 2;
            }
        }
    }

    private static void ApplyMatrix(TopologyChart chart) {
        var pad = Math.Max(24, chart.Viewport.Padding);
        var nodes = chart.Nodes.OrderBy(node => node.GroupId ?? string.Empty, StringComparer.Ordinal).ThenBy(node => node.Id, StringComparer.Ordinal).ToList();
        var columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(nodes.Count)));
        var top = pad + (string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72);
        var cellW = (chart.Viewport.Width - pad * 2) / columns;

        for (var i = 0; i < nodes.Count; i++) {
            var node = nodes[i];
            if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
            var col = i % columns;
            var row = i / columns;
            node.X = pad + col * cellW + (cellW - node.Width) / 2;
            node.Y = top + row * (node.Height + 42);
        }
    }

    private static void PlaceNodesInGroup(IList<TopologyNode> nodes, TopologyGroup group) {
        if (nodes.Count == 0) return;
        var columns = Math.Max(1, Math.Min(3, nodes.Count));
        var innerX = group.X + 24;
        var innerY = group.Y + 74;
        var usableW = Math.Max(80, group.Width - 48);
        var cellW = usableW / columns;

        for (var i = 0; i < nodes.Count; i++) {
            var node = nodes[i];
            if (!IsUnset(node.X) || !IsUnset(node.Y)) continue;
            var col = i % columns;
            var row = i / columns;
            node.X = innerX + col * cellW + (cellW - node.Width) / 2;
            node.Y = innerY + row * (node.Height + 34);
        }
    }

    private static int GetLayer(TopologyNode node) {
        if (node.Metadata.TryGetValue("layer", out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var layer)) return layer;
        return node.Kind switch {
            TopologyNodeKind.Group => 0,
            TopologyNodeKind.Namespace => 1,
            TopologyNodeKind.Location => 2,
            TopologyNodeKind.Hub => 3,
            TopologyNodeKind.Branch => 4,
            TopologyNodeKind.Server => 5,
            TopologyNodeKind.Service => 6,
            TopologyNodeKind.Endpoint => 7,
            _ => 4
        };
    }

    private static bool IsUnset(double value) => Math.Abs(value) < 0.0001;

    private static TopologyChart Clone(TopologyChart chart) {
        var copy = new TopologyChart {
            Id = chart.Id,
            Title = chart.Title,
            Subtitle = chart.Subtitle,
            LayoutMode = chart.LayoutMode,
            LayoutDirection = chart.LayoutDirection,
            Viewport = new TopologyViewport { Width = chart.Viewport.Width, Height = chart.Viewport.Height, Padding = chart.Viewport.Padding },
            Legend = chart.Legend,
            Theme = chart.Theme
        };
        foreach (var group in chart.Groups) copy.Groups.Add(Clone(group));
        foreach (var node in chart.Nodes) copy.Nodes.Add(Clone(node));
        foreach (var edge in chart.Edges) copy.Edges.Add(Clone(edge));
        return copy;
    }

    private static TopologyGroup Clone(TopologyGroup group) {
        var copy = new TopologyGroup {
            Id = group.Id,
            Label = group.Label,
            Subtitle = group.Subtitle,
            Status = group.Status,
            X = group.X,
            Y = group.Y,
            Width = group.Width,
            Height = group.Height,
            Href = group.Href,
            Tooltip = group.Tooltip,
            CssClass = group.CssClass,
            Symbol = group.Symbol,
            Color = group.Color
        };
        foreach (var item in group.Metadata) copy.Metadata[item.Key] = item.Value;
        return copy;
    }

    private static TopologyNode Clone(TopologyNode node) {
        var copy = new TopologyNode {
            Id = node.Id,
            Label = node.Label,
            Subtitle = node.Subtitle,
            Kind = node.Kind,
            Symbol = node.Symbol,
            DisplayMode = node.DisplayMode,
            Badge = node.Badge,
            Status = node.Status,
            GroupId = node.GroupId,
            X = node.X,
            Y = node.Y,
            Width = node.Width,
            Height = node.Height,
            Href = node.Href,
            Tooltip = node.Tooltip,
            CssClass = node.CssClass,
            Color = node.Color
        };
        foreach (var item in node.Metrics) copy.Metrics[item.Key] = item.Value;
        foreach (var item in node.Metadata) copy.Metadata[item.Key] = item.Value;
        return copy;
    }

    private static TopologyEdge Clone(TopologyEdge edge) {
        var copy = new TopologyEdge {
            Id = edge.Id,
            SourceNodeId = edge.SourceNodeId,
            TargetNodeId = edge.TargetNodeId,
            Kind = edge.Kind,
            Status = edge.Status,
            Direction = edge.Direction,
            Routing = edge.Routing,
            LineStyle = edge.LineStyle,
            SourcePort = edge.SourcePort,
            TargetPort = edge.TargetPort,
            RouteLane = edge.RouteLane,
            Label = edge.Label,
            SecondaryLabel = edge.SecondaryLabel,
            TertiaryLabel = edge.TertiaryLabel,
            Href = edge.Href,
            Tooltip = edge.Tooltip,
            CssClass = edge.CssClass,
            IsMuted = edge.IsMuted
        };
        foreach (var item in edge.Metrics) copy.Metrics[item.Key] = item.Value;
        foreach (var item in edge.Metadata) copy.Metadata[item.Key] = item.Value;
        copy.Waypoints.AddRange(edge.Waypoints);
        return copy;
    }
}
