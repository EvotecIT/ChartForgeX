using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChartValidator {
    internal TopologyValidationResult ValidateExternalPresentationInputs(TopologyChart chart, TopologyRenderOptions options) {
        var result = new TopologyValidationResult();
        ValidateExternalPresentation(chart, result, options);
        return result;
    }
    private static void ValidateExternalPresentation(TopologyChart chart, TopologyValidationResult result, TopologyRenderOptions? options = null) {
        options ??= chart.RenderOptions ?? new TopologyRenderOptions();
        foreach (var node in chart.Nodes) {
            var error = TopologyExternalPresentationRules.Error(EffectiveNodeDisplayMode(node, options), node.Shape, node.TableContent != null,
                chart.LayoutMode, node.CaptionPlacement, node.CaptionSurface, node.CaptionGap, node.GlyphScale, node.CornerRadius, node.CaptionTextStyle != null);
            if (error != null) Add(result, "node-external-presentation", "Node '" + node.Id + "' " + error + ".", node.Id);
        }
        if (!options.ReadableDenseLayout) return;
        foreach (var edge in chart.Edges) {
            if (edge.PathGeometry != null || edge.Waypoints.Count != 0 || edge.Routing != TopologyEdgeRouting.ObstacleAvoidingOrthogonal) continue;
            Check(edge.SourceNodeId, edge.SourcePort, edge.SourcePortId, (edge.LayoutInference & TopologyEdgeLayoutInference.SourcePort) != 0);
            Check(edge.TargetNodeId, edge.TargetPort, edge.TargetPortId, (edge.LayoutInference & TopologyEdgeLayoutInference.TargetPort) != 0);
            void Check(string nodeId, TopologyEdgePort side, string? portId, bool inferred) {
                if (inferred || side == TopologyEdgePort.Auto || !string.IsNullOrWhiteSpace(portId)) return;
                var node = chart.Nodes.Find(candidate => candidate.Id == nodeId);
                if (node != null && TopologyExternalCaption.HasPolicy(node) && TopologyNodeFootprint.CaptionOnSide(chart, node, side))
                    Add(result, "edge-caption-port", "Edge '" + edge.Id + "' cannot use an explicit " + side + " port through node '" + node.Id + "'s visible external caption. Use another side or hide the label.", edge.Id);
            }
        }
    }
}
