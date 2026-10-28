using System;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Reuses the resolved topology observation without running flow layout a second time.</summary>
    internal static VisualArtifactInterchangeEnvelope FromPreparedFlow(FlowArtifact flow, VisualArtifactInterchangeEnvelope envelope) {
        if (flow.Lanes.Count != envelope.Groups.Count || flow.Steps.Count != envelope.Nodes.Count || flow.Connectors.Count != envelope.Edges.Count)
            throw new InvalidOperationException("Prepared flow geometry must retain each source lane, step and connector.");
        envelope.Kind = VisualArtifactKind.Flow; envelope.Family = VisualArtifactInterchangeFamily.Flow;
        var padding = envelope.Topology?.ViewportPadding;
        envelope.Topology = null;
        // Flow retains its own typed source contract, rather than a topology-only caption restoration policy.
        if (envelope.Presentation != null) {
            envelope.Presentation.TopologyCaptions = null;
        }
        envelope.Flow = new VisualArtifactInterchangeFlowArtifact { LayoutMode = flow.LayoutMode, LayoutDirection = flow.Direction,
            ViewportPadding = padding, ExportFormats = flow.ExportFormats == FlowArtifact.DefaultExportFormats ? null : flow.ExportFormats };
        Copy(flow.Metadata, envelope.Extensions);
        for (var i = 0; i < flow.Lanes.Count; i++) {
            var source = flow.Lanes[i]; var group = envelope.Groups[i];
            group.Role = VisualArtifactInterchangeGroupRole.FlowLane; group.Kind = "FlowLane"; group.Topology = null;
            group.Label = source.Label; group.Status = source.Status.ToString(); group.Color = source.Color;
            Copy(source.Metadata, group.Extensions);
        }
        for (var i = 0; i < flow.Steps.Count; i++) {
            var source = flow.Steps[i]; var node = envelope.Nodes[i];
            node.Role = VisualArtifactInterchangeNodeRole.FlowStep; node.Kind = source.Kind.ToString(); node.Topology = null;
            node.Flow = new VisualArtifactInterchangeFlowNode { Kind = source.Kind, FitLabelToContent = source.FitLabelToContent,
                AuthoredWidth = source.Width == node.Width ? null : source.Width, AuthoredHeight = source.Height == node.Height ? null : source.Height };
            node.Label = source.Label; node.Subtitle = source.Subtitle; node.Status = source.Status.ToString();
            Copy(source.Metadata, node.Extensions);
        }
        for (var i = 0; i < flow.Connectors.Count; i++) {
            var source = flow.Connectors[i]; var edge = envelope.Edges[i];
            edge.Role = VisualArtifactInterchangeEdgeRole.FlowConnector; edge.Kind = source.Kind.ToString(); edge.Topology = null;
            edge.Flow = new VisualArtifactInterchangeFlowEdge { Kind = source.Kind, Direction = source.Direction };
            edge.Label = source.Label; edge.Status = source.Status.ToString(); edge.Color = source.Color;
            // These empty placeholders belong to the generated topology, not the ordered Flow source model.
            // Restoration still rejects an envelope that actually authors unsupported secondary/tertiary captions.
            edge.SecondaryLabel = null; edge.TertiaryLabel = null;
            Copy(source.Metadata, edge.Extensions);
        }
        // Native compilation has already validated the graph and Flow source policies.
        // Font bytes, JSON value counts and general text budgets belong to actual portable access.
        return envelope;
    }
}
