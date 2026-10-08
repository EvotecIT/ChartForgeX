using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart : IVisualRenderable {
    /// <summary>Compiles a detached topology layout into the common static scene and native semantic snapshot.</summary>
    public PreparedVisual Prepare(VisualRenderContext context) => Prepare(context, null);

    /// <summary>Compiles topology-specific layout, routing and presentation within the common frame and theme.</summary>
    /// <param name="context">Common logical size, frame, typography and theme.</param>
    /// <param name="options">Optional topology configuration; otherwise the chart's configured options apply.</param>
    /// <returns>Detached static pixels, geometry and typed semantic handoff from one resolved diagram.</returns>
    public PreparedVisual Prepare(VisualRenderContext context, TopologyRenderOptions? options) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        return new VisualTopologyCompiler(this, context, ResolveRenderOptions(options)).Compile();
    }
}
