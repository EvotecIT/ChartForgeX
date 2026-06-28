using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static string PolylinePath(IReadOnlyList<GraphScenePoint> points) {
        var writer = new StringBuilder();
        for (var i = 0; i < points.Count; i++) {
            writer.Append(i == 0 ? "M " : " L ");
            writer.Append(Number(points[i].X));
            writer.Append(' ');
            writer.Append(Number(points[i].Y));
        }

        return writer.ToString();
    }

    private static IReadOnlyList<GraphScenePoint> PolylineRenderPoints(GraphSceneEdge edge, GraphSceneNode? sourceNode, GraphSceneNode? targetNode, double? targetBoundaryInset, double? sourceBoundaryInset) {
        if (edge.RoutePoints.Count < 2) return edge.RoutePoints;
        if (!HasSourceArrow(edge) && !HasTargetArrow(edge) && !sourceBoundaryInset.HasValue && !targetBoundaryInset.HasValue) return edge.RoutePoints;
        var points = edge.RoutePoints.ToArray();
        var source = new Point(points[0].X, points[0].Y);
        var sourceGuide = new Point(points[1].X, points[1].Y);
        var renderedSource = SourceBoundaryPoint(edge, source, sourceGuide, null, sourceNode, sourceBoundaryInset);
        points[0] = new GraphScenePoint(renderedSource.X, renderedSource.Y);
        var last = points.Length - 1;
        var targetGuide = new Point(points[last - 1].X, points[last - 1].Y);
        var target = new Point(points[last].X, points[last].Y);
        var renderedTarget = TargetBoundaryPoint(edge, targetGuide, target, null, targetNode, targetBoundaryInset);
        points[last] = new GraphScenePoint(renderedTarget.X, renderedTarget.Y);
        return points;
    }

    private static Point PolylineMidpoint(IReadOnlyList<GraphScenePoint> points, double yOffset) {
        var total = 0d;
        for (var i = 1; i < points.Count; i++) total += Distance(points[i - 1], points[i]);
        if (total <= 0) return new Point(points[0].X, points[0].Y + yOffset);
        var halfway = total / 2;
        var traversed = 0d;
        for (var i = 1; i < points.Count; i++) {
            var length = Distance(points[i - 1], points[i]);
            if (traversed + length >= halfway) {
                var ratio = length <= 0 ? 0 : (halfway - traversed) / length;
                return new Point(points[i - 1].X + (points[i].X - points[i - 1].X) * ratio, points[i - 1].Y + (points[i].Y - points[i - 1].Y) * ratio + yOffset);
            }

            traversed += length;
        }

        return new Point(points[points.Count - 1].X, points[points.Count - 1].Y + yOffset);
    }

    private static double Distance(GraphScenePoint a, GraphScenePoint b) {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static string? RoutePointsData(GraphSceneEdge edge) {
        if (edge.RoutePoints.Count == 0) return null;
        return string.Join(";", edge.RoutePoints.Select(point => Number(point.X) + "," + Number(point.Y)));
    }
}
