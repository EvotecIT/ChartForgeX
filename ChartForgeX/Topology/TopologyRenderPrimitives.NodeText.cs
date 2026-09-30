using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static List<string> NodeTextLines(string value, double maxWidth, double fontSize, bool bold, int maxLines, TopologyRenderOptions options, int maximumCharacters = NodeLabelMaxLength) {
        if (string.IsNullOrWhiteSpace(value)) return new List<string>();
        maxLines = Math.Max(1, maxLines);
        var allowMultiline = options.AllowMultilineNodeLabels;
        var wrap = options.WrapNodeLabels;
        maximumCharacters = Math.Max(1, maximumCharacters);
        if (!allowMultiline && !wrap) return new List<string> { TrimToEstimatedWidth(TrimTo(value, maximumCharacters), maxWidth, fontSize, bold, options.TextMeasurement) };

        var lines = new List<string>();
        foreach (var explicitLine in SplitExplicitLines(value, allowMultiline)) {
            if (lines.Count >= maxLines) break;
            var trimmed = explicitLine.Trim();
            if (trimmed.Length == 0) continue;
            if (!wrap || EstimateTextWidth(trimmed, fontSize, bold, options.TextMeasurement) <= maxWidth) {
                lines.Add(TrimToEstimatedWidth(TrimTo(trimmed, maximumCharacters * maxLines), maxWidth, fontSize, bold, options.TextMeasurement));
                continue;
            }

            AddWrappedNodeTextLines(lines, trimmed, maxWidth, fontSize, bold, maxLines, maximumCharacters, options.TextMeasurement);
        }

        if (lines.Count == 0) lines.Add(TrimToEstimatedWidth(TrimTo(value.Trim(), maximumCharacters), maxWidth, fontSize, bold, options.TextMeasurement));
        if (lines.Count > maxLines) lines.RemoveRange(maxLines, lines.Count - maxLines);
        return lines;
    }

    /// <summary>
    /// Returns the caption lines drawn below a tile. Readable dense layouts wrap a caption that does not fit on one
    /// line, at spaces and after separators first and inside a word as a last resort, and only shorten the last
    /// allowed line; other layouts keep one shortened line unless wrapping is switched on.
    /// </summary>
    public static List<string> TileCaptionLines(TopologyNode node, TopologyRenderOptions options) {
        var maxWidth = Math.Max(node.Width + 34, 54);
        var limit = NodeTitleMaxLength(node, TopologyNodeDisplayMode.Tile);
        var label = node.Label ?? string.Empty;
        if (!options.ReadableDenseLayout || options.WrapNodeLabels || label.IndexOfAny(new[] { '\r', '\n' }) >= 0) {
            return NodeTextLines(label, maxWidth, 11, true, options.MaxNodeLabelLines, options, limit);
        }

        var lines = new List<string>();
        var maxLines = Math.Max(1, options.MaxNodeLabelLines);
        // The character limit cuts the text without an ellipsis here; the ellipsis is added to the last line after
        // wrapping, so it never takes part in choosing a break.
        var rest = label.Trim();
        var boundaries = System.Globalization.StringInfo.ParseCombiningCharacters(rest);
        var shortened = boundaries.Length > limit * maxLines;
        if (shortened) rest = rest.Substring(0, boundaries[limit * maxLines]).TrimEnd();
        while (rest.Length > 0 && lines.Count < maxLines) {
            if (lines.Count == maxLines - 1 || EstimateTextWidth(shortened ? rest + "..." : rest, 11, true, options.TextMeasurement) <= maxWidth) {
                lines.Add(shortened ? EndWithEllipsis(rest, maxWidth, options.TextMeasurement) : TrimToEstimatedWidth(rest, maxWidth, 11, true, options.TextMeasurement));
                break;
            }

            var cut = CaptionBreak(rest, maxWidth, options.TextMeasurement);
            lines.Add(rest.Substring(0, cut).TrimEnd());
            rest = rest.Substring(cut).TrimStart();
        }

        return lines;
    }

    // Shortens the text until it fits with an ellipsis after it.
    private static string EndWithEllipsis(string text, double maxWidth, TextMeasurementContext? measurement) {
        var boundaries = System.Globalization.StringInfo.ParseCombiningCharacters(text);
        for (var count = boundaries.Length; count > 0; count--) {
            var candidate = text.Substring(0, count == boundaries.Length ? text.Length : boundaries[count]).TrimEnd().TrimEnd('.').TrimEnd() + "...";
            if (EstimateTextWidth(candidate, 11, true, measurement) <= maxWidth) return candidate;
        }

        return string.Empty;
    }

    // Returns how many characters of the text go on the current line: up to the last separator that fits, else as many
    // characters as fit.
    private static int CaptionBreak(string text, double maxWidth, TextMeasurementContext? measurement) {
        var boundaries = System.Globalization.StringInfo.ParseCombiningCharacters(text);
        var fits = boundaries.Length > 1 ? boundaries[1] : text.Length;
        var separator = 0;
        for (var i = 1; i <= boundaries.Length; i++) {
            var end = i == boundaries.Length ? text.Length : boundaries[i];
            if (EstimateTextWidth(text.Substring(0, end), 11, true, measurement) > maxWidth) break;
            fits = end;
            var last = text[end - 1];
            if (end < text.Length && (char.IsWhiteSpace(last) || last == '-' || last == '_' || last == '.' || last == '/')) separator = end;
        }

        return separator > 0 ? separator : fits;
    }

    public static string NodeTextFitProbe(string value, TopologyRenderOptions options) {
        if (string.IsNullOrWhiteSpace(value) || !options.AllowMultilineNodeLabels) return value;
        var best = string.Empty;
        foreach (var line in SplitExplicitLines(value, true)) {
            var trimmed = line.Trim();
            if (trimmed.Length > best.Length) best = trimmed;
        }

        return best.Length == 0 ? value : best;
    }

    public static string NodeTextFitProbe(string value, double maxWidth, double fontSize, bool bold, int maxLines, TopologyRenderOptions options) {
        if (string.IsNullOrWhiteSpace(value)) return value;
        if (!options.WrapNodeLabels || value.IndexOfAny(new[] { '\r', '\n' }) >= 0) return NodeTextFitProbe(value, options);
        var lines = NodeTextLines(value, maxWidth, fontSize, bold, maxLines, options);
        var best = string.Empty;
        var bestWidth = -1.0;
        foreach (var line in lines) {
            var width = EstimateTextWidth(line, fontSize, bold, options.TextMeasurement);
            if (width <= bestWidth) continue;
            best = line;
            bestWidth = width;
        }

        return best.Length == 0 ? value : best;
    }

    public static double NodeDetailStartOffset(TopologyNode node, TopologyRenderOptions options) {
        var textWidth = Math.Max(24, node.Width - 52);
        var titleLimit = NodeTitleMaxLength(node, TopologyNodeDisplayMode.Card);
        var titleValue = TrimTo(node.Label, options.AllowMultilineNodeLabels || options.WrapNodeLabels ? titleLimit * Math.Max(1, options.MaxNodeLabelLines) : titleLimit);
        var titleSize = FitFontSize(NodeTextFitProbe(titleValue, textWidth, 12.5, true, options.MaxNodeLabelLines, options), textWidth, 12.5, 10, true, options.TextMeasurement);
        var titleLines = NodeTextLines(titleValue, textWidth, titleSize, true, options.MaxNodeLabelLines, options, titleLimit);
        var titleLastBaseline = 28 + Math.Max(0, titleLines.Count - 1) * 14;
        var detailStart = Math.Max(63, titleLastBaseline + 14);

        if (string.IsNullOrWhiteSpace(node.Subtitle)) return detailStart;
        if (options.CardSubtitleMode == TopologyCardSubtitleMode.Chip) {
            return Math.Max(detailStart, CardSubtitleChipOffset(node, options) + 28);
        }

        var subtitleStart = Math.Max(47, 28 + titleLines.Count * 13 + 3);
        var subtitleLines = NodeTextLines(node.Subtitle!, textWidth, 10.5, false, options.MaxNodeSubtitleLines, options);
        var subtitleLastBaseline = subtitleStart + Math.Max(0, subtitleLines.Count - 1) * 12;
        return Math.Max(detailStart, subtitleLastBaseline + 14);
    }

    public static double CardSubtitleChipOffset(TopologyNode node, TopologyRenderOptions options) {
        if (node.Details.Count == 0) return node.Height - 22;
        var textWidth = Math.Max(24, node.Width - 52);
        var titleLimit = NodeTitleMaxLength(node, TopologyNodeDisplayMode.Card);
        var titleValue = TrimTo(node.Label, options.AllowMultilineNodeLabels || options.WrapNodeLabels ? titleLimit * Math.Max(1, options.MaxNodeLabelLines) : titleLimit);
        var titleSize = FitFontSize(NodeTextFitProbe(titleValue, textWidth, 12.5, true, options.MaxNodeLabelLines, options), textWidth, 12.5, 10, true, options.TextMeasurement);
        var titleLines = NodeTextLines(titleValue, textWidth, titleSize, true, options.MaxNodeLabelLines, options, titleLimit);
        return Math.Max(36, 28 + Math.Max(0, titleLines.Count - 1) * 14 + 6);
    }

    private static IEnumerable<string> SplitExplicitLines(string value, bool allowMultiline) {
        if (!allowMultiline) {
            yield return value.Replace("\r", " ").Replace("\n", " ");
            yield break;
        }

        foreach (var line in value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) yield return line;
    }

    private static void AddWrappedNodeTextLines(List<string> lines, string value, double maxWidth, double fontSize, bool bold, int maxLines, int maximumCharacters, TextMeasurementContext? measurement) {
        value = TrimTo(value, maximumCharacters);
        var words = value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();
        foreach (var word in words) {
            if (lines.Count >= maxLines) break;
            var candidate = current.Length == 0 ? word : current.ToString() + " " + word;
            if (EstimateTextWidth(candidate, fontSize, bold, measurement) <= maxWidth) {
                current.Clear();
                current.Append(candidate);
                continue;
            }

            if (current.Length > 0) {
                lines.Add(current.ToString());
                current.Clear();
            }

            if (EstimateTextWidth(word, fontSize, bold, measurement) > maxWidth) lines.Add(TrimToEstimatedWidth(word, maxWidth, fontSize, bold, measurement));
            else current.Append(word);
        }

        if (current.Length > 0 && lines.Count < maxLines) lines.Add(current.ToString());
        if (lines.Count == maxLines && words.Length > 0) {
            var lastIndex = lines.Count - 1;
            lines[lastIndex] = TrimToEstimatedWidth(lines[lastIndex], maxWidth, fontSize, bold, measurement);
        }
    }
}
