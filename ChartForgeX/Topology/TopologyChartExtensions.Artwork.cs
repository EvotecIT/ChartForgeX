using System;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>
    /// Adds an auto-placed node with node-specific artwork.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The node id.</param>
    /// <param name="label">The node label.</param>
    /// <param name="artwork">Trusted SVG or image artwork for SVG and HTML renderers.</param>
    /// <param name="kind">The node kind.</param>
    /// <param name="status">The node status.</param>
    /// <param name="groupId">The optional group id.</param>
    /// <param name="subtitle">The optional subtitle.</param>
    /// <param name="href">The optional href.</param>
    /// <param name="tooltip">The optional tooltip.</param>
    /// <param name="width">The node width.</param>
    /// <param name="height">The node height.</param>
    /// <param name="symbol">The optional short visual symbol shown by fallback renderers.</param>
    /// <param name="cssClass">The optional caller-provided CSS class tokens.</param>
    /// <param name="color">The optional node accent color, independent from health status.</param>
    /// <param name="backgroundColor">The optional node surface fill color.</param>
    /// <param name="displayMode">The display mode to apply with the artwork.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart AddAutoArtworkNode(this TopologyChart chart, string id, string label, TopologyIconArtwork artwork, TopologyNodeKind kind = TopologyNodeKind.Generic, TopologyHealthStatus status = TopologyHealthStatus.Unknown, string? groupId = null, string? subtitle = null, string? href = null, string? tooltip = null, double width = 120, double height = 64, string? symbol = null, string? cssClass = null, string? color = null, string? backgroundColor = null, TopologyNodeDisplayMode displayMode = TopologyNodeDisplayMode.Artwork) {
        return AddArtworkNode(chart, id, label, artwork, 0, 0, kind, status, groupId, subtitle, href, tooltip, width, height, symbol, cssClass, color, backgroundColor, displayMode);
    }

    /// <summary>
    /// Adds a positioned node with node-specific artwork.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The node id.</param>
    /// <param name="label">The node label.</param>
    /// <param name="artwork">Trusted SVG or image artwork for SVG and HTML renderers.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="kind">The node kind.</param>
    /// <param name="status">The node status.</param>
    /// <param name="groupId">The optional group id.</param>
    /// <param name="subtitle">The optional subtitle.</param>
    /// <param name="href">The optional href.</param>
    /// <param name="tooltip">The optional tooltip.</param>
    /// <param name="width">The node width.</param>
    /// <param name="height">The node height.</param>
    /// <param name="symbol">The optional short visual symbol shown by fallback renderers.</param>
    /// <param name="cssClass">The optional caller-provided CSS class tokens.</param>
    /// <param name="color">The optional node accent color, independent from health status.</param>
    /// <param name="backgroundColor">The optional node surface fill color.</param>
    /// <param name="displayMode">The display mode to apply with the artwork.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart AddArtworkNode(this TopologyChart chart, string id, string label, TopologyIconArtwork artwork, double x, double y, TopologyNodeKind kind = TopologyNodeKind.Generic, TopologyHealthStatus status = TopologyHealthStatus.Unknown, string? groupId = null, string? subtitle = null, string? href = null, string? tooltip = null, double width = 120, double height = 64, string? symbol = null, string? cssClass = null, string? color = null, string? backgroundColor = null, TopologyNodeDisplayMode displayMode = TopologyNodeDisplayMode.Artwork) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateArtwork(artwork);
        ValidateEnum(typeof(TopologyNodeDisplayMode), displayMode, nameof(displayMode), "Topology node display modes");
        chart.AddNode(id, label, x, y, kind, status, groupId, subtitle, href, tooltip, width, height, symbol, cssClass, color, iconId: null, backgroundColor: backgroundColor);
        ApplyArtwork(chart.Nodes[chart.Nodes.Count - 1], artwork, displayMode);
        return chart;
    }

    /// <summary>
    /// Applies node-specific artwork directly to an existing topology node.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="nodeId">The node id.</param>
    /// <param name="artwork">Trusted SVG or image artwork for SVG and HTML renderers.</param>
    /// <param name="displayMode">The display mode to apply with the artwork.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithNodeArtwork(this TopologyChart chart, string nodeId, TopologyIconArtwork artwork, TopologyNodeDisplayMode displayMode = TopologyNodeDisplayMode.Artwork) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        nodeId = RequiredText(nodeId, nameof(nodeId), "Topology node ids");
        ValidateArtwork(artwork);
        ValidateEnum(typeof(TopologyNodeDisplayMode), displayMode, nameof(displayMode), "Topology node display modes");
        foreach (var node in chart.Nodes) {
            if (!string.Equals(node.Id, nodeId, StringComparison.Ordinal)) continue;
            ApplyArtwork(node, artwork, displayMode);
            return chart;
        }

        throw new ArgumentException("Topology node '" + nodeId + "' was not found.", nameof(nodeId));
    }

    /// <summary>
    /// Removes node-specific artwork from an existing topology node.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="nodeId">The node id.</param>
    /// <param name="resetArtworkDisplayMode">Whether to clear an artwork-only display override so icon catalog display defaults can apply again.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithoutNodeArtwork(this TopologyChart chart, string nodeId, bool resetArtworkDisplayMode = true) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        nodeId = RequiredText(nodeId, nameof(nodeId), "Topology node ids");
        foreach (var node in chart.Nodes) {
            if (!string.Equals(node.Id, nodeId, StringComparison.Ordinal)) continue;
            node.Artwork = null;
            if (resetArtworkDisplayMode && node.DisplayMode == TopologyNodeDisplayMode.Artwork) node.DisplayMode = null;
            return chart;
        }

        throw new ArgumentException("Topology node '" + nodeId + "' was not found.", nameof(nodeId));
    }

    private static void ApplyArtwork(TopologyNode node, TopologyIconArtwork artwork, TopologyNodeDisplayMode displayMode) {
        node.Artwork = artwork;
        node.DisplayMode = displayMode;
    }

    private static void ValidateArtwork(TopologyIconArtwork artwork) {
        if (artwork == null) throw new ArgumentNullException(nameof(artwork));
        if (!artwork.IsSafe) throw new ArgumentException("Topology node artwork contains unsafe SVG or image href content.", nameof(artwork));
        if (!artwork.HasSvgBody && !artwork.HasImageHref) throw new ArgumentException("Topology node artwork must define inline SVG or an image href.", nameof(artwork));
    }
}
