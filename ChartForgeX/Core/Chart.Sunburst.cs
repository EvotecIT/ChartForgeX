using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a Sunburst with explicit node IDs. Leaf weights determine sectors; internal values aggregate their leaves.</summary>
    public Chart AddSunburst(string name, IEnumerable<ChartNode> nodes, IEnumerable<ChartTreeLink> links, ChartColor? color = null) {
        EnsureCanAddSeries();
        var series = new ChartSeries(name, ChartSeriesKind.Sunburst, Array.Empty<ChartPoint>()) { Color = color };
        series.SetRelationships(ChartRelationshipIndex.Hierarchy(nodes, links, aggregateLeaves: true));
        AppendSeries(series);
        return this;
    }
}
