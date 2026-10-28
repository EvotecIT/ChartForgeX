using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    /// <summary>Places actual responsibility bands recursively while sharing global process ranks across the entire hierarchy.</summary>
    private static void ApplyNestedSwimlane(TopologyChart chart, TopologyRenderOptions? options) {
        var hierarchy = TopologyGroupHierarchy.Validate(chart.Groups.Select(group => (group.Id, group.ParentGroupId)));
        if (!hierarchy.IsValid) throw new TopologyValidationException(hierarchy);
        ApplySwimlaneRankHints(chart, options);
        var render = options ?? new TopologyRenderOptions();
        var horizontal = chart.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        var nodeGap = options?.LayeredNodeSpacing ?? 24;
        var rankGap = options?.LayeredRankSpacing ?? 96;
        var pad = chart.Viewport.Padding;
        var top = pad + (string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72);
        var nodes = chart.Nodes.Where(node => node.DisplayMode != TopologyNodeDisplayMode.Hidden).ToArray();
        var direct = nodes.ToLookup(node => node.GroupId ?? string.Empty, StringComparer.Ordinal);
        var children = chart.Groups.ToLookup(group => group.ParentGroupId ?? string.Empty, StringComparer.Ordinal);
        var rootGroups = SwimlaneRootGroups(chart, options);
        var depths = new Dictionary<string, int>(StringComparer.Ordinal);
        var starts = new Dictionary<string, double>(StringComparer.Ordinal);
        var crossSizes = new Dictionary<string, double>(StringComparer.Ordinal);
        var headers = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var group in rootGroups) RecordDepth(group, 0);
        var maximumDepth = depths.Values.DefaultIfEmpty(0).Max();

        // Rank offsets are shared by parent-owned cards, descendant lanes and ungrouped actors.
        var offsets = new Dictionary<int, double>();
        var rankSpan = 0d;
        int? previousRank = null;
        foreach (var rank in nodes.GroupBy(GetLayer).OrderBy(rank => rank.Key)) {
            if (previousRank.HasValue) rankSpan += Math.Max(0, (double)rank.Key - previousRank.Value - 1) * rankGap;
            offsets[rank.Key] = rankSpan;
            rankSpan += rank.Max(node => horizontal ? node.Width : node.Height) + rankGap;
            previousRank = rank.Key;
        }

        var rankOrigin = Math.Max(40, (maximumDepth + 1) * 24d);
        var mainExtent = rankOrigin * 2 + rankSpan;
        if (horizontal) {
            foreach (var group in chart.Groups)
                mainExtent = Math.Max(mainExtent, depths[group.Id] * 48d + MinimumWidth(group));
            foreach (var group in rootGroups) MeasureCross(group);
        } else {
            foreach (var group in rootGroups) MeasureCross(group);
            foreach (var group in rootGroups) RecordMainStart(group, 0);
            rankOrigin = Math.Max(40, chart.Groups.Max(group => starts[group.Id] + headers[group.Id]));
            mainExtent = rankOrigin * 2 + rankSpan;
        }

        var crossOffset = 0d;
        var free = direct[string.Empty].ToArray();
        if (free.Length > 0) {
            PlaceNodes(free, 24);
            crossOffset = DirectSpan(free) + 60;
        }
        foreach (var group in rootGroups) {
            PlaceGroup(group, crossOffset);
            crossOffset += crossSizes[group.Id] + 12;
        }
        // Symmetric main-axis shell insets keep all ancestor headers clear after reversal.
        // Existing mirroring also owns authored ports, waypoints and explicit label anchors.
        if (chart.LayoutDirection == TopologyLayoutDirection.RightToLeft) MirrorLayoutHorizontally(chart, pad, pad + mainExtent);
        if (chart.LayoutDirection == TopologyLayoutDirection.BottomToTop) MirrorLayoutVertically(chart, top, top + mainExtent);

        void RecordDepth(TopologyGroup group, int depth) {
            depths[group.Id] = depth;
            foreach (var child in children[group.Id]) RecordDepth(child, depth + 1);
        }

        double MinimumWidth(TopologyGroup group) {
            var width = 190d;
            if (!group.ShowLabel || !TopologyGroupHeader.IsDrawn(render) || group.Presentation == TopologyGroupPresentation.Compartment) return width;
            var size = chart.Theme.Typography.DataLabelSize;
            var reserve = 24 + TopologyGroupHeader.SymbolReserve(group, render);
            width = Math.Max(width, DenseHeaderWidth(chart, group.Label, size, true, render) + reserve + 1);
            if (!string.IsNullOrWhiteSpace(group.Subtitle))
                width = Math.Max(width, DenseHeaderWidth(chart, group.Subtitle!, size * .85, false, render) + reserve + 1);
            return width;
        }

        double HeaderInset(TopologyGroup group, double width) {
            if (!TopologyGroupHeader.IsDrawn(render)) return 24;
            var measuring = new TopologyGroup { Id = group.Id, Label = group.Label, Subtitle = group.Subtitle,
                ShowLabel = group.ShowLabel, Presentation = group.Presentation, Symbol = group.Symbol, IconId = group.IconId,
                X = 0, Y = 0, Width = width, Height = 110 };
            return Math.Max(24, TopologyGroupHeader.ReservedBounds(chart, measuring, render).Bottom + 12);
        }

        double DirectSpan(IReadOnlyList<TopologyNode> members) => members.GroupBy(GetLayer)
            .Select(rank => rank.Sum(node => horizontal ? node.Height : node.Width) + Math.Max(0, rank.Count() - 1) * nodeGap)
            .DefaultIfEmpty(0).Max();

        double MeasureCross(TopologyGroup group) {
            var childBands = children[group.Id].ToArray();
            var members = direct[group.Id].ToArray();
            var span = DirectSpan(members);
            foreach (var child in childBands) {
                if (span > 0) span += 12;
                span += MeasureCross(child);
            }
            if (horizontal) {
                var width = mainExtent - depths[group.Id] * 48;
                headers[group.Id] = HeaderInset(group, width);
                starts[group.Id] = depths[group.Id] * 24;
                crossSizes[group.Id] = Math.Max(80, headers[group.Id] + span + 24);
            } else {
                crossSizes[group.Id] = Math.Max(MinimumWidth(group), span + 48);
                headers[group.Id] = HeaderInset(group, crossSizes[group.Id]);
            }
            return crossSizes[group.Id];
        }

        void RecordMainStart(TopologyGroup group, double start) {
            starts[group.Id] = start;
            foreach (var child in children[group.Id]) RecordMainStart(child, start + headers[group.Id]);
        }

        void PlaceGroup(TopologyGroup group, double cross) {
            var start = starts[group.Id];
            group.X = pad + (horizontal ? start : cross);
            group.Y = top + (horizontal ? cross : start);
            group.Width = horizontal ? mainExtent - start * 2 : crossSizes[group.Id];
            group.Height = horizontal ? crossSizes[group.Id] : mainExtent - start * 2;
            var members = direct[group.Id].ToArray();
            var inset = horizontal ? headers[group.Id] : 24;
            PlaceNodes(members, cross + inset);
            var cursor = cross + inset + DirectSpan(members);
            if (members.Length > 0 && children[group.Id].Any()) cursor += 12;
            foreach (var child in children[group.Id]) {
                PlaceGroup(child, cursor);
                cursor += crossSizes[child.Id] + 12;
            }
        }

        void PlaceNodes(IReadOnlyList<TopologyNode> members, double cross) {
            foreach (var rank in members.GroupBy(GetLayer)) {
                var cursor = cross;
                foreach (var node in rank) {
                    node.X = pad + (horizontal ? rankOrigin + offsets[rank.Key] : cursor);
                    node.Y = top + (horizontal ? cursor : rankOrigin + offsets[rank.Key]);
                    cursor += (horizontal ? node.Height : node.Width) + nodeGap;
                }
            }
        }
    }
}
