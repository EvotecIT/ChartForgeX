using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    internal TextMetrics MeasureText(TextContent content, TextStyle style) => TextLayoutEngine.Measure(content, style);

    internal void InlineText(TextContent content, ChartRect bounds, TextStyle style, TextWrapMode wrap,
        int? maximumLines, string? role, string? id, SvgPaint? paint = null, bool verticallyCentered = false) {
        if (content == null) throw new ArgumentNullException(nameof(content));
        if (style == null) throw new ArgumentNullException(nameof(style));
        var transformed = content.Transform(style.TextCase); var resolved = style.Clone(); resolved.TextCase = TextCaseTransform.None;
        var captured = new VisualSceneInlineTextContext(resolved, _textCapture);
        var layout = TextLayoutEngine.LayoutCaptured(transformed, bounds.Width, captured, wrap, maximumLines);
        using (PushGroup(id, role, new Dictionary<string, string> { ["aria-label"] = content.PlainText, ["data-cfx-caption"] = content.PlainText })) {
            for (var row = 0; row < layout.Lines.Count; row++) {
                var line = captured.Prepare(layout.Lines[row].Content);
                var left = bounds.X + (style.Alignment == TextAlignment.Center ? (bounds.Width - line.Width) / 2
                    : style.Alignment == TextAlignment.Right ? bounds.Width - line.Width : 0);
                var baseline = VisualSceneInlineTextContext.Baseline(layout, line, row, bounds.Y, bounds.Height, verticallyCentered);
                baseline += style.Baseline == TextBaseline.Superscript ? -style.EffectiveFontSize * .35
                    : style.Baseline == TextBaseline.Subscript ? style.EffectiveFontSize * .22 : 0;
                foreach (var piece in line.Pieces) {
                    if (piece.Text.Face.Font == null) AddDiagnostic(new VisualDiagnostic("text.inline-builtin-font",
                        "This structured caption uses captured built-in TinyFont ink. Its existing character repertoire and lack of contextual-script shaping remain explicit."));
                    var prepared = piece.Text;
                    PreparedText(prepared, left + piece.Offset, baseline, style.Color, TextAlignment.Left,
                        "inline-text-run", null, paint, includeDecorations: true);
                }
            }
        }
        if (layout.Trimmed) AddDiagnostic(new VisualDiagnostic("text.inline-truncated", "Inline caption text was shortened to fit its native allocation; the complete semantic text is retained."));
    }
}
