using System;
using ChartForgeX.Core;

namespace ChartForgeX.Topology;

/// <summary>
/// Provides reusable topology render-option presets.
/// </summary>
public static class TopologyRenderOptionsExtensions {
    /// <summary>Applies a reusable topology spacing and presentation profile.</summary>
    public static TopologyRenderOptions ApplyLayoutPreset(this TopologyRenderOptions options, TopologyLayoutPreset preset) {
        if (options == null) throw new System.ArgumentNullException(nameof(options));
        if (!System.Enum.IsDefined(typeof(TopologyLayoutPreset), preset)) throw new System.ArgumentOutOfRangeException(nameof(preset), preset, "Unknown topology layout preset.");
        options.LayoutPreset = preset;
        switch (preset) {
            case TopologyLayoutPreset.Automatic:
                options.LayeredNodeSpacing = null;
                options.LayeredRankSpacing = null;
                options.NodeDisplayMode = TopologyNodeDisplayMode.Card;
                options.WrapNodeLabels = false;
                options.MaxNodeLabelLines = 2;
                options.MaxNodeSubtitleLines = 2;
                break;
            case TopologyLayoutPreset.Dense:
                options.LayeredNodeSpacing = 14;
                options.LayeredRankSpacing = 28;
                options.NodeDisplayMode = TopologyNodeDisplayMode.CompactCard;
                options.WrapNodeLabels = false;
                options.MaxNodeLabelLines = 1;
                options.MaxNodeSubtitleLines = 1;
                break;
            case TopologyLayoutPreset.Compact:
                options.LayeredNodeSpacing = 20;
                options.LayeredRankSpacing = 34;
                options.NodeDisplayMode = TopologyNodeDisplayMode.CompactCard;
                options.WrapNodeLabels = false;
                options.MaxNodeLabelLines = 2;
                options.MaxNodeSubtitleLines = 2;
                break;
            case TopologyLayoutPreset.Balanced:
                options.LayeredNodeSpacing = 28;
                options.LayeredRankSpacing = 42;
                options.NodeDisplayMode = TopologyNodeDisplayMode.Card;
                options.WrapNodeLabels = false;
                options.MaxNodeLabelLines = 2;
                options.MaxNodeSubtitleLines = 2;
                break;
            case TopologyLayoutPreset.Presentation:
                options.LayeredNodeSpacing = 42;
                options.LayeredRankSpacing = 64;
                options.NodeDisplayMode = TopologyNodeDisplayMode.Card;
                options.WrapNodeLabels = true;
                options.MaxNodeLabelLines = 2;
                options.MaxNodeSubtitleLines = 2;
                break;
        }
        options.MarkLayoutPresetApplied();
        return options;
    }

    /// <summary>
    /// Applies a preset to existing render options.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="preset">The preset to apply.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions ApplyPreset(this TopologyRenderOptions options, TopologyViewPreset preset) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (!System.Enum.IsDefined(typeof(TopologyViewPreset), preset)) throw new System.ArgumentOutOfRangeException(nameof(preset), preset, "Unknown topology view preset.");
        options.Preset = preset;
        switch (preset) {
            case TopologyViewPreset.Grouped:
                options.IncludeGroups = true;
                options.IncludeGroupLabels = true;
                options.IncludeNodeLabels = true;
                options.IncludeEdgeLabels = true;
                options.NodeDisplayMode = TopologyNodeDisplayMode.Card;
                break;
            case TopologyViewPreset.Ungrouped:
                options.IncludeGroups = false;
                options.IncludeGroupLabels = false;
                options.IncludeNodeLabels = true;
                options.IncludeEdgeLabels = true;
                break;
            case TopologyViewPreset.Connectivity:
                options.IncludeGroups = true;
                options.IncludeEdgeLabels = true;
                options.IncludeDirectionMarkers = true;
                options.LegendMode = TopologyLegendMode.Merge;
                break;
            case TopologyViewPreset.Dependency:
                options.IncludeGroups = true;
                options.IncludeEdgeLabels = true;
                options.IncludeDirectionMarkers = true;
                options.NodeDisplayMode = TopologyNodeDisplayMode.CompactCard;
                break;
            case TopologyViewPreset.Offenders:
                AddHighlightStatus(options, TopologyHealthStatus.Warning);
                AddHighlightStatus(options, TopologyHealthStatus.Critical);
                options.IncludeGroups = true;
                options.IncludeEdgeLabels = true;
                options.LegendMode = TopologyLegendMode.Merge;
                break;
            case TopologyViewPreset.Compact:
                options.NodeDisplayMode = TopologyNodeDisplayMode.CompactCard;
                options.IncludeNodeLabels = true;
                options.IncludeStatusBadges = true;
                break;
            case TopologyViewPreset.MetricLabels:
                options.IncludeEdgeLabels = true;
                options.IncludeDirectionMarkers = true;
                options.NodeDisplayMode = TopologyNodeDisplayMode.CompactCard;
                break;
            case TopologyViewPreset.RelationshipOverview:
                options.RelationshipOverviewStyle();
                break;
        }

