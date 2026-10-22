using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid state diagram.
/// </summary>
public sealed class MermaidStateDocument : MermaidDocument {
    /// <summary>Gets or sets the optional diagram direction statement.</summary>
    public string? Direction { get; set; }

    /// <summary>Gets states in source order.</summary>
    public List<MermaidStateNode> States { get; } = new();

    /// <summary>Gets transitions in source order.</summary>
    public List<MermaidStateTransition> Transitions { get; } = new();

    /// <summary>Gets retained non-rendered state statements.</summary>
    public List<MermaidStateStatement> Statements { get; } = new();

    /// <summary>Gets named paint definitions in source order, including retained unsupported declarations.</summary>
    public List<MermaidStyleDefinition> ClassDefinitions { get; } = new();

    /// <summary>Gets explicit class and style statements at their original source locations.</summary>
    public List<MermaidRawStatement> StyleStatements { get; } = new();

    /// <summary>Gets visible state-attached notes in source order.</summary>
    public List<MermaidDiagramNote> Notes { get; } = new();

    /// <summary>Gets the ordered regions of composite states, including empty regions.</summary>
    /// <remarks>A composite without a concurrency boundary has one implicit region.</remarks>
    public List<MermaidStateRegion> Regions { get; } = new();
}

/// <summary>
/// Describes one state diagram node.
/// </summary>
public sealed class MermaidStateNode : MermaidAstNode {
    /// <summary>Initializes a state node.</summary>
    public MermaidStateNode(string id, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("State id must not be empty.", nameof(id));
        Id = id;
    }

    /// <summary>Gets the state id.</summary>
    public string Id { get; }

    /// <summary>Gets or sets an optional description label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the containing composite state id.</summary>
    public string? ParentId { get; set; }

    /// <summary>Gets or sets the region within the containing composite state.</summary>
    public string? RegionId { get; set; }

    /// <summary>Gets or sets whether this state declares a composite boundary, including an empty one.</summary>
    public bool IsComposite { get; set; }

    /// <summary>Gets or sets the direction authored within this composite state.</summary>
    public string? Direction { get; set; }

    /// <summary>Gets named classes in assignment order; later classes override earlier class paints.</summary>
    public List<string> Classes { get; } = new();

    /// <summary>Gets direct declarations, which override named/default paints without changing state semantics.</summary>
    public Dictionary<string, string> Styles { get; } = new();

    /// <summary>Gets or sets the special state marker such as choice, fork, join, start, or end.</summary>
    public string? Kind { get; set; }
}

/// <summary>
/// Describes one state transition.
/// </summary>
public sealed class MermaidStateTransition : MermaidAstNode {
    /// <summary>Initializes a state transition.</summary>
    public MermaidStateTransition(string sourceId, string targetId, string? label, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Source state id must not be empty.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentException("Target state id must not be empty.", nameof(targetId));
        SourceId = sourceId;
        TargetId = targetId;
        Label = label;
    }

    /// <summary>Gets the source state id.</summary>
    public string SourceId { get; }

    /// <summary>Gets the target state id.</summary>
    public string TargetId { get; }

    /// <summary>Gets the optional transition label.</summary>
    public string? Label { get; }
}

/// <summary>
/// Describes a retained non-transition state statement.
/// </summary>
public sealed class MermaidStateStatement : MermaidAstNode {
    /// <summary>Initializes a state statement.</summary>
    public MermaidStateStatement(string text, MermaidSourceSpan span) : base(span) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Gets the raw statement text.</summary>
    public string Text { get; }
}
