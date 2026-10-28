namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeSnapshot {
    private static VisualArtifactInterchangeTopologyHierarchy? Copy(VisualArtifactInterchangeTopologyHierarchy? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeTopologyHierarchy { Source = Copy(source.Source)!, SourceId = source.SourceId,
            SourcePadding = source.SourcePadding, LayoutDirection = source.LayoutDirection };
        copy.CollapsedNodeIds.AddRange(source.CollapsedNodeIds); copy.NodeOrder.AddRange(source.NodeOrder);
        foreach (var value in source.SourceNodes) {
            var node = new VisualArtifactInterchangeTopologySourceNode { Id = value.Id, SourceId = value.SourceId, CssClass = value.CssClass,
                HasPositionOverride = value.HasPositionOverride, PreserveDisplayModeSize = value.PreserveDisplayModeSize,
                DisplayMode = value.DisplayMode, ShowStatusBadge = value.ShowStatusBadge };
            node.PortIds.AddRange(value.PortIds); copy.SourceNodes.Add(node);
        }
        foreach (var value in source.SourceGroups) copy.SourceGroups.Add(new VisualArtifactInterchangeTopologySourceGroup {
            Id = value.Id, SourceId = value.SourceId, CssClass = value.CssClass, HasPositionOverride = value.HasPositionOverride });
        foreach (var value in source.SourceEdges) copy.SourceEdges.Add(CopyIdentity(value));
        foreach (var value in source.SourceScenarios) copy.SourceScenarios.Add(CopyIdentity(value));
        foreach (var value in source.SourceAnnotations) copy.SourceAnnotations.Add(CopyIdentity(value));
        foreach (var value in source.SourceAlignments) copy.SourceAlignments.Add(CopyIdentity(value));
        return copy;
    }
    private static VisualArtifactInterchangeTopologySourceIdentity CopyIdentity(VisualArtifactInterchangeTopologySourceIdentity value) =>
        new() { Id = value.Id, SourceId = value.SourceId, CssClass = value.CssClass };
}
