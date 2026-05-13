using System;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>
    /// Applies a reusable icon definition to a node.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="nodeId">The node id.</param>
    /// <param name="iconId">The icon id, for example <c>network:switch</c>.</param>
    /// <param name="catalog">Optional icon catalog. When omitted, built-in packs are used.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithNodeIcon(this TopologyChart chart, string nodeId, string iconId, TopologyIconCatalog? catalog = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        nodeId = RequiredText(nodeId, nameof(nodeId), "Topology node ids");
        var icon = ResolveIcon(iconId, catalog);
        foreach (var node in chart.Nodes) {
            if (!string.Equals(node.Id, nodeId, StringComparison.Ordinal)) continue;
            ApplyIcon(node, icon, updateKind: true);
            return chart;
        }

        throw new ArgumentException("Topology node '" + nodeId + "' was not found.", nameof(nodeId));
    }

    /// <summary>
    /// Applies a reusable icon definition to all nodes of a kind.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="kind">The node kind to update.</param>
    /// <param name="iconId">The icon id, for example <c>common:certificate</c>.</param>
    /// <param name="catalog">Optional icon catalog. When omitted, built-in packs are used.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithNodesOfKindIcon(this TopologyChart chart, TopologyNodeKind kind, string iconId, TopologyIconCatalog? catalog = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateEnum(typeof(TopologyNodeKind), kind, nameof(kind), "Topology node kinds");
        var icon = ResolveIcon(iconId, catalog);
        foreach (var node in chart.Nodes) {
            if (node.Kind != kind) continue;
            ApplyIcon(node, icon, updateKind: false);
        }

        return chart;
    }

    /// <summary>
    /// Adds an auto-placed node from a reusable icon definition.
    /// </summary>
    public static TopologyChart AddAutoIconNode(this TopologyChart chart, string id, string label, string iconId, TopologyHealthStatus status = TopologyHealthStatus.Unknown, string? groupId = null, string? subtitle = null, string? href = null, string? tooltip = null, double width = 120, double height = 64, string? cssClass = null, TopologyIconCatalog? catalog = null) {
        return AddIconNode(chart, id, label, iconId, 0, 0, status, groupId, subtitle, href, tooltip, width, height, cssClass, catalog);
    }

    /// <summary>
    /// Adds a positioned node from a reusable icon definition.
    /// </summary>
    public static TopologyChart AddIconNode(this TopologyChart chart, string id, string label, string iconId, double x, double y, TopologyHealthStatus status = TopologyHealthStatus.Unknown, string? groupId = null, string? subtitle = null, string? href = null, string? tooltip = null, double width = 120, double height = 64, string? cssClass = null, TopologyIconCatalog? catalog = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var icon = ResolveIcon(iconId, catalog);
        chart.AddNode(id, label, x, y, icon.NodeKind, status, groupId, subtitle, href, tooltip, width, height, icon.Symbol, cssClass, icon.Color, icon.QualifiedId);
        var node = chart.Nodes[chart.Nodes.Count - 1];
        if (icon.DisplayMode.HasValue) node.DisplayMode = icon.DisplayMode.Value;
        return chart;
    }

    /// <summary>
    /// Applies a reusable icon definition to a group.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="groupId">The group id.</param>
    /// <param name="iconId">The icon id, for example <c>microsoft-ad:site</c>.</param>
    /// <param name="catalog">Optional icon catalog. When omitted, built-in packs are used.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithGroupIcon(this TopologyChart chart, string groupId, string iconId, TopologyIconCatalog? catalog = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        groupId = RequiredText(groupId, nameof(groupId), "Topology group ids");
        var icon = ResolveIcon(iconId, catalog);
        foreach (var group in chart.Groups) {
            if (!string.Equals(group.Id, groupId, StringComparison.Ordinal)) continue;
            group.IconId = icon.QualifiedId;
            group.Symbol = string.IsNullOrWhiteSpace(group.Symbol) ? icon.Symbol : group.Symbol;
            group.Color = string.IsNullOrWhiteSpace(group.Color) ? icon.Color : group.Color;
            return chart;
        }

        throw new ArgumentException("Topology group '" + groupId + "' was not found.", nameof(groupId));
    }

    private static TopologyIconDefinition ResolveIcon(string iconId, TopologyIconCatalog? catalog) {
        var requiredIconId = RequiredText(iconId, nameof(iconId), "Topology icon ids");
        var icon = (catalog ?? TopologyIconCatalog.Default()).Resolve(requiredIconId);
        if (icon == null) throw new ArgumentException("Topology icon '" + requiredIconId + "' was not found.", nameof(iconId));
        return icon;
    }

    private static void ApplyIcon(TopologyNode node, TopologyIconDefinition icon, bool updateKind) {
        node.IconId = icon.QualifiedId;
        if (updateKind) node.Kind = icon.NodeKind;
        if (string.IsNullOrWhiteSpace(node.Symbol)) node.Symbol = icon.Symbol;
        if (string.IsNullOrWhiteSpace(node.Color)) node.Color = icon.Color;
        if (!node.DisplayMode.HasValue && icon.DisplayMode.HasValue) node.DisplayMode = icon.DisplayMode.Value;
    }
}
