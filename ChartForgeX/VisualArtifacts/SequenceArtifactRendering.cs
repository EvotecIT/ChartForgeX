using System;
using System.Globalization;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Exports sequence models through the shared prepared scene and preserves source-only artifact handoff.</summary>
public static class SequenceArtifactRendering {
    /// <summary>Wraps authored sequence semantics without preparing a preview or changing the source model.</summary>
    /// <param name="sequence">The sequence source model.</param>
    /// <param name="sourceLanguage">The source language that produced the model.</param>
    /// <returns>A lazy source envelope; prepare explicitly before ToArtifact when a detached display snapshot is required.</returns>
    public static VisualArtifact ToVisualArtifact(this SequenceArtifact sequence, VisualArtifactSourceLanguage sourceLanguage = VisualArtifactSourceLanguage.Native) {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));
        var artifact = VisualArtifact.Create(sequence.Id, VisualArtifactKind.Sequence, sequence);
        artifact.SourceLanguage = sourceLanguage;
        artifact.Accessibility.Name = sequence.Accessibility.Name;
        artifact.Accessibility.Description = sequence.Accessibility.Description;
        artifact.Accessibility.Language = sequence.Accessibility.Language;
        artifact.Accessibility.IsDecorative = sequence.Accessibility.IsDecorative;
        artifact.Title = sequence.Title;
        artifact.Subtitle = sequence.Subtitle;
        artifact.NaturalSize = CalculateNaturalSize(sequence);
        artifact.ExportFormats = sequence.ExportFormats;
        artifact.Metadata["sequence.participants"] = sequence.Participants.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["sequence.messages"] = sequence.Messages.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["sequence.activations"] = sequence.Activations.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["sequence.notes"] = sequence.Notes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(SequenceArtifact);
        return artifact;
    }

    // Source interchange retains authored bounds. Prepared interchange captures its resolved display bounds separately.
    internal static VisualArtifactSize CalculateNaturalSize(SequenceArtifact sequence) {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));
        return new VisualArtifactSize(sequence.Width, sequence.Height);
    }

    /// <summary>Renders a sequence to shared SVG with measured content expansion beyond its authored minimum size.</summary>
    /// <param name="sequence">The source model; its width, height and padding supply the default request.</param>
    /// <returns>Script-free SVG from a native prepared scene.</returns>
    public static string ToSvg(this SequenceArtifact sequence) => SequencePreparedCompiler.PrepareDefault(sequence).ToSvg();

    /// <summary>Renders the same native measured sequence scene directly to PNG.</summary>
    /// <param name="sequence">The source model; its width, height and padding supply the default request.</param>
    /// <returns>PNG bytes using the shared raster defaults.</returns>
    public static byte[] ToPng(this SequenceArtifact sequence) => SequencePreparedCompiler.PrepareDefault(sequence).ToPng();
}
