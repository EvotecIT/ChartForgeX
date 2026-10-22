using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>Describes one named ER group and its local layout direction.</summary>
public sealed class MermaidEntityGroup : MermaidAstNode {
    /// <summary>Initializes a group with its source identity and visible label.</summary>
    public MermaidEntityGroup(string id, string label, MermaidSourceSpan span) : base(span) {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("ER group id is required.", nameof(id)) : id;
        Label = label ?? throw new ArgumentNullException(nameof(label));
    }
    /// <summary>Gets the stable group identity.</summary>
    public string Id { get; }
    /// <summary>Gets or sets the visible group label.</summary>
    public string Label { get; set; }
    /// <summary>Gets or sets the containing group identity.</summary>
    public string? ParentId { get; set; }
    /// <summary>Gets or sets a local direction; null inherits its parent.</summary>
    public string? Direction { get; set; }
    /// <summary>Gets style classes assigned in source order.</summary>
    public List<string> Classes { get; } = new();
    /// <summary>Gets resolved style declarations, including diagnosed properties without a native mapping.</summary>
    public Dictionary<string, string> Styles { get; } = new();
}
