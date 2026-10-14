using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds an error-bar series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="errorBars">The point estimates and bounds to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddErrorBar(string name, IEnumerable<ChartErrorBar> errorBars, ChartColor? color = null) {
        var series = ChartSeries.CreateErrorBar(name, errorBars, color);
        if (series.ObservationCount == 0) throw new ArgumentException("The series must contain at least one observation.", nameof(errorBars));
        AppendSeries(series);
        return this;
    }
}
