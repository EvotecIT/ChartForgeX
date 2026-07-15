using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

/// <summary>
/// Measures and wraps text with the dependency-free raster font engine used by ChartForgeX.
/// </summary>
public static class TextLayoutEngine {
    /// <summary>Measures text without wrapping.</summary>
    public static TextMetrics Measure(string text, TextStyle style) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (style == null) throw new ArgumentNullException(nameof(style));
        var font = TypographyFontResolver.Resolve(style.Font);
        var lineHeight = ResolveLineHeight(style, font);
        var lines = SplitParagraphs(text);
        var width = 0d;
        for (var i = 0; i < lines.Count; i++) width = Math.Max(width, MeasureWidth(lines[i], style, font));
        return new TextMetrics(width, Math.Max(1, lines.Count) * lineHeight, lineHeight);
    }

    /// <summary>Wraps and measures text inside a fixed-width region.</summary>
    public static TextLayout Layout(string text, double maximumWidth, TextStyle style, TextWrapMode wrapMode = TextWrapMode.Word, int? maximumLines = null, TextTrimming trimming = TextTrimming.Ellipsis) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (style == null) throw new ArgumentNullException(nameof(style));
        if (!IsFinite(maximumWidth) || maximumWidth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumWidth), maximumWidth, "Maximum width must be finite and greater than zero.");
        if (maximumLines <= 0) throw new ArgumentOutOfRangeException(nameof(maximumLines), maximumLines, "Maximum lines must be greater than zero.");
        if (!Enum.IsDefined(typeof(TextWrapMode), wrapMode)) throw new ArgumentOutOfRangeException(nameof(wrapMode), wrapMode, "Unknown text wrap mode.");
        if (!Enum.IsDefined(typeof(TextTrimming), trimming)) throw new ArgumentOutOfRangeException(nameof(trimming), trimming, "Unknown text trimming mode.");

        var font = TypographyFontResolver.Resolve(style.Font);
        var resolved = new List<TextLayoutLine>();
        var trimmed = false;
        foreach (var paragraph in SplitParagraphs(text)) {
            var paragraphLines = WrapParagraph(paragraph, maximumWidth, style, font, wrapMode);
            for (var i = 0; i < paragraphLines.Count; i++) {
                if (maximumLines.HasValue && resolved.Count >= maximumLines.Value) {
                    trimmed = true;
                    break;
                }

                resolved.Add(paragraphLines[i]);
            }

            if (trimmed) break;
        }

        if (resolved.Count == 0) resolved.Add(new TextLayoutLine(string.Empty, 0));
        if (trimmed && trimming == TextTrimming.Ellipsis) {
            var last = resolved.Count - 1;
            resolved[last] = Ellipsize(resolved[last].Text, maximumWidth, style, font);
        }

        var width = 0d;
        for (var i = 0; i < resolved.Count; i++) width = Math.Max(width, resolved[i].Width);
        var lineHeight = ResolveLineHeight(style, font);
        return new TextLayout(resolved, new TextMetrics(width, resolved.Count * lineHeight, lineHeight), trimmed);
    }

    internal static double MeasureWidth(string text, TextStyle style, TrueTypeFont? font) {
        var width = RgbaCanvas.MeasureTextWidth(text, style.FontSize, font);
        if (style.Font.Weight >= 600 && text.Length > 0) width += Math.Max(0.6, style.FontSize / 18.0);
        return width;
    }

    internal static double ResolveLineHeight(TextStyle style, TrueTypeFont? font) => Math.Max(1, RgbaCanvas.MeasureTextHeight(style.FontSize, font) * style.LineHeight);

    private static List<TextLayoutLine> WrapParagraph(string paragraph, double maximumWidth, TextStyle style, TrueTypeFont? font, TextWrapMode wrapMode) {
        if (paragraph.Length == 0) return new List<TextLayoutLine> { new(string.Empty, 0) };
        if (wrapMode == TextWrapMode.NoWrap) return new List<TextLayoutLine> { new(paragraph, MeasureWidth(paragraph, style, font)) };
        if (wrapMode == TextWrapMode.Character) return WrapCharacters(paragraph, maximumWidth, style, font);

        var output = new List<TextLayoutLine>();
        var words = paragraph.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var current = string.Empty;
        for (var i = 0; i < words.Length; i++) {
            var candidate = current.Length == 0 ? words[i] : current + " " + words[i];
            var candidateWidth = MeasureWidth(candidate, style, font);
            if (candidateWidth <= maximumWidth) {
                current = candidate;
                continue;
            }

            if (current.Length > 0) {
                output.Add(new TextLayoutLine(current, MeasureWidth(current, style, font)));
                current = string.Empty;
            }

            var wordWidth = MeasureWidth(words[i], style, font);
            if (wordWidth <= maximumWidth) current = words[i];
            else {
                var pieces = WrapCharacters(words[i], maximumWidth, style, font);
                for (var piece = 0; piece + 1 < pieces.Count; piece++) output.Add(pieces[piece]);
                current = pieces[pieces.Count - 1].Text;
            }
        }

        if (current.Length > 0) output.Add(new TextLayoutLine(current, MeasureWidth(current, style, font)));
        return output;
    }

    private static List<TextLayoutLine> WrapCharacters(string text, double maximumWidth, TextStyle style, TrueTypeFont? font) {
        var output = new List<TextLayoutLine>();
        var start = 0;
        while (start < text.Length) {
            var length = 1;
            var bestLength = 1;
            while (start + length <= text.Length) {
                var candidate = text.Substring(start, length);
                if (MeasureWidth(candidate, style, font) > maximumWidth) break;
                bestLength = length;
                length++;
            }

            var line = text.Substring(start, bestLength);
            output.Add(new TextLayoutLine(line, MeasureWidth(line, style, font)));
            start += bestLength;
        }

        return output;
    }

    private static TextLayoutLine Ellipsize(string text, double maximumWidth, TextStyle style, TrueTypeFont? font) {
        const string ellipsis = "…";
        if (MeasureWidth(ellipsis, style, font) > maximumWidth) return new TextLayoutLine(string.Empty, 0);
        var candidate = text.TrimEnd();
        while (candidate.Length > 0 && MeasureWidth(candidate + ellipsis, style, font) > maximumWidth) candidate = candidate.Substring(0, candidate.Length - 1).TrimEnd();
        var result = candidate + ellipsis;
        return new TextLayoutLine(result, MeasureWidth(result, style, font));
    }

    private static List<string> SplitParagraphs(string text) => new(text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
