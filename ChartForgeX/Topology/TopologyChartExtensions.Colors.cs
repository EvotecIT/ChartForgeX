using System;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {

    /// <summary>
    /// Sets reusable node styling and optional icon assignment for all nodes of a kind in one pass.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="kind">The node kind to update.</param>
    /// <param name="color">The optional node accent color.</param>
    /// <param name="backgroundColor">The optional node surface fill color.</param>
    /// <param name="iconId">The optional icon id, for example <c>common:certificate</c>.</param>
    /// <param name="catalog">Optional icon catalog. When omitted, built-in packs are used.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart NodesOfKindStyle(this TopologyChart chart, TopologyNodeKind kind, string? color = null, string? backgroundColor = null, string? iconId = null, TopologyIconCatalog? catalog = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateEnum(typeof(TopologyNodeKind), kind, nameof(kind), "Topology node kinds");
        var icon = string.IsNullOrWhiteSpace(iconId) ? null : ResolveIcon(iconId!, catalog);
        foreach (var node in chart.Nodes) {
            if (node.Kind != kind) continue;
            if (color != null) node.Color = color;
            if (backgroundColor != null) node.BackgroundColor = backgroundColor;
            if (icon != null) ApplyIcon(node, icon, updateKind: true);
        }

        return chart;
    }
}
