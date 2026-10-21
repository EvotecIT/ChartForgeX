using System;

namespace ChartForgeX.Mermaid;

/// <summary>A visible diagram note, separate from the graph's semantic nodes.</summary>
public sealed class MermaidDiagramNote : MermaidAstNode {
    /// <summary>Creates a note with its authored text, optional attachment, and source location.</summary>
    public MermaidDiagramNote(string text, string? nodeId, bool leftOfNode, MermaidSourceSpan span) : base(span) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        NodeId = nodeId;
        LeftOfNode = leftOfNode;
    }

    /// <summary>Gets the visible note text, including authored line breaks.</summary>
    public string Text { get; }

    /// <summary>Gets the attached class or state id, or null for a floating note.</summary>
    public string? NodeId { get; }

    /// <summary>Gets whether an attached note is placed to the left of its node.</summary>
    public bool LeftOfNode { get; }
}
