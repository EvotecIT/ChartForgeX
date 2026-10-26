using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildDetailText(TopologyNodeDetail detail, ChartRect bounds, double size, bool active) {
        var value = NodeDetailText(detail);
        var content = DetailContent(detail);
        if (content.IsStructured) {
            var inlineRows = TopologyTextLayout.AuthoredRowCount(content.PlainText, _options.AllowMultilineNodeLabels);
            var inlineAuthored = detail.TextStyle?.Color is ChartColor explicitColor ? explicitColor.ToHexRgba() : detail.Color;
            var inlineColor = Highlight(detail.TextStyle?.Color ?? Color(detail.Color, _colors.Foreground), active);
            InlineText(content, bounds, size * _scale, inlineColor, "topology-node-detail", inlineRows,
                paint: SourcePaint(inlineAuthored ?? _svgTheme?.Foreground, inlineColor, string.IsNullOrWhiteSpace(inlineAuthored) ? SvgColorRole.Text : SvgColorRole.Any, HighlightFactor(active), _colors.Foreground), detailPolicy: detail.TextStyle);
            return;
        }
        var rows = TopologyTextLayout.AuthoredRowCount(value, _options.AllowMultilineNodeLabels);
        var color = Color(detail.Color, _colors.Foreground);
        if (detail.TextStyle == null || !detail.TextStyle.HasOverrides) {
            Text(value, bounds, size, Highlight(color, active), 400, "topology-node-detail", rows,
                paint: SourcePaint(detail.Color ?? _svgTheme?.Foreground, Highlight(color, active), string.IsNullOrWhiteSpace(detail.Color) ? SvgColorRole.Text : SvgColorRole.Any, HighlightFactor(active), _colors.Foreground));
            return;
        }

        var policy = TopologyTextPolicy.Resolve(_textPolicy, _context.Font, size, 400);
        var style = detail.TextStyle.Resolve(new TextStyle { Font = policy.Font, FontSize = size, Color = color, LineHeight = 1,
            LetterSpacing = policy.LetterSpacing });
        style.FontSize *= _scale; style.LetterSpacing *= _scale;
        style.Color = Highlight(style.Color, active);
        TextMetrics Measure(string text, double candidateSize, int weight) {
            var candidate = style.Clone();
            candidate.FontSize = candidateSize;
            candidate.Font.Weight = weight;
            return _builder.MeasureText(text, candidate);
        }
        var layout = TopologyTextLayout.Prepare(value, bounds.Width, bounds.Height, style.FontSize, style.Font.Weight, rows,
            false, DotNodeSymbolFontSize * _scale, _options, Measure,
            (code, message) => _builder.AddDiagnostic(new VisualDiagnostic(code, message)));
        if (layout == null) return;
        style.FontSize = layout.Size;
        var baseline = bounds.Y + _builder.TextAscent(style);
        var authored = detail.TextStyle.Color.HasValue ? detail.TextStyle.Color.Value.ToHexRgba() : detail.Color;
        var paint = SourcePaint(authored ?? _svgTheme?.Foreground, style.Color,
            string.IsNullOrWhiteSpace(authored) ? SvgColorRole.Text : SvgColorRole.Any, HighlightFactor(active), _colors.Foreground);
        foreach (var line in layout.Lines) {
            if (line.Length > 0) _builder.Text(line, bounds.X, baseline, style, "topology-node-detail", paint: paint);
            baseline += layout.LineHeight;
        }
    }
}
