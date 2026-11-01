/// <summary>Weighted requests flow from their channels through automatic and manual processing to an outcome.</summary>
public static class SankeyRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var requests = ChartSeries.CreateSankey(new ChartSeriesId("requests"), "Requests", new[] {
            new ChartNode("portal", "Portal"), new ChartNode("email", "Email"), new ChartNode("phone", "Phone"),
            new ChartNode("automatic", "Automatic"), new ChartNode("manual", "Manual"),
            new ChartNode("completed", "Completed"), new ChartNode("review", "Review"), new ChartNode("rejected", "Rejected")
        }, new[] {
            new ChartFlowLink("portal-automatic", "portal", "automatic", 410), new ChartFlowLink("portal-manual", "portal", "manual", 90),
            new ChartFlowLink("email-automatic", "email", "automatic", 160), new ChartFlowLink("email-manual", "email", "manual", 140),
            new ChartFlowLink("phone-manual", "phone", "manual", 200),
            new ChartFlowLink("automatic-completed", "automatic", "completed", 505), new ChartFlowLink("automatic-review", "automatic", "review", 45),
            new ChartFlowLink("automatic-rejected", "automatic", "rejected", 20),
            new ChartFlowLink("manual-completed", "manual", "completed", 330), new ChartFlowLink("manual-review", "manual", "review", 70),
            new ChartFlowLink("manual-rejected", "manual", "rejected", 30)
        });
        if (variant == "sparse") requests.ReplaceFlowData(new[] {
            new ChartNode("received", "Received"), new ChartNode("completed", "Completed"), new ChartNode("review", "Review")
        }, new[] {
            new ChartFlowLink("received-completed", "received", "completed", 85),
            new ChartFlowLink("received-review", "received", "review", 15)
        });
        if (variant != "sparse") {
            // Channels and processing paths take series colours; outcomes take the theme's status colours.
            var colors = VisualTheme.Graphite().Resolve(mode);
            for (var index = 0; index < 5; index++) requests.WithPointColor(index, colors.Palette[index]);
            requests.WithNodeState("completed", ChartSeriesState.Success)
                .WithNodeState("review", ChartSeriesState.Warning)
                .WithNodeState("rejected", ChartSeriesState.Danger);
        }
        return Chart.Create().AddSeries(requests).WithValueFormat(ChartValueFormat.Number("0", CultureInfo.InvariantCulture));
    }
}
