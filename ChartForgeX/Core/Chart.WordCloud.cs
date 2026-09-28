using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a deterministic dependency-free word cloud chart.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="items">The weighted terms to render.</param>
    /// <param name="color">An optional base color. When null, the theme palette colors the terms.</param>
    /// <returns>The current chart.</returns>
    public Chart AddWordCloud(string name, IEnumerable<ChartWordCloudItem> items, ChartColor? color = null) {
        EnsureCanAddSeries();
        if (items == null) throw new ArgumentNullException(nameof(items));
        var points = new List<ChartPoint>();
        var labels = new List<ChartAxisLabel>();
        var colors = new List<ChartColor?>();
        var index = 1;
        foreach (var item in items) {
            points.Add(new ChartPoint(index, item.Weight));
            labels.Add(new ChartAxisLabel(index, item.Text));
            colors.Add(item.Color);
            index++;
        }

        if (points.Count == 0) throw new ArgumentException("Word clouds must contain at least one term.", nameof(items));
        Options.XAxisLabels.Clear();
        Options.XAxisLabels.AddRange(labels);
        Add(name, ChartSeriesKind.WordCloud, points, color);
        Series[Series.Count - 1].PointColors.AddRange(colors);
        return this;
    }
}
