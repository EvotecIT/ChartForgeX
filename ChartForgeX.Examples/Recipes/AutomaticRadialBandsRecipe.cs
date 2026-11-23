/// <summary>Three independent service mixes use source-ordered automatic Donut bands.</summary>
public static class AutomaticRadialBandsRecipe {
    /// <summary>Creates immutable allocation without assigning or changing authored RadialBand values.</summary>
    public static Chart Create(string variant = "automatic-rings", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("automatic-rings" or "compact-automatic-rings")) throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = Chart.Create().XLabels("Subscriptions", "Consulting", "Support")
            .RadialBandAllocation(new ChartRadialBandAllocation(.2, .04)).Pie(p => p.ShowCenterLabel(false).SliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent));
        var values = new[] { new[] { 50d, 30, 20 }, new[] { 90d, 45, 15 }, new[] { 110d, 66, 44 } };
        for (var index = 0; index < values.Length; index++) {
            var series = new ChartSeries(new ChartSeriesId("period-" + index), "Period " + (index + 1), ChartSeriesKind.Donut,
                values[index].Select((value, category) => new ChartPoint(category + 1, value)))
                .ObservationIds(new[] { new ChartObservationId("subscriptions"), new ChartObservationId("consulting"), new ChartObservationId("support") })
                .ShowDataLabels(index == values.Length - 1);
            if (index == values.Length - 1) {
                series.DataLabelPlacement = ChartDataLabelPlacement.Outside;
                series.PointSliceRadiusRatio(0, .35).PointSliceRadiusRatio(1, .7).PointSliceRadiusRatio(2, 1);
            }
            chart.AddSeries(series);
        }
        return chart;
    }
}
