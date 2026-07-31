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
        var width = 0d;
        var lineCount = 0;
        foreach (var line in TextLineScanner.Enumerate(text)) {
            width = Math.Max(width, MeasureWidth(line.Read(text), style, font));
            lineCount++;
        }
        return new TextMetrics(width, Math.Max(1, lineCount) * lineHeight, lineHeight);
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
        foreach (var paragraphSlice in TextLineScanner.Enumerate(text)) {
            var remainingLines = maximumLines.HasValue
                ? Math.Max(0, maximumLines.Value - resolved.Count)
                : (int?)null;
            var paragraphLines = WrapParagraph(
                paragraphSlice.Read(text),
                maximumWidth,
                style,
                font,
                wrapMode,
                remainingLines,
                out var paragraphTrimmed);
            for (var i = 0; i < paragraphLines.Count; i++) {
                resolved.Add(paragraphLines[i]);
            }

            if (paragraphTrimmed) {
                trimmed = true;
                break;
            }
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

    private static List<TextLayoutLine> WrapParagraph(
        string paragraph,
        double maximumWidth,
        TextStyle style,
        TrueTypeFont? font,
        TextWrapMode wrapMode,
        int? maximumLines,
        out bool trimmed) {
        trimmed = false;
        if (maximumLines == 0) {
            trimmed = true;
            return new List<TextLayoutLine>();
        }
        if (paragraph.Length == 0) return new List<TextLayoutLine> { new(string.Empty, 0) };
        if (wrapMode == TextWrapMode.NoWrap) return new List<TextLayoutLine> { new(paragraph, MeasureWidth(paragraph, style, font)) };
        if (wrapMode == TextWrapMode.Character) {
            return WrapCharacters(
                paragraph,
                maximumWidth,
                style,
                font,
                maximumLines,
                out trimmed);
        }

        var output = new List<TextLayoutLine>();
        var current = string.Empty;
        var cursor = 0;
        while (cursor < paragraph.Length) {
            while (cursor < paragraph.Length && (paragraph[cursor] == ' ' || paragraph[cursor] == '\t')) cursor++;
            if (cursor >= paragraph.Length) break;
            var wordStart = cursor;
            while (cursor < paragraph.Length && paragraph[cursor] != ' ' && paragraph[cursor] != '\t') cursor++;
            var word = paragraph.Substring(wordStart, cursor - wordStart);
            var candidate = current.Length == 0 ? word : current + " " + word;
            var candidateWidth = MeasureWidth(candidate, style, font);
            if (candidateWidth <= maximumWidth) {
                current = candidate;
                continue;
            }

            if (current.Length > 0) {
                output.Add(new TextLayoutLine(current, MeasureWidth(current, style, font)));
                current = string.Empty;
                if (maximumLines.HasValue && output.Count >= maximumLines.Value) {
                    trimmed = true;
                    break;
                }
            }

            var wordWidth = MeasureWidth(word, style, font);
            if (wordWidth <= maximumWidth) current = word;
            else {
                var availableLines = maximumLines.HasValue
                    ? Math.Max(0, maximumLines.Value - output.Count)
                    : (int?)null;
                var pieces = WrapCharacters(
                    word,
                    maximumWidth,
                    style,
                    font,
                    availableLines,
                    out var wordTrimmed);
                if (wordTrimmed) {
                    output.AddRange(pieces);
                    trimmed = true;
                    break;
                }
                for (var piece = 0; piece + 1 < pieces.Count; piece++) output.Add(pieces[piece]);
                current = pieces[pieces.Count - 1].Text;
            }
        }

        if (!trimmed && current.Length > 0) {
            if (maximumLines.HasValue && output.Count >= maximumLines.Value) {
                trimmed = true;
            } else {
                output.Add(new TextLayoutLine(current, MeasureWidth(current, style, font)));
            }
        }
        return output;
    }

    private static List<TextLayoutLine> WrapCharacters(
        string text,
        double maximumWidth,
        TextStyle style,
        TrueTypeFont? font,
        int? maximumLines,
        out bool trimmed) {
        var output = new List<TextLayoutLine>();
        var start = 0;
        while (start < text.Length) {
            if (maximumLines.HasValue && output.Count >= maximumLines.Value) break;
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

        trimmed = start < text.Length;
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

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
