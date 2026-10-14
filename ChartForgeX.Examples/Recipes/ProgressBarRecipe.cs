/// <summary>Labeled progress rows with a shared maximum and optional track styling.</summary>
public static class ProgressBarRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var chart = Chart.Create().AddProgressBars("Delivery", new[] {
            new ChartProgressItem("Reviewed", 76), new ChartProgressItem("Verified", 58), new ChartProgressItem("Complete", 42)
        }).WithProgressHandles();
        if (variant == "options") chart.WithProgressMaximum(120).WithProgressBarThickness(.42).WithProgressTrackOpacity(.3);
        return chart.WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}
