using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    private static List<ChartPoint> AvoidActorCaptions(TopologyEdge edge, TopologyNode source, TopologyNode target, List<ChartPoint> points) {
        if (edge.Waypoints.Count > 0 || edge.Routing is not (TopologyEdgeRouting.Straight or TopologyEdgeRouting.Orthogonal)) return points;
        var changed = false;
        if (edge.SourcePort == TopologyEdgePort.Auto && string.IsNullOrWhiteSpace(edge.SourcePortId)) changed |= Detour(source, true);
        if (edge.TargetPort == TopologyEdgePort.Auto && string.IsNullOrWhiteSpace(edge.TargetPortId)) changed |= Detour(target, false);
        if (changed) {
            // A new final leg can approach an ellipse at a different angle.
            if (edge.SourcePort == TopologyEdgePort.Auto && string.IsNullOrWhiteSpace(edge.SourcePortId))
                TopologyNodeShapeGeometry.AttachRoute(source, points, 0, 1, EdgeEndpointGap);
            if (edge.TargetPort == TopologyEdgePort.Auto && string.IsNullOrWhiteSpace(edge.TargetPortId))
                TopologyNodeShapeGeometry.AttachRoute(target, points, points.Count - 1, points.Count - 2, EdgeEndpointGap);
        }
        return points;

        bool Detour(TopologyNode node, bool fromStart) {
            if (node.Shape != TopologyNodeShape.Actor || points.Count < 2) return false;
            var endpointIndex = fromStart ? 0 : points.Count - 1;
            var adjacent = points[fromStart ? 1 : points.Count - 2];
            // Reserve the caption area beneath the stick figure, including wrapped labels.
            var bottom = node.Y + Math.Max(102, node.Height);
            var caption = LabelBox.FromBounds(node.X, node.Y + 70, node.X + node.Width, bottom);
            if (!caption.Intersects(points[endpointIndex], adjacent)) return false;
            var center = TopologyNodeShapeGeometry.RoutingCenter(node);
            var right = adjacent.X >= center.X;
            var port = right ? TopologyEdgePort.Right : TopologyEdgePort.Left;
            var outsideX = right ? node.X + node.Width + EdgeEndpointGap : node.X - EdgeEndpointGap;
            var clearY = edge.Routing == TopologyEdgeRouting.Orthogonal ? Math.Max(bottom + EdgeEndpointGap, adjacent.Y) : bottom + EdgeEndpointGap;
            var detour = new List<ChartPoint> { new(outsideX, center.Y), new(outsideX, clearY) };
            if (edge.Routing == TopologyEdgeRouting.Orthogonal && Math.Abs(clearY - adjacent.Y) > .001)
                detour.Add(new ChartPoint(adjacent.X, clearY));
            points[endpointIndex] = TopologyNodeShapeGeometry.BoundaryPoint(node, outsideX, center.Y, port, EdgeEndpointGap);
            if (!fromStart) detour.Reverse();
            points.InsertRange(fromStart ? 1 : endpointIndex, detour);
            return true;
        }
    }
}
