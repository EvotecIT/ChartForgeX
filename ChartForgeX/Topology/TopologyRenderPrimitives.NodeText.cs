using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static List<string> NodeTextLines(string value, double maxWidth, double fontSize, bool bold, int maxLines, TopologyRenderOptions options) {
        if (string.IsNullOrWhiteSpace(value)) return new List<string>();
        maxLines = Math.Max(1, maxLines);
        var allowMultiline = options.AllowMultilineNodeLabels;
        var wrap = options.WrapNodeLabels;
        if (!allowMultiline && !wrap) return new List<string> { TrimToEstimatedWidth(TrimTo(value, NodeLabelMaxLength), maxWidth, fontSize, bold) };

        var lines = new List<string>();
        foreach (var explicitLine in SplitExplicitLines(value, allowMultiline)) {
            if (lines.Count >= maxLines) break;
            var trimmed = explicitLine.Trim();
            if (trimmed.Length == 0) continue;
            if (!wrap || EstimateTextWidth(trimmed, fontSize, bold) <= maxWidth) {
                lines.Add(TrimToEstimatedWidth(TrimTo(trimmed, NodeLabelMaxLength * maxLines), maxWidth, fontSize, bold));
                continue;
            }

            AddWrappedNodeTextLines(lines, trimmed, maxWidth, fontSize, bold, maxLines);
        }

        if (lines.Count == 0) lines.Add(TrimToEstimatedWidth(TrimTo(value.Trim(), NodeLabelMaxLength), maxWidth, fontSize, bold));
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

    private static IEnumerable<string> SplitExplicitLines(string value, bool allowMultiline) {
        if (!allowMultiline) {
            yield return value.Replace("\r", " ").Replace("\n", " ");
            yield break;
        }

        foreach (var line in value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) yield return line;
    }

    private static void AddWrappedNodeTextLines(List<string> lines, string value, double maxWidth, double fontSize, bool bold, int maxLines) {
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
