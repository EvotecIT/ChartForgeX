using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    private static void ApplyEndpointPortSpreading(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, TopologyNode source, TopologyNode target, List<ChartPoint> points, bool namedOnly = false) {
        if (points.Count < 2) return;
        // A direct segment has no independent legs for two named ports: adjusting either
        // endpoint would otherwise move the opposite endpoint through its adjacent point.
        if (UsesOrthogonalRoute(edge) && points.Count == 2 &&
            !string.IsNullOrWhiteSpace(edge.SourcePortId) && !string.IsNullOrWhiteSpace(edge.TargetPortId)) {
            var start = points[0];
            var end = points[1];
            if (Math.Abs(start.Y - end.Y) < 0.001) {
                var middleX = (start.X + end.X) / 2;
                points.Insert(1, new ChartPoint(middleX, start.Y));
                points.Insert(2, new ChartPoint(middleX, end.Y));
            } else if (Math.Abs(start.X - end.X) < 0.001) {
                var middleY = (start.Y + end.Y) / 2;
                points.Insert(1, new ChartPoint(start.X, middleY));
                points.Insert(2, new ChartPoint(end.X, middleY));
            }
        }
        var sourcePort = EffectiveEndpointPort(edge, source, target, true);
        var targetPort = EffectiveEndpointPort(edge, target, source, false);
        if (!string.IsNullOrWhiteSpace(edge.SourcePortId)) {
            ApplyNamedEndpoint(chart, source, edge.SourcePortId!, edge, points, 0, 1);
        } else if (!namedOnly && sourcePort != TopologyEdgePort.Auto && LegMatchesPort(chart, edge, points[0], points[1], sourcePort)) {
            var original = points[0];
            var spread = SpreadEndpoint(chart, edge, nodes, source, sourcePort, original);
            points[0] = spread;
            PreserveOrthogonalEndpointLeg(edge, points, 1, sourcePort, original, spread);
        }

        if (!string.IsNullOrWhiteSpace(edge.TargetPortId)) {
            ApplyNamedEndpoint(chart, target, edge.TargetPortId!, edge, points, points.Count - 1, points.Count - 2);
        } else if (!namedOnly && targetPort != TopologyEdgePort.Auto && LegMatchesPort(chart, edge, points[points.Count - 1], points[points.Count - 2], targetPort)) {
            var targetIndex = points.Count - 1;
            var original = points[targetIndex];
            var spread = SpreadEndpoint(chart, edge, nodes, target, targetPort, original);
            points[targetIndex] = spread;
            PreserveOrthogonalEndpointLeg(edge, points, targetIndex - 1, targetPort, original, spread);
        }
    }

    // In a readable dense layout an edge the route planner could not connect falls back to the corridor candidates,
    // which may leave through a different side than the inferred port; spreading along the recorded side would then
    // push the endpoint off the card, so it only applies when the end leg runs along the port's axis.
    private static bool LegMatchesPort(TopologyChart chart, TopologyEdge edge, ChartPoint end, ChartPoint next, TopologyEdgePort port) {
        if (!TopologyLayoutEngine.UsesReadableDenseLayout(chart) || edge.Routing != TopologyEdgeRouting.ObstacleAvoidingOrthogonal || edge.Waypoints.Count > 0) return true;
        var horizontal = Math.Abs(end.Y - next.Y) < 0.01;
        var vertical = Math.Abs(end.X - next.X) < 0.01;
        if (horizontal == vertical) return true;
        return port is TopologyEdgePort.Left or TopologyEdgePort.Right ? horizontal : vertical;
    }

    private static ChartPoint SpreadEndpoint(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, TopologyNode node, TopologyEdgePort port, ChartPoint point) {
        var related = EndpointPortPeers(chart, nodes, node.Id, port);
        if (related.Count < 2) return point;
        var index = related.FindIndex(candidate => ReferenceEquals(candidate, edge));
        if (index < 0) return point;
        // UML markers are wider than the fine lines on ordinary topology cards.
        var offset = (index - (related.Count - 1) / 2.0) * (node.Shape.HasValue ? 16 : EdgePortFanSpacing);
        var maximum = port is TopologyEdgePort.Top or TopologyEdgePort.Bottom
            ? Math.Max(0, node.Width / 2 - EdgeEndpointSidePadding)
            : Math.Max(0, node.Height / 2 - EdgeEndpointSidePadding);
        if (node.Shape == TopologyNodeShape.Actor) maximum = Math.Min(maximum,
            port is TopologyEdgePort.Top or TopologyEdgePort.Bottom ? 14 : 26);
        offset = Clamp(offset, -maximum, maximum);
        var spread = port switch {
            TopologyEdgePort.Top or TopologyEdgePort.Bottom => new ChartPoint(point.X + offset, point.Y),
            TopologyEdgePort.Left or TopologyEdgePort.Right => new ChartPoint(point.X, point.Y + offset),
            _ => point
        };
        return spread;
    }

    private static void PreserveOrthogonalEndpointLeg(TopologyEdge edge, List<ChartPoint> points, int adjacentIndex, TopologyEdgePort port, ChartPoint original, ChartPoint spread) {
        if (!UsesOrthogonalRoute(edge) || adjacentIndex < 0 || adjacentIndex >= points.Count) return;
        var adjacent = points[adjacentIndex];
        if (port is TopologyEdgePort.Top or TopologyEdgePort.Bottom) {
            var deltaX = spread.X - original.X;
            if (Math.Abs(deltaX) > 0.0001 && Math.Abs(adjacent.X - original.X) < 0.0001) {
                points[adjacentIndex] = new ChartPoint(adjacent.X + deltaX, adjacent.Y);
            }

            return;
        }

        if (port is TopologyEdgePort.Left or TopologyEdgePort.Right) {
            var deltaY = spread.Y - original.Y;
            if (Math.Abs(deltaY) > 0.0001 && Math.Abs(adjacent.Y - original.Y) < 0.0001) {
                points[adjacentIndex] = new ChartPoint(adjacent.X, adjacent.Y + deltaY);
            }
        }
    }

    private static List<TopologyEdge> EndpointPortPeers(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes, string nodeId, TopologyEdgePort port) {
        return chart.Edges
            .Where(candidate => EndpointUsesPort(candidate, nodeId, port, nodes))
            .OrderBy(candidate => EndpointPeerSortKey(candidate, nodeId, nodes))
            .ThenBy(candidate => candidate.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static bool EndpointUsesPort(TopologyEdge edge, string nodeId, TopologyEdgePort port, IReadOnlyDictionary<string, TopologyNode> nodes) {
        if (!nodes.TryGetValue(edge.SourceNodeId, out var source) || !nodes.TryGetValue(edge.TargetNodeId, out var target)) return false;
        return (string.Equals(edge.SourceNodeId, nodeId, StringComparison.Ordinal) && EffectiveEndpointPort(edge, source, target, true) == port)
            || (string.Equals(edge.TargetNodeId, nodeId, StringComparison.Ordinal) && EffectiveEndpointPort(edge, target, source, false) == port);
    }

    private static TopologyEdgePort EffectiveEndpointPort(TopologyEdge edge, TopologyNode node, TopologyNode other, bool fromSource) {
        var port = fromSource ? edge.SourcePort : edge.TargetPort;
        if (port != TopologyEdgePort.Auto || !node.Shape.HasValue || edge.Routing != TopologyEdgeRouting.Orthogonal || edge.Waypoints.Count > 0) return port;
        if (!string.IsNullOrWhiteSpace(fromSource ? edge.SourcePortId : edge.TargetPortId)) return port;
        var center = TopologyNodeShapeGeometry.RoutingCenter(node);
        var toward = TopologyNodeShapeGeometry.RoutingCenter(other);
        var horizontal = ShouldRouteHorizontally(edge.SourcePort, edge.TargetPort, Math.Abs(toward.X - center.X) >= Math.Abs(toward.Y - center.Y));
        return OrthogonalShapePort(node, port, horizontal, toward);
    }

    private static TopologyEdgePort OrthogonalShapePort(TopologyNode node, TopologyEdgePort port, bool horizontal, ChartPoint toward) {
        if (!node.Shape.HasValue || port != TopologyEdgePort.Auto) return port;
        var center = TopologyNodeShapeGeometry.RoutingCenter(node);
        return horizontal ? (toward.X >= center.X ? TopologyEdgePort.Right : TopologyEdgePort.Left)
            : (toward.Y >= center.Y ? TopologyEdgePort.Bottom : TopologyEdgePort.Top);
    }

    private static double EndpointPeerSortKey(TopologyEdge edge, string nodeId, IReadOnlyDictionary<string, TopologyNode> nodes) {
        var otherId = string.Equals(edge.SourceNodeId, nodeId, StringComparison.Ordinal) ? edge.TargetNodeId : edge.SourceNodeId;
        if (!nodes.TryGetValue(nodeId, out var node) || !nodes.TryGetValue(otherId, out var other)) return 0;
        var dx = CenterX(other) - CenterX(node);
        var dy = CenterY(other) - CenterY(node);
        return Math.Atan2(dy, dx);
    }

}
