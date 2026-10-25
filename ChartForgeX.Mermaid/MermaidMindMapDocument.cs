using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid mindmap.
/// </summary>
public sealed class MermaidMindMapDocument : MermaidDocument {
    private MermaidMindMapColorMode _colorBy;

    /// <summary>Gets or sets how categorical colors follow the source hierarchy. Branch is the default.</summary>
    public MermaidMindMapColorMode ColorBy {
        get => _colorBy;
        set {
            if (!Enum.IsDefined(typeof(MermaidMindMapColorMode), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _colorBy = value;
        }
    }

    /// <summary>Gets mindmap nodes in source order.</summary>
    public List<MermaidMindMapNode> Nodes { get; } = new();
}

/// <summary>
/// Describes one Mermaid mindmap node.
/// </summary>
public sealed class MermaidMindMapNode : MermaidAstNode {
    /// <summary>Initializes a mindmap node.</summary>
    public MermaidMindMapNode(string id, string text, int level, MermaidSourceSpan span) : this(id, text, level, span, null) { }

    /// <summary>Initializes a node with its optional authored prefix identity, independently of its generated hierarchy id.</summary>
    public MermaidMindMapNode(string id, string text, int level, MermaidSourceSpan span, string? sourceId) : base(span) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Mindmap node id must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Mindmap node text must not be empty.", nameof(text));
        if (sourceId != null && string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Authored mindmap identity must not be empty.", nameof(sourceId));
        Id = id;
        SourceId = sourceId;
        Text = text;
        Level = level;
    }

    /// <summary>Gets the generated stable node id.</summary>
    public string Id { get; }

    /// <summary>Gets the optional authored prefix before a shape delimiter. Generated ids continue to own hierarchy links.</summary>
    public string? SourceId { get; }

    /// <summary>Gets the source caption, including any Markdown wrapper, before safe native text lowering.</summary>
    public string Text { get; }

    /// <summary>Gets the structural parent-child depth, starting at zero for the root, independently of indentation width.</summary>
    public int Level { get; }

    /// <summary>Gets or sets the parent node id.</summary>
    public string? ParentId { get; set; }

    /// <summary>Gets or sets the parsed shape token.</summary>
    public string? Shape { get; set; }

    /// <summary>Gets parsed class names.</summary>
    public List<string> Classes { get; } = new();

    /// <summary>Gets authored icon tokens. Only explicit qualified built-in catalog ids have a native visual mapping.</summary>
    public List<string> Icons { get; } = new();
}

/// <summary>Determines the categorical color section assigned to each mindmap node and its incoming edge.</summary>
public enum MermaidMindMapColorMode {
    /// <summary>Each root branch and all its descendants share a section.</summary>
    Branch = 0,
    /// <summary>Nodes at the same structural depth share a section across branches.</summary>
    Depth = 1
}
