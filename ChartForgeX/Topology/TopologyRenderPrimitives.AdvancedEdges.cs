using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public const double EndpointLabelFontSize = 9.5;

    public static ChartPoint EdgeEndpointLabelPoint(TopologyChart chart, TopologyRenderOptions options, ChartPoint endpoint, ChartPoint adjacent, string text) {
        var dx = adjacent.X - endpoint.X;
        var dy = adjacent.Y - endpoint.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.001) { dx = 1; dy = 0; length = 1; }
        var ux = dx / length;
        var uy = dy / length;
        var width = EstimateTextWidth(text.Trim(), EndpointLabelFontSize, true, options.TextMeasurement) + 6;
        var height = EndpointLabelFontSize + 6;
        var normalOffset = Math.Max(14, Math.Abs(uy) * width / 2 + Math.Abs(ux) * height / 2 + 4);
        var obstacles = chart.Nodes.Where(node => EffectiveNodeDisplayMode(node, options) != TopologyNodeDisplayMode.Hidden)
            .Select(node => EdgeLabelNodeObstacle(node, options, 4)).ToList();
        if (options.IncludeGroups && options.IncludeGroupLabels) obstacles.AddRange(chart.Groups.Select(group => LabelBox.FromGroupHeader(group, 4)));
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var segments = new List<(ChartPoint Start, ChartPoint End)>();
        foreach (var edge in chart.Edges) {
            if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;
            var route = EdgePoints(chart, edge, nodes);
            var painted = RenderedEdgeSamplePoints(chart, edge, nodes, route);
            for (var i = 1; i < painted.Count; i++) segments.Add((painted[i - 1], painted[i]));
            if (RenderedSourceMarker(edge, options.IncludeDirectionMarkers) != TopologyMarkerKind.None)
                obstacles.Add(LabelBox.FromCenter(route[0].X, route[0].Y, 30, 30));
            if (RenderedTargetMarker(edge, options.IncludeDirectionMarkers) != TopologyMarkerKind.None)
                obstacles.Add(LabelBox.FromCenter(route[route.Count - 1].X, route[route.Count - 1].Y, 30, 30));
        }
        var best = endpoint;
        var bestScore = double.PositiveInfinity;
        for (var step = 0; step < 6; step++) {
            foreach (var side in new[] { 1, -1 }) {
                var along = 10 + step * 12;
                var candidate = new ChartPoint(endpoint.X + ux * along - uy * normalOffset * side,
                    endpoint.Y + uy * along + ux * normalOffset * side);
                var box = LabelBox.FromCenter(candidate.X, candidate.Y, width, height);
                var clearance = box.Expand(3);
                var crossings = segments.Count(segment => clearance.Intersects(segment.Start, segment.End));
                var score = OverlapScore(box, obstacles) * 1000 + crossings * 1000 + step * 12 + (side == 1 ? 0 : 1);
                if (score >= bestScore) continue;
                best = candidate;
                bestScore = score;
            }
        }
        return best;
    }

    public static string EdgeDash(TopologyEdge edge) {
        if (edge.DashPattern.Count > 0) return string.Join(" ", edge.DashPattern.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)));
        return edge.LineStyle switch {
            TopologyEdgeLineStyle.Solid => "none",
            TopologyEdgeLineStyle.Dashed => "8 5",
            TopologyEdgeLineStyle.Dotted => "2 5",
            _ => EdgeDash(edge.Status)
        };
    }

    public static double[]? EdgePngDashArray(TopologyEdge edge) {
        if (edge.DashPattern.Count > 0) return edge.DashPattern.ToArray();
        var dash = EdgePngDash(edge);
        return dash.Dashed ? new[] { dash.Dash, dash.Gap } : null;
    }

    public static string EffectiveEdgeDash(TopologyEdge edge) {
        return edge.IsMuted && edge.DashPattern.Count == 0 && edge.LineStyle == TopologyEdgeLineStyle.Auto
            ? "none"
            : EdgeDash(edge);
    }

    public static double[]? EffectiveEdgePngDashArray(TopologyEdge edge) {
        return edge.IsMuted && edge.DashPattern.Count == 0 && edge.LineStyle == TopologyEdgeLineStyle.Auto
            ? null
            : EdgePngDashArray(edge);
    }

    public static TopologyMarkerKind EffectiveSourceMarker(TopologyEdge edge) {
        if (edge.SourceMarker.HasValue) return edge.SourceMarker.Value;
        return edge.Direction is VisualLinkDirection.Backward or VisualLinkDirection.Bidirectional ? TopologyMarkerKind.Arrow : TopologyMarkerKind.None;
    }

    public static TopologyMarkerKind EffectiveTargetMarker(TopologyEdge edge) {
        if (edge.TargetMarker.HasValue) return edge.TargetMarker.Value;
        return edge.Direction is VisualLinkDirection.Forward or VisualLinkDirection.Bidirectional ? TopologyMarkerKind.Arrow : TopologyMarkerKind.None;
    }

    public static TopologyMarkerKind RenderedSourceMarker(TopologyEdge edge, bool includeDirectionMarkers) {
        return edge.SourceMarker ?? (includeDirectionMarkers ? EffectiveSourceMarker(edge) : TopologyMarkerKind.None);
    }

    public static TopologyMarkerKind RenderedTargetMarker(TopologyEdge edge, bool includeDirectionMarkers) {
        return edge.TargetMarker ?? (includeDirectionMarkers ? EffectiveTargetMarker(edge) : TopologyMarkerKind.None);
    }

    private static void ApplyNamedEndpoint(TopologyChart chart, TopologyNode node, string portId, TopologyEdge edge, List<ChartPoint> points, int endpointIndex, int adjacentIndex) {
        var port = node.Ports.FirstOrDefault(candidate => string.Equals(candidate.Id, portId, StringComparison.Ordinal));
        if (port == null) return;
        if (TopologyLayoutEngine.UsesReadableDenseLayout(chart) &&
            edge.Routing == TopologyEdgeRouting.ObstacleAvoidingOrthogonal && edge.Waypoints.Count == 0 &&
            port.Side == TopologyEdgePort.Bottom && TopologyNodeFootprint.Caption(chart, node).Height > 0.5) {
            throw new InvalidOperationException("Readable dense tile '" + node.Id + "' cannot use named Bottom port '" +
                port.Id + "' beneath its visible caption. Use a top or side port, or hide the node label.");
        }
        var original = points[endpointIndex];
        var gap = EdgeEndpointGap;
        var point = port.Side switch {
            TopologyEdgePort.Top => new ChartPoint(node.X + node.Width * port.Offset, node.Y - gap),
            TopologyEdgePort.Right => new ChartPoint(node.X + node.Width + gap, node.Y + node.Height * port.Offset),
            TopologyEdgePort.Bottom => new ChartPoint(node.X + node.Width * port.Offset, node.Y + node.Height + gap),
            TopologyEdgePort.Left => new ChartPoint(node.X - gap, node.Y + node.Height * port.Offset),
            _ => original
        };
        points[endpointIndex] = point;
        PreserveOrthogonalEndpointLeg(edge, points, adjacentIndex, port.Side, original, point);
    }
}
