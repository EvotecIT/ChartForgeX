using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    /// <summary>Repairs only anonymous Actor-endpoint routes that actually intersect another measured enabled Actor caption.</summary>
    private static List<ChartPoint> AvoidOtherActorCaptions(TopologyChart chart, TopologyEdge edge, TopologyNode source, TopologyNode target,
        List<ChartPoint> points, double routeLane, IReadOnlyDictionary<string, TopologyNode> nodes) {
        if (edge.PathGeometry != null || edge.Waypoints.Count != 0 || edge.Routing is not (TopologyEdgeRouting.Straight or TopologyEdgeRouting.Orthogonal)
            || edge.SourcePort != TopologyEdgePort.Auto || edge.TargetPort != TopologyEdgePort.Auto
            || !string.IsNullOrWhiteSpace(edge.SourcePortId) || !string.IsNullOrWhiteSpace(edge.TargetPortId)
            || !TopologyNodeShapeGeometry.IsActor(source) && !TopologyNodeShapeGeometry.IsActor(target)) return points;
        var options = chart.RenderOptions ?? chart.ResolveRenderOptions(null);
        var captions = new List<ChartRect>(); var crosses = false;
        foreach (var node in chart.Nodes) {
            if (node.Id == source.Id || node.Id == target.Id || !TopologyActorCaption.Applies(node, options)) continue;
            // This is the same actual title/subtitle plan used by paint, layout footprint and label obstacles.
            var bounds = TopologyActorCaption.Prepare(node, options, chart.TextMeasurement).Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) continue;
            captions.Add(bounds); crosses |= IntersectsActorCaption(bounds, points);
        }
        if (!crosses) return points; // Preserve the exact incumbent point object/bytes on ordinary routes.
        var routed = TopologyEdgeRouter.RouteAroundActorCaptions(chart, edge, source, target, routeLane, captions).Points;
        // Reuse the actual fan-out owner on the new route; never move an authored/named port to obtain clearance.
        var spread = ApplySafeEndpointSpreading(chart, edge, nodes, source, target, new List<ChartPoint>(routed));
        foreach (var caption in captions) if (IntersectsActorCaption(caption, spread)) return routed;
        return spread;
    }

    private static bool IntersectsActorCaption(ChartRect bounds, IReadOnlyList<ChartPoint> points) {
        var box = LabelBox.FromBounds(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
        for (var index = 1; index < points.Count; index++) if (box.Intersects(points[index - 1], points[index])) return true;
        return false;
    }
}
