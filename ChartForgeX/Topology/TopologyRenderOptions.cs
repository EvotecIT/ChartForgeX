using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Topology;

/// <summary>
/// Defines topology rendering options.
/// </summary>
public sealed class TopologyRenderOptions {
    private ChartLineVisualStyle? _edgeVisualStyle;

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

    /// <summary>Gets or sets whether icon-mode nodes should render compact labels below the icon.</summary>
    public bool IncludeIconLabels { get; set; }

    /// <summary>Gets or sets whether tile nodes should render their subtitle as a compact chip below the tile label.</summary>
    public bool IncludeTileSubtitles { get; set; }

    /// <summary>Gets or sets whether explicit line breaks in node labels and subtitles should be rendered as multiple SVG/PNG text rows.</summary>
    public bool AllowMultilineNodeLabels { get; set; } = true;

    /// <summary>Gets or sets whether node labels and subtitles can wrap at word boundaries when they exceed their available width.</summary>
    public bool WrapNodeLabels { get; set; }

    /// <summary>Gets or sets the maximum number of rendered title rows for card, compact-card, tile, and icon-label node text.</summary>
    public int MaxNodeLabelLines { get; set; } = 2;

    /// <summary>Gets or sets the maximum number of rendered subtitle rows for card and tile node text.</summary>
    public int MaxNodeSubtitleLines { get; set; } = 2;

    /// <summary>Gets or sets how card and compact-card node subtitles should be rendered.</summary>
    public TopologyCardSubtitleMode CardSubtitleMode { get; set; } = TopologyCardSubtitleMode.Text;

    /// <summary>Gets or sets whether edge labels should be rendered.</summary>
    public bool IncludeEdgeLabels { get; set; } = true;

    /// <summary>Gets or sets whether edge labels should render a subtle background plate.</summary>
    public bool IncludeEdgeLabelBackplates { get; set; } = true;

    /// <summary>Gets or sets whether displaced edge labels should draw a subtle leader back to their edge route.</summary>
    public bool IncludeEdgeLabelLeaders { get; set; }

    /// <summary>Gets or sets whether direction markers should be rendered on directed edges.</summary>
    public bool IncludeDirectionMarkers { get; set; } = true;

    /// <summary>Gets or sets whether node status badges should be rendered.</summary>
    public bool IncludeStatusBadges { get; set; } = true;

    /// <summary>Gets or sets whether monitoring group headers should render compact status dots.</summary>
    public bool IncludeGroupStatusDots { get; set; }

    /// <summary>Gets or sets whether SVG title tooltip elements should be rendered.</summary>
    public bool IncludeTooltips { get; set; } = true;

    /// <summary>Gets or sets whether scoped CSS should be emitted.</summary>
    public bool IncludeCss { get; set; } = true;

    /// <summary>Gets or sets whether element metadata and metrics should be emitted as SVG data attributes.</summary>
    public bool IncludeDataAttributes { get; set; } = true;

    /// <summary>Gets or sets whether the SVG should include responsive sizing style.</summary>
    public bool UseResponsiveSvg { get; set; } = true;

    /// <summary>Gets or sets whether rendered topology content should scale down to remain inside the requested viewport instead of expanding it.</summary>
    public bool FitContentToViewport { get; set; }

    /// <summary>Gets or sets whether complete HTML pages should include lightweight selection interactions. Defaults to static, script-free HTML.</summary>
    public bool EnableHtmlInteractions { get; set; }

    /// <summary>Gets or sets whether interactive HTML pages should include lightweight zoom and pan controls.</summary>
    public bool EnableHtmlViewportControls { get; set; }

    /// <summary>Gets or sets whether interactive HTML pages should include lightweight SVG and PNG export controls.</summary>
    public bool EnableHtmlExportControls { get; set; }

    /// <summary>Gets or sets whether interactive HTML pages should synchronize selection and viewport state with wrappers in the same sync group.</summary>
    public bool EnableHtmlSynchronizedState { get; set; }

    /// <summary>Gets or sets whether interactive HTML pages should render scenario picker controls when scenarios are present.</summary>
    public bool EnableHtmlScenarioControls { get; set; } = true;

    /// <summary>Gets or sets whether interactive HTML pages should render a compact scenario detail panel when scenarios are present.</summary>
    public bool EnableHtmlScenarioPanel { get; set; } = true;

    /// <summary>Gets or sets whether interactive HTML pages should read and update scenario state from the page query string.</summary>
    public bool EnableHtmlScenarioUrlState { get; set; }

