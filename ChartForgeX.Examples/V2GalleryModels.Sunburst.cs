using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

public static partial class V2GalleryModels {
    private static Chart Sunburst(string variant, VisualThemeMode mode) {
        var inclusive = variant is "authored-total" or "compact-authored-total";
        var configured = inclusive || variant is "options" or "compact-options";
        var items = new[] {
            new ChartHierarchyItem("all", "All teams", value: configured ? 140 : null, colorValue: configured ? -10 : null),
            new ChartHierarchyItem("engineering", "Engineering", "all", configured ? 80 : null, configured ? 12 : null),
            new ChartHierarchyItem("operations", "Operations", "all", colorValue: configured ? 0 : null),
            new ChartHierarchyItem("platform", "Platform", "engineering", 35, configured ? -4 : null),
            new ChartHierarchyItem("engineering-support", "Support", "engineering", 25, configured ? 5 : null),
            new ChartHierarchyItem("operations-support", "Support", "operations", 40),
            new ChartHierarchyItem("reserve", "Reserve", "operations", 0)
        };
        var chart = Chart.Create().WithDataLabels().AddSunburst("Allocated work", items);
        if (configured) {
            var colors = VisualTheme.Graphite().Resolve(mode);
            var low = mode == VisualThemeMode.Dark ? ChartColor.FromRgb(153, 138, 224) : ChartColor.FromRgb(101, 78, 169);
            var high = mode == VisualThemeMode.Dark ? ChartColor.FromRgb(238, 157, 98) : ChartColor.FromRgb(191, 92, 34);
            chart.ConfigureSunburst(options => {
                options.CornerRadius = 6;
                options.SecondaryLabelFormatter = context => context.Item.Id == "all" ? "Allocation" : context.Item.Id == "engineering" ? "Delivery teams"
                    : context.Item.Id == "operations-support" ? "Shared service" : null;
                options.ParentValuePolicy = inclusive ? ChartHierarchyValuePolicy.AuthoredTotal : ChartHierarchyValuePolicy.LeafAggregate;
                options.ColorLegendTitle = "Change (%)";
                options.ColorScale = ChartColorScale.Diverging(low, colors.Border, high, 0).WithValueRange(-10, 15)
                    .WithLabels("Decrease", "No change", "Increase").WithNoDataColor(colors.Surface);
            });
            chart.Series[0].WithNodeState("engineering-support", ChartSeriesState.Warning);
            chart.Series[0].WithPointFillPattern(4, ChartFillPattern.DiagonalForward);
        }
        return chart;
    }
}
