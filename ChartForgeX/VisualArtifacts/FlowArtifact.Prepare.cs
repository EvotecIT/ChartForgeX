using System;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class FlowArtifact : IVisualRenderable {
    /// <summary>Prepares a flow through the shared topology compiler while retaining native flow semantics.</summary>
    /// <param name="context">Common size, frame, theme and typography.</param>
    /// <returns>A detached static scene and resolved native flow geometry.</returns>
    public PreparedVisual Prepare(VisualRenderContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        var topology = this.ToTopologyChart();
        for (var i = 0; i < Steps.Count; i++) {
            var step = Steps[i]; var node = topology.Nodes[i];
            node.DisplayMode = TopologyNodeDisplayMode.Card;
            node.PreserveDisplayModeSize = true;
            node.ShowStatusBadge = step.Status != VisualBlocks.VisualStatus.None;
            node.Shape = step.Kind switch {
                FlowArtifactStepKind.Decision => TopologyNodeShape.Diamond,
                FlowArtifactStepKind.Start or FlowArtifactStepKind.End => TopologyNodeShape.Stadium,
                FlowArtifactStepKind.Data => TopologyNodeShape.Cylinder,
                FlowArtifactStepKind.Input => TopologyNodeShape.Parallelogram,
                FlowArtifactStepKind.Output => TopologyNodeShape.ParallelogramAlt,
                _ => TopologyNodeShape.Rounded
            };
        }
        return new VisualTopologyCompiler(topology, context, new TopologyRenderOptions { IncludeLegend = false, WrapNodeLabels = true, FitContentToViewport = true }).Compile(this);
    }
}
