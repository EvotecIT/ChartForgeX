using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    private static List<ChartPoint> CalculateEdgePoints(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes) {
        if (edge.PathGeometry != null) return edge.PathGeometry.RoutePoints();
        var source = nodes[edge.SourceNodeId];
        var target = nodes[edge.TargetNodeId];
        var offset = EdgeRouteOffset(chart, edge);
        var routeLane = EdgeRouteLane(chart, edge, offset);
        var points = edge.Waypoints.Count == 0
            ? (edge.Routing == TopologyEdgeRouting.ObstacleAvoidingOrthogonal
                || chart.Alignments.Count > 0 && edge.Routing == TopologyEdgeRouting.Orthogonal)
                ? TopologyEdgeRouter.Route(chart, edge, source, target, routeLane).Points
                : EdgePoints(source, target, edge.Routing, edge.SourcePort, edge.TargetPort, routeLane)
            : EdgePoints(source, target, edge.Waypoints, edge.SourcePort, edge.TargetPort);
        points = ApplySafeEndpointSpreading(chart, edge, nodes, source, target, points);
        if (Math.Abs(offset) < 0.0001 || UsesOrthogonalRoute(edge)) {
            points = ApplyActorEndpointRouting(chart, edge, source, target, points);
            if (edge.Waypoints.Count == 0 && (edge.Routing == TopologyEdgeRouting.ObstacleAvoidingOrthogonal
                || chart.Alignments.Count > 0 && edge.Routing == TopologyEdgeRouting.Orthogonal))
                points = TopologyEdgeRouter.RefineBodyClearance(chart, edge, source, target, points, routeLane);
            return AvoidOtherActorCaptions(chart, edge, source, target, points, routeLane, nodes);
        }

        var vectorSource = string.Compare(edge.SourceNodeId, edge.TargetNodeId, StringComparison.Ordinal) <= 0 ? source : target;
        var vectorTarget = ReferenceEquals(vectorSource, source) ? target : source;
        var dx = CenterX(vectorTarget) - CenterX(vectorSource);
        var dy = CenterY(vectorTarget) - CenterY(vectorSource);
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.0001) return points;

        var ox = -dy / length * offset;
        var oy = dx / length * offset;
        return AvoidOtherActorCaptions(chart, edge, source, target,
            ApplyActorEndpointRouting(chart, edge, source, target, OffsetParallelRoutePreservingNamedEndpoints(points, edge, ox, oy)), routeLane, nodes);
    }
}
