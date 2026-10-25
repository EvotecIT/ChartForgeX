using System;
using System.Linq;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>Shows or hides a semantic health ring without changing the node's accent, artwork or badge.</summary>
    /// <param name="chart">The topology to configure.</param>
    /// <param name="nodeId">The stable identifier of an existing node.</param>
    /// <param name="show">Whether to draw the health ring.</param>
    /// <returns>The same chart.</returns>
    public static TopologyChart WithNodeHealthRing(this TopologyChart chart, string nodeId, bool show = true) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        nodeId = RequiredText(nodeId, nameof(nodeId), "Topology node ids");
        var node = chart.Nodes.FirstOrDefault(candidate => candidate.Id == nodeId)
            ?? throw new ArgumentException("Topology node '" + nodeId + "' was not found.", nameof(nodeId));
        node.ShowHealthRing = show;
        return chart;
    }
}
