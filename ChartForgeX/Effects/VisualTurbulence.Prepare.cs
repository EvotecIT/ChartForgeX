using System;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Effects;

public sealed partial class VisualTurbulence {
    /// <summary>Captures this immutable procedural source in the caller's existing shared native frame.</summary>
    /// <param name="context">The resolved viewport, insets, headings, theme and fonts; no token or host service is retained.</param>
    /// <returns>A detached native source surface, rendered by the same scalar plan in PNG/Baked and finite NativeFilters SVG.</returns>
    /// <remarks>Frame decoration uses the existing frame owner. Portable source parameters use the common typed
    /// TurbulenceSource policy; reconstruction requires an explicit same-size context, not a saved scene command list.</remarks>
    public PreparedVisual Prepare(VisualRenderContext context) => PrepareSource(context, null);

    internal PreparedVisual PrepareSource(VisualRenderContext context, VisualArtifactInterchangeEnvelope? evidence) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font, context.NativeTextCapture);
        var bounds = VisualFrameLayout.Build(builder, context, Array.Empty<VisualLegendEntry>());
        builder.AddRegion(new VisualSemanticRegion("turbulence", "procedural-source", bounds, "Procedural turbulence"));
        if (bounds.Width > 0 && bounds.Height > 0) builder.ProceduralSource(this, bounds);
        var semantics = VisualArtifactInterchangeMapping.FromPreparedTurbulence(this, context, evidence);
        return new PreparedVisual(builder.Build(), VisualArtifactInterchangeMapping.TurbulenceAccessibility(evidence), semantics)
            .CaptureTurbulenceSource().WithCapturedText(context.NativeTextCapture);
    }
}
