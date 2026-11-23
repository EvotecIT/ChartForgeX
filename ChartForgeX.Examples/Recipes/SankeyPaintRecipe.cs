/// <summary>Endpoint gradients and outlined labels use the same native Sankey scene in SVG and PNG.</summary>
public static class SankeyPaintRecipe {
    public static Chart Create(string variant = "gradient-colors", ThemeMode mode = ThemeMode.Light) {
        var chart = Chart.Create().AddSankey("Requests", new[] {
            new ChartNode("received", "Received"), new ChartNode("review", "Review"), new ChartNode("ready", "Ready")
        }, new[] {
            new ChartFlowLink("standard", "received", "review", 60),
            new ChartFlowLink("priority", "received", "review", 20),
            new ChartFlowLink("delivered", "review", "ready", 80)
        }).Sankey(options => {
            options.FlowColorMode = ChartSankeyFlowColorMode.Gradient;
            options.LabelStyle = ChartSankeyLabelStyle.Outlined;
            options.RibbonOpacity = .6; options.NodeWidth = 16;
        }).ValueFormat(ChartValueFormat.Number("0", CultureInfo.InvariantCulture));
        chart.Series[0].PointColor(0, ChartColor.FromHex("#2864B4"))
            .PointColor(1, ChartColor.FromHex("#BB6428")).PointColor(2, ChartColor.FromHex("#279473"))
            .FlowStyle("priority", new ChartFlowStyle(fill: ChartColor.FromHex("#7954AA"), fillOpacity: .75));
        return chart;
    }
}
