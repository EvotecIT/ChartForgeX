using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>
/// Defines topology rendering options.
/// </summary>
public sealed class TopologyRenderOptions {
    /// <summary>
    /// Creates render options from a reusable topology view preset.
    /// </summary>
    /// <param name="preset">The preset to apply.</param>
    /// <returns>Topology render options.</returns>
    public static TopologyRenderOptions FromPreset(TopologyViewPreset preset) {
        return new TopologyRenderOptions().ApplyPreset(preset);
    }

    /// <summary>Gets or sets whether the chart title should be rendered.</summary>
    public bool IncludeTitle { get; set; } = true;

    /// <summary>Gets or sets whether the legend should be rendered.</summary>
    public bool IncludeLegend { get; set; } = true;

    /// <summary>Gets or sets whether group containers should be rendered.</summary>
    public bool IncludeGroups { get; set; } = true;

    /// <summary>Gets or sets whether group labels should be rendered.</summary>
    public bool IncludeGroupLabels { get; set; } = true;

    /// <summary>Gets or sets whether node labels should be rendered.</summary>
    public bool IncludeNodeLabels { get; set; } = true;

    /// <summary>Gets or sets whether tile nodes should render their subtitle as a compact chip below the tile label.</summary>
    public bool IncludeTileSubtitles { get; set; }

    /// <summary>Gets or sets how card and compact-card node subtitles should be rendered.</summary>
    public TopologyCardSubtitleMode CardSubtitleMode { get; set; } = TopologyCardSubtitleMode.Text;

    /// <summary>Gets or sets whether edge labels should be rendered.</summary>
    public bool IncludeEdgeLabels { get; set; } = true;

    /// <summary>Gets or sets whether edge labels should render a subtle background plate.</summary>
    public bool IncludeEdgeLabelBackplates { get; set; } = true;

    /// <summary>Gets or sets whether direction markers should be rendered on directed edges.</summary>
    public bool IncludeDirectionMarkers { get; set; } = true;

    /// <summary>Gets or sets whether node status badges should be rendered.</summary>
    public bool IncludeStatusBadges { get; set; } = true;

    /// <summary>Gets or sets whether SVG title tooltip elements should be rendered.</summary>
    public bool IncludeTooltips { get; set; } = true;

    /// <summary>Gets or sets whether scoped CSS should be emitted.</summary>
    public bool IncludeCss { get; set; } = true;

    /// <summary>Gets or sets whether element metadata and metrics should be emitted as SVG data attributes.</summary>
    public bool IncludeDataAttributes { get; set; } = true;

    /// <summary>Gets or sets whether the SVG should include responsive sizing style.</summary>
    public bool UseResponsiveSvg { get; set; } = true;

    /// <summary>Gets or sets whether complete HTML pages should include lightweight selection interactions.</summary>
    public bool EnableHtmlInteractions { get; set; } = true;

    /// <summary>Gets or sets whether links should open in a new tab.</summary>
    public bool OpenLinksInNewTab { get; set; }

    /// <summary>Gets or sets the CSS class prefix.</summary>
    public string? CssClassPrefix { get; set; } = "cfx-topology";

    /// <summary>Gets or sets an optional focused topology view.</summary>
    public TopologyView? View { get; set; }

    /// <summary>Gets or sets a reusable render preset.</summary>
    public TopologyViewPreset Preset { get; set; } = TopologyViewPreset.Default;

    /// <summary>Gets or sets how topology nodes should be presented.</summary>
    public TopologyNodeDisplayMode NodeDisplayMode { get; set; } = TopologyNodeDisplayMode.Card;

    /// <summary>Gets or sets how the topology legend should be sourced.</summary>
    public TopologyLegendMode LegendMode { get; set; } = TopologyLegendMode.Explicit;

    /// <summary>Gets or sets the edge metric key to use as the primary edge label. When unset, the edge label is used.</summary>
    public string? EdgeLabelMetricKey { get; set; }

    /// <summary>Gets or sets the edge metric key to use as the secondary edge label. When unset, the edge secondary label is used.</summary>
    public string? EdgeSecondaryLabelMetricKey { get; set; }

    /// <summary>Gets or sets the edge metric key to use as the tertiary edge label. When unset, the edge tertiary label is used.</summary>
    public string? EdgeTertiaryLabelMetricKey { get; set; }

    /// <summary>Gets health statuses to highlight while dimming non-matching topology elements.</summary>
    public List<TopologyHealthStatus> HighlightStatuses { get; } = new();

    /// <summary>Gets group ids to highlight while dimming non-matching topology elements.</summary>
    public List<string> HighlightGroupIds { get; } = new();

    /// <summary>Gets node ids to highlight while dimming non-matching topology elements.</summary>
    public List<string> HighlightNodeIds { get; } = new();

    /// <summary>Gets edge ids to highlight while dimming non-matching topology elements.</summary>
    public List<string> HighlightEdgeIds { get; } = new();

    /// <summary>Gets or sets whether edges connected to highlighted nodes should also be highlighted.</summary>
    public bool HighlightConnectedEdges { get; set; } = true;

    /// <summary>Gets group ids to mark as selected without filtering or dimming the topology.</summary>
    public List<string> SelectedGroupIds { get; } = new();

    /// <summary>Gets node ids to mark as selected without filtering or dimming the topology.</summary>
    public List<string> SelectedNodeIds { get; } = new();

    /// <summary>Gets edge ids to mark as selected without filtering or dimming the topology.</summary>
    public List<string> SelectedEdgeIds { get; } = new();

    /// <summary>Gets or sets the opacity used for non-highlighted elements when highlighting is active.</summary>
    public double DimmedOpacity { get; set; } = 0.28;

    /// <summary>Gets or sets the PNG supersampling scale used by the topology PNG renderer.</summary>
    public int PngSupersamplingScale { get; set; } = 2;

    /// <summary>Gets or sets the PNG output scale used by the topology PNG renderer.</summary>
    public int PngOutputScale { get; set; } = 1;
}
