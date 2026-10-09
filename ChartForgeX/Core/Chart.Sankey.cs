using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a Sankey chart with explicit node and flow IDs. Labels may repeat and flows may run in parallel.</summary>
    public Chart AddSankey(string name, IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> links, ChartColor? color = null) {
        EnsureCanAddSeries();
        var series = new ChartSeries(name, ChartSeriesKind.Sankey, Array.Empty<ChartPoint>()) { Color = color };
        series.SetRelationships(ChartRelationshipIndex.Sankey(nodes, links));
        AppendSeries(series);
        return this;
    }
}
