/// <summary>Assessment counts shown through repeated person symbols.</summary>
public static class PictorialRecipe {
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        var chart = Chart.Create().AddPictorial("Assessments", new[] {
            new ChartPictorialItem("Team A", 74), new ChartPictorialItem("Team B", 46), new ChartPictorialItem("Team C", 61)
        }, ChartPictorialShape.Person).Pictorial(p => p.Max(100).ValuePerSymbol(10).Columns(10));
        if (variant == "options") chart.Pictorial(p => p.SvgPath("M2 2H22V22H2Z M8 8V16H16V8Z", new ChartRect(0, 0, 24, 24)));
        return chart;
    }
}
