using System;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Base type for source-preserving Mermaid AST nodes.
/// </summary>
public abstract class MermaidAstNode {
    /// <summary>Initializes an AST node with a source span.</summary>
    protected MermaidAstNode(MermaidSourceSpan span) => Span = span;

    /// <summary>Gets the source span for this node.</summary>
    public MermaidSourceSpan Span { get; }
}

/// <summary>
/// Describes an unclassified Mermaid statement retained for later semantic parsing.
/// </summary>
public sealed class MermaidRawStatement : MermaidAstNode {
    private string _text;

    /// <summary>Initializes a raw Mermaid statement.</summary>
    public MermaidRawStatement(string text, MermaidSourceSpan span) : base(span) {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Gets or sets the raw statement text.</summary>
    public string Text { get => _text; set => _text = value ?? throw new ArgumentNullException(nameof(value)); }
}
