using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static List<ChartPoint> RenderedEdgeSamplePoints(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, IReadOnlyList<ChartPoint> points, int segments = 24) {
        if (edge.Interpolation.HasValue) return InterpolatedOrdinaryPath(chart, edge, points).Flatten(maximumPoints: 10000);
        if (edge.PathGeometry != null) return points.ToList();
        if (IsGeographicCurve(chart, edge, nodes)) return GeographicCurveSamplePoints(chart, edge, nodes, points, segments);
        if (edge.Routing != TopologyEdgeRouting.Curved || edge.Waypoints.Count != 0) return points.ToList();
        if (points.Count == 4 && !string.IsNullOrWhiteSpace(edge.SourcePortId) && !string.IsNullOrWhiteSpace(edge.TargetPortId)) return CubicCurveSamplePoints(points[0], points[1], points[2], points[3], segments);
        if (points.Count != 2) return points.ToList();
        var start = points[0]; var end = points[1];
        var source = nodes[edge.SourceNodeId]; var target = nodes[edge.TargetNodeId];
        var sourcePort = source.Ports.Find(port => port.Id == edge.SourcePortId)?.Side ?? edge.SourcePort;
        var targetPort = target.Ports.Find(port => port.Id == edge.TargetPortId)?.Side ?? edge.TargetPort;
        ResolveCurvePorts(source, target, ref sourcePort, ref targetPort);
        var horizontal = sourcePort is TopologyEdgePort.Left or TopologyEdgePort.Right;
        if (chart.LayoutMode != TopologyLayoutMode.MindMap && edge.SourcePort == TopologyEdgePort.Auto && edge.TargetPort == TopologyEdgePort.Auto
            && (horizontal ? Math.Abs(end.Y - start.Y) < .001 : Math.Abs(end.X - start.X) < .001)) {
            // Two smooth halves preserve a real curve and facing endpoint normals on aligned nodes.
            var span = horizontal ? end.X - start.X : end.Y - start.Y;
            var bow = Math.Min(28, Math.Max(8, Math.Abs(span) * .12));
            var middle = horizontal ? new ChartPoint((start.X + end.X) / 2, start.Y - bow)
                : new ChartPoint(start.X - bow, (start.Y + end.Y) / 2);
            var first = CubicCurveSamplePoints(start,
                horizontal ? new ChartPoint(start.X + span * .25, start.Y) : new ChartPoint(start.X, start.Y + span * .25),
                horizontal ? new ChartPoint(middle.X - span * .15, middle.Y) : new ChartPoint(middle.X, middle.Y - span * .15), middle, segments);
            var last = CubicCurveSamplePoints(middle,
                horizontal ? new ChartPoint(middle.X + span * .15, middle.Y) : new ChartPoint(middle.X, middle.Y + span * .15),
                horizontal ? new ChartPoint(end.X - span * .25, end.Y) : new ChartPoint(end.X, end.Y - span * .25), end, segments);
            first.AddRange(last.Skip(1)); return first;
        }
        StandardCurveControlPoints(points, out var firstControl, out var secondControl, sourcePort, targetPort);
        return CubicCurveSamplePoints(points[0], firstControl, secondControl, points[1], segments);
    }

    // Attachment and controls must use the same node centers, rather than inferring a second side from clipped endpoints.
    private static void ResolveCurvePorts(TopologyNode source, TopologyNode target, ref TopologyEdgePort sourcePort, ref TopologyEdgePort targetPort) {
        var start = TopologyNodeShapeGeometry.RoutingCenter(source);
        var end = TopologyNodeShapeGeometry.RoutingCenter(target);
        var horizontal = Math.Abs(end.X - start.X) >= Math.Abs(end.Y - start.Y);
        if (sourcePort == TopologyEdgePort.Auto) sourcePort = horizontal
            ? end.X >= start.X ? TopologyEdgePort.Right : TopologyEdgePort.Left
            : end.Y >= start.Y ? TopologyEdgePort.Bottom : TopologyEdgePort.Top;
        if (targetPort == TopologyEdgePort.Auto) targetPort = horizontal
            ? end.X >= start.X ? TopologyEdgePort.Left : TopologyEdgePort.Right
            : end.Y >= start.Y ? TopologyEdgePort.Top : TopologyEdgePort.Bottom;
    }

    private static void StandardCurveControlPoints(IReadOnlyList<ChartPoint> points, out ChartPoint firstControl, out ChartPoint secondControl,
        TopologyEdgePort sourcePort, TopologyEdgePort targetPort) {
        var start = points[0];
        var end = points[points.Count - 1];
        var dx = end.X - start.X; var dy = end.Y - start.Y;
        firstControl = Control(start, sourcePort);
        secondControl = Control(end, targetPort);

        ChartPoint Control(ChartPoint endpoint, TopologyEdgePort side) {
            var span = side is TopologyEdgePort.Left or TopologyEdgePort.Right ? Math.Abs(dx) : Math.Abs(dy);
            if (span < .001) span = Math.Max(Math.Abs(dx), Math.Abs(dy));
            return side switch {
                TopologyEdgePort.Left => new ChartPoint(endpoint.X - span / 2, endpoint.Y),
                TopologyEdgePort.Right => new ChartPoint(endpoint.X + span / 2, endpoint.Y),
                TopologyEdgePort.Top => new ChartPoint(endpoint.X, endpoint.Y - span / 2),
                _ => new ChartPoint(endpoint.X, endpoint.Y + span / 2)
            };
        }
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
