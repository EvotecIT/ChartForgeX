using System;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Reuses the resolved topology observation without running flow layout a second time.</summary>
    internal static VisualArtifactInterchangeEnvelope FromPreparedFlow(FlowArtifact flow, VisualArtifactInterchangeEnvelope envelope) {
        if (flow.Lanes.Count != envelope.Groups.Count || flow.Steps.Count != envelope.Nodes.Count || flow.Connectors.Count != envelope.Edges.Count)
            throw new InvalidOperationException("Prepared flow geometry must retain each source lane, step and connector.");
        envelope.Kind = VisualArtifactKind.Flow; envelope.Family = VisualArtifactInterchangeFamily.Flow;
        envelope.Topology = null;
        envelope.Flow = new VisualArtifactInterchangeFlowArtifact { LayoutMode = flow.LayoutMode, LayoutDirection = flow.Direction };
        foreach (var pair in flow.Metadata) envelope.Extensions[pair.Key] = pair.Value;
        for (var i = 0; i < flow.Lanes.Count; i++) {
            var source = flow.Lanes[i]; var group = envelope.Groups[i];
            group.Role = VisualArtifactInterchangeGroupRole.FlowLane; group.Kind = "FlowLane"; group.Topology = null;
            group.Label = source.Label; group.Status = source.Status.ToString(); group.Color = source.Color;
            foreach (var pair in source.Metadata) group.Extensions[pair.Key] = pair.Value;
        }
        for (var i = 0; i < flow.Steps.Count; i++) {
            var source = flow.Steps[i]; var node = envelope.Nodes[i];
            node.Role = VisualArtifactInterchangeNodeRole.FlowStep; node.Kind = source.Kind.ToString(); node.Topology = null;
            node.Flow = new VisualArtifactInterchangeFlowNode { Kind = source.Kind };
            node.Label = source.Label; node.Subtitle = source.Subtitle; node.Status = source.Status.ToString();
            foreach (var pair in source.Metadata) node.Extensions[pair.Key] = pair.Value;
        }
        for (var i = 0; i < flow.Connectors.Count; i++) {
            var source = flow.Connectors[i]; var edge = envelope.Edges[i];
            edge.Role = VisualArtifactInterchangeEdgeRole.FlowConnector; edge.Kind = source.Kind.ToString(); edge.Topology = null;
            edge.Flow = new VisualArtifactInterchangeFlowEdge { Kind = source.Kind, Direction = source.Direction };
            edge.Label = source.Label; edge.Status = source.Status.ToString(); edge.Color = source.Color;
            foreach (var pair in source.Metadata) edge.Extensions[pair.Key] = pair.Value;
        }
        envelope.Validate();
        return envelope;
    }
}
