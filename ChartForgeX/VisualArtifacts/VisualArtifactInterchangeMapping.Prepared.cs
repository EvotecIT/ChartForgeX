using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    private static VisualArtifactInterchangeEnvelope FromPreparedArtifact(VisualArtifact artifact, PreparedVisual prepared) {
        VisualArtifactRendering.PreparedModel(artifact, prepared);
        var common = Common(artifact, out _);
        if (artifact.PreparedInterchangeSnapshot == null) {
            common.Validate();
            return common;
        }
        // A fresh read isolates each host from both the caller's original model and earlier interchange readers.
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.PreparedInterchangeSnapshot);
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
        envelope.Validate();
        return envelope;
    }
}
