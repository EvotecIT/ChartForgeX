/// <summary>Compares delivery-stage orders on a physical horizontal value axis.</summary>
public static class LollipopHorizontalRecipe {
    /// <summary>Creates the horizontal example through the canonical lollipop and physical-axis owners.</summary>
    public static Chart Create(string variant = "horizontal", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("horizontal" or "compact-horizontal")) throw new ArgumentOutOfRangeException(nameof(variant));
        var points = Enumerable.Range(1, 6).Select(index => new ChartPoint(index, 20 + index * 7 % 29));
        var orders = new ChartSeries(new ChartSeriesId("orders"), "Orders", ChartSeriesKind.Lollipop, points)
            .WithObservationIds(new[] { "received", "packed", "shipped", "transit", "delivered", "reviewed" }.Select(id => new ChartObservationId(id)));
        var chart = Chart.Create().WithXLabels("Received", "Packed", "Shipped", "In transit", "Delivered", "Reviewed")
            .WithXAxis("Orders").WithYAxis("Delivery stage").WithLegend(false).AddSeries(orders)
            .WithValueFormat(ChartValueFormat.Number("0", CultureInfo.InvariantCulture));
        chart.Options.LollipopOrientation = ChartOrientation.Horizontal;
        return chart;
    }
}
