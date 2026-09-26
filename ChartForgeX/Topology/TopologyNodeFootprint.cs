using System;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Estimates the caption (label lines and optional subtitle chip) drawn below tile nodes, matching the normalizer's
/// visual bounds, so dense layouts reserve space for it and edge routing treats it as part of the card. Only used with
/// <see cref="TopologyRenderOptions.ReadableDenseLayout"/>; otherwise the caption is ignored as before.
/// </summary>
internal static class TopologyNodeFootprint {
    private const double CaptionFontSize = 11;
    private const double CaptionLineHeight = 14;

    /// <summary>Returns the caption width and the height it adds below the node, or zero for modes without a caption.</summary>
    public static (double Width, double Height) Caption(TopologyChart chart, TopologyNode node) {
        var options = chart.RenderOptions;
        if (options == null || !options.ReadableDenseLayout || !options.IncludeNodeLabels || (node.DisplayMode ?? options.NodeDisplayMode) != TopologyNodeDisplayMode.Tile) return (0, 0);
        var width = 0.0;
        var height = 0.0;
        var lineCount = 1;
        if (!string.IsNullOrWhiteSpace(node.Label)) {
            var lines = NodeTextLines(node.Label, Math.Max(node.Width + 34, 54), CaptionFontSize, true, options.MaxNodeLabelLines, options);
            lineCount = Math.Max(1, lines.Count);
            foreach (var line in lines) width = Math.Max(width, EstimateTextWidth(line, CaptionFontSize, true, chart.TextMeasurement));
            height = 8 + lineCount * CaptionLineHeight;
        }

        if (options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle)) {
            width = Math.Max(width, SubtitleChip(node, TopologyNodeDisplayMode.Tile, options).Width);
            height = Math.Max(height, 24 + lineCount * CaptionLineHeight);
        }

        return (width, height);
    }

    /// <summary>Returns the node width including a wider caption.</summary>
    public static double Width(TopologyChart chart, TopologyNode node) => Math.Max(node.Width, Caption(chart, node).Width);

    /// <summary>Returns the node height including the caption below it.</summary>
    public static double Height(TopologyChart chart, TopologyNode node) => node.Height + Caption(chart, node).Height;
}
