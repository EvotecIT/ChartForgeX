namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeSnapshot {
    private static VisualArtifactInterchangeCapturedEffect Copy(VisualArtifactInterchangeCapturedEffect source) => new() {
        // Effect selections/options are already immutable. The mutable DTO container and raw mask arrays are detached.
        Blur = source.Blur,
        DropShadow = source.DropShadow,
        InnerShadow = source.InnerShadow,
        Outline = source.Outline,
        Bloom = source.Bloom,
        Mask = source.Mask == null ? null : Copy(source.Mask),
        CaptureFrame = source.CaptureFrame == null ? null : new VisualArtifactInterchangeCaptureFrame {
            Bounds = source.CaptureFrame.Bounds,
            RasterOptions = source.CaptureFrame.RasterOptions
        }
    };
}
