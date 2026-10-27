using System;
using System.IO;
using System.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void SequenceSurfaceShadowSourceContracts() {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("PackageProbe.sequence-retained-surface-shadows.mmd")
            ?? throw new InvalidOperationException("Missing retained surface source fixture.");
        using var reader = new StreamReader(stream);
        var result = RenderFixture(reader.ReadToEnd());
        if (result.HasErrors || result.Artifact?.Model is not SequenceArtifact model ||
            model.Participants.Any(participant => participant.SurfaceShadow?.Opacity != .06)) {
            throw new InvalidOperationException("Source glyph shadow did not reach retained native owners.");
        }
        // Bare stylesheet URL diagnostics are retained until the independent reference lane
        // establishes their actual target. This consumer never disables genuine source effects.
        foreach (var diagnostic in result.Diagnostics) Console.WriteLine(diagnostic.Code + ": " + diagnostic.Message);
        if (result.Artifact.ToPng().Length == 0 || !result.Artifact.ToSvg().Contains("<text")) {
            throw new InvalidOperationException("Source shadow export lost native pixels or retained text.");
        }
        var restored = VisualArtifactInterchangeEnvelope.FromJson(result.Artifact.ToInterchangeJson()).ToSequenceArtifact();
        if (restored.Participants[0].SurfaceShadow?.Opacity != .06) throw new InvalidOperationException("Portable source shadow was lost.");
    }
}
