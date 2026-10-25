using System;

namespace ChartForgeX.Mermaid;

/// <summary>A visible diagram note, separate from the graph's semantic nodes.</summary>
public sealed class MermaidDiagramNote : MermaidAstNode {
    /// <summary>Creates a note with its authored text, optional attachment, and source location.</summary>
    public MermaidDiagramNote(string text, string? nodeId, bool leftOfNode, MermaidSourceSpan span)
        : this(text, nodeId, leftOfNode, span, null) {
    }

    /// <summary>Creates a note with an optional authored identity, separate from its attached diagram node.</summary>
    /// <param name="text">Complete visible note text, including literal line breaks.</param>
    /// <param name="nodeId">The attached diagram node identity, or null for a floating note.</param>
    /// <param name="leftOfNode">Whether an attached note uses the left gutter.</param>
    /// <param name="span">The original source location.</param>
    /// <param name="id">An authored note identity; null retains automatic native identity.</param>
    public MermaidDiagramNote(string text, string? nodeId, bool leftOfNode, MermaidSourceSpan span, string? id) : base(span) {
        if (id != null && string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Authored note ids must not be empty.", nameof(id));
        Id = id;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        NodeId = nodeId;
        LeftOfNode = leftOfNode;
    }

    /// <summary>Gets the authored note identity, or null when the native converter assigns one.</summary>
    public string? Id { get; }

    /// <summary>Gets the visible note text, including authored line breaks.</summary>
    public string Text { get; }

    /// <summary>Gets the attached diagram node id, or null for a floating note.</summary>
    public string? NodeId { get; }

    /// <summary>Gets whether an attached note is placed to the left of its node.</summary>
    public bool LeftOfNode { get; }
}
