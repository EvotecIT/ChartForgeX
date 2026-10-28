using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildActorIcon(TopologyNode node, ChartColor accent, bool active, SvgColorRole role) {
        if (!node.IconShape.HasValue && ResolveNodeIcon(node, _options) == null && ResolveRenderableNodeArtwork(node, _options) == null && string.IsNullOrWhiteSpace(node.Symbol)) {
            _builder.AddDiagnostic(new VisualDiagnostic("topology.actor-icon-unresolved", "The actor icon has no resolved native glyph or safe artwork; its frame and source identity are retained."));
            return;
        }
        var scale = TopologyNodeShapeGeometry.ActorScale(node);
        BuildGlyph(node, node.X + node.Width / 2, node.Y + 32 * scale, accent, 1.6 * scale, role, artworkOpacity: HighlightFactor(active), canonicalAnchor: TopologyPreparedNodeBasis.Resolve(node)?.GlyphAnchor);
    }
}
