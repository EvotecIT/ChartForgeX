using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Projects authored sequence semantics without invoking the legacy preview layout.</summary>
    internal static VisualArtifactInterchangeEnvelope FromPreparedSequence(SequenceArtifact sequence, VisualRenderContext context) {
        var artifact = VisualArtifact.Create(sequence.Id, VisualArtifactKind.Sequence, sequence);
        var envelope = Common(artifact, out var metadataKeys);
        MapSequence(envelope, sequence, metadataKeys, new VisualArtifactSize(context.Layout.Size.Width, context.Layout.Size.Height));
        envelope.Presentation = new VisualArtifactInterchangePresentation {
            Theme = MapTheme(context.Theme.Resolve(context.ThemeMode), context.Font.Family)
        };
        envelope.Validate();
        return envelope;
    }

    private static VisualArtifactInterchangeTheme MapTheme(VisualThemeColors colors, string fontFamily) {
        var status = colors.Status;
        return new VisualArtifactInterchangeTheme {
            Background = colors.Background.ToCss(),
            Foreground = colors.Foreground.ToCss(),
            MutedForeground = colors.MutedForeground.ToCss(),
            Card = colors.ElevatedSurface.ToCss(),
            Surface = colors.Surface.ToCss(),
            Border = colors.Border.ToCss(),
            Accent = colors.Accent.ToCss(),
            Healthy = status.Pass.Fill.ToCss(),
            Warning = status.Medium.Fill.ToCss(),
            Critical = status.Critical.Fill.ToCss(),
            Unknown = status.Neutral.Fill.ToCss(),
            Disabled = status.Maintenance.Fill.ToCss(),
            FontFamily = fontFamily
        };
    }
}
