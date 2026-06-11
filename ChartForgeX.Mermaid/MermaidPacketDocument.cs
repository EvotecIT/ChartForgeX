using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid packet diagram.
/// </summary>
public sealed class MermaidPacketDocument : MermaidDocument {
    private string? _title;

    /// <summary>Gets packet statements retained in source order.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets parsed packet fields.</summary>
    public List<MermaidPacketField> Fields { get; } = new();

    /// <summary>Gets or sets the optional packet title statement.</summary>
    public string? Title { get => _title; set => _title = value; }
}

/// <summary>
/// Describes one Mermaid packet field.
/// </summary>
public sealed class MermaidPacketField {
    private string _label;

    /// <summary>Initializes a packet field.</summary>
    public MermaidPacketField(int startBit, int endBit, string label, MermaidSourceSpan span) {
        if (startBit < 0) throw new ArgumentOutOfRangeException(nameof(startBit), startBit, "Packet field start bit must be zero or greater.");
        if (endBit < startBit) throw new ArgumentOutOfRangeException(nameof(endBit), endBit, "Packet field end bit must be greater than or equal to the start bit.");
        StartBit = startBit;
        EndBit = endBit;
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Span = span;
    }

    /// <summary>Gets the first bit covered by this field.</summary>
    public int StartBit { get; }

    /// <summary>Gets the last bit covered by this field.</summary>
    public int EndBit { get; }

    /// <summary>Gets the covered bit count.</summary>
    public int BitLength => EndBit - StartBit + 1;

    /// <summary>Gets or sets the field label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets the source span for the field statement.</summary>
    public MermaidSourceSpan Span { get; }
}
