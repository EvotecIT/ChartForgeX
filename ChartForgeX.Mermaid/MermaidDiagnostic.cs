namespace ChartForgeX.Mermaid;

/// <summary>
/// Defines Mermaid parser diagnostic severity.
/// </summary>
public enum MermaidDiagnosticSeverity {
    /// <summary>Informational note.</summary>
    Information,
    /// <summary>Warning that does not block inspection.</summary>
    Warning,
    /// <summary>Error that blocks a supported parse result.</summary>
    Error
}

/// <summary>
/// Describes a Mermaid parser diagnostic.
/// </summary>
public sealed class MermaidDiagnostic {
    /// <summary>Gets or sets diagnostic severity.</summary>
    public MermaidDiagnosticSeverity Severity { get; set; }

    /// <summary>Gets or sets the source span associated with the diagnostic.</summary>
    public MermaidSourceSpan Span { get; set; } = new(1, 1, 0);

    /// <summary>Gets or sets the diagnostic message.</summary>
    public string Message { get; set; } = string.Empty;
}
