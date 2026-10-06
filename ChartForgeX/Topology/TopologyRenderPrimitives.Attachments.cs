using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    private static ChartPoint BoundaryPoint(TopologyNode node, double towardX, double towardY) {
        return BoundaryPoint(node, towardX, towardY, TopologyEdgePort.Auto);
    }

    private static ChartPoint BoundaryPoint(TopologyNode node, double towardX, double towardY, TopologyEdgePort port) {
        if (node.Shape.HasValue) return TopologyNodeShapeGeometry.BoundaryPoint(node, towardX, towardY, port, EdgeEndpointGap);
        var centerX = CenterX(node);
        var centerY = CenterY(node);
        if (port != TopologyEdgePort.Auto) {
            return port switch {
                TopologyEdgePort.Top => new ChartPoint(centerX, node.Y - EdgeEndpointGap),
                TopologyEdgePort.Right => new ChartPoint(node.X + node.Width + EdgeEndpointGap, centerY),
                TopologyEdgePort.Bottom => new ChartPoint(centerX, node.Y + node.Height + EdgeEndpointGap),
                TopologyEdgePort.Left => new ChartPoint(node.X - EdgeEndpointGap, centerY),
                _ => new ChartPoint(centerX, centerY)
            };
        }

        var dx = towardX - centerX;
        var dy = towardY - centerY;
        if (Math.Abs(dx) < 0.000001 && Math.Abs(dy) < 0.000001) return new ChartPoint(centerX, centerY);

        var scaleX = Math.Abs(dx) < 0.000001 ? double.PositiveInfinity : (node.Width / 2 + EdgeEndpointGap) / Math.Abs(dx);
        var scaleY = Math.Abs(dy) < 0.000001 ? double.PositiveInfinity : (node.Height / 2 + EdgeEndpointGap) / Math.Abs(dy);
        var scale = Math.Min(scaleX, scaleY);
        return new ChartPoint(centerX + dx * scale, centerY + dy * scale);
    }

    private static bool ShouldRouteHorizontally(TopologyEdgePort sourcePort, TopologyEdgePort targetPort, bool fallback) {
        var hasHorizontalPort = sourcePort is TopologyEdgePort.Left or TopologyEdgePort.Right || targetPort is TopologyEdgePort.Left or TopologyEdgePort.Right;
        var hasVerticalPort = sourcePort is TopologyEdgePort.Top or TopologyEdgePort.Bottom || targetPort is TopologyEdgePort.Top or TopologyEdgePort.Bottom;
        if (hasHorizontalPort && !hasVerticalPort) return true;
        if (hasVerticalPort && !hasHorizontalPort) return false;
        return fallback;
    }

    private static double Distance(ChartPoint a, ChartPoint b) {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

}
