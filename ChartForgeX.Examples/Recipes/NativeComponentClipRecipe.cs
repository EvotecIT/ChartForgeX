/// <summary>Retains full authored tuples while the native compiler owns plot clipping.</summary>
public static class NativeComponentClipRecipe {
    /// <summary>Creates an explicit fixed native window and real values entering that window.</summary>
    public static Chart Create(string family, bool finish, int width, VisualThemeMode mode, string fontFamily) {
        if (family is not ("bubble" or "range" or "error" or "candlestick" or "ohlc")) throw new ArgumentOutOfRangeException(nameof(family));
        var value = finish ? 40d : 120d; var chart = Chart.Create().WithTitle("Raw values through native clip").WithSize(width, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(fontFamily), mode).WithLegend(true).WithDataLabels(false);
        var id = new ChartSeriesId("delivery"); ChartSeries series;
        if (family == "bubble") {
            series = ChartSeries.CreateBubble(id, family, new[] { new ChartBubble(1, value, 20), new ChartBubble(3, 45, 30) });
            chart.ConfigureBubble(options => { options.WithSizeDomain(0, 100); options.SizeDimension.ShowLegend = false; options.MinimumRadius = 6; options.MaximumRadius = 18; });
        } else if (family == "range") series = ChartSeries.CreateRangeArea(id, family, new[] { new ChartRangeBand(1, value - 10, value + 10), new ChartRangeBand(3, 35, 55) });
        else if (family == "error") series = ChartSeries.CreateErrorBar(id, family, new[] { new ChartErrorBar(1, value, yBounds: new ChartErrorBounds(value - 10, value + 10)), new ChartErrorBar(3, 45, yBounds: new ChartErrorBounds(35, 55)) });
        else {
            var prices = new[] { new ChartCandlestick(1, value, value + 20, value - 10, value + 10), new ChartCandlestick(3, 45, 70, 20, 50) };
            series = family == "ohlc" ? ChartSeries.CreateOhlc(id, family, prices) : ChartSeries.CreateCandlestick(id, family, prices);
        }
        chart.AddSeries(series.WithObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later") })); chart.ConfigureXAxis(axis => { axis.Minimum = 0; axis.Maximum = 4; }); chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 100; }); return chart;
    }
}
