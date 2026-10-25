using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactRendering {
    /// <summary>Creates a static artifact with source facts and a distinct captured output frame.</summary>
    /// <param name="layer">The immutable completed native capture.</param>
    /// <param name="id">Optional stable source artifact identity.</param>
    /// <returns>A native paint artifact with a lazy explicit portable boundary.</returns>
    /// <remarks>Native SVG/PNG handoff does not impose wire JSON or snapshot budgets. Explicit interchange
    /// validates the complete source, root operations, terminal frame and post-masks before copying mask pixels.
    /// Source dimensions remain separate from captured output; generic PreparedVisual dimension checks stay strict.</remarks>
    public static VisualArtifact ToArtifact(this CapturedVisualLayer layer, string? id = null) {
        if (layer == null) throw new ArgumentNullException(nameof(layer));
        var sourceArtifact = layer.Source.ToArtifact(id);
        var artifact = layer.Visual.ToArtifact(sourceArtifact.Id, sourceArtifact.Kind);
        artifact.Title = sourceArtifact.Title;
        artifact.Subtitle = sourceArtifact.Subtitle;
        artifact.SourceLanguage = sourceArtifact.SourceLanguage;
        artifact.CapturedLayer = layer;
        if (!layer.Source.HasUnsupportedInterchangePresentation) {
            artifact.PreparedInterchangeSnapshot = new Lazy<string>(() =>
                VisualArtifactInterchangeMapping.CaptureLayerEnvelope(sourceArtifact, layer).ToJson());
            artifact.ExportFormats |= VisualArtifactExportFormat.Json;
        }
        foreach (var pair in sourceArtifact.Metadata) artifact.Metadata.Add(pair.Key, pair.Value);
        return artifact;
    }
}
