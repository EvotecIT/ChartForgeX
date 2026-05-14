namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static TopologyIconDefinition? ResolveNodeIcon(TopologyNode node, TopologyRenderOptions options) {
        return string.IsNullOrWhiteSpace(node.IconId) ? null : (options.IconCatalog ?? TopologyIconCatalog.Default()).Resolve(node.IconId);
    }

    public static TopologyIconArtwork? ResolveRenderableNodeArtwork(TopologyNode node, TopologyRenderOptions options) {
        if (IsRenderableArtwork(node.Artwork)) return node.Artwork;
        var iconArtwork = ResolveNodeIcon(node, options)?.Artwork;
        return IsRenderableArtwork(iconArtwork) ? iconArtwork : null;
    }

    public static string? ResolveNodeArtworkSource(TopologyNode node, TopologyRenderOptions options) {
        if (IsRenderableArtwork(node.Artwork)) return "node";
        return IsRenderableArtwork(ResolveNodeIcon(node, options)?.Artwork) ? "icon" : null;
    }

    public static bool HasRenderableNodeArtwork(TopologyNode node) {
        return IsRenderableArtwork(node.Artwork);
    }

    public static TopologyIconDefinition? ResolveGroupIcon(TopologyGroup group, TopologyRenderOptions options) {
        return string.IsNullOrWhiteSpace(group.IconId) ? null : (options.IconCatalog ?? TopologyIconCatalog.Default()).Resolve(group.IconId);
    }

    public static TopologyIconShape EffectiveIconShape(TopologyNode node, TopologyRenderOptions options) {
        var icon = ResolveNodeIcon(node, options);
        if (icon != null && icon.Shape != TopologyIconShape.Auto) return icon.Shape;
        return node.Kind switch {
            TopologyNodeKind.Hub or TopologyNodeKind.Branch or TopologyNodeKind.Location => TopologyIconShape.Site,
            TopologyNodeKind.Server => TopologyIconShape.Server,
            TopologyNodeKind.Database => TopologyIconShape.Database,
            TopologyNodeKind.Storage => TopologyIconShape.Storage,
            TopologyNodeKind.Cloud => TopologyIconShape.Cloud,
            TopologyNodeKind.Network => TopologyIconShape.Network,
            TopologyNodeKind.NetworkSegment => TopologyIconShape.NetworkSegment,
            TopologyNodeKind.Service => TopologyIconShape.Service,
            TopologyNodeKind.Application => TopologyIconShape.Application,
            TopologyNodeKind.Endpoint => TopologyIconShape.Desktop,
            TopologyNodeKind.Certificate => TopologyIconShape.Certificate,
            TopologyNodeKind.Person => TopologyIconShape.Person,
            TopologyNodeKind.Team => TopologyIconShape.Team,
            TopologyNodeKind.Namespace => TopologyIconShape.Domain,
            _ => TopologyIconShape.Badge
        };
    }

    public static string NodeGlyph(TopologyNode node, TopologyRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(node.Symbol)) return TrimTo(node.Symbol!.Trim(), 4);
        var icon = ResolveNodeIcon(node, options);
        if (icon != null && !string.IsNullOrWhiteSpace(icon.Symbol)) return TrimTo(icon.Symbol!, 4);
        return KindGlyph(icon?.NodeKind ?? node.Kind);
    }

    public static TopologyNodeDisplayMode EffectiveNodeDisplayMode(TopologyNode node, TopologyRenderOptions options) {
        if (node.DisplayMode.HasValue) return node.DisplayMode.Value;
        if (IsRenderableArtwork(node.Artwork)) return TopologyNodeDisplayMode.Artwork;
        var icon = ResolveNodeIcon(node, options);
        return icon?.DisplayMode ?? options.NodeDisplayMode;
    }

    private static bool IsRenderableArtwork(TopologyIconArtwork? artwork) {
        return artwork != null && artwork.IsSafe && (artwork.HasSvgBody || artwork.HasImageHref);
    }
}
