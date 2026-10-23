using System;

namespace ChartForgeX.Mermaid;

/// <summary>A native relationship paint and label-offset update for the identified C4 endpoints.</summary>
public sealed class MermaidC4RelationshipStyle : MermaidAstNode {
    /// <summary>Initializes an update.</summary>
    public MermaidC4RelationshipStyle(string sourceAlias, string targetAlias, MermaidSourceSpan span) : base(span) {
        SourceAlias = sourceAlias ?? throw new ArgumentNullException(nameof(sourceAlias)); TargetAlias = targetAlias ?? throw new ArgumentNullException(nameof(targetAlias));
    }
    /// <summary>Gets the source element identity.</summary>
    public string SourceAlias { get; }
    /// <summary>Gets the target element identity.</summary>
    public string TargetAlias { get; }
    /// <summary>Gets or sets the label paint.</summary>
    public string? TextColor { get; set; }
    /// <summary>Gets or sets the connector paint.</summary>
    public string? LineColor { get; set; }
    /// <summary>Gets or sets the explicit horizontal label offset.</summary>
    public double? OffsetX { get; set; }
    /// <summary>Gets or sets the explicit vertical label offset.</summary>
    public double? OffsetY { get; set; }
}
