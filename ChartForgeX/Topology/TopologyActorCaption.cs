using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>Measures opted-in Actor title/subtitle content once for native paint and all external reservations.</summary>
internal sealed class TopologyActorCaption {
    private TopologyActorCaption(TextContent title, TextStyle titleStyle, TextLayout titleLayout, ChartRect titleBounds,
        TextContent subtitle, TextStyle subtitleStyle, TextLayout subtitleLayout, ChartRect subtitleBounds, TextWrapMode wrap) {
        Title = title; TitleStyle = titleStyle; TitleBounds = titleBounds;
        Subtitle = subtitle; SubtitleStyle = subtitleStyle; SubtitleBounds = subtitleBounds; Wrap = wrap;
        var width = Math.Max(titleLayout.Metrics.Width, subtitleLayout.Metrics.Width);
        Bounds = new ChartRect(titleBounds.X + (titleBounds.Width - width) / 2, titleBounds.Y, width, subtitleBounds.Bottom - titleBounds.Y);
    }
    internal TextContent Title { get; }
    internal TextContent Subtitle { get; }
    internal TextStyle TitleStyle { get; }
    internal TextStyle SubtitleStyle { get; }
    internal ChartRect TitleBounds { get; }
    internal ChartRect SubtitleBounds { get; }
    internal ChartRect Bounds { get; }
    internal TextWrapMode Wrap { get; }
    internal static bool Applies(TopologyNode node, TopologyRenderOptions options) => TopologyNodeShapeGeometry.IsActor(node)
        && EffectiveNodeDisplayMode(node, options) is TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard
        && options.IncludeNodeLabels && node.ShowLabel && !string.IsNullOrWhiteSpace(node.Subtitle);

    internal static TopologyActorCaption Prepare(TopologyNode node, TopologyRenderOptions options, TextMeasurementContext? measurement = null) {
        measurement ??= options.TextMeasurement ?? new TextMeasurementContext(TopologyPaint.Default.FontFamily);
        var units = TopologyPreparedNodeBasis.Units(node);
        var baseSize = 11 * options.ResolvedEdgeLabelScale * units;
        var size = node.TextStyle?.FontSize ?? baseSize;
        var title = node.LabelContent;
        if (node.MaximumLabelCharacters is int maximum && title.PlainText.Length > maximum)
            title = title.Slice(0, TextElementBoundary.Snap(title.PlainText, maximum));
        title = TopologyTextLayout.DisplayRows(title, options.AllowMultilineNodeLabels);
        var subtitle = TopologyTextLayout.DisplayRows(TextContent.Plain(node.Subtitle!), options.AllowMultilineNodeLabels);
        var titleStyle = title.IsStructured ? TopologyTextPolicy.ResolveStructured(node.TextStyle, measurement.Font, size)
            : TopologyTextPolicy.Resolve(node.TextStyle, measurement.Font, size, 600);
        titleStyle.FontSize = size; titleStyle.Alignment = TextAlignment.Center;
        var subtitleSize = node.SubtitleTextStyle?.FontSize ?? size * .85;
        var subtitleStyle = TopologyTextPolicy.ResolveRole(node.TextStyle, node.SubtitleTextStyle, measurement.Font, subtitleSize, 400);
        subtitleStyle.Alignment = TextAlignment.Center;
        var width = units == 1 ? Math.Max(1, Math.Min(node.Width - 2 * (node.ContentPadding ?? 6), node.MaximumTextWidth ?? node.Width * .9))
            : Math.Max(1, Math.Min(node.Width - 2 * (node.ContentPadding ?? 6) * units, node.MaximumTextWidth.HasValue ? node.MaximumTextWidth.Value * units : node.Width * .9));
        var wrap = options.WrapNodeLabels ? TextWrapMode.Word : TextWrapMode.NoWrap;
        var titleLayout = TextLayoutEngine.Layout(title, width, titleStyle, wrap, Math.Max(1, options.MaxNodeLabelLines));
        var subtitleLayout = TextLayoutEngine.Layout(subtitle, width, subtitleStyle, wrap, Math.Max(1, options.MaxNodeSubtitleLines));
        var left = CenterX(node) - width / 2;
        var top = DiagramNodeLabelCenterY(node) - titleLayout.Metrics.LineHeight / 2;
        var titleBounds = new ChartRect(left, top, width, titleLayout.Metrics.Height);
        var subtitleBounds = new ChartRect(left, titleBounds.Bottom + 4 * units, width, subtitleLayout.Metrics.Height);
        return new TopologyActorCaption(title, titleStyle, titleLayout, titleBounds, subtitle, subtitleStyle, subtitleLayout, subtitleBounds, wrap);
    }
}
