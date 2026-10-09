using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

public static partial class V2GalleryModels {
    private static Chart Treemap(string variant, VisualThemeMode mode) {
        var items = variant == "sparse" ? new[] {
            new ChartTreemapItem("independent", "Independent team", value: 100, colorValue: 7),
            new ChartTreemapItem("reserve", "Reserve", value: 0)
        } : new[] {
            new ChartTreemapItem("north", "North"),
            new ChartTreemapItem("north-services", "Services", parentId: "north"),
            new ChartTreemapItem("north-support", "Support", parentId: "north-services", value: 18, colorValue: -4),
            new ChartTreemapItem("north-research", "Research", parentId: "north-services", value: 12, colorValue: 11),
            new ChartTreemapItem("north-platform", "Platform", parentId: "north", value: 18, colorValue: 3),
            new ChartTreemapItem("south", "South"),
            new ChartTreemapItem("south-services", "Services", parentId: "south"),
            new ChartTreemapItem("south-support", "Support", parentId: "south-services", value: 22, colorValue: 8),
            new ChartTreemapItem("south-field", "Field", parentId: "south-services", value: 15, colorValue: 1),
            new ChartTreemapItem("south-operations", "Operations", parentId: "south", value: 15),
            new ChartTreemapItem("research", "Independent research", value: 8, colorValue: 14),
            new ChartTreemapItem("reserve", "Reserve", value: 0)
        };
        var colors = VisualTheme.Graphite().Resolve(mode);
        var low = mode == VisualThemeMode.Dark ? ChartColor.FromRgb(153, 138, 224) : ChartColor.FromRgb(101, 78, 169);
        var high = mode == VisualThemeMode.Dark ? ChartColor.FromRgb(238, 157, 98) : ChartColor.FromRgb(191, 92, 34);
        var scale = variant == "options" ? ChartColorScale.Discrete(new[] {
            new ChartColorBand(0, low, "Decline"), new ChartColorBand(10, colors.Border, "Steady"), new ChartColorBand(null, high, "Growth")
        }) : ChartColorScale.Diverging(low, colors.Border, high, 0).WithValueRange(-10, 15).WithLabels("Decrease", "No change", "Increase");
        return Chart.Create().AddTreemap("Allocated work", items).ConfigureTreemap(options => {
            options.GroupPadding = variant == "options" ? 8 : 6;
            options.Gap = 3;
            options.ShowGroupLabels = true;
            options.ColorLegendTitle = "Change (%)";
            options.ColorScale = scale.WithNoDataColor(colors.Surface);
        });
    }
}
