using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    internal static ChartPoint ResolveExplicitLabelCenter(TopologyChart chart, TopologyRenderOptions options, TopologyEdge edge,
        TopologyLabelPosition position, string label, string secondary, string tertiary, double width, double height, int lineCount, double outputScale = 1) {
        if (position.VerticalAnchor == TopologyLabelVerticalAnchor.Center) return position.Point;
        // The first painted row may be a secondary/tertiary label. Reuse the same row
        // grouping, role sizes, actual resolved face and text plan as the native painter.
        var value = !string.IsNullOrWhiteSpace(label) ? label : !string.IsNullOrWhiteSpace(secondary) ? secondary : tertiary;
        var rows = EdgeLabelRows(value, options); var allocation = rows * height / System.Math.Max(1, lineCount);
        var font = options.TextMeasurement?.Font ?? chart.Theme.Typography.ToFontSpec();
        var units = TopologyPreparedEdgePlan.Units(edge);
        var size = edge.TextStyle?.FontSize ?? chart.Theme.Typography.DataLabelSize * units;
        var content = EdgeCaptionContent(edge, value);
        if (content.IsStructured) {
            var inlineStyle = TopologyTextPolicy.ResolveStructured(edge.TextStyle, font, size); inlineStyle.FontSize = size * outputScale;
            content = TopologyTextLayout.DisplayRows(content, options.AllowMultilineNodeLabels).Transform(inlineStyle.TextCase);
            var metrics = TextLayoutEngine.Measure(content, inlineStyle);
            var maximum = System.Math.Min(System.Math.Max(1, rows), (int)System.Math.Floor(allocation * outputScale / metrics.LineHeight));
            if (maximum < 1) return position.Point;
            var captured = new VisualSceneInlineTextContext(inlineStyle);
            var layout = TextLayoutEngine.LayoutCaptured(content, System.Math.Max(0, width - 8 * units) * outputScale,
                captured, options.WrapNodeLabels ? TextWrapMode.Word : TextWrapMode.NoWrap, maximum);
            if (layout.Lines.Count == 0) return position.Point;
            var first = captured.Prepare(layout.Lines[0].Content);
            var inlineBaseline = (VisualSceneInlineTextContext.Baseline(layout, first, 0, 0, allocation * outputScale, true)
                + VisualSceneTextFace.BaselineOffset(inlineStyle)) / outputScale;
            return new ChartPoint(position.Point.X, position.Point.Y + height / 2 - inlineBaseline);
        }
        var style = TopologyTextPolicy.Resolve(edge.TextStyle, font, size, 600); style.FontSize = size * outputScale;
        var face = new VisualSceneTextFace(style);
        var plan = TopologyTextLayout.Prepare(value, System.Math.Max(0, width - 8 * units) * outputScale, allocation * outputScale, size * outputScale, 600, rows,
            false, DotNodeSymbolFontSize, options, (text, candidate, _) => face.Prepare(text, candidate).Metrics);
        if (plan == null) return position.Point; // The existing painter reports an allocation without a complete line.
        style.FontSize = plan.Size;
        var baseline = (plan.FirstBaseline(0, allocation * outputScale, true, face.Ascent(plan.Size)) + VisualSceneTextFace.BaselineOffset(style)) / outputScale;
        return new ChartPoint(position.Point.X, position.Point.Y + height / 2 - baseline);
    }
}
