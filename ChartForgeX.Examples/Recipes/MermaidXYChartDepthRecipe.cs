/// <summary>Authors the native Core counterpart to the separately parsed XY source fixture.</summary>
public static class MermaidXYChartDepthRecipe {
    /// <summary>Creates the compact/wide horizontal or vertical chart without loading a source parser.</summary>
    public static Chart Create(string variant = "wide-vertical", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("wide-vertical" or "compact-vertical" or "wide-horizontal" or "compact-horizontal"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var horizontal = variant.EndsWith("-horizontal", StringComparison.Ordinal);
        var chart = Chart.Create().Title("Intake and completed work").Subtitle(string.Empty)
            .Size(variant.StartsWith("compact-", StringComparison.Ordinal) ? 390 : 1100, 520)
            .Theme(VisualTheme.Graphite()).ThemeMode(mode).Legend();
        chart.Options.Lines.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
        var categoryAxis = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        var valueAxis = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        if (horizontal) chart.YAxis(y => y.Title("Month")).XAxis(x => x.Title("Requests"));
        else chart.XAxis(x => x.Title("Month")).YAxis(y => y.Title("Requests"));
        var categories = new[] { "Jan", "Feb", "Mar", "Apr" };
        for (var index = 0; index < categories.Length; index++) categoryAxis.Labels.Add(new ChartAxisLabel(index + 1, categories[index]));
        valueAxis.Min = 0; valueAxis.Max = 100;
        categoryAxis.ShowLine = true; valueAxis.ShowLine = true; categoryAxis.LabelAngle = 0;
        var intake = new[] { new ChartPoint(1, 18), new ChartPoint(2, 70), new ChartPoint(3, 44), new ChartPoint(4, 85) };
        if (horizontal) chart.AddHorizontalBar("Intake", intake); else chart.AddBar("Intake", intake);
        chart.Series[0].ShowDataLabels(false).DataLabelPlacement(ChartDataLabelPlacement.Outside);
        chart.AddLine("Completed", new[] { new ChartPoint(1, 12), new ChartPoint(2, 60), new ChartPoint(3, 33), new ChartPoint(4, 75) });
        var line = chart.Series[1];
        var captions = new[] { "Opened", "", "Reviewed, ready", "Released" };
        for (var index = 0; index < captions.Length; index++) line.PointLabel(index, captions[index]);
        line.ShowDataLabels(true);
        chart.TitleStyle(style => style.FontSize = 22);
        chart.LegendStyle(style => style.FontSize = 14);
        return chart;
    }
}
