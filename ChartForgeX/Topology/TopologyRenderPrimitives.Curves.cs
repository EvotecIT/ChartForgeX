using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static List<ChartPoint> RenderedEdgeSamplePoints(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, IReadOnlyList<ChartPoint> points, int segments = 24) {
        if (IsGeographicCurve(chart, edge, nodes)) return GeographicCurveSamplePoints(chart, edge, nodes, points, segments);
        if (edge.Routing != TopologyEdgeRouting.Curved || edge.Waypoints.Count != 0) return points.ToList();
        if (points.Count == 4 && !string.IsNullOrWhiteSpace(edge.SourcePortId) && !string.IsNullOrWhiteSpace(edge.TargetPortId)) return CubicCurveSamplePoints(points[0], points[1], points[2], points[3], segments);
        if (points.Count != 2) return points.ToList();
        StandardCurveControlPoints(points, out var firstControl, out var secondControl);
        return CubicCurveSamplePoints(points[0], firstControl, secondControl, points[1], segments);
    }

    public static void StandardCurveControlPoints(IReadOnlyList<ChartPoint> points, out ChartPoint firstControl, out ChartPoint secondControl) {
        var start = points[0];
        var end = points[points.Count - 1];
        var lift = Math.Max(40, Math.Abs(end.X - start.X) * 0.12);
        firstControl = new ChartPoint(start.X, start.Y - lift);
        secondControl = new ChartPoint(end.X, end.Y - lift);
    }

    private static List<ChartPoint> CubicCurveSamplePoints(ChartPoint start, ChartPoint firstControl, ChartPoint secondControl, ChartPoint end, int segments) {
        var count = Math.Max(2, segments);
        var sampled = new List<ChartPoint>(count + 1);
        for (var index = 0; index <= count; index++) {
            var t = index / (double)count;
            var u = 1 - t;
            sampled.Add(new ChartPoint(
                u * u * u * start.X + 3 * u * u * t * firstControl.X + 3 * u * t * t * secondControl.X + t * t * t * end.X,
                u * u * u * start.Y + 3 * u * u * t * firstControl.Y + 3 * u * t * t * secondControl.Y + t * t * t * end.Y));
        }
        return sampled;
    }
}
