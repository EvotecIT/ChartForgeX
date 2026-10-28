using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private static void ApplySwimlane(TopologyChart chart, TopologyRenderOptions? options) {
        if (HasEffectiveSwimlaneDirection(chart)) {
            if (!TryApplyCompoundLayered(chart, options)) throw new NotSupportedException("Locally directed swimlane groups require automatic native group composition.");
            return;
        }
        if (chart.Groups.Any(group => group.ParentGroupId != null)) {
            ApplyNestedSwimlane(chart, options);
            return;
        }
        ApplySwimlaneRankHints(chart, options);
        var horizontal = chart.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        var nodeGap = options?.LayeredNodeSpacing ?? 24;
        var rankGap = options?.LayeredRankSpacing ?? 96;
        var pad = chart.Viewport.Padding;
        var top = pad + (string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72);
        var nodes = chart.Nodes.Where(node => node.DisplayMode != TopologyNodeDisplayMode.Hidden).ToList();
        if (nodes.Count == 0) return;
        var ranks = nodes.Select(GetLayer).Distinct().OrderBy(rank => rank).ToList();
        var offsets = new Dictionary<int, double>();
        var extent = 40.0;
        var previousRank = ranks[0];
        foreach (var rank in ranks) {
            extent += Math.Max(0, (double)rank - previousRank - 1) * rankGap;
            offsets[rank] = extent;
            extent += nodes.Where(node => GetLayer(node) == rank).Max(node => horizontal ? node.Width : node.Height) + rankGap;
            previousRank = rank;
        }
        extent += 16;
        var render = options ?? new TopologyRenderOptions();
        if (horizontal && TopologyGroupHeader.IsDrawn(render)) {
            var size = chart.Theme.Typography.DataLabelSize;
            foreach (var group in chart.Groups.Where(group => group.ShowLabel)) {
                var reserve = 24 + TopologyGroupHeader.SymbolReserve(group, render);
                extent = Math.Max(extent, DenseHeaderWidth(chart, group.Label, size, true, render) + reserve + 1);
                if (!string.IsNullOrWhiteSpace(group.Subtitle)) extent = Math.Max(extent,
                    DenseHeaderWidth(chart, group.Subtitle!, size * .85, false, render) + reserve + 1);
            }
        }
        var laneOffset = 0.0;
        var laneIds = SwimlaneRootGroups(chart, options).Select(group => group.Id).ToList();
        if (nodes.Any(node => node.GroupId == null)) laneIds.Insert(0, string.Empty);
        foreach (var laneId in laneIds) {
            var laneNodes = nodes.Where(node => (node.GroupId ?? string.Empty) == laneId).ToList();
            var lane = chart.Groups.FirstOrDefault(group => group.Id == laneId);
            var inset = 72d;
            if (horizontal && lane != null && TopologyGroupHeader.IsDrawn(render)
                && (!lane.ShowLabel || (TopologyGroupHeader.CaptionRows(lane.Label, render) > 1 || TopologyGroupHeader.CaptionRows(lane.Subtitle, render) > 1))) {
                var measuring = new TopologyGroup { Id = lane.Id, Label = lane.Label, Subtitle = lane.Subtitle, ShowLabel = lane.ShowLabel,
                    Symbol = lane.Symbol, IconId = lane.IconId, X = 0, Y = 0, Width = extent, Height = 110 };
                inset = Math.Max(lane.ShowLabel ? inset : 24, TopologyGroupHeader.ReservedBounds(chart, measuring, render).Bottom + 12);
            }
            var laneExtent = laneNodes.Count == 0 ? 110 : laneNodes.GroupBy(GetLayer).Max(group => group.Sum(node => horizontal ? node.Height : node.Width) + Math.Max(0, group.Count() - 1) * nodeGap) + (horizontal ? Math.Max(96, inset + 24) : 96);
            foreach (var rank in laneNodes.GroupBy(GetLayer)) {
                var withinLane = inset;
                foreach (var node in rank) {
                    node.X = horizontal ? pad + offsets[rank.Key] : pad + laneOffset + withinLane;
                    node.Y = horizontal ? top + laneOffset + withinLane : top + offsets[rank.Key];
                    withinLane += (horizontal ? node.Height : node.Width) + nodeGap;
                }
            }
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
