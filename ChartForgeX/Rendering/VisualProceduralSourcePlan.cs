using System;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

/// <summary>Adapts an actual native surface to the same source-free procedural plan; no second sampler or painter.</summary>
internal sealed class VisualProceduralSourcePlan {
    internal VisualProceduralSourcePlan(VisualSceneProceduralSource source, int density)
        : this(source, source.Effect, source.Bounds, density) { }
    internal VisualProceduralSourcePlan(VisualSceneProceduralSource source, VisualTurbulence effect, ChartRect bounds, int density) {
        Source = source;
        var covered = RasterEffectGrid.Cover(bounds, density);
        SamplingBounds = new ChartRect((double)covered.X / density, (double)covered.Y / density,
            (double)covered.Width / density, (double)covered.Height / density);
        var local = new RasterEffectRegion(0, 0, covered.Width, covered.Height);
        var parameters = new RasterTurbulenceParameters(effect, bounds, local, density, default,
            new ChartPoint(SamplingBounds.X, SamplingBounds.Y));
        Plan = RasterEffectPlan.CreateTurbulence(parameters);
    }
    internal VisualSceneProceduralSource Source { get; }
    internal ChartRect SamplingBounds { get; }
    internal RasterEffectPlan Plan { get; }
    internal RgbaImage Capture(CancellationToken cancellation) =>
        RasterEffects.Generate(RasterEffectPreflight.PrepareSourceFree(Plan), 0, cancellation).Image;
}
