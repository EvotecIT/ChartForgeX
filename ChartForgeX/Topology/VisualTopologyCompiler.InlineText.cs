using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private static TextContent NodeCaptionContent(TopologyNode node) {
        var content = node.LabelContent;
        return node.MaximumLabelCharacters is int maximum && content.PlainText.Length > maximum
            ? content.Slice(0, TextElementBoundary.Snap(content.PlainText, maximum)) : content;
    }
    private static TextContent DetailContent(TopologyNodeDetail detail) => detail.Content
        ?? TextContent.Join(TextContent.Plain(detail.Label + " "), detail.ValueContent);
    private TextStyle InlineStyle(double size, ChartColor color, bool centered = false, TextStyleOverride? detailPolicy = null) {
        var style = TopologyTextPolicy.ResolveStructured(_textPolicy, _context.Font, size);
        if (detailPolicy != null) style = detailPolicy.Resolve(style);
        // As with C4's plain resolver, fitted logical size is already selected by the caller.
        style.FontSize = detailPolicy?.FontSize is double authored ? authored * _scale : size;
        style.Color = color; style.Alignment = centered ? TextAlignment.Center : TextAlignment.Left;
        return style;
    }
    private TextContent DisplayRows(TextContent content) => TopologyTextLayout.DisplayRows(content, _options.AllowMultilineNodeLabels);
    private ChartRect? Text(TextContent content, ChartRect bounds, double size, ChartColor color, int weight,
        string role, int maxLines = 1, bool centered = false, string? id = null, SvgPaint? paint = null,
        double opacity = 1, bool fitSingleLine = false, bool verticallyCentered = false, double defaultUnits = 1) {
        if (!content.IsStructured) return Text(content.PlainText, bounds, size, color, weight, role, maxLines,
            centered, id, paint, opacity, fitSingleLine, verticallyCentered, defaultUnits: defaultUnits);
        return InlineText(content, bounds, size * _scale, color, role, maxLines, centered, id, paint ?? ForegroundPaint(color, opacity), verticallyCentered, fitSingleLine, defaultUnits: defaultUnits);
    }
    private ChartRect? InlineText(TextContent content, ChartRect bounds, double size, ChartColor color, string role,
        int maxLines, bool centered = false, string? id = null, SvgPaint? paint = null, bool verticallyCentered = false,
        bool fitSingleLine = false, TextStyleOverride? detailPolicy = null, double defaultUnits = 1) {
        if (bounds.Width <= 0 || bounds.Height <= 0 || content.PlainText.Length == 0) return null;
        content = DisplayRows(content); var style = InlineStyle(size, color, centered, detailPolicy);
        var wrap = !fitSingleLine && _options.WrapNodeLabels ? TextWrapMode.Word : TextWrapMode.NoWrap;
        if (fitSingleLine) {
            size = ChartTextFitting.FitFontSize(Math.Min(size, DotNodeSymbolFontSize * defaultUnits * _scale), size, candidate => {
                var probe = style.Clone(); probe.FontSize = candidate;
                var measured = TextLayoutEngine.Measure(content, probe); return measured.Width <= bounds.Width && measured.Height <= bounds.Height;
            });
            style.FontSize = size;
        }
        var metrics = TextLayoutEngine.Measure(content, style);
        var rows = Math.Min(Math.Max(1, maxLines), (int)Math.Floor(bounds.Height / metrics.LineHeight));
        if (rows < 1) { _builder.AddDiagnostic(new VisualDiagnostic("topology.label-height", "A structured caption has no complete native line in its allocation; full content remains semantic data.")); return null; }
        var layout = TextLayoutEngine.Layout(content, bounds.Width, style, wrap, rows);
        _builder.InlineText(content, bounds, style, wrap, rows, role, id, paint ?? ForegroundPaint(color), verticallyCentered);
        return new ChartRect(bounds.X + (centered ? (bounds.Width - layout.Metrics.Width) / 2 : 0),
            bounds.Y + (verticallyCentered ? Math.Max(0, bounds.Height - layout.Metrics.Height) / 2 : 0), layout.Metrics.Width, layout.Metrics.Height);
    }
    private double CaptionLineHeight(TextContent content, double size, TextStyleOverride? detailPolicy = null) => content.IsStructured
        ? TextLayoutEngine.Measure(content, InlineStyle(size, _colors.Foreground, detailPolicy: detailPolicy)).LineHeight
        : detailPolicy?.HasOverrides == true ? MeasureRoleText("Ag", RoleTextOutputSize(size, detailPolicy), 400, detailPolicy).LineHeight : MeasurePolicyText("Ag", size, 600).LineHeight;
    private System.Collections.Generic.List<string> CaptionLines(TextContent content, double width, double size, int weight,
        int maximum, out bool overflow, TextStyleOverride? detailPolicy = null) {
        if (!content.IsStructured) return FitTextLines(content.PlainText, width, size, weight, maximum, false, false, out overflow, detailPolicy);
        if (width <= 0) { overflow = content.PlainText.Length > 0; return new System.Collections.Generic.List<string>(); }
        var layout = TextLayoutEngine.Layout(DisplayRows(content), width, InlineStyle(size, _colors.Foreground, detailPolicy: detailPolicy),
            _options.WrapNodeLabels ? TextWrapMode.Word : TextWrapMode.NoWrap, Math.Max(1, maximum));
        overflow = layout.Trimmed; var lines = new System.Collections.Generic.List<string>();
        foreach (var line in layout.Lines) lines.Add(line.Text); return lines;
    }
    private int CaptionRowCount(TextContent content, double width, double size, int maximum) {
        if (!content.IsStructured) return TextLineCount(content.PlainText, width, size / _scale, 600, maximum);
        return CaptionLines(content, width, size, 400, maximum, out _).Count;
    }
    private double CaptionNaturalWidth(TextContent content, double size, TextStyleOverride? detailPolicy = null) => content.IsStructured
        ? TextLayoutEngine.Measure(DisplayRows(content), InlineStyle(size, _colors.Foreground, detailPolicy: detailPolicy)).Width
        : detailPolicy?.HasOverrides == true ? MeasureRoleText(content.PlainText, RoleTextSize(size, detailPolicy), 400, detailPolicy).Width : MeasurePolicyText(content.PlainText, size, 600).Width;
    private double FitInlineNodeTextSize(TopologyNode node, ChartRect bounds, double preferred, bool caption) =>
        ChartTextFitting.FitFontSize(Math.Min(8, preferred), preferred, size => {
            var content = NodeCaptionContent(node);
            var height = CaptionLineHeight(content, size * _scale) * CaptionRowCount(content, bounds.Width, size * _scale, _options.MaxNodeLabelLines);
            if (!string.IsNullOrWhiteSpace(node.Subtitle) && (!caption || _options.IncludeTileSubtitles))
                height += MeasureRoleText("Ag", RoleTextSize(size * .85, node.SubtitleTextStyle) * _scale, 400, node.SubtitleTextStyle).LineHeight
                    * TextLineCount(node.Subtitle!, bounds.Width, size * .85, 400, _options.MaxNodeSubtitleLines, node.SubtitleTextStyle);
            if (node.Details.Count == 0) return height <= bounds.Height;
            var first = Point(new ChartPoint(node.X, node.Y + NodeDetailStartOffset(node, _options))).Y;
            var top = Math.Max(bounds.Top + height + 12 * _scale, first - PolicyTextAscent(size * .8 * _scale, 400));
            foreach (var detail in node.Details) {
                var rows = TopologyTextLayout.AuthoredRowCount(NodeDetailText(detail), _options.AllowMultilineNodeLabels);
                var row = DetailContent(detail).IsStructured || detail.TextStyle?.HasOverrides == true ? CaptionLineHeight(DetailContent(detail), size * .8 * _scale, detail.TextStyle)
                    : MeasurePolicyText("Ag", size * .8 * _scale, 400).LineHeight;
                top += detail.BadgeStyle == null ? rows * Math.Max(size * _scale * 1.4, row)
                    : DetailRowHeight(detail, size * .8, Math.Max(size * 1.4, row / _scale)) * _scale;
            }
            return top <= bounds.Bottom;
        });
    private ChartRect ShapeCaptionBounds(TopologyNode node, TextContent content, double size) {
        if (!content.IsStructured) return ShapeCaptionBounds(node, content.PlainText, size);
        var style = InlineStyle(size * _scale, _colors.Foreground); var lineHeight = TextLayoutEngine.Measure(content, style).LineHeight / _scale;
        var best = TopologyNodeShapeGeometry.CaptionBounds(node, lineHeight); var retained = -1;
        var maximum = Math.Min(Math.Max(1, _options.MaxNodeLabelLines), Math.Max(1, (int)Math.Floor(node.Height / lineHeight)));
        for (var rows = 1; rows <= maximum; rows++) {
            var bounds = TopologyNodeShapeGeometry.CaptionBounds(node, rows * lineHeight + 1e-8);
            if (bounds.Width <= 0 || bounds.Height + 1e-8 < rows * lineHeight) break;
            var layout = TextLayoutEngine.Layout(content, bounds.Width * _scale, style,
                _options.WrapNodeLabels ? TextWrapMode.Word : TextWrapMode.NoWrap, rows);
            if (!layout.Trimmed && layout.Metrics.Height <= bounds.Height * _scale + 1e-8) return bounds;
            var count = 0; foreach (var line in layout.Lines) count += line.Text.Length;
            if (count > retained) { retained = count; best = bounds; }
        }
        return best;
    }
    private double FitMeasuredNodeLabelSize(TextContent content, double preferred, Func<double, ChartRect> contentBounds) {
        if (!content.IsStructured) return FitMeasuredNodeLabelSize(content.PlainText, preferred, contentBounds);
        return ChartTextFitting.FitFontSize(Math.Min(11, preferred), preferred, candidate => {
            var bounds = contentBounds(candidate); if (bounds.Width <= 0 || bounds.Height <= 0) return false;
            var style = InlineStyle(candidate * _scale, _colors.Foreground);
            var height = TextLayoutEngine.Measure(content, style).LineHeight;
            var rows = Math.Min(Math.Max(1, _options.MaxNodeLabelLines), (int)Math.Floor(bounds.Height / height));
            if (rows < 1) return false;
            return !TextLayoutEngine.Layout(content, bounds.Width, style,
                _options.WrapNodeLabels ? TextWrapMode.Word : TextWrapMode.NoWrap, rows).Trimmed;
        });
    }
}
