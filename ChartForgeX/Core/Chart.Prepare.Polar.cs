using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    internal ChartPolarPreparation? PolarPreparation { get; private set; }
    /// <summary>Prepares a detached continuous Polar view using directional sector clipping and independent radius-domain zoom.</summary>
    /// <param name="context">The logical size, theme, frame and typography.</param>
    /// <param name="viewport">The angle/radius request, or null to restore the full view.</param>
    /// <remarks>All authored observations, raw radians, source IDs and order remain captured. Marks outside requested bounds are hidden
    /// and cannot become HTML targets. A radius window uniformly magnifies captured full-domain line/area geometry and crops it with
    /// an annular paint clip, preserving original cubic curves, tangents and fill. Its nearer projected boundary is an inner aperture, not the center.</remarks>
    /// <returns>A detached scene with its explicit PolarProjection when layout is usable.</returns>
    public PreparedVisual PreparePolar(VisualRenderContext context, ChartPolarViewport? viewport = null) =>
        PrepareCore(context, null, polarViewport: viewport, requirePolar: true);
}
