using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid Ishikawa diagram.
/// </summary>
public sealed class MermaidIshikawaDocument : MermaidDocument {
    /// <summary>Gets Ishikawa statements retained in source order.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets or sets the root effect node.</summary>
    public MermaidIshikawaNode? Root { get; set; }
}

/// <summary>
/// Describes one Mermaid Ishikawa cause node.
/// </summary>
public sealed class MermaidIshikawaNode {
    private readonly List<MermaidIshikawaNode> _children = new();
    private string _text;

    /// <summary>Initializes an Ishikawa node.</summary>
    public MermaidIshikawaNode(string text, int level, MermaidSourceSpan span) {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        Level = level;
        Span = span;
    }

    /// <summary>Gets or sets the display text.</summary>
    public string Text { get => _text; set => _text = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets the normalized tree level where zero is the root effect.</summary>
    public int Level { get; }

    /// <summary>Gets the source span for the node.</summary>
    public MermaidSourceSpan Span { get; }

    /// <summary>Gets child causes.</summary>
    public IReadOnlyList<MermaidIshikawaNode> Children => _children;

    /// <summary>Adds a child node.</summary>
    public void AddChild(MermaidIshikawaNode child) {
        if (child == null) throw new ArgumentNullException(nameof(child));
        _children.Add(child);
    }
}
