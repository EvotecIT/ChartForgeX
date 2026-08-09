using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static List<string> NodeTextLines(string value, double maxWidth, double fontSize, bool bold, int maxLines, TopologyRenderOptions options, int maximumCharacters = NodeLabelMaxLength) {
        if (string.IsNullOrWhiteSpace(value)) return new List<string>();
        maxLines = Math.Max(1, maxLines);
        var allowMultiline = options.AllowMultilineNodeLabels;
        var wrap = options.WrapNodeLabels;
        maximumCharacters = Math.Max(1, maximumCharacters);
        if (!allowMultiline && !wrap) return new List<string> { TrimToEstimatedWidth(TrimTo(value, maximumCharacters), maxWidth, fontSize, bold) };

        var lines = new List<string>();
        foreach (var explicitLine in SplitExplicitLines(value, allowMultiline)) {
            if (lines.Count >= maxLines) break;
            var trimmed = explicitLine.Trim();
            if (trimmed.Length == 0) continue;
            if (!wrap || EstimateTextWidth(trimmed, fontSize, bold) <= maxWidth) {
                lines.Add(TrimToEstimatedWidth(TrimTo(trimmed, maximumCharacters * maxLines), maxWidth, fontSize, bold));
                continue;
            }

            AddWrappedNodeTextLines(lines, trimmed, maxWidth, fontSize, bold, maxLines, maximumCharacters);
        }

        if (lines.Count == 0) lines.Add(TrimToEstimatedWidth(TrimTo(value.Trim(), maximumCharacters), maxWidth, fontSize, bold));
        if (lines.Count > maxLines) lines.RemoveRange(maxLines, lines.Count - maxLines);
        return lines;
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
            var width = EstimateTextWidth(line, fontSize, bold);
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
        var titleSize = FitFontSize(NodeTextFitProbe(titleValue, textWidth, 12.5, true, options.MaxNodeLabelLines, options), textWidth, 12.5, 10, true);
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
        var titleSize = FitFontSize(NodeTextFitProbe(titleValue, textWidth, 12.5, true, options.MaxNodeLabelLines, options), textWidth, 12.5, 10, true);
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

    private static void AddWrappedNodeTextLines(List<string> lines, string value, double maxWidth, double fontSize, bool bold, int maxLines, int maximumCharacters) {
        value = TrimTo(value, maximumCharacters);
        var words = value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();
        foreach (var word in words) {
            if (lines.Count >= maxLines) break;
            var candidate = current.Length == 0 ? word : current.ToString() + " " + word;
            if (EstimateTextWidth(candidate, fontSize, bold) <= maxWidth) {
                current.Clear();
                current.Append(candidate);
                continue;
            }

            if (current.Length > 0) {
                lines.Add(current.ToString());
                current.Clear();
            }

            if (EstimateTextWidth(word, fontSize, bold) > maxWidth) lines.Add(TrimToEstimatedWidth(word, maxWidth, fontSize, bold));
            else current.Append(word);
        }

        if (current.Length > 0 && lines.Count < maxLines) lines.Add(current.ToString());
        if (lines.Count == maxLines && words.Length > 0) {
            var lastIndex = lines.Count - 1;
            lines[lastIndex] = TrimToEstimatedWidth(lines[lastIndex], maxWidth, fontSize, bold);
        }
    }
}
