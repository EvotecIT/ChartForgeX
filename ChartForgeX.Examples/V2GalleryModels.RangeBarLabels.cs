using ChartForgeX.Core;

public static partial class V2GalleryModels {
    private static Chart RangeBarEndpointLabels(string variant) {
        var horizontal = variant.Contains("horizontal");
        var chart = Chart.Create().WithXLabels("Standard", "Express", "Bulk")
            .AddRangeBar("Expected", new[] { new ChartInterval(1, 15, 35), new ChartInterval(2, 10, 30), new ChartInterval(3, 25, 50) })
            .AddRangeBar("Observed", new[] { new ChartInterval(1, 20, 42), new ChartInterval(2, 32, 12), new ChartInterval(3, 30, 55) })
            .ConfigureRangeBar(options => {
                options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
                options.LabelContent = ChartRangeBarLabelContent.Endpoints;
                options.EndpointLabelFormatter = label => label.FormattedValue + (label.Endpoint == ChartRangeBarEndpoint.Start ? " S" : " E");
            }).WithBarStyle(ChartBarStyle.Flat).WithDataLabels();
        chart.Series[1].WithPointLabel(1, "Recorded express delivery");
        (horizontal ? chart.Options.XAxis : chart.Options.YAxis).WithBounds(0, 70);
        if (horizontal) {
            chart.WithXAxis("Delivery time (minutes)").WithYAxis("Service");
            chart.ConfigureDataLabelStyle(style => style.WithFontSize(10));
        }
        else chart.WithYAxis("Delivery time (minutes)");
        return chart;
    }
}
