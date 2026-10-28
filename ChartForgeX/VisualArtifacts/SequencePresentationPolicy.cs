using System.Linq;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Identifies the represented native policy that requires its paired captured theme.</summary>
internal static class SequencePresentationPolicy {
    internal static bool IsCaptured(SequenceArtifact source) =>
        HasPolicy(source.Layout, source.Typography,
            source.Participants.Any(item => item.NavigationLinks.Count > 0),
            source.Paint.LifelineStroke != null,
            source.Paint.NoteShadow != null || source.Paint.FragmentTabShadow != null
                || source.Paint.BackgroundShadow != null || source.Participants.Any(item => item.SurfaceShadow != null));

    internal static bool IsCaptured(VisualArtifactInterchangeEnvelope envelope) =>
        HasPolicy(envelope.Sequence?.Layout, envelope.Sequence?.Typography,
            envelope.Nodes.Any(item => item.Sequence?.NavigationLinks.Count > 0),
            envelope.Sequence?.LifelineStroke != null,
            envelope.Sequence?.NoteShadow != null || envelope.Sequence?.FragmentTabShadow != null
                || envelope.Sequence?.BackgroundShadow != null || envelope.Nodes.Any(item => item.Sequence?.SurfaceShadow != null));

    private static bool HasPolicy(SequenceLayoutPolicy? layout, SequenceTypography? typography,
        bool navigation, bool stroke, bool surface) =>
        layout?.HasOverrides == true || typography?.HasOverrides == true || navigation || stroke || surface;
}
