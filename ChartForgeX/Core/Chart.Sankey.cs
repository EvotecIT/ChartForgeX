using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a Sankey chart with explicit node and flow IDs. Labels may repeat and flows may run in parallel.</summary>
    public Chart AddSankey(string name, IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> links, ChartColor? color = null) {
        EnsureCanAddSeries();
        AppendSeries(ChartSeries.CreateSankey(name, nodes, links, color));
        return this;
    }
}
