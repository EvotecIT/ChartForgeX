using System;
using ChartForgeX.Themes;

namespace ChartForgeX.Mermaid;

/// <summary>Controls whether topology output keeps measured text sizes or fits the authored frame.</summary>
public enum MermaidTopologySizing {
    /// <summary>Expands the output canvas to the native measured content. Hosts may scroll it.</summary>
    Natural,
    /// <summary>Scales the completed content into the family's width and height.</summary>
    FixedViewport
}

/// <summary>Cross-family host overrides applied after source configuration and before native preparation.</summary>
/// <remarks>Null fields inherit source/native defaults. A family presentation overrides the shared presentation.
/// Themes are immutable native token snapshots; this API introduces no separate palette or font engine.</remarks>
public sealed class MermaidPresentationOptions {
    /// <summary>Gets or sets a native paired theme; null retains native colors and source color settings.</summary>
    public VisualTheme? Theme { get; set; }
    /// <summary>Gets or sets the native light/dark mode.</summary>
    public VisualThemeMode? ThemeMode { get; set; }
    /// <summary>Gets or sets the requested font family. Native font resolution still applies.</summary>
    public string? FontFamily { get; set; }
    /// <summary>Gets or sets the data-label size in pixels, scaling the native typography proportionally.</summary>
    public double? FontSize { get; set; }
    /// <summary>Gets or sets sizing for topology-backed families. Null uses Natural. Other native families retain their own layout contract.</summary>
    public MermaidTopologySizing? TopologySizing { get; set; }

    internal static MermaidPresentationOptions? Merge(MermaidPresentationOptions? defaults, MermaidPresentationOptions? family) {
        if (defaults == null && family == null) return null;
        var result = new MermaidPresentationOptions {
            Theme = family?.Theme ?? defaults?.Theme, ThemeMode = family?.ThemeMode ?? defaults?.ThemeMode,
            FontFamily = family?.FontFamily ?? defaults?.FontFamily, FontSize = family?.FontSize ?? defaults?.FontSize,
            TopologySizing = family?.TopologySizing ?? defaults?.TopologySizing
        };
        result.Validate();
        return result;
    }
    internal void Validate() {
        if (ThemeMode.HasValue && !Enum.IsDefined(typeof(VisualThemeMode), ThemeMode.Value)) throw new ArgumentOutOfRangeException(nameof(ThemeMode));
        if (TopologySizing.HasValue && !Enum.IsDefined(typeof(MermaidTopologySizing), TopologySizing.Value)) throw new ArgumentOutOfRangeException(nameof(TopologySizing));
        if (FontFamily != null && string.IsNullOrWhiteSpace(FontFamily)) throw new ArgumentException("A host font family must contain text.", nameof(FontFamily));
        if (FontSize.HasValue && (double.IsNaN(FontSize.Value) || double.IsInfinity(FontSize.Value) || FontSize.Value <= 0 || FontSize.Value > 512))
            throw new ArgumentOutOfRangeException(nameof(FontSize), "A host font size must be finite, positive and at most 512 pixels.");
    }
}

/// <summary>Determines whether recognized source losses may produce a native artifact.</summary>
public enum MermaidWarningPolicy {
    /// <summary>Returns warnings alongside the supported native output.</summary>
    Allow,
    /// <summary>Rejects artifact creation when any source warning is present.</summary>
    Reject
}
