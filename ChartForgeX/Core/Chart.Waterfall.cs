using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds signed changes and one automatically appended final total.</summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">Changes in input order. X supplies the category coordinate and Y supplies the raw change.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddWaterfall(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) =>
        Add(name, ChartSeriesKind.Waterfall, points, color);

    /// <summary>Adds immutable changes and explicit calculated checkpoints. No extra final total is appended.</summary>
    /// <param name="name">The series name.</param>
    /// <param name="items">Items in accumulation order, each with a distinct display coordinate.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    /// <remarks>Use existing axis labels and point overrides; point overrides index every authored item, including checkpoints.</remarks>
    public Chart AddWaterfall(string name, IEnumerable<ChartWaterfallItem> items, ChartColor? color = null) {
        var series = new ChartSeries(name, ChartSeriesKind.Waterfall, Array.Empty<ChartPoint>()) { Color = color };
        series.SetWaterfallItems(items);
        AppendSeries(series);
        return this;
    }
}
