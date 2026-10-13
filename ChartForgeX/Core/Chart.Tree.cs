using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a connected tree with explicit node IDs and one incoming branch per child. Weights do not change tree placement.</summary>
    public Chart AddTree(string name, IEnumerable<ChartNode> nodes, IEnumerable<ChartTreeLink> links, ChartColor? color = null) {
        EnsureCanAddSeries();
        AppendSeries(ChartSeries.CreateTree(name, nodes, links, color));
        return this;
    }
}
