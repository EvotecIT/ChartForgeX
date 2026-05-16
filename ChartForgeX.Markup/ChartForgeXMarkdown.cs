using System;
using System.Collections.Generic;

namespace ChartForgeX.Markup;

/// <summary>
/// Extracts ChartForgeX fenced blocks from Markdown.
/// </summary>
public static class ChartForgeXMarkdown {
    /// <summary>
    /// Returns the first topology payload from a Markdown document, or the original text when it is already raw topology markup.
    /// </summary>
    /// <param name="text">The Markdown or raw markup text.</param>
    /// <returns>The topology payload.</returns>
    public static string ExtractFirstTopologyPayload(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var blocks = ExtractTopologyPayloads(text);
        return blocks.Count == 0 ? text : blocks[0];
    }

    /// <summary>
    /// Extracts all fenced topology blocks from Markdown.
    /// </summary>
    /// <param name="text">The Markdown text.</param>
    /// <returns>Fenced topology payloads.</returns>
    public static List<string> ExtractTopologyPayloads(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var blocks = new List<string>();
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var inFence = false;
        var fence = string.Empty;
        var include = false;
        var payload = new List<string>();

        foreach (var line in lines) {
            var trimmed = line.TrimStart();
            if (!inFence) {
                if (!trimmed.StartsWith("```", StringComparison.Ordinal) && !trimmed.StartsWith("~~~~", StringComparison.Ordinal)) continue;
                var marker = trimmed[0] == '`' ? "`" : "~";
                var count = CountPrefix(trimmed, marker[0]);
                fence = new string(marker[0], count);
                var info = trimmed.Substring(count).Trim();
                include = IsTopologyFence(info);
                inFence = true;
                payload.Clear();
                continue;
            }

            if (trimmed.StartsWith(fence, StringComparison.Ordinal)) {
                if (include) blocks.Add(string.Join("\n", payload));
                inFence = false;
                include = false;
                payload.Clear();
                continue;
            }

            if (include) payload.Add(line);
        }

        return blocks;
    }

    private static bool IsTopologyFence(string info) {
        if (string.IsNullOrWhiteSpace(info)) return false;
        var normalized = info.Trim().ToLowerInvariant();
        return normalized == "chartforgex topology" ||
            normalized == "chartforgex-topology" ||
            normalized == "cfx topology" ||
            normalized == "cfx-topology";
    }

    private static int CountPrefix(string text, char value) {
        var count = 0;
        while (count < text.Length && text[count] == value) count++;
        return count;
    }
}
