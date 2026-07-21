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

    /// <summary>Gets or sets whether viewport fit and zoom controls should be rendered when viewport behavior is enabled.</summary>
    public bool IncludeViewportControls { get; set; } = true;

    /// <summary>Gets or sets whether physics controls should be rendered when runtime physics is enabled.</summary>
    public bool IncludePhysicsControls { get; set; } = true;

    /// <summary>Gets or sets whether an expandable development-time physics configurator should be rendered.</summary>
    public bool IncludePhysicsConfigurator { get; set; }

    /// <summary>Gets or sets whether the opt-in graph authoring controls should be rendered when manipulation is enabled.</summary>
    public bool IncludeManipulationControls { get; set; } = true;

    /// <summary>Gets or sets the explorer color theme used when the document first opens.</summary>
    public HtmlGraphExplorerTheme Theme { get; set; } = HtmlGraphExplorerTheme.System;

    /// <summary>Gets or sets whether the built-in appearance control should let people choose system, light, or dark mode.</summary>
    public bool IncludeThemeToggle { get; set; } = true;

    /// <summary>Gets or sets whether an appearance choice made in the browser should be reused by other graph explorers on the same origin.</summary>
    public bool PersistThemePreference { get; set; } = true;

    /// <summary>Gets or sets whether reusable graph interaction state should be restored from and saved to browser local storage.</summary>
    public bool PersistInteractionState { get; set; }

    /// <summary>Gets or sets an optional browser storage key used when <see cref="PersistInteractionState"/> is enabled.</summary>
    public string? InteractionStateStorageKey { get; set; }
}

/// <summary>
/// Names the initial color theme of a browser graph explorer.
/// </summary>
public enum HtmlGraphExplorerTheme {
    /// <summary>Follow the browser or operating-system color preference until the user chooses another theme.</summary>
    System,

    /// <summary>Use the light color theme.</summary>
    Light,

    /// <summary>Use the dark color theme.</summary>
    Dark
}

/// <summary>
/// Names browser rendering backends supported or planned by the graph explorer adapter.
/// </summary>
public enum HtmlGraphRenderBackend {
    /// <summary>Render the initial graph as inline SVG.</summary>
    Svg,

    /// <summary>Reserve the scene for a Canvas-backed runtime renderer.</summary>
    Canvas,

    /// <summary>Render large graph geometry with the dependency-free WebGL2 runtime, falling back to Canvas when unavailable.</summary>
    WebGl
}
