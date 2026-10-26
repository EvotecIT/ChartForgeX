using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    /// <summary>A procedural source is not a diagram payload or an arbitrary effect graph.</summary>
    private static void TurbulenceSource(VisualArtifactInterchangePresentation presentation, VisualArtifactInterchangeEnvelope envelope) {
        if (presentation.TurbulenceSource == null) return;
        if (envelope.Kind != VisualArtifactKind.Unknown || envelope.Family != VisualArtifactInterchangeFamily.None ||
            envelope.Topology != null || envelope.Flow != null || envelope.Sequence != null || envelope.Railroad != null ||
            envelope.TreeView != null || envelope.Waterfall != null || envelope.Journey != null || envelope.Timeline != null ||
            envelope.Groups.Count != 0 || envelope.Nodes.Count != 0 || envelope.Edges.Count != 0 ||
            envelope.Annotations.Count != 0 || envelope.Scenarios.Count != 0 || presentation.MapViewport != null ||
            presentation.Legend != null || presentation.TopologyCaptions != null)
            throw new ArgumentException("TurbulenceSource requires a pure Unknown/None root; diagram data and unsupported presentation cannot be discarded.", nameof(envelope));
        if (!envelope.Width.HasValue || !envelope.Height.HasValue)
            throw new ArgumentException("TurbulenceSource requires a complete positive source viewport.", nameof(envelope));
        // The immutable constructor already owns finite frequency/seed, known enums and nonnegative authored octaves.
        // Frame allocation is measured by the caller's actual context during preparation, not guessed from old evidence.
        _ = new VisualSize(envelope.Width.Value, envelope.Height.Value);
    }
}
