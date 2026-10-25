using System;
using System.Collections.Generic;
using System.Linq;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    /// <summary>Uses the native rank layout recursively, treating child regions as measured layout items.</summary>
    private static bool TryApplyCompoundLayered(TopologyChart chart, TopologyRenderOptions? options, bool inferRoutes = true) {
        var groupIds = new HashSet<string>(chart.Groups.Select(group => group.Id), StringComparer.Ordinal);
        if (!chart.Groups.Any(group => group.ParentGroupId != null || group.LayoutDirection.HasValue)
            && !chart.Edges.Any(edge => groupIds.Contains(edge.SourceNodeId) || groupIds.Contains(edge.TargetNodeId))
            && !(chart.Alignments.Count > 0 && chart.Groups.Count > 0)) return false;
        // Explicitly positioned diagrams keep their authored geometry. Compound automatic
        // layout applies only while the entire layout tree is automatic.
        if (chart.Nodes.Any(node => node.HasPositionOverride || !IsUnset(node.X) || !IsUnset(node.Y))
            || chart.Groups.Any(group => group.HasPositionOverride || group.X != 0 || group.Y != 0 || group.Width > 0 || group.Height > 0)) return false;
        var hierarchy = TopologyGroupHierarchy.Validate(chart.Groups.Select(group => (group.Id, group.ParentGroupId)));
        if (!hierarchy.IsValid) throw new TopologyValidationException(hierarchy);
        var groups = chart.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var swimlane = chart.LayoutMode == TopologyLayoutMode.Swimlane;
        var swimlaneConstraint = swimlane ? SwimlaneRankConstraint(chart, options) : null;
        var localOptions = (options ?? new TopologyRenderOptions()).CloneForRendering();
        localOptions.FitContentToViewport = true;
        localOptions.IncludeLegend = false;
        if (swimlane) {
            localOptions.LayeredNodeSpacing ??= 24;
            localOptions.LayeredRankSpacing ??= 96;
        }
        LayoutContainer(null);
        // Inferred paths from a local contracted graph are intentionally never copied:
        // real endpoints are routed against the fully expanded final geometry.
        if (inferRoutes) InferCompoundRoutes(chart, groups);
        return true;

        void LayoutContainer(string? parentId) {
            var inheritedDirection = parentId == null ? chart.LayoutDirection
                : TopologyGroupHierarchy.ResolveDirection(groups[parentId], id => groups[id], chart.LayoutDirection);
            var orderedResponsibilities = false;
            var children = swimlane && parentId == null ? SwimlaneRootGroups(chart, options, out orderedResponsibilities).ToArray()
                : chart.Groups.Where(group => group.ParentGroupId == parentId).ToArray();
            foreach (var child in children) {
                LayoutContainer(child.Id);
            }
            var directNodes = chart.Nodes.Where(node => node.GroupId == parentId).ToArray();
            if (parentId != null && children.Any(group => group.Presentation == TopologyGroupPresentation.Compartment)) {
                PlaceCompartments(chart, groups[parentId], children, directNodes.Length, inheritedDirection, localOptions, ShiftGroup);
                return;
            }
            var local = new TopologyChart {
                LayoutMode = TopologyLayoutMode.Layered, LayoutDirection = inheritedDirection,
                Viewport = new TopologyViewport { Width = chart.Viewport.Width, Height = chart.Viewport.Height, Padding = 24 },
                TextMeasurement = chart.TextMeasurement, RenderOptions = localOptions
            };
            var directByKey = directNodes.ToDictionary(node => "node:" + node.Id, StringComparer.Ordinal);
            var childrenByKey = children.ToDictionary(group => "group:" + group.Id, StringComparer.Ordinal);
            foreach (var entry in directByKey) local.Nodes.Add(Item(entry.Key, entry.Value.Label, LayeredNodeWidth(chart, entry.Value), LayeredNodeHeight(chart, entry.Value)));
            foreach (var entry in childrenByKey) local.Nodes.Add(Item(entry.Key, entry.Value.Label, entry.Value.Width, entry.Value.Height));
            if (swimlane) {
                foreach (var item in local.Nodes) {
                    var rank = directByKey.TryGetValue(item.Id, out var sourceNode) ? GetLayer(sourceNode)
                        : chart.Nodes.Where(node => BelongsTo(node.GroupId, childrenByKey[item.Id].Id)).Select(GetLayer).DefaultIfEmpty(0).Min();
                    item.Metadata["layer"] = rank.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            var rankConstraints = new HashSet<TopologyEdge>();
            foreach (var edge in chart.Edges) {
                var source = ImmediateItem(edge.SourceNodeId, parentId);
                var target = ImmediateItem(edge.TargetNodeId, parentId);
                if (source == null || target == null || source == target) continue;
                var localEdge = Clone(edge);
                localEdge.SourceNodeId = source; localEdge.TargetNodeId = target;
                localEdge.Waypoints.Clear(); localEdge.SourcePort = TopologyEdgePort.Auto; localEdge.TargetPort = TopologyEdgePort.Auto;
                local.Edges.Add(localEdge);
                if (swimlaneConstraint == null || swimlaneConstraint(edge)) rankConstraints.Add(localEdge);
            }
            var optimizeRanks = options?.LayeredOptimizeRanksByCrossings == true;
            ApplyEdgeRankHints(local, force: true, rankConstraint: swimlaneConstraint == null ? null : rankConstraints.Contains,
                preserveAuthoredRanks: swimlane || optimizeRanks, optimizeCrossings: optimizeRanks);
            var bands = orderedResponsibilities ? new LayeredResponsibilityBands(local, localOptions) : null;
            ApplyLayered(local, localOptions, inferRoutes && bands == null, ranksPrepared: swimlane || optimizeRanks, responsibilityBands: bands);
            var minX = local.Nodes.Select(node => node.X).DefaultIfEmpty(0).Min();
            var minY = local.Nodes.Select(node => node.Y).DefaultIfEmpty(0).Min();
            var width = local.Nodes.Select(node => node.X + node.Width - minX).DefaultIfEmpty(142).Max();
            var height = local.Nodes.Select(node => node.Y + node.Height - minY).DefaultIfEmpty(48).Max();
            var insetX = 24d;
            var insetY = 24d;
            if (parentId != null) {
                var parent = groups[parentId];
                var compartment = parent.Presentation == TopologyGroupPresentation.Compartment;
                var labelWidth = compartment || !parent.ShowLabel ? 0 : EstimateTextWidth(parent.Label, 12, true, chart.TextMeasurement) + 48;
                parent.Width = Math.Max(Math.Max(190, width + 48), labelWidth);
                parent.Height = Math.Max(80, height + 48);
                if (!compartment && TopologyGroupHeader.IsDrawn(localOptions)) {
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
                    node.X = x + (item.Width - node.Width) / 2; node.Y = y + TopologyNodeFootprint.TopInset(chart, node);
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

    /// <summary>Associates group ports and selects the canonical obstacle route for inferred automatic connections.</summary>
    private static void InferCompoundRoutes(TopologyChart chart, IReadOnlyDictionary<string, TopologyGroup> groups,
        bool preserveRouting = false, bool avoidOrthogonalObstacles = false) {
        var endpoints = TopologyGroupEndpoints.Resolve(chart);
        foreach (var edge in chart.Edges.Where(edge => edge.Waypoints.Count == 0)) {
            // Authored waypoints bypass inference. The bounded-grid policy promotes only
            // ordinary orthogonal routes, keeping explicit straight/curved choices and ports.
            if (!preserveRouting || avoidOrthogonalObstacles && edge.Routing == TopologyEdgeRouting.Orthogonal)
                edge.Routing = TopologyEdgeRouting.ObstacleAvoidingOrthogonal;
            if (!endpoints.TryGetValue(edge.SourceNodeId, out var source) || !endpoints.TryGetValue(edge.TargetNodeId, out var target)) continue;
            if (groups.ContainsKey(source.Id) && edge.SourcePort == TopologyEdgePort.Auto)
                edge.SourcePort = GroupPort(source, target);
            if (groups.ContainsKey(target.Id) && edge.TargetPort == TopologyEdgePort.Auto)
                edge.TargetPort = GroupPort(target, source);
        }
        TopologyEdgePort GroupPort(TopologyNode group, TopologyNode other) {
            var dx = CenterX(other) - CenterX(group);
            var dy = CenterY(other) - CenterY(group);
            if (Math.Abs(dx) >= Math.Abs(dy)) return dx < 0 ? TopologyEdgePort.Left : TopologyEdgePort.Right;
            // Group titles occupy the upper boundary. A side entry reaches the
            // shell below the reserved header when the other item is above it.
            return dy > 0 ? TopologyEdgePort.Bottom : dx < 0 ? TopologyEdgePort.Left : TopologyEdgePort.Right;
        }

    }

}
