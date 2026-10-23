using System;
using System.Collections.Generic;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>A ZenUML occurrence tree; expressions are inert display data, never executable instructions.</summary>
public sealed class MermaidZenUmlDocument : MermaidDocument {
    internal bool ContainsSyntaxErrors { get; set; }
    /// <summary>Gets or sets the source title.</summary>
    public string? Title { get; set; }
    /// <summary>Gets or sets the initiating participant; null retains an external caller for unqualified root calls.</summary>
    public string? StarterId { get; set; }
    /// <summary>Gets declared and inferred participants in first-reference order.</summary>
    public List<MermaidZenUmlParticipant> Participants { get; } = new();
    /// <summary>Gets non-overlapping groups of contiguous participant declarations.</summary>
    public List<MermaidZenUmlParticipantGroup> ParticipantGroups { get; } = new();
    /// <summary>Gets source-ordered statements, including nested occurrence and branch bodies.</summary>
    public List<MermaidZenUmlStatement> Statements { get; } = new();
}

/// <summary>A declared or inferred sequence lifeline.</summary>
public sealed class MermaidZenUmlParticipant : MermaidAstNode {
    /// <summary>Initializes a participant identity.</summary>
    public MermaidZenUmlParticipant(string id, MermaidSourceSpan span) : base(span) {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("ZenUML participant id is required.", nameof(id)) : id;
    }
    /// <summary>Gets the source identity.</summary>
    public string Id { get; }
    /// <summary>Gets or sets the display alias; null displays the identity.</summary>
    public string? Alias { get; set; }
    /// <summary>Gets or sets the native participant notation.</summary>
    public SequenceArtifactParticipantKind Kind { get; set; }
    /// <summary>Gets or sets whether a message inferred the participant.</summary>
    public bool IsImplicit { get; set; }
    /// <summary>Gets source presentation attributes, including those explicitly diagnosed as unsupported.</summary>
    public Dictionary<string, string> Attributes { get; } = new(StringComparer.Ordinal);
}

/// <summary>A source group around participant lifelines.</summary>
public sealed class MermaidZenUmlParticipantGroup : MermaidAstNode {
    /// <summary>Initializes a named participant group.</summary>
    public MermaidZenUmlParticipantGroup(string label, MermaidSourceSpan span) : base(span) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
    }
    /// <summary>Gets the authored group label.</summary>
    public string Label { get; }
    /// <summary>Gets the contained participant identities in declaration order.</summary>
    public List<string> ParticipantIds { get; } = new();
}

/// <summary>The semantics of an occurrence-tree statement, independent from native sequence drawing.</summary>
public enum MermaidZenUmlStatementKind {
    /// <summary>A synchronous method occurrence.</summary>
    Call,
    /// <summary>An asynchronous message.</summary>
    Async,
    /// <summary>An explicit reply or return from the current occurrence.</summary>
    Return,
    /// <summary>Creation of a participant at a message occurrence.</summary>
    Create,
    /// <summary>An if/else alternative.</summary>
    Conditional,
    /// <summary>A loop fragment.</summary>
    Loop,
    /// <summary>An optional fragment.</summary>
    Optional,
    /// <summary>A parallel fragment.</summary>
    Parallel,
    /// <summary>A critical fragment.</summary>
    Critical,
    /// <summary>A section or frame.</summary>
    Section,
    /// <summary>An exception-flow fragment with catch/finally branches.</summary>
    Try,
    /// <summary>A reference retained with an explicit native-presentation diagnostic.</summary>
    Reference,
    /// <summary>A divider retained with an explicit native-presentation diagnostic.</summary>
    Divider
}

/// <summary>A call, fragment or occurrence carrying exact source identities and spans.</summary>
public sealed class MermaidZenUmlStatement : MermaidAstNode {
    /// <summary>Initializes a typed statement.</summary>
    public MermaidZenUmlStatement(MermaidZenUmlStatementKind kind, string text, MermaidSourceSpan span) : base(span) {
        if (!Enum.IsDefined(typeof(MermaidZenUmlStatementKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Kind = kind; Text = text ?? throw new ArgumentNullException(nameof(text));
    }
    /// <summary>Gets the authored semantic kind.</summary>
    public MermaidZenUmlStatementKind Kind { get; }
    /// <summary>Gets the method, condition, response or section text.</summary>
    public string Text { get; }
    /// <summary>Gets or sets the caller; null denotes the external initiating lifeline.</summary>
    public string? CallerId { get; set; }
    /// <summary>Gets or sets the target participant for a call, asynchronous message, creation or return.</summary>
    public string? TargetId { get; set; }
    /// <summary>Gets or sets the complete inert assignment prefix, including a declared type or modifier.</summary>
    public string? Assignment { get; set; }
    /// <summary>Gets or sets the authored fragment keyword.</summary>
    public string? Keyword { get; set; }
    /// <summary>Gets or sets whether braces authored an activation body, including an empty body.</summary>
    public bool HasBody { get; set; }
    /// <summary>Gets comments displayed above this statement.</summary>
    public List<MermaidRawStatement> Comments { get; } = new();
    /// <summary>Gets the call occurrence or primary fragment body.</summary>
    public List<MermaidZenUmlStatement> Children { get; } = new();
    /// <summary>Gets alternative/catch/finally branches in source order.</summary>
    public List<MermaidZenUmlBranch> Branches { get; } = new();
}

/// <summary>An alternative or exception branch with its own inert condition and occurrence body.</summary>
public sealed class MermaidZenUmlBranch : MermaidAstNode {
    /// <summary>Initializes a source branch.</summary>
    public MermaidZenUmlBranch(string kind, string text, MermaidSourceSpan span) : base(span) {
        Kind = string.IsNullOrWhiteSpace(kind) ? throw new ArgumentException("ZenUML branch kind is required.", nameof(kind)) : kind;
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }
    /// <summary>Gets else/else-if/catch/finally.</summary>
    public string Kind { get; }
    /// <summary>Gets the inert condition or exception declaration.</summary>
    public string Text { get; }
    /// <summary>Gets source-ordered statements in this branch.</summary>
    public List<MermaidZenUmlStatement> Statements { get; } = new();
}
