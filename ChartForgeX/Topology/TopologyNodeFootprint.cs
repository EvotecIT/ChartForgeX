using System;
using ChartForgeX.Typography;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Reserves external captions for layout and routing using the same native allocation as painting.
/// Tile captions retain their readable-dense-layout policy; opted-in Icon typography reserves its resolved plate.
/// </summary>
internal static class TopologyNodeFootprint {
    /// <summary>Measures the exact native body, badge and external caption reservation used by layout normalization.</summary>
    internal static Bounds? RenderedBounds(TopologyNode node, TopologyRenderOptions options) {
        if (TopologyPreparedNodeBasis.ForPresentation(node) is {} basis) {
            var captured = RenderedBounds(basis.OriginalNode(node), options); if (!captured.HasValue) return null;
            var rect = basis.Project(node, new ChartForgeX.Primitives.ChartRect(captured.Value.X, captured.Value.Y,
                captured.Value.Right - captured.Value.X, captured.Value.Bottom - captured.Value.Y));
            return new Bounds(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
        var displayMode = EffectiveNodeDisplayMode(node, options);
        if (displayMode == TopologyNodeDisplayMode.Hidden) return null;
        var bounds = new Bounds(node.X, node.Y, node.X + node.Width, node.Y + node.Height);
        if (UseEnclosedShapeContent(node, displayMode, options)) return bounds;
        if (options.IncludeStatusBadges && ShouldRenderNodeStatusBadge(node, options)) {
            var cx = node.X + node.Width - 11;
            var cy = node.Y + 11;
            bounds = Include(bounds, cx - 10, cy - 10, cx + 10, cy + 10);
        }

        var badge = NodeBadge(node);
        if (!string.IsNullOrWhiteSpace(badge)) {
            var badgeBounds = NodeBadgeBounds(node, displayMode, options);
            bounds = Include(bounds, badgeBounds.X, badgeBounds.Y, badgeBounds.Right, badgeBounds.Bottom);
        }

        if (TopologyExternalCaption.HasPolicy(node)) {
            var occupied = TopologyExternalCaption.OccupiedBounds(node, TopologyExternalCaption.Prepare(node, options));
            return Include(bounds, occupied.Left, occupied.Top, occupied.Right, occupied.Bottom);
        }
        if (!options.IncludeNodeLabels) return bounds;
        if (TopologyActorCaption.Applies(node, options)) {
            var caption = TopologyActorCaption.Prepare(node, options).Bounds;
            return Include(bounds, caption.Left, caption.Top, caption.Right, caption.Bottom);
        }
        if (displayMode == TopologyNodeDisplayMode.Icon) {
            if (options.IncludeIconLabels) {
                var labelWidth = IconLabelPlateWidth(node, options.TextMeasurement, options.ResolvedIconLabelFontSize);
                var labelCenter = CenterX(node);
                var labelY = IconLabelPlateY(node);
                bounds = Include(bounds, labelCenter - labelWidth / 2, labelY, labelCenter + labelWidth / 2, labelY + IconLabelPlateHeight(node, options.TextMeasurement, options.ResolvedIconLabelFontSize));
            }

            return bounds;
        }

        if (displayMode == TopologyNodeDisplayMode.Tile) {
            var labelLines = TileCaptionLines(node, options);
            var labelCenter = CenterX(node);
            var labelTop = node.Y + node.Height + 4;
            var lineCount = Math.Max(1, node.LabelContent.IsStructured ? options.MaxNodeLabelLines : labelLines.Count);
            var lineHeight = node.LabelContent.IsStructured ? TopologyTextLayout.StructuredCaptionRowHeight(node, options.TextMeasurement) : 14;
            if (labelLines.Count == 0) bounds = Include(bounds, labelCenter, labelTop, labelCenter, labelTop + lineCount * lineHeight + 4);
            foreach (var line in labelLines) {
                var labelWidth = EstimateTextWidth(line, 11, true, options.TextMeasurement);
                if (node.LabelContent.IsStructured) labelWidth = Math.Max(node.Width, labelWidth);
                bounds = Include(bounds, labelCenter - labelWidth / 2, labelTop, labelCenter + labelWidth / 2, labelTop + lineCount * lineHeight + 4);
            }

            if (options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle)) {
                var subtitleWidth = SubtitleChip(node, TopologyNodeDisplayMode.Tile, options).Width;
                var subtitleHeight = 17d;
                if (node.SubtitleTextStyle?.HasOverrides == true) {
                    var measured = MeasuredSubtitleChip(node, TopologyNodeDisplayMode.Tile, options);
                    subtitleWidth = measured.Width; subtitleHeight = measured.Height;
                }
                var subtitleY = node.Y + node.Height + 7 + lineCount * lineHeight;
                bounds = Include(bounds, labelCenter - subtitleWidth / 2, subtitleY, labelCenter + subtitleWidth / 2, subtitleY + subtitleHeight);
            }
        }

        return bounds;
    }

    private static Bounds Include(Bounds bounds, double left, double top, double right, double bottom) {
        var x = Math.Min(bounds.X, left); var y = Math.Min(bounds.Y, top);
        return new Bounds(x, y, Math.Max(bounds.Right, right), Math.Max(bounds.Bottom, bottom));
    }

    /// <summary>Stores measured extrema directly, preserving the normalizer's default floating-point arithmetic.</summary>
    internal readonly struct Bounds {
        internal double X { get; }
        internal double Y { get; }
        internal double Right { get; }
        internal double Bottom { get; }
        internal Bounds(double x, double y, double right, double bottom) { X = x; Y = y; Right = right; Bottom = bottom; }
    }

    private const double CaptionFontSize = 11;
    private const double CaptionLineHeight = 14;

    /// <summary>Returns the caption width and the height it adds below the node, or zero for modes without a caption.</summary>
    public static (double Width, double Height) Caption(TopologyChart chart, TopologyNode node) {
        if (TopologyPreparedNodeBasis.ForPresentation(node) is {} basis) {
            var caption = Caption(chart, basis.OriginalNode(node)); return (caption.Width * basis.Factor, caption.Height * basis.Factor);
        }
        var options = chart.RenderOptions;
        if (options == null) return (0, 0);
        if (TopologyExternalCaption.HasPolicy(node)) {
            var bounds = TopologyExternalCaption.OccupiedBounds(node, TopologyExternalCaption.Prepare(node, options));
            return (bounds.Width, Math.Max(0, bounds.Height - node.Height));
        }
        var mode = EffectiveNodeDisplayMode(node, options);
        if (mode == TopologyNodeDisplayMode.Icon && node.TextStyle?.HasOverrides == true
            && options.IncludeNodeLabels && node.ShowLabel && options.IncludeIconLabels) {
            // An opted-in node caption must reserve its resolved plate for routes as well
            // as normalization and paint. Null typography keeps the incumbent footprint.
            var width = IconLabelPlateWidth(node, chart.TextMeasurement, options.ResolvedIconLabelFontSize);
            var height = IconLabelPlateY(node) - node.Y - node.Height
                + IconLabelPlateHeight(node, chart.TextMeasurement, options.ResolvedIconLabelFontSize);
            return (width, height);
        }
        if (TopologyActorCaption.Applies(node, options)) {
            var bounds = TopologyActorCaption.Prepare(node, options, chart.TextMeasurement).Bounds;
            return (bounds.Width, Math.Max(0, bounds.Bottom - node.Y - node.Height));
        }
        if (!options.ReadableDenseLayout) return (0, 0);
        return CaptionSize(node, options, chart.TextMeasurement, node.DisplayMode ?? options.NodeDisplayMode);
    }

    /// <summary>
    /// Returns the size of the caption the renderers draw below a tile node with the given options, whether or not the
    /// layout reserved room for it. Used by diagnostics to measure what is on the page.
    /// </summary>
    public static (double Width, double Height) RenderedCaption(TopologyNode node, TopologyRenderOptions options, TextMeasurementContext? measurement) {
        if (TopologyExternalCaption.HasPolicy(node)) {
            var occupied = TopologyExternalCaption.OccupiedBounds(node, TopologyExternalCaption.Prepare(node, options));
            return (occupied.Width, Math.Max(0, occupied.Height - node.Height));
        }
        if (TopologyPreparedNodeBasis.ForPresentation(node) is {} basis) {
            var raw = basis.OriginalNode(node); var caption = RenderedCaption(raw, options, measurement);
            return (caption.Width * basis.Factor, caption.Height * basis.Factor);
        }
        return CaptionSize(node, options, measurement, EffectiveNodeDisplayMode(node, options));
    }

    private static (double Width, double Height) CaptionSize(TopologyNode node, TopologyRenderOptions options, TextMeasurementContext? measurement, TopologyNodeDisplayMode mode) {
        if (!options.IncludeNodeLabels || mode != TopologyNodeDisplayMode.Tile) return (0, 0);
        var width = 0.0;
        var height = 0.0;
        var lineCount = 1;
        var lineHeight = node.LabelContent.IsStructured ? TopologyTextLayout.StructuredCaptionRowHeight(node, measurement) : CaptionLineHeight;
        if (!string.IsNullOrWhiteSpace(node.Label)) {
            var lines = TileCaptionLines(node, options);
            lineCount = Math.Max(1, node.LabelContent.IsStructured ? options.MaxNodeLabelLines : lines.Count);
            foreach (var line in lines) width = Math.Max(width, EstimateTextWidth(line, CaptionFontSize, true, measurement));
            if (node.LabelContent.IsStructured) width = Math.Max(width, node.Width);
            height = 8 + lineCount * lineHeight;
        }

        if (options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle)) {
            if (node.SubtitleTextStyle?.HasOverrides == true) {
                var measured = MeasuredSubtitleChip(node, TopologyNodeDisplayMode.Tile, options, measurement?.Font);
                width = Math.Max(width, measured.Width);
                height = Math.Max(height, 7 + lineCount * lineHeight + measured.Height);
            } else {
                width = Math.Max(width, SubtitleChip(node, TopologyNodeDisplayMode.Tile, options).Width);
                height = Math.Max(height, 24 + lineCount * lineHeight);
            }
        }

        return (width, height);
    }

