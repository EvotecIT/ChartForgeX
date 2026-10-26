using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

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

    /// <summary>Captures the same styled subtitle chip allocation for layout, diagnostics, routes and native paint.</summary>
    /// <remarks>Only explicit subtitle-role overrides call this owner. Ordinary chip estimates and paint remain unchanged.</remarks>
    internal static (string Text, double Width, double Height, double FontSize, TextMetrics Metrics) MeasuredSubtitleChip(
        TopologyNode node, TopologyNodeDisplayMode mode, TopologyRenderOptions options, FontSpec? font = null, double scale = 1) {
        var chip = SubtitleChip(node, mode, options);
        var preferred = node.SubtitleTextStyle?.FontSize
            ?? (node.TextStyle?.FontSize is double authored ? authored * (9.5 / 11) : options.ResolvedSubtitleChipFontSize);
        var style = TopologyTextPolicy.ResolveRole(node.TextStyle, node.SubtitleTextStyle,
            font ?? options.TextMeasurement?.Font ?? FontSpec.FromFamily(TopologyPaint.Default.FontFamily), 1, 700);
        style.LetterSpacing *= scale;
        var face = new VisualSceneTextFace(style);
        TextMetrics Measure(double size) => face.Prepare(chip.Text, size * scale).Metrics;
        var ordinary = Measure(preferred);
        var width = Math.Max(chip.Width * scale, ordinary.Width + 18 * scale);
        var height = Math.Max(17 * scale, ordinary.Height + 4 * scale);
        var size = ChartTextFitting.FitFontSize(Math.Min(9.5, preferred), preferred, candidate => {
            var measured = Measure(candidate);
            return measured.Width <= width - 18 * scale && measured.Height <= height - 4 * scale;
        });
        return (chip.Text, width, height, size, Measure(size));
    }

    internal static double NodeBadgeWidth(TopologyNode node, TopologyRenderOptions options) =>
        Math.Max(18, (options.ResolvedBadgeTextWidth?.Invoke(NodeBadge(node)) ??
            EstimateTextWidth(NodeBadge(node), 9, true, options.TextMeasurement)) + 12);

    /// <summary>Returns the shared badge extent used for layout reservation and native painting.</summary>
    internal static ChartRect NodeBadgeBounds(TopologyNode node, TopologyNodeDisplayMode mode, TopologyRenderOptions options) {
        if (TopologyPreparedNodeBasis.ForPresentation(node) is {} basis) return basis.Project(node, NodeBadgeBounds(basis.OriginalNode(node), mode, options));
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
