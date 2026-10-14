using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a range-area series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="ranges">The lower and upper interval values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <param name="smooth">A value indicating whether interval boundaries should be smoothed.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRangeArea(string name, IEnumerable<ChartRangeBand> ranges, ChartColor? color = null, bool smooth = true) {
        var series = ChartSeries.CreateRangeArea(name, ranges, color, smooth);
        if (series.ObservationCount == 0) throw new ArgumentException("The series must contain at least one observation.", nameof(ranges));
        AppendSeries(series);
        return this;
    }
}
