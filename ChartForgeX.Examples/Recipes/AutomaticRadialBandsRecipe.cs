/// <summary>Three independent service mixes use source-ordered automatic Donut bands.</summary>
public static class AutomaticRadialBandsRecipe {
    /// <summary>Creates immutable allocation without assigning or changing authored RadialBand values.</summary>
    public static Chart Create(string variant = "automatic-rings", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("automatic-rings" or "compact-automatic-rings")) throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = Chart.Create().WithXLabels("Subscriptions", "Consulting", "Support")
            .WithAutomaticRadialBands(new ChartRadialBandAllocation(.2, .04)).WithDonutCenterLabel(false)
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent)
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
        var values = new[] { new[] { 50d, 30, 20 }, new[] { 90d, 45, 15 }, new[] { 110d, 66, 44 } };
        for (var index = 0; index < values.Length; index++) {
            var series = new ChartSeries(new ChartSeriesId("period-" + index), "Period " + (index + 1), ChartSeriesKind.Donut,
                values[index].Select((value, category) => new ChartPoint(category + 1, value)))
                .WithObservationIds(new[] { new ChartObservationId("subscriptions"), new ChartObservationId("consulting"), new ChartObservationId("support") })
                .WithDataLabels(index == values.Length - 1);
            if (index == values.Length - 1) {
                series.DataLabelPlacement = ChartDataLabelPlacement.Outside;
                series.WithPointSliceRadiusRatio(0, .35).WithPointSliceRadiusRatio(1, .7).WithPointSliceRadiusRatio(2, 1);
            }
            chart.AddSeries(series);
        }
        return chart;
    }
}
