using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    private static List<ChartPoint> OffsetParallelRoutePreservingNamedEndpoints(List<ChartPoint> points, TopologyEdge edge, double offsetX, double offsetY) {
        var sourceEndpoint = points[0];
        var targetEndpoint = points[points.Count - 1];
        if (points.Count == 2 && !string.IsNullOrWhiteSpace(edge.SourcePortId) && !string.IsNullOrWhiteSpace(edge.TargetPortId)) {
            var dx = targetEndpoint.X - sourceEndpoint.X;
            var dy = targetEndpoint.Y - sourceEndpoint.Y;
            return new List<ChartPoint> {
                sourceEndpoint,
                new(sourceEndpoint.X + dx / 3 + offsetX, sourceEndpoint.Y + dy / 3 + offsetY),
                new(sourceEndpoint.X + dx * 2 / 3 + offsetX, sourceEndpoint.Y + dy * 2 / 3 + offsetY),
                targetEndpoint
            };
        }

        var offsetPoints = points.Select(point => new ChartPoint(point.X + offsetX, point.Y + offsetY)).ToList();
        if (!string.IsNullOrWhiteSpace(edge.SourcePortId)) offsetPoints[0] = sourceEndpoint;
        if (!string.IsNullOrWhiteSpace(edge.TargetPortId)) offsetPoints[offsetPoints.Count - 1] = targetEndpoint;
        return offsetPoints;
    }

    public static List<ChartPoint> NamedParallelCurveSamplePoints(TopologyEdge edge, IReadOnlyList<ChartPoint> points, int segments = 24) {
        if (edge.Routing != TopologyEdgeRouting.Curved || edge.Waypoints.Count != 0 || points.Count != 4 || string.IsNullOrWhiteSpace(edge.SourcePortId) || string.IsNullOrWhiteSpace(edge.TargetPortId)) return points.ToList();
        return RenderedEdgeSamplePointsForNamedCurve(points, segments);
    }

    private static List<ChartPoint> RenderedEdgeSamplePointsForNamedCurve(IReadOnlyList<ChartPoint> points, int segments) =>
        CubicCurveSamplePoints(points[0], points[1], points[2], points[3], segments);
}
