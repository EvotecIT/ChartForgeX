using System;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal static partial class SequencePreparedCompiler {
    // Shared native glyph extrema are also used by participant-band measurement. Captions
    // and semantic participant groups stay outside this numeric surface-only scope.
    private static IDisposable? ParticipantSurface(VisualSceneBuilder builder,
        SequenceArtifactParticipant participant, ChartRect bounds, double stroke) {
        if (participant.SurfaceShadow == null) return null;
        var surface = bounds;
        if (participant.Kind != SequenceArtifactParticipantKind.Participant) {
            var glyph = SequencePreparedLayout.ParticipantGlyphFootprint(participant.Kind);
            surface = new ChartRect(bounds.X + bounds.Width / 2 + glyph.X,
                bounds.Y + 21 + glyph.Y, glyph.Width, glyph.Height);
        }
        return Surface(builder, participant.SurfaceShadow, surface, stroke);
    }

    private static IDisposable? Surface(VisualSceneBuilder builder, VisualDropShadow? shadow,
        ChartRect surface, double stroke) {
        if (shadow == null) return null;
        // Existing scene primitives use centered strokes. The capture owner separately
        // reserves its working-sample antialias fringe, once, after density is selected.
        var half = stroke / 2;
        var ink = new ChartRect(surface.X - half, surface.Y - half,
            surface.Width + stroke, surface.Height + stroke);
        return builder.PushSurfaceShadow(shadow, surface, ink);
    }
}
