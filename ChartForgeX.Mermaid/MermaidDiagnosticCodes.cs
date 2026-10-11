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

    /// <summary>A source setting is retained without native application.</summary>
    public const string UnsupportedConfiguration = "CFXM005";

    /// <summary>Source configuration is malformed, has an invalid supported value or exceeds a resource limit.</summary>
    public const string InvalidConfiguration = "CFXM006";

    /// <summary>A notation statement is retained without an exact native rendering.</summary>
    public const string UnsupportedStatement = "CFXM007";

    /// <summary>A recognized notation statement or block boundary is invalid.</summary>
    public const string InvalidStatement = "CFXM008";
}
