using System;
using System.Collections.Generic;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    /// <summary>Captures node extents once for one layout, without retaining mutable scenes across renders.</summary>
    private static IReadOnlyDictionary<string, PreparedNodeExtents> MeasurePreparedNodeExtents(IReadOnlyList<GraphSceneNode> nodes, bool includeLabels) {
        var extents = new Dictionary<string, PreparedNodeExtents>(nodes.Count, StringComparer.Ordinal);
        foreach (var node in nodes) extents.Add(node.Id, MeasurePreparedNodeExtents(node, includeLabels));
        return extents;
    }

    /// <summary>Shares one badge measurement between both axes and the independent static viewport/neighborhood callers.</summary>
    private static PreparedNodeExtents MeasurePreparedNodeExtents(GraphSceneNode node, bool includeLabels) {
        var size = SafeNodeSize(node);
        var mark = PreparedNodeRadius(node);
        var halfWidth = mark; var halfHeight = mark;
        if (includeLabels && !string.IsNullOrWhiteSpace(node.BadgeText)) {
            var badge = NodeBadgeBounds(node, size);
            halfWidth = Math.Max(halfWidth, Math.Max(Math.Abs(badge.Left), Math.Abs(badge.Right)) + 1);
            halfHeight = Math.Max(halfHeight, Math.Max(Math.Abs(badge.Top), Math.Abs(badge.Bottom)) + 1);
        }
        if (!IsCardNode(node) && includeLabels) {
            var label = Math.Max(24, Math.Min(132, node.Label.Length * 3.5 + 10));
            if (!string.IsNullOrWhiteSpace(node.SecondaryLabel)) label = Math.Max(label, Math.Min(132, node.SecondaryLabel!.Length * 2.8 + 8));
            halfWidth = Math.Max(halfWidth, label);
            var labelBottom = node.Shape == GraphNodeShape.Text ? 9 : size + 24;
            if (!string.IsNullOrWhiteSpace(node.SecondaryLabel)) labelBottom += 15;
            halfHeight = Math.Max(halfHeight, labelBottom);
        }
        return new PreparedNodeExtents(halfWidth, halfHeight);
    }

    private static double PreparedNodeHalfWidth(GraphSceneNode node, bool includeLabels = true) =>
        MeasurePreparedNodeExtents(node, includeLabels).HalfWidth;

    private static double PreparedNodeHalfHeight(GraphSceneNode node, bool includeLabels = true) =>
        MeasurePreparedNodeExtents(node, includeLabels).HalfHeight;

    private readonly struct PreparedNodeExtents {
        internal double HalfWidth { get; }
        internal double HalfHeight { get; }
        internal PreparedNodeExtents(double halfWidth, double halfHeight) { HalfWidth = halfWidth; HalfHeight = halfHeight; }
    }
}
