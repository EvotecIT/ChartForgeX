using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static class TopologyLayoutNormalizer {
    private const double NodeGap = 16;
    private const double GroupPadding = 24;
    private const double GroupHeaderTopPadding = 14;
    private const double GroupHeaderBottomGap = 12;
    private const double RowTolerance = 36;
    private const double MinimumNodeWidth = 108;

    public static void Normalize(TopologyChart chart, TopologyRenderOptions? options = null) {
        options ??= new TopologyRenderOptions();
        ResolveGroupHeaderOverlaps(chart, options);
        ResolveNodeOverlaps(chart);
        ExpandGroupsForNodes(chart);
        FitViewportForRenderedContent(chart, options);
    }

    private static void ResolveGroupHeaderOverlaps(TopologyChart chart, TopologyRenderOptions options) {
        var groups = chart.Groups
            .Where(group => !string.IsNullOrWhiteSpace(group.Id))
            .GroupBy(group => group.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var node in chart.Nodes) {
            if (string.IsNullOrWhiteSpace(node.GroupId) || !groups.TryGetValue(node.GroupId!, out var group)) continue;
            var headerWidth = HeaderWidth(group, options);
            if (!Intersects(node.X, node.Y, node.Width, node.Height, HeaderX(group, options, headerWidth), HeaderY(group), headerWidth, HeaderHeight(group))) continue;
            var safeY = HeaderY(group) + HeaderHeight(group) + GroupHeaderBottomGap;
            if (node.Y < safeY) node.Y = safeY;
        }
    }

    private static void ResolveNodeOverlaps(TopologyChart chart) {
        var groups = new Dictionary<string, TopologyGroup>(StringComparer.Ordinal);
        foreach (var group in chart.Groups) {
            if (!string.IsNullOrWhiteSpace(group.Id) && !groups.ContainsKey(group.Id)) groups[group.Id] = group;
        }

        foreach (var set in chart.Nodes.GroupBy(node => node.GroupId ?? string.Empty, StringComparer.Ordinal)) {
            groups.TryGetValue(set.Key, out var group);
            ResolveRows(set.OrderBy(node => node.Y).ThenBy(node => node.X).ToList(), group);
        }
    }

    private static void ResolveRows(List<TopologyNode> nodes, TopologyGroup? group) {
        var rows = new List<List<TopologyNode>>();
        foreach (var node in nodes) {
            var row = rows.FirstOrDefault(candidate => Math.Abs(candidate[0].Y - node.Y) <= RowTolerance);
            if (row == null) {
                rows.Add(new List<TopologyNode> { node });
            } else {
                row.Add(node);
            }
        }

        foreach (var row in rows) ResolveRow(row.OrderBy(node => node.X).ToList(), group);
    }

    private static void ResolveRow(List<TopologyNode> row, TopologyGroup? group) {
        if (row.Count < 2) return;
        if (!HasOverlap(row)) return;

        var startX = group == null ? row[0].X : Math.Max(row[0].X, group.X + GroupPadding);
        var desiredWidth = row.Max(node => node.Width);
        if (group != null) {
            var available = Math.Max(MinimumNodeWidth, group.Width - GroupPadding * 2);
            var fitted = (available - NodeGap * (row.Count - 1)) / row.Count;
            if (fitted < desiredWidth) desiredWidth = Math.Max(MinimumNodeWidth, Math.Min(desiredWidth, fitted));
        }

        var currentX = startX;
        foreach (var node in row) {
            if (node.Width > desiredWidth) node.Width = desiredWidth;
            node.X = currentX;
            currentX += node.Width + NodeGap;
        }

        if (group != null) {
            var requiredRight = row.Max(node => node.X + node.Width) + GroupPadding;
            if (requiredRight > group.X + group.Width) group.Width = requiredRight - group.X;
        }
    }

    private static void ExpandGroupsForNodes(TopologyChart chart) {
        var groups = chart.Groups
            .Where(group => !string.IsNullOrWhiteSpace(group.Id))
            .GroupBy(group => group.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var nodeSet in chart.Nodes.Where(node => !string.IsNullOrWhiteSpace(node.GroupId)).GroupBy(node => node.GroupId!, StringComparer.Ordinal)) {
            if (!groups.TryGetValue(nodeSet.Key, out var group)) continue;
            var requiredBottom = nodeSet.Max(node => node.Y + node.Height) + GroupPadding;
            if (requiredBottom > group.Y + group.Height) group.Height = requiredBottom - group.Y;
        }
    }

    private static bool HasOverlap(IReadOnlyList<TopologyNode> row) {
        for (var i = 1; i < row.Count; i++) {
            var previous = row[i - 1];
            var current = row[i];
            if (current.X < previous.X + previous.Width + NodeGap && VerticallyOverlaps(previous, current)) return true;
        }

        return false;
    }

    private static bool VerticallyOverlaps(TopologyNode first, TopologyNode second) {
        return first.Y < second.Y + second.Height && second.Y < first.Y + first.Height;
    }

    private static bool Intersects(double ax, double ay, double aw, double ah, double bx, double by, double bw, double bh) {
        return ax < bx + bw && ax + aw > bx && ay < by + bh && ay + ah > by;
    }

    private static double HeaderX(TopologyGroup group, TopologyRenderOptions options, double width) =>
        IsMonitoringDashboardStyle(options) && UseNeutralGroupSurface(options) ? group.X + 22 : group.X + (group.Width - width) / 2;

    private static double HeaderY(TopologyGroup group) => group.Y + GroupHeaderTopPadding;

    private static double HeaderHeight(TopologyGroup group) => string.IsNullOrWhiteSpace(group.Subtitle) ? 40 : 60;

    private static double HeaderWidth(TopologyGroup group, TopologyRenderOptions options) {
        var rendersSymbol = !IsMonitoringDashboardStyle(options) || !string.IsNullOrWhiteSpace(group.Symbol);
        var maxLabelWidth = GroupHeaderLabelWidth(group, options, rendersSymbol);
        var labelSize = FitFontSize(group.Label, maxLabelWidth, 16, 12, true);
        var labelWidth = EstimateTextWidth(TrimToEstimatedWidth(group.Label, maxLabelWidth, labelSize, true), labelSize, true) + (rendersSymbol ? 30 : 0);
        var subtitleWidth = string.IsNullOrWhiteSpace(group.Subtitle) ? 0 : EstimateTextWidth(group.Subtitle!, 12, false);
        return Math.Min(Math.Max(96, Math.Max(labelWidth, subtitleWidth) + 12), Math.Max(96, group.Width - GroupPadding * 2));
    }

    private static double GroupHeaderLabelWidth(TopologyGroup group, TopologyRenderOptions options, bool includesLeadingSymbol) {
        var statusReserve = options.IncludeGroupStatusDots && IsMonitoringDashboardStyle(options) && group.Status != TopologyHealthStatus.Unknown ? 38 : 0;
        var symbolReserve = includesLeadingSymbol ? 42 : 0;
        return Math.Max(36, group.Width - 44 - statusReserve - symbolReserve);
    }

    private static void FitViewportForRenderedContent(TopologyChart chart, TopologyRenderOptions options) {
        var bounds = MeasureContent(chart, options);
        if (!bounds.HasContent) return;

        var surfaceInset = CanvasSurfaceInset(chart, options);
        var targetLeft = chart.Viewport.Padding + surfaceInset;
        var targetTop = chart.Viewport.Padding + surfaceInset + (string.IsNullOrWhiteSpace(chart.Title) && string.IsNullOrWhiteSpace(chart.Subtitle) ? 0 : 72);
        var dx = bounds.Left < targetLeft ? targetLeft - bounds.Left : 0;
        var dy = bounds.Top < targetTop ? targetTop - bounds.Top : 0;
        if (Math.Abs(dx) > 0.0001 || Math.Abs(dy) > 0.0001) {
            ShiftContent(chart, dx, dy);
            bounds = bounds.Shift(dx, dy);
        }

        var neededWidth = bounds.Right + chart.Viewport.Padding + surfaceInset;
        var neededHeight = bounds.Bottom + chart.Viewport.Padding + surfaceInset + LegendReservedHeight(chart.Legend, chart.Viewport);
        if (neededWidth > chart.Viewport.Width) chart.Viewport.Width = Math.Ceiling(neededWidth);
        if (neededHeight > chart.Viewport.Height) chart.Viewport.Height = Math.Ceiling(neededHeight);
    }

    private static ContentBounds MeasureContent(TopologyChart chart, TopologyRenderOptions options) {
        var bounds = ContentBounds.Empty;
        foreach (var group in chart.Groups) bounds = bounds.Include(group.X, group.Y, group.X + group.Width, group.Y + group.Height);
        foreach (var node in chart.Nodes) bounds = IncludeNodeVisualBounds(bounds, node, options);
        foreach (var point in chart.Edges.SelectMany(edge => edge.Waypoints)) bounds = bounds.Include(point.X, point.Y, point.X, point.Y);
        if (options.IncludeEdgeLabels && CanMeasureEdgeLabels(chart)) {
            foreach (var layout in EdgeLabelLayouts(chart, options)) {
                bounds = bounds.Include(layout.CenterX - layout.Width / 2, layout.CenterY - layout.Height / 2, layout.CenterX + layout.Width / 2, layout.CenterY + layout.Height / 2);
            }
        }

        return bounds;
    }

    private static bool CanMeasureEdgeLabels(TopologyChart chart) {
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (string.IsNullOrWhiteSpace(node.Id) || !nodeIds.Add(node.Id)) return false;
        }

        return chart.Edges.All(edge => nodeIds.Contains(edge.SourceNodeId) && nodeIds.Contains(edge.TargetNodeId));
    }

    private static ContentBounds IncludeNodeVisualBounds(ContentBounds bounds, TopologyNode node, TopologyRenderOptions options) {
        var displayMode = EffectiveNodeDisplayMode(node, options);
        if (displayMode == TopologyNodeDisplayMode.Hidden) return bounds;
        bounds = bounds.Include(node.X, node.Y, node.X + node.Width, node.Y + node.Height);
        if (options.IncludeStatusBadges && ShouldRenderNodeStatusBadge(node, options)) {
            var cx = node.X + node.Width - 11;
            var cy = node.Y + 11;
            bounds = bounds.Include(cx - 10, cy - 10, cx + 10, cy + 10);
        }

        var badge = NodeBadge(node);
        if (!string.IsNullOrWhiteSpace(badge)) {
            var width = Math.Max(18, badge.Length * 6.5 + 12);
            var height = 18.0;
            var x = displayMode == TopologyNodeDisplayMode.Dot ? CenterX(node) + 8 : displayMode == TopologyNodeDisplayMode.Icon ? CenterX(node) - width / 2 : node.X + node.Width - width - 6;
            var y = displayMode == TopologyNodeDisplayMode.Dot ? CenterY(node) - 21 : displayMode == TopologyNodeDisplayMode.Icon ? node.Y + node.Height + 4 : displayMode == TopologyNodeDisplayMode.Tile ? node.Y - 8 : node.Y + node.Height - height - 6;
            bounds = bounds.Include(x, y, x + width, y + height);
        }

        if (!options.IncludeNodeLabels) return bounds;
        if (displayMode == TopologyNodeDisplayMode.Icon) {
            if (options.IncludeIconLabels) {
                var labelWidth = IconLabelPlateWidth(node);
                var labelCenter = CenterX(node);
                var labelY = IconLabelPlateY(node);
                bounds = bounds.Include(labelCenter - labelWidth / 2, labelY, labelCenter + labelWidth / 2, labelY + 15);
            }

            return bounds;
        }

        if (displayMode == TopologyNodeDisplayMode.Tile) {
            var labelLines = NodeTextLines(node.Label, Math.Max(node.Width + 34, 54), 11, true, options.MaxNodeLabelLines, options);
            var labelCenter = CenterX(node);
            var labelTop = node.Y + node.Height + 4;
            var lineCount = Math.Max(1, labelLines.Count);
            if (labelLines.Count == 0) bounds = bounds.Include(labelCenter, labelTop, labelCenter, labelTop + lineCount * 14 + 4);
            foreach (var line in labelLines) {
                var labelWidth = EstimateTextWidth(line, 11, true);
                bounds = bounds.Include(labelCenter - labelWidth / 2, labelTop, labelCenter + labelWidth / 2, labelTop + lineCount * 14 + 4);
            }

            if (options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle)) {
                var subtitle = TrimTo(node.Subtitle!, 16);
                var subtitleWidth = Math.Min(Math.Max(46, subtitle.Length * 5.8 + 18), Math.Max(46, node.Width + 28));
                var subtitleY = node.Y + node.Height + 7 + lineCount * 14;
                bounds = bounds.Include(labelCenter - subtitleWidth / 2, subtitleY, labelCenter + subtitleWidth / 2, subtitleY + 17);
            }
        }

        return bounds;
    }

    private static void ShiftContent(TopologyChart chart, double dx, double dy) {
        foreach (var group in chart.Groups) {
            group.X += dx;
            group.Y += dy;
        }

        foreach (var node in chart.Nodes) {
            node.X += dx;
            node.Y += dy;
        }

        foreach (var edge in chart.Edges) {
            for (var i = 0; i < edge.Waypoints.Count; i++) {
                var point = edge.Waypoints[i];
                edge.Waypoints[i] = new ChartPoint(point.X + dx, point.Y + dy);
            }
        }
    }

    private readonly struct ContentBounds {
        private ContentBounds(double left, double top, double right, double bottom, bool hasContent) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
            HasContent = hasContent;
        }

        public static ContentBounds Empty => new(0, 0, 0, 0, false);

        public bool HasContent { get; }

        public double Left { get; }

        public double Top { get; }

        public double Right { get; }

        public double Bottom { get; }

        public ContentBounds Include(double left, double top, double right, double bottom) {
            return HasContent
                ? new ContentBounds(Math.Min(Left, left), Math.Min(Top, top), Math.Max(Right, right), Math.Max(Bottom, bottom), true)
                : new ContentBounds(left, top, right, bottom, true);
        }

        public ContentBounds Shift(double dx, double dy) => HasContent ? new ContentBounds(Left + dx, Top + dy, Right + dx, Bottom + dy, true) : this;
    }
}
