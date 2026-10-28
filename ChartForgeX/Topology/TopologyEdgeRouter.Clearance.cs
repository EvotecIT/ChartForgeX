using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyEdgeRouter {
    /// <summary>Reconsiders existing corridors only when the final named/fanned/shape endpoints cross a real foreign body.</summary>
    internal static List<ChartPoint> RefineBodyClearance(TopologyChart chart, TopologyEdge edge, TopologyNode source, TopologyNode target,
        List<ChartPoint> original, double routeLane) {
        if (original.Count < 2) return original;
        var bodies = new List<RouteBox>();
        var options = chart.RenderOptions ?? new TopologyRenderOptions();
        foreach (var node in chart.Nodes) {
            if (node.Id == source.Id || node.Id == target.Id || IsRouteBackdropArtwork(node)
                || EffectiveNodeDisplayMode(node, options) == TopologyNodeDisplayMode.Hidden) continue;
            // Real interiors are distinct from search padding. Padding may cover a free
            // endpoint gap while the actual card remains fully avoidable.
            if (node.Width > .000002 && node.Height > .000002)
                bodies.Add(new RouteBox(node.X + .000001, node.Y + .000001, node.X + node.Width - .000001, node.Y + node.Height - .000001));
        }
        if (RouteObstacleHits(original, bodies) == 0) return original;
        var ported = TopologyLayoutEngine.Clone(edge);
        // A named port remains the physical side owner even when its edge's enum is Auto.
        ported.SourcePort = source.Ports.Find(port => port.Id == edge.SourcePortId)?.Side ?? ported.SourcePort;
        ported.TargetPort = target.Ports.Find(port => port.Id == edge.TargetPortId)?.Side ?? ported.TargetPort;
        if (ported.SourcePort == TopologyEdgePort.Auto) ported.SourcePort = BoundarySide(source, original[0]);
        if (ported.TargetPort == TopologyEdgePort.Auto) ported.TargetPort = BoundarySide(target, original[original.Count - 1]);
        var sourcePoint = ClearanceEndpoint(source, ported.SourcePort, edge.SourcePortId, original[0], bodies);
        var targetPoint = ClearanceEndpoint(target, ported.TargetPort, edge.TargetPortId, original[original.Count - 1], bodies);
        var obstacles = RouteObstacles(chart, source.Id, target.Id, edge, includeCaptions: true);
        var segments = RouteSegments(chart, edge); var sourceCaption = CaptionBox(chart, source); var targetCaption = CaptionBox(chart, target);
        var candidates = new List<RouteCandidate> { new("existing-final", original) };
        foreach (var corridor in RouteXCandidates(chart, source, target, obstacles, routeLane)) {
            var points = VerticalPortAwareRoute(sourcePoint, targetPoint, corridor.Value, ported, bodies);
            if (points != null) candidates.Add(new RouteCandidate(corridor.Name + "-body-clearance", points));
        }
        foreach (var corridor in RouteYCandidates(chart, source, target, obstacles, routeLane)) {
            var points = HorizontalPortAwareRoute(sourcePoint, targetPoint, corridor.Value, ported, sourceCaption, targetCaption, bodies);
            if (points != null) candidates.Add(new RouteCandidate(corridor.Name + "-body-clearance", points));
        }
        var readable = TopologyLayoutEngine.UsesReadableDenseLayout(chart);
        return candidates.GroupBy(candidate => RouteKey(candidate.Points), StringComparer.Ordinal)
            .Select(group => group.OrderBy(candidate => candidate.Corridor, StringComparer.Ordinal).First())
            .Select(candidate => BuildPlan("ObstacleAvoidingOrthogonal", candidate.Corridor, candidate.Points, obstacles, segments, edge, candidates.Count,
                chart.TextMeasurement, options.IncludeEdgeLabels, options.ResolvedEdgeLabelScale))
            .OrderBy(plan => EndpointAttachmentFailures(plan.Points, source, target))
            .ThenBy(plan => ClearancePortFailures(plan.Points, ported))
            .ThenBy(plan => RouteObstacleHits(plan.Points, bodies) + OwnerReentries(plan.Points, source, target))
            .ThenBy(plan => RouteScore(plan, ported, readable, sourceCaption, targetCaption))
            .ThenBy(plan => RouteLength(plan.Points)).ThenBy(plan => RouteKey(plan.Points), StringComparer.Ordinal).First().Points;
    }

    private static ChartPoint ClearanceEndpoint(TopologyNode node, TopologyEdgePort port, string? portId, ChartPoint original, IReadOnlyList<RouteBox> bodies) {
        if (!bodies.Any(body => body.Left < original.X && original.X < body.Right && body.Top < original.Y && original.Y < body.Bottom)) return original;
        // Named offsets are already resolved. The existing attachment owner projects
        // that exact cross coordinate to the same physical side, including shapes.
        var points = new List<ChartPoint> { original, Outward(original, port, 1) };
        AttachEndpoint(node, port, portId, points, 0, 1);
        return ClearLead(points[0], port, EdgeEndpointGap, bodies);
    }

    private static ChartPoint ClearancePortLead(ChartPoint point, TopologyEdgePort port, RouteBox caption, IReadOnlyList<RouteBox> bodies, double routingUnits = 1) {
        var desired = IsHorizontalPort(port) ? HorizontalPortStub(point, port, caption, routingUnits) : Outward(point, port, 14 * routingUnits);
        return ClearLead(point, port, Distance(point, desired), bodies);
    }

    private static ChartPoint ClearLead(ChartPoint point, TopologyEdgePort port, double distance, IReadOnlyList<RouteBox> bodies) {
        var horizontal = IsHorizontalPort(port); var positive = port is TopologyEdgePort.Right or TopologyEdgePort.Bottom;
        foreach (var body in bodies) {
            if (horizontal ? point.Y < body.Top || point.Y > body.Bottom : point.X < body.Left || point.X > body.Right) continue;
            var available = horizontal ? positive ? body.Left - point.X : point.X - body.Right : positive ? body.Top - point.Y : point.Y - body.Bottom;
            if (available >= 0 && available < distance * 2) distance = Math.Min(distance, available / 2);
        }
        return Outward(point, port, distance);
    }

    private static ChartPoint Outward(ChartPoint point, TopologyEdgePort port, double distance) =>
        IsHorizontalPort(port) ? new ChartPoint(point.X + PortDirection(port) * distance, point.Y) : new ChartPoint(point.X, point.Y + PortDirection(port) * distance);

    private static int ClearancePortFailures(IReadOnlyList<ChartPoint> points, TopologyEdge edge) =>
        (PortSegmentPenalty(edge.SourcePort, points, 0, 1, true) > 0 ? 1 : 0)
        + (PortSegmentPenalty(edge.TargetPort, points, points.Count - 1, points.Count - 2, false) > 0 ? 1 : 0);

    private static int OwnerReentries(IReadOnlyList<ChartPoint> points, TopologyNode source, TopologyNode target) {
        var sourceBox = new RouteBox(source.X + .000001, source.Y + .000001, source.X + source.Width - .000001, source.Y + source.Height - .000001);
        var targetBox = new RouteBox(target.X + .000001, target.Y + .000001, target.X + target.Width - .000001, target.Y + target.Height - .000001);
        var hits = 0;
        for (var i = 0; i + 1 < points.Count; i++) {
            if (i > 0 && sourceBox.Intersects(points[i], points[i + 1])) hits++;
            if (i + 2 < points.Count && targetBox.Intersects(points[i], points[i + 1])) hits++;
        }
        return hits;
    }
}
