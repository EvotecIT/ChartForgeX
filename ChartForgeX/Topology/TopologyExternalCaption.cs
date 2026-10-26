using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>Measures opted-in external captions once for paint, actual footprint, fitting and routing.</summary>
internal static class TopologyExternalCaption {
    internal static bool HasPolicy(TopologyNode node) => node.CaptionPlacement.HasValue || node.CaptionSurface.HasValue
        || node.CaptionGap.HasValue || node.CaptionTextStyle != null;

    internal static Plan? Prepare(TopologyNode node, TopologyRenderOptions options) {
        if (TopologyPreparedNodeBasis.ForPresentation(node) is {} basis)
            return Prepare(basis.OriginalNode(node), options)?.Project(basis, node);
        var mode = EffectiveNodeDisplayMode(node, options);
        if (!HasPolicy(node) || !options.IncludeNodeLabels || !node.ShowLabel
            || mode == TopologyNodeDisplayMode.Icon && !options.IncludeIconLabels
            || mode == TopologyNodeDisplayMode.Artwork && !node.CaptionPlacement.HasValue
            || mode is not (TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Artwork)) return null;
        var measurement = options.TextMeasurement ?? new TextMeasurementContext(TopologyPaint.Default.FontFamily);
        var icon = mode == TopologyNodeDisplayMode.Icon;
        var content = node.LabelContent;
        if (node.MaximumLabelCharacters is int maximum && content.PlainText.Length > maximum)
            content = content.Slice(0, TextElementBoundary.Snap(content.PlainText, maximum));
        content = TopologyTextLayout.DisplayRows(content, options.AllowMultilineNodeLabels);
        var hasLabel = !string.IsNullOrWhiteSpace(content.PlainText);
        var hasSubtitle = !icon && options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle);
        if (!hasLabel && !hasSubtitle) return null;
        var size = node.CaptionTextStyle?.FontSize
            ?? (icon ? IconLabelFontSize(node, measurement, options.ResolvedIconLabelFontSize) : 11);
        var style = TopologyTextPolicy.ResolveRole(node.TextStyle, node.CaptionTextStyle, measurement.Font, size, content.IsStructured ? 400 : 700);
        style.Alignment = TextAlignment.Center;
        var width = Math.Max(1, node.MaximumTextWidth ?? node.Width + (icon ? 18 : 34));
        var wrap = options.WrapNodeLabels ? TextWrapMode.Word : TextWrapMode.NoWrap;
        var maxLines = Math.Max(1, options.MaxNodeLabelLines);
        var layout = TextLayoutEngine.Layout(content, width, style, wrap, maxLines);
        var labelWidth = hasLabel ? layout.Metrics.Width : 0;
        var labelHeight = hasLabel ? layout.Metrics.Height : 0;
        var chip = hasSubtitle ? MeasuredSubtitleChip(node, TopologyNodeDisplayMode.Tile, options, measurement.Font) : default;
        var plate = node.CaptionSurface == TopologyCaptionSurface.Plate || !node.CaptionSurface.HasValue && icon;
        var padX = plate ? 6d : 0; var padY = plate ? 2d : 0;
        var innerWidth = Math.Max(labelWidth, hasSubtitle ? chip.Width : 0);
        var innerHeight = labelHeight + (hasSubtitle ? (hasLabel ? 2 : 0) + chip.Height : 0);
        var height = innerHeight + 2 * padY;
        var gap = node.CaptionGap ?? (icon ? !string.IsNullOrWhiteSpace(NodeBadge(node)) ? 25 : 5 : 4);
        var top = node.CaptionPlacement == TopologyCaptionPlacement.Top ? node.Y - gap - height : node.Y + node.Height + gap;
        var center = node.X + node.Width / 2;
        var bounds = new ChartRect(center - innerWidth / 2 - padX, top, innerWidth + 2 * padX, height);
        var label = new ChartRect(center - width / 2, top + padY, width, labelHeight);
        var halfStroke = plate ? (options.ResolvedCaptionStrokeWidth ?? ChartForgeX.Themes.VisualTheme.Graphite().AxisStrokeWidth) / 2 : 0;
        var ink = new ChartRect(bounds.X - halfStroke, bounds.Y - halfStroke, bounds.Width + 2 * halfStroke, bounds.Height + 2 * halfStroke);
        return new Plan(ink, bounds, label, content, style, wrap, maxLines, hasLabel,
            hasSubtitle ? label.Bottom + (hasLabel ? 2 : 0) : (double?)null, plate);
    }

    internal static ChartRect OccupiedBounds(TopologyNode node, Plan? caption) {
        var left = node.X; var top = node.Y; var right = node.X + node.Width; var bottom = node.Y + node.Height;
        if (caption != null) { left = Math.Min(left, caption.Bounds.Left); top = Math.Min(top, caption.Bounds.Top);
            right = Math.Max(right, caption.Bounds.Right); bottom = Math.Max(bottom, caption.Bounds.Bottom); }
        return new ChartRect(left, top, right - left, bottom - top);
    }

    internal sealed class Plan {
        internal Plan(ChartRect bounds, ChartRect plateBounds, ChartRect label, TextContent content, TextStyle style, TextWrapMode wrap,
            int maximumLines, bool hasLabel, double? subtitleTop, bool plate) {
            Bounds = bounds; PlateBounds = plateBounds; Label = label; Content = content; Style = style; Wrap = wrap;
            MaximumLines = maximumLines; HasLabel = hasLabel; SubtitleTop = subtitleTop; Plate = plate;
        }
        internal ChartRect Bounds { get; }
        internal ChartRect PlateBounds { get; }
        internal ChartRect Label { get; }
        internal TextContent Content { get; }
        internal TextStyle Style { get; }
        internal TextWrapMode Wrap { get; }
        internal int MaximumLines { get; }
        internal bool HasLabel { get; }
        internal double? SubtitleTop { get; }
        internal bool Plate { get; }
        internal Plan Project(TopologyPreparedNodeBasis basis, TopologyNode node) {
            var style = Style.Clone(); style.FontSize *= basis.Factor; style.LetterSpacing *= basis.Factor;
            return new Plan(basis.Project(node, Bounds), basis.Project(node, PlateBounds), basis.Project(node, Label), Content, style, Wrap,
                MaximumLines, HasLabel, SubtitleTop.HasValue ? basis.Project(node, new ChartPoint(basis.OriginalBody.X, SubtitleTop.Value)).Y : (double?)null, Plate);
        }
    }
}
