using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

/// <summary>Prepares the measured topology text shared by native painting and layout obstacles.</summary>
internal sealed partial class TopologyTextLayout {
    private TopologyTextLayout(double size, double lineHeight, List<string> lines, double width) {
        Size = size; LineHeight = lineHeight; Lines = lines; Width = width;
    }
    internal double Size { get; }
    internal double LineHeight { get; }
    internal List<string> Lines { get; }
    internal double Width { get; }
    internal double Height => LineHeight * Lines.Count;

    /// <summary>Reserves the same complete native sibling faces used by fitted external captions.</summary>
    internal static double StructuredCaptionRowHeight(TopologyNode node, TextMeasurementContext? measurement) {
        var style = TopologyTextPolicy.ResolveStructured(node.TextStyle,
            measurement?.Font ?? FontSpec.FromFamily(TopologyPaint.Default.FontFamily), 11);
        style.FontSize = 11;
        return Math.Max(14, TextLayoutEngine.Measure(node.LabelContent, style).LineHeight);
    }

    /// <summary>Normalizes authored line endings once; disabling multiline joins those rows into one literal caption.</summary>
    /// <summary>Joins authored rows while retaining immutable inline styles and literal identity.</summary>
    internal static TextContent DisplayRows(TextContent content, bool allowMultiline) {
        if (allowMultiline) return content;
        var result = content.WithLiteral(string.Empty); var first = true;
        foreach (var slice in TextLineScanner.Enumerate(content.PlainText)) {
            if (!first) result = TextContent.Join(result, result.WithLiteral(" ", trailing: true));
            result = TextContent.Join(result, content.Slice(slice.Start, slice.Length)); first = false;
        }
        return result;
    }

    internal static string[] AuthoredRows(string value, bool allowMultiline) {
        var source = value.Replace("\r\n", "\n").Replace('\r', '\n');
        return allowMultiline ? source.Split('\n') : new[] { source.Replace('\n', ' ') };
    }

    /// <summary>Counts explicit rows, preserving blank-line space without inferring wrapping.</summary>
    internal static int AuthoredRowCount(string? value, bool allowMultiline) {
        if (!allowMultiline || value == null) return 1;
        var rows = 1;
        for (var index = 0; index < value.Length; index++) {
            if (value[index] == '\r') { rows++; if (index + 1 < value.Length && value[index + 1] == '\n') index++; }
            else if (value[index] == '\n') rows++;
        }
        return rows;
    }

    internal static TopologyTextLayout? Prepare(string value, double width, double height, double size, int weight,
        int maxLines, bool fitSingleLine, double minimumSize, TopologyRenderOptions options,
        Func<string, double, int, TextMetrics> measure, Action<string, string>? report = null) {
        if (string.IsNullOrEmpty(value) || width <= 0 || height <= 0) return null;
        if (fitSingleLine) {
            var preferred = measure(value, size, weight);
            if (preferred.Width > width || preferred.LineHeight > height) {
                var lower = Math.Min(size, minimumSize); var upper = size;
                // Bitmap advances can be discrete. Keep the existing readable floor and measured search.
                for (var attempt = 0; attempt < 28 && upper > lower; attempt++) {
                    var candidate = (lower + upper) / 2;
                    var measured = measure(value, candidate, weight);
                    if (measured.Width <= width && measured.LineHeight <= height) lower = candidate;
                    else upper = candidate;
                }
                if (lower > 0) size = lower;
            }
        }
        var lineHeight = measure("Ag", size, weight).LineHeight;
        maxLines = Math.Min(Math.Max(1, maxLines), (int)Math.Floor(height / lineHeight));
        if (maxLines < 1) {
            report?.Invoke("topology.label-height", "A topology label has no complete text line in its allocated bounds; full source text remains in semantic interchange.");
            return null;
        }
        var lines = FitLines(value, width, size, weight, maxLines, fitSingleLine, options, measure, report, out _);
        return new TopologyTextLayout(size, lineHeight, lines, lines.Count == 0 ? 0 : lines.Max(line => measure(line, size, weight).Width));
    }

    internal static List<string> FitLines(string value, double width, double size, int weight, int maxLines,
        bool fitSingleLine, TopologyRenderOptions options, Func<string, double, int, TextMetrics> measure,
        Action<string, string>? report, out bool overflow) {
        overflow = false;
        var queue = new Queue<string>(AuthoredRows(value, options.AllowMultilineNodeLabels));
        var lines = new List<string>();
        while (queue.Count > 0 && lines.Count < maxLines) {
            var text = queue.Dequeue();
            if (measure(text, size, weight).Width <= width) { lines.Add(text); continue; }
            if (fitSingleLine) {
                // Keep a fitting text-element prefix when an ellipsis would erase the only glyph that fits.
                lines.Add(ChartTextFitting.FitEnd(text, width, probe => measure(probe, size, weight).Width, string.Empty));
                overflow = true;
                report?.Invoke("topology.label-truncated", "A topology label exceeds its measured bounds; complete source text remains in semantic interchange.");
                continue;
            }
            var wrappingRow = options.WrapNodeLabels && lines.Count + 1 < maxLines;
            var suffix = wrappingRow ? string.Empty : "…";
            var elements = StringInfo.ParseCombiningCharacters(text); var count = elements.Length;
            while (count > 0 && measure(text.Substring(0, count == elements.Length ? text.Length : elements[count]) + suffix, size, weight).Width > width) count--;
            var length = count == 0 ? 0 : count == elements.Length ? text.Length : elements[count];
            if (wrappingRow && length > 0) {
                // A complete fitting word before whitespace belongs on this row. An
                // intermediate row paints no ellipsis, so neither should its fit probe.
                if (length < text.Length && !char.IsWhiteSpace(text[length])) {
                    var separator = text.LastIndexOf(' ', length - 1, length); if (separator > 0) length = separator;
                }
                lines.Add(text.Substring(0, length).TrimEnd());
                queue = new Queue<string>(new[] { text.Substring(length).TrimStart() }.Concat(queue));
            } else {
                lines.Add(length == 0 ? string.Empty : text.Substring(0, length) + "…"); overflow = true;
                report?.Invoke("topology.label-truncated", "A topology label was shortened; complete source text remains in semantic interchange.");
            }
        }
        if (queue.Count > 0) {
            overflow = true;
            report?.Invoke("topology.label-lines", "A topology label exceeds its configured line count; complete source text remains in semantic interchange.");
        }
        return lines;
    }
}
