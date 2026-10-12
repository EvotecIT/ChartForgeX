using ChartForgeX.Core;
using ChartForgeX.Primitives;

public static partial class V2GalleryModels {
    private static Chart? PlotRegionObservations(ChartSeriesKind kind, string variant) {
        var quadrants = kind == ChartSeriesKind.Scatter && variant is "quadrants" or "compact-quadrants";
        var bounded = kind == ChartSeriesKind.Bubble && variant is "plot-regions" or "compact-plot-regions";
        if (!quadrants && !bounded) return null;
        var chart = Chart.Create().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).WithDataLabels(false);
        if (quadrants) {
            return chart.WithXAxis("Effort (days)").WithYAxis("Impact (score)")
                .AddScatter("Proposed work", new[] { new ChartPoint(1.4, 8.1), new ChartPoint(3.1, 7.3),
                    new ChartPoint(7.2, 8.8), new ChartPoint(8.4, 6.8), new ChartPoint(2.5, 2.1), new ChartPoint(6.5, 3.2) })
                .AddPlotRegion(new(ChartColor.FromHex("#328c68"), xMaximum: 4, yMinimum: 6, label: "Prioritize", opacity: .18))
                .AddPlotRegion(new(ChartColor.FromHex("#5487b8"), xMinimum: 4, yMinimum: 6, label: "Plan", opacity: .15))
                .AddPlotRegion(new(ChartColor.FromHex("#ac9148"), xMaximum: 4, yMaximum: 6, label: "Consider", opacity: .13))
                .AddPlotRegion(new(ChartColor.FromHex("#ba6d67"), xMinimum: 4, yMaximum: 6, label: "Reassess", opacity: .13));
        }
        return chart.WithXAxis("Delay (hours)").WithYAxis("Available capacity (score)")
            .AddBubble("Work queues", new[] { new ChartBubble(1.2, 8.2, 12), new ChartBubble(3.2, 5.1, 36),
                new ChartBubble(5.9, 4.2, 20), new ChartBubble(8.1, 3.8, 49), new ChartBubble(8.7, 7.9, 16) })
            .ConfigureBubble(options => { options.MinimumRadius = 5; options.MaximumRadius = 14; })
            .AddPlotRegion(new(ChartColor.FromHex("#5487b8"), xMinimum: 2, xMaximum: 7, yMinimum: 3, yMaximum: 7,
                label: "Target", opacity: .16))
            .AddPlotRegion(new(ChartColor.FromHex("#ba6d67"), xMinimum: 7, yMaximum: 3, label: "Review", opacity: .18));
    }
}
