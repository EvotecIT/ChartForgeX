/// <summary>Compares complete stage observations without inventing values for unrecorded stages.</summary>
public static class SlopeStagesRecipe {
    /// <summary>Creates ordered stage lines, including one explicit disconnected interval, on either physical orientation.</summary>
    public static Chart Create(string variant = "stages", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("stages" or "compact-stages" or "horizontal-stages" or "compact-horizontal-stages"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithXLabels("Received", "Prepared", "Reviewed", "Accepted")
            .WithLegend(true).WithDataLabels(false)
            .AddSlope("Standard", new[] { new ChartPoint(1, 28), new ChartPoint(2, 43), new ChartPoint(3, 38), new ChartPoint(4, 64) })
            .AddSlope("Express", new[] { new ChartPoint(1, 52), new ChartPoint(2, 49), new ChartPoint(3, 58, true), new ChartPoint(4, 71) })
            .WithValueFormat(ChartValueFormat.Number("0", CultureInfo.InvariantCulture));
        var ids = new[] { "received", "prepared", "reviewed", "accepted" }.Select(id => new ChartObservationId(id)).ToArray();
        foreach (var series in chart.Series) series.WithObservationIds(ids);
        if (variant.EndsWith("horizontal-stages", StringComparison.Ordinal)) {
            chart.Options.SlopeOrientation = ChartOrientation.Horizontal;
            chart.WithXAxis("Recorded capacity").WithYAxis("Delivery stage");
        } else chart.WithXAxis("Delivery stage").WithYAxis("Recorded capacity");
        return chart;
    }
}
