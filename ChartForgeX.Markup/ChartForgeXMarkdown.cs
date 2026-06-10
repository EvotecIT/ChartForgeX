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
        var visualBlocks = VisualMarkupScanner.ExtractBlocks(text);
        var blocks = new List<ChartForgeXMarkdownBlock>();
        foreach (var block in visualBlocks) {
            if (block.Kind == VisualMarkupKind.Topology) blocks.Add(new ChartForgeXMarkdownBlock(block.Payload, block.StartLine));
        }

        return blocks;
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