    /// <summary>Gets or sets the optional HTML synchronization group name used by topology wrappers on the same page.</summary>
    public string? HtmlSyncGroupName { get; set; }

    /// <summary>Gets or sets the optional scenario id to activate when an interactive HTML page loads.</summary>
    public string? ActiveScenarioId { get; set; }

    /// <summary>Gets or sets optional script-free topology motion for SVG and sampled raster exports.</summary>
    public TopologyMotionOptions? Motion { get; set; }

    /// <summary>Gets or sets whether geographic topology layouts should render map callout summaries for coordinated groups.</summary>
    public bool IncludeGeographicCallouts { get; set; }

    /// <summary>Gets or sets whether geographic topology layouts should render soft status-colored region hulls around coordinated groups.</summary>
    public bool IncludeGeographicRegionHulls { get; set; }

    /// <summary>Gets or sets whether geographic callouts should prefer map-edge placements before near-anchor placement.</summary>
    public bool PreferGeographicCalloutMapEdges { get; set; }

    /// <summary>Gets or sets the maximum number of geographic callout summaries to render.</summary>
    public int GeographicCalloutMaxItems { get; set; } = 4;

    /// <summary>Gets or sets the padding added around nodes when calculating geographic region hull radius.</summary>
    public double GeographicRegionHullPadding { get; set; } = 22;

    /// <summary>Gets or sets the minimum geographic region hull radius.</summary>
    public double GeographicRegionHullMinRadius { get; set; } = 52;

    /// <summary>Gets or sets the maximum geographic region hull radius.</summary>
    public double GeographicRegionHullMaxRadius { get; set; } = 96;

    /// <summary>Gets or sets whether links should open in a new tab.</summary>
    public bool OpenLinksInNewTab { get; set; }

    /// <summary>Gets or sets the CSS class prefix.</summary>
    public string? CssClassPrefix { get; set; } = "cfx-topology";

    /// <summary>Gets or sets an optional focused topology view.</summary>
    public TopologyView? View { get; set; }

    /// <summary>Gets or sets a reusable render preset.</summary>
    public TopologyViewPreset Preset { get; set; } = TopologyViewPreset.Default;

    /// <summary>Gets or sets the reusable visual treatment used by renderers.</summary>
    public TopologyVisualStyle VisualStyle { get; set; } = TopologyVisualStyle.Default;

    /// <summary>Gets or sets the geographic map background treatment.</summary>
    public TopologyMapBackgroundStyle MapBackgroundStyle { get; set; } = TopologyMapBackgroundStyle.Auto;

    /// <summary>Gets or sets the dashboard-style canvas surface used behind non-geographic topology content.</summary>
    public TopologyCanvasSurfaceStyle CanvasSurfaceStyle { get; set; } = TopologyCanvasSurfaceStyle.Plain;

    /// <summary>Gets or sets the group card fill treatment.</summary>
    public TopologyGroupSurfaceStyle GroupSurfaceStyle { get; set; } = TopologyGroupSurfaceStyle.Auto;

    /// <summary>Gets or sets the card-like node fill treatment.</summary>
    public TopologyNodeSurfaceStyle NodeSurfaceStyle { get; set; } = TopologyNodeSurfaceStyle.Auto;

    /// <summary>Gets or sets how topology nodes should be presented.</summary>
    public TopologyNodeDisplayMode NodeDisplayMode { get; set; } = TopologyNodeDisplayMode.Card;

    /// <summary>Gets or sets the marker shape used for directed topology edges.</summary>
    public TopologyArrowMarkerStyle ArrowMarkerStyle { get; set; } = TopologyArrowMarkerStyle.Triangle;

    /// <summary>Gets or sets how orthogonal edge bends should be drawn.</summary>
    public TopologyEdgeCornerStyle EdgeCornerStyle { get; set; } = TopologyEdgeCornerStyle.Sharp;

    /// <summary>Gets or sets the radius used when rendering rounded orthogonal edge bends.</summary>
    public double EdgeCornerRadius { get; set; } = 12;

    /// <summary>Gets or sets an optional reusable visual style for topology edge strokes. When unset, the renderer uses its topology preset.</summary>
    public ChartLineVisualStyle? EdgeVisualStyle {
        get => _edgeVisualStyle;
        set => _edgeVisualStyle = value?.Clone();
    }

    /// <summary>Gets or sets an optional icon catalog used to resolve node and group icon ids. When unset, the built-in catalog is used.</summary>
    public TopologyIconCatalog? IconCatalog { get; set; }

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
