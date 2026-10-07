using ChartForgeX.Rendering;
using System.Collections.Generic;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    private static VisualArtifactInterchangeEnvelope FromMutablePreparedArtifact(VisualArtifact artifact, PreparedVisual prepared,
        IEnumerable<KeyValuePair<string, string>>? modelMetadata = null) {
        var envelope = prepared.SemanticInterchange!;
        var common = Common(artifact, out var reservedKeys);
        envelope.Id = common.Id; envelope.Kind = common.Kind; envelope.SourceLanguage = common.SourceLanguage;
        if (common.Title.Length > 0) envelope.Title = common.Title;
        if (common.Subtitle.Length > 0) envelope.Subtitle = common.Subtitle;
        envelope.Width = prepared.Size.Width; envelope.Height = prepared.Size.Height;
        envelope.AccessibleName = common.AccessibleName ?? envelope.AccessibleName;
        envelope.AccessibleDescription = common.AccessibleDescription ?? envelope.AccessibleDescription;
        envelope.Language = common.Language ?? envelope.Language; envelope.IsDecorative = common.IsDecorative;
        if (modelMetadata != null) envelope.Extensions.Clear();
        foreach (var entry in common.Extensions) envelope.Extensions[entry.Key] = entry.Value;
        if (modelMetadata != null) CopyMissing(modelMetadata, envelope.Extensions, reservedKeys);
        envelope.Validate();
        return envelope;
    }
}
