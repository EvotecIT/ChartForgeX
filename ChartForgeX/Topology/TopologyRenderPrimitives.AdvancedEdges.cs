using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static ChartPoint EdgeEndpointLabelPoint(ChartPoint endpoint, ChartPoint adjacent) {
        var dx = adjacent.X - endpoint.X;
        var dy = adjacent.Y - endpoint.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.001) return endpoint;
        var ux = dx / length;
        var uy = dy / length;
        return new ChartPoint(endpoint.X + ux * 10 - uy * 14, endpoint.Y + uy * 10 + ux * 14);
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

    private static void ApplyNamedEndpoint(TopologyNode node, string portId, TopologyEdge edge, List<ChartPoint> points, int endpointIndex, int adjacentIndex) {
        var port = node.Ports.FirstOrDefault(candidate => string.Equals(candidate.Id, portId, StringComparison.Ordinal));
        if (port == null) return;
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
