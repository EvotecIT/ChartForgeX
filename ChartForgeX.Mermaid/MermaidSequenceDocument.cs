using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes the declared kind of a Mermaid sequence participant.
/// </summary>
public enum MermaidSequenceParticipantKind {
    /// <summary>A standard participant.</summary>
    Participant,
    /// <summary>An actor participant.</summary>
    Actor,
    /// <summary>A boundary participant.</summary>
    Boundary,
    /// <summary>A control participant.</summary>
    Control,
    /// <summary>An entity participant.</summary>
    Entity,
    /// <summary>A database participant.</summary>
    Database,
    /// <summary>A collections participant.</summary>
    Collections,
    /// <summary>A queue participant.</summary>
    Queue
}

/// <summary>
/// Defines the kind of a parsed Mermaid sequence block statement.
/// </summary>
public enum MermaidSequenceBlockKind {
    /// <summary>A loop block.</summary>
    Loop,
    /// <summary>An alternative branch block.</summary>
    Alt,
    /// <summary>An optional branch block.</summary>
    Opt,
    /// <summary>A parallel branch block.</summary>
    Par,
    /// <summary>A critical region block.</summary>
    Critical,
    /// <summary>A background rectangle block.</summary>
    Rect,
    /// <summary>A break or exception-flow block.</summary>
    Break,
    /// <summary>An else branch inside an alternative block.</summary>
    Else,
    /// <summary>An and branch inside a parallel block.</summary>
    And,
    /// <summary>An option branch inside a critical block.</summary>
    Option,
    /// <summary>An end statement closing the current block.</summary>
    End
}

/// <summary>
/// Describes a Mermaid sequence diagram document.
/// </summary>
public sealed class MermaidSequenceDocument : MermaidDocument {
    /// <summary>Gets raw sequence statements retained with source spans.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets declared and inferred participants in source order.</summary>
    public List<MermaidSequenceParticipant> Participants { get; } = new();

    /// <summary>Gets parsed sequence messages.</summary>
    public List<MermaidSequenceMessage> Messages { get; } = new();

    /// <summary>Gets parsed sequence notes.</summary>
    public List<MermaidSequenceNote> Notes { get; } = new();

    /// <summary>Gets parsed activation and deactivation statements.</summary>
    public List<MermaidSequenceActivation> Activations { get; } = new();

    /// <summary>Gets parsed block control statements such as loop, alt, par, critical, rect, and end.</summary>
    public List<MermaidSequenceBlock> Blocks { get; } = new();

    /// <summary>Gets parsed actor menu links.</summary>
    public List<MermaidSequenceLink> Links { get; } = new();

    /// <summary>Gets parsed autonumber configuration, when present.</summary>
    public MermaidSequenceAutonumber? Autonumber { get; set; }
}

/// <summary>
/// Describes a Mermaid sequence participant.
/// </summary>
public sealed class MermaidSequenceParticipant : MermaidAstNode {
    private string _id;

    /// <summary>Initializes a sequence participant.</summary>
    public MermaidSequenceParticipant(string id, MermaidSourceSpan span) : base(span) {
        _id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Participant id is required.", nameof(id)) : id;
    }

    /// <summary>Gets or sets the Mermaid participant identifier.</summary>
    public string Id { get => _id; set => _id = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Participant id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the display alias.</summary>
    public string? Alias { get; set; }

    /// <summary>Gets or sets the declared participant kind.</summary>
    public MermaidSequenceParticipantKind Kind { get; set; }

    /// <summary>Gets or sets the raw participant configuration object, when supplied.</summary>
    public string? Configuration { get; set; }

    /// <summary>Gets or sets whether the participant was inferred from a message or note.</summary>
    public bool IsImplicit { get; set; }
}

/// <summary>
/// Describes a Mermaid sequence message.
/// </summary>
public sealed class MermaidSequenceMessage : MermaidAstNode {
    private string _sourceId;
    private string _targetId;
    private string _operator;

    /// <summary>Initializes a sequence message.</summary>
    public MermaidSequenceMessage(string sourceId, string targetId, string messageOperator, MermaidSourceSpan span) : base(span) {
        _sourceId = string.IsNullOrWhiteSpace(sourceId) ? throw new ArgumentException("Message source id is required.", nameof(sourceId)) : sourceId;
        _targetId = string.IsNullOrWhiteSpace(targetId) ? throw new ArgumentException("Message target id is required.", nameof(targetId)) : targetId;
        _operator = string.IsNullOrWhiteSpace(messageOperator) ? throw new ArgumentException("Message operator is required.", nameof(messageOperator)) : messageOperator;
    }

