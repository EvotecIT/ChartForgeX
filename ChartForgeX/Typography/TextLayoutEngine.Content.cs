using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.Typography;

public static partial class TextLayoutEngine {
    /// <summary>Measures safe inline content through the same native face/shaping owner used by scene preparation.</summary>
    public static TextMetrics Measure(TextContent content, TextStyle style) {
        if (content == null) throw new ArgumentNullException(nameof(content));
        if (style == null) throw new ArgumentNullException(nameof(style));
        if (!content.IsStructured) return Measure(content.PlainText, style);
        content = content.Transform(style.TextCase); var captured = new VisualSceneInlineTextContext(style);
        var width = 0d; var height = 0d; var lineHeight = 0d;
        foreach (var slice in TextLineScanner.Enumerate(content.PlainText)) {
            var line = captured.Prepare(content.Slice(slice.Start, slice.Length));
            width = Math.Max(width, line.Width); lineHeight = Math.Max(lineHeight, line.LineHeight); height += line.LineHeight;
        }
        return new TextMetrics(width, height, lineHeight);
    }
    /// <summary>Fits immutable styled content with the shared word/grapheme flow policy. No markup or browser measurement is evaluated.</summary>
    public static TextLayout Layout(TextContent content, double maximumWidth, TextStyle style,
        TextWrapMode wrapMode = TextWrapMode.Word, int? maximumLines = null, TextTrimming trimming = TextTrimming.Ellipsis) {
        if (content == null) throw new ArgumentNullException(nameof(content));
        if (style == null) throw new ArgumentNullException(nameof(style));
        if (!content.IsStructured) return Layout(content.PlainText, maximumWidth, style, wrapMode, maximumLines, trimming);
        if (!IsFinite(maximumWidth) || maximumWidth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumWidth));
        if (maximumLines <= 0) throw new ArgumentOutOfRangeException(nameof(maximumLines));
        if (!Enum.IsDefined(typeof(TextWrapMode), wrapMode)) throw new ArgumentOutOfRangeException(nameof(wrapMode));
        if (!Enum.IsDefined(typeof(TextTrimming), trimming)) throw new ArgumentOutOfRangeException(nameof(trimming));
        content = content.Transform(style.TextCase); var captured = new VisualSceneInlineTextContext(style);
        return LayoutCaptured(content, maximumWidth, captured, wrapMode, maximumLines, trimming);
    }

    internal static TextLayout LayoutCaptured(TextContent content, double maximumWidth, VisualSceneInlineTextContext captured,
        TextWrapMode wrapMode, int? maximumLines, TextTrimming trimming = TextTrimming.Ellipsis) {
        if (!IsFinite(maximumWidth) || maximumWidth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumWidth));
        var lineHeight = captured.Prepare(content.WithLiteral("Ag")).LineHeight;
        // Every real inline emphasis contributes to the row envelope, even when a short fitted row omits it.
        foreach (var run in content.Runs) lineHeight = Math.Max(lineHeight,
            captured.Prepare(TextContent.FromRuns(new[] { new TextContentRun("Ag", run.Emphasis) })).LineHeight);
        return TextFlowLayout.Layout(content, maximumWidth, value => captured.Prepare(value).Width, lineHeight, wrapMode, maximumLines, trimming);
    }
}
