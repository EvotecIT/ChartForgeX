using ChartForgeX.Rendering;
using System.Collections.Generic;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    private static VisualArtifactInterchangeEnvelope FromMutablePreparedArtifact(VisualArtifact artifact, PreparedVisual prepared,
        IEnumerable<KeyValuePair<string, string>>? modelMetadata = null) {
        var envelope = prepared.SemanticInterchange!;
        var common = Common(artifact, out var reservedKeys);
        // Mutable diagram envelopes project the current model identity and headings. Host metadata
        // stays independent; a prepared artifact instead retains its immutable captured identity.
        envelope.Kind = common.Kind; envelope.SourceLanguage = common.SourceLanguage;
        envelope.Width = prepared.Size.Width; envelope.Height = prepared.Size.Height;
        if (!artifact.HasModelAccessibilitySnapshot || artifact.Accessibility.Name != artifact.ModelAccessibilitySnapshot.Name)
            envelope.AccessibleName = common.AccessibleName ?? envelope.AccessibleName;
        if (!artifact.HasModelAccessibilitySnapshot || artifact.Accessibility.Description != artifact.ModelAccessibilitySnapshot.Description)
            envelope.AccessibleDescription = common.AccessibleDescription ?? envelope.AccessibleDescription;
        if (!artifact.HasModelAccessibilitySnapshot || artifact.Accessibility.Language != artifact.ModelAccessibilitySnapshot.Language)
            envelope.Language = common.Language ?? envelope.Language;
        if (!artifact.HasModelAccessibilitySnapshot || artifact.Accessibility.IsDecorative != artifact.ModelAccessibilitySnapshot.IsDecorative)
            envelope.IsDecorative = common.IsDecorative;
        if (modelMetadata != null) envelope.Extensions.Clear();
        foreach (var entry in common.Extensions) envelope.Extensions[entry.Key] = entry.Value;
        if (modelMetadata != null) CopyMissing(modelMetadata, envelope.Extensions, reservedKeys);
        envelope.Validate();
        return envelope;
    }
}
