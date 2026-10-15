using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart {
    /// <summary>
    /// Retains native painted header parts in chart coordinates on detached prepared geometry only.
    /// Fresh layout clears this snapshot; deferred routing and diagnostics reuse its immutable rectangles.
    /// </summary>
    internal IReadOnlyDictionary<string, IReadOnlyList<ChartRect>>? PreparedGroupHeaderParts { get; set; }
}
