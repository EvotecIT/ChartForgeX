using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildExternalCaption(TopologyNode node, ChartColor accent, bool active) {
        var plan = TopologyExternalCaption.Prepare(node, _options);
        if (plan == null) return;
        if (plan.Plate) {
            var bounds = Bounds(plan.PlateBounds.X, plan.PlateBounds.Y, plan.PlateBounds.Width, plan.PlateBounds.Height);
            var stroke = ChartColorMath.WithOpacity(accent, .4);
            _builder.Rect(bounds, Highlight(_colors.Surface, active), stroke, _context.Theme.AxisStrokeWidth * _scale,
                _context.Theme.BarRadius * _scale, "topology-node-icon-label",
                paint: new VisualScenePaintBinding(CardPaint(Highlight(_colors.Surface, active), HighlightFactor(active)), NodeAccentPaint(node, stroke, active, .4)));
        }
        if (plan.HasLabel) {
            var authored = node.CaptionTextStyle?.Color ?? node.TextStyle?.Color;
            var color = Highlight(authored ?? Color(node.LabelColor, _colors.Foreground), active);
            var style = plan.Style.Clone(); style.Color = color; style.FontSize *= _scale;
            var paint = authored.HasValue ? SourcePaint(authored.Value.ToHexRgba(), color, SvgColorRole.Any, HighlightFactor(active), _colors.Foreground)
                : string.IsNullOrWhiteSpace(node.LabelColor) ? ForegroundPaint(color, HighlightFactor(active))
                : SourcePaint(node.LabelColor, color, SvgColorRole.Any, HighlightFactor(active), _colors.Foreground);
            _builder.InlineText(plan.Content, Bounds(plan.Label.X, plan.Label.Y, plan.Label.Width, plan.Label.Height), style, plan.Wrap,
                plan.MaximumLines, "topology-node-label", node.Id + "-label", paint);
        }
        if (plan.SubtitleTop.HasValue) BuildSubtitleChip(node, TopologyNodeDisplayMode.Tile, plan.SubtitleTop.Value, accent, active);
    }
}
