/// <summary>Assessment counts shown through repeated person symbols.</summary>
public static class PictorialRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var chart = Chart.Create().AddPictorial("Assessments", new[] {
            new ChartPictorialItem("Team A", 74), new ChartPictorialItem("Team B", 46), new ChartPictorialItem("Team C", 61)
        }, ChartPictorialShape.Person).WithPictorialMaximum(100).WithPictorialValuePerSymbol(10).WithPictorialColumns(10);
        if (variant == "options") chart.WithPictorialSvgPath("M2 2H22V22H2Z M8 8V16H16V8Z", new ChartRect(0, 0, 24, 24));
        return chart.WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}
