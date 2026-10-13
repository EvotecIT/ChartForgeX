/// <summary>Four equal pie shares with labels above or below their measured anchors.</summary>
public static class EqualPieRecipe {
    /// <summary>Creates the symmetric label example using above-labels or below-labels.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "above-labels" or "below-labels")) throw new ArgumentOutOfRangeException(nameof(variant));
        var shares = new ChartSeries(new ChartSeriesId("equal-shares"), "Equal shares", ChartSeriesKind.Pie,
            Enumerable.Range(1, 4).Select(index => new ChartPoint(index, 1)));
        return Chart.Create().WithXLabels("A", "B", "C", "D").WithDataLabels()
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.Label).WithDonutCenterLabel(false)
            .WithDataLabelPlacement(variant == "below-labels" ? ChartDataLabelPlacement.Below : ChartDataLabelPlacement.Above)
            .AddSeries(shares);
    }
}
