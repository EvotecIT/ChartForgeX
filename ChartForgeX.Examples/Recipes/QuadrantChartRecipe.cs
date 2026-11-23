/// <summary>A quadrant chart: proposed projects placed by effort and impact, with four labelled regions.</summary>
public static class QuadrantChartRecipe {
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var projects = new (string Name, double Effort, double Impact)[] {
            ("SSO rollout", 2.1, 8.6), ("Backup audit", 1.4, 6.9), ("MFA for admins", 1.1, 9.2), ("DNS cleanup", 2.8, 6.4),
            ("Data centre move", 8.7, 8.1), ("New SIEM", 7.6, 7.4), ("Zero trust", 9.1, 9.0),
            ("Wiki refresh", 1.8, 2.6), ("Printer fleet", 3.1, 1.7), ("Desk booking", 2.4, 3.8),
            ("ERP upgrade", 8.2, 3.9), ("Legacy CRM port", 6.9, 2.2), ("Intranet redesign", 6.1, 4.4)
        };
        var count = variant == "compact" ? 8 : projects.Length;
        var series = ChartSeries.CreateScatter(new ChartSeriesId("projects"), "Projects",
            projects.Take(count).Select(project => new ChartPoint(project.Effort, project.Impact)));
        for (var index = 0; index < count; index++) series.PointLabel(index, projects[index].Name);
        series.Markers(markers => { markers.Radius = 6; });
        var colors = VisualTheme.Graphite().Resolve(mode);
        return Chart.Create().XAxis(x => x.Bounds(0, 10)).YAxis(y => y.Bounds(0, 10)).Legend(false).DataLabels()
            .XAxis(x => x.Title("Effort (person-months)")).YAxis(y => y.Title("Impact (score)")).AddSeries(series)
            .AddPlotRegion(new(colors.Status.Pass.Fill, xMaximum: 5, yMinimum: 5, label: "Quick wins", opacity: .14))
            .AddPlotRegion(new(colors.Palette.First(), xMinimum: 5, yMinimum: 5, label: "Major projects", opacity: .12))
            .AddPlotRegion(new(colors.Status.Medium.Fill, xMaximum: 5, yMaximum: 5, label: "Fill-ins", opacity: .12))
            .AddPlotRegion(new(colors.Status.Critical.Fill, xMinimum: 5, yMaximum: 5, label: "Thankless tasks", opacity: .1));
    }
}
