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
    /// Returns the first topology block from a Markdown document, or a raw-markup block when no topology fence is present.
    /// </summary>
    /// <param name="text">The Markdown or raw markup text.</param>
    /// <returns>The topology block payload and one-based source line.</returns>
    public static ChartForgeXMarkdownBlock ExtractFirstTopologyBlock(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var blocks = ExtractTopologyBlocks(text);
        return blocks.Count == 0 ? new ChartForgeXMarkdownBlock(text, 1) : blocks[0];
    }

    /// <summary>
    /// Extracts all fenced topology blocks from Markdown.
    /// </summary>
    /// <param name="text">The Markdown text.</param>
    /// <returns>Fenced topology payloads.</returns>
    public static List<string> ExtractTopologyPayloads(string text) {
        var blocks = ExtractTopologyBlocks(text);
        var payloads = new List<string>(blocks.Count);
        foreach (var block in blocks) payloads.Add(block.Payload);
        return payloads;
    }

    /// <summary>
    /// Extracts all fenced topology blocks from Markdown with their one-based source line.
    /// </summary>
    /// <param name="text">The Markdown text.</param>
    /// <returns>Fenced topology blocks.</returns>
    public static List<ChartForgeXMarkdownBlock> ExtractTopologyBlocks(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var blocks = new List<ChartForgeXMarkdownBlock>();
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var inFence = false;
        var fence = string.Empty;
        var include = false;
        var payloadStartLine = 1;
        var payload = new List<string>();

        for (var index = 0; index < lines.Length; index++) {
            var line = lines[index];
            var indent = LeadingIndentColumns(line);
            var trimmed = line.TrimStart();
            if (!inFence) {
                if (indent > 3) continue;
                if (!trimmed.StartsWith("```", StringComparison.Ordinal) && !trimmed.StartsWith("~~~", StringComparison.Ordinal)) continue;
                var marker = trimmed[0] == '`' ? "`" : "~";
                var count = CountPrefix(trimmed, marker[0]);
                fence = new string(marker[0], count);
                var info = trimmed.Substring(count).Trim();
                include = IsTopologyFence(info);
                inFence = true;
                payloadStartLine = index + 2;
                payload.Clear();
                continue;
            }

            if (indent <= 3 && IsClosingFence(trimmed, fence)) {
                if (include) blocks.Add(new ChartForgeXMarkdownBlock(string.Join("\n", payload), payloadStartLine));
                inFence = false;
                include = false;
                payload.Clear();
                continue;
            }

            if (include) payload.Add(line);
        }

        if (inFence && include) blocks.Add(new ChartForgeXMarkdownBlock(string.Join("\n", payload), payloadStartLine));
        return blocks;
    }

    private static bool IsTopologyFence(string info) {
        if (string.IsNullOrWhiteSpace(info)) return false;
        var normalized = info.Trim().ToLowerInvariant();
        return IsFenceName(normalized, "chartforgex topology") ||
            IsFenceName(normalized, "chartforgex-topology") ||
            IsFenceName(normalized, "cfx topology") ||
            IsFenceName(normalized, "cfx-topology");
    }

    private static bool IsFenceName(string info, string name) {
        if (info == name) return true;
        if (!info.StartsWith(name, StringComparison.Ordinal)) return false;
        var next = info[name.Length];
        return char.IsWhiteSpace(next) || next == '{';
    }

    private static int CountPrefix(string text, char value) {
        var count = 0;
        while (count < text.Length && text[count] == value) count++;
        return count;
    }

    private static bool IsClosingFence(string text, string fence) {
        var markerCount = CountPrefix(text, fence[0]);
        if (markerCount < fence.Length) return false;
        for (var i = markerCount; i < text.Length; i++) {
            if (!char.IsWhiteSpace(text[i])) return false;
        }

        return true;
    }

    private static int LeadingIndentColumns(string text) {
        var count = 0;
        for (var i = 0; i < text.Length; i++) {
            if (text[i] == ' ') {
                count++;
            } else if (text[i] == '\t') {
                count += 4;
            } else {
                break;
            }
        }

        return count;
    }
}

/// <summary>
/// Describes a fenced ChartForgeX Markdown block.
/// </summary>
public sealed class ChartForgeXMarkdownBlock {
    /// <summary>Initializes a new fenced block descriptor.</summary>
    /// <param name="payload">The extracted fence payload.</param>
    /// <param name="startLine">The one-based source line where the payload starts.</param>
    public ChartForgeXMarkdownBlock(string payload, int startLine) {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        StartLine = startLine < 1 ? 1 : startLine;
    }

    /// <summary>Gets the extracted fence payload.</summary>
    public string Payload { get; }

    /// <summary>Gets the one-based source line where the payload starts.</summary>
    public int StartLine { get; }
}
