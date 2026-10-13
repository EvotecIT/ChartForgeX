/// <summary>Creates a treemap with task counts and independently formatted temperature colors.</summary>
public static class TreemapColorDimensionRecipe {
    /// <summary>Creates the source model for a gallery variant without choosing an export backend.</summary>
    public static Chart Create(string variant = "color-dimension", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("color-dimension" or "compact-color-dimension"))
            throw new ArgumentOutOfRangeException(nameof(variant));

        var colors = VisualTheme.Graphite().Resolve(mode);
        var allocation = ChartSeries.CreateTreemap(new ChartSeriesId("allocation-source"), "Allocated work", new[] {
            new ChartHierarchyItem("north", "North"),
            new ChartHierarchyItem("support", "Support", parentId: "north", value: 18, colorValue: -4),
            new ChartHierarchyItem("research", "Research", parentId: "north", value: 12, colorValue: 11),
            new ChartHierarchyItem("south", "South"),
            new ChartHierarchyItem("field", "Field", parentId: "south", value: 22, colorValue: 8),
            new ChartHierarchyItem("platform", "Platform", parentId: "south", value: 15, colorValue: 1),
            new ChartHierarchyItem("operations", "Operations", parentId: "south", value: 15)
        });
        var scale = ChartColorScale.Diverging(new[] {
            new ChartColorStop(-20, ChartColor.FromHex("#2166AC")), new ChartColorStop(-3, ChartColor.FromHex("#67A9CF"))
        }, new ChartColorStop(0, ChartColor.FromHex("#D1D5DB")), new[] {
            new ChartColorStop(5, ChartColor.FromHex("#D9A441")), new ChartColorStop(40, ChartColor.FromHex("#B2182B"))
        }).WithLabels("Cold", "Freezing", "Warm").WithNoDataColor(colors.Border);
        return Chart.Create().ConfigureLabels(labels => labels.NoData = "Not sampled")
            .WithValueFormat(ChartValueFormat.Number("0 'tasks'", System.Globalization.CultureInfo.InvariantCulture)).WithDataLabels()
            .AddSeries(allocation).ConfigureTreemap(options => {
                options.Gap = 3;
                options.GroupPadding = 6;
                options.ColorScale = scale;
                options.ColorDimension.Name = "Temperature";
                options.ColorDimension.ValueFormat = ChartValueFormat.Number("0.0 '°C'", System.Globalization.CultureInfo.GetCultureInfo("pl-PL"));
                options.ColorDimension.LegendStyle = ChartColorLegendStyle.Auto;
            });
    }
}
