using System;
using ChartForgeX.Core;

namespace ChartForgeX.Topology;

/// <summary>
/// Provides reusable topology render-option presets.
/// </summary>
public static class TopologyRenderOptionsExtensions {
    /// <summary>
    /// Applies a preset to existing render options.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="preset">The preset to apply.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions ApplyPreset(this TopologyRenderOptions options, TopologyViewPreset preset) {
        if (options == null) throw new ArgumentNullException(nameof(options));
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
                options.WithRelationshipOverviewStyle();
                break;
        }

        return options;
    }

    /// <summary>
    /// Uses edge metrics as stacked edge labels.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="primaryMetricKey">The metric key for the primary edge label.</param>
    /// <param name="secondaryMetricKey">The optional metric key for the secondary edge label.</param>
    /// <param name="tertiaryMetricKey">The optional metric key for the tertiary edge label.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithEdgeMetricLabels(this TopologyRenderOptions options, string? primaryMetricKey, string? secondaryMetricKey = null, string? tertiaryMetricKey = null) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.IncludeEdgeLabels = true;
        options.EdgeLabelMetricKey = primaryMetricKey;
        options.EdgeSecondaryLabelMetricKey = secondaryMetricKey;
        options.EdgeTertiaryLabelMetricKey = tertiaryMetricKey;
        return options;
    }

    /// <summary>
    /// Uses a caller supplied visual style for topology edge strokes across SVG and PNG output.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="style">The edge stroke visual style. Pass null to return to the topology default.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithTopologyEdgeVisualStyle(this TopologyRenderOptions options, ChartLineVisualStyle? style) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.EdgeVisualStyle = style;
        return options;
    }

    /// <summary>
    /// Uses a plain single-stroke topology edge style without halo or highlight layers.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithPlainTopologyEdges(this TopologyRenderOptions options) =>
        options.WithTopologyEdgeVisualStyle(ChartLineVisualStyle.Plain());

    /// <summary>
    /// Uses a stronger luminous topology edge style for dark dashboards and deliberate emphasis.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithLuminousTopologyEdges(this TopologyRenderOptions options) =>
        options.WithTopologyEdgeVisualStyle(ChartLineVisualStyle.Luminous().WithAmbientHalo(0.045, 5.8).WithHalo(0.13, 3.0).WithHighlight(0.10, 0.28));

    /// <summary>
    /// Applies the reusable monitoring-dashboard visual treatment used for dense topology and geographic monitoring views.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithMonitoringDashboardStyle(this TopologyRenderOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.VisualStyle = TopologyVisualStyle.MonitoringDashboard;
        options.MapBackgroundStyle = TopologyMapBackgroundStyle.SoftSilhouette;
        options.CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.Panel;
        options.IncludeGeographicRegionHulls = true;
        options.GeographicRegionHullPadding = 16;
        options.GeographicRegionHullMaxRadius = 82;
        options.PreferGeographicCalloutMapEdges = true;
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
    public static TopologyRenderOptions WithRelationshipOverviewStyle(this TopologyRenderOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.WithMonitoringDashboardStyle();
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
    /// Renders group containers as neutral cards while preserving status/identity borders and labels.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithNeutralGroupSurfaces(this TopologyRenderOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.GroupSurfaceStyle = TopologyGroupSurfaceStyle.Neutral;
        return options;
    }

    /// <summary>
    /// Keeps topology output at the requested viewport size by scaling dense rendered content down when needed.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithFitContentToViewport(this TopologyRenderOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.FitContentToViewport = true;
        return options;
    }

    /// <summary>
    /// Enables reusable multi-line node labels and optional word wrapping for dense relationship diagrams.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="wrap">Whether text should wrap at word boundaries when it exceeds the available width.</param>
    /// <param name="maxLabelLines">The maximum title rows.</param>
    /// <param name="maxSubtitleLines">The maximum subtitle rows.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithMultilineNodeLabels(this TopologyRenderOptions options, bool wrap = true, int maxLabelLines = 2, int maxSubtitleLines = 2) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (maxLabelLines < 1) throw new ArgumentOutOfRangeException(nameof(maxLabelLines), "Maximum node label lines must be at least 1.");
        if (maxSubtitleLines < 1) throw new ArgumentOutOfRangeException(nameof(maxSubtitleLines), "Maximum node subtitle lines must be at least 1.");
        options.AllowMultilineNodeLabels = true;
        options.WrapNodeLabels = wrap;
        options.MaxNodeLabelLines = maxLabelLines;
        options.MaxNodeSubtitleLines = maxSubtitleLines;
        return options;
    }

    /// <summary>
    /// Activates an existing topology scenario for static highlighting and initial interactive HTML state.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="scenarioId">The scenario id.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithActiveScenario(this TopologyRenderOptions options, string scenarioId) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.ActiveScenarioId = RequiredScenarioToken(scenarioId, nameof(scenarioId));
        return options;
    }

    /// <summary>
    /// Clears the active topology scenario.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithoutActiveScenario(this TopologyRenderOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.ActiveScenarioId = null;
        return options;
    }

    /// <summary>
    /// Enables deterministic, script-free topology motion for SVG and sampled raster exports.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="motion">The topology motion options.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithMotion(this TopologyRenderOptions options, TopologyMotionOptions motion) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (motion == null) throw new ArgumentNullException(nameof(motion));
        motion.Validate();
        options.Motion = motion;
        return options;
    }

    /// <summary>
    /// Clears topology motion.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithoutMotion(this TopologyRenderOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.Motion = null;
        return options;
    }

    /// <summary>
    /// Enables or disables topology scenario picker controls in interactive HTML output.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="enabled">Whether interactive HTML should render scenario picker controls.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithHtmlScenarioControls(this TopologyRenderOptions options, bool enabled = true) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.EnableHtmlScenarioControls = enabled;
        return options;
    }

    /// <summary>
    /// Enables or disables the compact topology scenario detail panel in interactive HTML output.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="enabled">Whether interactive HTML should render the scenario detail panel.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithHtmlScenarioPanel(this TopologyRenderOptions options, bool enabled = true) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.EnableHtmlScenarioPanel = enabled;
        return options;
    }

    /// <summary>
    /// Enables or disables query-string scenario state in interactive HTML output.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="enabled">Whether interactive HTML should read and update scenario state from the page query string.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithHtmlScenarioUrlState(this TopologyRenderOptions options, bool enabled = true) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.EnableHtmlScenarioUrlState = enabled;
        return options;
    }

    /// <summary>
    /// Marks a topology group as selected without filtering or dimming the chart.
    /// </summary>
    /// <param name="options">The render options.</param>
    /// <param name="groupId">The group id.</param>
    /// <returns>The current render options.</returns>
    public static TopologyRenderOptions WithSelectedGroup(this TopologyRenderOptions options, string groupId) {
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
    public static TopologyRenderOptions WithSelectedNode(this TopologyRenderOptions options, string nodeId) {
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
    public static TopologyRenderOptions WithSelectedEdge(this TopologyRenderOptions options, string edgeId) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(edgeId)) throw new ArgumentException("Selected edge id cannot be empty.", nameof(edgeId));
        if (!options.SelectedEdgeIds.Contains(edgeId)) options.SelectedEdgeIds.Add(edgeId);
        return options;
    }

    private static void AddHighlightStatus(TopologyRenderOptions options, TopologyHealthStatus status) {
        if (!options.HighlightStatuses.Contains(status)) options.HighlightStatuses.Add(status);
    }

    private static string RequiredScenarioToken(string? value, string parameterName) {
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
