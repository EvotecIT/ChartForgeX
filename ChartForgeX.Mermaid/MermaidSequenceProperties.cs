using System;

namespace ChartForgeX.Mermaid;

/// <summary>Retains one inert participant properties JSON declaration and its original location.</summary>
public sealed class MermaidSequenceProperties : MermaidAstNode {
    private string _participantId, _rawJson;
    /// <summary>Initializes a located participant properties declaration.</summary>
    public MermaidSequenceProperties(string participantId, string rawJson, MermaidSourceSpan span) : base(span) {
        _participantId = string.IsNullOrWhiteSpace(participantId) ? throw new ArgumentException("Participant id is required.", nameof(participantId)) : participantId;
        _rawJson = rawJson ?? throw new ArgumentNullException(nameof(rawJson));
    }
    /// <summary>Gets or sets the participant identifier.</summary>
    public string ParticipantId { get => _participantId; set => _participantId = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Participant id is required.", nameof(value)) : value; }
    /// <summary>Gets or sets the exact authored JSON object text, without evaluating its contents.</summary>
    public string RawJson { get => _rawJson; set => _rawJson = value ?? throw new ArgumentNullException(nameof(value)); }
}
