using System;
using System.Collections.Generic;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>One authored Architecture align occurrence with its ordered members and original source span.</summary>
public sealed class MermaidArchitectureAlignment : MermaidAstNode {
    private TopologyAlignmentKind _kind;
    /// <summary>Creates a row/column occurrence with detached exact member IDs.</summary>
    public MermaidArchitectureAlignment(TopologyAlignmentKind kind, IEnumerable<string> members, MermaidSourceSpan span) : base(span) {
        Kind = kind;
        if (members == null) throw new ArgumentNullException(nameof(members));
        foreach (var member in members) { CheckMemberCount(Members.Count + 1, span); Members.Add(member); }
    }
    internal static void CheckMemberCount(int count, MermaidSourceSpan span) {
        const int maximum = ChartForgeX.VisualArtifacts.VisualArtifactInterchangeValidation.MaximumNodes;
        if (count > maximum) throw new MermaidResourceLimitException("Interchange.MaximumNodes", maximum, count, span);
    }
    /// <summary>Gets or sets the global row or column semantics.</summary>
    public TopologyAlignmentKind Kind {
        get => _kind;
        set { if (!Enum.IsDefined(typeof(TopologyAlignmentKind), value)) throw new ArgumentOutOfRangeException(nameof(value)); _kind = value; }
    }
    /// <summary>Gets exact service/junction IDs in authored order.</summary>
    public List<string> Members { get; } = new();
}
