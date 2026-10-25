using System;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal enum RasterPixelOperation { None, Mask, Blend, FloodBlend, PorterDuff, FloodPorterDuff }

internal sealed partial class RasterEffectPlan {
    internal RasterPixelOperation PixelOperation { get; }
    internal RgbaImage? Operand { get; }
    internal int OperandX { get; }
    internal int OperandY { get; }
    internal VisualMaskMode MaskMode { get; }
    internal int QuantizeOriginX { get; }
    internal int QuantizeOriginY { get; }
    internal VisualBlendMode BlendMode { get; }
    internal ChartColor Flood { get; }
    internal VisualCompositeOperator CompositeOperator { get; }
    private RasterEffectPlan(RasterEffectRegion source, RasterPixelOperation operation, RgbaImage? operand, int x, int y,
        VisualMaskMode maskMode, VisualBlendMode blendMode, ChartColor flood, int phaseX = 0, int phaseY = 0,
        VisualCompositeOperator compositeOperator = VisualCompositeOperator.SourceOver) {
        _ = new RasterEffectRegion(source.X, source.Y, source.Width, source.Height);
        SourceRegion = OutputRegion = ResultRegion = MorphologyRegion = source;
        PixelOperation = operation; Operand = operand; OperandX = x; OperandY = y; CompositeOperator = compositeOperator;
        MaskMode = maskMode; BlendMode = blendMode; Flood = flood; QuantizeOriginX = phaseX; QuantizeOriginY = phaseY;
        HorizontalRadii = Array.AsReadOnly(Array.Empty<int>()); VerticalRadii = Array.AsReadOnly(Array.Empty<int>());
        WorkSamples = checked(Pixels * 64); // Zeroed result, both reads, channel arithmetic, final writes; no timing claim.
    }
    internal static RasterEffectPlan Mask(RasterEffectRegion source, RgbaImage mask, int x, int y, VisualMaskMode mode, int phaseX = 0, int phaseY = 0) {
        ValidateMaskMode(mode);
        return new(source, RasterPixelOperation.Mask, mask, x, y, mode, VisualBlendMode.Normal, default, phaseX, phaseY);
    }
    internal static RasterEffectPlan Composite(RasterEffectRegion backdrop, RgbaImage source, int x, int y, VisualBlendMode mode) {
        ValidateBlendMode(mode);
        return new(backdrop, RasterPixelOperation.Blend, source, x, y, default, mode, default);
    }
    internal static RasterEffectPlan FloodComposite(RasterEffectRegion source, ChartColor backdrop, VisualBlendMode mode) {
        ValidateBlendMode(mode);
        return new(source, RasterPixelOperation.FloodBlend, null, 0, 0, default, mode, backdrop);
    }
    internal static RasterEffectPlan Composite(RasterEffectRegion backdrop, RgbaImage source, int x, int y,
        VisualCompositeOperator operation) {
        ValidateCompositeOperator(operation);
        return new(backdrop, RasterPixelOperation.PorterDuff, source, x, y, default, default, default,
            compositeOperator: operation);
    }
    internal static RasterEffectPlan FloodComposite(RasterEffectRegion source, ChartColor backdrop,
        VisualCompositeOperator operation) {
        ValidateCompositeOperator(operation);
        return new(source, RasterPixelOperation.FloodPorterDuff, null, 0, 0, default, default, backdrop,
            compositeOperator: operation);
    }
    internal static void ValidateCompositeOperator(VisualCompositeOperator operation) {
        if (operation < VisualCompositeOperator.SourceOver || operation > VisualCompositeOperator.Clear)
            throw new ArgumentOutOfRangeException(nameof(operation));
    }
    internal static void ValidateMaskMode(VisualMaskMode mode) {
        if (mode != VisualMaskMode.Alpha && mode != VisualMaskMode.Luminance) throw new ArgumentOutOfRangeException(nameof(mode));
    }
    internal static void ValidateBlendMode(VisualBlendMode mode) {
        if (mode < VisualBlendMode.Normal || mode > VisualBlendMode.Luminosity)
            throw new ArgumentOutOfRangeException(nameof(mode));
    }
}
