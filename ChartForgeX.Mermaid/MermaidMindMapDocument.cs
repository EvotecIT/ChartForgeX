using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid mindmap.
/// </summary>
public sealed class MermaidMindMapDocument : MermaidDocument {
    /// <summary>Gets mindmap nodes in source order.</summary>
    public List<MermaidMindMapNode> Nodes { get; } = new();
}

/// <summary>
/// Describes one Mermaid mindmap node.
/// </summary>
public sealed class MermaidMindMapNode : MermaidAstNode {
    /// <summary>Initializes a mindmap node.</summary>
    public MermaidMindMapNode(string id, string text, int level, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Mindmap node id must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Mindmap node text must not be empty.", nameof(text));
        Id = id;
        Text = text;
        Level = level;
    }

    /// <summary>Gets the generated stable node id.</summary>
    public string Id { get; }

    /// <summary>Gets the displayed node text.</summary>
    public string Text { get; }

    /// <summary>Gets the indentation-derived logical level.</summary>
    public int Level { get; }

    /// <summary>Gets or sets the parent node id.</summary>
    public string? ParentId { get; set; }

    /// <summary>Gets or sets the parsed shape token.</summary>
    public string? Shape { get; set; }

    /// <summary>Gets parsed class names.</summary>
    public List<string> Classes { get; } = new();

    /// <summary>Gets parsed icon class tokens.</summary>
    public List<string> Icons { get; } = new();
}
