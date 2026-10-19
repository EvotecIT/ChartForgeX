namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    /// <summary>Identifies native shapes whose complete content footprint is enclosed by their node body.</summary>
    internal static bool UseEnclosedShapeContent(TopologyNode node, TopologyNodeDisplayMode mode, TopologyRenderOptions options) => node.Shape.HasValue
        && node.Shape != TopologyNodeShape.Actor
        && mode is TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard or TopologyNodeDisplayMode.Pill
        && (node.Shape is not (TopologyNodeShape.Rectangle or TopologyNodeShape.Rounded) || node.FitLabelToContent)
        && (HasInlineNodeGlyph(node, mode, options) || !string.IsNullOrWhiteSpace(node.Subtitle) || node.Details.Count > 0
            || !string.IsNullOrWhiteSpace(node.Badge) || options.IncludeStatusBadges && node.ShowStatusBadge);

    internal static bool HasInlineNodeGlyph(TopologyNode node, TopologyNodeDisplayMode mode, TopologyRenderOptions options) =>
        node.Kind != TopologyNodeKind.Generic || !string.IsNullOrWhiteSpace(node.Symbol) || !string.IsNullOrWhiteSpace(node.IconId)
        || ResolveRenderableNodeArtwork(node, options) != null
        || node.Shape != TopologyNodeShape.Actor && mode is TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Artwork;
}
