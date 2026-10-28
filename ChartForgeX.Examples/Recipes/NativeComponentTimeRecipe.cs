/// <summary>Uses actual elapsed, session, ordinal or calendar category owners with fixed time coordinates.</summary>
public static class NativeComponentTimeRecipe {
    /// <summary>Creates copied authored temporal X policy and real numeric typed values.</summary>
    public static Chart Create(string family, string policy, bool finish, int width, VisualThemeMode mode, string fontFamily) {
        if (family is not ("candlestick" or "ohlc" or "bubble" or "range" or "error")) throw new ArgumentOutOfRangeException(nameof(family));
        var value = finish ? 60d : 40d; var first = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc); var later = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithTitle("Captured time categories").WithSize(width, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(fontFamily), mode).WithLegend(true).WithDataLabels(true);
        var id = new ChartSeriesId("delivery"); ChartSeries series;
        if (family == "bubble") {
            series = ChartSeries.CreateBubble(id, family, new[] { new ChartBubble(first, value, 20), new ChartBubble(later, 45, 30) });
            chart.ConfigureBubble(options => { options.WithSizeDomain(0, 100); options.SizeDimension.ShowLegend = false; options.MinimumRadius = 6; options.MaximumRadius = 18; });
        } else if (family == "range") series = ChartSeries.CreateRangeArea(id, family, new[] { new ChartRangeBand(first, 10, value), new ChartRangeBand(later, 20, 55) });
        else if (family == "error") series = ChartSeries.CreateErrorBar(id, family, new[] { new ChartErrorBar(first, value, yBounds: new ChartErrorBounds(value - 10, value + 10)), new ChartErrorBar(later, 45, yBounds: new ChartErrorBounds(35, 55)) });
        else {
            var prices = new[] { new ChartCandlestick(first, 40, 80, 10, value), new ChartCandlestick(later, 45, 70, 20, 50) };
            series = family == "ohlc" ? ChartSeries.CreateOhlc(id, family, prices) : ChartSeries.CreateCandlestick(id, family, prices);
        }
        chart.AddSeries(series.WithObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later") })); chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 100; });
        chart.WithXAxisTimeScale(TimeZoneInfo.Utc).WithXAxisBounds(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToOADate(), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc).ToOADate());
        chart.ConfigureXAxis(axis => {
            if (policy == "sessions") axis.WithTimeSessions(new ChartTimeSessionDomain(new[] { new ChartTimeSession(first.AddDays(-1), first.AddDays(1)), new ChartTimeSession(later.AddDays(-1), later.AddDays(1)) }));
            else if (policy == "ordinal") axis.WithTimeSpacing(ChartTimeSpacing.Ordinal);
            else if (policy == "calendar") axis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month)));
            else if (policy != "elapsed") throw new ArgumentOutOfRangeException(nameof(policy));
        });
        return chart;
    }
}
