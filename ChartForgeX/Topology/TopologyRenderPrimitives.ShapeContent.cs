using System.Linq;
namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    /// <summary>Identifies native shapes whose complete content footprint is enclosed by their node body.</summary>
    internal static bool UseEnclosedShapeContent(TopologyNode node, TopologyNodeDisplayMode mode, TopologyRenderOptions options) => node.TableContent != null || (node.Shape.HasValue || node.CornerRadius.HasValue || node.Details.Any(detail => detail.BadgeStyle != null))
        && !TopologyNodeShapeGeometry.IsActor(node)
        && mode is TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard or TopologyNodeDisplayMode.Pill
        && (node.Shape is not (TopologyNodeShape.Rectangle or TopologyNodeShape.Rounded) || node.CornerRadius.HasValue || node.FitLabelToContent || node.TextStyle?.HasOverrides == true || node.MaximumTextWidth.HasValue || node.ContentPadding.HasValue || node.Details.Any(detail => detail.BadgeStyle != null))
        && (HasInlineNodeGlyph(node, mode, options) || !string.IsNullOrWhiteSpace(node.Subtitle) || node.Details.Count > 0
            || !string.IsNullOrWhiteSpace(node.Badge) || options.IncludeStatusBadges && node.ShowStatusBadge
            || node.MaximumTextWidth.HasValue || node.ContentPadding.HasValue);

    internal static bool HasInlineNodeGlyph(TopologyNode node, TopologyNodeDisplayMode mode, TopologyRenderOptions options) =>
        TopologyRenderPrimitives.HasExplicitIconShape(node.IconShape) || node.Kind != TopologyNodeKind.Generic || !string.IsNullOrWhiteSpace(node.Symbol) || !string.IsNullOrWhiteSpace(node.IconId)
        || ResolveRenderableNodeArtwork(node, options) != null
        || !TopologyNodeShapeGeometry.IsActor(node) && mode is TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Artwork;
}
