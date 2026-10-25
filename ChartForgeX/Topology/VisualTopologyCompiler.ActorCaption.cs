using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildActorCaption(TopologyNode node, bool active) {
        var caption = TopologyActorCaption.Prepare(node, _options, _chart.TextMeasurement);
        var color = Highlight(Color(node.LabelColor, _colors.Foreground), active);
        var titleStyle = caption.TitleStyle.Clone(); titleStyle.Color = color; titleStyle.FontSize *= _scale;
        var titlePaint = string.IsNullOrWhiteSpace(node.LabelColor)
            ? ForegroundPaint(color, HighlightFactor(active))
            : SourcePaint(node.LabelColor, color, ChartForgeX.Themes.SvgColorRole.Any, HighlightFactor(active), _colors.Foreground);
        _builder.InlineText(caption.Title, Project(caption.TitleBounds), titleStyle, caption.Wrap,
            _options.MaxNodeLabelLines, "topology-node-label", node.Id + "-label", titlePaint);
        var subtitleStyle = caption.SubtitleStyle.Clone(); subtitleStyle.Color = NodeSubtitleColor(node, active); subtitleStyle.FontSize *= _scale;
        _builder.InlineText(caption.Subtitle, Project(caption.SubtitleBounds), subtitleStyle, caption.Wrap,
            _options.MaxNodeSubtitleLines, "topology-node-subtitle", node.Id + "-subtitle", NodeSubtitlePaint(node, active));

        ChartRect Project(ChartRect bounds) => Bounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }
}
