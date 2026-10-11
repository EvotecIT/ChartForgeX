namespace ChartForgeX.Mermaid;

/// <summary>Stable identifiers for diagram selection and native conversion diagnostics.</summary>
public static class MermaidDiagnosticCodes {
    /// <summary>The source declares an unknown diagram family.</summary>
    public const string UnknownDiagram = "CFXM001";

    /// <summary>The family is recognized but has no semantic parser or native renderer.</summary>
    public const string DiagnosticOnlyDiagram = "CFXM002";

    /// <summary>An authored layout engine is replaced by the native static layout.</summary>
    public const string UnsupportedLayout = "CFXM003";

    /// <summary>A parsed document could not be converted into a native artifact.</summary>
    public const string ConversionFailed = "CFXM004";
}
