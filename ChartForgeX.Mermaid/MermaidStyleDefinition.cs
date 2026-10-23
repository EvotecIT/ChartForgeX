using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>Describes a named style definition shared by Mermaid diagram families.</summary>
public sealed class MermaidStyleDefinition : MermaidAstNode {
    private string _name;

    /// <summary>Initializes a named style definition at its source location.</summary>
    public MermaidStyleDefinition(string name, MermaidSourceSpan span) : base(span) {
        _name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Style name is required.", nameof(name)) : name;
    }

    /// <summary>Gets or sets the style name used by the family's class-assignment syntax.</summary>
    public string Name { get => _name; set => _name = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Style name is required.", nameof(value)) : value; }

    /// <summary>Gets declarations in the definition, including diagnosed properties without a native mapping.</summary>
    public Dictionary<string, string> Styles { get; } = new();
}
