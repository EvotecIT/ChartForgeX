using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyEdgeRouter {
    // The same clamp domain is observed by semantic capture and ordinary route candidates.
    // Clamp(value,min,max) returns constant max when min>max; [max,max] preserves that exact legacy result.
    internal static ChartRect RoutingViewport(TopologyChart chart) {
        var minX=chart.Viewport.Padding; var maxX=chart.Viewport.Width-chart.Viewport.Padding;
        var minY=chart.Viewport.Padding+(string.IsNullOrWhiteSpace(chart.Title)&&string.IsNullOrWhiteSpace(chart.Subtitle)?0:72);
        var maxY=chart.Viewport.Height-chart.Viewport.Padding-LegendReservedHeight(chart.Legend,chart.Viewport);
        return new ChartRect(Math.Min(minX,maxX),Math.Min(minY,maxY),Math.Max(0,maxX-minX),Math.Max(0,maxY-minY));
    }

    private static List<ChartPoint>? HorizontalPortAwareRoute(ChartPoint sourcePoint, ChartPoint targetPoint, double corridorY, TopologyEdge edge, RouteBox sourceCaption, RouteBox targetCaption, IReadOnlyList<RouteBox>? bodyObstacles = null, double routingUnits = 1) {
        if (bodyObstacles == null && !IsHorizontalPort(edge.SourcePort) && !IsHorizontalPort(edge.TargetPort)) return null;
        var sourceStub = bodyObstacles == null ? HorizontalPortStub(sourcePoint, edge.SourcePort, sourceCaption, routingUnits) : ClearancePortLead(sourcePoint, edge.SourcePort, sourceCaption, bodyObstacles, routingUnits);
        var targetStub = bodyObstacles == null ? HorizontalPortStub(targetPoint, edge.TargetPort, targetCaption, routingUnits) : ClearancePortLead(targetPoint, edge.TargetPort, targetCaption, bodyObstacles, routingUnits);
        return NormalizePoints(new[] {
            sourcePoint,
            sourceStub,
            new ChartPoint(sourceStub.X, corridorY),
            new ChartPoint(targetStub.X, corridorY),
            targetStub,
            targetPoint
        }, routingUnits);
    }

    private static ChartPoint HorizontalPortStub(ChartPoint point, TopologyEdgePort port, RouteBox caption, double routingUnits = 1) {
        if (!IsHorizontalPort(port)) return point;
        var x = point.X + PortDirection(port) * 14 * routingUnits;
        // Turn outside a wide caption instead of leaving the card correctly and then running through its text.
        if (caption.Height > 0.5 * routingUnits) x = port == TopologyEdgePort.Left ? Math.Min(x, caption.Left - 7 * routingUnits) : Math.Max(x, caption.Right + 7 * routingUnits);
        return new ChartPoint(x, point.Y);
    }

    private static List<ChartPoint>? VerticalPortAwareRoute(ChartPoint sourcePoint, ChartPoint targetPoint, double corridorX, TopologyEdge edge, IReadOnlyList<RouteBox>? bodyObstacles = null, double routingUnits = 1) {
        if (bodyObstacles == null && !IsVerticalPort(edge.SourcePort) && !IsVerticalPort(edge.TargetPort)) return null;
        var sourceStub = bodyObstacles == null ? IsVerticalPort(edge.SourcePort) ? new ChartPoint(sourcePoint.X, sourcePoint.Y + PortDirection(edge.SourcePort) * 14 * routingUnits) : sourcePoint
            : ClearancePortLead(sourcePoint, edge.SourcePort, default, bodyObstacles, routingUnits);
        var targetStub = bodyObstacles == null ? IsVerticalPort(edge.TargetPort) ? new ChartPoint(targetPoint.X, targetPoint.Y + PortDirection(edge.TargetPort) * 14 * routingUnits) : targetPoint
            : ClearancePortLead(targetPoint, edge.TargetPort, default, bodyObstacles, routingUnits);
        return NormalizePoints(new[] {
            sourcePoint,
            sourceStub,
            new ChartPoint(corridorX, sourceStub.Y),
            new ChartPoint(corridorX, targetStub.Y),
            targetStub,
            targetPoint
        }, routingUnits);
    }

    private static bool IsHorizontalPort(TopologyEdgePort port) => port == TopologyEdgePort.Left || port == TopologyEdgePort.Right;

    private static bool IsVerticalPort(TopologyEdgePort port) => port == TopologyEdgePort.Top || port == TopologyEdgePort.Bottom;

    private static double PortDirection(TopologyEdgePort port) => port == TopologyEdgePort.Left || port == TopologyEdgePort.Top ? -1 : 1;

    private static List<ChartPoint> NormalizePoints(IEnumerable<ChartPoint> points, double routingUnits = 1) {
        var normalized = new List<ChartPoint>();
        foreach (var point in points) {
            if (normalized.Count == 0 || Distance(normalized[normalized.Count - 1], point) > 0.001 * routingUnits) normalized.Add(point);
        }

        return normalized;
    }

    private static IEnumerable<RouteCorridor> RouteXCandidates(TopologyChart chart, TopologyNode source, TopologyNode target, IReadOnlyList<RouteBox> obstacles, double routeLane, double routingUnits = 1, ChartRect? routingViewport = null) {
        var margin = 18 * routingUnits;
        var domain = routingViewport ?? RoutingViewport(chart);
        var min = domain.Left; var max = domain.Right;
        var sourceBounds = NodeRouteBox(chart, source);
        var targetBounds = NodeRouteBox(chart, target);
        yield return new RouteCorridor("vertical-mid", Clamp((CenterX(source) + CenterX(target)) / 2 + routeLane, min, max));
        yield return new RouteCorridor("vertical-viewport-left", Clamp(min + margin, min, max));
        yield return new RouteCorridor("vertical-viewport-right", Clamp(max - margin, min, max));
        yield return new RouteCorridor("vertical-source-left", Clamp(sourceBounds.Left - margin, min, max));
        yield return new RouteCorridor("vertical-source-right", Clamp(sourceBounds.Right + margin, min, max));
        yield return new RouteCorridor("vertical-target-left", Clamp(targetBounds.Left - margin, min, max));
        yield return new RouteCorridor("vertical-target-right", Clamp(targetBounds.Right + margin, min, max));
        foreach (var obstacle in obstacles) {
            yield return new RouteCorridor("vertical-obstacle-left", Clamp(obstacle.Left - margin, min, max));
            yield return new RouteCorridor("vertical-obstacle-right", Clamp(obstacle.Right + margin, min, max));
        }
    }

    private static IEnumerable<RouteCorridor> RouteYCandidates(TopologyChart chart, TopologyNode source, TopologyNode target, IReadOnlyList<RouteBox> obstacles, double routeLane, double routingUnits = 1, ChartRect? routingViewport = null) {
        var margin = 18 * routingUnits;
        var domain = routingViewport ?? RoutingViewport(chart);
        var min = domain.Top; var max = domain.Bottom;
        var sourceBounds = NodeRouteBox(chart, source);
        var targetBounds = NodeRouteBox(chart, target);
        yield return new RouteCorridor("horizontal-mid", Clamp((CenterY(source) + CenterY(target)) / 2 + routeLane, min, max));
        yield return new RouteCorridor("horizontal-viewport-top", Clamp(min + margin, min, max));
        yield return new RouteCorridor("horizontal-viewport-bottom", Clamp(max - margin, min, max));
        yield return new RouteCorridor("horizontal-source-top", Clamp(source.Y - margin, min, max));
        yield return new RouteCorridor("horizontal-source-bottom", Clamp(sourceBounds.Bottom + margin, min, max));
        yield return new RouteCorridor("horizontal-target-top", Clamp(target.Y - margin, min, max));
        yield return new RouteCorridor("horizontal-target-bottom", Clamp(targetBounds.Bottom + margin, min, max));
        foreach (var obstacle in obstacles) {
            yield return new RouteCorridor("horizontal-obstacle-top", Clamp(obstacle.Top - margin, min, max));
            yield return new RouteCorridor("horizontal-obstacle-bottom", Clamp(obstacle.Bottom + margin, min, max));
        }
    }

}
