namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Configures the self-contained HTML graph explorer adapter.
/// </summary>
public sealed class HtmlGraphExplorerOptions {
    /// <summary>Gets or sets an optional page title for complete HTML documents.</summary>
    public string? PageTitle { get; set; }

    /// <summary>Gets or sets an optional CSP nonce for inline script elements.</summary>
    public string? ScriptNonce { get; set; }

    /// <summary>Gets or sets an optional deterministic SVG id scope for repeated embeds of the same scene on one page.</summary>
    public string? IdScope { get; set; }

    /// <summary>Gets or sets the initial graph renderer backend advertised to host code.</summary>
    public HtmlGraphRenderBackend RenderBackend { get; set; } = HtmlGraphRenderBackend.Svg;

    /// <summary>Gets or sets whether large SVG scenes may switch to the dependency-free Canvas runtime.</summary>
    public bool AllowCanvasFallback { get; set; } = true;

    /// <summary>Gets or sets whether the built-in search field should be rendered.</summary>
    public bool IncludeSearch { get; set; } = true;

    /// <summary>Gets or sets whether basic status and kind filter controls should be rendered.</summary>
    public bool IncludeFilters { get; set; } = true;

    /// <summary>Gets or sets whether clustering controls should be rendered when the scene advertises clusters.</summary>
    public bool IncludeClusterControls { get; set; } = true;

    /// <summary>Gets or sets whether physics controls should be rendered when runtime physics is enabled.</summary>
    public bool IncludePhysicsControls { get; set; } = true;
}

/// <summary>
/// Names browser rendering backends supported or planned by the graph explorer adapter.
/// </summary>
public enum HtmlGraphRenderBackend {
    /// <summary>Render the initial graph as inline SVG.</summary>
    Svg,

    /// <summary>Reserve the scene for a Canvas-backed runtime renderer.</summary>
    Canvas,

    /// <summary>Request the planned WebGL-backed runtime renderer. Until it ships, the adapter routes this request to Canvas.</summary>
    WebGl
}
