using System;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>
    /// Sets an explicit group position while preserving the current group size and styling.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="groupId">The group id.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithGroupPosition(this TopologyChart chart, string groupId, double x, double y) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        groupId = RequiredText(groupId, nameof(groupId), "Topology group ids");
        ValidateFinite(x, nameof(x), "Topology group x-coordinates");
        ValidateFinite(y, nameof(y), "Topology group y-coordinates");
        foreach (var group in chart.Groups) {
            if (!string.Equals(group.Id, groupId, StringComparison.Ordinal)) continue;
            group.X = x;
            group.Y = y;
            group.HasPositionOverride = true;
            return chart;
        }

        throw new ArgumentException("Topology group '" + groupId + "' was not found.", nameof(groupId));
    }
}
