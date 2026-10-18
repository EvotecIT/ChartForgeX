using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a range-band series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="bands">The lower and upper band values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRangeBand(string name, IEnumerable<ChartRangeBand> bands, ChartColor? color = null) {
        var series = ChartSeries.CreateRangeBand(name, bands, color);
        AppendSeries(series);
        return this;
    }
}
