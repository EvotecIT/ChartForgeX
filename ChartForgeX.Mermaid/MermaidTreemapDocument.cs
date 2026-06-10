using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid treemap diagram.
/// </summary>
public sealed class MermaidTreemapDocument : MermaidDocument {
    /// <summary>Gets retained non-empty treemap statements.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets the top-level treemap nodes.</summary>
    public List<MermaidTreemapNode> Roots { get; } = new();

    /// <summary>Gets all treemap nodes in source order.</summary>
    public List<MermaidTreemapNode> Nodes { get; } = new();
}

/// <summary>
/// Describes one Mermaid treemap node.
/// </summary>
public sealed class MermaidTreemapNode : MermaidAstNode {
    private readonly string _label;

    /// <summary>Initializes a treemap node.</summary>
    public MermaidTreemapNode(string label, double? value, int indent, MermaidTreemapNode? parent, string? className, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Treemap node label must not be empty.", nameof(label));
        _label = label;
        Value = value;
        Indent = indent;
        Parent = parent;
        ClassName = className;
    }

    /// <summary>Gets the display label.</summary>
    public string Label => _label;

    /// <summary>Gets the optional leaf value. Null means this node is a section/parent node.</summary>
    public double? Value { get; }

    /// <summary>Gets the source indentation level.</summary>
    public int Indent { get; }

    /// <summary>Gets the optional Mermaid class name from <c>:::class</c> syntax.</summary>
    public string? ClassName { get; }

    /// <summary>Gets the parent node, or null for roots.</summary>
    public MermaidTreemapNode? Parent { get; }

    /// <summary>Gets child nodes.</summary>
    public List<MermaidTreemapNode> Children { get; } = new();

    /// <summary>Gets whether the node is a leaf with a numeric value.</summary>
    public bool IsLeaf => Value.HasValue;

    /// <summary>Gets the node path from the root.</summary>
    public string Path {
        get {
            var labels = new Stack<string>();
            for (var current = this; current != null; current = current.Parent) labels.Push(current.Label);
            return string.Join(" / ", labels);
        }
    }
}
