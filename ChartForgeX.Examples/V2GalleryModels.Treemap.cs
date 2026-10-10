using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

public static partial class V2GalleryModels {
    private static Chart Treemap(string variant, VisualThemeMode mode) {
        var items = variant == "sparse" ? new[] {
            new ChartHierarchyItem("independent", "Independent team", value: 100, colorValue: 7),
            new ChartHierarchyItem("reserve", "Reserve", value: 0)
        } : new[] {
            new ChartHierarchyItem("north", "North"),
            new ChartHierarchyItem("north-services", "Services", parentId: "north"),
            new ChartHierarchyItem("north-support", "Support", parentId: "north-services", value: 18, colorValue: -4),
            new ChartHierarchyItem("north-research", "Research", parentId: "north-services", value: 12, colorValue: 11),
            new ChartHierarchyItem("north-platform", "Platform", parentId: "north", value: 18, colorValue: 3),
            new ChartHierarchyItem("south", "South"),
            new ChartHierarchyItem("south-services", "Services", parentId: "south"),
            new ChartHierarchyItem("south-support", "Support", parentId: "south-services", value: 22, colorValue: 8),
            new ChartHierarchyItem("south-field", "Field", parentId: "south-services", value: 15, colorValue: 1),
            new ChartHierarchyItem("south-operations", "Operations", parentId: "south", value: 15),
            new ChartHierarchyItem("research", "Independent research", value: 8, colorValue: 14),
            new ChartHierarchyItem("reserve", "Reserve", value: 0)
        };
        var colors = VisualTheme.Graphite().Resolve(mode);
        var low = mode == VisualThemeMode.Dark ? ChartColor.FromRgb(153, 138, 224) : ChartColor.FromRgb(101, 78, 169);
        var high = mode == VisualThemeMode.Dark ? ChartColor.FromRgb(238, 157, 98) : ChartColor.FromRgb(191, 92, 34);
        var scale = variant == "options" ? ChartColorScale.Discrete(new[] {
            new ChartColorBand(0, low, "Decline"), new ChartColorBand(10, colors.Border, "Steady"), new ChartColorBand(null, high, "Growth")
        }) : ChartColorScale.Diverging(low, colors.Border, high, 0).WithValueRange(-10, 15).WithLabels("Decrease", "No change", "Increase");
        var chart = Chart.Create().WithDataLabels(true).AddTreemap("Allocated work", items).ConfigureTreemap(options => {
            options.GroupPadding = variant == "options" ? 8 : 6;
            options.Gap = 3;
            options.ShowGroupLabels = true;
            options.ColorLegendTitle = "Change (%)";
            options.ColorScale = scale.WithNoDataColor(colors.Surface);
        });
        if (variant == "options") chart.Series[0].WithNodeState("south-support", ChartSeriesState.Warning);
        return chart;
    }
}
