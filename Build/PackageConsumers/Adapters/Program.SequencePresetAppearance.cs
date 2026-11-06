using System;
using System.IO;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
internal static partial class Program {
    private static void SequencePresetAppearanceSourceContracts() {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("PackageProbe.sequence-preset-appearance.mmd")
            ?? throw new InvalidOperationException("Missing palette fixture.");
        using var reader = new StreamReader(stream);
        var source = reader.ReadToEnd();
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var options = new MermaidRenderOptions { Presentation = new MermaidPresentationOptions { ThemeMode = mode } };
            var result = RenderFixture(source, options);
            if (result.HasErrors || result.Artifact?.Model is not SequenceArtifact model || model.Layout?.ParticipantBands == null)
                throw new InvalidOperationException("Omitted Sequence preset/band projection failed.");
            if (model.Participants[0].Fill != null || model.Paint.ParticipantFill != null
                // Diagrams sit on the Graphite card surface; node surfaces are raised slightly from it.
                || !model.Theme.Resolve(mode).Background.Equals(VisualTheme.Graphite().Resolve(mode).ElevatedSurface)
                || result.Artifact.ToPng().Length == 0)
                throw new InvalidOperationException("Unthemed Sequence did not inherit Graphite in the effective host mode.");
            // Mermaid's default theme renders Graphite; an opt-in preset such as forest keeps its own palette.
            var standard = RenderFixture("%%{init:{\"theme\":\"default\"}}%%\n" + source, options);
            if (standard.HasErrors || standard.Artifact?.Model is not SequenceArtifact standardModel
                || (standardModel.Participants[0].Fill ?? standardModel.Paint.ParticipantFill) != null)
                throw new InvalidOperationException("Explicit default Sequence theme did not render Graphite.");
            var preset = RenderFixture("%%{init:{\"theme\":\"forest\"}}%%\n" + source, options);
            if (preset.HasErrors || preset.Artifact?.Model is not SequenceArtifact presetModel
                || (presetModel.Participants[0].Fill ?? presetModel.Paint.ParticipantFill) == null)
                throw new InvalidOperationException("Explicit forest Sequence preset did not keep its palette.");
            var restored = VisualArtifactInterchangeEnvelope.FromJson(result.Artifact.ToInterchangeJson()).ToSequenceArtifact();
            restored.Messages[0].Text = "Edited";
            if (!restored.ToSvg().Contains("Edited"))
                throw new InvalidOperationException("Source preset facts did not restore to editable native owners.");
        }
    }
}
