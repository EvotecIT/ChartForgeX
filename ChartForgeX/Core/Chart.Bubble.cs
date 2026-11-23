using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {

    /// <summary>
    /// Adds a bubble chart series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="bubbles">The bubble values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddBubble(string name, IEnumerable<ChartBubble> bubbles, ChartColor? color = null) {
        var series = ChartSeries.CreateBubble(name, bubbles, color);
        AppendSeries(series);
        return this;
    }
}
