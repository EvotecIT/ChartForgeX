using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    // Reuses the existing Cartesian presentation owner as an internal copied carrier only.
    // This does not create a Waterfall artifact or change any public common envelope.
    internal static VisualArtifactInterchangeWaterfallArtifact CaptureVisibilityPresentation(Chart chart) {
        var source = new VisualArtifactInterchangeWaterfallArtifact(); CaptureWaterfallPresentation(chart, source); return source;
    }
    internal static void RestoreVisibilityPresentation(Chart chart, VisualArtifactInterchangeWaterfallArtifact source) =>
        ApplyWaterfallPresentation(chart, source);
}
