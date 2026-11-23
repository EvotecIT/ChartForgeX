/// <summary>A three-panel comparison with shared Cartesian axes.</summary>
public static class ChartGridRecipe {
    public static ChartGrid Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        var observations = Enumerable.Range(1, 6).Select(index => new ChartPoint(index, 20 + index * 7 % 29)).ToArray();
        var requests = Chart.Create().Title("Observed requests").XLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("requests"), "Requests", observations));
        var reviewed = Chart.Create().Title("Reviewed requests").XLabels("North", "South", "East", "West", "Central", "Online")
            .AddSeries(ChartSeries.CreateBar(new ChartSeriesId("orders"), "Orders", observations));
        var capacity = Chart.Create().Title("Capacity used").XLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
            .AddSeries(ChartSeries.CreateArea(new ChartSeriesId("used-capacity"), "Used capacity", observations));
        return ChartGrid.Create().Columns(2).Gap(16).PanelSize(600, 375)
            .Add(requests).Add(reviewed).Add(capacity, 2).ShareAxes();
    }
}
