using System.Collections.Generic;
using System.Linq;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Markup;

/// <summary>
/// Represents the result of parsing Markdown into product-neutral visual artifacts.
/// </summary>
public sealed class VisualMarkupParseResult {
    /// <summary>Gets parsed visual artifacts.</summary>
    public List<VisualArtifact> Artifacts { get; } = new();

    /// <summary>Gets parser and scanner diagnostics.</summary>
    public List<MarkupDiagnostic> Diagnostics { get; } = new();

    /// <summary>Gets whether parsing produced errors.</summary>
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Error);
}
