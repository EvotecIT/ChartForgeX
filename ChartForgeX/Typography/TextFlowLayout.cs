using System;
using System.Collections.Generic;

namespace ChartForgeX.Typography;

/// <summary>One measured word/grapheme wrapping policy for plain and structured native content.</summary>
internal static class TextFlowLayout {
    internal static TextLayout Layout(TextContent content, double maximumWidth, Func<TextContent, double> measure,
        double lineHeight, TextWrapMode wrap, int? maximumLines, TextTrimming trimming) {
        var output = new List<TextLayoutLine>(); var trimmed = false; var omitted = false; var ellipsized = false;
        foreach (var slice in TextLineScanner.Enumerate(content.PlainText)) {
            var remaining = maximumLines.HasValue ? Math.Max(0, maximumLines.Value - output.Count) : (int?)null;
            var lines = Paragraph(content.Slice(slice.Start, slice.Length), maximumWidth, measure, wrap, remaining, out var lost);
            foreach (var value in lines) {
                var line = value; ellipsized = false;
                if (wrap == TextWrapMode.NoWrap && measure(line) > maximumWidth) {
                    trimmed = true;
                    if (trimming == TextTrimming.Ellipsis) { line = Ellipsis(line, maximumWidth, measure); ellipsized = true; }
                }
                output.Add(new TextLayoutLine(line, measure(line)));
            }
            if (lost) { trimmed = true; omitted = true; break; }
        }
        if (output.Count == 0) output.Add(new TextLayoutLine(content.WithLiteral(string.Empty), 0));
        if (omitted && trimming == TextTrimming.Ellipsis && !ellipsized) {
            var last = output.Count - 1; var line = Ellipsis(output[last].Content, maximumWidth, measure);
            output[last] = new TextLayoutLine(line, measure(line));
        }
        var width = 0d; foreach (var line in output) width = Math.Max(width, line.Width);
        return new TextLayout(Array.AsReadOnly(output.ToArray()), new TextMetrics(width, output.Count * lineHeight, lineHeight), trimmed);
    }

    private static List<TextContent> Paragraph(TextContent content, double width, Func<TextContent, double> measure,
        TextWrapMode wrap, int? remaining, out bool trimmed) {
        trimmed = false; var result = new List<TextContent>(); var text = content.PlainText;
        if (remaining == 0) { trimmed = true; return result; }
        if (text.Length == 0 || wrap == TextWrapMode.NoWrap) { result.Add(content); return result; }
        if (wrap == TextWrapMode.Character) return Characters(content, width, measure, remaining, out trimmed);
        var current = content.WithLiteral(string.Empty); var cursor = 0;
        while (cursor < text.Length) {
            while (cursor < text.Length && (text[cursor] == ' ' || text[cursor] == '\t')) cursor++;
            if (cursor >= text.Length) break;
            var start = cursor; while (cursor < text.Length && text[cursor] != ' ' && text[cursor] != '\t') cursor++;
            var word = content.Slice(start, cursor - start);
            var candidate = current.PlainText.Length == 0 ? word : TextContent.Join(TextContent.Join(current, current.WithLiteral(" ", trailing: true)), word);
            if (measure(candidate) <= width) { current = candidate; continue; }
            if (current.PlainText.Length > 0) {
                result.Add(current); current = content.WithLiteral(string.Empty);
                if (remaining.HasValue && result.Count >= remaining.Value) { trimmed = true; break; }
            }
            if (measure(word) <= width) current = word;
            else {
                var available = remaining.HasValue ? Math.Max(0, remaining.Value - result.Count) : (int?)null;
                var pieces = Characters(word, width, measure, available, out var lost);
                if (lost) { result.AddRange(pieces); trimmed = true; break; }
                for (var index = 0; index + 1 < pieces.Count; index++) result.Add(pieces[index]);
                current = pieces[pieces.Count - 1];
            }
        }
        if (!trimmed && current.PlainText.Length > 0) {
            if (remaining.HasValue && result.Count >= remaining.Value) trimmed = true;
            else result.Add(current);
        }
        return result;
    }

    private static List<TextContent> Characters(TextContent content, double width, Func<TextContent, double> measure,
        int? remaining, out bool trimmed) {
        var result = new List<TextContent>(); var text = content.PlainText; var start = 0;
        while (start < text.Length) {
            if (remaining.HasValue && result.Count >= remaining.Value) break;
            var end = TextElementBoundary.Next(text, start); var best = end;
            while (end <= text.Length) {
                if (measure(content.Slice(start, end - start)) > width) break;
                best = end; if (end == text.Length) break; end = TextElementBoundary.Next(text, end);
            }
            result.Add(content.Slice(start, best - start)); start = best;
        }
        trimmed = start < text.Length; return result;
    }

    private static TextContent Ellipsis(TextContent content, double width, Func<TextContent, double> measure) {
        var marker = content.WithLiteral("…", trailing: true);
        if (measure(marker) > width) return content.WithLiteral(string.Empty);
        var text = content.PlainText.TrimEnd(); var length = text.Length;
        while (length > 0 && measure(TextContent.Join(content.Slice(0, length), marker)) > width)
            length = content.PlainText.Substring(0, TextElementBoundary.Snap(content.PlainText, length - 1)).TrimEnd().Length;
        return TextContent.Join(content.Slice(0, length), marker);
    }
}
