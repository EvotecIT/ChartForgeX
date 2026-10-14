/// <summary>Weighted requests flow through automatic and manual processing.</summary>
public static class SankeyRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var requests = ChartSeries.CreateSankey(new ChartSeriesId("requests"), "Requests", new[] {
            new ChartNode("received", "Received"), new ChartNode("automatic", "Automatic"), new ChartNode("manual", "Manual"),
            new ChartNode("completed", "Completed"), new ChartNode("review", "Review")
        }, new[] {
            new ChartFlowLink("received-automatic", "received", "automatic", 72), new ChartFlowLink("received-manual", "received", "manual", 28),
            new ChartFlowLink("automatic-completed", "automatic", "completed", 65), new ChartFlowLink("automatic-review", "automatic", "review", 7),
            new ChartFlowLink("manual-completed", "manual", "completed", 20), new ChartFlowLink("manual-review", "manual", "review", 8)
        });
        if (variant == "sparse") requests.ReplaceFlowData(new[] {
            new ChartNode("received", "Received"), new ChartNode("completed", "Completed"), new ChartNode("review", "Review")
        }, new[] {
            new ChartFlowLink("received-completed", "received", "completed", 85),
            new ChartFlowLink("received-review", "received", "review", 15)
        });
        return Chart.Create().AddSeries(requests).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}
