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
            var width = 0d; var height = 0d;
            Pack(nodes.Select(node => new GridItem(node.Width, node.Height, (x, y) => { node.X = x; node.Y = y; })).ToArray(), policy.MaximumNodesPerRow);
            Pack(children.Select(group => new GridItem(group.Width, group.Height, (x, y) => ShiftGroup(group, x, y))).ToArray(), policy.MaximumGroupsPerRow);
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
                if (items.Length == 0) return;
                if (height > 0) height += policy.VerticalSpacing;
                for (var start = 0; start < items.Length; start += Math.Min(maximum, items.Length - start)) {
                    var count = Math.Min(maximum, items.Length - start); var x = start == 0 ? 0d : policy.RowIndent; var rowHeight = 0d;
                    for (var i = 0; i < count; i++) {
                        var item = items[start + i]; item.Place(x, height);
                        x += item.Width + policy.HorizontalSpacing; rowHeight = Math.Max(rowHeight, item.Height);
                    }
                    width = Math.Max(width, x - policy.HorizontalSpacing); height += rowHeight;
                    if (start + count < items.Length) height += policy.VerticalSpacing;
                }
            }
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
        internal GridItem(double width, double height, Action<double, double> place) { Width = width; Height = height; Place = place; }
        internal double Width { get; }
        internal double Height { get; }
        internal Action<double, double> Place { get; }
    }
}
