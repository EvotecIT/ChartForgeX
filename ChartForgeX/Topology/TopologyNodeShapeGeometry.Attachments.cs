using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    internal static ChartPoint RoutingCenter(TopologyNode node) => new(node.X + node.Width / 2,
        node.Shape == TopologyNodeShape.Actor ? node.Y + 34 : node.Y + node.Height / 2);

    internal static ChartPoint BoundaryPoint(TopologyNode node, double towardX, double towardY, TopologyEdgePort port, double gap) {
        var center = RoutingCenter(node);
        // Open stick figures have no enclosed boundary. Use an extremity rather than
        // a diagonal ray that can leave the torso immediately alongside an arm.
        if (node.Shape == TopologyNodeShape.Actor && port == TopologyEdgePort.Auto) port = AutomaticActorPort(node, towardX, towardY);
        var direction = port switch {
            TopologyEdgePort.Top => new ChartPoint(0, -1),
            TopologyEdgePort.Right => new ChartPoint(1, 0),
            TopologyEdgePort.Bottom => new ChartPoint(0, 1),
            TopologyEdgePort.Left => new ChartPoint(-1, 0),
            _ => new ChartPoint(towardX - center.X, towardY - center.Y)
        };
        return SurfacePoint(node, center, direction, gap);
    }

    internal static TopologyEdgePort AutomaticActorPort(TopologyNode node, double towardX, double towardY) {
        var center = RoutingCenter(node);
        var dx = towardX - center.X; var dy = towardY - center.Y;
        return Math.Abs(dx) >= Math.Abs(dy) ? (dx >= 0 ? TopologyEdgePort.Right : TopologyEdgePort.Left)
            : (dy >= 0 ? TopologyEdgePort.Bottom : TopologyEdgePort.Top);
    }

    // Keep the last route leg on the same line after fan-out or named-port spreading.
    internal static void AttachRoute(TopologyNode node, List<ChartPoint> points, int endpointIndex, int adjacentIndex, double gap) {
        if (!node.Shape.HasValue || points.Count < 2) return;
        var endpoint = points[endpointIndex];
        var adjacent = points[adjacentIndex];
        var direction = new ChartPoint(adjacent.X - endpoint.X, adjacent.Y - endpoint.Y);
        points[endpointIndex] = SurfacePoint(node, endpoint, direction, gap);
    }

    private static ChartPoint SurfacePoint(TopologyNode node, ChartPoint origin, ChartPoint direction, double gap) {
        var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        if (length < 0.000001) return origin;
        var unit = new ChartPoint(direction.X / length, direction.Y / length);
        var furthest = double.NegativeInfinity;
        foreach (var path in ChartMapPathParser.ParseSubpaths(Path(node), 1)) {
            for (var i = 1; i < path.Points.Count; i++) Intersect(path.Points[i - 1], path.Points[i]);
            if (path.IsClosed && path.Points.Count > 1) Intersect(path.Points[path.Points.Count - 1], path.Points[0]);
        }
        return double.IsNegativeInfinity(furthest) ? origin : new ChartPoint(origin.X + unit.X * (furthest + gap), origin.Y + unit.Y * (furthest + gap));

        void Intersect(ChartPoint start, ChartPoint end) {
            var sx = end.X - start.X; var sy = end.Y - start.Y;
            var ox = start.X - origin.X; var oy = start.Y - origin.Y;
            var denominator = unit.X * sy - unit.Y * sx;
            if (Math.Abs(denominator) < 0.000001) {
                if (Math.Abs(ox * unit.Y - oy * unit.X) < 0.000001) {
                    furthest = Math.Max(furthest, ox * unit.X + oy * unit.Y);
                    furthest = Math.Max(furthest, (end.X - origin.X) * unit.X + (end.Y - origin.Y) * unit.Y);
                }
                return;
            }
            var alongSegment = (ox * unit.Y - oy * unit.X) / denominator;
            if (alongSegment < -0.000001 || alongSegment > 1.000001) return;
            furthest = Math.Max(furthest, (ox * sy - oy * sx) / denominator);
        }
    }
}
