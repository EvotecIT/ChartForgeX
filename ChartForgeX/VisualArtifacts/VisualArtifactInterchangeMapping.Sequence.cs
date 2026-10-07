using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Projects authored sequence semantics without invoking the legacy preview layout.</summary>
    internal static VisualArtifactInterchangeEnvelope FromPreparedSequence(SequenceArtifact sequence, VisualSize size) {
        var artifact = VisualArtifact.Create(sequence.Id, VisualArtifactKind.Sequence, sequence);
        var envelope = Common(artifact, out var metadataKeys);
        MapSequence(envelope, sequence, metadataKeys, new VisualArtifactSize(size.Width, size.Height));
        envelope.Validate();
        return envelope;
    }
}
