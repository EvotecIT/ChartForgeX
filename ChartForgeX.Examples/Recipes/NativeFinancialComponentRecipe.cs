/// <summary>Creates actual OHLC price tuples with native direction and observation policies.</summary>
public static class NativeFinancialComponentRecipe {
    /// <summary>Creates an authored fixed numeric price source with caller-owned fonts.</summary>
    public static Chart Create(bool ohlc, bool finish, int width, VisualThemeMode mode, string family) {
        var open = finish ? 60d : 40d; var high = finish ? 80d : 70d; var low = finish ? 10d : 20d; var close = finish ? 40d : 60d;
        var chart = Chart.Create().WithTitle("Actual financial components").WithSize(width, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(family), mode).WithLegend(true).WithDataLabels(true);
        foreach (var key in new[] { "delivery", "comparison" }) {
            var changed = key == "delivery"; var values = new[] { new ChartCandlestick(1, changed ? open : 40, changed ? high : 70, changed ? low : 20, changed ? close : 60), new ChartCandlestick(3, 50, 75, 25, 45) };
            var series = (ohlc ? ChartSeries.CreateOhlc(new ChartSeriesId(key), key, values) : ChartSeries.CreateCandlestick(new ChartSeriesId(key), key, values))
                .WithObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later") });
            {
                series.ConfigureFinancial(options => {
                    options.Rising.Stroke = ChartColor.FromHex("#218C74"); options.Rising.StrokeWidth = 2.5; options.Rising.DashPattern = new[] { 3d, 2d }; options.Rising.DashOffset = -1;
                    options.Falling.Stroke = ChartColor.FromHex("#CF6D45"); options.Falling.StrokeWidth = 3; options.Falling.DashPattern = new[] { 2d, 1d }; options.Falling.StrokeOpacity = .8;
                    if (!ohlc) { options.Rising.FillOpacity = .25; options.Falling.FillOpacity = 0; options.Rising.Wick.StrokeWidth = 1.5; options.Falling.Wick.DashPattern = new[] { 4d, 2d }; }
                });
                series.ConfigureFinancialValue(0, options => { options.StrokeWidth = 4; if (!ohlc) options.Wick.Stroke = ChartColor.FromHex("#3987E5"); });
                series.WithPointColor(1, ChartColor.FromHex("#729BB7"));
            }
            chart.AddSeries(series);
        }
        chart.ConfigureXAxis(axis => { axis.Minimum = 0; axis.Maximum = 4; }); chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 100; }); return chart;
    }
}
