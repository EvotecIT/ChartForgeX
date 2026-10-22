using System;

namespace ChartForgeX.Mermaid;

/// <summary>Identifies one ordered region inside a composite state.</summary>
public sealed class MermaidStateRegion : MermaidAstNode {
    /// <summary>Creates a region whose identity remains stable across parsing and native projection.</summary>
    public MermaidStateRegion(string id, string compositeStateId, int index, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Region id must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(compositeStateId)) throw new ArgumentException("Composite state id must not be empty.", nameof(compositeStateId));
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        Id = id;
        CompositeStateId = compositeStateId;
        Index = index;
    }

    /// <summary>Gets the stable region identity.</summary>
    public string Id { get; }
    /// <summary>Gets the containing composite state identity.</summary>
    public string CompositeStateId { get; }
    /// <summary>Gets the zero-based source order within that composite.</summary>
    public int Index { get; }
    /// <summary>Gets or sets the local layout direction, or null to inherit the composite direction.</summary>
    public string? Direction { get; set; }
}
