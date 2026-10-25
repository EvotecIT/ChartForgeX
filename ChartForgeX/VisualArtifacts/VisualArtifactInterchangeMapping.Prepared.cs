using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    internal static void ValidatePreparedJsonExport(VisualArtifact artifact, PreparedVisual prepared) =>
        _ = FromPreparedArtifact(artifact, prepared, cloneMaskPixels: false);

    private static VisualArtifactInterchangeEnvelope FromPreparedArtifact(VisualArtifact artifact, PreparedVisual prepared, bool cloneMaskPixels = true) {
        prepared.RequireSupportedInterchangePresentation();
        VisualArtifactRendering.PreparedModel(artifact, prepared);
        if (artifact.CapturedLayer != null) return FromCapturedLayerArtifact(artifact, prepared, cloneMaskPixels);
        var envelope = FromPreparedSourceFacts(artifact);
        prepared.CapturePortableEffects(envelope, cloneMaskPixels);
        envelope.Validate();
        return envelope;
    }

    private static VisualArtifactInterchangeEnvelope FromPreparedSourceFacts(VisualArtifact artifact) {
        var common = Common(artifact, out _);
        if (artifact.PreparedInterchangeSnapshot == null) return common;
        // A fresh read isolates each host from both the caller's original model and earlier interchange readers.
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.PreparedInterchangeSnapshot.Value);
        envelope.Id = common.Id;
        envelope.Kind = common.Kind;
        envelope.SourceLanguage = common.SourceLanguage;
        envelope.Title = common.Title;
        envelope.Subtitle = common.Subtitle;
        envelope.Width = common.Width;
        envelope.Height = common.Height;
        envelope.AccessibleName = common.AccessibleName;
        envelope.AccessibleDescription = common.AccessibleDescription;
        envelope.Language = common.Language;
        envelope.IsDecorative = common.IsDecorative;
        envelope.Extensions.Clear();
        foreach (var pair in common.Extensions) envelope.Extensions.Add(pair.Key, pair.Value);
        return envelope;
    }
}
