using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private static void ApplySwimlane(TopologyChart chart, TopologyRenderOptions? options) {
        ApplyEdgeRankHints(chart);
        var horizontal = chart.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        var pad = chart.Viewport.Padding;
        var top = pad + (string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72);
        var nodes = chart.Nodes.Where(node => node.DisplayMode != TopologyNodeDisplayMode.Hidden).ToList();
        if (nodes.Count == 0) return;
        var ranks = nodes.Select(GetLayer).Distinct().OrderBy(rank => rank).ToList();
        var offsets = new Dictionary<int, double>();
        var extent = 40.0;
        foreach (var rank in ranks) {
            offsets[rank] = extent;
            extent += nodes.Where(node => GetLayer(node) == rank).Max(node => horizontal ? node.Width : node.Height) + 96;
        }
        extent += 16;
        var laneOffset = 0.0;
        var laneIds = chart.Groups.Select(group => group.Id).ToList();
        if (nodes.Any(node => node.GroupId == null)) laneIds.Insert(0, string.Empty);
        foreach (var laneId in laneIds) {
            var laneNodes = nodes.Where(node => (node.GroupId ?? string.Empty) == laneId).ToList();
            var laneExtent = laneNodes.Count == 0 ? 110 : laneNodes.GroupBy(GetLayer).Max(group => group.Sum(node => horizontal ? node.Height : node.Width) + Math.Max(0, group.Count() - 1) * 24) + 96;
            foreach (var rank in laneNodes.GroupBy(GetLayer)) {
                var withinLane = 72.0;
                foreach (var node in rank) {
                    node.X = horizontal ? pad + offsets[rank.Key] : pad + laneOffset + withinLane;
                    node.Y = horizontal ? top + laneOffset + withinLane : top + offsets[rank.Key];
                    withinLane += (horizontal ? node.Height : node.Width) + 24;
                }
            }
            var lane = chart.Groups.FirstOrDefault(group => group.Id == laneId);
            if (lane != null) {
                lane.X = pad + (horizontal ? 0 : laneOffset);
                lane.Y = top + (horizontal ? laneOffset : 0);
                lane.Width = horizontal ? extent : laneExtent;
                lane.Height = horizontal ? laneExtent : extent;
            }
            laneOffset += laneExtent + 12;
        }
        if (chart.LayoutDirection == TopologyLayoutDirection.RightToLeft) MirrorLayoutHorizontally(chart, pad, pad + extent);
        if (chart.LayoutDirection == TopologyLayoutDirection.BottomToTop) MirrorLayoutVertically(chart, top, top + extent);
    }
}
