using System;
using System.Collections.Generic;
using System.Linq;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    /// <summary>Uses the native rank layout recursively, treating child regions as measured layout items.</summary>
    private static bool TryApplyCompoundLayered(TopologyChart chart, TopologyRenderOptions? options) {
        var groupIds = new HashSet<string>(chart.Groups.Select(group => group.Id), StringComparer.Ordinal);
        if (!chart.Groups.Any(group => group.ParentGroupId != null || group.LayoutDirection.HasValue)
            && !chart.Edges.Any(edge => groupIds.Contains(edge.SourceNodeId) || groupIds.Contains(edge.TargetNodeId))) return false;
        // Explicitly positioned diagrams keep their authored geometry. Compound automatic
        // layout applies only while the entire layout tree is automatic.
        if (chart.Nodes.Any(node => node.HasPositionOverride || !IsUnset(node.X) || !IsUnset(node.Y))
            || chart.Groups.Any(group => group.HasPositionOverride || group.X != 0 || group.Y != 0 || group.Width > 0 || group.Height > 0)) return false;
        var hierarchy = TopologyGroupHierarchy.Validate(chart.Groups.Select(group => (group.Id, group.ParentGroupId)));
        if (!hierarchy.IsValid) throw new TopologyValidationException(hierarchy);
        var groups = chart.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var localOptions = (options ?? new TopologyRenderOptions()).CloneForRendering();
        localOptions.FitContentToViewport = true;
        localOptions.IncludeLegend = false;
        LayoutContainer(null, chart.LayoutDirection);
        // Inferred paths from a local contracted graph are intentionally never copied:
        // real endpoints are routed against the fully expanded final geometry.
        var endpoints = TopologyGroupEndpoints.Resolve(chart);
        foreach (var edge in chart.Edges.Where(edge => edge.Waypoints.Count == 0)) {
            edge.Routing = TopologyEdgeRouting.ObstacleAvoidingOrthogonal;
            if (!endpoints.TryGetValue(edge.SourceNodeId, out var source) || !endpoints.TryGetValue(edge.TargetNodeId, out var target)) continue;
            if (groups.ContainsKey(source.Id) && edge.SourcePort == TopologyEdgePort.Auto)
                edge.SourcePort = GroupPort(source, target);
            if (groups.ContainsKey(target.Id) && edge.TargetPort == TopologyEdgePort.Auto)
                edge.TargetPort = GroupPort(target, source);
        }
        return true;

        TopologyEdgePort GroupPort(TopologyNode group, TopologyNode other) {
            var dx = CenterX(other) - CenterX(group);
            var dy = CenterY(other) - CenterY(group);
            if (Math.Abs(dx) >= Math.Abs(dy)) return dx < 0 ? TopologyEdgePort.Left : TopologyEdgePort.Right;
            // Group titles occupy the upper boundary. A side entry reaches the
            // shell below the reserved header when the other item is above it.
            return dy > 0 ? TopologyEdgePort.Bottom : dx < 0 ? TopologyEdgePort.Left : TopologyEdgePort.Right;
        }

        void LayoutContainer(string? parentId, TopologyLayoutDirection inheritedDirection) {
            var children = chart.Groups.Where(group => group.ParentGroupId == parentId).ToArray();
            foreach (var child in children) {
                LayoutContainer(child.Id, child.LayoutDirection ?? inheritedDirection);
            }
            var directNodes = chart.Nodes.Where(node => node.GroupId == parentId).ToArray();
            var local = new TopologyChart {
                LayoutMode = TopologyLayoutMode.Layered, LayoutDirection = inheritedDirection,
                Viewport = new TopologyViewport { Width = chart.Viewport.Width, Height = chart.Viewport.Height, Padding = 24 },
                TextMeasurement = chart.TextMeasurement, RenderOptions = localOptions
            };
            var directByKey = directNodes.ToDictionary(node => "node:" + node.Id, StringComparer.Ordinal);
            var childrenByKey = children.ToDictionary(group => "group:" + group.Id, StringComparer.Ordinal);
            foreach (var entry in directByKey) local.Nodes.Add(Item(entry.Key, entry.Value.Label, entry.Value.Width, entry.Value.Height));
            foreach (var entry in childrenByKey) local.Nodes.Add(Item(entry.Key, entry.Value.Label, entry.Value.Width, entry.Value.Height));
            foreach (var edge in chart.Edges) {
                var source = ImmediateItem(edge.SourceNodeId, parentId);
                var target = ImmediateItem(edge.TargetNodeId, parentId);
                if (source == null || target == null || source == target) continue;
                var localEdge = Clone(edge);
                localEdge.SourceNodeId = source; localEdge.TargetNodeId = target;
                localEdge.Waypoints.Clear(); localEdge.SourcePort = TopologyEdgePort.Auto; localEdge.TargetPort = TopologyEdgePort.Auto;
                local.Edges.Add(localEdge);
            }
            ApplyEdgeRankHints(local, force: true);
            ApplyLayered(local, localOptions);
            var minX = local.Nodes.Select(node => node.X).DefaultIfEmpty(0).Min();
            var minY = local.Nodes.Select(node => node.Y).DefaultIfEmpty(0).Min();
            var width = local.Nodes.Select(node => node.X + node.Width - minX).DefaultIfEmpty(142).Max();
            var height = local.Nodes.Select(node => node.Y + node.Height - minY).DefaultIfEmpty(48).Max();
            var insetX = 24d;
            var insetY = 24d;
            if (parentId != null) {
                var parent = groups[parentId];
                var labelWidth = EstimateTextWidth(parent.Label, 12, true, chart.TextMeasurement) + 48;
                parent.Width = Math.Max(Math.Max(190, width + 48), labelWidth);
                parent.Height = Math.Max(80, height + 48);
                if (TopologyGroupHeader.IsDrawn(localOptions)) {
                    insetY = TopologyGroupHeader.ReservedBounds(chart, parent, localOptions).Bottom - parent.Y + 12;
                    parent.Height = Math.Max(80, height + insetY + 24);
                }
                parent.X = 0; parent.Y = 0;
            } else {
                insetX = chart.Viewport.Padding;
                insetY = chart.Viewport.Padding + (string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72);
            }
            foreach (var item in local.Nodes) {
                var x = item.X - minX + insetX;
                var y = item.Y - minY + insetY;
                if (directByKey.TryGetValue(item.Id, out var node)) {
                    node.X = x; node.Y = y;
                    foreach (var metadata in item.Metadata) node.Metadata[metadata.Key] = metadata.Value;
                } else ShiftGroup(childrenByKey[item.Id], x, y);
            }
        }

        TopologyNode Item(string id, string label, double width, double height) => new() {
            Id = id, Label = label, Width = width, Height = height, PreserveDisplayModeSize = true,
            Shape = TopologyNodeShape.Rectangle, ShowStatusBadge = false
        };

        string? ImmediateItem(string endpoint, string? parentId) {
            var groupId = nodes.TryGetValue(endpoint, out var node) ? node.GroupId
                : groups.TryGetValue(endpoint, out var group) ? group.ParentGroupId : null;
            if (!nodes.ContainsKey(endpoint) && !groups.ContainsKey(endpoint)) return null;
            var itemId = (nodes.ContainsKey(endpoint) ? "node:" : "group:") + endpoint;
            while (groupId != parentId) {
                if (groupId == null || !groups.TryGetValue(groupId, out var parentGroup)) return null;
                itemId = "group:" + groupId; groupId = parentGroup.ParentGroupId;
            }
            return itemId;
        }

        void ShiftGroup(TopologyGroup group, double x, double y) {
            var dx = x - group.X; var dy = y - group.Y;
            group.X = x; group.Y = y;
            foreach (var node in chart.Nodes.Where(node => BelongsTo(node.GroupId, group.Id))) { node.X += dx; node.Y += dy; }
            foreach (var descendant in chart.Groups.Where(other => other.Id != group.Id && BelongsTo(other.ParentGroupId, group.Id))) {
                descendant.X += dx; descendant.Y += dy;
            }
        }

        bool BelongsTo(string? groupId, string ancestorId) {
            while (groupId != null && groups.TryGetValue(groupId, out var group)) {
                if (groupId == ancestorId) return true;
                groupId = group.ParentGroupId;
            }
            return false;
        }
    }

}
