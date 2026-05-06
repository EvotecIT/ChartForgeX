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

    public static void Normalize(TopologyChart chart) {
        ResolveGroupHeaderOverlaps(chart);
        ResolveNodeOverlaps(chart);
        ExpandGroupsForNodes(chart);
        FitViewportForRenderedContent(chart);
    }

    private static void ResolveGroupHeaderOverlaps(TopologyChart chart) {
        var groups = chart.Groups
            .Where(group => !string.IsNullOrWhiteSpace(group.Id))
            .GroupBy(group => group.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var node in chart.Nodes) {
            if (string.IsNullOrWhiteSpace(node.GroupId) || !groups.TryGetValue(node.GroupId!, out var group)) continue;
            if (!Intersects(node.X, node.Y, node.Width, node.Height, HeaderX(group), HeaderY(group), HeaderWidth(group), HeaderHeight(group))) continue;
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

    private static double HeaderX(TopologyGroup group) => group.X + GroupPadding;

    private static double HeaderY(TopologyGroup group) => group.Y + GroupHeaderTopPadding;

    private static double HeaderHeight(TopologyGroup group) => string.IsNullOrWhiteSpace(group.Subtitle) ? 40 : 60;

    private static double HeaderWidth(TopologyGroup group) {
        var labelWidth = EstimateTextWidth(group.Label, 16, true);
        var subtitleWidth = string.IsNullOrWhiteSpace(group.Subtitle) ? 0 : EstimateTextWidth(group.Subtitle!, 12, false);
        return Math.Min(Math.Max(96, Math.Max(labelWidth, subtitleWidth) + 12), Math.Max(96, group.Width - GroupPadding * 2));
    }

    private static double EstimateTextWidth(string value, double fontSize, bool bold) {
        var weightFactor = bold ? 0.62 : 0.56;
        return value.Length * fontSize * weightFactor;
    }

    private static void FitViewportForRenderedContent(TopologyChart chart) {
        var bounds = MeasureContent(chart);
        if (!bounds.HasContent) return;

        var targetLeft = chart.Viewport.Padding;
        var targetTop = chart.Viewport.Padding + (string.IsNullOrWhiteSpace(chart.Title) && string.IsNullOrWhiteSpace(chart.Subtitle) ? 0 : 72);
        var dx = bounds.Left < targetLeft ? targetLeft - bounds.Left : 0;
        var dy = bounds.Top < targetTop ? targetTop - bounds.Top : 0;
        if (Math.Abs(dx) > 0.0001 || Math.Abs(dy) > 0.0001) {
            ShiftContent(chart, dx, dy);
            bounds = bounds.Shift(dx, dy);
        }

        var neededWidth = bounds.Right + chart.Viewport.Padding;
        var neededHeight = bounds.Bottom + chart.Viewport.Padding + LegendReservedHeight(chart.Legend);
        if (neededWidth > chart.Viewport.Width) chart.Viewport.Width = Math.Ceiling(neededWidth);
        if (neededHeight > chart.Viewport.Height) chart.Viewport.Height = Math.Ceiling(neededHeight);
    }

    private static ContentBounds MeasureContent(TopologyChart chart) {
        var bounds = ContentBounds.Empty;
        foreach (var group in chart.Groups) bounds = bounds.Include(group.X, group.Y, group.X + group.Width, group.Y + group.Height);
        foreach (var node in chart.Nodes) bounds = bounds.Include(node.X, node.Y, node.X + node.Width, node.Y + node.Height);
        foreach (var point in chart.Edges.SelectMany(edge => edge.Waypoints)) bounds = bounds.Include(point.X, point.Y, point.X, point.Y);
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
