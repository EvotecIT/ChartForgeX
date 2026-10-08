using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class SequenceArtifact : IVisualRenderable {
    /// <summary>Prepares measured participants, messages, activations, notes and interaction fragments into a detached native scene.</summary>
    /// <remarks>Uses the common frame, theme and fixed logical viewport. Content that cannot fit must be given a larger viewport. Both SVG and raster export consume the same prepared geometry and text.</remarks>
    public PreparedVisual Prepare(VisualRenderContext context) => SequencePreparedCompiler.Prepare(this, context);
}
