using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>Immutable capture of the existing qualified caption flags, without adding native authoring options.</summary>
internal sealed class TopologyCaptionPresentation {
    internal TopologyCaptionPresentation(bool labels, bool icons, bool subtitles, bool wrap, bool multiline, bool readable, int titleLines, int subtitleLines, TopologyRoutePresentation? routes = null) {
        Routes = routes;
        IncludeNodeLabels = labels; IncludeIconLabels = icons; IncludeTileSubtitles = subtitles; WrapNodeLabels = wrap;
        AllowMultilineNodeLabels = multiline; ReadableDenseLayout = readable; MaxNodeLabelLines = titleLines; MaxNodeSubtitleLines = subtitleLines;
    }
    internal TopologyRoutePresentation? Routes { get; }
    internal bool IncludeNodeLabels { get; }
    internal bool IncludeIconLabels { get; }
    internal bool IncludeTileSubtitles { get; }
    internal bool WrapNodeLabels { get; }
    internal bool AllowMultilineNodeLabels { get; }
    internal bool ReadableDenseLayout { get; }
    internal int MaxNodeLabelLines { get; }
    internal int MaxNodeSubtitleLines { get; }

    internal static TopologyCaptionPresentation? Capture(TopologyRenderOptions options, IEnumerable<TopologyNode> nodes, IEnumerable<TopologyEdge>? edges = null) {
        var qualified = false;
        foreach (var node in nodes) {
            var mode = TopologyRenderPrimitives.EffectiveNodeDisplayMode(node, options);
            if (TopologyPreparedNodeBasis.Resolve(node) != null || TopologyPreparedNodeBasis.IsActor(node.Shape) || mode is TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile) { qualified = true; break; }
        }
        var qualifiedEdges = false;
        var hasEdges = false;
        if (edges != null) foreach (var edge in edges) { hasEdges = true; if (edge.PreparedEdgePlan != null) qualifiedEdges = true; }
        var routes = hasEdges ? TopologyRoutePresentation.Capture(options) : null;
        qualifiedEdges |= routes != null;
        if ((!qualified && !qualifiedEdges) || Matches(options, new TopologyRenderOptions()) && routes == null) return null;
        return new TopologyCaptionPresentation(options.IncludeNodeLabels, options.IncludeIconLabels, options.IncludeTileSubtitles,
            options.WrapNodeLabels, options.AllowMultilineNodeLabels, options.ReadableDenseLayout, options.MaxNodeLabelLines, options.MaxNodeSubtitleLines, routes);
    }
    internal void Apply(TopologyRenderOptions options) {
        Routes?.Apply(options);
        options.IncludeNodeLabels = IncludeNodeLabels; options.IncludeIconLabels = IncludeIconLabels; options.IncludeTileSubtitles = IncludeTileSubtitles;
        options.WrapNodeLabels = WrapNodeLabels; options.AllowMultilineNodeLabels = AllowMultilineNodeLabels; options.ReadableDenseLayout = ReadableDenseLayout;
        options.MaxNodeLabelLines = MaxNodeLabelLines; options.MaxNodeSubtitleLines = MaxNodeSubtitleLines;
    }
    internal void Validate(int maximumLines) {
        Routes?.ValidatePortable();
        // A portable line ceiling cannot exceed the existing portable text-element/character budget.
        // Static native preparation keeps its incumbent option behavior; validation occurs only at an explicit wire boundary.
        if (MaxNodeLabelLines < 1 || MaxNodeLabelLines > maximumLines
            || MaxNodeSubtitleLines < 1 || MaxNodeSubtitleLines > maximumLines)
            throw new ArgumentOutOfRangeException(nameof(MaxNodeLabelLines), "Captured caption line ceilings must be positive within the portable text-character budget.");
    }
    private static bool Matches(TopologyRenderOptions first, TopologyRenderOptions second) => first.IncludeNodeLabels == second.IncludeNodeLabels
        && first.IncludeIconLabels == second.IncludeIconLabels && first.IncludeTileSubtitles == second.IncludeTileSubtitles
        && first.WrapNodeLabels == second.WrapNodeLabels && first.AllowMultilineNodeLabels == second.AllowMultilineNodeLabels
        && first.ReadableDenseLayout == second.ReadableDenseLayout && first.MaxNodeLabelLines == second.MaxNodeLabelLines
        && first.MaxNodeSubtitleLines == second.MaxNodeSubtitleLines;
}
