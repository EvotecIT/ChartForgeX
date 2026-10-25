/// <summary>Opts into bounded caption shrinking and whole-text-element ellipsis while retaining complete source names.</summary>
public static class TreemapCaptionPoliciesRecipe {
    /// <summary>Creates the complete source model; layout and exports use the existing Treemap owner.</summary>
    public static Chart Create(string variant = "caption-policies", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("caption-policies" or "compact-caption-policies")) throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = Chart.Create().WithLegend(false).WithDataLabels().WithTransparentBackground(false)
            .AddTreemap("Reconciliation", new[] {
                new ChartHierarchyItem("all", "International reconciliation, settlement and regional ownership — complete source caption"),
                new ChartHierarchyItem("processing", "Regional processing reconciliation and settlement allocation", "all", 55),
                new ChartHierarchyItem("regional", "North America processing", "all", 30),
                new ChartHierarchyItem("reserve", "Reserve capacity", "all", 15)
            }).ConfigureTreemap(options => {
                options.MinimumLabelFontSize = 11;
                options.LabelTrimming = ChartForgeX.Typography.TextTrimming.Ellipsis;
                options.ShowValues = true;
            });
        chart.Series[0].ConfigureDataLabelStyle(style => style.WithFontSize(14));
        return chart.WithValueFormat(ChartValueFormat.Number("0", System.Globalization.CultureInfo.InvariantCulture));
    }
}
