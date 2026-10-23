using System;

namespace ChartForgeX.Mermaid;

/// <summary>Describes the stable identity, display label and parent of a class namespace.</summary>
public sealed class MermaidClassNamespace : MermaidAstNode {
    /// <summary>Initializes a namespace with its qualified identity and visible label.</summary>
    public MermaidClassNamespace(string id, string label, MermaidSourceSpan span) : base(span) {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Namespace id is required.", nameof(id)) : id;
        Label = label ?? throw new ArgumentNullException(nameof(label));
    }
    /// <summary>Gets the qualified namespace identity.</summary>
    public string Id { get; }
    /// <summary>Gets or sets the visible namespace label.</summary>
    public string Label { get; set; }
    /// <summary>Gets or sets the containing namespace identity.</summary>
    public string? ParentId { get; set; }
}
