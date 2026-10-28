using System;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

/// <summary>One immutable procedural surface; no raster source, provider or duplicate semantic child.</summary>
internal sealed class VisualSceneProceduralSource : VisualSceneNode {
    internal VisualSceneProceduralSource(VisualTurbulence effect, ChartRect bounds)
        : base("procedural-source", "turbulence") {
        Effect = effect ?? throw new ArgumentNullException(nameof(effect));
        if (bounds.Width <= 0 || bounds.Height <= 0) throw new ArgumentOutOfRangeException(nameof(bounds));
        Bounds = bounds;
        // Exact finite source/coordinate admission without allocating any pixel/lattice arrays.
        _ = new VisualProceduralSourcePlan(this, effect, bounds, 1);
    }
    internal VisualTurbulence Effect { get; }
    internal ChartRect Bounds { get; }
}

internal sealed partial class VisualSceneBuilder {
    internal void ProceduralSource(VisualTurbulence effect, ChartRect bounds) =>
        _nodes.Add(new VisualSceneProceduralSource(effect, bounds));
}
