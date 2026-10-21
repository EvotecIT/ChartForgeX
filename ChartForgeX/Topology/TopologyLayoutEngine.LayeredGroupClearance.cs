using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    /// <summary>Moves an automatic shell across ranks, preserving flow coordinates and authored outsiders.</summary>
    private static void SeparateLayeredShellFromUngroupedNodes(TopologyChart chart, TopologyGroup group,
        IReadOnlyList<TopologyGroup> placedGroups, bool horizontal) {
        var outsiders = chart.Nodes.Where(node => node.GroupId == null).ToArray();
        if (outsiders.Length == 0) return;
        var flowStart = horizontal ? group.X : group.Y;
        var flowLength = horizontal ? group.Width : group.Height;
        var crossLength = horizontal ? group.Height : group.Width;
        var origin = horizontal ? group.Y : group.X;
        var obstacles = placedGroups.Select(other => new ChartRect(other.X, other.Y, other.Width, other.Height))
            .Concat(outsiders.Select(node => {
                var width = TopologyNodeFootprint.Width(chart, node);
                return new ChartRect(node.X + (node.Width - width) / 2, node.Y, width, TopologyNodeFootprint.Height(chart, node));
            }))
            .Where(bounds => flowStart < (horizontal ? bounds.Right : bounds.Bottom) + 24
                && flowStart + flowLength + 24 > (horizontal ? bounds.X : bounds.Y))
            .Select(bounds => (Start: (horizontal ? bounds.Y : bounds.X) - crossLength - 24,
                End: (horizontal ? bounds.Bottom : bounds.Right) + 24))
            .OrderBy(interval => interval.Start).ToArray();
        // Each interval contains shell origins that would overlap a full foreign footprint.
        // Walking in either direction finds the nearest free origin without moving a rank.
        var before = origin; var after = origin;
        foreach (var interval in obstacles)
            if (after > interval.Start && after < interval.End) after = interval.End;
        for (var index = obstacles.Length - 1; index >= 0; index--)
            if (before > obstacles[index].Start && before < obstacles[index].End) before = obstacles[index].Start;
        var coordinate = before >= chart.Viewport.Padding && Math.Abs(before - origin) <= Math.Abs(after - origin) ? before : after;
        if (horizontal) group.Y = coordinate;
        else group.X = coordinate;
    }
}
