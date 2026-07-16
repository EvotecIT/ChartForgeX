namespace ChartForgeX.Markup;

/// <summary>
/// Describes a markup parser diagnostic.
/// </summary>
public sealed class MarkupDiagnostic {
    /// <summary>Gets or sets the one-based source line.</summary>
    public int Line { get; set; }

    /// <summary>Gets or sets the diagnostic severity.</summary>
    public MarkupDiagnosticSeverity Severity { get; set; }

    /// <summary>Gets or sets the diagnostic message.</summary>
    public string Message { get; set; } = string.Empty;
}
