using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a candlestick series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="candles">The open, high, low, and close values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddCandlestick(string name, IEnumerable<ChartCandlestick> candles, ChartColor? color = null) {
        var series = ChartSeries.CreateCandlestick(name, candles, color);
        if (series.ObservationCount == 0) throw new ArgumentException("The series must contain at least one observation.", nameof(candles));
        AppendSeries(series);
        return this;
    }
}
