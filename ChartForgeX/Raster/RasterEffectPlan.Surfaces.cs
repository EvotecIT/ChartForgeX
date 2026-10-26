using System;
using ChartForgeX.Effects;

namespace ChartForgeX.Raster;

internal enum RasterSurfaceOperation { None, Capture, Convert, Alpha, Offset, Flood, Color, Blend, Composite, Arithmetic, Morphology, Blur }

internal sealed partial class RasterEffectPlan {
    internal RasterSurfaceOperation SurfaceOperation { get; private set; }
    internal RasterEffectPixel16 SurfaceFloodColor { get; private set; }
    internal bool SurfaceLinearRgb { get; private set; }
    internal RasterEffectSurface? SurfaceOperand { get; private set; }
    internal RasterColorTransform? SurfaceColorTransform { get; private set; }
    internal int SurfaceRadiusX { get; private set; }
    internal int SurfaceRadiusY { get; private set; }
    internal bool SurfaceMaximize { get; private set; }
    internal double SurfaceK1 { get; private set; }
    internal double SurfaceK2 { get; private set; }
    internal double SurfaceK3 { get; private set; }
    internal double SurfaceK4 { get; private set; }
    internal long SurfaceAllocatedBytes => SurfaceOperation == RasterSurfaceOperation.Blur
        ? checked(Pixels * 16 + (long)SourceRegion.Width * SourceRegion.Height * 8)
        : SurfaceOperation == RasterSurfaceOperation.Morphology
            ? checked(Pixels * 24 + (long)Math.Max(SourceRegion.Width, SourceRegion.Height) * 16)
            : checked(Pixels * 8);

    private RasterEffectPlan(RasterEffectRegion region, RasterSurfaceOperation operation, bool linearRgb) {
        SourceRegion = OutputRegion = ResultRegion = MorphologyRegion = region;
        SurfaceOperation = operation; SurfaceLinearRgb = linearRgb;
        HorizontalRadii = VerticalRadii = Array.AsReadOnly(Array.Empty<int>());
        WorkSamples = checked(Pixels * (operation == RasterSurfaceOperation.Color ? 160L : 96L));
    }
    internal static RasterEffectPlan SurfaceCapture(RasterEffectRegion region) => new(region, RasterSurfaceOperation.Capture, false);
    internal static RasterEffectPlan SurfaceConvert(RasterEffectRegion region, bool linearRgb) => new(region, RasterSurfaceOperation.Convert, linearRgb);
    internal static RasterEffectPlan SurfaceAlpha(RasterEffectRegion region, bool linearRgb) => new(region, RasterSurfaceOperation.Alpha, linearRgb);
    internal static RasterEffectPlan SurfaceOffset(RasterEffectRegion region, int x, int y, bool linearRgb) =>
        new(region, RasterSurfaceOperation.Offset, linearRgb) { OffsetX = x, OffsetY = y };
    internal static RasterEffectPlan SurfaceFlood(RasterEffectRegion region, RasterEffectPixel16 color, bool linearRgb) =>
        new(region, RasterSurfaceOperation.Flood, linearRgb) { SurfaceFloodColor = color };
    internal static RasterEffectPlan SurfaceColor(RasterEffectRegion region, RasterColorTransform transform) =>
        new(region, RasterSurfaceOperation.Color, transform.LinearRgb) {
            SurfaceColorTransform = transform, WorkSamples = checked((long)region.Width * region.Height * 160 * transform.StageCount)
        };
    internal static RasterEffectPlan SurfaceBlend(RasterEffectRegion region, RasterEffectSurface source, VisualBlendMode mode, bool linearRgb) {
        ValidateBlendMode(mode);
        return new(region, RasterSurfaceOperation.Blend, linearRgb) { SurfaceOperand = source, BlendMode = mode };
    }
    internal static RasterEffectPlan SurfaceComposite(RasterEffectRegion region, RasterEffectSurface source, VisualCompositeOperator operation, bool linearRgb) {
        ValidateCompositeOperator(operation);
        return new(region, RasterSurfaceOperation.Composite, linearRgb) { SurfaceOperand = source, CompositeOperator = operation };
    }
    internal static RasterEffectPlan SurfaceArithmetic(RasterEffectRegion region, RasterEffectSurface source,
        double k1, double k2, double k3, double k4, bool linearRgb) {
        if (!FiniteSurface(k1) || !FiniteSurface(k2) || !FiniteSurface(k3) || !FiniteSurface(k4))
            throw new ArgumentOutOfRangeException(nameof(k1));
        return new(region, RasterSurfaceOperation.Arithmetic, linearRgb) {
            SurfaceOperand = source, SurfaceK1 = k1, SurfaceK2 = k2, SurfaceK3 = k3, SurfaceK4 = k4
        };
    }
    internal static RasterEffectPlan SurfaceMorphology(RasterEffectRegion region, int x, int y, bool maximize, bool linearRgb) {
        if (x < 0 || y < 0) throw new ArgumentOutOfRangeException(nameof(x));
        return new(region, RasterSurfaceOperation.Morphology, linearRgb) {
            SurfaceRadiusX = x, SurfaceRadiusY = y, SurfaceMaximize = maximize,
            WorkSamples = checked((long)region.Width * region.Height * 256 + (long)Math.Max(region.Width, region.Height) * 16)
        };
    }
    internal static RasterEffectPlan SurfaceBlur(RasterEffectRegion region, int sigmaXQ8, int sigmaYQ8, bool linearRgb) {
        var plan = Blur(region, sigmaXQ8, sigmaYQ8);
        plan.SurfaceOperation = RasterSurfaceOperation.Blur; plan.SurfaceLinearRgb = linearRgb;
        plan.WorkSamples = checked(plan.WorkSamples + plan.Pixels * 8 + (long)region.Width * region.Height * 8);
        return plan;
    }
    private static bool FiniteSurface(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
