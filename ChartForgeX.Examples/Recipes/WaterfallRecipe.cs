/// <summary>Builds running-balance waterfalls with signed changes and explicit checkpoints.</summary>
public static class WaterfallRecipe {
    /// <summary>Creates the selected waterfall example and its orientation.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options" or "horizontal" or "compact-horizontal"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = Chart.Create();
        if (variant is "options" or "compact-options" or "horizontal" or "compact-horizontal") {
            var horizontal = variant is "horizontal" or "compact-horizontal";
            var movement = ChartSeries.CreateWaterfall(new ChartSeriesId("movement"), "Movement", new[] {
                ChartWaterfallItem.OpeningBalance(1, 120), ChartWaterfallItem.Delta(2, -30), ChartWaterfallItem.Subtotal(3),
                ChartWaterfallItem.Delta(4, -130), ChartWaterfallItem.Subtotal(5), ChartWaterfallItem.Delta(6, 70),
                ChartWaterfallItem.Total(7)
            }).WithPointColor(4, "#A5358A");
            chart.WithXLabels("Open", "Adjust", "Stage", "Costs", "Cost sum", "Recover", "Balance").AddSeries(movement)
                .ConfigureWaterfall(options => options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical);
            if (horizontal) chart.WithXAxis("Running balance").WithYAxis("Stage").WithBarStyle(ChartBarStyle.SegmentedCapsule);
            else chart.WithYAxis("Running balance");
            chart.Options.XAxis.WithReversal(!horizontal || variant == "compact-horizontal");
            chart.Options.YAxis.WithReversal();
            chart.WithDataLabels();
        }
        else {
            var changes = new ChartSeries(new ChartSeriesId("changes"), "Changes", ChartSeriesKind.Waterfall, new[] {
                new ChartPoint(1, 60), new ChartPoint(2, -15), new ChartPoint(3, 25), new ChartPoint(4, -10), new ChartPoint(5, 12)
            });
            chart.WithXLabels("North", "South", "East", "West", "Central", "Total").AddSeries(changes);
        }
        return chart.WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}
