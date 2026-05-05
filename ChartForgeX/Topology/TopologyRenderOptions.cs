namespace ChartForgeX.Topology;

/// <summary>
/// Defines topology rendering options.
/// </summary>
public sealed class TopologyRenderOptions {
    /// <summary>Gets or sets whether the chart title should be rendered.</summary>
    public bool IncludeTitle { get; set; } = true;

    /// <summary>Gets or sets whether the legend should be rendered.</summary>
    public bool IncludeLegend { get; set; } = true;

    /// <summary>Gets or sets whether SVG title tooltip elements should be rendered.</summary>
    public bool IncludeTooltips { get; set; } = true;

    /// <summary>Gets or sets whether scoped CSS should be emitted.</summary>
    public bool IncludeCss { get; set; } = true;

    /// <summary>Gets or sets whether the SVG should include responsive sizing style.</summary>
    public bool UseResponsiveSvg { get; set; } = true;

    /// <summary>Gets or sets whether links should open in a new tab.</summary>
    public bool OpenLinksInNewTab { get; set; }

    /// <summary>Gets or sets the CSS class prefix.</summary>
    public string? CssClassPrefix { get; set; } = "cfx-topology";

    /// <summary>Gets or sets an optional focused topology view.</summary>
    public TopologyView? View { get; set; }

    /// <summary>Gets or sets the PNG supersampling scale used by the topology PNG renderer.</summary>
    public int PngSupersamplingScale { get; set; } = 2;

    /// <summary>Gets or sets the PNG output scale used by the topology PNG renderer.</summary>
    public int PngOutputScale { get; set; } = 1;
}
