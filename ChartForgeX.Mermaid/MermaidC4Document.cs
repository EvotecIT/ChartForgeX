using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid C4 diagram.
/// </summary>
public sealed class MermaidC4Document : MermaidDocument {
    /// <summary>Gets or sets the concrete C4 header token such as <c>C4Context</c>.</summary>
    public string DiagramType { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional Mermaid title statement.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets an optional Mermaid direction statement.</summary>
    public string? Direction { get; set; }

    /// <summary>Gets raw C4 statements retained with source spans.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets parsed C4 boundaries and deployment nodes in source order.</summary>
    public List<MermaidC4Boundary> Boundaries { get; } = new();

    /// <summary>Gets parsed C4 elements in source order.</summary>
    public List<MermaidC4Element> Elements { get; } = new();

    /// <summary>Gets parsed C4 relationships in source order.</summary>
    public List<MermaidC4Relationship> Relationships { get; } = new();

    /// <summary>Gets retained C4 statements whose semantics are not rendered exactly yet.</summary>
    public List<MermaidRawStatement> RetainedStatements { get; } = new();
}

/// <summary>
/// Describes a C4 boundary or deployment node.
/// </summary>
public sealed class MermaidC4Boundary : MermaidAstNode {
    private string _id;
    private string _label;
    private string _kind;

    /// <summary>Initializes a C4 boundary.</summary>
    public MermaidC4Boundary(string id, string label, string kind, MermaidSourceSpan span) : base(span) {
        _id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Boundary id is required.", nameof(id)) : id;
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _kind = string.IsNullOrWhiteSpace(kind) ? throw new ArgumentException("Boundary kind is required.", nameof(kind)) : kind;
    }

    /// <summary>Gets or sets the boundary id.</summary>
    public string Id { get => _id; set => _id = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Boundary id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the boundary label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the C4 boundary kind.</summary>
    public string Kind { get => _kind; set => _kind = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Boundary kind is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the optional C4 boundary type argument.</summary>
    public string? Type { get; set; }

    /// <summary>Gets or sets the optional parent boundary id.</summary>
    public string? ParentId { get; set; }
}

/// <summary>
/// Describes a C4 person, system, container, component, or deployment node.
/// </summary>
public sealed class MermaidC4Element : MermaidAstNode {
    private string _alias;
    private string _label;
    private string _kind;

    /// <summary>Initializes a C4 element.</summary>
    public MermaidC4Element(string alias, string label, string kind, MermaidSourceSpan span) : base(span) {
        _alias = string.IsNullOrWhiteSpace(alias) ? throw new ArgumentException("Element alias is required.", nameof(alias)) : alias;
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _kind = string.IsNullOrWhiteSpace(kind) ? throw new ArgumentException("Element kind is required.", nameof(kind)) : kind;
    }

    /// <summary>Gets or sets the C4 element alias.</summary>
    public string Alias { get => _alias; set => _alias = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Element alias is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the normalized C4 element kind.</summary>
    public string Kind { get => _kind; set => _kind = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Element kind is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the optional element description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the optional technology token for containers and components.</summary>
    public string? Technology { get; set; }

    /// <summary>Gets or sets optional Mermaid sprite metadata.</summary>
    public string? Sprite { get; set; }

    /// <summary>Gets or sets optional Mermaid tags.</summary>
    public string? Tags { get; set; }

    /// <summary>Gets or sets optional Mermaid link metadata.</summary>
    public string? Link { get; set; }

    /// <summary>Gets or sets the containing boundary id.</summary>
    public string? BoundaryId { get; set; }
}

/// <summary>
/// Describes a C4 relationship.
/// </summary>
public sealed class MermaidC4Relationship : MermaidAstNode {
    private string _sourceAlias;
    private string _targetAlias;
    private string _kind;

    /// <summary>Initializes a C4 relationship.</summary>
    public MermaidC4Relationship(string sourceAlias, string targetAlias, string kind, MermaidSourceSpan span) : base(span) {
        _sourceAlias = string.IsNullOrWhiteSpace(sourceAlias) ? throw new ArgumentException("Relationship source is required.", nameof(sourceAlias)) : sourceAlias;
        _targetAlias = string.IsNullOrWhiteSpace(targetAlias) ? throw new ArgumentException("Relationship target is required.", nameof(targetAlias)) : targetAlias;
        _kind = string.IsNullOrWhiteSpace(kind) ? throw new ArgumentException("Relationship kind is required.", nameof(kind)) : kind;
    }

    /// <summary>Gets or sets the source element alias.</summary>
    public string SourceAlias { get => _sourceAlias; set => _sourceAlias = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Relationship source is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the target element alias.</summary>
    public string TargetAlias { get => _targetAlias; set => _targetAlias = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Relationship target is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the normalized relationship kind.</summary>
    public string Kind { get => _kind; set => _kind = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Relationship kind is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the optional relationship label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the optional technology label.</summary>
    public string? Technology { get; set; }

    /// <summary>Gets or sets optional Mermaid tags.</summary>
    public string? Tags { get; set; }

    /// <summary>Gets or sets optional Mermaid link metadata.</summary>
    public string? Link { get; set; }
}
