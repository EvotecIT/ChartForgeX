using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Represents a Mermaid parse result.
/// </summary>
/// <typeparam name="TDocument">The Mermaid document type.</typeparam>
public sealed class MermaidParseResult<TDocument> where TDocument : MermaidDocument {
    /// <summary>Gets or sets the parsed Mermaid document.</summary>
    public TDocument? Document { get; set; }

    /// <summary>Gets parser diagnostics.</summary>
    public List<MermaidDiagnostic> Diagnostics { get; } = new();

    /// <summary>Gets whether parsing produced errors.</summary>
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error);
}
