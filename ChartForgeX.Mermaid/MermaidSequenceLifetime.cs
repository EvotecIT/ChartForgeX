using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>Describes a participant creation or destruction immediately before its associated message.</summary>
public sealed class MermaidSequenceLifetime : MermaidAstNode {
    /// <summary>Initializes a participant lifetime directive.</summary>
    public MermaidSequenceLifetime(string participantId, bool creates, MermaidSourceSpan span) : base(span) {
        ParticipantId = string.IsNullOrWhiteSpace(participantId) ? throw new ArgumentException("Participant id is required.", nameof(participantId)) : participantId;
        Creates = creates;
    }

    /// <summary>Gets the affected participant identifier.</summary>
    public string ParticipantId { get; }

    /// <summary>Gets whether the directive creates rather than destroys the participant.</summary>
    public bool Creates { get; }

    /// <summary>Gets the zero-based index of the associated creating or destroying message.</summary>
    public int MessageIndex { get; internal set; }
}

/// <summary>Describes a vertical participant box in a Mermaid sequence diagram.</summary>
public sealed class MermaidSequenceParticipantBox : MermaidAstNode {
    /// <summary>Initializes a participant box.</summary>
    public MermaidSequenceParticipantBox(MermaidSourceSpan span) : base(span) { }

    /// <summary>Gets or sets the visible group label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the authored color, when present.</summary>
    public string? Color { get; set; }

    /// <summary>Gets identifiers in the box's contiguous participant range.</summary>
    public List<string> ParticipantIds { get; } = new();

    /// <summary>Gets the closing statement's location.</summary>
    public MermaidSourceSpan? EndSpan { get; internal set; }
}
