using System;
using System.Collections.Generic;
using System.Linq;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    /// <summary>Packs automatic nested regions using the existing measured group-header and route owners.</summary>
    private static void ApplyBoundedGroupGrid(TopologyChart chart) {
        var policy = chart.GridLayout!;
        if (chart.Nodes.Any(node => node.HasPositionOverride || !IsUnset(node.X) || !IsUnset(node.Y))
            || chart.Groups.Any(group => group.HasPositionOverride || group.X != 0 || group.Y != 0 || group.Width > 0 || group.Height > 0))
            throw new InvalidOperationException("Bounded GroupGrid rows require automatic node and group placement.");
        var validation = TopologyGroupHierarchy.Validate(chart.Groups.Select(group => (group.Id, group.ParentGroupId)));
        if (!validation.IsValid) throw new TopologyValidationException(validation);
        var groups = chart.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var options = chart.RenderOptions ?? new TopologyRenderOptions();
        Layout(null);
        InferCompoundRoutes(chart, groups, preserveRouting: true, avoidOrthogonalObstacles: true);

        void Layout(string? parentId) {
            var children = chart.Groups.Where(group => group.ParentGroupId == parentId).ToArray();
            foreach (var child in children) Layout(child.Id);
            var nodes = chart.Nodes.Where(node => node.GroupId == parentId).ToArray();
            var width = 0d; var height = 0d; GridItem[]? previous = null;
            Pack(nodes.Select(node => new GridItem("node:" + node.Id, node.Width, node.Height, (x, y) => { node.X = x; node.Y = y; })).ToArray(), policy.MaximumNodesPerRow);
            Pack(children.Select(group => new GridItem("group:" + group.Id, group.Width, group.Height, (x, y) => ShiftGroup(group, x, y))).ToArray(), policy.MaximumGroupsPerRow);
            var insetX = policy.PaddingX ?? Math.Max(24, chart.Viewport.Padding); var insetY = (policy.PaddingY ?? Math.Max(24, chart.Viewport.Padding)) + (string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72);
            if (parentId != null) {
                var parent = groups[parentId]; parent.X = 0; parent.Y = 0;
                parent.Width = Math.Max(190, width + 2 * (policy.PaddingX ?? 24)); parent.Height = Math.Max(80, height + 2 * (policy.PaddingY ?? 24));
                insetX = policy.PaddingX ?? 24; insetY = policy.PaddingY ?? 24;
                if (TopologyGroupHeader.IsDrawn(options)) insetY = Math.Max(insetY, TopologyGroupHeader.ReservedBounds(chart, parent, options).Bottom + 12);
                parent.Height = Math.Max(80, height + insetY + (policy.PaddingY ?? 24));
            }
            foreach (var node in nodes) { node.X += insetX; node.Y += insetY; }
            foreach (var child in children) ShiftGroup(child, child.X + insetX, child.Y + insetY);

            void Pack(GridItem[] items, int maximum) {
                for (var start = 0; start < items.Length; start += Math.Min(maximum, items.Length - start)) {
                    var row = items.Skip(start).Take(Math.Min(maximum, items.Length - start)).ToArray();
                    if (previous != null) height += Math.Max(policy.VerticalSpacing, CaptionGap(previous, row, parentId, horizontal: false));
                    var x = start == 0 ? 0d : policy.RowIndent; var rowHeight = 0d;
                    for (var i = 0; i < row.Length; i++) {
                        if (i > 0) x += Math.Max(policy.HorizontalSpacing, CaptionGap(new[] { row[i - 1] }, new[] { row[i] }, parentId, horizontal: true));
                        row[i].Place(x, height);
                        x += row[i].Width; rowHeight = Math.Max(rowHeight, row[i].Height);
                    }
                    width = Math.Max(width, x); height += rowHeight; previous = row;
                }
            }
        }
        // A labelled relationship between neighbouring cells keeps room for its caption on its own segment,
        // including one that ends on a region boundary; the authored spacing remains the minimum.
        double CaptionGap(GridItem[] first, GridItem[] second, string? parentId, bool horizontal) {
            if (!options.IncludeEdgeLabels) return 0;
            var gap = 0d;
            foreach (var edge in chart.Edges) {
                var source = ImmediateItem(edge.SourceNodeId, parentId); var target = ImmediateItem(edge.TargetNodeId, parentId);
                if (source == null || target == null) continue;
                var joins = first.Any(item => item.Id == source) && second.Any(item => item.Id == target)
                    || first.Any(item => item.Id == target) && second.Any(item => item.Id == source);
                if (!joins || EdgeLabelBadge(chart, edge, options) is not { } badge) continue;
                gap = Math.Max(gap, (horizontal ? badge.Width : badge.Height) + EdgeEndpointGap * 2 + 32);
            }
            return gap;
        }
        string? ImmediateItem(string endpoint, string? parentId) {
            var node = chart.Nodes.FirstOrDefault(item => item.Id == endpoint);
            string? id = node != null ? "node:" + endpoint : groups.ContainsKey(endpoint) ? "group:" + endpoint : null;
            var groupId = node != null ? node.GroupId : groups.TryGetValue(endpoint, out var own) ? own.ParentGroupId : null;
            while (id != null && groupId != parentId) {
                if (groupId == null || !groups.TryGetValue(groupId, out var parent)) return null;
                id = "group:" + groupId; groupId = parent.ParentGroupId;
            }
            return id;
        }
        void ShiftGroup(TopologyGroup group, double x, double y) {
            var dx = x - group.X; var dy = y - group.Y; group.X = x; group.Y = y;
            foreach (var node in chart.Nodes.Where(node => BelongsTo(node.GroupId, group.Id))) { node.X += dx; node.Y += dy; }
            foreach (var child in chart.Groups.Where(child => child.Id != group.Id && BelongsTo(child.ParentGroupId, group.Id))) { child.X += dx; child.Y += dy; }
        }
        bool BelongsTo(string? id, string ancestor) {
            while (id != null && groups.TryGetValue(id, out var group)) { if (id == ancestor) return true; id = group.ParentGroupId; }
            return false;
        }
    }
    private sealed class GridItem {
        internal GridItem(string id, double width, double height, Action<double, double> place) { Id = id; Width = width; Height = height; Place = place; }
        internal string Id { get; }
        internal double Width { get; }
        internal double Height { get; }
        internal Action<double, double> Place { get; }
    }
}
