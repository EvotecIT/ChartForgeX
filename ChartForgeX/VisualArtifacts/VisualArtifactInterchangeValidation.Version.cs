namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    // Keep additive version-2 payloads in one predicate so other typed native data can compose here.
    internal static int RequiredVersion(VisualArtifactInterchangeEnvelope envelope) =>
        RequiresVersion2Data(envelope) || (envelope.Topology?.Hierarchy != null && RequiresVersion2Data(envelope.Topology.Hierarchy.Source)) ? 2 : 1;

    private static bool RequiresVersion2Data(VisualArtifactInterchangeEnvelope envelope) {
        if (envelope.Presentation?.CapturedEffects.Count > 0 || envelope.Presentation?.Frame != null || envelope.Presentation?.ContextFont != null || envelope.Presentation?.TurbulenceSource != null) return true;
        if(envelope.Flow?.ViewportPadding!=null||envelope.Flow?.ExportFormats!=null)return true;
        foreach(var node in envelope.Nodes)if(node?.Flow?.AuthoredWidth!=null||node?.Flow?.AuthoredHeight!=null)return true;
        foreach (var note in envelope.Annotations) if (note?.TopologyDefaultUnits != null || note?.TopologyRoutingViewport != null) return true;
        if (envelope.Sequence is { } surface &&
            (surface.NoteShadow != null || surface.FragmentTabShadow != null || surface.BackgroundShadow != null)) return true;
        foreach (var node in envelope.Nodes) if (node?.Sequence?.SurfaceShadow != null) return true;
        if(envelope.Extensions.ContainsKey("chartforgex.sequence.paint.fragmentTitleText"))return true;
        foreach(var node in envelope.Nodes)if(node?.Sequence is {} ink&&(ink.Border!=null||ink.ActivationFill!=null||ink.ActivationBorder!=null))return true;
        if(envelope.Sequence?.LifelineStroke!=null)return true;
        foreach(var node in envelope.Nodes)if(node?.Sequence?.NavigationLinks.Count>0)return true;
        if (envelope.Journey != null || envelope.Timeline != null || envelope.Presentation?.TopologyCaptions != null) return true;
        if (envelope.Family == VisualArtifactInterchangeFamily.Sequence &&
            (envelope.Extensions.ContainsKey("chartforgex.sequence.paint.numberText") || envelope.Extensions.ContainsKey("chartforgex.sequence.paint.numberFill"))) return true;
        if (envelope.Sequence?.Layout?.HasOverrides == true || envelope.Sequence?.Typography?.HasOverrides == true || envelope.Sequence?.Theme != null || envelope.Sequence?.ThemeMode != null) return true;
        if (envelope.Presentation?.Theme?.Definition != null || envelope.Presentation?.Theme?.Mode != null) return true;
        if (envelope.Presentation?.Theme?.TitleSize != null || envelope.Presentation?.Theme?.DataLabelSize != null
            || envelope.Presentation?.Theme?.SubtitleSize != null || envelope.Presentation?.Theme?.LegendSize != null) return true;
        foreach (var node in envelope.Nodes) if (node?.Topology?.PreparedNodeBasis != null || node?.Topology?.SurfaceElementAppearance != null) return true;
        if (envelope.Topology?.PathDecorations.Count > 0 || envelope.Topology?.ViewportPadding != null || envelope.Topology?.ContentClip != null || envelope.Topology?.CrossingPaint != null) return true;
        foreach(var group in envelope.Groups)if(group?.Topology is {} gp&&(gp.StrokeWidth.HasValue||gp.Opacity.HasValue||gp.SurfaceOpacity.HasValue||gp.DashPattern.Count>0))return true;
        foreach(var node in envelope.Nodes)if(node?.Topology is {} np&&(np.StrokeWidth.HasValue||np.Opacity.HasValue||np.SurfaceOpacity.HasValue||np.DashPattern.Count>0))return true;
        foreach (var node in envelope.Nodes) if (node?.Topology is {} external && (external.BorderColor != null || external.CaptionPlacement.HasValue
            || external.CaptionSurface.HasValue || external.CaptionGap.HasValue || external.GlyphScale.HasValue || external.CornerRadius.HasValue || external.CaptionTextStyle != null)) return true;
        foreach (var group in envelope.Groups) if (group?.Topology?.LabelContent != null) return true;
        foreach (var edge in envelope.Edges) if (edge?.Topology?.LabelContent != null || edge?.Topology?.LabelBackgroundColor != null || edge?.Topology?.PreparedEdgePlan != null) return true;
        foreach(var node in envelope.Nodes)if(node?.Sequence is {} participant&&(participant.Stereotype!=null||participant.Symbol!=null||participant.Fill!=null||participant.MinimumWidth!=null))return true;
        foreach(var item in envelope.Annotations)if(item?.Sequence?.BlockKind is SequenceArtifactBlockKind.Reference or SequenceArtifactBlockKind.Divider)return true;
        foreach (var node in envelope.Nodes) {
            if (node?.Topology?.Shape is ChartForgeX.Topology.TopologyNodeShape shape && (int)shape >= 27) return true;
            if (node?.Topology?.SubtitleTextStyle != null || node?.Topology?.TableContent != null || node?.Topology?.IconShape != null
                || node?.Topology?.Shape is ChartForgeX.Topology.TopologyNodeShape.ActorHollow or ChartForgeX.Topology.TopologyNodeShape.ActorBusiness
                    or ChartForgeX.Topology.TopologyNodeShape.ActorBusinessHollow or ChartForgeX.Topology.TopologyNodeShape.ActorSilhouette
                    or ChartForgeX.Topology.TopologyNodeShape.ActorIcon or ChartForgeX.Topology.TopologyNodeShape.BusinessEllipse) return true;
            if (node != null) foreach (var detail in node.Details) if (detail?.BadgeStyle != null) return true;
        }
        foreach (var edge in envelope.Edges) if (edge?.Topology is { } topology && (topology.Interpolation != null || topology.InterpolationCornerBlendLength != null || topology.PathGeometry != null || topology.LabelPosition != null || topology.SourceMarkerStyle != null || topology.TargetMarkerStyle != null)) return true;
        return false;
    }
}
