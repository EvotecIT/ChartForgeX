using System;
using System.Collections.Generic;
using ChartForgeX.Mermaid;

namespace ChartForgeX.Markup.Mermaid;

/// <summary>
/// Parses Markdown visual fences with built-in ChartForgeX support plus Mermaid artifact support.
/// </summary>
public sealed class MermaidVisualMarkupParser {
    private readonly VisualMarkupParser _parser;

    /// <summary>
    /// Initializes a parser with default Mermaid flowchart rendering options.
    /// </summary>
    public MermaidVisualMarkupParser() : this(new MermaidVisualMarkupRenderOptions()) {
    }

    /// <summary>
    /// Initializes a parser with Mermaid rendering options.
    /// </summary>
    /// <param name="renderOptions">Optional rendering defaults by Mermaid diagram kind.</param>
    public MermaidVisualMarkupParser(MermaidVisualMarkupRenderOptions renderOptions) {
        if (renderOptions == null) throw new ArgumentNullException(nameof(renderOptions));
        _parser = new VisualMarkupParser(new MermaidVisualMarkupBlockParser(renderOptions));
    }

    /// <summary>
    /// Parses Markdown visual fences into ChartForgeX visual artifacts, including Mermaid flowchart fences.
    /// </summary>
    /// <param name="text">Markdown text.</param>
    /// <returns>The visual markup parse result.</returns>
    public VisualMarkupParseResult Parse(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        return _parser.Parse(text);
    }

    /// <summary>
    /// Parses visual blocks that were already discovered by another Markdown parser into ChartForgeX visual artifacts.
    /// </summary>
    /// <param name="blocks">Pre-scanned visual blocks, for example blocks discovered by a host Markdown pipeline.</param>
    /// <returns>The visual markup parse result.</returns>
    public VisualMarkupParseResult ParseBlocks(IEnumerable<VisualMarkupBlock> blocks) {
        if (blocks == null) throw new ArgumentNullException(nameof(blocks));
        return _parser.ParseBlocks(blocks);
    }
}
