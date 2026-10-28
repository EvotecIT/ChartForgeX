using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    private static List<ChartPoint> ApplyActorEndpointRouting(TopologyChart chart, TopologyEdge edge, TopologyNode source, TopologyNode target, List<ChartPoint> points) {
        if (edge.Waypoints.Count > 0 || edge.Routing is not (TopologyEdgeRouting.Straight or TopologyEdgeRouting.Orthogonal)) return points;
        var sourceAuto = edge.SourcePort == TopologyEdgePort.Auto && string.IsNullOrWhiteSpace(edge.SourcePortId);
        var targetAuto = edge.TargetPort == TopologyEdgePort.Auto && string.IsNullOrWhiteSpace(edge.TargetPortId);
        if ((!sourceAuto || !TopologyNodeShapeGeometry.IsActor(source)) && (!targetAuto || !TopologyNodeShapeGeometry.IsActor(target))) return points;
        var options = chart.RenderOptions ?? chart.ResolveRenderOptions(null);
        if (edge.Routing == TopologyEdgeRouting.Orthogonal && sourceAuto && targetAuto &&
            (CrossesCaption(source, points[0], points[1]) || CrossesCaption(target, points[points.Count - 1], points[points.Count - 2]))) {
            // A side corridor avoids consuming a short gap between the caption and the
            // next node, and leaves independent, correctly directed endpoint legs.
            var right = TopologyNodeShapeGeometry.RoutingCenter(target).X >= TopologyNodeShapeGeometry.RoutingCenter(source).X;
            var port = right ? TopologyEdgePort.Right : TopologyEdgePort.Left;
            var start = BoundaryPoint(source, CenterX(target), CenterY(target), port);
            var end = BoundaryPoint(target, CenterX(source), CenterY(source), port);
            var sourceCaption = CaptionBounds(source); var targetCaption = CaptionBounds(target);
            var units = Math.Max(TopologyPreparedNodeBasis.Units(source), TopologyPreparedNodeBasis.Units(target));
            var gap = EdgeEndpointGap * units; var lead = 16 * units;
            var corridor = right ? Math.Max(Math.Max(source.X + source.Width, target.X + target.Width), Math.Max(sourceCaption.Right, targetCaption.Right)) + gap + lead
                : Math.Min(Math.Min(source.X, target.X), Math.Min(sourceCaption.Left, targetCaption.Left)) - gap - lead;
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

        ChartRect CaptionBounds(TopologyNode node) {
            if (!TopologyNodeShapeGeometry.IsActor(node) || !options.IncludeNodeLabels) return new ChartRect(node.X, node.Y, node.Width, 0);
            var mode = EffectiveNodeDisplayMode(node, options);
            if (mode == TopologyNodeDisplayMode.Icon) {
                if (!options.IncludeIconLabels) return new ChartRect(node.X, node.Y, node.Width, 0);
                var width = IconLabelPlateWidth(node, chart.TextMeasurement, options.ResolvedIconLabelFontSize);
                return new ChartRect(CenterX(node) - width / 2, IconLabelPlateY(node), width,
                    IconLabelPlateHeight(node, chart.TextMeasurement, options.ResolvedIconLabelFontSize));
            }
            if (mode == TopologyNodeDisplayMode.Tile) {
                var caption = TopologyNodeFootprint.RenderedCaption(node, options, chart.TextMeasurement);
                var width = Math.Max(node.Width, caption.Width);
                var gap = 4 * TopologyPreparedNodeBasis.Units(node);
                return new ChartRect(CenterX(node) - width / 2, node.Y + node.Height + gap, width, Math.Max(0, caption.Height - gap));
            }
            if (node.LabelContent.IsStructured) {
                var height = Math.Max(1, options.MaxNodeLabelLines)
                    * TopologyTextLayout.StructuredCaptionRowHeight(node, chart.TextMeasurement) + 1e-8;
                return new ChartRect(node.X, DiagramNodeLabelCenterY(node) - height / 2, node.Width, height);
            }
            if (TopologyActorCaption.Applies(node, options)) return TopologyActorCaption.Prepare(node, options, chart.TextMeasurement).Bounds;
            var lines = DiagramNodeLabelLines(node, options);
            return new ChartRect(node.X, DiagramNodeLabelCenterY(node) - 14, node.Width,
                Math.Max(0, lines.Count - 1) * 14 + 25);
        }

        bool CrossesCaption(TopologyNode node, ChartPoint endpoint, ChartPoint adjacent) {
            var bounds = CaptionBounds(node);
            return bounds.Height > 0 && LabelBox.FromBounds(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom).Intersects(endpoint, adjacent);
        }

        void Reattach(TopologyNode node, int index, int adjacentIndex) {
            var adjacent = points[adjacentIndex];
            if (edge.Routing == TopologyEdgeRouting.Straight)
                points[index] = BoundaryPoint(node, adjacent.X, adjacent.Y, TopologyEdgePort.Auto);
            else TopologyNodeShapeGeometry.AttachRoute(node, points, index, adjacentIndex, EdgeEndpointGap);
        }

        bool Detour(TopologyNode node, bool fromStart) {
            if (!TopologyNodeShapeGeometry.IsActor(node) || points.Count < 2) return false;
            var endpointIndex = fromStart ? 0 : points.Count - 1;
            var adjacentIndex = fromStart ? 1 : points.Count - 2;
            var adjacent = points[adjacentIndex];
            var center = TopologyNodeShapeGeometry.RoutingCenter(node);
            var units = TopologyPreparedNodeBasis.Units(node);
            if (!CrossesCaption(node, points[endpointIndex], adjacent)) {
                if (edge.Routing != TopologyEdgeRouting.Straight) return false;
                var direction = TopologyNodeShapeGeometry.AutomaticActorPort(node, adjacent.X, adjacent.Y);
                var attachment = BoundaryPoint(node, adjacent.X, adjacent.Y, direction);
                var lead = direction switch {
                    TopologyEdgePort.Left => new ChartPoint(attachment.X - 14 * units, attachment.Y),
                    TopologyEdgePort.Right => new ChartPoint(attachment.X + 14 * units, attachment.Y),
                    TopologyEdgePort.Top => new ChartPoint(attachment.X, attachment.Y - 14 * units),
                    _ => new ChartPoint(attachment.X, attachment.Y + 14 * units)
                };
                points[endpointIndex] = attachment;
                points.Insert(fromStart ? 1 : endpointIndex, lead);
                return true;
            }
            var right = adjacent.X >= center.X;
            var port = right ? TopologyEdgePort.Right : TopologyEdgePort.Left;
            var caption = CaptionBounds(node);
            var outsideX = right ? Math.Max(node.X + node.Width, caption.Right) + EdgeEndpointGap * units : Math.Min(node.X, caption.Left) - EdgeEndpointGap * units;
            var clearY = caption.Bottom + EdgeEndpointGap * units;
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
        NodeTextLines(node.Label, node.Width * .65, 12 * TopologyPreparedNodeBasis.Units(node), true, options.MaxNodeLabelLines, options, node.MaximumLabelCharacters ?? NodeLabelMaxLength);

    public static double DiagramNodeLabelCenterY(TopologyNode node) => TopologyNodeShapeGeometry.IsActor(node) ? node.Y + 68 * TopologyNodeShapeGeometry.ActorScale(node) + 16 * TopologyPreparedNodeBasis.Units(node)
        : node.Details.Count > 0 || !string.IsNullOrWhiteSpace(node.Subtitle) ? node.Y + 24 : CenterY(node);
}
