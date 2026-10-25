using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart {
    internal IReadOnlyDictionary<string, IReadOnlyList<ChartPoint>>? PreparedRegionRoutes { get; set; }
    internal TopologyRegionPaintPlan? PreparedRegionPaintPlan { get; set; }
    /// <summary>
    /// Retains native painted header parts in chart coordinates on detached prepared geometry only.
    /// Fresh layout clears this snapshot; diagnostics and caption clearance reuse its immutable rectangles.
    /// </summary>
    internal IReadOnlyDictionary<string, IReadOnlyList<ChartRect>>? PreparedGroupHeaderParts { get; set; }

    /// <summary>Retains the layout-policy reservations used to plan routes, separately from actual painted glyph bounds.</summary>
    internal IReadOnlyDictionary<string, IReadOnlyList<ChartRect>>? PreparedGroupHeaderLayoutParts { get; set; }

    /// <summary>Reuses policy-based header reservations only within the current detached layout pass.</summary>
    internal TopologyGroupHeaderLayout? GroupHeaderLayout { get; set; }
}
