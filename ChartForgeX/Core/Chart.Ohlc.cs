using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds an OHLC series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="values">The open, high, low, and close values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddOhlc(string name, IEnumerable<ChartCandlestick> values, ChartColor? color = null) {
        var series = ChartSeries.CreateOhlc(name, values, color);
        if (series.ObservationCount == 0) throw new ArgumentException("The series must contain at least one observation.", nameof(values));
        AppendSeries(series);
        return this;
    }
}
