/// <summary>Four equal donut shares with labels above or below their measured anchors.</summary>
public static class EqualDonutRecipe {
    /// <summary>Creates the symmetric label example using above-labels or below-labels.</summary>
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("wide" or "above-labels" or "below-labels")) throw new ArgumentOutOfRangeException(nameof(variant));
        var shares = new ChartSeries(new ChartSeriesId("equal-shares"), "Equal shares", ChartSeriesKind.Donut,
            Enumerable.Range(1, 4).Select(index => new ChartPoint(index, 1)));
        return Chart.Create().XLabels("A", "B", "C", "D").DataLabels()
            .Pie(p => p.SliceLabelContent(ChartPieSliceLabelContent.Label).ShowCenterLabel(false))
            .DataLabels(d => d.Placement(variant == "below-labels" ? ChartDataLabelPlacement.Below : ChartDataLabelPlacement.Above))
            .AddSeries(shares);
    }
}
