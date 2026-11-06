using System;
using ChartForgeX.Topology;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Defines static core rendering options shared by artifacts and host adapters.</summary>
public sealed class VisualArtifactRenderOptions {
    private VisualArtifactHtmlSizing _htmlSizing;

    /// <summary>Gets or sets how standalone HTML fits completed SVG geometry to its host.</summary>
    /// <remarks>Automatic preserves sequence size in a scrollable region and keeps other families' existing sizing.</remarks>
    public VisualArtifactHtmlSizing HtmlSizing {
        get => _htmlSizing;
        set => _htmlSizing = Enum.IsDefined(typeof(VisualArtifactHtmlSizing), value)
            ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }
    /// <summary>Gets or sets topology-specific rendering options.</summary>
    public TopologyRenderOptions? Topology { get; set; }
    /// <summary>Gets or sets dependency-free raster encoding options.</summary>
    public RasterImageOptions? Raster { get; set; }
    /// <summary>Gets or sets whether SVG embeds the bundled product font faces.</summary>
    /// <remarks>Off by default: hosts supply the faces once (<see cref="ChartForgeX.Typography.ChartFontAssets.StyleSheet"/>)
    /// or cannot use CSS fonts. Saved SVG files turn it on so they render their measured typography standalone.</remarks>
    public bool EmbedProductFonts { get; set; }
}

/// <summary>Controls host sizing without reflowing a completed visual scene or changing SVG/PNG exports.</summary>
public enum VisualArtifactHtmlSizing {
    /// <summary>Preserves sequence size; other families retain their existing standalone HTML sizing.</summary>
    Automatic,
    /// <summary>Scales the completed visual down to fit the available page width.</summary>
    FitToWidth,
    /// <summary>Keeps the completed SVG's authored size in a keyboard-accessible horizontal scroll region.</summary>
    PreserveSize
}
