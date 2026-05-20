using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static List<ChartPoint> DottedMapConnectorProjectedRoute(ChartMapConnector connector, ChartMapViewport viewport, ChartRect map) {
        var source = connector.RoutePoints.Length > 0
            ? connector.RoutePoints.Select(point => new ChartPoint(point.Longitude, point.Latitude))
            : new[] { new ChartPoint(connector.FromLongitude, connector.FromLatitude), new ChartPoint(connector.ToLongitude, connector.ToLatitude) };
        return source.Select(point => new ChartPoint(ProjectMapX(map, viewport, point.X), ProjectMapY(map, viewport, point.Y))).ToList();
    }

    private static string DottedMapConnectorPath(IReadOnlyList<ChartPoint> routePoints, ChartPoint control, bool hasWaypoints) {
        if (!hasWaypoints) return $"M {F(routePoints[0].X)} {F(routePoints[0].Y)} Q {F(control.X)} {F(control.Y)} {F(routePoints[routePoints.Count - 1].X)} {F(routePoints[routePoints.Count - 1].Y)}";
        var path = new StringBuilder();
        for (var i = 0; i < routePoints.Count; i++) {
            path.Append(i == 0 ? "M " : " L ");
            path.Append(F(routePoints[i].X)).Append(' ').Append(F(routePoints[i].Y));
        }

        return path.ToString();
    }

    private static string BuildDottedMapConnectorArrowPath(IReadOnlyList<ChartPoint> routePoints, double dot) {
        var points = DottedMapConnectorArrowPoints(routePoints, dot);
        return "M " + F(points[0].X) + " " + F(points[0].Y) + " L " + F(points[1].X) + " " + F(points[1].Y) + " L " + F(points[2].X) + " " + F(points[2].Y) + " Z";
    }

    private static ChartPoint[] DottedMapConnectorArrowPoints(IReadOnlyList<ChartPoint> routePoints, double dot) {
        var tip = DottedMapPolylinePoint(routePoints, 0.63);
        var before = DottedMapPolylinePoint(routePoints, 0.58);
        var after = DottedMapPolylinePoint(routePoints, 0.68);
        var dx = after.X - before.X;
        var dy = after.Y - before.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.000001 && routePoints.Count >= 2) {
            var previous = routePoints[Math.Max(0, routePoints.Count - 2)];
            var last = routePoints[routePoints.Count - 1];
            dx = last.X - previous.X;
            dy = last.Y - previous.Y;
            length = Math.Max(0.000001, Math.Sqrt(dx * dx + dy * dy));
        }

        var unitX = dx / length;
        var unitY = dy / length;
        var arrowLength = Math.Max(10, dot * 4.8);
        var arrowWidth = Math.Max(6, dot * 2.4);
        var baseX = tip.X - unitX * arrowLength;
        var baseY = tip.Y - unitY * arrowLength;
        var perpX = -unitY;
        var perpY = unitX;
        return new[] {
            tip,
            new ChartPoint(baseX + perpX * arrowWidth / 2, baseY + perpY * arrowWidth / 2),
            new ChartPoint(baseX - perpX * arrowWidth / 2, baseY - perpY * arrowWidth / 2)
        };
    }

    private static ChartPoint DottedMapPolylinePoint(IReadOnlyList<ChartPoint> points, double progress) {
        if (points.Count == 0) return new ChartPoint(0, 0);
        if (points.Count == 1) return points[0];
        var total = 0.0;
        for (var i = 0; i < points.Count - 1; i++) total += Distance(points[i], points[i + 1]);
        if (total <= 0) return points[0];
        var target = total * Clamp(progress, 0, 1);
        var walked = 0.0;
        for (var i = 0; i < points.Count - 1; i++) {
            var segment = Distance(points[i], points[i + 1]);
            if (walked + segment >= target) {
                var t = (target - walked) / Math.Max(segment, 0.000001);
                return new ChartPoint(points[i].X + (points[i + 1].X - points[i].X) * t, points[i].Y + (points[i + 1].Y - points[i].Y) * t);
            }

            walked += segment;
        }

        return points[points.Count - 1];
    }

    private static List<ChartPoint> DottedMapSmoothRoute(IReadOnlyList<ChartPoint> points) {
        if (points.Count <= 2) return points.ToList();
        var smoothed = new List<ChartPoint>();
        for (var i = 0; i < points.Count - 1; i++) {
            var previous = points[Math.Max(0, i - 1)];
            var current = points[i];
            var next = points[i + 1];
            var following = points[Math.Min(points.Count - 1, i + 2)];
            var steps = Math.Max(8, Math.Min(22, (int)Math.Ceiling(Distance(current, next) / 34.0)));
            for (var step = 0; step < steps; step++) {
                if (i > 0 || step > 0) {
                    var t = step / (double)steps;
                    smoothed.Add(CatmullRom(previous, current, next, following, t));
                } else {
                    smoothed.Add(current);
                }
            }
        }

        smoothed.Add(points[points.Count - 1]);
        return smoothed;
    }

    private static ChartPoint CatmullRom(ChartPoint p0, ChartPoint p1, ChartPoint p2, ChartPoint p3, double t) {
        var t2 = t * t;
        var t3 = t2 * t;
        var x = 0.5 * ((2 * p1.X) + (-p0.X + p2.X) * t + (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 + (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3);
        var y = 0.5 * ((2 * p1.Y) + (-p0.Y + p2.Y) * t + (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 + (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3);
        return new ChartPoint(x, y);
    }

    private static bool IsVisibleMapConnector(ChartMapViewport viewport, ChartMapConnector connector) {
        if (connector.RoutePoints.Length == 0) return IsVisibleMapCoordinate(viewport, connector.FromLongitude, connector.FromLatitude) && IsVisibleMapCoordinate(viewport, connector.ToLongitude, connector.ToLatitude);
        foreach (var point in connector.RoutePoints) {
            if (!IsVisibleMapCoordinate(viewport, point.Longitude, point.Latitude)) return false;
        }

        return true;
    }

    private static double Distance(ChartPoint a, ChartPoint b) {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
