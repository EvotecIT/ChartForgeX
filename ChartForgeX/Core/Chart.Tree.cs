using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a connected tree with explicit node IDs and one incoming branch per child. Weights do not change tree placement.</summary>
    public Chart AddTree(string name, IEnumerable<ChartNode> nodes, IEnumerable<ChartTreeLink> links, ChartColor? color = null) {
        EnsureCanAddSeries();
        var series = new ChartSeries(name, ChartSeriesKind.Tree, Array.Empty<ChartPoint>()) { Color = color };
        series.SetRelationships(ChartRelationshipIndex.Hierarchy(nodes, links, aggregateLeaves: false));
        AppendSeries(series);
        return this;
    }
}
