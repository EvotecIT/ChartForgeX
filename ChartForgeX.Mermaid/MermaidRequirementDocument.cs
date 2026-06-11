using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid requirement diagram.
/// </summary>
public sealed class MermaidRequirementDocument : MermaidDocument {
    /// <summary>Gets raw requirement statements retained with source spans.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets or sets the optional Mermaid layout direction token.</summary>
    public string? Direction { get; set; }

    /// <summary>Gets parsed requirement nodes in source order.</summary>
    public List<MermaidRequirementNode> Requirements { get; } = new();

    /// <summary>Gets parsed external element nodes in source order.</summary>
    public List<MermaidRequirementElement> Elements { get; } = new();

    /// <summary>Gets parsed requirement relationships in source order.</summary>
    public List<MermaidRequirementRelationship> Relationships { get; } = new();

    /// <summary>Gets retained style and class statements that are not rendered exactly by the static topology preview.</summary>
    public List<MermaidRawStatement> StyleStatements { get; } = new();
}

/// <summary>
/// Describes one Mermaid requirement block.
/// </summary>
public sealed class MermaidRequirementNode : MermaidAstNode {
    private string _name;
    private string _requirementType;

    /// <summary>Initializes a requirement node.</summary>
    public MermaidRequirementNode(string name, string requirementType, MermaidSourceSpan span) : base(span) {
        _name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Requirement name is required.", nameof(name)) : name;
        _requirementType = string.IsNullOrWhiteSpace(requirementType) ? throw new ArgumentException("Requirement type is required.", nameof(requirementType)) : requirementType;
    }

    /// <summary>Gets or sets the Mermaid requirement name.</summary>
    public string Name { get => _name; set => _name = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Requirement name is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the Mermaid requirement type token.</summary>
    public string RequirementType { get => _requirementType; set => _requirementType = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Requirement type is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the requirement id field.</summary>
    public string? RequirementId { get; set; }

    /// <summary>Gets or sets the requirement text field.</summary>
    public string? Text { get; set; }

    /// <summary>Gets or sets the requirement risk field.</summary>
    public string? Risk { get; set; }

    /// <summary>Gets or sets the requirement verification method field.</summary>
    public string? VerifyMethod { get; set; }

    /// <summary>Gets style classes assigned to the requirement.</summary>
    public List<string> Classes { get; } = new();
}

/// <summary>
/// Describes one Mermaid requirement element block.
/// </summary>
public sealed class MermaidRequirementElement : MermaidAstNode {
    private string _name;

    /// <summary>Initializes a requirement element.</summary>
    public MermaidRequirementElement(string name, MermaidSourceSpan span) : base(span) {
        _name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Element name is required.", nameof(name)) : name;
    }

    /// <summary>Gets or sets the Mermaid element name.</summary>
    public string Name { get => _name; set => _name = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Element name is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the element type field.</summary>
    public string? ElementType { get; set; }

    /// <summary>Gets or sets the element document reference field.</summary>
    public string? DocumentReference { get; set; }

    /// <summary>Gets style classes assigned to the element.</summary>
    public List<string> Classes { get; } = new();
}

/// <summary>
/// Describes one Mermaid requirement relationship.
/// </summary>
public sealed class MermaidRequirementRelationship : MermaidAstNode {
    private string _sourceName;
    private string _targetName;
    private string _relationshipType;

    /// <summary>Initializes a requirement relationship.</summary>
    public MermaidRequirementRelationship(string sourceName, string targetName, string relationshipType, MermaidSourceSpan span) : base(span) {
        _sourceName = string.IsNullOrWhiteSpace(sourceName) ? throw new ArgumentException("Source name is required.", nameof(sourceName)) : sourceName;
        _targetName = string.IsNullOrWhiteSpace(targetName) ? throw new ArgumentException("Target name is required.", nameof(targetName)) : targetName;
        _relationshipType = string.IsNullOrWhiteSpace(relationshipType) ? throw new ArgumentException("Relationship type is required.", nameof(relationshipType)) : relationshipType;
    }

    /// <summary>Gets or sets the source requirement or element name.</summary>
    public string SourceName { get => _sourceName; set => _sourceName = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Source name is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the target requirement or element name.</summary>
    public string TargetName { get => _targetName; set => _targetName = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Target name is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the Mermaid relationship type token.</summary>
    public string RelationshipType { get => _relationshipType; set => _relationshipType = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Relationship type is required.", nameof(value)) : value; }
}
