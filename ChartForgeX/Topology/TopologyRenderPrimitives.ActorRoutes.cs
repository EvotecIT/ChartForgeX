using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    private static List<ChartPoint> ApplyActorEndpointRouting(TopologyChart chart, TopologyEdge edge, TopologyNode source, TopologyNode target, List<ChartPoint> points) {
        if (edge.Waypoints.Count > 0 || edge.Routing is not (TopologyEdgeRouting.Straight or TopologyEdgeRouting.Orthogonal)) return points;
        var sourceAuto = edge.SourcePort == TopologyEdgePort.Auto && string.IsNullOrWhiteSpace(edge.SourcePortId);
        var targetAuto = edge.TargetPort == TopologyEdgePort.Auto && string.IsNullOrWhiteSpace(edge.TargetPortId);
        if ((!sourceAuto || source.Shape != TopologyNodeShape.Actor) && (!targetAuto || target.Shape != TopologyNodeShape.Actor)) return points;
        var options = chart.RenderOptions ?? chart.ResolveRenderOptions(null);
        if (edge.Routing == TopologyEdgeRouting.Orthogonal && sourceAuto && targetAuto &&
            (CrossesCaption(source, points[0], points[1]) || CrossesCaption(target, points[points.Count - 1], points[points.Count - 2]))) {
            // A side corridor avoids consuming a short gap between the caption and the
            // next node, and leaves independent, correctly directed endpoint legs.
            var right = TopologyNodeShapeGeometry.RoutingCenter(target).X >= TopologyNodeShapeGeometry.RoutingCenter(source).X;
            var port = right ? TopologyEdgePort.Right : TopologyEdgePort.Left;
            var start = BoundaryPoint(source, CenterX(target), CenterY(target), port);
            var end = BoundaryPoint(target, CenterX(source), CenterY(source), port);
            var corridor = right ? Math.Max(source.X + source.Width, target.X + target.Width) + EdgeEndpointGap + 16
                : Math.Min(source.X, target.X) - EdgeEndpointGap - 16;
            return new List<ChartPoint> { start, new(corridor, start.Y), new(corridor, end.Y), end };
        }
        var changed = false;
        if (sourceAuto) changed |= Detour(source, true);
        if (targetAuto) changed |= Detour(target, false);
        if (changed) {
            if (sourceAuto) Reattach(source, 0, 1);
            if (targetAuto) Reattach(target, points.Count - 1, points.Count - 2);
        }
        return points;

        double CaptionBottom(TopologyNode node) {
            var lines = DiagramNodeLabelLines(node, options);
            return DiagramNodeLabelCenterY(node) + Math.Max(0, lines.Count - 1) * 7 + 11;
        }

        bool CrossesCaption(TopologyNode node, ChartPoint endpoint, ChartPoint adjacent) =>
            node.Shape == TopologyNodeShape.Actor && options.IncludeNodeLabels &&
            LabelBox.FromBounds(node.X, node.Y + 70, node.X + node.Width, CaptionBottom(node)).Intersects(endpoint, adjacent);

        void Reattach(TopologyNode node, int index, int adjacentIndex) {
            var adjacent = points[adjacentIndex];
            if (edge.Routing == TopologyEdgeRouting.Straight)
                points[index] = BoundaryPoint(node, adjacent.X, adjacent.Y, TopologyEdgePort.Auto);
            else TopologyNodeShapeGeometry.AttachRoute(node, points, index, adjacentIndex, EdgeEndpointGap);
        }

        bool Detour(TopologyNode node, bool fromStart) {
            if (node.Shape != TopologyNodeShape.Actor || points.Count < 2) return false;
            var endpointIndex = fromStart ? 0 : points.Count - 1;
            var adjacentIndex = fromStart ? 1 : points.Count - 2;
            var adjacent = points[adjacentIndex];
            var center = TopologyNodeShapeGeometry.RoutingCenter(node);
            if (!CrossesCaption(node, points[endpointIndex], adjacent)) {
                if (edge.Routing != TopologyEdgeRouting.Straight) return false;
                var direction = TopologyNodeShapeGeometry.AutomaticActorPort(node, adjacent.X, adjacent.Y);
                var attachment = BoundaryPoint(node, adjacent.X, adjacent.Y, direction);
                var lead = direction switch {
                    TopologyEdgePort.Left => new ChartPoint(attachment.X - 14, attachment.Y),
                    TopologyEdgePort.Right => new ChartPoint(attachment.X + 14, attachment.Y),
                    TopologyEdgePort.Top => new ChartPoint(attachment.X, attachment.Y - 14),
                    _ => new ChartPoint(attachment.X, attachment.Y + 14)
                };
                points[endpointIndex] = attachment;
                points.Insert(fromStart ? 1 : endpointIndex, lead);
                return true;
            }
            var right = adjacent.X >= center.X;
            var port = right ? TopologyEdgePort.Right : TopologyEdgePort.Left;
            var outsideX = right ? node.X + node.Width + EdgeEndpointGap : node.X - EdgeEndpointGap;
            var clearY = CaptionBottom(node) + EdgeEndpointGap;
            if (edge.Routing == TopologyEdgeRouting.Orthogonal && points.Count >= 4) {
                // Move both corners of the anonymous crossbar instead of retaining
                // a return leg through the caption. Caller waypoints are excluded.
                var secondIndex = fromStart ? 2 : points.Count - 3;
                points[adjacentIndex] = new ChartPoint(adjacent.X, clearY);
                points[secondIndex] = new ChartPoint(points[secondIndex].X, clearY);
            }
            var detour = new List<ChartPoint> { new(outsideX, center.Y), new(outsideX, clearY) };
            points[endpointIndex] = TopologyNodeShapeGeometry.BoundaryPoint(node, outsideX, center.Y, port, EdgeEndpointGap);
            if (!fromStart) detour.Reverse();
            points.InsertRange(fromStart ? 1 : endpointIndex, detour);
            return true;
        }
    }

    public static IReadOnlyList<string> DiagramNodeLabelLines(TopologyNode node, TopologyRenderOptions options) =>
        NodeTextLines(node.Label, node.Width * .65, 12, true, options.MaxNodeLabelLines, options, node.MaximumLabelCharacters ?? NodeLabelMaxLength);

    public static double DiagramNodeLabelCenterY(TopologyNode node) => node.Shape == TopologyNodeShape.Actor ? node.Y + 84
        : node.Details.Count > 0 || !string.IsNullOrWhiteSpace(node.Subtitle) ? node.Y + 24 : CenterY(node);
}
