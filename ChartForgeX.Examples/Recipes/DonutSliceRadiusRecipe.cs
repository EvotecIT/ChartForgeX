/// <summary>Authored Donut radii remain independent of each value's angular share.</summary>
public static class DonutSliceRadiusRecipe {
    /// <summary>Creates service shares with explicit source identities and normalized band-relative radii.</summary>
    public static Chart Create(string variant = "variable-radius", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("variable-radius" or "compact-variable-radius")) throw new ArgumentOutOfRangeException(nameof(variant));
        var services = new ChartSeries(new ChartSeriesId("services"), "Services", ChartSeriesKind.Donut,
            new[] { new ChartPoint(1, 80), new ChartPoint(2, 60), new ChartPoint(3, 20) })
            .WithObservationIds(new[] { new ChartObservationId("subscriptions"), new ChartObservationId("consulting"), new ChartObservationId("support") })
            .WithPointSliceRadiusRatio(0, .35).WithPointSliceRadiusRatio(1, .7).WithPointSliceRadiusRatio(2, 1);
        services.DataLabelPlacement = ChartDataLabelPlacement.Outside;
        return Chart.Create().WithXLabels("Subscriptions", "Consulting", "Support").WithDataLabels()
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).WithDonutCenterLabel(false)
            .AddSeries(services).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}
