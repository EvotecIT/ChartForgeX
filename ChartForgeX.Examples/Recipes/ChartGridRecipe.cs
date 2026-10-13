/// <summary>A three-panel comparison with shared Cartesian axes.</summary>
public static class ChartGridRecipe {
    public static ChartGrid Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var observations = Enumerable.Range(1, 6).Select(index => new ChartPoint(index, 20 + index * 7 % 29)).ToArray();
        var requests = Chart.Create().WithTitle("Observed requests").WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("requests"), "Requests", observations))
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
        var reviewed = Chart.Create().WithTitle("Reviewed requests").WithXLabels("North", "South", "East", "West", "Central", "Online")
            .AddSeries(ChartSeries.CreateBar(new ChartSeriesId("orders"), "Orders", observations))
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
        var capacity = Chart.Create().WithTitle("Capacity used").WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
            .AddSeries(ChartSeries.CreateArea(new ChartSeriesId("used-capacity"), "Used capacity", observations))
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
        return ChartGrid.Create().WithColumns(2).WithGap(16).WithPanelSize(640, 400)
            .Add(requests).Add(reviewed).Add(capacity, 2).WithSharedAxes();
    }
}
