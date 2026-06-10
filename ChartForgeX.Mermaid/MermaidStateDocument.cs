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

    /// <summary>Gets retained notes, class definitions, and other non-rendered state statements.</summary>
    public List<MermaidStateStatement> Statements { get; } = new();
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
