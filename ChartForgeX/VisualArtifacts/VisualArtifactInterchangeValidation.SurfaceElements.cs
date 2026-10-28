using System;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateSurfaceElements(VisualArtifactInterchangeNode node) {
        var elements = node.Topology?.SurfaceElementAppearance; if (elements == null) return;
        var native = node.Topology!;
        if (native.Shape != elements.Shape || native.DisplayMode != elements.DisplayMode
            || native.SurfaceOpacity != elements.Opacity || native.TableContent != null)
            throw new ArgumentException("Captured surface elements must agree with the native shape, mode and paint basis.");
    }
}
