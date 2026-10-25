/// <summary>Places complete supported primary names on captured native circular lanes without changing hierarchy allocation.</summary>
public static class SunburstCurvedLabelsRecipe {
    /// <summary>Creates the source model for a compact or wide consumer, independently of the export backend.</summary>
    public static Chart Create(string variant = "curved-labels", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("curved-labels" or "compact-curved-labels")) throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = Chart.Create().WithLegend(false).WithDataLabels().WithTransparentBackground(false)
            .AddSunburst("Regional allocation", new[] {
                new ChartHierarchyItem("all", "Allocation"),
                new ChartHierarchyItem("operations", "Operations and service delivery", "all"),
                new ChartHierarchyItem("engineering", "Engineering and platform ownership", "all"),
                new ChartHierarchyItem("reconciliation", "Regional reconciliation", "operations", 25),
                new ChartHierarchyItem("settlement", "Settlement operations", "operations", 25),
                new ChartHierarchyItem("automation", "Automation services", "engineering", 25),
                new ChartHierarchyItem("platform", "Platform operations", "engineering", 25)
            }).ConfigureSunburst(options => {
                options.CurvedLabels = true;
                options.SectorGap = 3;
                options.CornerRadius = 3;
                options.MinimumLabelFontSize = 11;
            });
        chart.Series[0].ConfigureDataLabelStyle(style => style.WithFontSize(13));
        return chart;
    }
}
