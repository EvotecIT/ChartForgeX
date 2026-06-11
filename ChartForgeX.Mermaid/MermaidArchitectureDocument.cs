using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid architecture diagram.
/// </summary>
public sealed class MermaidArchitectureDocument : MermaidDocument {
    /// <summary>Gets raw architecture statements retained with source spans.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets parsed architecture groups in source order.</summary>
    public List<MermaidArchitectureGroup> Groups { get; } = new();

    /// <summary>Gets parsed architecture services in source order.</summary>
    public List<MermaidArchitectureService> Services { get; } = new();

    /// <summary>Gets parsed architecture junctions in source order.</summary>
    public List<MermaidArchitectureJunction> Junctions { get; } = new();

    /// <summary>Gets parsed architecture edges in source order.</summary>
    public List<MermaidArchitectureEdge> Edges { get; } = new();
}

/// <summary>
/// Describes a Mermaid architecture group.
/// </summary>
public sealed class MermaidArchitectureGroup : MermaidAstNode {
    private string _id;
    private string _title;

    /// <summary>Initializes an architecture group.</summary>
    public MermaidArchitectureGroup(string id, string title, MermaidSourceSpan span) : base(span) {
        _id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Group id is required.", nameof(id)) : id;
        _title = title ?? throw new ArgumentNullException(nameof(title));
    }

    /// <summary>Gets or sets the Mermaid group identifier.</summary>
    public string Id { get => _id; set => _id = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Group id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the group title.</summary>
    public string Title { get => _title; set => _title = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the optional Mermaid icon token.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets the optional parent group id declared with Mermaid's <c>in</c> keyword.</summary>
    public string? ParentId { get; set; }
}

/// <summary>
/// Describes a Mermaid architecture service.
/// </summary>
public sealed class MermaidArchitectureService : MermaidAstNode {
    private string _id;
    private string _title;

    /// <summary>Initializes an architecture service.</summary>
    public MermaidArchitectureService(string id, string title, MermaidSourceSpan span) : base(span) {
        _id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Service id is required.", nameof(id)) : id;
        _title = title ?? throw new ArgumentNullException(nameof(title));
    }

    /// <summary>Gets or sets the Mermaid service identifier.</summary>
    public string Id { get => _id; set => _id = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Service id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the service title.</summary>
    public string Title { get => _title; set => _title = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the optional Mermaid icon token.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets the optional parent group id declared with Mermaid's <c>in</c> keyword.</summary>
    public string? GroupId { get; set; }
}

/// <summary>
/// Describes a Mermaid architecture junction.
/// </summary>
public sealed class MermaidArchitectureJunction : MermaidAstNode {
    private string _id;

    /// <summary>Initializes an architecture junction.</summary>
    public MermaidArchitectureJunction(string id, MermaidSourceSpan span) : base(span) {
        _id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Junction id is required.", nameof(id)) : id;
    }

    /// <summary>Gets or sets the Mermaid junction identifier.</summary>
    public string Id { get => _id; set => _id = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Junction id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the optional parent group id declared with Mermaid's <c>in</c> keyword.</summary>
    public string? GroupId { get; set; }
}

/// <summary>
/// Describes a Mermaid architecture edge endpoint.
/// </summary>
public sealed class MermaidArchitectureEndpoint {
    private string _id;

    /// <summary>Initializes an architecture edge endpoint.</summary>
    public MermaidArchitectureEndpoint(string id) {
        _id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Endpoint id is required.", nameof(id)) : id;
    }

    /// <summary>Gets or sets the endpoint service or junction id.</summary>
    public string Id { get => _id; set => _id = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Endpoint id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the optional endpoint side token, such as <c>L</c>, <c>R</c>, <c>T</c>, or <c>B</c>.</summary>
    public string? Side { get; set; }

    /// <summary>Gets or sets whether Mermaid's <c>{group}</c> edge modifier was present.</summary>
    public bool GroupBoundary { get; set; }
}

/// <summary>
/// Describes a Mermaid architecture edge.
/// </summary>
public sealed class MermaidArchitectureEdge : MermaidAstNode {
    private string _operator;

    /// <summary>Initializes an architecture edge.</summary>
    public MermaidArchitectureEdge(MermaidArchitectureEndpoint left, MermaidArchitectureEndpoint right, string edgeOperator, MermaidSourceSpan span) : base(span) {
        Left = left ?? throw new ArgumentNullException(nameof(left));
        Right = right ?? throw new ArgumentNullException(nameof(right));
        _operator = string.IsNullOrWhiteSpace(edgeOperator) ? throw new ArgumentException("Edge operator is required.", nameof(edgeOperator)) : edgeOperator;
    }

    /// <summary>Gets the left Mermaid endpoint.</summary>
    public MermaidArchitectureEndpoint Left { get; }

    /// <summary>Gets the right Mermaid endpoint.</summary>
    public MermaidArchitectureEndpoint Right { get; }

    /// <summary>Gets or sets the raw Mermaid edge operator.</summary>
    public string Operator { get => _operator; set => _operator = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Edge operator is required.", nameof(value)) : value; }
}
