using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {

    /// <summary>
    /// Adds a range-bar series with the configured orientation and one distinct category slot per series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="intervals">The interval values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRangeBar(string name, IEnumerable<ChartInterval> intervals, ChartColor? color = null) {
        var series = ChartSeries.CreateRangeBar(name, intervals, color);
        AppendSeries(series);
        return this;
    }
}