    internal static ChartRect? ExplicitOccupiedBounds(TopologyNode node, TopologyRenderOptions options) =>
        TopologyExternalCaption.HasPolicy(node) ? TopologyExternalCaption.OccupiedBounds(node, TopologyExternalCaption.Prepare(node, options)) : (ChartRect?)null;

    internal static double TopInset(TopologyChart chart, TopologyNode node) => chart.RenderOptions is {} options
        && TopologyExternalCaption.HasPolicy(node) ? Math.Max(0, node.Y - TopologyExternalCaption.OccupiedBounds(node, TopologyExternalCaption.Prepare(node, options)).Top) : 0;

    internal static bool CaptionOnSide(TopologyChart chart, TopologyNode node, TopologyEdgePort side) {
        if (chart.RenderOptions is {} options && TopologyExternalCaption.HasPolicy(node)) {
            var plan = TopologyExternalCaption.Prepare(node, options);
            return plan != null && (side == TopologyEdgePort.Top && plan.Bounds.Top < node.Y
                || side == TopologyEdgePort.Bottom && plan.Bounds.Bottom > node.Y + node.Height);
        }
        return side == TopologyEdgePort.Bottom && Caption(chart, node).Height > .5;
    }

    /// <summary>Returns the node width including a wider caption.</summary>
    public static double Width(TopologyChart chart, TopologyNode node) => Math.Max(node.Width, Caption(chart, node).Width);

    /// <summary>Returns the node height including the caption below it.</summary>
    public static double Height(TopologyChart chart, TopologyNode node) => node.Height + Caption(chart, node).Height;
}
