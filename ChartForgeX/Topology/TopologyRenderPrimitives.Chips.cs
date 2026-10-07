using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    internal static (string Text, double Width) BannerTitle(TopologyChart chart, TopologyRenderOptions options) {
        var available = Math.Max(0, chart.Viewport.Width - chart.Viewport.Padding * 2);
        var text = TrimToEstimatedWidth(chart.Title!, Math.Max(0, available - 72), 34, true, options.TextMeasurement);
        return (text, Math.Min(available, Math.Max(360, EstimateTextWidth(text, 34, true, options.TextMeasurement) + 72)));
    }

    internal static (string Text, double Width) SubtitleChip(TopologyNode node, TopologyNodeDisplayMode mode, TopologyRenderOptions options) {
        bool tile = mode == TopologyNodeDisplayMode.Tile;
        var maximum = Math.Max(tile ? 46 : 48, tile ? node.Width + 28 : node.Width - 50);
        var text = TrimToEstimatedWidth(TrimTo(node.Subtitle!, mode == TopologyNodeDisplayMode.CompactCard ? 12 : 16), maximum - 18, 9.5, true, options.TextMeasurement);
        return (text, Math.Min(maximum, Math.Max(tile ? 46 : 48, EstimateTextWidth(text, 9.5, true, options.TextMeasurement) + 18)));
    }

    internal static double NodeBadgeWidth(TopologyNode node, TopologyRenderOptions options) =>
        Math.Max(18, (options.ResolvedBadgeTextWidth?.Invoke(NodeBadge(node)) ??
            EstimateTextWidth(NodeBadge(node), 9, true, options.TextMeasurement)) + 12);

    /// <summary>Returns the shared badge extent used for layout reservation and native painting.</summary>
    internal static ChartRect NodeBadgeBounds(TopologyNode node, TopologyNodeDisplayMode mode, TopologyRenderOptions options) {
        var width = NodeBadgeWidth(node, options);
        const double height = 18;
        var x = mode == TopologyNodeDisplayMode.Dot ? CenterX(node) + 8
            : mode == TopologyNodeDisplayMode.Icon ? CenterX(node) - width / 2 : node.X + node.Width - width - 6;
        var y = mode == TopologyNodeDisplayMode.Dot ? CenterY(node) - 21
            : mode == TopologyNodeDisplayMode.Icon ? node.Y + node.Height + 4
            : mode == TopologyNodeDisplayMode.Tile ? node.Y - 8 : node.Y + node.Height - height - 6;
        return new ChartRect(x, y, width, height);
    }
}
