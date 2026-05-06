using System;

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
}
