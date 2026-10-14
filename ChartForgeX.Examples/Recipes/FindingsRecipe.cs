/// <summary>Findings by category with explicit zero, empty, dense and semantic-color variants.</summary>
public static class FindingsRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var count = variant == "empty" ? 0 : variant == "dense" ? 24 : 7;
        var points = Enumerable.Range(0, count).Where(index => variant != "missing" || index is not (2 or 3))
            .Select(index => new ChartPoint(index + 1, variant == "zero" ? 0 : 20 + index * 3 % 23));
        var findings = new ChartSeries(new ChartSeriesId("findings"), "Findings", ChartSeriesKind.Donut, points);
        if (variant is "explicit-status" or "explicit-series") {
            var colors = VisualTheme.Graphite().Resolve(mode);
            var selected = variant == "explicit-status"
                ? new[] { colors.Status.Pass.Fill, colors.Status.Medium.Fill, colors.Status.Critical.Fill }
                : colors.Palette.Take(3).ToArray();
            for (var index = 0; index < findings.Points.Count; index++) findings.WithPointColor(index, selected[index % selected.Length]);
        }
        return Chart.Create().WithXLabels(Enumerable.Range(1, count).Select(index => variant == "wrapped-legend"
            ? "Assessment category " + index + " requiring follow-up" : "Day " + index).ToArray()).AddSeries(findings);
    }
}