    /// <summary>Gets or sets the source participant id.</summary>
    public string SourceId { get => _sourceId; set => _sourceId = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Message source id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the target participant id.</summary>
    public string TargetId { get => _targetId; set => _targetId = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Message target id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the raw Mermaid message operator.</summary>
    public string Operator { get => _operator; set => _operator = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Message operator is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the message text.</summary>
    public string? Text { get; set; }

    /// <summary>Gets or sets whether the message uses Mermaid central connection syntax.</summary>
    public bool IsCentralConnection { get; set; }

    /// <summary>Gets or sets whether the operator activates the target participant.</summary>
    public bool ActivatesTarget { get; set; }

    /// <summary>Gets or sets whether the operator deactivates a participant.</summary>
    public bool Deactivates { get; set; }
}

/// <summary>
/// Describes a Mermaid sequence note.
/// </summary>
public sealed class MermaidSequenceNote : MermaidAstNode {
    private string _placement;

    /// <summary>Initializes a sequence note.</summary>
    public MermaidSequenceNote(string placement, MermaidSourceSpan span) : base(span) {
        _placement = string.IsNullOrWhiteSpace(placement) ? throw new ArgumentException("Note placement is required.", nameof(placement)) : placement;
    }

    /// <summary>Gets or sets the note placement, such as right of, left of, or over.</summary>
    public string Placement { get => _placement; set => _placement = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Note placement is required.", nameof(value)) : value; }

    /// <summary>Gets participant ids referenced by the note.</summary>
    public List<string> ParticipantIds { get; } = new();

    /// <summary>Gets or sets the note text.</summary>
    public string? Text { get; set; }
}

/// <summary>
/// Describes a Mermaid sequence activation statement.
/// </summary>
public sealed class MermaidSequenceActivation : MermaidAstNode {
    private string _participantId;

    /// <summary>Initializes a sequence activation statement.</summary>
    public MermaidSequenceActivation(string participantId, bool active, MermaidSourceSpan span) : base(span) {
        _participantId = string.IsNullOrWhiteSpace(participantId) ? throw new ArgumentException("Participant id is required.", nameof(participantId)) : participantId;
        Active = active;
    }

    /// <summary>Gets or sets the participant id.</summary>
    public string ParticipantId { get => _participantId; set => _participantId = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Participant id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets whether this statement activates or deactivates the participant.</summary>
    public bool Active { get; set; }
}

/// <summary>
/// Describes a Mermaid sequence block control statement.
/// </summary>
public sealed class MermaidSequenceBlock : MermaidAstNode {
    /// <summary>Initializes a sequence block statement.</summary>
    public MermaidSequenceBlock(MermaidSequenceBlockKind kind, MermaidSourceSpan span) : base(span) => Kind = kind;

    /// <summary>Gets or sets the block statement kind.</summary>
    public MermaidSequenceBlockKind Kind { get; set; }

    /// <summary>Gets or sets the block text, branch text, or color value.</summary>
    public string? Text { get; set; }

    /// <summary>Gets or sets the nesting depth at this statement.</summary>
    public int Depth { get; set; }
}

/// <summary>
/// Describes Mermaid sequence autonumber configuration.
/// </summary>
public sealed class MermaidSequenceAutonumber : MermaidAstNode {
    /// <summary>Initializes autonumber configuration.</summary>
    public MermaidSequenceAutonumber(MermaidSourceSpan span) : base(span) {
    }

    /// <summary>Gets or sets the optional starting number text.</summary>
    public string? Start { get; set; }

    /// <summary>Gets or sets the optional increment text.</summary>
    public string? Increment { get; set; }
}

/// <summary>
/// Describes a Mermaid sequence actor menu link.
/// </summary>
public sealed class MermaidSequenceLink : MermaidAstNode {
    private string _participantId;

    /// <summary>Initializes a sequence link.</summary>
    public MermaidSequenceLink(string participantId, MermaidSourceSpan span) : base(span) {
        _participantId = string.IsNullOrWhiteSpace(participantId) ? throw new ArgumentException("Participant id is required.", nameof(participantId)) : participantId;
    }

    /// <summary>Gets or sets the linked participant id.</summary>
    public string ParticipantId { get => _participantId; set => _participantId = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Participant id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the link label for simple link syntax.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the link URL for simple link syntax.</summary>
    public string? Url { get; set; }

    /// <summary>Gets or sets the raw JSON object for advanced links syntax.</summary>
    public string? RawJson { get; set; }
}
