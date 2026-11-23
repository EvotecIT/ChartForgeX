/// <summary>Compares complete stage observations without inventing values for unrecorded stages.</summary>
public static class SlopeStagesRecipe {
    /// <summary>Creates ordered stage lines, including one explicit disconnected interval, on either physical orientation.</summary>
    public static Chart Create(string variant = "stages", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("stages" or "compact-stages" or "horizontal-stages" or "compact-horizontal-stages"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = Chart.Create().Theme(VisualTheme.Graphite()).ThemeMode(mode).XLabels("Received", "Prepared", "Reviewed", "Accepted")
            .Legend(true).DataLabels(false)
            .AddSlope("Standard", new[] { new ChartPoint(1, 28), new ChartPoint(2, 43), new ChartPoint(3, 38), new ChartPoint(4, 64) })
            .AddSlope("Express", new[] { new ChartPoint(1, 52), new ChartPoint(2, 49), new ChartPoint(3, 58, true), new ChartPoint(4, 71) })
            .ValueFormat(ChartValueFormat.Number("0", CultureInfo.InvariantCulture));
        var ids = new[] { "received", "prepared", "reviewed", "accepted" }.Select(id => new ChartObservationId(id)).ToArray();
        foreach (var series in chart.Series) series.ObservationIds(ids);
        if (variant.EndsWith("horizontal-stages", StringComparison.Ordinal)) {
            chart.Options.SlopeOrientation = ChartOrientation.Horizontal;
            chart.XAxis(x => x.Title("Recorded capacity")).YAxis(y => y.Title("Delivery stage"));
        } else chart.XAxis(x => x.Title("Delivery stage")).YAxis(y => y.Title("Recorded capacity"));
        return chart;
    }
}
