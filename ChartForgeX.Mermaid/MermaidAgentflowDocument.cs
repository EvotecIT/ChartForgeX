using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>A source-preserving agent workflow; instructions and connector metadata are inert diagram data.</summary>
public sealed class MermaidAgentflowDocument : MermaidDocument {
    /// <summary>Gets or sets the authored direction; null uses top-to-bottom native layout.</summary>
    public string? Direction { get; set; }
    /// <summary>Gets nodes in first-reference order, excluding container identities.</summary>
    public List<MermaidAgentflowNode> Nodes { get; } = new();
    /// <summary>Gets flows in declaration order.</summary>
    public List<MermaidAgentflowFlow> Flows { get; } = new();
    /// <summary>Gets relationships in source order.</summary>
    public List<MermaidAgentflowEdge> Edges { get; } = new();
}

/// <summary>The semantic meaning of an Agentflow edge.</summary>
public enum MermaidAgentflowEdgeKind {
    /// <summary>Control or data proceeds to the target.</summary>
    Sequence,
    /// <summary>The source consults the target without transferring control.</summary>
    Reference,
    /// <summary>A failure, cancellation or escalation path.</summary>
    Failure
}

/// <summary>A task, tool, input, decision, reference document, action or connector.</summary>
public sealed class MermaidAgentflowNode : MermaidAstNode {
    /// <summary>Initializes a stable node identity.</summary>
    public MermaidAgentflowNode(string id, MermaidSourceSpan span) : base(span) {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Agentflow node id is required.", nameof(id)) : id;
        LabelSpan = span;
    }
    /// <summary>Gets the source identity.</summary>
    public string Id { get; }
    /// <summary>Gets or sets a display label; null displays the identity.</summary>
    public string? Label { get; set; }
    /// <summary>Gets or sets the authored shape name; the default is task.</summary>
    public string Shape { get; set; } = "task";
    /// <summary>Gets or sets the immediate containing flow.</summary>
    public string? ParentFlowId { get; set; }
    /// <summary>Gets or sets whether global containment excludes this node from flows.</summary>
    public bool IsGlobal { get; set; }
    /// <summary>Gets or sets whether the connector keyword declared this node.</summary>
    public bool IsConnector { get; set; }
    /// <summary>Gets inert metadata, with exact prototype-shaped mapping keys removed recursively.</summary>
    public Dictionary<string, MermaidMetadataValue> Metadata { get; } = new(StringComparer.Ordinal);
    internal MermaidSourceSpan LabelSpan { get; set; }
}

/// <summary>A nested workflow container with optional collapsed presentation.</summary>
public sealed class MermaidAgentflowFlow : MermaidAstNode {
    /// <summary>Initializes a stable flow identity.</summary>
    public MermaidAgentflowFlow(string id, MermaidSourceSpan span) : base(span) {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Agentflow flow id is required.", nameof(id)) : id;
    }
    /// <summary>Gets the source identity.</summary>
    public string Id { get; }
    /// <summary>Gets or sets a display label; null displays the identity.</summary>
    public string? Label { get; set; }
    /// <summary>Gets or sets the containing flow identity.</summary>
    public string? ParentId { get; set; }
    /// <summary>Gets or sets the locally authored direction.</summary>
    public string? Direction { get; set; }
    /// <summary>Gets or sets whether native presentation folds this flow into one summary node.</summary>
    public bool Collapsed { get; set; }
    /// <summary>Gets inert source metadata.</summary>
    public Dictionary<string, MermaidMetadataValue> Metadata { get; } = new(StringComparer.Ordinal);
}

/// <summary>A source-ordered workflow relationship.</summary>
public sealed class MermaidAgentflowEdge : MermaidAstNode {
    /// <summary>Initializes a workflow edge.</summary>
    public MermaidAgentflowEdge(string id, string sourceId, string targetId, MermaidAgentflowEdgeKind kind, MermaidSourceSpan span) : base(span) {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Agentflow edge id is required.", nameof(id)) : id;
        SourceId = string.IsNullOrWhiteSpace(sourceId) ? throw new ArgumentException("Agentflow edge source is required.", nameof(sourceId)) : sourceId;
        TargetId = string.IsNullOrWhiteSpace(targetId) ? throw new ArgumentException("Agentflow edge target is required.", nameof(targetId)) : targetId;
        if (!Enum.IsDefined(typeof(MermaidAgentflowEdgeKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Kind = kind;
    }
    /// <summary>Gets the explicit or generated relationship identity.</summary>
    public string Id { get; }
    /// <summary>Gets the source node or flow identity.</summary>
    public string SourceId { get; }
    /// <summary>Gets the target node or flow identity.</summary>
    public string TargetId { get; }
    /// <summary>Gets the authored edge meaning.</summary>
    public MermaidAgentflowEdgeKind Kind { get; }
    /// <summary>Gets or sets the authored branch label.</summary>
    public string? Label { get; set; }
    /// <summary>Gets inert source metadata.</summary>
    public Dictionary<string, MermaidMetadataValue> Metadata { get; } = new(StringComparer.Ordinal);
}
