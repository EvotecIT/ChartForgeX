/// <summary>Authored Donut radii remain independent of each value's angular share.</summary>
public static class DonutSliceRadiusRecipe {
    /// <summary>Creates service shares with explicit source identities and normalized band-relative radii.</summary>
    public static Chart Create(string variant = "variable-radius", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("variable-radius" or "compact-variable-radius")) throw new ArgumentOutOfRangeException(nameof(variant));
        var services = new ChartSeries(new ChartSeriesId("services"), "Services", ChartSeriesKind.Donut,
            new[] { new ChartPoint(1, 80), new ChartPoint(2, 60), new ChartPoint(3, 20) })
            .ObservationIds(new[] { new ChartObservationId("subscriptions"), new ChartObservationId("consulting"), new ChartObservationId("support") })
            .PointSliceRadiusRatio(0, .35).PointSliceRadiusRatio(1, .7).PointSliceRadiusRatio(2, 1);
        services.DataLabelPlacement = ChartDataLabelPlacement.Outside;
        return Chart.Create().XLabels("Subscriptions", "Consulting", "Support").DataLabels()
            .Pie(p => p.SliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).ShowCenterLabel(false))
            .AddSeries(services);
    }
}
