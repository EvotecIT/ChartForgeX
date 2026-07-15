namespace ChartForgeX.Diagnostics;

/// <summary>
/// Defines severity shared by visual parsers and diagnostics.
/// </summary>
public enum VisualDiagnosticSeverity {
    /// <summary>Informational note.</summary>
    Information,
    /// <summary>Warning that does not block a usable result.</summary>
    Warning,
    /// <summary>Error that blocks a valid result.</summary>
    Error
}
