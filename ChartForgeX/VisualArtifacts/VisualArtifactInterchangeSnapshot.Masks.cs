namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeSnapshot {
    // Static source preparation keeps its existing semantic budgets; carried pixel snapshots
    // still use their own finite allocation boundary before an array is copied.
    private static void ValidateCapturedEffectSnapshots(VisualArtifactInterchangeEnvelope source) {
        if (source.Presentation?.CapturedEffects.Count > 0)
            VisualArtifactInterchangeValidation.CapturedEffects(source.Presentation, source);
    }

    private static VisualArtifactInterchangeMask Copy(VisualArtifactInterchangeMask source) => new() {
        ImageWidth = source.ImageWidth,
        ImageHeight = source.ImageHeight,
        OriginX = source.OriginX,
        OriginY = source.OriginY,
        PixelsPerUnit = source.PixelsPerUnit,
        Mode = source.Mode,
        OrderedDither = source.OrderedDither,
        Pixels = (byte[])source.Pixels.Clone()
    };
}