        options.MarkPresetApplied();
        return options;
    }

    /// <summary>
    /// Uses a stronger luminous topology edge style for dark dashboards and deliberate emphasis.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions LuminousEdges(this TopologyRenderOptions options) =>
        options.EdgeVisualStyle(ChartLineVisualStyle.Luminous().AmbientHaloOpacity(0.045).AmbientHaloStrokeExtra(5.8).HaloOpacity(0.13).HaloStrokeExtra(3.0).HighlightOpacity(0.10).HighlightStrokeRatio(0.28));

    /// <summary>
    /// Applies the reusable monitoring-dashboard visual treatment used for dense topology and geographic monitoring views.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions MonitoringDashboardStyle(this TopologyRenderOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.VisualStyle = TopologyVisualStyle.MonitoringDashboard;
        options.MapBackgroundStyle = TopologyMapBackgroundStyle.SoftSilhouette;
        options.CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.Panel;
        options.Geography.IncludeRegionHulls = true;
        options.Geography.RegionHullPadding = 16;
        options.Geography.RegionHullMaxRadius = 82;
        options.Geography.PreferCalloutMapEdges = true;
        options.IncludeGroupStatusDots = true;
        options.IncludeEdgeLabelBackplates = false;
        if (options.LegendMode != TopologyLegendMode.Explicit) options.LegendMode = TopologyLegendMode.Merge;
        return options;
    }

    /// <summary>
    /// Applies a reusable relationship-overview treatment for entity maps, dependency overviews, and evidence correlation diagrams.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions RelationshipOverviewStyle(this TopologyRenderOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.MonitoringDashboardStyle();
        options.NodeDisplayMode = TopologyNodeDisplayMode.Card;
        options.CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.PanelGrid;
        options.ArrowMarkerStyle = TopologyArrowMarkerStyle.Chevron;
        options.NodeSurfaceStyle = TopologyNodeSurfaceStyle.AccentBand;
        options.EdgeCornerStyle = TopologyEdgeCornerStyle.Rounded;
        options.EdgeCornerRadius = 14;
        options.CardSubtitleMode = TopologyCardSubtitleMode.Text;
        options.AllowMultilineNodeLabels = true;
        options.WrapNodeLabels = true;
        options.MaxNodeLabelLines = 2;
        options.MaxNodeSubtitleLines = 2;
        options.IncludeEdgeLabels = true;
        options.IncludeDirectionMarkers = true;
        options.IncludeEdgeLabelBackplates = true;
        options.IncludeEdgeLabelLeaders = true;
        options.IncludeStatusBadges = true;
        options.LegendMode = TopologyLegendMode.Enrich;
        return options;
    }

    /// <summary>
    /// Applies a clean informational treatment for centered mind maps.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions MindMapStyle(this TopologyRenderOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.IncludeLegend = false;
        options.IncludeGroups = false;
        options.IncludeEdgeLabels = false;
        options.IncludeDirectionMarkers = false;
        options.IncludeStatusBadges = false;
        options.CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.Plain;
        options.HeaderStyle = TopologyHeaderStyle.CenterBanner;
        options.NodeSurfaceStyle = TopologyNodeSurfaceStyle.Tinted;
        options.CardSubtitleMode = TopologyCardSubtitleMode.Text;
        options.EdgeCornerStyle = TopologyEdgeCornerStyle.Rounded;
        options.EdgeCornerRadius = 24;
        options.EdgeVisualStyle = ChartLineVisualStyle.Plain();
        options.AllowMultilineNodeLabels = true;
        options.WrapNodeLabels = true;
        options.MaxNodeLabelLines = 2;
        options.MaxNodeSubtitleLines = 1;
        return options;
    }

    /// <summary>
    /// Applies a readable force-graph treatment for relationship-heavy topology clouds.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions ForceGraphStyle(this TopologyRenderOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.MonitoringDashboardStyle();
        options.UseForceGraphPresentation = true;
        options.ForceLayoutProfile = TopologyForceLayoutProfile.RelationshipGraph;
        options.NodeDisplayMode = TopologyNodeDisplayMode.Dot;
        options.CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.Panel;
        options.GroupSurfaceStyle = TopologyGroupSurfaceStyle.Neutral;
        options.IncludeEdgeLabels = false;
        options.IncludeEdgeLabelBackplates = false;
        options.IncludeEdgeLabelLeaders = false;
        options.IncludeDirectionMarkers = false;
        options.IncludeGroups = false;
        options.IncludeGroupLabels = false;
        options.IncludeLegend = false;
        options.IncludeNodeLabels = false;
        options.IncludeIconLabels = false;
        options.IncludeTileSubtitles = false;
        options.IncludeStatusBadges = false;
        options.LegendMode = TopologyLegendMode.Merge;
        options.EdgeVisualStyle = ChartLineVisualStyle.Plain();
        return options;
    }

    /// <summary>
    /// Selects the root and caps used by relationship-radial ego layouts.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="nodeId">The root node id.</param>
    /// <param name="maxDepth">The maximum number of relationship hops to expand.</param>
    /// <param name="maxFanout">The maximum neighbor count expanded per node.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions RelationshipRadialFocus(this TopologyRenderOptions options, string nodeId, int maxDepth = 2, int maxFanout = 18) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(nodeId)) throw new ArgumentException("Relationship radial root node id cannot be empty.", nameof(nodeId));
        if (maxDepth < 1) throw new ArgumentOutOfRangeException(nameof(maxDepth), "Relationship radial depth must be greater than zero.");
        if (maxFanout < 1) throw new ArgumentOutOfRangeException(nameof(maxFanout), "Relationship radial fanout must be greater than zero.");
        options.RelationshipRootNodeId = nodeId;
        options.RelationshipRadialMaxDepth = maxDepth;
        options.RelationshipRadialMaxFanout = maxFanout;
        if (!options.SelectedNodeIds.Contains(nodeId)) options.SelectedNodeIds.Add(nodeId);
        return options;
    }

    /// <summary>
    /// Enables or disables topology scenario picker controls in interactive HTML output.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="enabled">Whether interactive HTML should render scenario picker controls.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions HtmlScenarioControls(this TopologyRenderOptions options, bool enabled = true) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.Html.ScenarioControls = enabled;
        if (enabled) options.Html.Interactions = true;
        return options;
    }

    /// <summary>
    /// Chooses how topology scenario controls are rendered in interactive HTML output.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="mode">The scenario control mode.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions HtmlScenarioControlMode(this TopologyRenderOptions options, TopologyHtmlScenarioControlMode mode) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        TopologyModelGuards.EnumDefined(mode, nameof(mode));
        options.Html.ScenarioControlMode = mode;
        options.Html.Interactions = true;
        options.Html.ScenarioControls = true;
        return options;
    }

    /// <summary>
    /// Enables or disables the compact topology scenario detail panel in interactive HTML output.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="enabled">Whether interactive HTML should render the scenario detail panel.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions HtmlScenarioPanel(this TopologyRenderOptions options, bool enabled = true) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.Html.ScenarioPanel = enabled;
        if (enabled) options.Html.Interactions = true;
        return options;
    }

    /// <summary>
    /// Shows each scenario's matching sequence diagram below the topology and keeps its active message in step with playback.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="enabled">Whether interactive HTML should render scenario sequence diagrams.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions HtmlScenarioSequence(this TopologyRenderOptions options, bool enabled = true) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.Html.ScenarioSequence = enabled;
        if (enabled) options.Html.Interactions = true;
        return options;
    }

    /// <summary>
    /// Enables or disables the built-in selected-record panel in interactive HTML output.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="enabled">Whether interactive HTML should render the selected-record panel.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions HtmlSelectionPanel(this TopologyRenderOptions options, bool enabled = true) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.Html.SelectionPanel = enabled;
        if (enabled) options.Html.Interactions = true;
        return options;
    }

    /// <summary>
    /// Enables or disables the fullscreen viewport control in interactive HTML output.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="enabled">Whether interactive HTML should render a fullscreen control.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions HtmlFullscreenControl(this TopologyRenderOptions options, bool enabled = true) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.Html.FullscreenControl = enabled;
        if (enabled) options.Html.Interactions = true;
        return options;
    }

    /// <summary>
    /// Enables or disables query-string scenario state in interactive HTML output.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="enabled">Whether interactive HTML should read and update scenario state from the page query string.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions HtmlScenarioUrlState(this TopologyRenderOptions options, bool enabled = true) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.Html.ScenarioUrlState = enabled;
        if (enabled) options.Html.Interactions = true;
        return options;
    }

    /// <summary>
    /// Enables or disables force-directed graph exploration controls in interactive HTML output.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="enabled">Whether interactive HTML should render graph exploration controls.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions HtmlForceGraphControls(this TopologyRenderOptions options, bool enabled = true) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.Html.ForceGraphControls = enabled;
        if (enabled) {
            options.Html.Interactions = true;
            options.Html.ViewportControls = true;
        }

        return options;
    }

    /// <summary>
    /// Enables or disables the interactive HTML find bar for dense topologies.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="enabled">Whether interactive HTML should render a find bar with text, group and kind filters.</param>
    /// <returns>The current render options.</returns>
    /// <remarks>Matches are highlighted rather than hidden, and the viewport moves to them at a readable scale.</remarks>
    public static TopologyRenderOptions HtmlFindControls(this TopologyRenderOptions options, bool enabled = true) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.Html.FindControls = enabled;
        if (enabled) {
            options.Html.Interactions = true;
            options.Html.ViewportControls = true;
        }

        return options;
    }

    /// <summary>
    /// Marks a topology group as selected without filtering or dimming the chart.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="groupId">The group id.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions SelectGroup(this TopologyRenderOptions options, string groupId) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(groupId)) throw new ArgumentException("Selected group id cannot be empty.", nameof(groupId));
        if (!options.SelectedGroupIds.Contains(groupId)) options.SelectedGroupIds.Add(groupId);
        return options;
    }

    /// <summary>
    /// Marks a topology node as selected without filtering or dimming the chart.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="nodeId">The node id.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions SelectNode(this TopologyRenderOptions options, string nodeId) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(nodeId)) throw new ArgumentException("Selected node id cannot be empty.", nameof(nodeId));
        if (!options.SelectedNodeIds.Contains(nodeId)) options.SelectedNodeIds.Add(nodeId);
        return options;
    }

    /// <summary>
    /// Marks a topology edge as selected without filtering or dimming the chart.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions SelectEdge(this TopologyRenderOptions options, string edgeId) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(edgeId)) throw new ArgumentException("Selected edge id cannot be empty.", nameof(edgeId));
        if (!options.SelectedEdgeIds.Contains(edgeId)) options.SelectedEdgeIds.Add(edgeId);
        return options;
    }

    private static void AddHighlightStatus(TopologyRenderOptions options, TopologyHealthStatus status) {
        if (!options.HighlightStatuses.Contains(status)) options.HighlightStatuses.Add(status);
    }

    internal static string RequiredScenarioToken(string? value, string parameterName) {
        if (value == null) throw new ArgumentNullException(parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length == 0) throw new ArgumentException("Active scenario id cannot be empty.", parameterName);
        foreach (var ch in trimmed) {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.') continue;
            throw new ArgumentException("Active scenario id may contain only letters, digits, dots, underscores, and hyphens.", parameterName);
        }

        return trimmed;
    }
}
