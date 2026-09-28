using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a progress-bar chart that visualizes labeled values against a shared maximum.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="items">The labeled values to render.</param>
    /// <param name="maximum">The shared maximum value.</param>
    /// <param name="color">An optional base color. When null, the theme palette colors each row.</param>
    /// <returns>The current chart.</returns>
    public Chart AddProgressBars(string name, IEnumerable<ChartProgressItem> items, double maximum = 100, ChartColor? color = null) {
        EnsureCanAddSeries();
        if (items == null) throw new ArgumentNullException(nameof(items));
        ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Progress maximum must be greater than zero.");
        var points = new List<ChartPoint>();
        var labels = new List<ChartAxisLabel>();
        var colors = new List<ChartColor?>();
        var index = 1;
        foreach (var item in items) {
            points.Add(new ChartPoint(index, item.Value));
            labels.Add(new ChartAxisLabel(index, item.Label));
            colors.Add(item.Color);
            index++;
        }

        if (points.Count == 0) throw new ArgumentException("Progress-bar charts must contain at least one item.", nameof(items));
        Options.ProgressMaximum = maximum;
        Options.XAxisLabels.Clear();
        Options.XAxisLabels.AddRange(labels);
        Add(name, ChartSeriesKind.ProgressBar, points, color);
        Series[Series.Count - 1].PointColors.AddRange(colors);
        return this;
    }
}
