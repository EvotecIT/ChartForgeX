using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid entity relationship diagram.
/// </summary>
public sealed class MermaidEntityRelationshipDocument : MermaidDocument {
    /// <summary>Gets entities in source order.</summary>
    public List<MermaidEntityNode> Entities { get; } = new();

    /// <summary>Gets relationships in source order.</summary>
    public List<MermaidEntityRelationship> Relationships { get; } = new();
}

/// <summary>
/// Describes one ER entity.
/// </summary>
public sealed class MermaidEntityNode : MermaidAstNode {
    /// <summary>Initializes an entity node.</summary>
    public MermaidEntityNode(string id, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Entity id must not be empty.", nameof(id));
        Id = id;
    }

    /// <summary>Gets the entity id.</summary>
    public string Id { get; }

    /// <summary>Gets entity attributes.</summary>
    public List<MermaidEntityAttribute> Attributes { get; } = new();
}

/// <summary>
/// Describes one ER entity attribute.
/// </summary>
public sealed class MermaidEntityAttribute : MermaidAstNode {
    /// <summary>Initializes an entity attribute.</summary>
    public MermaidEntityAttribute(string type, string name, string? key, string? comment, MermaidSourceSpan span) : base(span) {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Key = key;
        Comment = comment;
    }

    /// <summary>Gets the attribute type.</summary>
    public string Type { get; }

    /// <summary>Gets the attribute name.</summary>
    public string Name { get; }

    /// <summary>Gets optional key markers such as PK or FK.</summary>
    public string? Key { get; }

    /// <summary>Gets optional attribute comment text.</summary>
    public string? Comment { get; }
}

/// <summary>
/// Describes one ER relationship.
/// </summary>
public sealed class MermaidEntityRelationship : MermaidAstNode {
    /// <summary>Initializes an ER relationship.</summary>
    public MermaidEntityRelationship(string sourceId, string targetId, string connector, string label, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Source entity id must not be empty.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentException("Target entity id must not be empty.", nameof(targetId));
        if (string.IsNullOrWhiteSpace(connector)) throw new ArgumentException("ER relationship connector must not be empty.", nameof(connector));
        SourceId = sourceId;
        TargetId = targetId;
        Connector = connector;
        Label = label ?? string.Empty;
    }

    /// <summary>Gets the source entity id.</summary>
    public string SourceId { get; }

    /// <summary>Gets the target entity id.</summary>
    public string TargetId { get; }

    /// <summary>Gets the Mermaid crow's-foot connector.</summary>
    public string Connector { get; }

    /// <summary>Gets the relationship label.</summary>
    public string Label { get; }
}
