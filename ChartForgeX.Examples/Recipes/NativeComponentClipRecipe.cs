/// <summary>Retains full authored tuples while the native compiler owns plot clipping.</summary>
public static class NativeComponentClipRecipe {
    /// <summary>Creates an explicit fixed native window and real values entering that window.</summary>
    public static Chart Create(string family, bool finish, int width, ThemeMode mode, string fontFamily) {
        if (family is not ("bubble" or "range" or "error" or "candlestick" or "ohlc")) throw new ArgumentOutOfRangeException(nameof(family));
        var value = finish ? 40d : 120d; var chart = Chart.Create().Title("Raw values through native clip").Size(width, 420).Theme(VisualTheme.Graphite().WithFontFamily(fontFamily)).ThemeMode(mode).Legend(true).DataLabels(false);
        var id = new ChartSeriesId("delivery"); ChartSeries series;
        if (family == "bubble") {
            series = ChartSeries.CreateBubble(id, family, new[] { new ChartBubble(1, value, 20), new ChartBubble(3, 45, 30) });
            chart.Bubble(options => { options.SizeDomain(0, 100); options.SizeDimension.ShowLegend = false; options.MinimumRadius = 6; options.MaximumRadius = 18; });
        } else if (family == "range") series = ChartSeries.CreateRangeArea(id, family, new[] { new ChartRangeBand(1, value - 10, value + 10), new ChartRangeBand(3, 35, 55) });
        else if (family == "error") series = ChartSeries.CreateErrorBar(id, family, new[] { new ChartErrorBar(1, value, yBounds: new ChartErrorBounds(value - 10, value + 10)), new ChartErrorBar(3, 45, yBounds: new ChartErrorBounds(35, 55)) });
        else {
            var prices = new[] { new ChartCandlestick(1, value, value + 20, value - 10, value + 10), new ChartCandlestick(3, 45, 70, 20, 50) };
            series = family == "ohlc" ? ChartSeries.CreateOhlc(id, family, prices) : ChartSeries.CreateCandlestick(id, family, prices);
        }
        chart.AddSeries(series.ObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later") })); chart.XAxis(axis => { axis.Min = 0; axis.Max = 4; }); chart.YAxis(axis => { axis.Min = 0; axis.Max = 100; }); return chart;
    }
}
