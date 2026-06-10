using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid class diagram.
/// </summary>
public sealed class MermaidClassDocument : MermaidDocument {
    /// <summary>Gets classes in source order.</summary>
    public List<MermaidClassNode> Classes { get; } = new();

    /// <summary>Gets class relationships in source order.</summary>
    public List<MermaidClassRelationship> Relationships { get; } = new();

    /// <summary>Gets class definition statements retained from source.</summary>
    public List<MermaidClassStyleStatement> StyleStatements { get; } = new();
}

/// <summary>
/// Describes one Mermaid class.
/// </summary>
public sealed class MermaidClassNode : MermaidAstNode {
    /// <summary>Initializes a class node.</summary>
    public MermaidClassNode(string id, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Class id must not be empty.", nameof(id));
        Id = id;
    }

    /// <summary>Gets the class id.</summary>
    public string Id { get; }

    /// <summary>Gets or sets an optional display label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets class annotations such as interface or abstract.</summary>
    public List<string> Annotations { get; } = new();

    /// <summary>Gets parsed class members.</summary>
    public List<MermaidClassMember> Members { get; } = new();
}

/// <summary>
/// Describes one class member.
/// </summary>
public sealed class MermaidClassMember : MermaidAstNode {
    /// <summary>Initializes a class member.</summary>
    public MermaidClassMember(string text, bool method, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Class member text must not be empty.", nameof(text));
        Text = text;
        IsMethod = method;
    }

    /// <summary>Gets the member text.</summary>
    public string Text { get; }

    /// <summary>Gets whether the member is a method/function.</summary>
    public bool IsMethod { get; }
}

/// <summary>
/// Describes a Mermaid class relationship.
/// </summary>
public sealed class MermaidClassRelationship : MermaidAstNode {
    /// <summary>Initializes a class relationship.</summary>
    public MermaidClassRelationship(string sourceId, string targetId, string connector, string? label, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Source class id must not be empty.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentException("Target class id must not be empty.", nameof(targetId));
        if (string.IsNullOrWhiteSpace(connector)) throw new ArgumentException("Class relationship connector must not be empty.", nameof(connector));
        SourceId = sourceId;
        TargetId = targetId;
        Connector = connector;
        Label = label;
    }

    /// <summary>Gets the source class id.</summary>
    public string SourceId { get; }

    /// <summary>Gets the target class id.</summary>
    public string TargetId { get; }

    /// <summary>Gets the Mermaid relationship connector.</summary>
    public string Connector { get; }

    /// <summary>Gets the optional relationship label.</summary>
    public string? Label { get; }
}

/// <summary>
/// Describes class styling or class assignment statements retained from source.
/// </summary>
public sealed class MermaidClassStyleStatement : MermaidAstNode {
    /// <summary>Initializes a retained class styling statement.</summary>
    public MermaidClassStyleStatement(string text, MermaidSourceSpan span) : base(span) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Gets the raw statement text.</summary>
    public string Text { get; }
}
