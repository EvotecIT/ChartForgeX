using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a pictorial chart that visualizes values with repeated built-in symbols.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="items">The labeled values to render.</param>
    /// <param name="shape">The symbol shape.</param>
    /// <param name="color">An optional base color. When null, the theme palette colors each row.</param>
    /// <returns>The current chart.</returns>
    public Chart AddPictorial(string name, IEnumerable<ChartPictorialItem> items, ChartPictorialShape shape = ChartPictorialShape.Circle, ChartColor? color = null) {
        EnsureCanAddSeries();
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (!Enum.IsDefined(typeof(ChartPictorialShape), shape)) throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown pictorial shape.");
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

        if (points.Count == 0) throw new ArgumentException("Pictorial charts must contain at least one item.", nameof(items));
        Options.PictorialShape = shape;
        Options.XAxisLabels.Clear();
        Options.XAxisLabels.AddRange(labels);
        Add(name, ChartSeriesKind.Pictorial, points, color);
        Series[Series.Count - 1].PointColors.AddRange(colors);
        return this;
    }
}
