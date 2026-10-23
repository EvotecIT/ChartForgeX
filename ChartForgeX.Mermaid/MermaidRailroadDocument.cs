using System;
using System.Collections.Generic;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>The supported railroad source frontend, independent of its native grammar model.</summary>
public enum MermaidRailroadDialect {
    /// <summary>Explicit railroad constructors.</summary>
    IntermediateRepresentation,
    /// <summary>W3C and ISO-style Extended Backus-Naur Form.</summary>
    Ebnf,
    /// <summary>Mermaid's semicolon-terminated ABNF subset.</summary>
    Abnf,
    /// <summary>Parsing Expression Grammar with ordered choice and predicates.</summary>
    Peg
}

/// <summary>A source-preserving railroad grammar lowered to the reusable native expression model.</summary>
public sealed class MermaidRailroadDocument : MermaidDocument {
    /// <summary>Gets or sets the selected source notation.</summary>
    public MermaidRailroadDialect Dialect { get; set; }
    /// <summary>Gets or sets the optional diagram title.</summary>
    public string? Title { get; set; }
    /// <summary>Gets rules in source order.</summary>
    public List<MermaidRailroadRule> Rules { get; } = new();
    /// <summary>Gets raw rule statements with original source spans.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();
}

/// <summary>A source-located rule whose references remain symbolic.</summary>
public sealed class MermaidRailroadRule : MermaidAstNode {
    /// <summary>Initializes a rule.</summary>
    public MermaidRailroadRule(string id, MermaidRailroadExpression definition, MermaidSourceSpan span) : base(span) {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("A railroad rule identity is required.", nameof(id)) : id; Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }
    /// <summary>Gets the authored stable rule identity.</summary>
    public string Id { get; }
    /// <summary>Gets the typed located expression.</summary>
    public MermaidRailroadExpression Definition { get; }
}

/// <summary>A source location for an immutable native grammar expression and its semantic children.</summary>
public sealed class MermaidRailroadExpression : MermaidAstNode {
    /// <summary>Initializes a source expression.</summary>
    public MermaidRailroadExpression(RailroadExpression expression, MermaidSourceSpan span, IEnumerable<MermaidRailroadExpression>? children = null) : base(span) {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        var copy = children == null ? new List<MermaidRailroadExpression>() : new List<MermaidRailroadExpression>(children);
        if (copy.Count != expression.Children.Count) throw new ArgumentException("Located children must match the immutable grammar expression.", nameof(children));
        for (var index = 0; index < copy.Count; index++) if (copy[index] == null || !ReferenceEquals(copy[index].Expression, expression.Children[index]))
            throw new ArgumentException("Located children must retain the corresponding native expression identities.", nameof(children));
        Children = copy.AsReadOnly();
    }
    /// <summary>Gets the reusable native semantic expression.</summary>
    public RailroadExpression Expression { get; }
    /// <summary>Gets its located semantic children in order.</summary>
    public IReadOnlyList<MermaidRailroadExpression> Children { get; }
}
