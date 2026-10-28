/// <summary>Authors the native Core counterpart to the separately parsed XY source fixture.</summary>
public static class MermaidXYChartDepthRecipe {
    /// <summary>Creates the compact/wide horizontal or vertical chart without loading a source parser.</summary>
    public static Chart Create(string variant = "wide-vertical", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide-vertical" or "compact-vertical" or "wide-horizontal" or "compact-horizontal"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var horizontal = variant.EndsWith("-horizontal", StringComparison.Ordinal);
        var chart = Chart.Create().WithTitle("Intake and completed work").WithSubtitle(string.Empty)
            .WithSize(variant.StartsWith("compact-", StringComparison.Ordinal) ? 390 : 1100, 520)
            .WithTheme(VisualTheme.Graphite(), mode).WithLegend();
        chart.Options.LineOrientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
        var categoryAxis = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        var valueAxis = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        if (horizontal) chart.WithYAxis("Month").WithXAxis("Requests");
        else chart.WithXAxis("Month").WithYAxis("Requests");
        var categories = new[] { "Jan", "Feb", "Mar", "Apr" };
        for (var index = 0; index < categories.Length; index++) categoryAxis.Labels.Add(new ChartAxisLabel(index + 1, categories[index]));
        valueAxis.Minimum = 0; valueAxis.Maximum = 100;
        categoryAxis.ShowLine = true; valueAxis.ShowLine = true; categoryAxis.LabelAngle = 0;
        var intake = new[] { new ChartPoint(1, 18), new ChartPoint(2, 70), new ChartPoint(3, 44), new ChartPoint(4, 85) };
        if (horizontal) chart.AddHorizontalBar("Intake", intake); else chart.AddBar("Intake", intake);
        chart.Series[0].WithDataLabels(false).WithDataLabelPlacement(ChartDataLabelPlacement.Outside);
        chart.AddLine("Completed", new[] { new ChartPoint(1, 12), new ChartPoint(2, 60), new ChartPoint(3, 33), new ChartPoint(4, 75) });
        var line = chart.Series[1];
        var captions = new[] { "Opened", "", "Reviewed, ready", "Released" };
        for (var index = 0; index < captions.Length; index++) line.WithPointLabel(index, captions[index]);
        line.WithDataLabels();
        chart.ConfigureTitleStyle(style => style.FontSize = 22);
        chart.ConfigureLegendStyle(style => style.FontSize = 14);
        return chart;
    }
}
