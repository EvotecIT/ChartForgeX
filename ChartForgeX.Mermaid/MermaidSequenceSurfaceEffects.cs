using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Projects validated finite source appearance into reusable native surface roles once.</summary>
internal static class MermaidSequenceSurfaceEffects {
    internal static void Apply(SequenceArtifact sequence, MermaidSourceConfiguration configuration,
        MermaidThemePreset? preset, MermaidPresentationOptions? host) {
        var neo = configuration.Look == null || configuration.Look == "neo";
        if (neo) {
            var name = preset?.Name ?? configuration.Theme ??
                (sequence.ThemeMode == ChartForgeX.Themes.VisualThemeMode.Dark ? "redux-dark-color" : "redux-color");
            var tint = name is "redux" or "redux-color"
                ? ChartColor.FromRgb(0, 0, 0) : ChartColor.FromRgb(255, 255, 255);
            // The pinned renderer inserts this filter independently of stylesheet dropShadow.
            var glyph = new VisualDropShadow(tint, 4, 4, 0, opacity: .06,
                relativeRegion: new ChartRect(-.1, -.1, 1.3, 1.3));
            foreach (var participant in sequence.Participants) {
                if (participant.Kind != SequenceArtifactParticipantKind.Actor) participant.SurfaceShadow = glyph;
            }
        }
        var style = configuration.Preset?.ResolveStyleShadow(preset?.Name, host?.Theme != null);
        if (neo) {
            sequence.Paint.NoteShadow = style;
            sequence.Paint.FragmentTabShadow = style;
        }
        // The maintained stylesheet applies rect backgrounds in both looks. It does not
        // apply its unmatched outer-path selector to the ordinary participant rectangle.
        sequence.Paint.BackgroundShadow = style;
    }
}
