using System;
using System.Collections.Generic;
using System.Globalization;
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
        text = TextCaseTransformer.Apply(text, style.TextCase, CultureInfo.InvariantCulture);
        var font = TypographyFontResolver.ResolveFace(style.Font);
        var lineHeight = ResolveLineHeight(style, font.Font);
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

        text = TextCaseTransformer.Apply(text, style.TextCase, CultureInfo.InvariantCulture);
        var font = TypographyFontResolver.ResolveFace(style.Font);
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
        var lineHeight = ResolveLineHeight(style, font.Font);
        return new TextLayout(resolved, new TextMetrics(width, resolved.Count * lineHeight, lineHeight), trimmed);
    }

    /// <summary>Measures with a face chosen elsewhere; bold and italic requested by the style are synthesized on it.</summary>
    internal static double MeasureWidth(string text, TextStyle style, TrueTypeFont? font) =>
        MeasureWidth(text, style, new ResolvedTypeface(font, style.Font.Weight >= 600, style.Font.Italic));

    internal static double MeasureWidth(string text, TextStyle style, ResolvedTypeface face) {
        var width = RgbaCanvas.MeasureTextWidth(text, style.EffectiveFontSize, face.Font, face.SynthesizeItalic);
        // A real italic face leans past its last advance just as a sheared one does; reserving the same
        // overhang keeps layout independent of which faces the host has installed.
        if (style.Font.Italic && !face.SynthesizeItalic && text.Length > 0) width += TrueTypeFont.ItalicOverhang(style.EffectiveFontSize);
        if (face.SynthesizeBold && text.Length > 0) width += Math.Max(0.6, style.EffectiveFontSize / 18.0);
        return width;
    }

    internal static double ResolveLineHeight(TextStyle style, TrueTypeFont? font) {
        var height = RgbaCanvas.MeasureTextHeight(style.EffectiveFontSize, font);
        if (style.UnderlineStyle != TextDecorationStyle.None) {
            var thickness = Math.Max(1, style.EffectiveFontSize / 13.0);
            height = Math.Max(height, style.EffectiveFontSize + 2 + TextDecorationMetrics.OuterExtent(style.UnderlineStyle, thickness));
        }
        return Math.Max(1, height * style.LineHeight);
    }

    private static List<TextLayoutLine> WrapParagraph(
        string paragraph,
        double maximumWidth,
        TextStyle style,
        ResolvedTypeface font,
        TextWrapMode wrapMode,
        int? maximumLines,
        out bool trimmed) {
        trimmed = false;
        if (maximumLines == 0) {
            trimmed = true;
            return new List<TextLayoutLine>();
        }
        if (paragraph.Length == 0) return new List<TextLayoutLine> { new(string.Empty, 0) };
        if (wrapMode == TextWrapMode.NoWrap) {
            // A line wider than the region is trimmed like a wrapped one past its last line: Ellipsis ends it with a
            // marker, None clips it. Before, the full width was returned and Ellipsis never applied.
            var width = MeasureWidth(paragraph, style, font);
            trimmed = width > maximumWidth;
            return new List<TextLayoutLine> { new(paragraph, width) };
        }
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
        ResolvedTypeface font,
        int? maximumLines,
        out bool trimmed) {
        var output = new List<TextLayoutLine>();
        var start = 0;
        while (start < text.Length) {
            if (maximumLines.HasValue && output.Count >= maximumLines.Value) break;
            // Lines break between whole characters: a surrogate pair or a base and its marks stay together.
            var end = TextElementBoundary.Next(text, start);
            var bestEnd = end;
            while (end <= text.Length) {
                var candidate = text.Substring(start, end - start);
                if (MeasureWidth(candidate, style, font) > maximumWidth) break;
                bestEnd = end;
                if (end == text.Length) break;
                end = TextElementBoundary.Next(text, end);
            }

            var line = text.Substring(start, bestEnd - start);
            output.Add(new TextLayoutLine(line, MeasureWidth(line, style, font)));
            start = bestEnd;
        }

        trimmed = start < text.Length;
        return output;
    }

    private static TextLayoutLine Ellipsize(string text, double maximumWidth, TextStyle style, ResolvedTypeface font) {
        const string ellipsis = "…";
        if (MeasureWidth(ellipsis, style, font) > maximumWidth) return new TextLayoutLine(string.Empty, 0);
        var candidate = text.TrimEnd();
        while (candidate.Length > 0 && MeasureWidth(candidate + ellipsis, style, font) > maximumWidth) candidate = candidate.Substring(0, TextElementBoundary.Snap(candidate, candidate.Length - 1)).TrimEnd();
        var result = candidate + ellipsis;
        return new TextLayoutLine(result, MeasureWidth(result, style, font));
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
