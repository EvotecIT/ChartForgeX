using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid Sankey diagram document.
/// </summary>
public sealed class MermaidSankeyDocument : MermaidDocument {
    /// <summary>Gets retained raw CSV rows.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets parsed source-to-target weighted links.</summary>
    public List<MermaidSankeyLink> Links { get; } = new();
}

/// <summary>
/// Describes one Mermaid Sankey CSV link row.
/// </summary>
public sealed class MermaidSankeyLink : MermaidAstNode {
    private string _source;
    private string _target;

    /// <summary>Initializes a Mermaid Sankey link.</summary>
    public MermaidSankeyLink(string source, string target, double value, MermaidSourceSpan span) : base(span) {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _target = target ?? throw new ArgumentNullException(nameof(target));
        Value = value;
    }

    /// <summary>Gets or sets the source node label.</summary>
    public string Source { get => _source; set => _source = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the target node label.</summary>
    public string Target { get => _target; set => _target = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the positive link value.</summary>
    public double Value { get; set; }
}
