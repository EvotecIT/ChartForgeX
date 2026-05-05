using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Topology;

internal static class TopologyLayoutEngine {
    public static TopologyChart Prepare(TopologyChart chart, TopologyView? view = null) {
        var copy = Clone(chart);
        if (view != null) ApplyView(copy, view);
        switch (copy.LayoutMode) {
            case TopologyLayoutMode.RegionGrid:
                ApplyRegionGrid(copy);
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

        return copy;
    }

    private static void ApplyView(TopologyChart chart, TopologyView view) {
        if (!string.IsNullOrWhiteSpace(view.Id)) chart.Id = string.IsNullOrWhiteSpace(chart.Id) ? view.Id : chart.Id + "-" + view.Id;
        if (!string.IsNullOrWhiteSpace(view.Title)) chart.Title = view.Title;
        if (!string.IsNullOrWhiteSpace(view.Subtitle)) chart.Subtitle = view.Subtitle;

        var selectedGroupIds = new HashSet<string>(view.GroupIds, StringComparer.Ordinal);
        var selectedNodeIds = new HashSet<string>(view.NodeIds, StringComparer.Ordinal);
        var selectedEdgeIds = new HashSet<string>(view.EdgeIds, StringComparer.Ordinal);
        var hasGroupFilter = selectedGroupIds.Count > 0;
        var hasNodeFilter = selectedNodeIds.Count > 0;
        var hasEdgeFilter = selectedEdgeIds.Count > 0;
        if (!hasGroupFilter && !hasNodeFilter && !hasEdgeFilter) return;

        var visibleNodes = chart.Nodes
            .Where(node => (!hasNodeFilter || selectedNodeIds.Contains(node.Id)) && (!hasGroupFilter || string.IsNullOrWhiteSpace(node.GroupId) || selectedGroupIds.Contains(node.GroupId!)))
            .ToList();
        if (!hasNodeFilter && hasGroupFilter) visibleNodes = chart.Nodes.Where(node => !string.IsNullOrWhiteSpace(node.GroupId) && selectedGroupIds.Contains(node.GroupId!)).ToList();
        if (hasNodeFilter && !hasGroupFilter) visibleNodes = chart.Nodes.Where(node => selectedNodeIds.Contains(node.Id)).ToList();

        var visibleNodeIds = new HashSet<string>(visibleNodes.Select(node => node.Id), StringComparer.Ordinal);
        var visibleEdges = hasEdgeFilter
            ? chart.Edges.Where(edge => selectedEdgeIds.Contains(edge.Id) && visibleNodeIds.Contains(edge.SourceNodeId) && visibleNodeIds.Contains(edge.TargetNodeId)).ToList()
            : view.IncludeConnectedEdges
                ? chart.Edges.Where(edge => visibleNodeIds.Contains(edge.SourceNodeId) && visibleNodeIds.Contains(edge.TargetNodeId)).ToList()
                : new List<TopologyEdge>();

        var visibleGroupIds = new HashSet<string>(selectedGroupIds, StringComparer.Ordinal);
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

    private static void ApplyRegionGrid(TopologyChart chart) {
        if (chart.Groups.Count == 0) {
            ApplyMatrix(chart);
            return;
        }

        var pad = Math.Max(16, chart.Viewport.Padding);
        var titleOffset = string.IsNullOrWhiteSpace(chart.Title) ? 0 : 64;
        var legendOffset = chart.Legend == null ? 0 : 110;
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
            var hub = nodes.FirstOrDefault(node => node.Kind == TopologyNodeKind.HubSite || node.Kind == TopologyNodeKind.Region || node.Metadata.ContainsKey("hub")) ?? nodes[0];
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
        var availableH = Math.Max(100, chart.Viewport.Height - top - pad - (chart.Legend == null ? 0 : 100));
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
            TopologyNodeKind.Forest => 0,
            TopologyNodeKind.Domain => 1,
            TopologyNodeKind.Region => 2,
            TopologyNodeKind.HubSite => 3,
            TopologyNodeKind.Site => 4,
            TopologyNodeKind.BranchSite => 5,
            TopologyNodeKind.DomainController => 6,
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
            Tooltip = group.Tooltip
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
            Status = node.Status,
            GroupId = node.GroupId,
            X = node.X,
            Y = node.Y,
            Width = node.Width,
            Height = node.Height,
            Href = node.Href,
            Tooltip = node.Tooltip
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
            Label = edge.Label,
            SecondaryLabel = edge.SecondaryLabel,
            Href = edge.Href,
            Tooltip = edge.Tooltip
        };
        foreach (var item in edge.Metrics) copy.Metrics[item.Key] = item.Value;
        foreach (var item in edge.Metadata) copy.Metadata[item.Key] = item.Value;
        return copy;
    }
}
