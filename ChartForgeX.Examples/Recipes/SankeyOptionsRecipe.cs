/// <summary>Aligned and ordered flows retain parallel-link identities and independent link paint.</summary>
public static class SankeyOptionsRecipe {
    public static Chart Create(string variant = "options", VisualThemeMode mode = VisualThemeMode.Light) {
        var requests = ChartSeries.CreateSankey(new ChartSeriesId("request-flows"), "Requests", new[] {
            new ChartNode("received", "Received"), new ChartNode("north", "North"), new ChartNode("south", "South"),
            new ChartNode("north-support", "Support"), new ChartNode("south-support", "Support"), new ChartNode("completed", "Completed"),
            new ChartNode("review", "Review"), new ChartNode("imported", "Imported")
        }, new[] {
            new ChartFlowLink("received-north", "received", "north", 72), new ChartFlowLink("received-south", "received", "south", 28),
            new ChartFlowLink("north-standard", "north", "north-support", 52), new ChartFlowLink("north-priority", "north", "north-support", 20),
            new ChartFlowLink("south-standard", "south", "south-support", 28),
            new ChartFlowLink("north-completed", "north-support", "completed", 72), new ChartFlowLink("south-completed", "south-support", "completed", 28),
            new ChartFlowLink("received-review", "received", "review", 5), new ChartFlowLink("imported-completed", "imported", "completed", 10)
        });
        requests.WithNodeState("north-support", ChartSeriesState.Warning).WithPointColor(0, ChartColor.FromHex("#7356BD"))
            .WithFlowStyle("north-priority", new ChartFlowStyle(fill: ChartColor.FromHex("#AF6B24"), fillOpacity: .7,
                fillPattern: ChartFillPattern.DiagonalForward, stroke: ChartColor.FromHex("#764415"), strokeWidth: 1))
            .WithFlowState("north-priority", ChartSeriesState.Warning);
        return Chart.Create().AddSeries(requests).ConfigureSankey(options => {
            options.Alignment = ChartSankeyAlignment.Center;
            options.VerticalAlignment = ChartSankeyVerticalAlignment.Top;
            options.NodeOrder = ChartSankeyNodeOrder.LabelAscending;
            options.LabelPlacement = ChartSankeyLabelPlacement.Center;
            options.EdgeLabelPlacement = ChartSankeyEdgeLabelPlacement.Outside;
            options.NodeWidth = 16; options.NodeGap = 12; options.NodeCornerRadius = 1;
            options.RibbonFill = ChartColor.FromHex("#607DAA"); options.RibbonOpacity = .45;
        }).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}
