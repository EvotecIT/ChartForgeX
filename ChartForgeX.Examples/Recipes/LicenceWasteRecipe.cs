/// <summary>Builds the bridge from purchased to effectively used Microsoft 365 licences, as a licence report shows it.</summary>
public static class LicenceWasteRecipe {
    /// <summary>Creates the licence waterfall; the compact variant keeps the same data.</summary>
    public static Chart Create(string variant = "licence-waste", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("licence-waste" or "compact-licence-waste")) throw new ArgumentOutOfRangeException(nameof(variant));
        var bridge = ChartSeries.CreateWaterfall(new ChartSeriesId("licences"), "Licences", new[] {
            ChartWaterfallItem.OpeningBalance(1, 1200), ChartWaterfallItem.Delta(2, -86), ChartWaterfallItem.Delta(3, -142),
            ChartWaterfallItem.Delta(4, -57), ChartWaterfallItem.Total(5), ChartWaterfallItem.Delta(6, -38), ChartWaterfallItem.Total(7)
        });
        var chart = Chart.Create().WithXLabels("Purchased", "Unassigned", "Inactive", "Disabled", "In use", "Duplicates", "Effective")
            .AddSeries(bridge).WithDataLabels().WithValueFormat(ChartValueFormat.Number("0", CultureInfo.InvariantCulture));
        // Seven named steps read better as rows on a phone than as shortened column captions.
        if (variant == "compact-licence-waste") {
            chart.ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal).WithXAxis("Licences");
            // Read the bridge top to bottom, from purchased seats to effective use.
            chart.Options.YAxis.WithReversal();
            return chart;
        }
        return chart.WithYAxis("Licences");
    }
}
