/// <summary>Builds one monthly revenue chart per legend option: reference-line entries, value-formatted entries and authored keys.</summary>
public static class LegendControlRecipe {
    /// <summary>Gets the option variants this recipe draws; each also has a compact form.</summary>
    public static readonly string[] Options = { "legend-reference", "legend-values", "legend-keys" };

    /// <summary>Creates the selected legend option example.</summary>
    public static Chart Create(string variant = "legend-reference", VisualThemeMode mode = VisualThemeMode.Light) {
        var option = variant.StartsWith("compact-", StringComparison.Ordinal) ? variant.Substring(8) : variant;
        if (!Options.Contains(option)) throw new ArgumentOutOfRangeException(nameof(variant));
        // Twelve months of revenue and cost, in dollars, against a 1.2M monthly target and a tolerance band.
        var months = Enumerable.Range(1, 12).ToArray();
        var target = ChartColor.FromHex("#D97706");
        var chart = Chart.Create().WithXAxis("Month").WithYAxis("USD").WithValueFormat(ChartValueFormat.Compact())
            .AddLine("Revenue", months.Select(month => new ChartPoint(month, 940_000 + month * 31_000 + month % 3 * 42_000)).ToArray())
            .AddLine("Cost", months.Select(month => new ChartPoint(month, 610_000 + month * 18_000 + month % 4 * 25_000)).ToArray());
        switch (option) {
            case "legend-reference":
                // The target line and its tolerance band are annotations; the legend names them beside the series.
                chart.AddHorizontalBand(1_100_000, 1_300_000, color: target, opacity: .12).AddHorizontalLine(1_200_000, color: target)
                    .AddLegendItem(ChartLegendItem.Line("Target 1.2M", target, ChartLegendItem.ReferenceDash, 1.6))
                    .AddLegendItem(ChartLegendItem.Band("Tolerance ±100k", target, .12));
                break;
            case "legend-values":
                // Each series entry carries its annual total, formatted like the value axis.
                chart.WithLegendFormatter(item => item.Series == null ? null : item.Label + " — " + item.FormatValue(item.Total) + " total");
                break;
            case "legend-keys":
                // Diamond and square markers on every point, larger legend keys that draw them, and an entry for the review lines.
                chart.Series[0].ConfigureMarkers(markers => { markers.Enabled = true; markers.Shape = ChartMarkerShape.Diamond; });
                chart.Series[1].ConfigureMarkers(markers => { markers.Enabled = true; markers.Shape = ChartMarkerShape.Square; });
                chart.WithLegendKey(ChartLegendKeyShape.Marker, 12).AddVerticalLine(6, "H1 review", target).AddVerticalLine(12, "Year end", target)
                    .AddLegendItem(ChartLegendItem.Line("Review month", target, ChartLegendItem.ReferenceDash, 1.6));
                break;
        }
        return chart;
    }
}
