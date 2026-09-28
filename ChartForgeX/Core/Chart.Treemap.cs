using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a flat treemap series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="items">The labeled values to render.</param>
    /// <param name="color">An optional base color. When null, the theme palette colors the tiles.</param>
    /// <returns>The current chart.</returns>
    public Chart AddTreemap(string name, IEnumerable<ChartTreemapItem> items, ChartColor? color = null) {
        EnsureCanAddSeries();
        if (items == null) throw new ArgumentNullException(nameof(items));
        var points = new List<ChartPoint>();
        var labels = new List<ChartAxisLabel>();
        var index = 1;
        foreach (var item in items) {
            points.Add(new ChartPoint(index, item.Value));
            labels.Add(new ChartAxisLabel(index, item.Label));
            index++;
        }

        if (points.Count == 0) throw new ArgumentException("Treemaps must contain at least one item.", nameof(items));
        Options.XAxisLabels.Clear();
        Options.XAxisLabels.AddRange(labels);
        return Add(name, ChartSeriesKind.Treemap, points, color);
    }
}
