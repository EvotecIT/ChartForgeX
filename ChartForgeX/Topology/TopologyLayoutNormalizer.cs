using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static class TopologyLayoutNormalizer {
    internal const double NodeGap = 16;
    internal const double GroupPadding = 24;
    internal const double GroupHeaderBottomGap = 12;
    private const double RowTolerance = 36;
    private const double MinimumNodeWidth = 108;
    private const double MinimumAutoGroupWidth = 190;
    private const double MinimumAutoGroupHeight = 170;

    internal static void PrepareGroupShellsForPacking(TopologyChart chart, TopologyRenderOptions? options) {
        ResolveGroupHeaderOverlaps(chart, options ?? new TopologyRenderOptions());
        ExpandGroupsForNodes(chart);
    }

    /// <summary>Only translates/fits a solved layout; packing would invalidate its authored equalities.</summary>
    internal static void NormalizeAligned(TopologyChart chart, TopologyRenderOptions? options) =>
        FitViewportForRenderedContent(chart, options ?? new TopologyRenderOptions());

    public static void Normalize(TopologyChart chart, TopologyRenderOptions? options = null) {
        options ??= new TopologyRenderOptions();
        ExpandGroupsForNodes(chart);
        ResolveGroupHeaderOverlaps(chart, options);
        ResolveNodeOverlaps(chart);
        ExpandGroupsForNodes(chart);
        FitViewportForRenderedContent(chart, options);
    }

    private static void ResolveGroupHeaderOverlaps(TopologyChart chart, TopologyRenderOptions options) {
        if (!TopologyGroupHeader.IsDrawn(options)) return;
        var groups = chart.Groups
            .Where(group => !string.IsNullOrWhiteSpace(group.Id))
            .GroupBy(group => group.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var node in chart.Nodes) {
            if (string.IsNullOrWhiteSpace(node.GroupId) || !groups.TryGetValue(node.GroupId!, out var group)) continue;
            var parts = TopologyGroupHeader.LayoutParts(chart, group, options);
            if (!parts.Any(part => Intersects(node.X, node.Y, node.Width, node.Height, part.X, part.Y, part.Width, part.Height))) continue;
            var header = TopologyGroupHeader.ReservedBounds(chart, group, options);
            var safeY = header.Bottom + GroupHeaderBottomGap;
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

        var nodesByGroup = chart.Nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.GroupId))
            .GroupBy(node => node.GroupId!, StringComparer.Ordinal)
            .ToDictionary(nodeSet => nodeSet.Key, nodeSet => nodeSet.ToList(), StringComparer.Ordinal);

        foreach (var group in groups.Values) {
            if (!nodesByGroup.TryGetValue(group.Id, out var nodes) || nodes.Count == 0) {
                if (group.Width <= 0) group.Width = MinimumAutoGroupWidth;
                if (group.Height <= 0) group.Height = MinimumAutoGroupHeight;
                continue;
            }

            var footprints = nodes.Select(node => chart.RenderOptions is {} options
                && TopologyNodeFootprint.ExplicitOccupiedBounds(node, options) is {} occupied
                    ? occupied : new ChartForgeX.Primitives.ChartRect(node.X, node.Y, node.Width, node.Height)).ToArray();
            var requiredRight = footprints.Max(bounds => bounds.Right) + GroupPadding;
            var requiredBottom = footprints.Max(bounds => bounds.Bottom) + GroupPadding;
            if (group.Width <= 0) {
                var requiredLeft = footprints.Min(bounds => bounds.Left) - GroupPadding;
                if (requiredLeft < group.X) group.X = requiredLeft;
                group.Width = Math.Max(MinimumAutoGroupWidth, requiredRight - group.X);
            }

            if (group.Height <= 0) {
                var requiredTop = footprints.Min(bounds => bounds.Top) - GroupPadding;
                if (requiredTop < group.Y) group.Y = requiredTop;
                group.Height = Math.Max(MinimumAutoGroupHeight, requiredBottom - group.Y);
            } else if (requiredBottom > group.Y + group.Height) {
                group.Height = requiredBottom - group.Y;
            }
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

    private static void FitViewportForRenderedContent(TopologyChart chart, TopologyRenderOptions options) {
        var bounds = MeasureContent(chart, options);
        if (!bounds.HasContent) return;

        var surfaceInset = CanvasSurfaceInset(chart, options);
        var targetLeft = chart.Viewport.Padding + surfaceInset;
        var targetTop = chart.Viewport.Padding + surfaceInset + (string.IsNullOrWhiteSpace(chart.Title) && string.IsNullOrWhiteSpace(chart.Subtitle) ? 0 : HeaderReservedHeight);
        var dx = bounds.Left < targetLeft ? targetLeft - bounds.Left : 0;
        var dy = bounds.Top < targetTop ? targetTop - bounds.Top : 0;
        if (Math.Abs(dx) > 0.0001 || Math.Abs(dy) > 0.0001) {
            ShiftContent(chart, dx, dy);
            bounds = bounds.Shift(dx, dy);
        }

        var neededWidth = bounds.Right + chart.Viewport.Padding + surfaceInset;
        var neededHeight = bounds.Bottom + chart.Viewport.Padding + surfaceInset + LegendReservedHeight(chart.Legend, chart.Viewport);
        if (neededWidth > chart.Viewport.Width) chart.Viewport.Width = Math.Ceiling(neededWidth);
        if (neededHeight > chart.Viewport.Height || FitsHeightToContent(chart, options)) chart.Viewport.Height = Math.Ceiling(neededHeight);
    }

    /// <summary>
    /// Returns true when the canvas height follows the content instead of the requested viewport height: readable dense
    /// grouped layouts size their site panels to their content, so a taller viewport would only leave a gap above the
    /// legend.
    /// </summary>
    private static bool FitsHeightToContent(TopologyChart chart, TopologyRenderOptions options) =>
        options.ReadableDenseLayout && chart.LayoutMode == TopologyLayoutMode.DenseGrouped && chart.Groups.Count > 0;

    private static ContentBounds MeasureContent(TopologyChart chart, TopologyRenderOptions options) {
        var bounds = ContentBounds.Empty;
        foreach (var group in chart.Groups) bounds = bounds.Include(group.X, group.Y, group.X + group.Width, group.Y + group.Height);
        foreach (var node in chart.Nodes) bounds = IncludeNodeVisualBounds(bounds, node, options);
        foreach (var point in chart.Edges.SelectMany(edge => edge.Waypoints)) bounds = bounds.Include(point.X, point.Y, point.X, point.Y);
        foreach (var edge in chart.Edges) if (edge.PathGeometry != null) {
            var path = edge.PathGeometry.Capture().Bounds(); var half = (edge.StrokeWidth ?? chart.Theme.SeriesStrokeWidth) / 2;
            bounds = bounds.Include(path.Left - half, path.Top - half, path.Right + half, path.Bottom + half);
        }

        foreach (var decoration in chart.PathDecorations) {
            var path = decoration.Capture().Bounds(); var half = decoration.Stroke.HasValue ? decoration.StrokeWidth / 2 : 0;
            bounds = bounds.Include(path.Left - half, path.Top - half, path.Right + half, path.Bottom + half);
        }
        if (chart.Edges.Any(edge => edge.SourceMarkerStyle != null || edge.TargetMarkerStyle != null) && CanMeasureEdgeLabels(chart)) {
            var nodes = TopologyGroupEndpoints.Resolve(chart);
            foreach (var edge in chart.Edges) if (StyledMarkerBounds(chart, edge, options, nodes) is ChartRect marker)
                bounds = bounds.Include(marker.Left, marker.Top, marker.Right, marker.Bottom);
        }
        if (options.IncludeEdgeLabels && CanMeasureEdgeLabels(chart)) {
            foreach (var layout in EdgeLabelLayouts(chart, options)) {
                if (layout.Edge.LabelPosition == null) {
                    bounds = bounds.Include(layout.CenterX - layout.Width / 2, layout.CenterY - layout.Height / 2, layout.CenterX + layout.Width / 2, layout.CenterY + layout.Height / 2);
                    continue;
                }
                var labelBounds = TopologyTextLayout.PositionEnvelope(layout.Edge.LabelPosition, layout.CenterX, layout.CenterY, layout.Width, layout.Height);
                bounds = bounds.Include(labelBounds.Left, labelBounds.Top, labelBounds.Right, labelBounds.Bottom);
            }
        }
        if (options.IncludeEndpointLabels && chart.Edges.Any(edge => !string.IsNullOrWhiteSpace(edge.SourceLabel) || !string.IsNullOrWhiteSpace(edge.TargetLabel)) && CanMeasureEdgeLabels(chart)) {
            var nodes = TopologyGroupEndpoints.Resolve(chart);
            foreach (var edge in chart.Edges.Where(edge => !string.IsNullOrWhiteSpace(edge.SourceLabel) || !string.IsNullOrWhiteSpace(edge.TargetLabel))) {
                var route = TopologyResolvedRouteSamples.Sample(chart, options, edge, nodes, EdgePoints(chart, edge, nodes));
                IncludeEndpoint(edge, edge.SourceLabel, route[0], route[1]);
                IncludeEndpoint(edge, edge.TargetLabel, route[route.Count - 1], route[route.Count - 2]);
            }
            void IncludeEndpoint(TopologyEdge edge, string? text, ChartPoint endpoint, ChartPoint adjacent) {
                if (string.IsNullOrWhiteSpace(text)) return;
                var layout = EdgeEndpointLabelLayout(chart, options, endpoint, adjacent, text!, TopologyPreparedEdgePlan.Units(edge));
                bounds = bounds.Include(layout.Bounds.Left, layout.Bounds.Top, layout.Bounds.Right, layout.Bounds.Bottom);
            }
        }

        return bounds;
    }

    private static bool CanMeasureEdgeLabels(TopologyChart chart) {
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (string.IsNullOrWhiteSpace(node.Id) || !nodeIds.Add(node.Id)) return false;
        }
        foreach (var group in chart.Groups) nodeIds.Add(group.Id);

        return chart.Edges.All(edge => nodeIds.Contains(edge.SourceNodeId) && nodeIds.Contains(edge.TargetNodeId));
    }

    private static ContentBounds IncludeNodeVisualBounds(ContentBounds bounds, TopologyNode node, TopologyRenderOptions options) {
        var rendered = TopologyNodeFootprint.RenderedBounds(node, options);
        return rendered.HasValue ? bounds.Include(rendered.Value.X, rendered.Value.Y, rendered.Value.Right, rendered.Value.Bottom) : bounds;
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

        for (var index = 0; index < chart.PathDecorations.Count; index++)
            chart.PathDecorations[index] = chart.PathDecorations[index].Transform(point => new ChartPoint(point.X + dx, point.Y + dy), 1);
        foreach (var edge in chart.Edges) {
            if (edge.PathGeometry != null) edge.PathGeometry = edge.PathGeometry.Transform(point => new ChartPoint(point.X + dx, point.Y + dy));
            if (edge.LabelPosition != null) edge.LabelPosition = edge.LabelPosition.Transform(point => new ChartPoint(point.X + dx, point.Y + dy));
            if (edge.HasLabelAnchorOverride) {
                edge.LabelAnchorX += dx;
                edge.LabelAnchorY += dy;
            }

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
