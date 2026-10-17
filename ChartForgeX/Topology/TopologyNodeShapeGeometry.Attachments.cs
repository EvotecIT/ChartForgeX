using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    internal static ChartPoint RoutingCenter(TopologyNode node) => new(node.X + node.Width / 2,
        node.Shape == TopologyNodeShape.Actor ? node.Y + 34 * ActorScale(node) : node.Y + node.Height / 2);

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
    internal static void AttachRoute(TopologyNode node, List<ChartPoint> points, int endpointIndex, int adjacentIndex, double gap,
        TopologyEdgePort port = TopologyEdgePort.Auto, double? offset = null) {
        if (!node.Shape.HasValue || points.Count < 2) return;
        var endpoint = points[endpointIndex];
        var adjacent = points[adjacentIndex];
        var center = RoutingCenter(node);
        if (offset.HasValue && port != TopologyEdgePort.Auto) {
            points[endpointIndex] = NamedPortPoint(node, port, offset.Value, gap);
            return;
        }
        var direction = port == TopologyEdgePort.Auto
            ? new ChartPoint(adjacent.X - endpoint.X, adjacent.Y - endpoint.Y)
            : port switch {
                TopologyEdgePort.Top => new ChartPoint(0, -1),
                TopologyEdgePort.Right => new ChartPoint(1, 0),
                TopologyEdgePort.Bottom => new ChartPoint(0, 1),
                TopologyEdgePort.Left => new ChartPoint(-1, 0),
                _ => new ChartPoint(adjacent.X - endpoint.X, adjacent.Y - endpoint.Y)
            };
        points[endpointIndex] = SurfacePoint(node, port == TopologyEdgePort.Auto ? endpoint : center, direction, gap);
    }

    private static ChartPoint NamedPortPoint(TopologyNode node, TopologyEdgePort port, double offset, double gap) {
        var vertical = port is TopologyEdgePort.Left or TopologyEdgePort.Right;
        var coordinate = vertical ? node.Y + node.Height * offset : node.X + node.Width * offset;
        var intersections = new List<ChartPoint>();
        if (node.Shape == TopologyNodeShape.Actor) {
            foreach (var part in SceneParts(node)) AddIntersections(part.Path.Flatten(1), part.Close);
        } else {
            foreach (var path in ChartMapPathParser.ParseSubpaths(Path(node), 1)) AddIntersections(path.Points, path.IsClosed);
        }
        if (intersections.Count == 0)
            return BoundaryPoint(node, node.X + node.Width / 2, node.Y + node.Height / 2, port, gap);
        var boundary = port switch {
            TopologyEdgePort.Right => intersections.OrderByDescending(point => point.X).First(),
            TopologyEdgePort.Left => intersections.OrderBy(point => point.X).First(),
            TopologyEdgePort.Bottom => intersections.OrderByDescending(point => point.Y).First(),
            _ => intersections.OrderBy(point => point.Y).First()
        };
        return port switch {
            TopologyEdgePort.Right => new ChartPoint(boundary.X + gap, boundary.Y),
            TopologyEdgePort.Left => new ChartPoint(boundary.X - gap, boundary.Y),
            TopologyEdgePort.Bottom => new ChartPoint(boundary.X, boundary.Y + gap),
            _ => new ChartPoint(boundary.X, boundary.Y - gap)
        };

        void AddIntersections(IReadOnlyList<ChartPoint> points, bool closed) {
            if (points.Count < 2) return;
            var segmentCount = closed ? points.Count : points.Count - 1;
            for (var index = 0; index < segmentCount; index++) {
                var first = points[index]; var second = points[(index + 1) % points.Count];
                var a = vertical ? first.Y : first.X;
                var b = vertical ? second.Y : second.X;
                var av = vertical ? first.X : first.Y;
                var bv = vertical ? second.X : second.Y;
                if (Math.Abs(a - coordinate) < .001) intersections.Add(first);
                if ((a < coordinate && b > coordinate) || (a > coordinate && b < coordinate)) {
                    var ratio = (coordinate - a) / (b - a);
                    intersections.Add(vertical
                        ? new ChartPoint(av + (bv - av) * ratio, coordinate)
                        : new ChartPoint(coordinate, av + (bv - av) * ratio));
                }
                if (index == segmentCount - 1 && Math.Abs(b - coordinate) < .001) intersections.Add(second);
            }
        }
    }

    private static ChartPoint SurfacePoint(TopologyNode node, ChartPoint origin, ChartPoint direction, double gap) {
        var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        if (length < 0.000001) return origin;
        var unit = new ChartPoint(direction.X / length, direction.Y / length);
        var furthest = double.NegativeInfinity;
        // Compact figures have fractional arm coordinates. Route against their painted
        // numeric contours rather than rounding a horizontal arm through SVG text first.
        if (node.Shape == TopologyNodeShape.Actor) {
            foreach (var part in SceneParts(node)) Contour(part.Path.Flatten(1), part.Close);
        } else foreach (var path in ChartMapPathParser.ParseSubpaths(Path(node), 1)) Contour(path.Points, path.IsClosed);
        return double.IsNegativeInfinity(furthest) ? origin : new ChartPoint(origin.X + unit.X * (furthest + gap), origin.Y + unit.Y * (furthest + gap));

        void Contour(IReadOnlyList<ChartPoint> points, bool closed) {
            for (var i = 1; i < points.Count; i++) Intersect(points[i - 1], points[i]);
            if (closed && points.Count > 1) Intersect(points[points.Count - 1], points[0]);
        }

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
