using System;

namespace ChartForgeX.Mermaid;

/// <summary>Retains one source-authored annotation legend origin and its declaration location.</summary>
public sealed class MermaidWardleyAnnotationBox {
    /// <summary>Creates a normalized visibility/evolution origin; conversion validates the coordinate domain.</summary>
    public MermaidWardleyAnnotationBox(double visibility, double evolution, MermaidSourceSpan span) {
        Visibility = visibility; Evolution = evolution; Span = span;
    }
    /// <summary>Gets the normalized vertical visibility coordinate.</summary>
    public double Visibility { get; }
    /// <summary>Gets the normalized horizontal evolution coordinate.</summary>
    public double Evolution { get; }
    /// <summary>Gets the original source declaration span.</summary>
    public MermaidSourceSpan Span { get; }
}
