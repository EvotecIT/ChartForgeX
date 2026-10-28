using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid quadrant chart.
/// </summary>
public sealed class MermaidQuadrantDocument : MermaidDocument {
    /// <summary>Gets raw quadrant statements retained with source spans.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets or sets the optional Mermaid quadrant title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the low-side X axis label.</summary>
    public string? XAxisStart { get; set; }

    /// <summary>Gets or sets the high-side X axis label.</summary>
    public string? XAxisEnd { get; set; }

    /// <summary>Gets or sets the low-side Y axis label.</summary>
    public string? YAxisStart { get; set; }

    /// <summary>Gets or sets the high-side Y axis label.</summary>
    public string? YAxisEnd { get; set; }

    /// <summary>Gets quadrant labels keyed from 1 to 4.</summary>
    public Dictionary<int, string> QuadrantLabels { get; } = new();

    /// <summary>Gets source-ordered named point styles. The last complete definition of a name wins.</summary>
    public List<MermaidStyleDefinition> ClassDefinitions { get; } = new();

    /// <summary>Gets plotted quadrant points in source order.</summary>
    public List<MermaidQuadrantPoint> Points { get; } = new();
}

/// <summary>
/// Describes one Mermaid quadrant point.
/// </summary>
public sealed class MermaidQuadrantPoint : MermaidAstNode {
    private string _label;

    /// <summary>Initializes a quadrant point.</summary>
    public MermaidQuadrantPoint(string label, double x, double y, MermaidSourceSpan span) : base(span) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        X = x;
        Y = y;
    }

    /// <summary>Gets or sets the point label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the optional single inline class reference.</summary>
    public string? ClassName { get; set; }

    /// <summary>Gets direct point declarations, including diagnosed properties without a native mapping.</summary>
    public Dictionary<string, string> Styles { get; } = new();

    /// <summary>Gets or sets the normalized X position.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the normalized Y position.</summary>
    public double Y { get; set; }
}
