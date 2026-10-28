using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid tree view diagram.
/// </summary>
public sealed class MermaidTreeViewDocument : MermaidDocument {
    /// <summary>Gets retained non-empty tree view statements.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets top-level tree view nodes.</summary>
    public List<MermaidTreeViewNode> Roots { get; } = new();

    /// <summary>Gets all tree view nodes in source order.</summary>
    public List<MermaidTreeViewNode> Nodes { get; } = new();
}

/// <summary>
/// Describes one Mermaid tree view node.
/// </summary>
public sealed class MermaidTreeViewNode : MermaidAstNode {
    private readonly string _id;
    private readonly string _label;
    private readonly IReadOnlyList<string> _classes;

    /// <summary>Initializes a tree view node.</summary>
    public MermaidTreeViewNode(string id, string label, int indent, int level, MermaidTreeViewNode? parent, MermaidSourceSpan span,
        string? description = null, IEnumerable<string>? classes = null, string? iconReference = null) : base(span) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Tree view node id must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Tree view node label must not be empty.", nameof(label));
        _id = id;
        _label = label;
        Indent = indent;
        Level = level;
        Parent = parent;
        Description = description ?? string.Empty;
        _classes = new List<string>(classes ?? Array.Empty<string>()).AsReadOnly();
        IconReference = iconReference;
    }

    /// <summary>Gets the generated stable node id.</summary>
    public string Id => _id;

    /// <summary>Gets the display label.</summary>
    public string Label => _label;

    /// <summary>Gets the complete inline description, separate from the label.</summary>
    public string Description { get; }

    /// <summary>Gets copied source class names; external CSS is never executed.</summary>
    public IReadOnlyList<string> Classes => _classes;

    /// <summary>Gets the explicit icon name, null for automatic policy or an empty string to hide it.</summary>
    public string? IconReference { get; }

    /// <summary>Gets whether the source explicitly marks a directory with a trailing slash.</summary>
    public bool IsDirectory => Label.EndsWith("/", StringComparison.Ordinal);

    /// <summary>Gets the source indentation width.</summary>
    public int Indent { get; }

    /// <summary>Gets the indentation-derived hierarchy level.</summary>
    public int Level { get; }

    /// <summary>Gets the parent node, or null for root nodes.</summary>
    public MermaidTreeViewNode? Parent { get; }

    /// <summary>Gets child nodes.</summary>
    public List<MermaidTreeViewNode> Children { get; } = new();

    /// <summary>Gets whether the node has child nodes.</summary>
    public bool IsBranch => Children.Count > 0;
}
