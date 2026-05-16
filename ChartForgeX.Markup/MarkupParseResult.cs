using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Markup;

/// <summary>
/// Represents the result of parsing ChartForgeX markup.
/// </summary>
public sealed class MarkupParseResult<TDocument> where TDocument : class {
    /// <summary>Gets or sets the parsed document.</summary>
    public TDocument? Document { get; set; }

    /// <summary>Gets parser diagnostics.</summary>
    public List<MarkupDiagnostic> Diagnostics { get; } = new();

    /// <summary>Gets whether parsing produced errors.</summary>
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Error);
}
