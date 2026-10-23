/// <summary>Two independent service mixes in explicit concentric Pie and Donut bands.</summary>
public static class ConcentricPieRecipe {
    /// <summary>Creates independent totals and source identities without implying equal totals across rings.</summary>
    public static Chart Create(string variant = "concentric", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("concentric" or "compact-concentric")) throw new ArgumentOutOfRangeException(nameof(variant));
        var prior = new ChartSeries(new ChartSeriesId("prior"), "Prior", ChartSeriesKind.Donut,
            new[] { new ChartPoint(1, 75), new ChartPoint(2, 25) })
            .WithRadialBand(new ChartRadialBand(.22, .52))
            .WithObservationIds(new[] { new ChartObservationId("subscriptions"), new ChartObservationId("consulting") });
        var current = new ChartSeries(new ChartSeriesId("current"), "Current", ChartSeriesKind.Pie,
            new[] { new ChartPoint(1, 132), new ChartPoint(2, 88) })
            .WithRadialBand(new ChartRadialBand(.66, 1))
            .WithObservationIds(new[] { new ChartObservationId("subscriptions"), new ChartObservationId("consulting") });
        prior.DataLabelPlacement = ChartDataLabelPlacement.Inside;
        current.DataLabelPlacement = ChartDataLabelPlacement.Inside;
        // The legend retains each period's values; keep these small concentric holes free of center text.
        return Chart.Create().WithXLabels("Subscriptions", "Consulting").WithDataLabels()
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.Percent).WithDonutCenterLabel(false)
            .AddSeries(prior).AddSeries(current)
            .WithValueFormat(ChartValueFormat.Number("0.##", System.Globalization.CultureInfo.InvariantCulture));
    }
}
